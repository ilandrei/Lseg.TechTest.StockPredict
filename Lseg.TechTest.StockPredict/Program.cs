using Lseg.TechTest.StockPredict.Data;
using Lseg.TechTest.StockPredict.Data.Interfaces;
using Lseg.TechTest.StockPredict.Service;
using Lseg.TechTest.StockPredict.Service.Interfaces;
using Lseg.TechTest.StockPredict.Shared;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IStockFileService, StockFileService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();

builder.Services.AddScoped<IStockFileRepository, StockFileRepository>();

builder.Services.Configure<StockFileConfigSection>(builder.Configuration.GetSection("StockFileConfig"));
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();