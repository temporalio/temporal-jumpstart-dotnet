# GrafanaCloud Integration Setup

This guide walks you through setting up the OpenTelemetry Collector to forward Temporal .NET metrics to GrafanaCloud.

## Prerequisites

- Docker and Docker Compose installed
- A GrafanaCloud account (free tier available at https://grafana.com/products/cloud/)
- Your Temporal .NET worker running and exposing Prometheus metrics

## Step 1: Get GrafanaCloud Credentials

1. Log into your GrafanaCloud account
2. Navigate to **Connections** → **Add new connection** → **Hosted Prometheus metrics**
3. Or go directly to: https://grafana.com/docs/grafana-cloud/send-data/metrics/metrics-prometheus/
4. You'll need:
   - **Prometheus Remote Write Endpoint**: Usually looks like `https://prometheus-prod-XX-XXX.grafana.net/api/prom/push`
   - **Username**: Your instance ID (e.g., `123456`)
   - **API Key/Token**: Generate a new API token with `metrics:write` permission

## Step 2: Create Environment Variables

Create a `.env` file in the project root (this file is gitignored):

```bash
# .env
GRAFANA_CLOUD_PROMETHEUS_ENDPOINT=https://prometheus-prod-XX-XXX.grafana.net/api/prom/push
GRAFANA_CLOUD_API_KEY=<your-instance-id>:<your-api-token>
```

**Note**: The API key should be in the format `username:password` or use a Bearer token. If using username/password:
```bash
GRAFANA_CLOUD_API_KEY=123456:glc_xxxxxxxxxxxxxxxxxxxx
```

Alternatively, if using Bearer token format, the collector config already sets the Authorization header correctly.

## Step 3: Start the OpenTelemetry Collector

```bash
# Start the collector
docker-compose -f docker-compose.otel.yaml up -d

# View logs to verify it's running
docker-compose -f docker-compose.otel.yaml logs -f otel-collector
```

You should see log messages indicating:
- The collector has started successfully
- It's scraping metrics from your Temporal worker (port 9464)
- It's sending data to GrafanaCloud

## Step 4: Start Your Temporal Worker

Make sure your Temporal .NET worker is running and exposing metrics:

```bash
cd src/Onboardings/Onboardings.Workers
dotnet run --configuration=LocalWorker
```

The worker will expose Prometheus metrics on port 9464 (as configured in TemporalExtensions.cs).

## Step 5: Verify Metrics in GrafanaCloud

1. Log into your GrafanaCloud dashboard
2. Go to **Explore**
3. Select your Prometheus data source
4. Query for metrics like:
   - `temporal_workflow_*`
   - `temporal_activity_*`
   - `temporal_worker_*`
5. Filter by label: `service="temporal-worker"`

## Troubleshooting

### Collector can't reach worker metrics

If you see errors about connecting to `host.docker.internal:9464`:

**On Linux**: Replace `host.docker.internal` with your machine's IP or `172.17.0.1` in `otel-collector-config.yaml`:
```yaml
- targets: ['172.17.0.1:9464']
```

**On macOS/Windows**: `host.docker.internal` should work out of the box.

### No metrics appearing in GrafanaCloud

1. Check collector logs: `docker-compose -f docker-compose.otel.yaml logs otel-collector`
2. Verify worker is exposing metrics: `curl http://localhost:9464/metrics`
3. Check GrafanaCloud API key has `metrics:write` permission
4. Verify the endpoint URL is correct (should end with `/api/prom/push`)

### Authentication errors

If you see 401/403 errors, check your API key format:
- Should be `<username>:<api-token>` or just the token if using Bearer auth
- Make sure the API token is active and has proper permissions

## Customization

### Change scrape interval

Edit `otel-collector-config.yaml`:
```yaml
scrape_interval: 30s  # Change from default 15s
```

### Add additional labels

Edit the `resource` processor in `otel-collector-config.yaml`:
```yaml
processors:
  resource:
    attributes:
      - key: team
        value: platform
        action: upsert
```

### Enable debug logging

Edit `otel-collector-config.yaml`, uncomment the logging exporter:
```yaml
service:
  pipelines:
    metrics:
      exporters: [prometheusremotewrite, logging]
```

## Stopping the Collector

```bash
docker-compose -f docker-compose.otel.yaml down
```

## Architecture

```
Temporal .NET Worker (localhost:9464)
         ↓ (scrape every 15s)
OpenTelemetry Collector (Docker)
         ↓ (batch & forward)
GrafanaCloud Prometheus
         ↓
GrafanaCloud Dashboards
```

## Next Steps

- Create Grafana dashboards for your Temporal metrics
- Set up alerts for workflow failures or high activity latency
- Consider adding traces and logs for full observability