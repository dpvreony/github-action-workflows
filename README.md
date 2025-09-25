# Common .NET Setup and Build Workflow

This repository contains a reusable GitHub Actions workflow for building, analyzing, and releasing .NET projects. The workflow is designed to be called from other workflows or repositories using the `workflow_call` trigger, making it easy to share common CI/CD logic across multiple .NET projects.

## Workflow File

- **Main Workflow:** `.github/workflows/dotnet-ci.yml`
  - Trigger: `workflow_call`
  - Input: `solutionName` (required) — Name of the solution file **without** extension.
  - Secrets: Supports optional secrets for NuGet, SonarCloud, VirusTotal, Codecov, etc.

## Jobs Overview

The workflow consists of several jobs, many of which delegate their logic to dedicated sub-workflows:

| Job Name                  | Description                                           | Implementation File                                   |
|---------------------------|------------------------------------------------------|-------------------------------------------------------|
| `build`                   | Builds the specified solution                        | `_wfc_dotnet-ci-build.yml`                            |
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
| `release`                 | Creates a GitHub release and pushes NuGet packages   | Inline (uses `actions/create-release` & `dotnet nuget push`) |

### Sub-workflow Files

These workflow files are referenced by the main workflow via the `uses:` keyword:

- `.github/workflows/_wfc_dotnet-ci-build.yml`
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
    secrets:
      NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
      SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

## Key Features

- **Modular Design:** Uses sub-workflows for modularity and reusability.
- **Security:** Checks for environment protection before NuGet release.
- **Release Automation:** Creates GitHub releases and pushes NuGet packages.
- **Dependency Checks:** Reviews dependencies and validates Renovate configs.
- **Comprehensive Analysis:** Includes code inspection, license checking, object-modeling technique (OMT) diagram generation, vulnerable and deprecated NuGet package checks.

---

For details on each sub-workflow, refer to the respective YAML files in `.github/workflows/`.
