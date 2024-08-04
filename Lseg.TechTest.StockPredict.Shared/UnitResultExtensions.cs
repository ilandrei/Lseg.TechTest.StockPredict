using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace Lseg.TechTest.StockPredict.Shared;

public static class UnitResultExtensions
{
    public static ObjectResult ToHttpResponse(this Error error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = (int)error.HttpStatusCode
        };
    }
}
