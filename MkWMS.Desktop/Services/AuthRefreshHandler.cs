using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MkWMS.Desktop.Services
{






    public class AuthRefreshHandler : DelegatingHandler
    {
        private readonly AuthService _auth;
        private readonly string _baseAddress;
        private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        private static Task<bool>? _inFlightRefresh;
        private static readonly object _lockObj = new();

        public AuthRefreshHandler(AuthService auth, string baseAddress, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _auth = auth;
            _baseAddress = baseAddress;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            var path = request.RequestUri?.AbsolutePath ?? "";
            bool isAuthEndpoint = path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase)
                               || path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase);

            if (response.StatusCode != HttpStatusCode.Unauthorized || isAuthEndpoint || string.IsNullOrEmpty(_auth.RefreshToken))
                return response;

            bool refreshed = await RefreshOnceAsync();
            if (!refreshed)
                return response;

            var retryRequest = await CloneRequestAsync(request);
            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);

            response.Dispose();
            return await base.SendAsync(retryRequest, cancellationToken);
        }





        private Task<bool> RefreshOnceAsync()
        {
            lock (_lockObj)
            {
                if (_inFlightRefresh == null || _inFlightRefresh.IsCompleted)
                    _inFlightRefresh = DoRefreshAsync();
                return _inFlightRefresh;
            }
        }

        private async Task<bool> DoRefreshAsync()
        {
            var refreshToken = _auth.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            try
            {
                using var certBypass = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                using var plainClient = new HttpClient(certBypass) { BaseAddress = new Uri(_baseAddress) };

                var resp = await plainClient.PostAsJsonAsync("auth/refresh", new { RefreshToken = refreshToken });
                if (!resp.IsSuccessStatusCode)
                {



                    _auth.Logout();
                    return false;
                }

                var json = await resp.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<RefreshResponseDto>(json, _json);
                if (dto?.Token == null || dto.RefreshToken == null)
                    return false;

                _auth.SetTokens(dto.Token, dto.RefreshToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            if (request.Content != null)
            {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);
                foreach (var header in request.Content.Headers)
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }

        private class RefreshResponseDto
        {
            public string? Token { get; set; }
            public string? RefreshToken { get; set; }
        }
    }
}
