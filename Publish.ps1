#Requires -Version 7.0

<#
.SYNOPSIS
    Builds, tests, and publishes the Halo PSA API NuGet package.

.DESCRIPTION
    This script performs the following operations:
    1. Cleans and restores the solution
    2. Builds the solution in Release configuration
    3. Runs all unit and integration tests with 100% success rate requirement
    4. Prompts for version tag if tests pass
    5. Creates and pushes the version tag to trigger CI/CD pipeline
    6. The GitHub Actions workflow will then build and publish to NuGet.org

.PARAMETER Version
    The version to tag and publish (e.g., "1.0.0", "2.1.3-beta")
    If not provided, will prompt for input.

.PARAMETER SkipTests
    Skip running tests (not recommended for production releases).

.PARAMETER DryRun
    Perform all operations except creating and pushing the git tag.

.EXAMPLE
    .\Publish.ps1
    Prompts for version and runs full publish process.

.EXAMPLE
    .\Publish.ps1 -Version "1.2.0"
    Publishes version 1.2.0 after running tests.

.EXAMPLE
    .\Publish.ps1 -Version "2.0.0-beta" -DryRun
    Tests the process without actually creating tags.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [switch]$SkipTests,

    [Parameter(Mandatory = $false)]
    [switch]$DryRun
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ANSI color codes for better output
$Red = "`e[31m"
$Green = "`e[32m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Magenta = "`e[35m"
$Cyan = "`e[36m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = $Reset
    )
    # Use Write-Information for display output to avoid capturing in variables
    # This satisfies Codacy while maintaining proper PowerShell output behavior
    $InformationPreference = 'Continue'
    Write-Information "$Color$Message$Reset" -InformationAction Continue
}

function Write-Step {
    param([string]$Message)
    Write-ColorOutput "?? $Message" $Blue
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "? $Message" $Green
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "??  $Message" $Yellow
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "? $Message" $Red
}

function Test-GitRepository {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw "Git is not installed or not in PATH"
    }

    if (-not (Test-Path ".git")) {
        throw "Not in a git repository"
    }

    # Check for uncommitted changes
    $status = git status --porcelain
    if ($status) {
        Write-Warning "Uncommitted changes detected:"
        git status --short
        $continue = Read-Host "Continue anyway? (y/N)"
        if ($continue -ne "y" -and $continue -ne "Y") {
            throw "Aborted due to uncommitted changes"
        }
    }
}

function Test-DotNetVersion {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET CLI is not installed or not in PATH"
    }

    $dotnetVersion = dotnet --version
    Write-ColorOutput "Using .NET version: $dotnetVersion" $Cyan

    # Check if .NET 10 is available (project targets .NET 9/.NET 10)
    $sdks = dotnet --list-sdks
    $hasNet10 = $sdks | Where-Object { $_ -match "10\." }
    if (-not $hasNet10) {
        Write-Warning ".NET 10 SDK not found. Project targets .NET 9/.NET 10."
        Write-ColorOutput "Available SDKs:" $Cyan
        $sdks | ForEach-Object { Write-ColorOutput "  $_" $Cyan }
    }
}

function Invoke-Build {
    Write-Step "Cleaning solution..."
    dotnet clean --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Clean failed" }

    Write-Step "Restoring NuGet packages..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

    Write-Step "Building solution in Release configuration..."
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }

    Write-Success "Build completed successfully"
}

