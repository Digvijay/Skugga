# Release Guide for Skugga v1.1.0

## Summary of Changes

Your `refactor/new-work` branch contains:
1. ✅ Repository reorganized to enterprise-grade structure
2. ✅ CI workflow paths fixed after reorganization
3. ✅ System.Collections.Immutable version conflicts resolved
4. ✅ GitHub Actions updated to latest versions (checkout@v6, setup-dotnet@v5, upload-artifact@v6, gittools/actions@v4.2.0)
5. ✅ Project files organized into logical subdirectories
6. ✅ Both generators (Skugga.Generator + Skugga.Core.Generators) packaged as analyzers
7. ✅ All 937 tests passing
8. ✅ Clean builds with 0 warnings, 0 errors

## NuGet Package Configuration ✅

The Skugga NuGet package (v1.1.0) now includes:

### Runtime Library
- `lib/net8.0/Skugga.Core.dll` - Main runtime library

### Source Generators (Automatically register as analyzers)
- `analyzers/dotnet/cs/Skugga.Generator.dll` - Main mock generator
- `analyzers/dotnet/cs/Skugga.Core.Generators.dll` - Extension method generator

### Build Targets (Automatic configuration)
- `build/Skugga.targets` - Configures interceptors preview namespace
- `buildTransitive/Skugga.targets` - Transitive dependency support

### Package Metadata
- README.md, icon.png, LICENSE

## How It Works

When developers install the Skugga NuGet package:

1. **NuGet automatically recognizes** DLLs in `analyzers/dotnet/cs/` as Roslyn analyzers
2. **Both generators are registered** with the project's compilation
3. **Build targets automatically run** and configure `InterceptorsPreviewNamespaces`
4. **Generators execute during build** producing mock implementations and extensions
5. **No manual configuration required** - it just works!

## Release Process

### Step 1: Merge to Development Branch

```bash
# Ensure you're on refactor/new-work with all changes committed
git checkout refactor/new-work
git log --oneline -8  # Review your commits

# Merge into development
git checkout development
git pull origin development
git merge refactor/new-work --no-ff -m "feat: v1.1.0 enterprise-grade refactor

- Repository reorganization (enterprise structure)
- CI/CD improvements (latest GitHub Actions)
- Dependency fixes (System.Collections.Immutable)
- Project file organization
- Dual generator packaging
- All tests passing (937 tests)"

# Push to remote
git push origin development
```

### Step 2: Merge to Master Branch

```bash
# Merge development into master
git checkout master
git pull origin master
git merge development --no-ff -m "release: v1.1.0

Major refactoring release with enterprise-grade structure,
improved CI/CD, and proper NuGet packaging with dual generators."

# Push to remote
git push origin master
```

### Step 3: Create and Push Tag

```bash
# Create annotated tag for v1.1.0
git tag -a v1.1.0 -m "Release v1.1.0

Highlights:
- Enterprise-grade repository structure
- Improved NuGet packaging (both generators included)
- Updated CI/CD pipelines
- All dependency conflicts resolved
- 937 tests passing

See CHANGELOG.md for full details."

# Push the tag to trigger the publish workflow
git push origin v1.1.0
```

### Step 4: Monitor the Release

The GitHub Actions workflow will automatically:

1. ✅ Checkout code with tag
2. ✅ Setup .NET 8 and .NET 10
3. ✅ Restore dependencies
4. ✅ Build entire solution (Release mode)
5. ✅ Run all 937 tests
6. ✅ Build Skugga.Generator (Release)
7. ✅ Build Skugga.Core.Generators (Release)
8. ✅ Pack Skugga.Core.csproj with both generators
9. ✅ Publish to NuGet.org using Trusted Publishing
10. ✅ Upload artifacts for verification

Watch the workflow at:
`https://github.com/Digvijay/Skugga/actions/workflows/publish.yml`

### Step 5: Verify Publication

After the workflow succeeds:

1. Check NuGet.org: `https://www.nuget.org/packages/Skugga/1.1.0`
2. Verify package contents:
   ```bash
   dotnet nuget locals all --clear
   dotnet tool install --global nuget.commandline
   nuget install Skugga -Version 1.1.0 -OutputDirectory ./temp
   unzip -l ./temp/Skugga.1.1.0/Skugga.1.1.0.nupkg | grep analyzers
   ```

3. Test in a new project:
   ```bash
   mkdir TestSkugga && cd TestSkugga
   dotnet new console
   dotnet add package Skugga --version 1.1.0
   # Verify generators are recognized (check .csproj or build output)
   ```

## Rollback Plan

If issues are discovered after release:

```bash
# Yank the package from NuGet (doesn't delete, marks as unlisted)
# Login to NuGet.org and use the web UI to yank the version

# Or use dotnet CLI
dotnet nuget delete Skugga 1.1.0 --source https://api.nuget.org/v3/index.json --api-key YOUR_KEY

# Then fix issues, increment version to 1.1.1, and re-release
```

## Post-Release Checklist

- [ ] Tag v1.1.0 pushed to GitHub
- [ ] GitHub Actions workflow completed successfully
- [ ] Package visible on NuGet.org
- [ ] Package downloaded and tested locally
- [ ] Both generators visible in test project
- [ ] Update GitHub Release notes
- [ ] Announce release (if applicable)

## Notes

- **Version is hardcoded** in `src/Skugga.Core/Skugga.Core.csproj` as `1.1.0`
- **Tag version overrides** this via `/p:PackageVersion` in the workflow
- **Trusted Publishing** is configured (no API key in workflow, uses OIDC)
- **Symbols package** (.snupkg) is automatically created for debugging
- **Deterministic builds** enabled for reproducibility
- **Source Link** enabled for debugging into package source

## Troubleshooting

### Workflow Fails at "Build Generators"
- Check that both generator projects build locally in Release mode
- Verify netstandard2.0 target

### Package Missing Generators
- Check workflow logs for the Pack step
- Verify DLL paths in Skugga.Core.csproj `<None Include=...>` elements
- Ensure Build Generators step completed successfully

### Generators Not Recognized After Install
- Check that `analyzers/dotnet/cs/` path is correct in package
- Verify build targets are in `build/` and `buildTransive/` folders
- Check consumer project has `<InterceptorsPreviewNamespaces>` property

### Tests Fail During Publish
- All tests must pass before attempting to publish
- Run `dotnet test Skugga.slnx --configuration Release` locally first

---

**Ready to Release?** Follow the steps above to publish Skugga v1.1.0! 🚀
