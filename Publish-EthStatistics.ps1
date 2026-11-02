param(
    [switch]$Auto
)

$ErrorActionPreference = "Stop"

# ============================================================
# ETHSTATISTICS AUTOMATIC RELEASE PUBLISHER
# ============================================================

$repo = "C:\Users\zoisa\source\repos\EthStatistics"

$appPackages = Join-Path `
    $repo `
    "EthStatistics.Package\AppPackages"

$webDownloads = Join-Path `
    $repo `
    "EthStatisticsWeb\public\downloads"

$baseUrl =
    "https://microsoftfodev.github.io/EthStatistics/downloads"

$metadataPath = Join-Path `
    $webDownloads `
    "latest-version.json"


# ============================================================
# HEADER
# ============================================================

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "       EthStatistics Automatic Publisher" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""


# ============================================================
# VALIDATE DIRECTORIES
# ============================================================

if (-not (Test-Path $repo))
{
    throw "Repository not found: $repo"
}

if (-not (Test-Path $appPackages))
{
    throw "AppPackages directory not found: $appPackages"
}

if (-not (Test-Path $webDownloads))
{
    Write-Host "Creating website downloads directory..." -ForegroundColor Yellow

    New-Item `
        -ItemType Directory `
        -Path $webDownloads `
        -Force |
        Out-Null
}


# ============================================================
# FIND NEWEST MSIX BUNDLE
# ============================================================

Write-Host "Searching for newest published MSIX bundle..." -ForegroundColor Yellow

$bundle = Get-ChildItem `
    -Path $appPackages `
    -Filter "*.msixbundle" `
    -File `
    -Recurse |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1


if ($null -eq $bundle)
{
    if ($Auto)
    {
        Write-Host "No MSIX bundle found. Nothing to publish." -ForegroundColor Yellow
        exit 0
    }

    throw "No MSIX bundle was found under $appPackages"
}


Write-Host ""
Write-Host "Newest bundle:" -ForegroundColor Green
Write-Host $bundle.FullName
Write-Host ""


# ============================================================
# DETECT VERSION FROM BUNDLE NAME
# ============================================================

$versionMatch = [regex]::Match(
    $bundle.Name,
    '(\d+\.\d+\.\d+\.\d+)'
)


if (-not $versionMatch.Success)
{
    throw "Could not detect package version from $($bundle.Name)"
}


$versionText =
    $versionMatch.Groups[1].Value


try
{
    $newVersion =
        [Version]$versionText
}
catch
{
    throw "Invalid package version: $versionText"
}


Write-Host "Detected package version: $newVersion" -ForegroundColor Green
Write-Host ""


# ============================================================
# READ CURRENTLY PUBLISHED VERSION
# ============================================================

$currentVersion =
    [Version]"0.0.0.0"


if (Test-Path $metadataPath)
{
    try
    {
        $currentMetadata =
            Get-Content `
                -Path $metadataPath `
                -Raw |
            ConvertFrom-Json


        if (
            $null -ne $currentMetadata.version `
            -and `
            -not [string]::IsNullOrWhiteSpace(
                [string]$currentMetadata.version
            )
        )
        {
            $currentVersion =
                [Version]$currentMetadata.version
        }
    }
    catch
    {
        Write-Host `
            "Existing latest-version.json could not be read." `
            -ForegroundColor Yellow

        Write-Host `
            "It will be regenerated." `
            -ForegroundColor Yellow

        $currentVersion =
            [Version]"0.0.0.0"
    }
}


Write-Host "Currently published version: $currentVersion" -ForegroundColor Cyan
Write-Host ""


# ============================================================
# CHECK WHETHER THIS IS ACTUALLY A NEW RELEASE
# ============================================================

if ($newVersion -le $currentVersion)
{
    Write-Host "No new release detected." -ForegroundColor Green
    Write-Host ""
    Write-Host "Newest package:     $newVersion"
    Write-Host "Published version:  $currentVersion"
    Write-Host ""
    Write-Host "Nothing will be copied, committed or pushed." -ForegroundColor Green
    Write-Host ""

    exit 0
}


# ============================================================
# DETERMINE PACKAGE FOLDER
# ============================================================

$sourceVersionFolder =
    $bundle.Directory.FullName

$versionFolderName =
    Split-Path `
        -Path $sourceVersionFolder `
        -Leaf


Write-Host "Package folder: $versionFolderName" -ForegroundColor Cyan


# ============================================================
# SAFETY CHECK
# ============================================================

$escapedVersion =
    [regex]::Escape(
        $versionText
    )


if ($versionFolderName -notmatch $escapedVersion)
{
    throw @"
Package version mismatch.

Detected version:
$versionText

Bundle:
$($bundle.Name)

Folder:
$versionFolderName

Publishing stopped.
"@
}


# ============================================================
# AUTOMATIC RELEASE NOTES
# ============================================================

$releaseNotes =
    "EthStatistics version $versionText is now available."


# ============================================================
# COPY PACKAGE TO WEBSITE
# ============================================================

$destinationVersionFolder =
    Join-Path `
        $webDownloads `
        $versionFolderName


Write-Host ""
Write-Host "Copying package to website..." -ForegroundColor Yellow


if (Test-Path $destinationVersionFolder)
{
    Remove-Item `
        -Path $destinationVersionFolder `
        -Recurse `
        -Force
}


