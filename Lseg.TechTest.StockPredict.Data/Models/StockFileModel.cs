namespace Lseg.TechTest.StockPredict.Data.Models;

public record StockFileModel(string ExchangeName, string StockTicker, List<StockFileLineModel> Contents);