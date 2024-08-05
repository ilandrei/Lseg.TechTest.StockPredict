using System.Net;
using CSharpFunctionalExtensions;
using Lseg.TechTest.StockPredict.Data.Interfaces;
using Lseg.TechTest.StockPredict.Data.Models;
using Lseg.TechTest.StockPredict.Service.Interfaces;
using Lseg.TechTest.StockPredict.Shared;
using Lseg.TechTest.StockPredict.Shared.Enums;

namespace Lseg.TechTest.StockPredict.Service;

public class StockFileService :IStockFileService
{
    private readonly IStockFileRepository _stockFileRepository;
    private readonly IPredictionService _predictionService;

    public StockFileService(IStockFileRepository stockFileRepository,IPredictionService predictionService)
    {
        _stockFileRepository = stockFileRepository;
        _predictionService = predictionService;
    }

    public async Task<Result<List<CsvFileModel>,Error>> GenerateSampleDataFromStocks(int maxStockFilesPerExchange, 
        int? predictionCount = null, PredictionAlgorithm predictionAlgorithm = PredictionAlgorithm.Primitive)
    {
        if (maxStockFilesPerExchange <= 0)
            return new Error(HttpStatusCode.BadRequest, "maxStockFilesPerExchange must be 1 or more");
        
        var baseOutputFolderName = DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
        
        return await _stockFileRepository.GetAllCsvFromBaseDirectory()
            .Map(subFolders => 
                    //remove subfolders that don't contain at least 1 file
                    subFolders.Where(sf => sf.DirectoryFileNames.Count != 0).ToList())
            .Map(subFolders => 
                //process only maxStockFilesPerExchange files per folder
                subFolders.Select(exchangeFolder => exchangeFolder 
                    with {DirectoryFileNames = exchangeFolder.DirectoryFileNames.OrderBy(e => e).Take(maxStockFilesPerExchange).ToList()}))
            .Bind(async subFolders =>
            {
                //for each exchange, get the file contents, failing if any of them are not in the right format
                var fileContents = new List<CsvFileModel>();

                foreach (var subFolder in subFolders)
                {
                    foreach (var filePath in subFolder.DirectoryFileNames.Select(file => Path.Combine(subFolder.DirectoryPath, file)))
                    {
                        var contents = await GetFileContent(filePath,predictionCount,predictionAlgorithm);
                        if (contents.IsFailure) return Result.Failure<List<CsvFileModel>,Error>(contents.Error);
                        var resultingFileContent = new CsvFileModel(FilePath:filePath, Contents: contents.Value);
                        
                        fileContents.Add(resultingFileContent);
                    }
                }
                return Result.Success<List<CsvFileModel>,Error>(fileContents);
            })
            .Bind(fileModels =>
            {
                //create the folder structure and save the files
                var createFoldersResult =
                    _stockFileRepository.CreateOutputFolders(baseOutputFolderName,
                        fileModels.Select(fm => Directory.GetParent(fm.FilePath)!.FullName).Distinct().ToList());
                return createFoldersResult.IsFailure ? Result.Failure<List<CsvFileModel>,Error>(createFoldersResult.Error) : fileModels;
            })
            .Bind(async fileModels =>
            {
                var saveFilesResult = await _stockFileRepository.SaveStockFiles(baseOutputFolderName,fileModels);
                
                return saveFilesResult.IsFailure ? Result.Failure<List<CsvFileModel>,Error>(saveFilesResult.Error) : 
                    Result.Success<List<CsvFileModel>,Error>(fileModels);
            })
            ;
    }

    private async Task<Result<List<StockFileLineModel>,Error>> GetFileContent(string path, int? predictionCount = null, 
        PredictionAlgorithm predictionAlgorithm = PredictionAlgorithm.Primitive)
        => await _stockFileRepository.GetStockFileRandomLines(path)
            .Bind(stocks => predictionCount == null? Result.Success<List<StockFileLineModel>,Error>(stocks) 
                : _predictionService.PredictStocks(stocks,predictionAlgorithm,predictionCount.Value));
}