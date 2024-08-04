using CSharpFunctionalExtensions;
using Lseg.TechTest.StockPredict.Data.Models;
using Lseg.TechTest.StockPredict.Shared;
using Lseg.TechTest.StockPredict.Shared.Enums;

namespace Lseg.TechTest.StockPredict.Service.Interfaces;

public interface IStockFileService
{
    Task<Result<List<StockFileModel>, Error>> GenerateSampleDataFromStocks(int maxStockFilesPerExchange,int? predictionCount = null,PredictionAlgorithm predictionAlgorithm = PredictionAlgorithm.Primitive);
}