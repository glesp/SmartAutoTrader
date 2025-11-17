@echo off
REM SmartAutoTrader Setup Script for Windows
REM This script helps set up the development environment

setlocal enabledelayedexpansion

echo ===================================================================
echo   SmartAutoTrader - Development Environment Setup (Windows)
echo ===================================================================
echo.

REM Check prerequisites
echo Checking prerequisites...
echo.

REM Check Node.js
where node >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    for /f "tokens=*" %%i in ('node --version') do set NODE_VERSION=%%i
    echo [OK] Node.js installed: !NODE_VERSION!
) else (
    echo [ERROR] Node.js is not installed. Please install Node.js 18 or higher.
    exit /b 1
)

REM Check Python
where python >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    for /f "tokens=*" %%i in ('python --version') do set PYTHON_VERSION=%%i
    echo [OK] Python installed: !PYTHON_VERSION!
) else (
    echo [ERROR] Python is not installed. Please install Python 3.10 or higher.
    exit /b 1
)

REM Check .NET
where dotnet >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
    echo [OK] .NET installed: !DOTNET_VERSION!
) else (
    echo [ERROR] .NET SDK is not installed. Please install .NET 8 SDK.
    exit /b 1
)

echo.
echo All prerequisites are installed!
echo.

REM Setup Backend
echo ===================================================================
echo   Setting up Backend (.NET)
echo ===================================================================
echo.

cd backend

if not exist "appsettings.Development.json" (
    echo [WARNING] appsettings.Development.json not found. Creating template...
    (
        echo {
        echo   "Logging": {
        echo     "LogLevel": {
        echo       "Default": "Information",
        echo       "Microsoft.AspNetCore": "Warning"
        echo     }
        echo   },
        echo   "Jwt": {
        echo     "Key": "YourSecretKeyHere_AtLeast32Characters_ForJWT_ChangeThis",
        echo     "Issuer": "SmartAutoTrader",
        echo     "Audience": "SmartAutoTraderUsers"
        echo   },
        echo   "Services": {
        echo     "ParameterExtraction": {
        echo       "Endpoint": "http://localhost:5006/extract_parameters",
        echo       "Timeout": 30
        echo     }
        echo   }
        echo }
    ) > appsettings.Development.json
    echo [OK] Created appsettings.Development.json (remember to update JWT Key!)
) else (
    echo [OK] appsettings.Development.json already exists
)

echo [OK] Restoring .NET dependencies...
dotnet restore SmartAutoTrader.API.csproj

REM Check if EF tools are installed
where dotnet-ef >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [WARNING] Entity Framework tools not found. Installing...
    dotnet tool install --global dotnet-ef
)

echo [OK] Running database migrations...
dotnet ef database update

cd ..

echo [OK] Backend setup complete!
echo.

REM Setup Frontend
echo ===================================================================
echo   Setting up Frontend (React)
echo ===================================================================
echo.

cd frontend

echo [OK] Installing frontend dependencies...
call npm ci

cd ..

echo [OK] Frontend setup complete!
echo.

REM Setup Python Service
echo ===================================================================
echo   Setting up Python Service
echo ===================================================================
echo.

cd PythonServices\parameter_extraction_service

if not exist ".env" (
    echo [WARNING] .env file not found. Creating template...
    (
        echo OPENROUTER_API_KEY=your_openrouter_api_key_here
        echo PORT=5006
    ) > .env
    echo [OK] Created .env file (remember to add your OpenRouter API key!)
) else (
    echo [OK] .env file already exists
)

if not exist "venv" (
    echo [OK] Creating Python virtual environment...
    python -m venv venv
)

echo [OK] Activating virtual environment and installing dependencies...
echo [WARNING] This may take several minutes (downloading PyTorch and transformers)...

call venv\Scripts\activate.bat
python -m pip install --upgrade pip
pip install -r requirements.txt

cd ..\..

echo [OK] Python service setup complete!
echo.

REM Final instructions
echo ===================================================================
echo   Setup Complete! 🎉
echo ===================================================================
echo.
echo Next steps:
echo.
echo 1. Update your environment variables:
echo    - backend\appsettings.Development.json (JWT Key)
echo    - PythonServices\parameter_extraction_service\.env (OpenRouter API Key)
echo.
echo 2. Run the services in separate command prompts:
echo.
echo    Terminal 1 - Backend:
echo    ^> cd backend ^&^& dotnet run --launch-profile https
echo.
echo    Terminal 2 - Frontend:
echo    ^> cd frontend ^&^& npm run dev
echo.
echo    Terminal 3 - Python Service:
echo    ^> cd PythonServices\parameter_extraction_service
echo    ^> venv\Scripts\activate.bat
echo    ^> python parameter_extraction_service.py
echo.
echo 3. Open http://localhost:5173 in your browser
echo.
echo For more information, see QUICKSTART.md
echo.

pause
