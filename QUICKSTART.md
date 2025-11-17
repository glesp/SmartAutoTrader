# SmartAutoTrader Quick Start Guide

This guide will help you get the SmartAutoTrader application running on your local machine in minutes.

## Prerequisites

Before you begin, ensure you have the following installed:

- **Node.js** (v18 or higher) - [Download](https://nodejs.org/)
- **Python** (3.10 or higher) - [Download](https://www.python.org/downloads/)
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Git** - [Download](https://git-scm.com/)

To verify installations, run:
```bash
node --version    # Should show v18 or higher
python --version  # Should show 3.10 or higher
dotnet --version  # Should show 8.0.x
```

## Quick Setup (All Services)

### 1. Clone the Repository

```bash
git clone https://github.com/glesp/SmartAutoTrader.git
cd SmartAutoTrader
```

### 2. Set Up Environment Variables

#### Backend (.NET API)

Create or update `backend/appsettings.Development.json` (or use User Secrets):

```json
{
  "Jwt": {
    "Key": "YourSecretKeyHere_AtLeast32Characters_ForJWT",
    "Issuer": "SmartAutoTrader",
    "Audience": "SmartAutoTraderUsers"
  },
  "Services": {
    "ParameterExtraction": {
      "Endpoint": "http://localhost:5006/extract_parameters",
      "Timeout": 30
    }
  }
}
```

#### Python Service

Create `PythonServices/parameter_extraction_service/.env`:

```env
OPENROUTER_API_KEY=your_openrouter_api_key_here
PORT=5006
```

> **Note**: Get your OpenRouter API key from [https://openrouter.ai/](https://openrouter.ai/)

### 3. Install Dependencies

Open **three separate terminal windows** and run:

#### Terminal 1: Backend
```bash
cd backend
dotnet restore SmartAutoTrader.API.csproj
dotnet ef database update  # Creates the database
```

#### Terminal 2: Frontend
```bash
cd frontend
npm ci
```

#### Terminal 3: Python Service
```bash
cd PythonServices/parameter_extraction_service
python -m venv venv
# On Windows:
venv\Scripts\activate
# On macOS/Linux:
source venv/bin/activate
pip install -r requirements.txt
```

### 4. Run All Services

Keep the three terminals open and run:

#### Terminal 1: Backend API
```bash
cd backend
dotnet run --launch-profile https
```
**Default URL**: https://localhost:7079

#### Terminal 2: Frontend
```bash
cd frontend
npm run dev
```
**Default URL**: http://localhost:5173

#### Terminal 3: Python Service
```bash
cd PythonServices/parameter_extraction_service
# Make sure venv is activated
python parameter_extraction_service.py
```
**Default URL**: http://localhost:5006

### 5. Access the Application

Open your browser and navigate to:
```
http://localhost:5173
```

You should see the SmartAutoTrader homepage. You can now:
- Register a new account
- Browse vehicles
- Use the AI chat assistant for recommendations

## Troubleshooting

### Common Issues

#### 1. Port Already in Use

If you see an error like "Address already in use":

```bash
# Find and kill the process using the port (example for port 5173)
# On Linux/macOS:
lsof -ti:5173 | xargs kill -9
# On Windows:
netstat -ano | findstr :5173
taskkill /PID <PID> /F
```

#### 2. Database Migration Errors

If `dotnet ef database update` fails:

```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Retry the migration
cd backend
dotnet ef database update
```

#### 3. Python Dependencies Installation Slow

The PyTorch and transformers packages are large. The installation may take 5-10 minutes.

```bash
# Optional: Install without CUDA if you don't have an NVIDIA GPU
pip install torch --index-url https://download.pytorch.org/whl/cpu
pip install -r requirements.txt
```

#### 4. OpenRouter API Key Missing

If the Python service fails with API key errors:

1. Ensure your `.env` file exists in `PythonServices/parameter_extraction_service/`
2. Check that `OPENROUTER_API_KEY` is set correctly
3. Verify your API key is valid at [https://openrouter.ai/](https://openrouter.ai/)

#### 5. CORS Errors in Browser Console

If you see CORS errors, verify:

1. Backend is running on the correct port (check `launchSettings.json`)
2. Frontend API URL matches backend URL
3. Backend CORS configuration allows the frontend origin

#### 6. Frontend Build Errors

If `npm ci` fails:

```bash
# Delete node_modules and package-lock.json
rm -rf node_modules package-lock.json
# Reinstall
npm install
```

### Verification Checklist

Use this checklist to verify everything is running:

- [ ] Backend API responding at https://localhost:7079/api/vehicles (or your configured port)
- [ ] Frontend accessible at http://localhost:5173
- [ ] Python service responding at http://localhost:5006/health (if health endpoint exists)
- [ ] Can register a new user account
- [ ] Can view vehicle listings
- [ ] Chat assistant is working (sends messages without errors)

## Development Tips

### Running Tests

```bash
# Backend tests
cd SmartAutoTrader.Tests
dotnet test

# Frontend tests
cd frontend
npm test

# Python tests
cd PythonServices/parameter_extraction_service
pytest
```

### Linting

```bash
# Backend formatting
cd backend
dotnet format SmartAutoTrader.API.csproj

# Frontend linting
cd frontend
npm run lint

# Python linting
cd PythonServices/parameter_extraction_service
flake8 parameter_extraction_service.py retriever/retriever.py
```

### Hot Reload

All three services support hot reload:

- **Backend**: Changes to .cs files will trigger automatic rebuild
- **Frontend**: Vite provides instant HMR (Hot Module Replacement)
- **Python**: Flask will auto-reload on file changes (if debug mode is enabled)

## Next Steps

- Review the [main README](README.md) for detailed architecture information
- Explore the API endpoints using the `.http` file in the backend folder
- Customize the vehicle seeding data in `backend/DataSeeding/`
- Configure your preferred AI models in the Python service

## Need Help?

If you encounter issues not covered here:

1. Check the [GitHub Issues](https://github.com/glesp/SmartAutoTrader/issues)
2. Review service logs (terminal output)
3. Ensure all prerequisites are correctly installed
4. Verify environment variables are set correctly

---

**Happy Coding! 🚗💨**
