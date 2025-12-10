# Onboardings

Generated content ends up in the `Onboardings.Generated` project.

## Running the Applications

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Temporal CLI](https://docs.temporal.io/cli#install) (for local development)
* [Buf](https://buf.build/docs/cli/installation/) (for proto generation)

### Running Onboardings.Api

#### Local Development (Temporal CLI)
```sh
cd {SolutionRoot}/src/Onboardings/Onboardings.Api
dotnet run --configuration=Local
```

#### Temporal Cloud
```sh
cd {SolutionRoot}/src/Onboardings/Onboardings.Api
dotnet run --configuration=Cloud
```

### Running Onboardings.Workers

#### Local Development (Temporal CLI)
```sh
cd {SolutionRoot}/src/Onboardings/Onboardings.Workers
dotnet run --configuration=Local
```

#### Local Worker (Alternative Local Config)
```sh
cd {SolutionRoot}/src/Onboardings/Onboardings.Workers
dotnet run --configuration=LocalWorker
```

#### Temporal Cloud
```sh
cd {SolutionRoot}/src/Onboardings/Onboardings.Workers
dotnet run --configuration=Cloud
```

### Configuration

The launch profiles use configuration files located in `{SolutionRoot}/config/`:

- **`appsettings.Local.json`**: Connects to local Temporal CLI service (`localhost:7233`) with default namespace
- **`appsettings.LocalWorker.json`**: Alternative local configuration with different Prometheus metrics port (9464)
- **`appsettings.Cloud.json`**: Connects to Temporal Cloud with mTLS authentication

### Protobufs

The messages used in the `onboardings` and `snailforce` services will be generated as follows.

```sh
cd {SolutionRoot}/src/Onboardings
# generate messages
buf generate
```
