# Common .NET Setup and Build Workflow

This repository contains a reusable GitHub Actions workflow for building, analyzing, and releasing .NET projects. The workflow is designed to be called from other workflows or repositories using the `workflow_call` trigger, making it easy to share common CI/CD logic across multiple .NET projects.

## Workflow File

- **Main Workflow:** `.github/workflows/dotnet-ci.yml`
  - Trigger: `workflow_call`

### Inputs

| Input | Required | Type | Default | Description |
|-------|----------|------|---------|-------------|
| `solutionName` | **Yes** | `string` | — | Name of the solution file **without** the extension (e.g., `MySolution` not `MySolution.sln`). This value is also used to derive the paths for unit tests (`{solutionName}.UnitTests`), integration tests (`{solutionName}.IntegrationTests`), and benchmarks (`{solutionName}.Benchmarks`). |
| `useSlnx` | No | `boolean` | `false` | When set to `true`, uses the new `.slnx` XML-based solution format instead of the traditional `.sln` format. The `.slnx` format is a simplified, XML-based solution file format introduced in .NET 9. |
| `buildOs` | No | `string` | `linux` | Specifies the primary operating system for building, packing, and generating release artifacts. Valid values: `linux` or `windows`. The primary OS is responsible for creating NuGet packages, SBOM, and other release artifacts. |
| `requiresMacOS` | No | `boolean` | `false` | When set to `true`, enables additional build and test execution on macOS. Note: macOS builds are for validation only and never generate release artifacts. Useful for ensuring cross-platform compatibility. |
| `runIntTestsOnPrimaryOsOnly` | No | `boolean` | `false` | When set to `true`, integration tests will only run on the primary build OS (specified by `buildOs`). Useful when integration tests only support a specific runtime, such as WPF binaries that only work on Windows. |

### Secrets

| Secret | Required | Description |
|--------|----------|-------------|
| `NUGET_USER` | No | Your NuGet.org profile/username (NOT your email address) for Trusted Publishers authentication via OIDC. When configured, enables secure NuGet package publishing without storing long-lived API keys. You must first configure your repository as a Trusted Publisher on NuGet.org. |
| `NUGET_API_KEY` | No | Traditional NuGet.org API key for package publishing. Used as a fallback when Trusted Publishers (OIDC) is not configured. Less secure than OIDC as it requires storing a long-lived secret. |
| `SONAR_TOKEN` | No | Authentication token for SonarCloud code analysis. Required for SonarCloud integration. Obtain from your SonarCloud account settings. |
| `SONAR_PROJECT_KEY` | No | The unique project key for your SonarCloud project. Found in your SonarCloud project settings. Required along with `SONAR_TOKEN` and `SONAR_ORGANISATION_KEY` for SonarCloud analysis. |
| `SONAR_ORGANISATION_KEY` | No | Your SonarCloud organization key. Found in your SonarCloud organization settings. Required along with `SONAR_TOKEN` and `SONAR_PROJECT_KEY` for SonarCloud analysis. |
| `VIRUSTOTAL_API_KEY` | No | API key for VirusTotal scanning of NuGet packages. When configured, uploads packages to VirusTotal for malware scanning before release. Obtain from your VirusTotal account. |
| `CODECOV_TOKEN` | No | Upload token for Codecov code coverage reporting. When configured, uploads test coverage reports to Codecov for tracking and visualization. Obtain from your Codecov project settings. |

## Multi-OS Build Support

The workflow supports building and testing on multiple operating systems:

- **Linux (default):** The default primary OS for building, packing, and testing. All release artifacts are generated on Linux by default.
- **Windows:** Can be configured as the primary OS for building and packing when required. Set `buildOs: windows` to use Windows as the primary build OS.
- **macOS (optional):** Can be enabled for additional build and test coverage. macOS builds never generate release artifacts. Enable with `requiresMacOS: true`.

**Key behaviors:**
- Only the **primary OS** (specified by `buildOs`) generates and uploads release artifacts (NuGet packages, SBOM, etc.)
- When the primary OS is set to Windows, Linux does not run build/pack operations to avoid duplicate artifacts
- macOS builds, when enabled, only build and test - they never pack or release
- Test coverage is uploaded only from the primary OS
- Binlog artifacts are OS-specific and include the OS name in the artifact name

## Jobs Overview

The workflow consists of several jobs, many of which delegate their logic to dedicated sub-workflows:

