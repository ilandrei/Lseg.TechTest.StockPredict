using CSharpFunctionalExtensions;
using Lseg.TechTest.StockPredict.Data.Models;
using Lseg.TechTest.StockPredict.Shared;

namespace Lseg.TechTest.StockPredict.Data.Interfaces;

public interface IStockFileRepository
{
    Result<List<StockDirectoryFilesModel>, Error> ParseStockDirectory();
    Task<Result<List<StockFileLineModel>, Error>> GetStockFileRandomLines(string path);
    UnitResult<Error> CreateOutputFolders(string baseFolderName, List<string> toList);
    Task<UnitResult<Error>> SaveStockFiles(IEnumerable<Tuple<string, List<StockFileLineModel>>> select);
}