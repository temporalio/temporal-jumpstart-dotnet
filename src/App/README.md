# App

Generic scaffold for building Temporal workflow applications with .NET.

## Running the Applications

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Temporal CLI](https://docs.temporal.io/cli#install) (for local development)

### Local Development Setup

1. **Start Temporal CLI dev server:**
   ```sh
   temporal server start-dev
   ```

2. **Run the Worker (in a separate terminal):**
   ```sh
   cd {SolutionRoot}/src/App/App.Workers
   dotnet run --launch-profile local
   ```

3. **Run the API (in a separate terminal):**
   ```sh
   cd {SolutionRoot}/src/App/App.Api
   dotnet run --launch-profile local
   ```

### Accessing the API

Once the API is running, you can access:

- **Swagger UI**: [https://localhost:7148/swagger](https://localhost:7148/swagger)
- **HTTP endpoint**: http://localhost:5031
- **HTTPS endpoint**: https://localhost:7148

### API Endpoints

The API exposes the following endpoints under `/api/v1/users`:

- **POST `/api/v1/users/{id}`** - Start a new workflow execution
- **GET `/api/v1/users/{id}`** - Query workflow state

### Configuration

The application uses configuration files located in `{SolutionRoot}/config/`:

- **`appsettings.Local.json`**: Connects to local Temporal CLI service (`localhost:7233`) with default namespace
- **`appsettings.Cloud.json`**: Connects to Temporal Cloud with mTLS authentication
