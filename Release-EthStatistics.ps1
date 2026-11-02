param(
    [ValidateSet("Revision", "Build", "Minor", "Major")]
    [string]$Increment = "Build",

    [switch]$NoPush
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# ============================================================
# CONFIGURATION
# ============================================================

$repo = "C:\Users\zoisa\source\repos\EthStatistics"

$packagingProject = Join-Path `
    $repo `
    "EthStatistics.Package\EthStatistics.Package.wapproj"

$manifestPath = Join-Path `
    $repo `
    "EthStatistics.Package\Package.appxmanifest"

$appPackagesPath = Join-Path `
    $repo `
    "EthStatistics.Package\AppPackages"

$publisherScript = Join-Path `
    $repo `
    "Publish-EthStatistics.ps1"

$certificateThumbprint =
    "0550DD920F9066E7A9DE580A3F2693EC635C7255"


# ============================================================
# FUNCTIONS
# ============================================================

function Write-Section
{
    param([string]$Text)

    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host " $Text" -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
}


function Get-MSBuildPath
{
    $vswhere = Join-Path `
        ${env:ProgramFiles(x86)} `
        "Microsoft Visual Studio\Installer\vswhere.exe"

    if (-not (Test-Path $vswhere))
    {
        throw "vswhere.exe was not found."
    }

    $installationPath = & $vswhere `
        -latest `
        -products * `
        -requires Microsoft.Component.MSBuild `
        -property installationPath

    if ([string]::IsNullOrWhiteSpace($installationPath))
    {
        throw "Visual Studio with MSBuild was not found."
    }

    $paths = @(
        (Join-Path $installationPath "MSBuild\Current\Bin\amd64\MSBuild.exe"),
        (Join-Path $installationPath "MSBuild\Current\Bin\MSBuild.exe")
    )

    foreach ($candidate in $paths)
    {
        if (Test-Path $candidate)
        {
            return $candidate
        }
    }

    throw "MSBuild.exe was not found."
}


function Get-ManifestVersion
{
    param([string]$Path)

    [xml]$xml = Get-Content `
        -Path $Path `
        -Raw

    return [Version]$xml.Package.Identity.Version
}


function Set-ManifestVersion
{
    param(
        [string]$Path,
        [Version]$Version
    )

    [xml]$xml = Get-Content `
        -Path $Path `
        -Raw

    $xml.Package.Identity.Version =
        $Version.ToString()

    $settings =
        New-Object System.Xml.XmlWriterSettings

    $settings.Indent = $true

    $settings.Encoding =
        New-Object System.Text.UTF8Encoding($false)

    $writer =
        [System.Xml.XmlWriter]::Create(
            $Path,
            $settings
        )

    try
    {
        $xml.Save($writer)
    }
    finally
    {
        $writer.Dispose()
    }
}


function Get-NextVersion
{
    param(
        [Version]$Current,
        [string]$Part
    )

    $major = $Current.Major
    $minor = $Current.Minor
    $build = $Current.Build
    $revision = $Current.Revision

    switch ($Part)
    {
        "Major"
        {
            $major++
            $minor = 0
            $build = 0
            $revision = 0
        }

        "Minor"
        {
            $minor++
            $build = 0
            $revision = 0
        }

        "Build"
        {
            $build++
            $revision = 0
        }

        "Revision"
        {
            $revision++
        }
    }

    return New-Object Version(
        $major,
        $minor,
        $build,
        $revision
    )
}


# ============================================================
# VALIDATION
# ============================================================

Write-Section "ETHSTATISTICS RELEASE"

foreach ($required in @(
    $packagingProject,
    $manifestPath,
    $publisherScript
))
{
    if (-not (Test-Path $required))
    {
        throw "Required file not found: $required"
    }
}


# ============================================================
# CHECK GIT BRANCH
# ============================================================

Push-Location $repo

try
{
    $branch =
        (git branch --show-current).Trim()

    if ($branch -ne "Dev")
    {
        throw "Release must be performed from Dev. Current branch: $branch"
    }
}
finally
{
    Pop-Location
}


Write-Host "Branch: Dev" -ForegroundColor Green


# ============================================================
# FIND MSBUILD
# ============================================================

$msbuild =
    Get-MSBuildPath

Write-Host "MSBuild: $msbuild" -ForegroundColor Green


# ============================================================
# VERSION
# ============================================================

$currentVersion =
    Get-ManifestVersion `
        -Path $manifestPath

$newVersion =
    Get-NextVersion `
        -Current $currentVersion `
        -Part $Increment


Write-Host ""
Write-Host "Current version: $currentVersion"
Write-Host "New version:     $newVersion" -ForegroundColor Green


# ============================================================
# VERIFY CERTIFICATE
# ============================================================

$certificate =
    Get-ChildItem `
        "Cert:\CurrentUser\My\$certificateThumbprint" `
        -ErrorAction SilentlyContinue

if ($null -eq $certificate)
{
    $certificate =
        Get-ChildItem `
            "Cert:\LocalMachine\My\$certificateThumbprint" `
            -ErrorAction SilentlyContinue
}

if ($null -eq $certificate)
{
    throw "Signing certificate was not found."
}

if (-not $certificate.HasPrivateKey)
{
    throw "Signing certificate has no accessible private key."
}

if ($certificate.NotAfter -le (Get-Date))
{
    throw "Signing certificate has expired."
}


Write-Host "Signing certificate: OK" -ForegroundColor Green


# ============================================================
# UPDATE MANIFEST VERSION
# ============================================================

Set-ManifestVersion `
    -Path $manifestPath `
    -Version $newVersion

$verifyVersion =
    Get-ManifestVersion `
        -Path $manifestPath

if ($verifyVersion -ne $newVersion)
{
    throw "Manifest version update failed."
}


Write-Host "Manifest version updated." -ForegroundColor Green


# ============================================================
# BUILD PACKAGE
# ============================================================

Write-Section "BUILDING MSIX"

& $msbuild `
    $packagingProject `
    /restore `
    /t:Build `
    /m `
    /nologo `
    /verbosity:minimal `
    /p:Configuration=Release `
    /p:Platform=x64 `
    /p:AppxBundle=Always `
    /p:AppxBundlePlatforms=x64 `
    /p:UapAppxPackageBuildMode=SideloadOnly `
    /p:AppxPackageSigningEnabled=true `
    /p:AppxAutoIncrementPackageRevision=false `
    /p:GenerateAppInstallerFile=false `
    /p:SkipEthStatisticsAutoPublish=true


if ($LASTEXITCODE -ne 0)
{
    throw "MSIX build failed."
}


# ============================================================
# LOCATE NEW BUNDLE
# ============================================================

$bundle = Get-ChildItem `
    -Path $appPackagesPath `
    -Recurse `
    -File `
    -Filter "*.msixbundle" |
ForEach-Object {

    if ($_.Name -match '(\d+\.\d+\.\d+\.\d+)')
    {
        [PSCustomObject]@{
            File =
                $_

            Version =
                [Version]$Matches[1]
        }
    }

} |
Where-Object {
    $_.Version -eq $newVersion
} |
Sort-Object {
    $_.File.LastWriteTimeUtc
} -Descending |
Select-Object -First 1


if ($null -eq $bundle)
{
    throw "Bundle $newVersion was not generated."
}


Write-Host ""
Write-Host "Bundle created:" -ForegroundColor Green
Write-Host $bundle.File.FullName


# ============================================================
# VERIFY SIGNATURE
# ============================================================

$signature =
    Get-AuthenticodeSignature `
        -FilePath $bundle.File.FullName


if ($signature.Status -ne "Valid")
{
    throw "Generated bundle signature is not valid: $($signature.Status)"
}


if (
    $signature.SignerCertificate.Thumbprint `
    -ne `
    $certificateThumbprint
)
{
    throw "Generated bundle uses the wrong signing certificate."
}


Write-Host "Bundle signature verified." -ForegroundColor Green


# ============================================================
# PUBLISH WEBSITE
# ============================================================

if (-not $NoPush)
{
    Write-Section "PUBLISHING"

    powershell.exe `
        -NoProfile `
        -ExecutionPolicy Bypass `
        -File $publisherScript `
        -Auto

    if ($LASTEXITCODE -ne 0)
    {
        throw "Publish-EthStatistics.ps1 failed."
    }
}


# ============================================================
# COMPLETE
# ============================================================

Write-Section "RELEASE COMPLETE"

Write-Host "Version: $newVersion" -ForegroundColor Green

Write-Host ""
Write-Host "Bundle:"
Write-Host $bundle.File.FullName

if ($NoPush)
{
    Write-Host ""
    Write-Host "GitHub publishing was skipped." -ForegroundColor Yellow
}
else
{
    Write-Host ""
    Write-Host "Release pushed to Dev." -ForegroundColor Green
    Write-Host "Wait for GitHub Pages deployment." -ForegroundColor Yellow
}

Write-Host ""
