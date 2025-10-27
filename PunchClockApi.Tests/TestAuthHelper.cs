using System.Net.Http.Json;
using System.Text.Json;

namespace PunchClockApi.Tests;

/// <summary>
/// Helper class for authentication in tests.
/// </summary>
public static class TestAuthHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Login with credentials and return access token.
    /// </summary>
    public static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var loginRequest = new { username, password };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, JsonOptions);
        
        return loginResponse?.AccessToken 
            ?? throw new InvalidOperationException("Login failed - no access token returned");
    }

    /// <summary>
    /// Add authorization header to HttpClient.
    /// </summary>
    public static void AddAuthHeader(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}
