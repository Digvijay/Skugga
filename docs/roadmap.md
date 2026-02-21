# Roadmap

## Released

### v1.4.0 -- 100% Moq Feature Parity 
- LINQ to Mocks (`Mock.Of<T>`)
- Ref/Out parameter support
- MockRepository
- Protected member mocking
- Recursive mocks
- Additional argument matchers (`It.IsIn`, `It.IsNotNull`, `It.IsRegex`)

### v1.3.0 -- Enterprise Quality 
- GitHub CodeQL integration
- CycloneDX SBOM generation
- EditorConfig enforcement
- AOT compatibility analysis

### v1.2.0 -- Documentation & Tooling 
- Comprehensive documentation (60KB+ across 4 guides)
- Spectral-inspired OpenAPI linting (16 rules)
- Incremental generation cache
- Parallel generation
- Demo projects for all features

### v1.1.0 -- Enterprise Structure 
- Microsoft-style project organization
- Dual generator NuGet packaging
- CI/CD improvements
- 937 tests passing

### v1.0.0 -- Initial Release 
- Core mocking with `Mock.Create<T>()`
- Setup/Returns/Verify
- Chaos mode
- AutoScribe
- Zero-allocation assertions
- Native AOT compatibility

## Planned

### v1.5.0 -- Enhanced OpenAPI
- OpenAPI 2.0 (Swagger) support
- YAML format support
- Operation filtering by tags
- Custom type mapping
- ETag-based cache invalidation

### v2.0.0 -- Next Generation
- IDE integration (Visual Studio / Rider)
- Real-time mock suggestions
- Performance profiling integration
- Cross-platform test runners
