# Contributing

We welcome community contributions! Skugga is evolving from proof-of-concept to production-ready, and your help is valuable.

## How to Contribute

-  **Found a bug?** Open an [Issue](https://github.com/Digvijay/Skugga/issues)
-  **Have an idea?** Start a [Discussion](https://github.com/Digvijay/Skugga/discussions)
-  **Want to help?** Check the [Contributing Guide](https://github.com/Digvijay/Skugga/blob/master/CONTRIBUTING.md)
-  **Submit a PR** following the guidelines

## Development Setup

```bash
# Clone the repository
git clone https://github.com/Digvijay/Skugga.git
cd Skugga

# Restore and build
dotnet restore Skugga.slnx
dotnet build Skugga.slnx

# Run tests
dotnet test Skugga.slnx
```

## Code Standards

- Follow the `.editorconfig` rules
- All code must pass `dotnet format --verify-no-changes`
- `TreatWarningsAsErrors` is enabled in Release builds
- All new features must include tests

## Branch Strategy

- `master` -- stable releases
- `development` -- integration branch
- `feature/**` -- feature branches
