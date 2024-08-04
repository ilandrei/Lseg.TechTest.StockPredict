namespace Lseg.TechTest.StockPredict.Shared;

public class StockFileConfigSection
{
    public string RootPath { get; set; }
    public string OutputPath { get; set; }
    public int SmallFileSizeThreshold { get; set; }
    public int PreviousLineCountUsedForPrediction { get; set; }
}