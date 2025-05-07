FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SmartAutoTrader.API.csproj .
RUN dotnet restore "SmartAutoTrader.API.csproj"
COPY . .
RUN dotnet publish "SmartAutoTrader.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartAutoTrader.API.dll"]