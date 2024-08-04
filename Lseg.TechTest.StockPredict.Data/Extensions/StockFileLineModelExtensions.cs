using System.Globalization;
using System.Net;
using CSharpFunctionalExtensions;
using Lseg.TechTest.StockPredict.Data.Models;
using Lseg.TechTest.StockPredict.Shared;

namespace Lseg.TechTest.StockPredict.Data.Extensions;

public static class StockFileLineModelExtensions
{
    public static Result<StockFileLineModel,Error> ToStockFileLineModel(this string csvLine,string path)
    {
        var malformedLineResult = Result.Failure<StockFileLineModel, Error>(new Error(HttpStatusCode.BadRequest,
            $"Line Malformed - Path [{path}] - Line [{csvLine}]"));
        var tokens = csvLine.Split(',');
        if (tokens.Length < 3) return malformedLineResult;

        var stockFileLineResult = new StockFileLineModel("",DateTime.MinValue, 0);
        
        if (string.IsNullOrWhiteSpace(tokens[0])) return malformedLineResult;
        stockFileLineResult = stockFileLineResult with { CompanyTicker = tokens[0] };

        if (!DateTime.TryParseExact(tokens[1], "dd-MM-yyyy", CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out var parsedDate)) return malformedLineResult;
        stockFileLineResult = stockFileLineResult with { TimeStamp = parsedDate };

        if (!double.TryParse(tokens[2], out var parsedStockValue)) return malformedLineResult;
        stockFileLineResult = stockFileLineResult with { StockValue = parsedStockValue };


        return stockFileLineResult;
    }
}