# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/SmartAutoTrader.API.csproj", "backend/"]

# Restore dependencies for the main project
RUN dotnet restore "backend/SmartAutoTrader.API.csproj"

# Copy the rest of the backend source code
COPY backend/. ./backend/

# Set the working directory for the publish command
WORKDIR "/src/backend"
# Publish the application
RUN dotnet publish "SmartAutoTrader.API.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Default ASP.NET Core port in containers (matches your API Container App ingress target port)
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartAutoTrader.API.dll"]