Copy-Item `
    -Path $sourceVersionFolder `
    -Destination $destinationVersionFolder `
    -Recurse `
    -Force


# ============================================================
# VERIFY COPIED BUNDLE EXISTS
# ============================================================

$destinationBundle =
    Join-Path `
        $destinationVersionFolder `
        $bundle.Name


if (-not (Test-Path $destinationBundle))
{
    throw "Copied bundle could not be found: $destinationBundle"
}


# ============================================================
# VERIFY PACKAGE HASH
# ============================================================

Write-Host "Verifying package SHA256..." -ForegroundColor Yellow


$sourceHash =
    (
        Get-FileHash `
            -Path $bundle.FullName `
            -Algorithm SHA256
    ).Hash


$destinationHash =
    (
        Get-FileHash `
            -Path $destinationBundle `
            -Algorithm SHA256
    ).Hash


if ($sourceHash -ne $destinationHash)
{
    throw @"
SHA256 verification failed.

Source:
$sourceHash

Destination:
$destinationHash
"@
}


Write-Host "Package integrity verified." -ForegroundColor Green


# ============================================================
# BUILD DOWNLOAD URL
# ============================================================

$bundleUrl =
    "$baseUrl/$versionFolderName/$($bundle.Name)"


Write-Host ""
Write-Host "Download URL:" -ForegroundColor Cyan
Write-Host $bundleUrl
Write-Host ""


# ============================================================
# GENERATE latest-version.json
# ============================================================

$metadata =
    [ordered]@{
        version      = $versionText
        downloadUrl  = $bundleUrl
        releaseNotes = $releaseNotes
    }


$json =
    $metadata |
    ConvertTo-Json `
        -Depth 5


$utf8WithoutBom =
    New-Object `
        System.Text.UTF8Encoding($false)


[System.IO.File]::WriteAllText(
    $metadataPath,
    $json,
    $utf8WithoutBom
)


# ============================================================
# VERIFY GENERATED JSON
# ============================================================

$verification =
    Get-Content `
        -Path $metadataPath `
        -Raw |
    ConvertFrom-Json


if (
    [Version]$verification.version `
    -ne `
    $newVersion
)
{
    throw "Generated metadata contains the wrong version."
}


if (
    $verification.downloadUrl `
    -ne `
    $bundleUrl
)
{
    throw "Generated metadata contains the wrong download URL."
}


Write-Host "Generated latest-version.json:" -ForegroundColor Cyan
Write-Host ""

Get-Content `
    -Path $metadataPath `
    -Raw

Write-Host ""
Write-Host ""


# ============================================================
# MOVE TO REPOSITORY
# ============================================================

Push-Location $repo


try
{
    # ========================================================
    # VERIFY GIT BRANCH
    # ========================================================

    $branch =
        (
            git branch --show-current
        ).Trim()


    if ($LASTEXITCODE -ne 0)
    {
        throw "Could nott branch."
    }


    if ($branch -ne "Dev")
    {
        throw @"
Automatic publishing stopped.

Current branch:
$branch

Expected:
Dev
"@
    }


    Write-Host "Git branch: $branch" -ForegroundColor Cyan


    # ========================================================
    # STAGE FILES
    # ========================================================

    Write-Host ""
    Write-Host "Staging release files..." -ForegroundColor Yellow


    git add -A


    if ($LASTEXITCODE -ne 0)
    {
        throw "git add failed."
    }


    # ========================================================
    # CHECK GIT STATUS
    # ========================================================

    $status =
        git status --porcelain


    $statusText =
        $status -join "`n"


    if ([string]::IsNullOrWhiteSpace($statusText))
    {
        Write-Host ""
        Write-Host "No Git changes detected." -ForegroundColor Yellow
        Write-Host ""

        exit 0
    }


    Write-Host ""
    Write-Host "Files included in release:" -ForegroundColor Cyan


    foreach ($line in $status)
    {
        Write-Host $line
    }


    # ========================================================
    # COMMIT
    # ========================================================

    $commitMessage =
        "Release EthStatistics $versionText"


    Write-Host ""
    Write-Host "Creating commit: $commitMessage" -ForegroundColor Yellow


    git commit `
        -m $commitMessage


    if ($LASTEXITCODE -ne 0)
    {
        throw "git commit failed."
    }


    # ========================================================
    # PUSH
    # ========================================================

    Write-Host ""
    Write-Host "Pushing release to origin/Dev..." -ForegroundColor Yellow


    git push origin Dev


    if ($LASTEXITCODE -ne 0)
    {
        throw "git push failed."
    }
}
finally
{
    Pop-Location
}


# ============================================================
# SUCCESS
# ============================================================

Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host "       RELEASE PUBLISHED SUCCESSFULLY" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
Write-Host ""

Write-Host "Version:" -ForegroundColor Cyan
Write-Host $versionText

Write-Host ""

Write-Host "Bundle:" -ForegroundColor Cyan
Write-Host $bundleUrl

Write-Host ""

Write-Host "Metadata:" -ForegroundColor Cyan
Write-Host "$baseUrl/latest-version.json"

Write-Host ""

Write-Host "Wait for GitHub Pages deployment before testing." -ForegroundColor Yellow
Write-Host ""