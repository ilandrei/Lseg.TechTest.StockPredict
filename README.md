
# StockPredict




## Installation

**Using Docker**

How to run the project locally:
Docker required

```bash
    code docker-compose.yml
```
Edit volumes to be valid paths on your local machine instead of '/Users/andreiilie/Desktop/Repos/stocktestdata', then save.
The first folder will need to be populated with the test data provided with the technical requirements (folders LSE,NASDAQ,NYSE, each of them containing the CSV files)
```shell
    docker-compose up
```

**Without Docker (.NET 7 Required)**

```shell
    cd Lseg.TechTest.StockPredict
    code appsettings.json
```
Edit lines 9,10 of appsettings.json, changing the current values with the desired input/output folder paths on your local system.
```json lines
    "RootPath": "/app/data",
    "OutputPath": "/app/output"
```
Then start the server by running
```shell
    dotnet run --launchSettings="http"
```

In both cases, the link used to access the server functionalities is [Swagger](http://localhost:8080/swagger/index.html)


## Documentation

In order to test the application functionalities, one can either use a third party tool for calling REST APIs (e.g. Postman), use the pre-built Swagger UI to visually send
requests to the server or simply paste the server URL in the address bar in any browser (e.g. http://localhost:8080/api/v1/Stock/GetSampleStockData?maxStockFilesPerExchange=3)


Server exposes 3 endpoints
- GET /api/v1/Stock/GetSampleStockData?maxStockFilesPerExchange=[number]
- GET /api/v1/Stock/PredictStockDataPrimitive?maxStockFilesPerExchange=[number]
- GET /api/v1/Stock/PredictStockDataLinear?maxStockFilesPerExchange=[number]&predictionCount=[number]

Where 'maxStockFilesPerExchange' is the number of stock files to be parsed for each exchange according to the challenge objectives.
'predictionCount' is the expected number of predictions to be generated on top of the 10 lines of sampled data

All 3 endpoints return a json of type:
```json
[
    {
        "filePath": "string",
        "contents": [
            {
                "companyTicker": "string",
                "timeStamp": "DateTime",
                "stockValue": "double"
            }
        ]
    }
]
```

In addition to the REST response, the result files will also be saved in the output folder set in the installation step, having the following structure:
- Base Folder - Current UTC timestamp in format 'yyyyMMddTHHmmss'
    - Inside this folder the same file/folder structure as in the input folder, excluding directories not containing any CSV files

Resulting files have the same structure as the input.

If an error occurs, the response will instead be a plain string with a human readable message and the corresponding status code

## Assumptions
- The program needs to be portable to any system, that's why Docker was used
- The program would need to open both small and large CSVs, that's why 2 file opening approaches was used, in order to not run into OutOfMemoryExceptions while opening large files
- The program would run on HDD instead of SSD, typical of a server that stores a lot of data, meaning parallelizing the IO would not lead to any performance gains
- The user would also want the sampled data as a response to the API call instead of only having it serialized in CSVs
- All CSV files would be in the directory designated as RoothPath in the configuration
- Any malformed CSV in the selected directory would lead to a fail fast error, not letting generating any unexpected CSV
- The user would be able to have CSV files in any folder structure inside the input folder, this structure being the same in the output
- The server would have the user permissions needed to read files in the input directory and write files in the output directory