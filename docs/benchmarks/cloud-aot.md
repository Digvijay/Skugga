# Cloud & AOT Performance

Skugga enables the full performance benefits of Native AOT for .NET applications.

## Cold Start Comparison

| Metric | Standard .NET (JIT) | Skugga (Native AOT) | Impact |
|--------|--------------------|--------------------|--------|
| **Startup Time** | 476 ms | **72 ms** | **6.6x Faster**  |

Serverless functions can respond to requests almost instantly, eliminating cold-start latency.

## Alpine vs. Debian (AOT Deployments)

| Metric | Native AOT (Alpine) | Native AOT (Debian) | Impact |
|--------|--------------------|--------------------|--------|
| **Startup Time** | **66 ms** | 835 ms | **12.6x Faster**  |

Alpine Linux's minimal footprint provides faster startup for Native AOT applications.

## Execution Performance

| Metric | Standard .NET (JIT) | Skugga (Native AOT) | Impact |
|--------|--------------------|--------------------|--------|
| **Execution Time** | ~1.3 s | **~0.3 s** | **4x Faster**  |

Lower CPU bills and more responsive applications.

## Container Size

| Configuration | Size | Notes |
|---------------|------|-------|
| Standard .NET | ~200 MB | Requires full runtime |
| **Skugga AOT** | **47 MB** | Self-contained, "distroless" |

**76% smaller** deployments = faster container pulls, quicker auto-scaling.

## Build Scalability

| Metric | Result |
|--------|--------|
| **500 mock classes** | 0.41s build time |
| **500 test executions** | 0.49s (~1ms each) |

Zero impact on developer workflow.

## Running on Azure

Skugga + Azure is a natural fit:

- **Azure Container Apps**: Minimal resource allocation, lower costs
- **Azure Functions**: Near-instant cold starts
- **Azure Kubernetes Service**: Faster pod scaling, smaller images

### Key Advantages

- **Cost Efficiency** -- 80% less CPU = lower compute bills
- **Instant Scale** -- Smaller images = faster pulls
- **Enhanced Security** -- "Distroless" containers reduce attack surface
