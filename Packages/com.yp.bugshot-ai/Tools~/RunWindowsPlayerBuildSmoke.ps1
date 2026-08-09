param(
    [string]$UnityPath = $env:UNITY_EXE,
    [string]$ProjectPath = "",
    [string]$BuildOutputPath = ""
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

$Stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$LogDirectory = Join-Path $ProjectPath "Logs"
$LogPath = Join-Path $LogDirectory "BugShotAI_player_build_smoke.log"

if ([string]::IsNullOrWhiteSpace($BuildOutputPath)) {
    $BuildOutputPath = Join-Path $ProjectPath "Builds\BugShotAIPlayerSmoke\$Stamp\BugShotAIPlayerSmoke.exe"
}

New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $BuildOutputPath) | Out-Null

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
    "-executeMethod", "YP.BugShotAI.Tests.BugShotAIPlayerBuildSmoke.BuildWindows64",
    "-bugshotBuildOutput", $BuildOutputPath,
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

$Deadline = (Get-Date).AddSeconds(10)
while (-not (Test-Path $BuildOutputPath) -and (Get-Date) -lt $Deadline) {
    Start-Sleep -Milliseconds 250
}

if (-not (Test-Path $BuildOutputPath)) {
    Write-Error "Player build output was not created: $BuildOutputPath"
    exit 31
}

Write-Host "BugShot AI Windows Player Build smoke passed. Output=$BuildOutputPath Log=$LogPath"
exit 0
