using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ImVision.Integration.Options;
using Microsoft.Extensions.Options;

namespace ImVision.Integration.Http;

public class ImVisionAuthHandler : DelegatingHandler
{
    private readonly ImVisionOptions _options;
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiration = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1, 1);

    public ImVisionAuthHandler(IOptions<ImVisionOptions> options)
    {
        _options = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_options.AuthenticationType.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            SetBasicAuth(request);
            return await base.SendAsync(request, cancellationToken);
        }
        
        if (_options.AuthenticationType.Equals("Token", StringComparison.OrdinalIgnoreCase))
        {
            // Nếu người dùng cấu hình cứng Token trong file appsettings, dùng luôn
            if (!string.IsNullOrWhiteSpace(_options.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
                return await base.SendAsync(request, cancellationToken);
            }

            // Nếu không, tự động gọi API lấy Token và tự động refresh
            var token = await GetOrFetchTokenAsync(request.RequestUri, cancellationToken);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Nếu gặp lỗi 401 Unauthorized, Token có thể bị hết hạn hoặc thu hồi.
            // Ta sẽ xóa Cache, lấy Token mới và gọi lại Request đó 1 lần nữa.
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _cachedToken = null;
                token = await GetOrFetchTokenAsync(request.RequestUri, cancellationToken);
                
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    response.Dispose(); // Xóa response 401 cũ
                    response = await base.SendAsync(request, cancellationToken); // Thử lại
                }
            }

            return response;
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private void SetBasicAuth(HttpRequestMessage request)
    {
        var authString = $"{_options.Username}:{_options.Password}";
        var base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64AuthString);
    }

    private async Task<string?> GetOrFetchTokenAsync(Uri? requestUri, CancellationToken cancellationToken)
    {
        // Kiểm tra xem Token còn hạn trong cache không
        if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiration)
        {
            return _cachedToken;
        }

        // Chặn luồng để đảm bảo chỉ 1 request đi lấy Token, các request khác chờ
        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Kiểm tra lại sau khi vào lock
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiration)
            {
                return _cachedToken;
            }

            // Lấy BaseUrl
            var baseUrl = _options.BaseUrl;
            if (string.IsNullOrEmpty(baseUrl) && requestUri != null)
            {
                baseUrl = $"{requestUri.Scheme}://{requestUri.Host}:{requestUri.Port}/";
            }

            if (string.IsNullOrEmpty(baseUrl)) return null;

            // API Endpoint là /token theo tài liệu imVision
            var tokenEndpoint = new Uri(new Uri(baseUrl), "token");

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            
            // Tài liệu imVision: "we use basic authentication and send a valid SM username password in a token request call"
            SetBasicAuth(tokenRequest);

            // Truyền thêm cả form parameters (grant_type=password) cho đúng chuẩn OAuth2
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", _options.Username ?? ""),
                new KeyValuePair<string, string>("password", _options.Password ?? "")
            });
            tokenRequest.Content = formContent;

            // Gọi API, dùng base.SendAsync để vượt qua handler hiện tại
            var response = await base.SendAsync(tokenRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var contentString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = JsonDocument.Parse(contentString);
            
            if (jsonDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            {
                _cachedToken = accessTokenElement.GetString();
                
                // Mặc định thời hạn token của imVision là 60 phút
                int expiresInSeconds = 3599; 
                if (jsonDoc.RootElement.TryGetProperty("expires_in", out var expiresElement) && expiresElement.TryGetInt32(out var expires))
                {
                    expiresInSeconds = expires;
                }

                // Cài đặt hạn sử dụng Token: Trừ hao 1 phút để hệ thống tự động làm mới trước khi chết hẳn
                _tokenExpiration = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds - 60);
                
                return _cachedToken;
            }

            return null;
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }
}