| Job Name                  | Description                                           | Implementation File                                   |
|---------------------------|------------------------------------------------------|-------------------------------------------------------|
| `appinspector`            | Code feature analysis                                 | `_wfc_dotnet-ci-appinspector.yml`                     |
| `build`                   | Orchestrates multi-OS builds                         | `_wfc_dotnet-ci-build.yml` (orchestrator)              |
| `build-linux`             | Builds on Linux (when selected as primary OS)        | `_wfc_dotnet-ci-build-linux.yml`                      |
| `build-macos`             | Builds on macOS (optional, never releases)           | `_wfc_dotnet-ci-build-macos.yml`                      |
| `build-windows`           | Builds on Windows (when selected as primary OS)      | `_wfc_dotnet-ci-build-windows.yml`                    |
| `check-codeql-enabled`    | Checks if CodeQL should run based on repository visibility and Advanced Security settings | `_wfc_dotnet-ci-check-codeql-enabled.yml`             |
| `check-deterministic-enabled` | Checks if deterministic builds are enabled (Deterministic and ContinuousIntegrationBuild properties) | `_wfc_dotnet-ci-check-deterministic-enabled.yml`      |
| `check-nuget-user-populated` | Checks if NUGET_USER secret is set | Inline                                                |
| `check-nuget-trusted-publisher-valid` | Validates NuGet Trusted Publisher login before approval by testing NuGet/login@v1 action | Inline (uses `NuGet/login@v1`)  |
| `check-nuget-environment` | Validates NuGet environment protection               | Inline (uses `dpvreony/ensure-environment-protected`)  |
| `check-release-required`  | Determines if a release is needed based on code changes | Inline (compares changes since last release)       |
| `codeql`                  | Performs CodeQL security analysis on C# code         | `_wfc_dotnet-ci-codeql.yml`                           |
| `dependency-review`       | Reviews new/changed dependencies (PR only)           | Inline (uses `actions/dependency-review-action`)       |
| `deprecated-nuget-packages` | Checks for deprecated NuGet packages               | `_wfc_dotnet-ci-deprecated-nuget-packages.yml`        |
| `licenses`                | Checks project licenses                              | `_wfc_dotnet-ci-licenses.yml`                         |
| `omd-generation`          | Generates object-modeling technique (OMT) diagrams   | `_wfc_dotnet-ci-omd-generation.yml`                   |
| `release`                 | Creates a GitHub release and pushes NuGet packages using Trusted Publishers (OIDC) | Inline (uses `actions/create-release` & `NuGet/login@v1`) |
| `snitch`                  | A tool that helps you find duplicate transitive package references | `_wfc_dotnet-ci-snitch.yml`                           |
| `validate-renovate`       | Validates Renovate configuration                     | Inline (uses `dpvreony/github-action-renovate-config-validator`) |
| `vulnerable-nuget-packages` | Checks for vulnerable NuGet packages               | `_wfc_dotnet-ci-vulnerable-nuget-packages.yml`        |

### Sub-workflow Files

These workflow files are referenced by the main workflow via the `uses:` keyword:

- `.github/workflows/_wfc_dotnet-ci-appinspector.yml`
- `.github/workflows/_wfc_dotnet-ci-build.yml` (orchestrator for multi-OS builds)
- `.github/workflows/_wfc_dotnet-ci-build-linux.yml` (Linux-specific build)
- `.github/workflows/_wfc_dotnet-ci-build-macos.yml` (macOS-specific build)
- `.github/workflows/_wfc_dotnet-ci-build-windows.yml` (Windows-specific build)
- `.github/workflows/_wfc_dotnet-ci-check-codeql-enabled.yml`
- `.github/workflows/_wfc_dotnet-ci-check-deterministic-enabled.yml`
- `.github/workflows/_wfc_dotnet-ci-codeql.yml`
- `.github/workflows/_wfc_dotnet-ci-deprecated-nuget-packages.yml`
- `.github/workflows/_wfc_dotnet-ci-licenses.yml`
- `.github/workflows/_wfc_dotnet-ci-omd-generation.yml`
- `.github/workflows/_wfc_dotnet-ci-snitch.yml`
- `.github/workflows/_wfc_dotnet-ci-vulnerable-nuget-packages.yml`

These files implement the detailed logic for each stage of the workflow. You can customize or extend them as needed.

## Architecture: Two-Checkout Pattern

The per-OS build workflows (`_wfc_dotnet-ci-build-linux.yml`, `_wfc_dotnet-ci-build-macos.yml`, `_wfc_dotnet-ci-build-windows.yml`) use a **two-checkout pattern** to enable proper operation when called from other repositories:

1. **Triggering Repository Checkout** (`path: a`): The repository being built is checked out to the `a` directory. This contains the source code, solution files, and `global.json` that define the .NET project being built.

