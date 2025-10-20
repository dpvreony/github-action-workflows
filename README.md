# Common .NET Setup and Build Workflow

This repository contains a reusable GitHub Actions workflow for building, analyzing, and releasing .NET projects. The workflow is designed to be called from other workflows or repositories using the `workflow_call` trigger, making it easy to share common CI/CD logic across multiple .NET projects.

## Workflow File

- **Main Workflow:** `.github/workflows/dotnet-ci.yml`
  - Trigger: `workflow_call`
  - Inputs:
    - `solutionName` (required) — Name of the solution file **without** extension.
    - `buildOs` (optional) — Primary OS for build and pack. Options: `linux` (default), `windows`.
    - `requiresMacOS` (optional) — If `true`, also builds and tests on macOS (never releases from macOS). Default: `false`.
  - Secrets: Supports optional secrets for NuGet, SonarCloud, VirusTotal, Codecov, etc.

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
| `build`                   | Orchestrates multi-OS builds                         | `_wfc_dotnet-ci-build.yml` (orchestrator)              |
| `build-linux`             | Builds on Linux (when selected as primary OS)        | `_wfc_dotnet-ci-build-linux.yml`                      |
| `build-windows`           | Builds on Windows (when selected as primary OS)      | `_wfc_dotnet-ci-build-windows.yml`                    |
| `build-macos`             | Builds on macOS (optional, never releases)           | `_wfc_dotnet-ci-build-macos.yml`                      |
| `licenses`                | Checks project licenses                              | `_wfc_dotnet-ci-licenses.yml`                         |
| `snitch`                  | A tool that helps you find duplicate transitive package references | `_wfc_dotnet-ci-snitch.yml`                           |
| `appinspector`            | code feature analysis                                 | `_wfc_dotnet-ci-appinspector.yml`                     |
| `omd-generation`          | Generates object-modeling technique (OMT) diagrams   | `_wfc_dotnet-ci-omd-generation.yml`                   |
| `vulnerable-nuget-packages` | Checks for vulnerable NuGet packages               | `_wfc_dotnet-ci-vulnerable-nuget-packages.yml`        |
| `deprecated-nuget-packages` | Checks for deprecated NuGet packages               | `_wfc_dotnet-ci-deprecated-nuget-packages.yml`        |
| `dependency-review`       | Reviews new/changed dependencies (PR only)           | Inline (uses `actions/dependency-review-action`)       |
| `validate-renovate`       | Validates Renovate configuration                     | Inline (uses `dpvreony/github-action-renovate-config-validator`) |
| `check-nuget-api-key`     | Checks if NuGet API key is set                       | Inline                                                |
| `check-nuget-environment` | Validates NuGet environment protection               | Inline (uses `dpvreony/ensure-environment-protected`)  |
| `check-release-required`  | Determines if a release is needed based on code changes | Inline (compares changes since last release)       |
| `release`                 | Creates a GitHub release and pushes NuGet packages   | Inline (uses `actions/create-release` & `dotnet nuget push`) |

### Sub-workflow Files

These workflow files are referenced by the main workflow via the `uses:` keyword:

- `.github/workflows/_wfc_dotnet-ci-build.yml` (orchestrator for multi-OS builds)
- `.github/workflows/_wfc_dotnet-ci-build-linux.yml` (Linux-specific build)
- `.github/workflows/_wfc_dotnet-ci-build-windows.yml` (Windows-specific build)
- `.github/workflows/_wfc_dotnet-ci-build-macos.yml` (macOS-specific build)
- `.github/workflows/_wfc_dotnet-ci-licenses.yml`
- `.github/workflows/_wfc_dotnet-ci-snitch.yml`
- `.github/workflows/_wfc_dotnet-ci-appinspector.yml`
- `.github/workflows/_wfc_dotnet-ci-omd-generation.yml`
- `.github/workflows/_wfc_dotnet-ci-vulnerable-nuget-packages.yml`
- `.github/workflows/_wfc_dotnet-ci-deprecated-nuget-packages.yml`

These files implement the detailed logic for each stage of the workflow. You can customize or extend them as needed.

## Usage

To use this workflow in your repository:

1. Reference this workflow via `workflow_call` in your own GitHub Actions workflow.
2. Pass the required `solutionName` input (without the `.sln` extension).
3. Optionally provide secrets for NuGet, SonarCloud, etc.
4. Ensure the required sub-workflow files exist in `.github/workflows/`.

Example workflow dispatch:

```yaml
jobs:
  ci:
    uses: dpvreony/github-action-workflows/.github/workflows/dotnet-ci.yml@main
    with:
      solutionName: MySolution
      # Optional: specify Windows as the primary build OS
      # buildOs: windows
      # Optional: enable macOS builds and tests
      # requiresMacOS: true
    secrets:
      NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
      SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
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

## Key Features

- **Multi-OS Support:** Build and test on Linux, Windows, and macOS with configurable primary OS for releases.
- **Modular Design:** Uses sub-workflows for modularity and reusability.
- **Security:** Checks for environment protection before NuGet release.
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
Superseded by release 1.2.3 (SHA: abc123def456) from run 1234567890
```

---

For details on each sub-workflow, refer to the respective YAML files in `.github/workflows/`.
