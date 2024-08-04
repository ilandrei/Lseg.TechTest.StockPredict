FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lseg.TechTest.StockPredict/Lseg.TechTest.StockPredict.csproj", "Lseg.TechTest.StockPredict/"]
COPY ["Lseg.TechTest.StockPredict.Data/Lseg.TechTest.StockPredict.Data.csproj", "Lseg.TechTest.StockPredict.Data/"]
COPY ["Lseg.TechTest.StockPredict.Shared/Lseg.TechTest.StockPredict.Shared.csproj", "Lseg.TechTest.StockPredict.Shared/"]
COPY ["Lseg.TechTest.StockPredict.Service/Lseg.TechTest.StockPredict.Service.csproj", "Lseg.TechTest.StockPredict.Service/"]
RUN dotnet restore "Lseg.TechTest.StockPredict/Lseg.TechTest.StockPredict.csproj"
COPY . .
WORKDIR "/src/Lseg.TechTest.StockPredict"
RUN dotnet build "Lseg.TechTest.StockPredict.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Lseg.TechTest.StockPredict.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lseg.TechTest.StockPredict.dll"]
