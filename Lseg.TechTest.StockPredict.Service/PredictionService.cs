using System.Net;
using CSharpFunctionalExtensions;
using Lseg.TechTest.StockPredict.Data.Models;
using Lseg.TechTest.StockPredict.Service.Interfaces;
using Lseg.TechTest.StockPredict.Shared;
using Lseg.TechTest.StockPredict.Shared.Enums;
using MathNet.Numerics;


namespace Lseg.TechTest.StockPredict.Service;

public class PredictionService:IPredictionService
{
    public Result<List<StockFileLineModel>, Error> PredictStocks(List<StockFileLineModel> input, PredictionAlgorithm algorithm, int predictionsCount)
    {
        if (input.Count == 0)
            return Result.Failure<List<StockFileLineModel>, Error>(new Error(HttpStatusCode.BadRequest,
                "Training input is empty"));
        if(predictionsCount is <= 0 or > 100000) 
            return Result.Failure<List<StockFileLineModel>, Error>(new Error(HttpStatusCode.BadRequest,
                "Prediction count is not valid"));
        
        return algorithm switch
        {
            PredictionAlgorithm.Primitive => PredictStocksPrimitive(input),
            PredictionAlgorithm.LinearRegression => PredictStocksLinearRegression(input, predictionsCount),
            _ => Result.Failure<List<StockFileLineModel>, Error>(new Error(HttpStatusCode.BadRequest,
                "Algorithm not supported"))
        };
    }

    

    private static Result<List<StockFileLineModel>, Error> PredictStocksPrimitive(List<StockFileLineModel> input)
    {
        var secondHighest = input.OrderByDescending(stock => stock.StockValue).Skip(1).FirstOrDefault();
        if (secondHighest == null)
            return Result.Failure<List<StockFileLineModel>, Error>(new Error(HttpStatusCode.BadRequest, "Not enough training points"));
        
        input.Add(secondHighest with { TimeStamp = input.Last().TimeStamp.AddDays(1) });
        input.Add(input.Last() with { TimeStamp = input.Last().TimeStamp.AddDays(1), 
            StockValue = Math.Round((input.Last().StockValue + input[^2].StockValue)/2,2) });
        input.Add(input.Last() with { TimeStamp = input.Last().TimeStamp.AddDays(1), 
            StockValue = Math.Round(Math.Min(input.Last().StockValue, input[^2].StockValue) 
                         + Math.Abs((input.Last().StockValue - input[^2].StockValue)/4),2) });
        return Result.Success<List<StockFileLineModel>,Error>(input);
    }
    
    //Documentation found at: https://numerics.mathdotnet.com/Regression
    private static Result<List<StockFileLineModel>, Error> PredictStocksLinearRegression(List<StockFileLineModel> input, int predictionsCount)
    {
        var inputCount = input.Count;
        var (intercept,slope) = Fit.Line(Enumerable.Range(0, inputCount).Select(i => (double)i).ToArray(), 
            input.Select(i => i.StockValue).ToArray());
        for (var i = inputCount; i < inputCount + predictionsCount; i++)
        {
            var previousDay = input[i - 1];
            input.Add(previousDay with {TimeStamp = previousDay.TimeStamp.AddDays(1), StockValue = Math.Round(slope * i + intercept,2) });
        }

        return Result.Success<List<StockFileLineModel>,Error>(input);
    }

}