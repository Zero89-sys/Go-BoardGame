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

        // Starting the engine
        public void StartEngine(string exePath, string modelPath, string configPath)
        {
            if (_process != null && !_process.HasExited) return;

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

                var result = await _currentCommandTcs.Task;
                Debug.WriteLine($"GTP Receive: {result}");
                return result;
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

        public async Task<string> GetScoreEstimation()
        {
            return await SendCommand("kata-score-est");
        }

        public void StopEngine()
        {
            if (_process != null && !_process.HasExited)
            {
                _input?.WriteLine("quit");
                _process.Kill();
            }
        }


        // Analysis
        public async Task<string> GetKataScoreEstimate()
        {
            var response = await SendCommand("kata-get-score");
            return response;
        }

        public async Task StartOwnershipAnalysisAsync(int boardSize)
        {
            _currentBoardSize = boardSize;
            LastOwnership = null;

            await SendCommand($"boardsize {boardSize}");
            await SendRawCommand("kata-analyze ownership true interval 50");
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
                Debug.WriteLine($"[GTP] Ownership data příliš krátká: {parts.Length}/{expectedLength}");
                return;
            }

            try
            {
                LastOwnership = parts
                    .Take(expectedLength)
                    .Select(p => double.Parse(p, CultureInfo.InvariantCulture))
                    .ToArray();

                Debug.WriteLine($"Ownership COMPLETE ({expectedLength} polí)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chyba při parsování ownership: {ex.Message}");
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

        private void ParseAnalysisLine(string line)
        {
            if (line.Contains("ownership"))
            {
                ParseOwnership(line, _currentBoardSize);
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
            Debug.WriteLine("GTP Service byl resetován");
        }

        public void ClearAnalysisState()
        {
            LastOwnership = null;
            _currentCommandTcs = null;
        }
    }
}
