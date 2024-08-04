
# StockPredict




## Installation

How to run the project locally:
Docker required

```bash
  code docker-compose.yml
```
Edit volumes to be valid paths on your local machine, then save.

```bash
docker-compose up
```
Then access [Swagger](http://localhost:8080/swagger/index.html)



## Documentation

App contains 3 endpoints
- /api/v1/Stock/GetSampleStockData?maxStockFilesPerExchange=[number]
- /api/v1/Stock/PredictStockDataPrimitive?maxStockFilesPerExchange=[number]
- /api/v1/Stock/PredictStockDataLinear?maxStockFilesPerExchange=[number]&predictionCount=[number]

Where 'maxStockFilesPerExchange' is the number of stock files to be parsed for each exchange according to the challenge objectives.
'predictionCount' is the expected number of predictions to be generated on top of the 10 lines of sampled data

All 3 endpoints return a json of type:
```json
[
    {
        "exchangeName": string,
        "stockTicker": string,
        "contents": [
            {
                "companyTicker": string,
                "timeStamp": DateTime,
                "stockValue": double
            }
        ]
    }
]
```
But also save the data as CSVs in the output folder set in the docker-compose file :
- Base Folder - Current timestamp in format 'yyyyMMddTHHmmss'
    - exchangeName1
        - ticker1.csv
        - ticker2.csv
    - exchangeName2
        - ticker3.csv
        - ticker4.csv

Resulting files have the same structure as the input.

If an error occurs, the response will instead be a plain string with a human readable message and the corresponding status code
## Assumptions
- The program needs to be portable to any system, that's why Docker was used
- The program would need to open both small and large CSVs, that's why 2 file opening approaches was used, in order to not run into OutOfMemoryExceptions while opening large files
- The program would run on HDD instead of SSD, typical of a server that stores a lot of data, meaning parallelizing the IO would not lead to any performance gains
- The user would also want the sampled data as a response to the API call instead of only having it serialized in CSVs