#requires -Version 7.3
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
# Preserve the Phase 3 entry point's whole-solution behavior as new services gain tests.
& "$PSScriptRoot/Test-All.ps1"
