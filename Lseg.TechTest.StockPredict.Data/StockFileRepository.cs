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

public class StockFileRepository :IStockFileRepository
{
    private readonly string _basePath;
    private readonly string _outputPath;
    private readonly int _smallFileSizeThreshold;
    private readonly int _previousLineCountUsedForPrediction;
    
    private readonly Random _random = new();
    private readonly ILogger<StockFileRepository> _logger;

    public StockFileRepository(IOptions<StockFileConfigSection> options, ILogger<StockFileRepository> logger)
    {
        _logger = logger;
        _basePath = options.Value.RootPath;
        _outputPath = options.Value.OutputPath;
        _smallFileSizeThreshold = options.Value.SmallFileSizeThreshold;
        _previousLineCountUsedForPrediction = options.Value.PreviousLineCountUsedForPrediction;
    }

    public Result<List<DirectoryFilesModel>, Error> GetAllCsvFromBaseDirectory()
    {
        List<string> subDirectories;
        try
        {
            if (!Directory.Exists(_basePath))
                return new Error(HttpStatusCode.InternalServerError, "Base directory does not exist");
            subDirectories = Directory.EnumerateDirectories(_basePath,"*",SearchOption.AllDirectories).ToList();
            subDirectories.Add(_basePath);
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
        {
            _logger.LogError(e,"Error loading base directory {Message}",e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error opening the base directory. Server doesn't have required permissions.");
        }
        
        try
        {
            return subDirectories.Select(sd =>
            {
                var files = Directory.EnumerateFiles(sd, "*.csv", SearchOption.TopDirectoryOnly);
                return new DirectoryFilesModel(sd, files.Select(Path.GetFileName).ToList()!);
            }).ToList();
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
        {
            _logger.LogError(e,"Error loading sub directory {Message}",e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error opening a sub directory. Server doesn't have required permissions.");
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

    public UnitResult<Error> CreateOutputFolders(string baseFolderName, List<string> previousPaths)
    {
        if (!Directory.Exists(_outputPath)) return new Error(HttpStatusCode.NotFound, "Output folder doesn't exist");
        if (Directory.Exists(Path.Combine(_outputPath, baseFolderName)))
            return new Error(HttpStatusCode.Conflict, $"Folder {baseFolderName} already exists in output folder");

        try
        {
            foreach (var relativePreviousPath in previousPaths
                         .Select(previousPath => previousPath.Replace(_basePath, "")
                             .TrimStart(Path.DirectorySeparatorChar)
                             .TrimStart(Path.AltDirectorySeparatorChar)
                         ))
            {
                var newPath = Path.Combine(_outputPath, baseFolderName, relativePreviousPath);
                Directory.CreateDirectory(newPath);
            }
        }
        catch (Exception e) when (e is UnauthorizedAccessException or SecurityException)
        {
            _logger.LogError(e, "Error creating exchange directory {Message}", e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error creating an exchange directory. Server doesn't have required permissions.");
        }
        catch (Exception e) when (e is NotSupportedException or IOException)
        {
            _logger.LogError(e, "Error creating exchange directory {Message}", e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error creating an exchange directory. Directory path is invalid.");
        }
        
        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> SaveStockFiles(string baseFolderName, List<CsvFileModel> fileModels)
    {
        try
        {
            foreach (var (previousPath,lines) in fileModels)
            {
                var previousRelativePath = previousPath.Replace(_basePath, "")
                    .TrimStart(Path.DirectorySeparatorChar)
                    .TrimStart(Path.AltDirectorySeparatorChar);
                var finalPath = Path.Combine(_outputPath, baseFolderName, previousRelativePath);
                
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
            _logger.LogError(e, "Error creating stock file {Message}", e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error creating a stock file. Server doesn't have required permissions.");
        }
        catch (Exception e) when (e is NotSupportedException or IOException)
        {
            _logger.LogError(e, "Error creating stock file {Message}", e.Message);
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
                return fileLines.Skip(startIndex).Take(_previousLineCountUsedForPrediction).ToList();
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
            _logger.LogError(e,"Error loading stock file {Message}",e.Message);
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
            _logger.LogError(e,"Error loading stock file {Message}",e.Message);
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
            _logger.LogError(e,"Error loading stock file {Message}",e.Message);
            return new Error(HttpStatusCode.InternalServerError,
                "There was an error opening the file. Try changing the file permissions.");
            
        }

        return fileContentList;
    }
}