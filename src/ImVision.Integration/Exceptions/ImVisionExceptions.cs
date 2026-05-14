namespace ImVision.Integration.Exceptions;

public class ImVisionException : Exception
{
    public int StatusCode { get; }

    public ImVisionException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public ImVisionException(string message, Exception innerException, int statusCode) : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

public class ImVisionBadRequestException : ImVisionException
{
    public ImVisionBadRequestException(string message) : base(message, 400) { }
}

public class ImVisionUnauthorizedException : ImVisionException
{
    public ImVisionUnauthorizedException(string message) : base(message, 401) { }
}

public class ImVisionNotFoundException : ImVisionException
{
    public ImVisionNotFoundException(string message) : base(message, 404) { }
}

public class ImVisionServerException : ImVisionException
{
    public ImVisionServerException(string message, int statusCode) : base(message, statusCode) { }
}