2. **Workflow Repository Checkout** (`path: github-action-workflows`): This repository (containing the reusable workflows and composite actions) is checked out to the `github-action-workflows` directory. This provides access to the composite action files at `.github/actions/dotnet-build-common/`.

**Why is this necessary?**

When a workflow is triggered via `workflow_call` from another repository, GitHub Actions only checks out the triggering repository by default. Since the build workflows use a local composite action (`.github/actions/dotnet-build-common`), that action's files must be available in the runner workspace. The two-checkout pattern solves this by explicitly checking out both repositories.

**Key implementation details:**

- The composite action uses `a/global.json` to determine the .NET SDK version
- All build operations work in the `a/src` directory
- Artifact outputs remain in the runner workspace root under `artifacts/`
- The composite action is referenced via `./github-action-workflows/.github/actions/dotnet-build-common`

### .NET Tool Installation Strategy

The composite action installs .NET tools globally rather than using local tool manifests. This design decision addresses path complexity issues that arise from the two-checkout pattern:

**Why global installation?**

When using `dotnet tool restore --tool-manifest`, the tools are installed relative to the manifest location. With two repositories checked out to different directories (`a/` and `github-action-workflows/`), this creates path resolution challenges:

1. The tool manifest is located in `github-action-workflows/.config/dotnet-tools.json`
2. The consuming repository's code is in `a/src/`
3. Local tools installed via manifest would need complex relative path handling to be accessible from both locations

**The global installation approach:**

The composite action parses the `dotnet-tools.json` file directly and installs each tool globally using `dotnet tool install --global`. This ensures:

- Tools are available in the system PATH regardless of the current working directory
- No path resolution issues between the two checked-out repositories
- Simplified tool invocation (just use the tool name, e.g., `dotnet-outdated` instead of `dotnet tool run dotnet-outdated`)
- Consistent tool availability across all workflow steps
- Updating of the tools remains "as-is" (i.e. Renovate)

This strategy makes the workflow more maintainable and eliminates the complexity of managing tool paths across repository boundaries.

## Usage

To use this workflow in your repository:

1. Reference this workflow via `workflow_call` in your own GitHub Actions workflow.
2. Pass the required `solutionName` input (without the `.sln` or `.slnx` extension).
3. Optionally configure `useSlnx: true` if using the new `.slnx` solution format.
4. Optionally provide secrets for NuGet, SonarCloud, etc.
5. Ensure the required sub-workflow files exist in `.github/workflows/`.

### Basic Example

```yaml
jobs:
  ci:
    uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
    with:
      solutionName: MySolution
    secrets:
      NUGET_USER: ${{ secrets.NUGET_USER }}
      SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      SONAR_PROJECT_KEY: ${{ secrets.SONAR_PROJECT_KEY }}
      SONAR_ORGANISATION_KEY: ${{ secrets.SONAR_ORGANISATION_KEY }}
      CODECOV_TOKEN: ${{ secrets.CODECOV_TOKEN }}
```

### Using .slnx Solution Format

For projects using the new XML-based `.slnx` solution format (introduced in .NET 9):

```yaml
jobs:
  ci:
    uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
    with:
      solutionName: MySolution
      useSlnx: true  # Uses MySolution.slnx instead of MySolution.sln
    secrets:
      NUGET_USER: ${{ secrets.NUGET_USER }}
```

### Multi-OS Usage Examples

**Default (Linux only):**
```yaml
uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
with:
  solutionName: MySolution
```

**Windows as primary OS:**
```yaml
uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
with:
  solutionName: MySolution
  buildOs: windows
```

**Linux with additional macOS builds:**
```yaml
uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
with:
  solutionName: MySolution
  requiresMacOS: true
```

**Windows with additional macOS builds:**
```yaml
uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
with:
  solutionName: MySolution
  buildOs: windows
  requiresMacOS: true
```

**Windows-only integration tests (e.g., WPF projects):**
```yaml
uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
with:
  solutionName: MySolution
  buildOs: windows
  runIntTestsOnPrimaryOsOnly: true  # Integration tests only run on Windows
```

**Using .slnx with Windows build:**
```yaml
uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
with:
  solutionName: MySolution
  useSlnx: true
  buildOs: windows
```

### Full Configuration Example

This example shows all available inputs and secrets:

