# Smart Auto Trader 🚗💨

[![CI Status] An AI-powered vehicle marketplace designed to offer personalized recommendations and streamline the car buying experience through intelligent chat interaction and advanced filtering.

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Environment Variables](#environment-variables)
  - [Backend Setup (.NET)](#backend-setup-net)
  - [Frontend Setup (React)](#frontend-setup-react)
  - [Python Service Setup](#python-service-setup)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Features ✨

- **User Authentication & Profile:** Secure user registration and login. Profile management for user details and preferences.
- **Vehicle Browsing & Filtering:** Display vehicle listings with detailed information. Advanced filtering options including make, model, year, price, fuel type, transmission, vehicle type, and mileage.
- **AI-Powered Recommendations:**
  - **Chat Interface:** Conversational AI assistant to understand user needs in natural language.
  - **Parameter Extraction:** Python service using LLMs (via OpenRouter) to extract search criteria (price, make, type, fuel, features, intent) from chat messages.
  - **Contextual Conversation:** Maintains conversation context for follow-up questions and refinements.
  - **Personalized Results:** Generates vehicle recommendations based on extracted criteria and conversation context.
- **Inquiry System:** Allows users to send inquiries about specific vehicles. _(Admin reply functionality might exist based on DTOs/status)_.
- **Favorites Management:** Users can save and manage their favorite vehicles.
- **Browsing History:** Tracks recently viewed vehicles for logged-in users.

## Tech Stack 💻

- **Frontend:** React, TypeScript, Material UI (MUI), Vite
- **Backend:** .NET 8 (C#), ASP.NET Core Web API, Entity Framework Core, SQLite
- **AI/Python Service:** Python (Flask), Sentence Transformers (for embeddings/retrieval), OpenRouter API Client
- **Authentication:** JWT (JSON Web Tokens)
- **Testing:** .NET Unit Tests (xUnit likely, based on template)
- **Linting/Formatting:** dotnet format, ESLint, flake8, black

## Project Structure

This repository is organized as a monorepo:

/
├── backend/ # .NET Core API (C#)
│ ├── Controllers/
│ ├── Data/
│ ├── Models/
│ ├── Services/ (Auth, AI Recommendation, Context)
│ ├── Repositories/
│ └── ...
├── frontend/ # React Application (TypeScript)
│ ├── public/
│ ├── src/
│ │ ├── components/ (Chat, Layout, Vehicles)
│ │ ├── contexts/ (Auth)
│ │ ├── pages/
│ │ ├── services/ (API client)
│ │ └── types/
│ └── ...
├── PythonServices/ # Python Microservices
│ └── parameter_extraction_service/ # Extracts parameters from chat
│ ├── retriever/
│ └── parameter_extraction_service.py
└── ... (Configuration files, Dockerfile, CI workflows etc.)

## Getting Started 🚀

### Prerequisites

- .NET 8 SDK: [Download & Install](https://dotnet.microsoft.com/download/dotnet/8.0)
- Node.js (v18 or higher): [Download & Install](https://nodejs.org/)
- Python (3.10 or higher recommended): [Download & Install](https://www.python.org/downloads/)
- npm (usually included with Node.js)
- Git: [Download & Install](https://git-scm.com/)

### Environment Variables

Create necessary environment configuration files (e.g., `.env` for Python, `appsettings.Development.json` secrets for .NET) and populate them with required values:

- **Backend (`appsettings.Development.json` or User Secrets):**
  - `ConnectionStrings:DefaultConnection` (If not using default SQLite name/location)
  - `Jwt:Key` (A strong secret key for JWT signing)
  - `Jwt:Issuer`
  - `Jwt:Audience`
  - `Services:ParameterExtraction:Endpoint` (URL for the Python service, e.g., `http://localhost:5006/extract_parameters`)
  - `Services:ParameterExtraction:Timeout` (Optional timeout in seconds)
- **Python Service (`.env` file in `PythonServices/parameter_extraction_service`):**
  - `OPENROUTER_API_KEY` (Your API key for OpenRouter)
  - _(Optionally other settings like port)_

### Backend Setup (.NET)

1.  Navigate to the backend project directory:
    ```bash
    cd backend
    ```
2.  Restore dependencies:
    ```bash
    dotnet restore SmartAutoTrader.API.csproj
    ```
    _(Note: Adjust `SmartAutoTrader.API.csproj` if your project file has a different name)_
3.  Apply Entity Framework migrations (if needed - creates the database schema):
    ```bash
    dotnet tool install --global dotnet-ef # Install EF tools if you haven't already
    dotnet ef database update
    ```
4.  Run the backend API:
    ```bash
    dotnet run --launch-profile https
    ```
    _(Check `launchSettings.json` for correct profile names and ports)_

### Frontend Setup (React)

1.  Navigate to the frontend directory:
    ```bash
    cd frontend
    ```
2.  Install dependencies:
    ```bash
    npm ci
    ```
3.  Start the development server:
    ```bash
    npm run dev
    ```
    _(This usually opens the app in your browser, often at `http://localhost:5173`)_

### Python Service Setup

1.  Navigate to the Python service directory:
    ```bash
    cd PythonServices/parameter_extraction_service
    ```
2.  Create and activate a virtual environment (Recommended):
    ```bash
    python -m venv venv
    # On Windows:
    # .\venv\Scripts\activate
    # On macOS/Linux:
    source venv/bin/activate
    ```
3.  Install dependencies:
    ```bash
    pip install -r requirements.txt
    ```
    _(You might need to create a `requirements.txt` file based on your imports: `Flask`, `requests`, `sentence-transformers`, `numpy`, etc.)_
4.  Run the Flask service:
    ```bash
    python parameter_extraction_service.py
    ```
    _(This usually starts the service on `http://localhost:5006` by default)_

## Usage

1.  Ensure the Backend API, Frontend App, and Python Service are all running.
2.  Open your browser and navigate to the frontend URL (e.g., `http://localhost:5173`).
3.  Register or log in.
4.  Browse vehicles or start interacting with the Chat Assistant on the Recommendations page.
