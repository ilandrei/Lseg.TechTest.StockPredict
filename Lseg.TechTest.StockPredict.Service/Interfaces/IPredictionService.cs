using CSharpFunctionalExtensions;
using Lseg.TechTest.StockPredict.Data.Models;
using Lseg.TechTest.StockPredict.Shared;
using Lseg.TechTest.StockPredict.Shared.Enums;

namespace Lseg.TechTest.StockPredict.Service.Interfaces;

public interface IPredictionService
{
    Result<List<StockFileLineModel>, Error> PredictStocks(List<StockFileLineModel> input,
        PredictionAlgorithm algorithm, int predictionsCount);
}