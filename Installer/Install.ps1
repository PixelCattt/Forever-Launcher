#Requires -Version 5.1
param([switch]$ElevatedRun)
$ErrorActionPreference = 'Stop'

$installDirectory = Join-Path $PSScriptRoot 'Forever-Launcher'
$launcherPath = Join-Path $installDirectory 'Forever-Launcher.exe'
$certificatePath = Join-Path $PSScriptRoot 'WoW-Forever.crt'
$hostsConfigPath = Join-Path $PSScriptRoot 'Server.conf'
$expectedThumbprint = 'C6D19B1450F778CA82E857AF527439D06FAF4B28'

# 1. Check whether this process is elevated.
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    # 2. Request elevation, then let this unelevated process open the launcher.
    $powershell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $arguments = '-NoProfile -ExecutionPolicy Bypass -File "{0}" -ElevatedRun' -f $PSCommandPath
    try {
        $process = Start-Process -FilePath $powershell -ArgumentList $arguments -Verb RunAs -Wait -PassThru
        if ($process.ExitCode -ne 0) { exit $process.ExitCode }
        Start-Process -FilePath $launcherPath
        exit 0
    }
    catch {
        Write-Host 'Administrator access is required to install the certificate and update the hosts file.'
        exit 1
    }
}

# 3. Check the latest GitHub release before changing the system.
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$headers = @{ 'User-Agent' = 'Forever-Launcher-Installer' }
$release = Invoke-RestMethod -Uri 'https://api.github.com/repos/PixelCattt/Forever-Launcher/releases/latest' -Headers $headers
if ($release.tag_name -notmatch '^v\d+\.\d+\.\d+$') {
    throw "The latest release has an unexpected tag: $($release.tag_name)"
}
$asset = $release.assets | Where-Object { $_.name -eq 'Forever-Launcher.exe' } | Select-Object -First 1
if (-not $asset -or -not $asset.browser_download_url) {
    throw "Release $($release.tag_name) does not contain Forever-Launcher.exe."
}

# 4. Check Server.conf and read its single hosts mapping.
if (-not (Test-Path -LiteralPath $hostsConfigPath -PathType Leaf)) {
    throw "Server configuration file is missing: $hostsConfigPath"
}
$hostsConfigLines = @(Get-Content -LiteralPath $hostsConfigPath | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$mappingLines = @($hostsConfigLines | Where-Object { $_.Trim() -notmatch '^#' })
if ($mappingLines.Count -ne 1 -or $hostsConfigLines.Count -lt 2) {
    throw 'Server.conf must contain a comment and exactly one IP/hostname mapping.'
}
$mappingParts = @($mappingLines[0].Trim() -split '\s+')
if ($mappingParts.Count -ne 2) {
    throw 'Server.conf must contain one valid IPv4 address and hostname.'
}
$hostsIp = $mappingParts[0]
$hostsName = $mappingParts[1]
$parsedIp = $null
if (-not [System.Net.IPAddress]::TryParse($hostsIp, [ref]$parsedIp) -or $parsedIp.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork -or $hostsName -notmatch '^[A-Za-z0-9.-]+$') {
    throw 'Server.conf must contain one valid IPv4 address and hostname.'
}

# 5. Check that the bundled certificate is present and is the expected root CA.
if (-not (Test-Path -LiteralPath $certificatePath -PathType Leaf)) {
    throw "Certificate file is missing: $certificatePath"
}
$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
if ($certificate.Thumbprint -ne $expectedThumbprint) {
    throw 'The bundled certificate does not match the expected WoW-Forever root certificate.'
}

# 6. Download the launcher only after all pre-download checks succeed.
New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
$downloadPath = Join-Path $installDirectory 'Forever-Launcher.exe.download'
try {
    Invoke-WebRequest -Uri $asset.browser_download_url -Headers $headers -OutFile $downloadPath -UseBasicParsing
    if ((Get-Item -LiteralPath $downloadPath).Length -lt 100000) {
        throw 'The downloaded launcher file is unexpectedly small.'
    }
    Move-Item -LiteralPath $downloadPath -Destination $launcherPath -Force
}
finally {
    if (Test-Path -LiteralPath $downloadPath) {
        Remove-Item -LiteralPath $downloadPath -Force
    }
}

# 7. Append Server.conf only when its host mapping is not already present.
$hostsPath = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'
$hostsContent = [System.IO.File]::ReadAllText($hostsPath)
$mappingPattern = '^\s*' + [regex]::Escape($hostsIp) + '\s+' + [regex]::Escape($hostsName) + '(?:\s|$)'
$mappingExists = $hostsContent -split "`r`n|`n|`r" | Where-Object {
    $line = ($_ -split '#', 2)[0].Trim()
    $line -match $mappingPattern
} | Select-Object -First 1

if (-not $mappingExists) {
    $configText = ($hostsConfigLines -join "`r`n").Trim()
    if ($hostsContent -notmatch '(?:\r\n|\n|\r){2}$') {
        if ($hostsContent -notmatch '(?:\r\n|\n|\r)$') {
            $hostsContent += "`r`n"
        }
        $hostsContent += "`r`n"
    }
    $hostsContent += $configText + "`r`n"
    [System.IO.File]::WriteAllText($hostsPath, $hostsContent, [System.Text.UTF8Encoding]::new($false))
    & ipconfig.exe /flushdns | Out-Null
}
else {
    Write-Host "Hosts mapping $hostsIp $hostsName is already present; leaving the hosts file unchanged."
}
# 8. Install the verified certificate if it is not already trusted.
$rootStore = 'Cert:\LocalMachine\Root'
$installedCertificate = Get-ChildItem -Path $rootStore | Where-Object { $_.Thumbprint -eq $expectedThumbprint } | Select-Object -First 1
if (-not $installedCertificate) {
    Import-Certificate -FilePath $certificatePath -CertStoreLocation $rootStore | Out-Null
    Write-Host 'Installed the WoW-Forever root certificate.'
}
else {
    Write-Host 'The WoW-Forever root certificate is already installed.'
}

# 9. Open the launcher after setup.
Write-Host "Installed $($release.tag_name) to: $installDirectory"
Write-Host 'The launcher creates its default servers.json beside itself the first time it starts.'
if (-not $ElevatedRun) {
    Start-Process -FilePath $launcherPath
}
