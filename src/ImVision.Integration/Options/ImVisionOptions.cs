namespace ImVision.Integration.Options;

public class ImVisionOptions
{
    public const string SectionName = "ImVision";
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthenticationType { get; set; } = "Basic"; // Basic or Token
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 500;
}
