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