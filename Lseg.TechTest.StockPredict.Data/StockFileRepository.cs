using System.Net;
using System.Security;
using CSharpFunctionalExtensions;
using Lseg.TechTest.StockPredict.Data.Extensions;
using Lseg.TechTest.StockPredict.Data.Interfaces;
using Lseg.TechTest.StockPredict.Data.Models;
using Lseg.TechTest.StockPredict.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lseg.TechTest.StockPredict.Data;

public class StockFileRepository(IOptions<StockFileConfigSection> options, ILogger<StockFileRepository> logger):IStockFileRepository
{
    private readonly string _basePath = options.Value.RootPath;
    private readonly string _outputPath = options.Value.OutputPath;
    private readonly int _smallFileSizeThreshold = 0;//options.Value.SmallFileSizeThreshold;
    private readonly int _previousLineCountUsedForPrediction = options.Value.PreviousLineCountUsedForPrediction;
    private readonly Random _random = new();

    public Result<List<StockDirectoryFilesModel>, Error> ParseStockDirectory()
    {
        List<string> exchangeDirectories;
        try
        {
            if (!Directory.Exists(_basePath))
                return new Error(HttpStatusCode.InternalServerError, "Stocks input directory does not exist");
            exchangeDirectories = Directory.EnumerateDirectories(_basePath).ToList();
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
        {
            logger.LogError(e,"Error loading stock directory {Message}",e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error opening the base stock directory. Server doesn't have required permissions.");
        }

        if (exchangeDirectories.Count == 0)
            return new Error(HttpStatusCode.BadRequest, "Input has no exchange directories.");

        try
        {
            return exchangeDirectories.Select(exchange =>
            {
                var directory = new DirectoryInfo(exchange);
                return new StockDirectoryFilesModel(DirectoryName: directory.Name,
                    DirectoryFilePaths: directory.GetFiles("*.csv").Select(file => file.FullName).ToList());
            }).ToList();
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
        {
            logger.LogError(e, "Error loading exchange directory {Message}", e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error opening an exchange directory. Server doesn't have required permissions.");
        }
    }
    
    public async Task<Result<List<StockFileLineModel>, Error>> GetStockFileRandomLines(string path)
    {
        if (!File.Exists(path))
            return new Error(HttpStatusCode.InternalServerError,
                "File was moved during processing");

        return new FileInfo(path).Length > _smallFileSizeThreshold
            ? await GetBigStockFileRandomLines(path)
            : await GetSmallStockFileRandomLines(path);

    }

    public UnitResult<Error> CreateOutputFolders(string baseFolderName, List<string> exchangeList)
    {
        if (!Directory.Exists(_outputPath)) return new Error(HttpStatusCode.NotFound, "Output folder doesn't exist");
        if (Directory.Exists(Path.Combine(_outputPath, baseFolderName)))
            return new Error(HttpStatusCode.Conflict, $"Folder {baseFolderName} already exists in output folder");

        try
        {
            foreach (var exchange in exchangeList)
            {
                Directory.CreateDirectory(Path.Combine(_outputPath, baseFolderName, exchange));
            }
        }
        catch (Exception e) when (e is UnauthorizedAccessException or SecurityException)
        {
            logger.LogError(e, "Error creating exchange directory {Message}", e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error creating an exchange directory. Server doesn't have required permissions.");
        }
        catch (Exception e) when (e is NotSupportedException or IOException)
        {
            logger.LogError(e, "Error creating exchange directory {Message}", e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error creating an exchange directory. Directory path is invalid.");
        }
        
        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> SaveStockFiles(IEnumerable<Tuple<string, List<StockFileLineModel>>> stockFiles)
    {
        try
        {
            foreach (var (path,lines) in stockFiles)
            {
                var finalPath = Path.Combine(_outputPath, path);

                await using var resultFile = File.CreateText(finalPath);
                foreach (var content in lines.Select(line =>
                             $"{line.CompanyTicker},{line.TimeStamp:dd-MM-yyyy},{Math.Round(line.StockValue, 2)}"))
                {
                    await resultFile.WriteLineAsync(content);
                }
            }
        }
        catch (Exception e) when (e is UnauthorizedAccessException or SecurityException)
        {
            logger.LogError(e, "Error creating stock file {Message}", e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error creating a stock file. Server doesn't have required permissions.");
        }
        catch (Exception e) when (e is NotSupportedException or IOException)
        {
            logger.LogError(e, "Error creating stock file {Message}", e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error creating a stock file. Directory path is invalid.");
        }
        return UnitResult.Success<Error>();
    }

    //load all file contents in memory - faster
    private async Task<Result<List<StockFileLineModel>, Error>> GetSmallStockFileRandomLines(string path)
    {
        return await GetAllLinesFromFile(path)
            .Ensure(fileLines => fileLines.Count >= _previousLineCountUsedForPrediction,
                new Error(HttpStatusCode.BadRequest,
                    $"File {path} has less than {_previousLineCountUsedForPrediction} required for prediction"))
            .Map(fileLines =>
            {
                var startIndex = _random.Next(0, fileLines.Count - _previousLineCountUsedForPrediction);
                return fileLines[startIndex..(startIndex+_previousLineCountUsedForPrediction)];
            });
    }

    //parse file twice to avoid OutOfMemoryException  - slower
    private async Task<Result<List<StockFileLineModel>, Error>> GetBigStockFileRandomLines(string path)
    {
        return await GetFileLineCount(path)
            .Ensure(lineCount => lineCount >= _previousLineCountUsedForPrediction, new Error(HttpStatusCode.BadRequest,
                $"File {path} has less than {_previousLineCountUsedForPrediction} required for prediction"))
            .Map(lineCount => _random.Next(0, lineCount - _previousLineCountUsedForPrediction))
            .Bind(startIndex => GetSliceOfFileLines(path,startIndex,startIndex + _previousLineCountUsedForPrediction));
    }

    private async Task<Result<List<StockFileLineModel>,Error>> GetSliceOfFileLines(string path, int startIndex, int endIndex)
    {
        var currentIndex = 0;
        var fileContentList = new List<StockFileLineModel>();
        try
        {
            using var reader = new StreamReader(path);

            while (!reader.EndOfStream)
            {
                var csvLine = await reader.ReadLineAsync();
                if (csvLine == null) break;
                currentIndex++;
                if (currentIndex > endIndex) break;
                if (currentIndex < startIndex) continue;

                var parsedFileLine = csvLine.ToStockFileLineModel(path);
                if (parsedFileLine.IsFailure)
                    return parsedFileLine.Error;
                fileContentList.Add(parsedFileLine.Value);
            }
        }
        catch (IOException e)
        {
            logger.LogError(e,"Error loading stock file {Message}",e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error opening the file. Try changing the file permissions.");
        }

        return fileContentList;
    }

    private async Task<Result<int,Error>> GetFileLineCount(string path)
    {
        var lineCount = 0;
        try
        {
            using var reader = new StreamReader(path);

            while (!reader.EndOfStream)
            {
                var csvLine = await reader.ReadLineAsync();
                if (csvLine == null) break;
                lineCount++;
            }
        } //at this point we're sure the file exists
        catch (IOException e)
        {
            logger.LogError(e,"Error loading stock file {Message}",e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error opening the file. Try changing the file permissions.");
        }
        return lineCount;
    }

    private async Task<Result<List<StockFileLineModel>, Error>> GetAllLinesFromFile(string path)
    {
        var fileContentList = new List<StockFileLineModel>();
        
        using var reader = new StreamReader(path);

        try
        {
            while (!reader.EndOfStream)
            {
                var csvLine = await reader.ReadLineAsync();
                if (csvLine == null) break;
                var parsedFileLine = csvLine.ToStockFileLineModel(path);
                if (parsedFileLine.IsFailure)
                    return parsedFileLine.Error;
                fileContentList.Add(parsedFileLine.Value);
            }
        }
        catch (IOException e)
        {
            logger.LogError(e,"Error loading stock file {Message}",e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error opening the file. Try changing the file permissions.");
            
        }

        return fileContentList;
    }
}