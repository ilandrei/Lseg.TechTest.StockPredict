using System.Net;

namespace Lseg.TechTest.StockPredict.Shared;

public sealed class Error
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

    public Error(HttpStatusCode httpStatusCode, string message, Exception? exception = null)
    {
        HttpStatusCode = httpStatusCode;
        Message = message;
        Exception = exception;
    }

    public HttpStatusCode HttpStatusCode { get; }

    public string Message { get; }

    public Exception? Exception { get; }
}
