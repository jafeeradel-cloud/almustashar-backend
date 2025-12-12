# ====== Build stage ======
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore
COPY RealEstateApi.csproj ./
RUN dotnet restore ./RealEstateApi.csproj

# copy all source files
COPY . ./
RUN dotnet publish ./RealEstateApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# ====== Runtime stage ======
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Render يعطي PORT ديناميكي
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

COPY --from=build /app/publish .

CMD ["dotnet", "RealEstateApi.dll"]
