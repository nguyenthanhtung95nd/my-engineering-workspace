#!/usr/bin/env pwsh
# Human-in-the-loop reproduction loop.
# Copy this file, edit the steps below, and run it.
# The agent runs the script; the user follows prompts in their terminal.
#
# Usage:
#   pwsh hitl-loop.template.ps1
#
# Two helpers:
#   Step "<instruction>"    → show instruction, wait for Enter
#   Capture "<question>"    → show question, return the user's response
#
# At the end, captured values are printed as KEY=VALUE for the agent to parse.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Step([string]$instruction) {
    Write-Host ""
    Write-Host ">>> $instruction"
    Read-Host "    [Enter when done]" | Out-Null
}

function Capture([string]$question) {
    Write-Host ""
    Write-Host ">>> $question"
    return Read-Host "    > "
}

# --- edit below ---------------------------------------------------------

Step "Navigate to the feature you want to reproduce."

$errored = Capture "Did it throw an error? (y/n)"

$errorMsg = Capture "Paste the error message (or 'none'):"

# --- edit above ---------------------------------------------------------

Write-Host ""
Write-Host "--- Captured ---"
Write-Host "ERRORED=$errored"
Write-Host "ERROR_MSG=$errorMsg"
