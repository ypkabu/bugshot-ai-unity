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

$ResultDirectory = Join-Path $ProjectPath "TestResults"
$LogDirectory = Join-Path $ProjectPath "Logs"
$ResultPath = Join-Path $ResultDirectory "BugShotAI_EditMode.xml"
$LogPath = Join-Path $LogDirectory "BugShotAI_EditMode.log"

New-Item -ItemType Directory -Force -Path $ResultDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null

if (Test-Path $ResultPath) {
    Remove-Item -LiteralPath $ResultPath -Force
}

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

Wait-UnityProjectUnlock

$UnityArgs = @(
    "-batchmode",
    "-projectPath", $ProjectPath,
    "-executeMethod", "YP.BugShotAI.Tests.BugShotAICommandLineTestRunner.RunEditModeTests",
    "-bugshotTestResults", $ResultPath,
    "-logFile", $LogPath
)

$ArgumentLine = ($UnityArgs | ForEach-Object {
    if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
}) -join ' '

# PowerShell 7 does not always wait for GUI executables invoked with the call operator.
$UnityProcess = Start-Process -FilePath $UnityPath -ArgumentList $ArgumentLine -NoNewWindow -Wait -PassThru
$UnityExitCode = $UnityProcess.ExitCode

if ($UnityExitCode -ne 0) {
    Write-Error "Unity exited with code $UnityExitCode. Log: $LogPath"
    exit $UnityExitCode
}

Wait-UnityProjectUnlock

$Deadline = (Get-Date).AddSeconds(30)
while (-not (Test-Path $ResultPath) -and (Get-Date) -lt $Deadline) {
    Start-Sleep -Milliseconds 250
}

if (-not (Test-Path $ResultPath)) {
    Write-Error "Test result XML was not created: $ResultPath"
    exit 11
}

[xml]$Results = Get-Content -LiteralPath $ResultPath
$Run = $Results.'test-run'
$Failed = [int]$Run.failed
$Inconclusive = [int]$Run.inconclusive

if ($Failed -gt 0 -or $Inconclusive -gt 0) {
    Write-Error "EditMode tests did not pass. Failed=$Failed Inconclusive=$Inconclusive Result=$ResultPath"
    exit 12
}

Write-Host "BugShot AI EditMode tests passed. Total=$($Run.total) Passed=$($Run.passed) Failed=$($Run.failed) Result=$ResultPath"
exit 0
