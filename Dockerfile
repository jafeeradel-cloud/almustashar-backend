
# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# copy everything else
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

COPY --from=build /app/publish .

CMD ["dotnet", "RealEstateApi.dll"]
