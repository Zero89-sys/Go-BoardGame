using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using Gogame.Dto;
using Avalonia.Media.Imaging;
using System.IO;

namespace Gogame.Models
{
    public class ApiResult<T>
    {
        public bool Success { get; init; }
        public T? Data {  get; init; }
        public Dictionary<string, List<string>> Errors { get; init; } = new();

        public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static ApiResult<T> Fail(Dictionary<string, List<string>> errors) => new() { Errors = errors };
    }
    public class ApiClient
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public ApiClient(HttpClient http) => _http = http;

        public void SetToken(string token) =>
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        public void ClearToken() =>
            _http.DefaultRequestHeaders.Authorization = null;

        // Registration endpoint
        public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest req)
            => PostAsync<AuthResponse>("api/users", req);

        // Login endpoint
        public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest req)
            => PostAsync<AuthResponse>("api/users/login", req);

        // Profile data endpoint
        public Task<ApiResult<UserProfileDto>> GetProfileAsync()
            => SendAsync<UserProfileDto>("api/users/profile-data", HttpMethod.Get, null);

        // Sends an HTTP POST request
        private async Task<ApiResult<T>> PostAsync<T>(string url, object body)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(url, body, Json);
                System.Diagnostics.Debug.WriteLine($"{url} -> {(int)response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    var data = string.IsNullOrWhiteSpace(raw)
                        ? default
                        : JsonSerializer.Deserialize<T>(raw, Json);
                    return ApiResult<T>.Ok(data!);
                }

                return ApiResult<T>.Fail(await ReadErrorsAsync(response));
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return ApiResult<T>.Fail(new() { [""] = new() { $"Request failed: {ex.Message}" } });
            }
        }

        // Sends a generalized HTTP request
        private async Task<ApiResult<T>> SendAsync<T>(string url, HttpMethod method, object? body)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);

                if(body != null)
                {
                    request.Content = JsonContent.Create(body, options: Json);
                }

                var response = await _http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<T>(Json);
                    return ApiResult<T>.Ok(data!);
                }

                return ApiResult<T>.Fail(await ReadErrorsAsync(response));
            }
            catch(Exception ex) when (ex is HttpRequestException or OperationCanceledException)
            {
                return ApiResult<T>.Fail(new() { [""] = new() { "Cannot reach the server or request timed out." } });
            }
        }

        // Image download
        public async Task<Bitmap?> LoadImageAsync(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            try
            {
                var bytes = await _http.GetByteArrayAsync(path);
                using var ms = new MemoryStream(bytes);
                return new Bitmap(ms);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Avatar load failed ({path}): {ex.Message}");
                return null;
            }
        }

        // Parses error details from a failed HTTP response
        private static async Task<Dictionary<string, List<string>>> ReadErrorsAsync(HttpResponseMessage response)
        {
            try
            {
                var raw = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("errors", out var inner) &&
                    inner.ValueKind == JsonValueKind.Object)
                {
                    root = inner;
                }

                var errors = root.Deserialize<Dictionary<string, List<string>>>(Json);
                if (errors is { Count: > 0 })
                    return new Dictionary<string, List<string>>(errors, StringComparer.OrdinalIgnoreCase); ;
            }
            catch (JsonException) { }

            return new() { [""] = new() { $"Request failed ({(int)response.StatusCode})." } };
        }
    }
}
