# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["backend/StockViewer.sln", "backend/"]
COPY ["backend/StockViewer.Api/StockViewer.Api.csproj", "backend/StockViewer.Api/"]
COPY ["backend/StockViewer.Core/StockViewer.Core.csproj", "backend/StockViewer.Core/"]
COPY ["backend/StockViewer.Worker/StockViewer.Worker.csproj", "backend/StockViewer.Worker/"]
COPY ["backend/StockViewer.Tests/StockViewer.Tests.csproj", "backend/StockViewer.Tests/"]

# Restore packages
RUN cd backend && dotnet restore

# Copy source code
COPY ["backend/", "backend/"]

# Build
RUN cd backend && dotnet build -c Release -o /app/build

# Publish API
FROM build AS publish-api
RUN cd backend && dotnet publish StockViewer.Api/StockViewer.Api.csproj -c Release -o /app/publish-api

# Runtime image for API
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS api-runtime
WORKDIR /app
COPY --from=publish-api /app/publish-api .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "StockViewer.Api.dll"]

# Publish Worker
FROM build AS publish-worker
RUN cd backend && dotnet publish StockViewer.Worker/StockViewer.Worker.csproj -c Release -o /app/publish-worker

# Runtime image for Worker
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS worker-runtime
WORKDIR /app
COPY --from=publish-worker /app/publish-worker .
ENTRYPOINT ["dotnet", "StockViewer.Worker.dll"]
