#!/bin/bash

# SmartAutoTrader Setup Script
# This script helps set up the development environment

set -e  # Exit on error

echo "==================================================================="
echo "  SmartAutoTrader - Development Environment Setup"
echo "==================================================================="
echo ""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

# Check prerequisites
echo "Checking prerequisites..."
echo ""

# Check Node.js
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    print_status "Node.js installed: $NODE_VERSION"
else
    print_error "Node.js is not installed. Please install Node.js 18 or higher."
    exit 1
fi

# Check Python
if command -v python3 &> /dev/null; then
    PYTHON_VERSION=$(python3 --version)
    print_status "Python installed: $PYTHON_VERSION"
else
    print_error "Python is not installed. Please install Python 3.10 or higher."
    exit 1
fi

# Check .NET
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    print_status ".NET installed: $DOTNET_VERSION"
else
    print_error ".NET SDK is not installed. Please install .NET 8 SDK."
    exit 1
fi

echo ""
echo "All prerequisites are installed!"
echo ""

# Setup Backend
echo "==================================================================="
echo "  Setting up Backend (.NET)"
echo "==================================================================="
echo ""

cd backend

if [ ! -f "appsettings.Development.json" ]; then
    print_warning "appsettings.Development.json not found. Creating template..."
    cat > appsettings.Development.json << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Key": "YourSecretKeyHere_AtLeast32Characters_ForJWT_ChangeThis",
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
EOF
    print_status "Created appsettings.Development.json (remember to update JWT Key!)"
else
    print_status "appsettings.Development.json already exists"
fi

print_status "Restoring .NET dependencies..."
dotnet restore SmartAutoTrader.API.csproj

# Check if EF tools are installed
if ! command -v dotnet-ef &> /dev/null; then
    print_warning "Entity Framework tools not found. Installing..."
    dotnet tool install --global dotnet-ef
fi

print_status "Running database migrations..."
dotnet ef database update

cd ..

print_status "Backend setup complete!"
echo ""

# Setup Frontend
echo "==================================================================="
echo "  Setting up Frontend (React)"
echo "==================================================================="
echo ""

cd frontend

print_status "Installing frontend dependencies..."
npm ci

cd ..

print_status "Frontend setup complete!"
echo ""

# Setup Python Service
echo "==================================================================="
echo "  Setting up Python Service"
echo "==================================================================="
echo ""

cd PythonServices/parameter_extraction_service

if [ ! -f ".env" ]; then
    print_warning ".env file not found. Creating template..."
    cat > .env << 'EOF'
OPENROUTER_API_KEY=your_openrouter_api_key_here
PORT=5006
EOF
    print_status "Created .env file (remember to add your OpenRouter API key!)"
else
    print_status ".env file already exists"
fi

if [ ! -d "venv" ]; then
    print_status "Creating Python virtual environment..."
    python3 -m venv venv
fi

print_status "Activating virtual environment and installing dependencies..."
print_warning "This may take several minutes (downloading PyTorch and transformers)..."

source venv/bin/activate
pip install --upgrade pip
pip install -r requirements.txt

cd ../..

print_status "Python service setup complete!"
echo ""

# Final instructions
echo "==================================================================="
echo "  Setup Complete! 🎉"
echo "==================================================================="
echo ""
echo "Next steps:"
echo ""
echo "1. Update your environment variables:"
echo "   - backend/appsettings.Development.json (JWT Key)"
echo "   - PythonServices/parameter_extraction_service/.env (OpenRouter API Key)"
echo ""
echo "2. Run the services in separate terminals:"
echo ""
echo "   Terminal 1 - Backend:"
echo "   $ cd backend && dotnet run --launch-profile https"
echo ""
echo "   Terminal 2 - Frontend:"
echo "   $ cd frontend && npm run dev"
echo ""
echo "   Terminal 3 - Python Service:"
echo "   $ cd PythonServices/parameter_extraction_service"
echo "   $ source venv/bin/activate"
echo "   $ python parameter_extraction_service.py"
echo ""
echo "3. Open http://localhost:5173 in your browser"
echo ""
echo "For more information, see QUICKSTART.md"
echo ""