```yaml
jobs:
  ci:
    uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
    with:
      solutionName: MySolution          # Required: solution name without extension
      useSlnx: false                    # Optional: use .slnx format (default: false)
      buildOs: linux                    # Optional: primary build OS (default: linux)
      requiresMacOS: false              # Optional: enable macOS builds (default: false)
      runIntTestsOnPrimaryOsOnly: false # Optional: run int tests only on primary OS (default: false)
    secrets:
      NUGET_USER: ${{ secrets.NUGET_USER }}                       # For Trusted Publishers (OIDC)
      NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}                 # Fallback API key
      SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}                     # SonarCloud authentication
      SONAR_PROJECT_KEY: ${{ secrets.SONAR_PROJECT_KEY }}         # SonarCloud project key
      SONAR_ORGANISATION_KEY: ${{ secrets.SONAR_ORGANISATION_KEY }} # SonarCloud org key
      VIRUSTOTAL_API_KEY: ${{ secrets.VIRUSTOTAL_API_KEY }}       # VirusTotal scanning
      CODECOV_TOKEN: ${{ secrets.CODECOV_TOKEN }}                 # Code coverage upload
```

## NuGet Publishing

The workflow uses NuGet.org's Trusted Publishers feature, which uses OpenID Connect (OIDC) tokens instead of API keys. This is more secure as it eliminates the need to store long-lived API keys as secrets.

**Setup:**
1. Configure your repository as a Trusted Publisher on NuGet.org for your packages
2. Add the `NUGET_USER` secret to your repository with your NuGet.org username (profile name, NOT your email address)
3. Ensure the `nuget` environment exists in your repository with appropriate protection rules
4. The workflow will automatically use the `NuGet/login@v1` action to obtain a short-lived API key via OIDC

**Example:**
```yaml
secrets:
  NUGET_USER: ${{ secrets.NUGET_USER }}  # Your NuGet.org profile name
```

**How it works:**
- The workflow checks if `NUGET_USER` is configured before requesting approval
- After approval, the `NuGet/login@v1` action exchanges the GitHub OIDC token for a short-lived NuGet API key
- The temporary API key is used to authenticate with NuGet.org for package publishing
- No long-lived API key is required

**Enabling/Disabling NuGet Publishing:**
- Set the `NUGET_USER` secret to enable NuGet package publishing
- Remove or unset the `NUGET_USER` secret to disable NuGet package publishing

## Key Features

- **Multi-OS Support:** Build and test on Linux, Windows, and macOS with configurable primary OS for releases.
- **Modular Design:** Uses sub-workflows for modularity and reusability.
- **Security:** Checks for environment protection before NuGet release. Includes CodeQL security analysis for public repositories and private repositories with Advanced Security enabled.
- **Smart Release Detection:** Automatically determines if a release is needed by comparing changes since the last release, excluding test files.
- **Old Build Protection:** Prevents outdated builds from triggering releases when a newer release already exists.
- **Pending Release Cancellation:** Automatically rejects old pending releases when a new release is approved.
- **Release Automation:** Creates GitHub releases and pushes NuGet packages.
- **Dependency Checks:** Reviews dependencies and validates Renovate configs.
- **Comprehensive Analysis:** Includes code inspection, license checking, object-modeling technique (OMT) diagram generation, vulnerable and deprecated NuGet package checks.

### Release Detection Logic

The `check-release-required` job intelligently determines if a new release is necessary by:

1. Fetching the latest GitHub release tag
2. If no release exists, defaulting to requiring a release
3. If a release exists, comparing changes between the release tag and the current commit
4. Checking for changes in the `/src/` directory while excluding test folders (matching pattern `*.*Tests`)
5. Setting a flag to indicate whether a release is required

This ensures that releases are only created when there are meaningful changes to the source code, not just test updates.

### Old Build Protection

The `check-release-required` job includes logic to prevent outdated builds from triggering releases:

When checking if a release is required, the workflow verifies that the current commit is not older than the latest release. This prevents the scenario where:
- Build A starts for commit X
- Build B starts for commit Y (newer than X)  
- Build B completes and creates a release
- Build A (now outdated) is prevented from triggering a release

The check uses git ancestry to determine if the current commit is an ancestor of the latest release's commit. If so, the build is considered outdated and `release_required` is set to `false`, preventing any release from being created.

### Pending Release Cancellation

When a new release is approved in the `nuget` environment, the workflow automatically cancels any older releases that are still waiting for approval. This prevents confusion and ensures only the most recent version gets released.

The cancellation process:

1. When a release job enters the `nuget` environment (after approval), it first checks for other workflow runs in 'waiting' status
2. For each waiting workflow run, it checks if there are pending deployments to the `nuget` environment
3. Any pending deployments found are automatically rejected with a comment explaining:
   - The version that superseded them
   - The commit SHA of the new release
   - The workflow run ID that was approved

This feature requires the `actions: write` permission on the release job to manage deployment approvals.

**Example rejection comment:**
```
Superseded by release 1.2.3 (SHA: abc123d) from run 1234567890
```

---

For details on each sub-workflow, refer to the respective YAML files in `.github/workflows/`.
