<#
.SYNOPSIS
    Builds ZeroHour.Sim and copies the assembly into the Unity project.

.DESCRIPTION
    The simulation is authored once in shared/ZeroHour.Sim and consumed as a compiled DLL by
    both the Unity client and the server. That is deliberate: if the client compiled its own
    copy from source it could drift from the server's, and a determinism guarantee that
    depends on two builds staying in step is not a guarantee at all.

    Run this after any change to shared/ZeroHour.Sim, then refresh Unity.

.PARAMETER Configuration
    Debug (default) or Release. Ship Release; Debug keeps assertions and better stack traces.

.PARAMETER SkipTests
    Skip the test run. Not recommended — the determinism guards live in that suite.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$repoRoot  = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$simProj   = Join-Path $repoRoot 'shared\ZeroHour.Sim\ZeroHour.Sim.csproj'
$pluginDir = Join-Path $repoRoot 'client\Assets\Plugins\ZeroHour.Sim'
$built     = Join-Path $repoRoot "shared\ZeroHour.Sim\bin\$Configuration\netstandard2.1\ZeroHour.Sim.dll"

Write-Host "Building ZeroHour.Sim ($Configuration)..." -ForegroundColor Cyan
dotnet build $simProj -c $Configuration --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "Sim build failed." }

if (-not $SkipTests) {
    Write-Host "Running determinism suite..." -ForegroundColor Cyan
    $testProj = Join-Path $repoRoot 'shared\ZeroHour.Sim.Tests\ZeroHour.Sim.Tests.csproj'
    dotnet test $testProj -c $Configuration --nologo -v quiet
    if ($LASTEXITCODE -ne 0) {
        # Refusing to publish a sim that fails its own guards is the entire point of the gate.
        throw "Tests failed. The DLL was NOT copied into Unity."
    }
}

if (-not (Test-Path $built)) { throw "Expected output missing: $built" }

New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-Item $built -Destination $pluginDir -Force

$pdb = [IO.Path]::ChangeExtension($built, '.pdb')
if ((Test-Path $pdb) -and $Configuration -eq 'Debug') {
    Copy-Item $pdb -Destination $pluginDir -Force
}

$size = [math]::Round((Get-Item (Join-Path $pluginDir 'ZeroHour.Sim.dll')).Length / 1KB, 1)
Write-Host "Copied ZeroHour.Sim.dll ($size KB) -> client\Assets\Plugins\ZeroHour.Sim\" -ForegroundColor Green
Write-Host "Refresh Unity (Ctrl+R) or run a 'refresh' Bridge command to pick it up." -ForegroundColor DarkGray
