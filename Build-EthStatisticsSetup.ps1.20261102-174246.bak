param(
    [Parameter(Mandatory = $true)][string]$BundlePath,
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$DownloadsDirectory,
    [Parameter(Mandatory = $true)][string]$MetadataPath
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$expectedThumbprint = "0550DD920F9066E7A9DE580A3F2693EC635C7255"
$certificatePath = Join-Path $DownloadsDirectory "EthStatistics.cer"
$setupName = "EthStatistics-Setup-$Version.zip"
$setupPath = Join-Path $DownloadsDirectory $setupName
$tempDirectory = Join-Path $env:TEMP "EthStatistics-Setup-$Version"
$setupUrl = "https://microsoftfodev.github.io/EthStatistics/downloads/$setupName"
if (-not (Test-Path $BundlePath)) { throw "Bundle not found: $BundlePath" }
if (-not (Test-Path $certificatePath)) { throw "Certificate not found: $certificatePath" }
if (-not (Test-Path $MetadataPath)) { throw "Metadata not found: $MetadataPath" }
$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
if ($certificate.HasPrivateKey) { throw "The public CER unexpectedly contains a private key." }
if ($certificate.Thumbprint -ne $expectedThumbprint) { throw "The public CER thumbprint is incorrect." }
$signature = Get-AuthenticodeSignature -FilePath $BundlePath
if ($signature.Status -ne "Valid") { throw "Bundle signature is not valid: $($signature.Status)" }
if ($signature.SignerCertificate.Thumbprint -ne $expectedThumbprint) { throw "Bundle and public certificate do not match." }
Remove-Item $tempDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $setupPath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null
Copy-Item $certificatePath (Join-Path $tempDirectory "EthStatistics.cer") -Force
Copy-Item $BundlePath (Join-Path $tempDirectory (Split-Path $BundlePath -Leaf)) -Force
$installer = @'
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$expectedThumbprint = "0550DD920F9066E7A9DE580A3F2693EC635C7255"
$setupFolder = $PSScriptRoot
$certificatePath = Join-Path $setupFolder "EthStatistics.cer"
$bundle = Get-ChildItem -Path $setupFolder -File -Filter "*.msixbundle" | Select-Object -First 1
$isAdministrator = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdministrator) {
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    Start-Process powershell.exe -Verb RunAs -ArgumentList $arguments
    exit
}
if (-not (Test-Path $certificatePath)) { throw "EthStatistics.cer is missing." }
if ($null -eq $bundle) { throw "The EthStatistics MSIX bundle is missing." }
$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
if ($certificate.HasPrivateKey) { throw "Security check failed: CER contains a private key." }
if ($certificate.Thumbprint -ne $expectedThumbprint) { throw "Security check failed: certificate thumbprint does not match." }
$signature = Get-AuthenticodeSignature -FilePath $bundle.FullName
if ($signature.Status -ne "Valid") { throw "Package signature validation failed: $($signature.Status)" }
if ($signature.SignerCertificate.Thumbprint -ne $expectedThumbprint) { throw "Package and certificate do not match." }
$trusted = Get-ChildItem "Cert:\LocalMachine\TrustedPeople" | Where-Object Thumbprint -eq $expectedThumbprint | Select-Object -First 1
if ($null -eq $trusted) { Import-Certificate -FilePath $certificatePath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null }
$trusted = Get-ChildItem "Cert:\LocalMachine\TrustedPeople" | Where-Object Thumbprint -eq $expectedThumbprint | Select-Object -First 1
if ($null -eq $trusted) { throw "The certificate could not be added to Local Machine Trusted People." }
Write-Host "Certificate verified and trusted." -ForegroundColor Green
Write-Host "Opening Windows App Installer..." -ForegroundColor Cyan
Start-Process -FilePath $bundle.FullName
'@
[System.IO.File]::WriteAllText((Join-Path $tempDirectory "Install-EthStatistics.ps1"), $installer, [System.Text.UTF8Encoding]::new($false))
$readme = @"
ETHSTATISTICS $Version
=====================
1. Extract this ZIP file.
2. Right-click Install-EthStatistics.ps1 and select Run with PowerShell.
3. Accept the administrator prompt.
4. Leave Launch when ready selected in Windows App Installer.
5. Select Install.
"@
[System.IO.File]::WriteAllText((Join-Path $tempDirectory "README.txt"), $readme, [System.Text.UTF8Encoding]::new($false))
Compress-Archive -Path (Join-Path $tempDirectory "*") -DestinationPath $setupPath -CompressionLevel Optimal -Force
Remove-Item $tempDirectory -Recurse -Force
$metadata = Get-Content $MetadataPath -Raw | ConvertFrom-Json
$ordered = [ordered]@{ version=[string]$metadata.version; downloadUrl=[string]$metadata.downloadUrl; setupUrl=$setupUrl; releaseNotes=[string]$metadata.releaseNotes }
[System.IO.File]::WriteAllText($MetadataPath, ($ordered | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))
Write-Host "Setup ZIP created: $setupPath" -ForegroundColor Green
Write-Host "Setup URL: $setupUrl" -ForegroundColor Cyan
