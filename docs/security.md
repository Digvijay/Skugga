# Security Policy

## Reporting Vulnerabilities

If you discover a security vulnerability, please report it responsibly:

- **Email:** [security@digvijay.dev](mailto:security@digvijay.dev)
- **Do not** open a public issue for security vulnerabilities

We will respond within 48 hours and work with you to resolve the issue.

## Supported Versions

| Version | Supported |
|---------|-----------|
| 1.4.x |  Active |
| 1.3.x |  Security fixes |
| 1.2.x |  Critical only |
| < 1.2 |  End of life |

## Security Measures

- **CycloneDX SBOM** generated with every release
- **GitHub CodeQL** scans on all branches
- **Dependabot** monitors dependencies
- **Zero runtime reflection** -- reduced attack surface
- **Native AOT compatible** -- "distroless" deployments supported

## Supply Chain Security

Skugga's NuGet packages include:
- SBOM (Software Bill of Materials) in CycloneDX format
- Repository commit hash for reproducible builds
- `IsAotCompatible` metadata for SDK verification
