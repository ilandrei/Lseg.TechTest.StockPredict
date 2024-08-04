using System.Net;
using CSharpFunctionalExtensions;
using Lseg.TechTest.StockPredict.Data.Interfaces;
using Lseg.TechTest.StockPredict.Data.Models;
using Lseg.TechTest.StockPredict.Service.Interfaces;
using Lseg.TechTest.StockPredict.Shared;
using Lseg.TechTest.StockPredict.Shared.Enums;

namespace Lseg.TechTest.StockPredict.Service;

public class StockFileService(IStockFileRepository stockFileRepository,IPredictionService predictionService):IStockFileService
{
    public async Task<Result<List<StockFileModel>,Error>> GenerateSampleDataFromStocks(int maxStockFilesPerExchange, 
        int? predictionCount = null, PredictionAlgorithm predictionAlgorithm = PredictionAlgorithm.Primitive)
    {
        if (maxStockFilesPerExchange <= 0)
            return new Error(HttpStatusCode.BadRequest, "maxStockFilesPerExchange must be 1 or more");
        return await stockFileRepository.ParseStockDirectory()
            .Ensure(exchangeFolders => 
                    //make sure all exchange stocks contain at least 1 file
                    exchangeFolders.Select(ef => ef.DirectoryFilePaths)
                        .All(folderContents => folderContents.Count != 0),
                new Error(HttpStatusCode.BadRequest,"Exchange directories must not be empty"))
            .Map(exchangeFolders => 
                //process only maxStockFilesPerExchange files per folder
                exchangeFolders.Select(exchangeFolder => exchangeFolder 
                    with {DirectoryFilePaths = exchangeFolder.DirectoryFilePaths.OrderBy(e => e).Take(maxStockFilesPerExchange).ToList()}))
            .Bind(async exchangeFolders =>
            {
                //for each exchange, get the file contents, failing if any of them are not in the right format
                var fileContents = new List<StockFileModel>();

                foreach (var exchange in exchangeFolders)
                {
                    foreach (var stockFilePath in exchange.DirectoryFilePaths)
                    {
                        var contents = await GetFileContent(stockFilePath,predictionCount,predictionAlgorithm);
                        if (contents.IsFailure) return Result.Failure<List<StockFileModel>,Error>(contents.Error);
                        var resultingFileContent = new StockFileModel(ExchangeName: exchange.DirectoryName,
                            StockTicker: Path.GetFileName(stockFilePath), Contents: contents.Value);
                        
                        fileContents.Add(resultingFileContent);
                    }
                }
                return Result.Success<List<StockFileModel>,Error>(fileContents);
            })
            .Bind(async fileModels =>
            {
                //create the folder structure and save the files
                var baseFolderName = DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
                var createFoldersResult =
                    stockFileRepository.CreateOutputFolders(baseFolderName,
                        fileModels.Select(fm => fm.ExchangeName).ToList());
                if(createFoldersResult.IsFailure) return Result.Failure<List<StockFileModel>,Error>(createFoldersResult.Error);

                var saveFilesResult = await stockFileRepository.SaveStockFiles(fileModels.Select(fm =>
                    new Tuple<string, List<StockFileLineModel>>(
                        Path.Combine(baseFolderName, fm.ExchangeName, fm.StockTicker), fm.Contents)));
                
                return saveFilesResult.IsFailure ? Result.Failure<List<StockFileModel>,Error>(saveFilesResult.Error) : 
                    Result.Success<List<StockFileModel>,Error>(fileModels);
            })
            ;
    }

    private async Task<Result<List<StockFileLineModel>,Error>> GetFileContent(string path, int? predictionCount = null, 
        PredictionAlgorithm predictionAlgorithm = PredictionAlgorithm.Primitive)
        => await stockFileRepository.GetStockFileRandomLines(path)
            .Bind(stocks => predictionCount == null? Result.Success<List<StockFileLineModel>,Error>(stocks) 
                : predictionService.PredictStocks(stocks,predictionAlgorithm,predictionCount.Value));
}