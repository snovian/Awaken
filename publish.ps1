# Portable release: self-contained single-file exe, no runtime needed on target.
# Usage: ./publish.ps1 [-Rid win-x64|win-arm64]
param(
    [string]$Rid = "win-x64",
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"

# Prefer user-local SDK (installed without admin), fall back to dotnet on PATH.
$userDotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
$dotnet = if (Test-Path $userDotnet) { $userDotnet } else { "dotnet" }

$output = Join-Path $PSScriptRoot "publish\$Rid"
& $dotnet publish (Join-Path $PSScriptRoot "src\Awaken\Awaken.csproj") `
    -c $Configuration -r $Rid --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $output

if ($LASTEXITCODE -ne 0) {
    throw "publish failed"
}

$exe = Join-Path $output "Awaken.exe"
$size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "OK: $exe ($size MB)"
