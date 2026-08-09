<#
.SYNOPSIS
    Runs the ZeroHour.Sim test suite inside a Linux container.

.DESCRIPTION
    Works around Windows Smart App Control, which blocks the freshly built, unsigned
    ZeroHour.Sim.dll from loading on this machine. The symptom is every test in the suite
    failing at once with:

        System.IO.FileLoadException : Could not load file or assembly '...ZeroHour.Sim.dll'.
        An Application Control policy has blocked this file. (0x800711C7)

    That is an OS policy decision, not a code fault: the same commit passes here and in CI.
    Confirm with:

        Get-WinEvent -LogName Microsoft-Windows-CodeIntegrity/Operational -MaxEvents 20

    The permanent fix is to turn Smart App Control off in Windows Security under
    "App & browser control". Be aware that switching it off is irreversible without
    reinstalling Windows, which is why this script exists rather than a setup step that
    changes your machine.

    The repo is mounted read-only and the build happens on a copy inside the container, so
    running this never touches the working tree or your bin/obj folders.

.EXAMPLE
    pwsh tools/scripts/test-sim-docker.ps1
#>
[CmdletBinding()]
param(
    # Extra arguments passed through to dotnet test, e.g. --filter GateTests.
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $DotnetTestArgs
)

$ErrorActionPreference = 'Stop'

# Nested Join-Path rather than a single three-argument call: the -AdditionalChildPath
# parameter only exists in PowerShell 7+, and this has to work in Windows PowerShell 5.1.
$repoRoot = (Resolve-Path (Join-Path (Join-Path $PSScriptRoot '..') '..')).Path

$image = 'mcr.microsoft.com/dotnet/sdk:10.0'
$project = 'shared/ZeroHour.Sim.Tests/ZeroHour.Sim.Tests.csproj'
$passThrough = if ($DotnetTestArgs) { ' ' + ($DotnetTestArgs -join ' ') } else { '' }

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'docker was not found on PATH. Install Docker Desktop, or run: dotnet test ' + $project
}

Write-Host "Running the sim suite in $image (repo mounted read-only)..." -ForegroundColor Cyan

# Copy to /work first: building in place would need write access to the mount, and the point
# of the read-only mount is that a container run can never dirty the working tree.
$script = @"
set -e
mkdir -p /work
cp -r /src/shared /work/
find /work -type d \( -name obj -o -name bin \) -prune -exec rm -rf {} +
cd /work
dotnet test $project --nologo -v q$passThrough
"@

# Strip CR before handing the script to bash. A PowerShell here-string carries CRLF endings
# on Windows, and bash treats the trailing CR as part of each command, producing errors as
# opaque as: cannot create directory '/work/'$'\r'.
$script = $script -replace "`r`n", "`n"

docker run --rm -v "${repoRoot}:/src:ro" $image bash -c $script
exit $LASTEXITCODE
