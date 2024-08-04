using Lseg.TechTest.StockPredict.Service.Interfaces;
using Lseg.TechTest.StockPredict.Shared;
using Lseg.TechTest.StockPredict.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Lseg.TechTest.StockPredict.Controllers;


[ApiController]
[Route("api/v1/[controller]")]
public class StockController(IStockFileService stockFileService):ControllerBase
{
    [HttpGet("GetSampleStockData")]
    public async Task<IActionResult> Get([FromQuery] int? maxStockFilesPerExchange)
    {
        var result = await stockFileService.GenerateSampleDataFromStocks(maxStockFilesPerExchange ?? 2);
        return result.IsFailure
            ? result.Error.ToHttpResponse()
            : Ok(result.Value);
    }
    [HttpGet("PredictStockDataLinear")]
    public async Task<IActionResult> PredictStockDataLinear([FromQuery] int? predictionCount,[FromQuery] int? maxStockFilesPerExchange)
    {
        var result = await stockFileService.GenerateSampleDataFromStocks(maxStockFilesPerExchange ?? 2,
            predictionCount,PredictionAlgorithm.LinearRegression);
        return result.IsFailure
            ? result.Error.ToHttpResponse()
            : Ok(result.Value);
    }
    [HttpGet("PredictStockDataPrimitive")]
    public async Task<IActionResult> PredictStockDataPrimitive([FromQuery] int? maxStockFilesPerExchange)
    {
        var result = await stockFileService.GenerateSampleDataFromStocks(maxStockFilesPerExchange ?? 2,3);
        return result.IsFailure
            ? result.Error.ToHttpResponse()
            : Ok(result.Value);
    }
}