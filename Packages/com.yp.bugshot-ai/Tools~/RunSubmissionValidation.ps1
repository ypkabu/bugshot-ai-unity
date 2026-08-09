param(
    [string]$UnityPath = $env:UNITY_EXE,
    [string]$ProjectPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $DefaultUnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe"
    if (Test-Path $DefaultUnityPath) {
        $UnityPath = $DefaultUnityPath
    } else {
        $UnityCommand = Get-Command Unity.exe -ErrorAction SilentlyContinue
        if ($UnityCommand -eq $null) {
            throw "Unity.exe was not found. Pass -UnityPath or set UNITY_EXE."
        }

        $UnityPath = $UnityCommand.Source
    }
}

$LogDirectory = Join-Path $ProjectPath "Logs"
$RunAllResult = Join-Path $LogDirectory "BugShotAI_SubmissionValidation_RunAll.json"
$RunAllLog = Join-Path $LogDirectory "BugShotAI_SubmissionValidation_RunAll.log"
$Phase1Result = Join-Path $LogDirectory "BugShotAI_SubmissionValidation_PersistencePhase1.json"
$Phase1Log = Join-Path $LogDirectory "BugShotAI_SubmissionValidation_PersistencePhase1.log"
$Phase2Result = Join-Path $LogDirectory "BugShotAI_SubmissionValidation_PersistencePhase2.json"
$Phase2Log = Join-Path $LogDirectory "BugShotAI_SubmissionValidation_PersistencePhase2.log"
$StatePath = Join-Path $LogDirectory "BugShotAI_SubmissionValidation_persistence_state.json"

New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null

function Wait-UnityProjectUnlock {
    $LockPath = Join-Path $ProjectPath "Temp\UnityLockfile"
    $EscapedProjectPath = [regex]::Escape($ProjectPath)
    $EscapedProjectPathForward = [regex]::Escape(($ProjectPath -replace "\\", "/"))
    $Deadline = (Get-Date).AddSeconds(60)

    Start-Sleep -Seconds 2

    while ((Get-Date) -lt $Deadline) {
        $UnityForProject = Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -match $EscapedProjectPath -or $_.CommandLine -match $EscapedProjectPathForward }

        if (-not (Test-Path $LockPath) -and $UnityForProject -eq $null) {
            Start-Sleep -Seconds 5
            return
        }

        Start-Sleep -Seconds 1
    }
}

function Invoke-UnityValidation {
    param(
        [string]$Method,
        [string]$ResultPath,
        [string]$LogPath,
        [string[]]$ExtraArgs = @()
    )

    Wait-UnityProjectUnlock

    if (Test-Path $ResultPath) {
        Remove-Item -LiteralPath $ResultPath -Force
    }

    $UnityArgs = @(
        "-batchmode",
        "-projectPath", $ProjectPath,
        "-executeMethod", $Method,
        "-bugshotSubmissionResults", $ResultPath,
        "-logFile", $LogPath
    ) + $ExtraArgs

    $ArgumentLine = ($UnityArgs | ForEach-Object {
        if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
    }) -join ' '

    # PowerShell 7 does not always wait for GUI executables invoked with the call operator.
    $UnityProcess = Start-Process -FilePath $UnityPath -ArgumentList $ArgumentLine -NoNewWindow -Wait -PassThru
    $UnityExitCode = $UnityProcess.ExitCode

    if ($UnityExitCode -ne 0) {
        Write-Error "Unity exited with code $UnityExitCode for $Method. Log: $LogPath"
        exit $UnityExitCode
    }

    Wait-UnityProjectUnlock

    $Deadline = (Get-Date).AddSeconds(30)
    while (-not (Test-Path $ResultPath) -and (Get-Date) -lt $Deadline) {
        Start-Sleep -Milliseconds 250
    }

    if (-not (Test-Path $ResultPath)) {
        Write-Error "Submission validation result was not created for $Method`: $ResultPath"
        exit 21
    }

    $Json = Get-Content -Raw -LiteralPath $ResultPath | ConvertFrom-Json
    if ([int]$Json.failedCount -gt 0) {
        Write-Error "Submission validation failed for $Method. Failed=$($Json.failedCount) Result=$ResultPath Log=$LogPath"
        exit 22
    }

    Write-Host "BugShot AI submission validation passed: $Method Result=$ResultPath"
}

Invoke-UnityValidation `
    -Method "YP.BugShotAI.Tests.BugShotAISubmissionValidation.RunAll" `
    -ResultPath $RunAllResult `
    -LogPath $RunAllLog

Invoke-UnityValidation `
    -Method "YP.BugShotAI.Tests.BugShotAISubmissionValidation.PersistencePhase1" `
    -ResultPath $Phase1Result `
    -LogPath $Phase1Log `
    -ExtraArgs @("-bugshotSubmissionState", $StatePath)

Invoke-UnityValidation `
    -Method "YP.BugShotAI.Tests.BugShotAISubmissionValidation.PersistencePhase2" `
    -ResultPath $Phase2Result `
    -LogPath $Phase2Log `
    -ExtraArgs @("-bugshotSubmissionState", $StatePath)

Write-Host "BugShot AI submission validation passed. Results:"
Write-Host "  $RunAllResult"
Write-Host "  $Phase1Result"
Write-Host "  $Phase2Result"
exit 0
