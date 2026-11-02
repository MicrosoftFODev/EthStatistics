param(
    [switch]$Auto
)

$ErrorActionPreference = "Stop"

$repo = "C:\Users\zoisa\source\repos\EthStatistics"
$appPackages = Join-Path $repo "EthStatistics.Package\AppPackages"
$webDownloads = Join-Path $repo "EthStatisticsWeb\public\downloads"
$metadataPath = Join-Path $webDownloads "latest-version.json"
$baseUrl = "https://microsoftfodev.github.io/EthStatistics/downloads"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " EthStatistics Release Publisher" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan


# ------------------------------------------------------------
# Validate paths
# ------------------------------------------------------------

if (-not (Test-Path $appPackages))
{
    throw "AppPackages directory not found: $appPackages"
}

if (-not (Test-Path $webDownloads))
{
    New-Item `
        -ItemType Directory `
        -Path $webDownloads `
        -Force |
        Out-Null
}


# ------------------------------------------------------------
# Find all MSIX bundles and determine highest VERSION
# ------------------------------------------------------------

Write-Host ""
Write-Host "Searching for latest package..." -ForegroundColor Yellow

$bundles = Get-ChildItem `
    -Path $appPackages `
    -Recurse `
    -File `
    -Filter "*.msixbundle" |
ForEach-Object {

    if ($_.Name -match '(\d+\.\d+\.\d+\.\d+)')
    {
        [PSCustomObject]@{
            File    = $_
            Version = [Version]$Matches[1]
        }
    }
}

$latest = $bundles |
    Sort-Object Version -Descending |
    Select-Object -First 1


if ($null -eq $latest)
{
    throw "No MSIX bundle was found."
}


$versionText = $latest.Version.ToString()
$bundle = $latest.File


Write-Host "Latest package: $versionText" -ForegroundColor Green
Write-Host "Bundle: $($bundle.FullName)"


# ------------------------------------------------------------
# Package directory
# ------------------------------------------------------------

$sourceFolder = $bundle.Directory.FullName
$folderName = Split-Path $sourceFolder -Leaf

$destinationFolder = Join-Path `
    $webDownloads `
    $folderName


# ------------------------------------------------------------
# Read LOCAL metadata version for information only
# ------------------------------------------------------------

$localPublishedVersion = [Version]"0.0.0.0"

if (Test-Path $metadataPath)
{
    try
    {
        $existingMetadata = Get-Content `
            -Path $metadataPath `
            -Raw |
            ConvertFrom-Json

        if (
            $null -ne $existingMetadata.version -and
            -not [string]::IsNullOrWhiteSpace(
                [string]$existingMetadata.version
            )
        )
        {
            $localPublishedVersion =
                [Version]$existingMetadata.version
        }
    }
    catch
    {
        Write-Host "Existing metadata is invalid and will be replaced." `
            -ForegroundColor Yellow
    }
}


Write-Host "Local metadata version: $localPublishedVersion" `
    -ForegroundColor Cyan


# ------------------------------------------------------------
# IMPORTANT:
# Always regenerate/copy metadata for the highest local package.
#
# We intentionally do NOT exit just because local JSON already has
# the same version. A previous failed run may have changed JSON
# locally without ever pushing it to GitHub.
# ------------------------------------------------------------

Write-Host ""
Write-Host "Preparing website release..." -ForegroundColor Yellow


# ------------------------------------------------------------
# Copy version folder
# ------------------------------------------------------------

if (Test-Path $destinationFolder)
{
    Remove-Item `
        -Path $destinationFolder `
        -Recurse `
        -Force
}

Copy-Item `
    -Path $sourceFolder `
    -Destination $destinationFolder `
    -Recurse `
    -Force


$destinationBundle = Join-Path `
    $destinationFolder `
    $bundle.Name


if (-not (Test-Path $destinationBundle))
{
    throw "Copied bundle could not be found."
}


