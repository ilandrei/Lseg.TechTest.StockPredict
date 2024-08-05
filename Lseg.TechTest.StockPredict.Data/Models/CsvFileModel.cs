namespace Lseg.TechTest.StockPredict.Data.Models;

public record CsvFileModel(string FilePath, List<StockFileLineModel> Contents);