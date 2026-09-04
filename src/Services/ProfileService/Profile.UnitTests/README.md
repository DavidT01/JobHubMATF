# Profile Service Testing

This document explains how to run the **`Profile Service`** unit tests and generate a code coverage report.

## Prerequisites

- **.NET SDK 10.0** or later
- **ReportGenerator** (optional)

## Run unit tests

From the **[Profile.UnitTests](.)** directory:

```powershell
dotnet test
```

The command restores dependencies, builds `Profile.API` and `Profile.UnitTests` and runs all discovered tests.

## Collect code coverage

To collect coverage in **Cobertura** format:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

The generated `xml` file is placed under a `TestResults` subdirectory.

## Generate an HTML report

After collecting coverage, generate the HTML report:

```powershell
reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:TestResults/coverage-report -reporttypes:Html
```

Open the report at `TestResults/coverage-report/index.html`. The report shows line, branch and method coverage for the code exercised by the `Profile Service` tests.

## Clean generated files

To remove test and coverage output:

```powershell
Remove-Item -Recurse -Force TestResults -ErrorAction SilentlyContinue
```
