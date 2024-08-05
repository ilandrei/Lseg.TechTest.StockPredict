using CSharpFunctionalExtensions;
using Lseg.TechTest.StockPredict.Data.Models;
using Lseg.TechTest.StockPredict.Shared;

namespace Lseg.TechTest.StockPredict.Data.Interfaces;

public interface IStockFileRepository
{
    Result<List<DirectoryFilesModel>, Error> GetAllCsvFromBaseDirectory();
    Task<Result<List<StockFileLineModel>, Error>> GetStockFileRandomLines(string path);
    UnitResult<Error> CreateOutputFolders(string baseFolderName, List<string> previousPaths);
    Task<UnitResult<Error>> SaveStockFiles(string baseFolderName, List<CsvFileModel> fileModels);
}