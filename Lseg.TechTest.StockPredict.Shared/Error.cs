using System.Net;

namespace Lseg.TechTest.StockPredict.Shared;

public sealed class Error(HttpStatusCode httpStatusCode, string message, Exception? exception = null)
{
    public Error(Exception exception)
        : this(HttpStatusCode.BadRequest, string.Empty, exception)
    {
    }


    public Error(string message)
        : this(HttpStatusCode.BadRequest, message, null)
    {
    }

    public Error(HttpStatusCode httpStatusCode)
        : this(httpStatusCode, string.Empty, null)
    {
    }

    public HttpStatusCode HttpStatusCode { get; } = httpStatusCode;

    public string Message { get; } = message;

    public Exception? Exception { get; } = exception;
}
