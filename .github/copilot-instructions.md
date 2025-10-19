# GitHub Copilot Instructions for github-action-workflows

## Repository Overview

This repository contains reusable GitHub Actions workflows designed for .NET projects. The workflows provide a modular, comprehensive CI/CD pipeline that can be called from other repositories using the `workflow_call` trigger.

## Architecture & Structure

### Workflow Naming Conventions

- **Main workflows**: Named descriptively (e.g., `dotnet-ci.yml`)
- **Sub-workflows**: Prefixed with `_wfc_` (workflow component) to indicate they are called by other workflows (e.g., `_wfc_dotnet-ci-build.yml`)
- All workflows use `workflow_call` trigger to enable reusability

### Directory Structure

```
.github/
  workflows/        # Reusable workflow definitions
    dotnet-ci.yml   # Main .NET CI/CD workflow
    _wfc_*.yml      # Sub-workflow components
src/
  Workflow/         # .NET solution and projects
```

## Coding Standards

### .NET Code Standards

Follow the conventions defined in `.editorconfig`:

- **Indentation**: 4 spaces
- **New lines**: All braces on new lines (Allman style)
- **Naming conventions**:
  - Private/internal fields: `_camelCase` (with underscore prefix)
  - Constants: `PascalCase`
  - Static fields: `camelCase`
- **Variable declarations**:
  - Avoid `var` for built-in types
  - Only use `var` when type is apparent
- **Code style**:
  - Avoid `this.` unless necessary
  - Use language keywords over BCL types (e.g., `string` not `String`)
  - Use pattern matching where appropriate
  - Use null propagation and coalescing expressions
  - Expression-bodied properties preferred

### YAML Workflow Standards

- Use consistent indentation (2 spaces)
- Include descriptive `name` fields for workflows and steps
- Use pinned action versions with SHA hashes for security
- Document required vs optional secrets
- Use environment variables for repeated values
- Include `if` conditions to skip unnecessary steps

## Workflow Inputs & Outputs

### Common Inputs

- `solutionName`: Required string, the .NET solution name WITHOUT extension
  - Used to derive project paths:
    - Unit tests: `{solutionName}.UnitTests`
    - Integration tests: `{solutionName}.IntegrationTests`
    - Benchmarks: `{solutionName}.Benchmarks`

### Common Secrets

- `NUGET_API_KEY`: For NuGet package publishing
- `SONAR_TOKEN`, `SONAR_PROJECT_KEY`, `SONAR_ORGANISATION_KEY`: For SonarCloud analysis
- `VIRUSTOTAL_API_KEY`: For security scanning
- `CODECOV_TOKEN`: For code coverage reporting

## Key Features & Components

### Build Workflow (`_wfc_dotnet-ci-build.yml`)

- Runs on Windows (currently windows-2025)
- Sets up .NET, Java, and Android SDK
- Installs workloads: android, aspire, ios, tvos, macos, maui
- Uses Nerdbank.GitVersioning (NBGV) for versioning
- Runs SonarCloud analysis (if configured)
- Executes unit tests with coverage
- Executes integration tests with coverage
- Produces NuGet packages
- Generates SBOM (Software Bill of Materials)
- Scans with VirusTotal (if configured)

### Other Workflow Components

- **licenses**: Validates project licenses
- **snitch**: Finds duplicate transitive package references
- **appinspector**: Performs code feature analysis
- **omd-generation**: Generates Object-Modeling Technique diagrams
- **vulnerable-nuget-packages**: Checks for vulnerable dependencies
- **deprecated-nuget-packages**: Checks for deprecated dependencies
- **dependency-review**: Reviews dependency changes in PRs
- **validate-renovate**: Validates Renovate configuration

### Release Logic

The release workflow intelligently determines if a release is needed by:

1. Fetching the latest GitHub release tag
2. Comparing changes between the release tag and current commit
3. Checking for changes in `/src/` directory
4. Excluding test folders (pattern `*.*Tests`)
5. Only creating releases for meaningful source code changes

## Versioning

This repository follows **Semantic Versioning 2.0.0**:

- **MAJOR**: Incompatible API changes
- **MINOR**: Backwards-compatible functionality additions
- **PATCH**: Backwards-compatible bug fixes

Versioning is managed by Nerdbank.GitVersioning (NBGV) using `version.json`.

## Development Guidelines

### Making Changes

1. **Minimal changes**: Make the smallest possible changes to achieve the goal
2. **Consistency**: Follow existing patterns in workflow structure
3. **Testing**: Test workflows locally or in a fork when possible
4. **Documentation**: Update README.md if workflow capabilities change
5. **Security**: Pin action versions with SHA hashes

### Adding New Workflows

1. For main workflows: Use descriptive names without prefixes
2. For sub-workflows: Use `_wfc_` prefix
3. Always use `workflow_call` trigger for reusability
4. Document all inputs and secrets
5. Follow the pattern of existing workflows
6. Add comprehensive steps with proper names

### Modifying Existing Workflows

1. Preserve backward compatibility when possible
2. Update workflow documentation if behavior changes
3. Test with multiple solution structures
4. Consider impact on repositories using these workflows
5. Update version if breaking changes are introduced

## Environment Variables

Standard .NET environment variables used:

```yaml
DOTNET_CLI_TELEMETRY_OPTOUT: 1
DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 1
DOTNET_NOLOGO: true
DOTNET_GENERATE_ASPNET_CERTIFICATE: false
```

## Artifacts

Standard artifact naming:

- `binlogs`: MSBuild binary logs (failure only)
- `unittestcoverage`: Unit test coverage reports
- `inttestcoverage`: Integration test coverage reports
- `nuget`: NuGet packages
- `omd`: Object-modeling diagrams
- `sbom`: Software Bill of Materials
- `outdated`: Outdated package reports

## Best Practices

1. **Reusability**: Design workflows to be called by other repositories
2. **Modularity**: Break complex workflows into sub-workflows
3. **Security**: Use secrets appropriately, never hardcode sensitive data
4. **Efficiency**: Skip unnecessary steps with conditions
5. **Observability**: Generate summaries and notices for key metrics
6. **Reliability**: Handle failures gracefully, upload artifacts for debugging
7. **Compatibility**: Support multiple .NET versions via global.json

## Common Commands

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build --configuration Release --no-restore

# Run unit tests
dotnet test {project}.UnitTests.csproj --configuration Release --no-build

# Run integration tests
dotnet test {project}.IntegrationTests.csproj --configuration Release --no-build

# Pack NuGet packages
dotnet pack --configuration Release --no-build

# List outdated packages
dotnet outdated
```

## Contributing

See `CONTRIBUTING.md` for contribution guidelines. Key points:

1. Fork the repository
2. Apply desired changes
3. Send a pull request
4. Follow existing code and workflow patterns
