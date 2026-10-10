# OpenTelemetry setup for non-.NET services

Use this reference when the user opts in to adding OpenTelemetry instrumentation to non-.NET services (Step 8). Aspire automatically injects `OTEL_EXPORTER_OTLP_ENDPOINT` into all managed resources — the services just need to read it.

## Node.js/TypeScript services

```bash
# Use the repo's package manager (npm/pnpm/yarn)
npm install @opentelemetry/sdk-node @opentelemetry/auto-instrumentations-node @opentelemetry/exporter-otlp-grpc
# or: pnpm add ...
# or: yarn add ...
```

Create an instrumentation file (e.g., `instrumentation.ts` or `instrumentation.js`):

```typescript
import { NodeSDK } from '@opentelemetry/sdk-node';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-otlp-grpc';
import { OTLPMetricExporter } from '@opentelemetry/exporter-otlp-grpc';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';

const sdk = new NodeSDK({
  traceExporter: new OTLPTraceExporter(),
  metricReader: new PeriodicExportingMetricReader({
    exporter: new OTLPMetricExporter(),
  }),
  instrumentations: [getNodeAutoInstrumentations()],
  serviceName: process.env.OTEL_SERVICE_NAME,
});

sdk.start();
```

Then ensure the service loads it early — either via `--require`/`--import` in the start script or by importing it as the first line of the entry point.

## Python services

```bash
pip install opentelemetry-distro opentelemetry-exporter-otlp
opentelemetry-bootstrap -a install  # auto-detect and install framework instrumentations
```

Add to the service's startup (e.g., top of `main.py` or as a separate `instrumentation.py`):

```python
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.metrics import MeterProvider
from opentelemetry.sdk.metrics.export import PeriodicExportingMetricReader
from opentelemetry.exporter.otlp.proto.grpc.metric_exporter import OTLPMetricExporter
from opentelemetry import trace, metrics
import os

resource = Resource.create({"service.name": os.environ.get("OTEL_SERVICE_NAME", "unknown")})

# Traces
trace.set_tracer_provider(TracerProvider(resource=resource))
trace.get_tracer_provider().add_span_processor(BatchSpanProcessor(OTLPSpanExporter()))

# Metrics
metrics.set_meter_provider(MeterProvider(
    resource=resource,
    metric_readers=[PeriodicExportingMetricReader(OTLPMetricExporter())],
))
```

Or more simply, run with the auto-instrumentation wrapper:

```bash
opentelemetry-instrument uvicorn main:app --host 0.0.0.0
```

## Go services

```bash
go get go.opentelemetry.io/otel
go get go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracegrpc
go get go.opentelemetry.io/otel/sdk/trace
go get go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp
```

Add initialization in `main()`:

```go
import (
    "go.opentelemetry.io/otel"
    "go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracegrpc"
    sdktrace "go.opentelemetry.io/otel/sdk/trace"
)

func initTracer() func() {
    exporter, _ := otlptracegrpc.New(context.Background())
    tp := sdktrace.NewTracerProvider(sdktrace.WithBatcher(exporter))
    otel.SetTracerProvider(tp)
    return func() { tp.Shutdown(context.Background()) }
}
```

Wrap HTTP handlers with `otelhttp.NewHandler()` for automatic HTTP span creation.

## Java services

Point the user to the [OpenTelemetry Java Agent](https://opentelemetry.io/docs/zero-code/java/agent/) — it's the easiest approach:

```bash
java -javaagent:opentelemetry-javaagent.jar -jar myapp.jar
```

The agent auto-instruments common frameworks. Aspire injects `OTEL_EXPORTER_OTLP_ENDPOINT` automatically.