# ------------------------------------------------------------
# SHA256 integrity check
# ------------------------------------------------------------

Write-Host "Verifying bundle..." -ForegroundColor Yellow

$sourceHash = (
    Get-FileHash `
        -Path $bundle.FullName `
        -Algorithm SHA256
).Hash

$destinationHash = (
    Get-FileHash `
        -Path $destinationBundle `
        -Algorithm SHA256
).Hash


if ($sourceHash -ne $destinationHash)
{
    throw "SHA256 verification failed."
}


Write-Host "Bundle verified." -ForegroundColor Green


# ------------------------------------------------------------
# Generate metadata
# ------------------------------------------------------------

$bundleUrl =
    "$baseUrl/$folderName/$($bundle.Name)"

$releaseNotes =
    "EthStatistics version $versionText is now available."

$metadata = [ordered]@{
    version      = $versionText
    downloadUrl  = $bundleUrl
    releaseNotes = $releaseNotes
}

$json = $metadata |
    ConvertTo-Json `
        -Depth 5

$utf8WithoutBom =
    New-Object System.Text.UTF8Encoding($false)

[System.IO.File]::WriteAllText(
    $metadataPath,
    $json,
    $utf8WithoutBom
)


# ------------------------------------------------------------
# Verify generated metadata
# ------------------------------------------------------------

$check = Get-Content `
    -Path $metadataPath `
    -Raw |
    ConvertFrom-Json


if ([Version]$check.version -ne $latest.Version)
{
    throw "Metadata version verification failed."
}

if ($check.downloadUrl -ne $bundleUrl)
{
    throw "Metadata URL verification failed."
}


Write-Host ""
Write-Host "Generated latest-version.json:" -ForegroundColor Cyan
Get-Content $metadataPath -Raw


# ------------------------------------------------------------
# Git publish
# ------------------------------------------------------------

Push-Location $repo

try
{
    $branch = (git branch --show-current).Trim()

    if ($LASTEXITCODE -ne 0)
    {
        throw "Could not determine Git branch."
    }

    if ($branch -ne "Dev")
    {
        throw "Current branch is '$branch'. Expected 'Dev'."
    }


    Write-Host ""
    Write-Host "Staging release..." -ForegroundColor Yellow

    git add -A

    if ($LASTEXITCODE -ne 0)
    {
        throw "git add failed."
    }


    $status = git status --porcelain
    $statusText = $status -join "`n"


    if ([string]::IsNullOrWhiteSpace($statusText))
    {
        Write-Host ""
        Write-Host "Everything is already committed." `
            -ForegroundColor Yellow

        Write-Host "Ensuring Dev is pushed..." `
            -ForegroundColor Yellow

        git push origin Dev

        if ($LASTEXITCODE -ne 0)
        {
            throw "git push failed."
        }
    }
    else
    {
        Write-Host ""
        Write-Host "Changes:" -ForegroundColor Cyan

        foreach ($line in $status)
        {
            Write-Host $line
        }


        $commitMessage =
            "Release EthStatistics $versionText"


        Write-Host ""
        Write-Host "Creating commit: $commitMessage" `
            -ForegroundColor Yellow

        git commit `
            -m $commitMessage

        if ($LASTEXITCODE -ne 0)
        {
            throw "git commit failed."
        }


        Write-Host ""
        Write-Host "Pushing to origin/Dev..." `
            -ForegroundColor Yellow

        git push origin Dev

        if ($LASTEXITCODE -ne 0)
        {
            throw "git push failed."
        }
    }
}
finally
{
    Pop-Location
}


# ------------------------------------------------------------
# Success
# ------------------------------------------------------------

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host " RELEASE COMPLETE" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Version: $versionText"
Write-Host "Bundle:  $bundleUrl"
Write-Host "Metadata: $baseUrl/latest-version.json"
Write-Host ""
Write-Host "Wait for GitHub Pages deployment." `
    -ForegroundColor Yellow
