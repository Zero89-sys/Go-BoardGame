using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gogame.Models
{
    public class GtpService
    {
        private Process? _process;
        private StreamWriter? _input;
        private StreamReader? _output;
        private CancellationTokenSource? _analysisCts;
        private readonly SemaphoreSlim _ioLock = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<string>? _currentCommandTcs;
        private int _currentBoardSize = 19;
        public double[]? LastOwnership { get; private set; }
        public static GtpService Instance { get; } = new GtpService();
        public bool IsAvailable { get; private set; } = false;

        // Starting the engine
        public async Task<bool> StartEngineAsync(string exePath, string modelPath, string configPath)
        {
            if (_process != null && !_process.HasExited && IsAvailable) return true;

            IsAvailable = false;
            
            if(!File.Exists(exePath) || !File.Exists(modelPath) || !File.Exists(configPath))
            {
                Debug.WriteLine("[GTP] Bot files (EXE, Model, or Config) were not found!");
                return false;
            }

            try
            {
                _process = new Process();
                _process.StartInfo.FileName = exePath;

                _process.StartInfo.Arguments = $"gtp -model \"{modelPath}\" -config \"{configPath}\"";

                _process.StartInfo.WorkingDirectory = Path.GetDirectoryName(exePath);

                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.RedirectStandardInput = true;
                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.CreateNoWindow = true;

                _process.StartInfo.RedirectStandardError = true;

                _process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Debug.WriteLine("Katago error: " + e.Data);
                    }
                };

                _process.Start();
                _process.BeginErrorReadLine();

                _input = _process.StandardInput;
                _output = _process.StandardOutput;
                _ = Task.Run(ReadOutputLoop);

                var pingTask = SendCommand("name");
                var completedTask = await Task.WhenAny(pingTask, Task.Delay(5000));

                if(completedTask == pingTask && pingTask.Result != null)
                {
                    IsAvailable = true;
                    Debug.WriteLine("[GTP] Engine has been successfully started.");
                    return true;
                }
                else
                {
                    Debug.WriteLine("[GTP] Engine did not respond in time (Timeout/Error).");
                    StopEngine();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GTP] Exception while starting the bot: {ex.Message}");
                StopEngine();
                return false;
            }
        }

        // Engine function
        public async Task<string?> SendCommand(string command)
        {
            if (_process == null || _process.HasExited) return null;

            await _ioLock.WaitAsync();
            try
            {
                _currentCommandTcs?.TrySetCanceled();
                _currentCommandTcs = new TaskCompletionSource<string>();

                Debug.WriteLine($"GTP Send: {command}");
                await _input!.WriteLineAsync(command);
                await _input.FlushAsync();

                var completedTask = await Task.WhenAny(_currentCommandTcs.Task, Task.Delay(5000));
                if(completedTask == _currentCommandTcs.Task)
                {
                    var result = await _currentCommandTcs.Task;
                    Debug.WriteLine($"GTP Receive: {result}");
                    return result;
                }
                Debug.WriteLine($"GTP Timeout for command: {command}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GTP Error: {ex.Message}");
                return null;
            }
            finally
            {
                _ioLock.Release();
            }
        }

        public void StopEngine()
        {
            IsAvailable = false;
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    _input?.WriteLine("quit");
                    _process.Kill();
                }
                catch { }
            }
            _process = null;
        }

        public async Task StartOwnershipAnalysisAsync(int boardSize)
        {
            _currentBoardSize = boardSize;
            LastOwnership = null;

            await SendCommand($"boardsize {boardSize}");
            await SendRawCommand("kata-analyze B interval 50 ownership true");
        }

        public async Task SetBoardSize(int size)
        {
            _currentBoardSize = size;
            await SendCommand($"boardsize {size}");
        }


        public async Task StopAnalysisAsync()
        {
            await SendRawCommand("stop");
        }

        private void ParseOwnership(string line, int size)
        {
            int idx = line.IndexOf("ownership");
            if (idx < 0) return;

            string dataPart = line.Substring(idx + "ownership".Length).Trim();
            var parts = dataPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int expectedLength = size * size;
            if (parts.Length < expectedLength)
            {
                Debug.WriteLine($"[GTP] Ownership data too short: {parts.Length}/{expectedLength}");
                return;
            }

            try
            {
                LastOwnership = parts
                    .Take(expectedLength)
                    .Select(p => double.Parse(p, CultureInfo.InvariantCulture))
                    .ToArray();

                Debug.WriteLine($"Ownership COMPLETE ({expectedLength} fields)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing ownership: {ex.Message}");
            }
        }

        private async Task ReadOutputLoop()
        {
            while (_process != null && !_process.HasExited)
            {
                var line = await _output!.ReadLineAsync();
                if (line == null) break;

                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                Debug.WriteLine($"[KATA] {line}");

                if (line.StartsWith("=") || line.StartsWith("?"))
                {
                    _currentCommandTcs?.TrySetResult(line);
                }
                else if (line.Contains("ownership"))
                {
                    ParseOwnership(line, _currentBoardSize);
                }
            }
        }

        public async Task SendRawCommand(string command)
        {
            if (_process == null || _process.HasExited) return;

            Debug.WriteLine($"GTP RAW: {command}");
            await _input!.WriteLineAsync(command);
            await _input.FlushAsync();
        }

        public void ResetServiceState()
        {
            _currentCommandTcs?.TrySetCanceled();
            _currentCommandTcs = null;

            if (_ioLock.CurrentCount == 0)
            {
                _ioLock.Release();
            }
            Debug.WriteLine("GTP Service was reset");
        }

        public void ClearAnalysisState()
        {
            LastOwnership = null;
            _currentCommandTcs = null;
        }
    }
}