function Invoke-Tests {
    if ($SkipTests) {
        Write-Warning "Skipping tests as requested"
        return
    }

    Write-Step "Running all tests..."
    Write-ColorOutput "?? Test execution started..." $Cyan

    # Run tests with detailed output and capture both stdout and stderr
    $testResult = dotnet test --configuration Release --no-build --verbosity normal 2>&1
    $testExitCode = $LASTEXITCODE

    # Display the test output
    $testResult | ForEach-Object { Write-Output $_ }

    if ($testExitCode -ne 0) {
        Write-Error "Tests failed with exit code $testExitCode"
        throw "Test execution failed"
    }

    # Look for the test summary in the output
    $summaryFound = $false
    $totalTests = 0
    $failedTests = 0
    $succeededTests = 0
    $skippedTests = 0

    # Parse the test output for results
    foreach ($line in $testResult) {
        if ($line -match "Test run summary:\s*(\w+)") {
            $summaryFound = $true
            $testStatus = $Matches[1]
            if ($testStatus -eq "Failed") {
                Write-Error "Test run summary indicates failure"
                throw "Test execution failed"
            }
        }

        if ($line -match "total:\s*(\d+)") {
            $totalTests = [int]$Matches[1]
        }

        if ($line -match "failed:\s*(\d+)") {
            $failedTests = [int]$Matches[1]
        }

        if ($line -match "succeeded:\s*(\d+)") {
            $succeededTests = [int]$Matches[1]
        }

        if ($line -match "skipped:\s*(\d+)") {
            $skippedTests = [int]$Matches[1]
        }
    }

    # Validate results
    if ($failedTests -gt 0) {
        Write-Error "Found $failedTests failed tests - 100% success rate required"
        throw "Test failures detected"
    }

    if ($skippedTests -gt 0) {
        Write-Warning "Found $skippedTests skipped tests"
        $continue = Read-Host "Skipped tests found. Continue with publish? (y/N)"
        if ($continue -ne "y" -and $continue -ne "Y") {
            throw "Aborted due to skipped tests"
        }
    }

    # If we got here and exit code was 0, tests passed
    Write-Success "All tests passed! ?"
    if ($totalTests -gt 0) {
        Write-ColorOutput "Test Results: $succeededTests/$totalTests tests succeeded, $failedTests failed, $skippedTests skipped" $Green
    } else {
        Write-ColorOutput "Test execution completed successfully" $Green
    }
}

function Get-VersionInput {
    if ($Version) {
        return $Version
    }

    Write-ColorOutput "`n?? Version Information" $Magenta

    # Use nbgv to get the automatically determined version
    Write-Step "Getting version from nbgv (Nerdbank.GitVersioning)..."
    try {
        $nbgvOutput = nbgv get-version --format json 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "nbgv command failed"
        }

        $versionInfo = $nbgvOutput | ConvertFrom-Json
        $autoVersion = $versionInfo.NuGetPackageVersion

        Write-ColorOutput "Automatically determined version: $autoVersion" $Green
        Write-ColorOutput "  Version: $($versionInfo.Version)" $Cyan
        Write-ColorOutput "  AssemblyVersion: $($versionInfo.AssemblyVersion)" $Cyan
        Write-ColorOutput "  NuGetPackageVersion: $($versionInfo.NuGetPackageVersion)" $Cyan

        # Show recent tags for context
        Write-ColorOutput "`nRecent tags:" $Cyan
        $recentTags = git tag --sort=-version:refname | Select-Object -First 5
        if ($recentTags) {
            $recentTags | ForEach-Object { Write-ColorOutput "  $_" $Cyan }
        } else {
            Write-ColorOutput "  No existing tags" $Cyan
        }

        # Check if tag already exists
        $tag = "v$autoVersion"
        $existingTag = git tag -l $tag
        if ($existingTag) {
            Write-Warning "Tag '$tag' already exists"
            $overwrite = Read-Host "Overwrite existing tag? (y/N)"
            if ($overwrite -ne "y" -and $overwrite -ne "Y") {
                throw "Tag already exists and overwrite not confirmed"
            }
        }

        return $autoVersion

    } catch {
        Write-Warning "Failed to get version from nbgv: $($_.Exception.Message)"
        Write-ColorOutput "Falling back to manual version input..." $Yellow

        # Fallback to manual input
        Write-ColorOutput "`nVersion format examples:" $Yellow
        Write-ColorOutput "  ? 1.0.0 (release)" $Yellow
        Write-ColorOutput "  ? 1.2.3-beta (pre-release)" $Yellow
        Write-ColorOutput "  ? 2.0.0-alpha.1 (pre-release)" $Yellow

        do {
            $inputVersion = Read-Host "`nEnter version to publish (without 'v' prefix)"
            if ([string]::IsNullOrWhiteSpace($inputVersion)) {
                Write-Warning "Version cannot be empty"
                continue
            }

            # Basic version validation
            if ($inputVersion -notmatch '^\d+\.\d+\.\d+(-[\w\d\.-]+)?$') {
                Write-Warning "Invalid version format. Use semantic versioning (e.g., 1.0.0, 1.2.3-beta)"
                continue
            }

            $tag = "v$inputVersion"

            # Check if tag already exists
            $existingTag = git tag -l $tag
            if ($existingTag) {
                Write-Warning "Tag '$tag' already exists"
                $overwrite = Read-Host "Overwrite existing tag? (y/N)"
                if ($overwrite -eq "y" -or $overwrite -eq "Y") {
                    return $inputVersion
                }
                continue
            }

            return $inputVersion
        } while ($true)
    }
}

