namespace Lseg.TechTest.StockPredict.Data.Models;

public record StockFileLineModel(string CompanyTicker, DateTime TimeStamp, double StockValue);