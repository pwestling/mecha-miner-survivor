#!/usr/bin/env pwsh
#
# Windows workflow wrapper. The single entry point for every repository verb.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface
#            AGENTS.md § Standard workflow surface ("Do not create competing
#              workflow entrypoints.")
# Requirements: TR-BLD-005, TR-BLD-001, TR-BLD-002
# Verification: tests/verification/FND-002.json (VER-FND-002-008 proves parity)
#
# This file is deliberately a launcher and nothing else, and it is the exact
# counterpart of build.sh. Doc 100 requires both wrappers to expose "identical verbs
# and argument names"; that is achieved by neither wrapper knowing any verb name.
# The verb table, argument names, exit classes, and structured artifacts all live in
# src/MechaMiner.Tools, so there is no second copy of workflow logic to drift.
#
# Exit classes, from doc 100 § Standard command surface (there is no class 1):
#   0 success                     4 validation/test failure
#   2 invalid verb/arguments      5 build/import/export/package failure
#   3 missing/mismatched pinned   6 performance/budget failure
#     environment                 7 authorization/credential/external state
#                                 8 unexpected tool-internal failure
#
# The launcher itself can produce only two of those: 3 when the pinned .NET SDK is
# not usable - absent from PATH, or present but pinned by global.json to a version no
# installed SDK satisfies - and 8 when the verb host does not build. Everything else
# is the owning tool's class, returned unchanged.
#
# Those two cases are deliberately separated below, identically to build.sh. "dotnet
# is on PATH" is not the same claim as "the pinned SDK resolves", and doc 100 defines
# class 3 as a "missing or mismatched pinned environment", so a mismatched pin is
# class 3 and never class 8.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$hostProject = Join-Path $repoRoot 'src/MechaMiner.Tools/MechaMiner.Tools.csproj'
$hostConfiguration = 'Debug'
$hostAssembly = Join-Path $repoRoot "src/MechaMiner.Tools/bin/$hostConfiguration/net8.0/MechaMiner.Tools.dll"
$launcherLogDir = Join-Path $repoRoot 'artifacts/wrapper'

$exitEnvironment = 3
$exitInternal = 8

# Noninteractive and locale-stable for every child tool.
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_UI_LANGUAGE = 'en-US'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  $instructions = @'
FAILED [MMT-3001] the pinned .NET SDK is not on PATH, so the verb host cannot run.
Exit class 3 (missing or mismatched pinned environment).

A .NET process cannot bootstrap .NET. Install the version global.json pins first,
from https://dot.net, into the default location, then run:

    ./build.ps1 doctor

On Linux and macOS the repository ships an idempotent installer for the same pins:

    sudo build/bootstrap-linux.sh
'@
  [Console]::Error.WriteLine($instructions)
  exit $exitEnvironment
}

# MISMATCH-PROBE-BEGIN
#
# The exact counterpart of build.sh's probe, and the same classification. dotnet being
# discoverable does not mean the pinned SDK is usable: global.json pins an exact
# version, hostfxr resolves that pin from the repository root, and a pin no installed
# SDK satisfies fails every SDK command including the verb host's own build. Reporting
# that as class 8 would blame the repository for an operator's environment.
#
# `dotnet --version` is run from the repository root because that is the directory
# whose global.json governs resolution. Its stderr is captured rather than allowed to
# terminate: this script sets $ErrorActionPreference to 'Stop', and PowerShell 7.3+
# can turn a native command's nonzero exit into a terminating error, which would skip
# the classification below and surface an unclassified failure instead.
$sdkResolution = ''
$sdkResolutionExit = 0
try {
  Push-Location $repoRoot
  $ErrorActionPreference = 'Continue'
  $sdkResolution = (& dotnet --version 2>&1 | Out-String).TrimEnd()
  $sdkResolutionExit = $LASTEXITCODE
}
finally {
  $ErrorActionPreference = 'Stop'
  Pop-Location
}

if ($sdkResolutionExit -ne 0) {
  [Console]::Error.WriteLine('FAILED [MMT-3001] global.json pins a .NET SDK version that is not installed here.')
  [Console]::Error.WriteLine('        Exit class 3 (missing or mismatched pinned environment).')
  [Console]::Error.WriteLine('        dotnet is discoverable, so this is a version mismatch and not an absent SDK.')
  [Console]::Error.WriteLine('        It is not a repository fault: nothing in src/ is broken.')
  [Console]::Error.WriteLine('')
  [Console]::Error.WriteLine('        Resolution attempt (dotnet --version, from the repository root):')
  foreach ($line in ($sdkResolution -split "`r?`n")) {
    [Console]::Error.WriteLine('          ' + $line)
  }
  [Console]::Error.WriteLine('')
  [Console]::Error.WriteLine('        Install the version global.json pins, from https://dot.net, into the')
  [Console]::Error.WriteLine('        default location, or correct the pin in global.json. Do not silently')
  [Console]::Error.WriteLine('        widen rollForward to make an unpinned SDK resolve: doc 100')
  [Console]::Error.WriteLine('        § Toolchain pinning requires the exact SDK to be pinned.')
  exit $exitEnvironment
}
# MISMATCH-PROBE-END

New-Item -ItemType Directory -Force -Path $launcherLogDir | Out-Null
$hostBuildLog = Join-Path $launcherLogDir 'host-build.log'

# Build the verb host before dispatching. `dotnet run` is deliberately not used: it
# returns 1 when the build fails, and 1 is not a class doc 100 defines.
& dotnet build $hostProject --configuration $hostConfiguration --nologo --verbosity quiet *> $hostBuildLog
if ($LASTEXITCODE -ne 0) {
  [Console]::Error.WriteLine('FAILED [MMT-8001] the verb host in src/MechaMiner.Tools did not build.')
  [Console]::Error.WriteLine('        Exit class 8 (unexpected tool-internal failure).')
  [Console]::Error.WriteLine('        Build log: artifacts/wrapper/host-build.log')
  if (Test-Path $hostBuildLog) {
    Get-Content -Tail 40 $hostBuildLog | ForEach-Object { [Console]::Error.WriteLine($_) }
  }
  exit $exitInternal
}

if (-not (Test-Path $hostAssembly)) {
  [Console]::Error.WriteLine("FAILED [MMT-8001] the verb host built but $hostAssembly is missing.")
  [Console]::Error.WriteLine('        Exit class 8 (unexpected tool-internal failure).')
  exit $exitInternal
}

# The repository root is passed explicitly so the host never searches upward for a
# marker file (TR-BLD-006). The host's exit class becomes this script's.
& dotnet $hostAssembly $repoRoot @args
exit $LASTEXITCODE