function Invoke-GitTag {
    param([string]$Version)

    $tag = "v$Version"

    if ($DryRun) {
        Write-Warning "DRY RUN: Would create and push tag '$tag'"
        Write-ColorOutput "Git commands that would be executed:" $Yellow
        Write-ColorOutput "  git tag -a $tag -m 'Release $Version'" $Yellow
        Write-ColorOutput "  git push origin $tag" $Yellow
        return
    }

    Write-Step "Creating git tag '$tag'..."

    # Delete existing tag if it exists (for overwrites)
    $existingTag = git tag -l $tag
    if ($existingTag) {
        Write-Warning "Deleting existing tag '$tag'..."
        git tag -d $tag
        git push --delete origin $tag 2>$null  # Ignore errors if remote tag doesn't exist
    }

    # Create new tag
    git tag -a $tag -m "Release $Version"
    if ($LASTEXITCODE -ne 0) { throw "Failed to create tag" }

    Write-Step "Pushing tag to origin..."
    git push origin $tag
    if ($LASTEXITCODE -ne 0) { throw "Failed to push tag" }

    Write-Success "Tag '$tag' created and pushed successfully!"
    Write-ColorOutput "?? GitHub Actions workflow will now:" $Magenta
    Write-ColorOutput "  • Build the solution" $Cyan
    Write-ColorOutput "  • Run tests" $Cyan
    Write-ColorOutput "  • Pack NuGet package" $Cyan
    Write-ColorOutput "  • Publish to NuGet.org" $Cyan
    Write-ColorOutput "  • Create GitHub release" $Cyan

    $actionsUrl = "https://github.com/panoramicdata/HaloPSA.Api/actions"
    Write-ColorOutput "`n?? Monitor the build at: $actionsUrl" $Blue
}

function Main {
    try {
        Write-ColorOutput "`n?? Halo PSA API NuGet Publishing Script" $Magenta
        Write-ColorOutput "=======================================" $Magenta

        # Pre-flight checks
        Write-Step "Performing pre-flight checks..."
        Test-GitRepository
        Test-DotNetVersion
        Write-Success "Pre-flight checks passed"

        # Build solution
        Invoke-Build

        # Run tests
        Invoke-Tests

        # Get version
        $versionToPublish = Get-VersionInput

        # Show publication summary (no confirmation needed for automated versioning)
        if (-not $DryRun) {
            Write-ColorOutput "`n?? Publication Summary:" $Magenta
            Write-ColorOutput "  Version: v$versionToPublish" $Cyan
            Write-ColorOutput "  Target: NuGet.org" $Cyan
            Write-ColorOutput "  Package: Halo.Api" $Cyan

            # Only prompt for confirmation if version was manually specified
            if ($Version) {
                $confirm = Read-Host "`nProceed with publication? (y/N)"
                if ($confirm -ne "y" -and $confirm -ne "Y") {
                    Write-Warning "Publication cancelled by user"
                    return
                }
            } else {
                Write-ColorOutput "`n? Proceeding with automated publication (version determined by nbgv)" $Green
            }
        }

        # Create and push tag
        Invoke-GitTag -Version $versionToPublish

        Write-Success "`n?? Publication process initiated successfully!"
        Write-ColorOutput "The NuGet package will be available shortly at:" $Green
        Write-ColorOutput "https://www.nuget.org/packages/HaloPsa.Api/$versionToPublish" $Blue

    } catch {
        Write-Error "`n?? Publication failed: $($_.Exception.Message)"
        Write-ColorOutput "Stack trace:" $Red
        Write-ColorOutput $_.ScriptStackTrace $Red
        exit 1
    }
}

# Run the main function
Main