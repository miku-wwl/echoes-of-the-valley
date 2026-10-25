# Initializes the official Unity Universal 3D template without GUI clicks.
# Does not author gameplay, fabricate scenes, build a game or overwrite an existing project.
# Run from the repository root: powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Init-Unity.ps1

[CmdletBinding()]
param(
    [string] $UnityExe = 'D:\Unity\Downloads\6000.3.27f1\Editor\Unity.exe',
    [string] $GameDirectory = '',
    [string] $TempDirectory = 'D:\Unity\Temp\EchoesInit'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($GameDirectory)) {
    $GameDirectory = Join-Path $repoRoot 'game'
}
$GameDirectory = [System.IO.Path]::GetFullPath($GameDirectory)
$logDirectory = Join-Path $repoRoot 'artifacts\bootstrap'

if (-not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
    throw "Unity editor not found: $UnityExe"
}
if (Test-Path -LiteralPath (Join-Path $GameDirectory 'ProjectSettings\ProjectVersion.txt')) {
    throw "Existing Unity project detected. Nothing modified: $GameDirectory"
}
if (Test-Path -LiteralPath $GameDirectory) {
    $first = Get-ChildItem -LiteralPath $GameDirectory -Force | Select-Object -First 1
    if ($null -ne $first) {
        throw "Destination already contains files; refusing to overwrite: $GameDirectory"
    }
}

$editorRoot = Split-Path -Parent (Split-Path -Parent $UnityExe)
$template = Join-Path $editorRoot 'Editor\Data\Resources\PackageManager\ProjectTemplates\com.unity.template.3d-cross-platform-17.0.14.tgz'
# $editorRoot is normally ...\6000.3.27f1, so the path above is its Editor/Data bundle.
if (-not (Test-Path -LiteralPath $template -PathType Leaf)) {
    throw "Verified Universal 3D template archive not found: $template. Locate the official Unity 6.3 template; do not substitute an unverified third-party project."
}

New-Item -ItemType Directory -Force -Path $logDirectory, $TempDirectory | Out-Null
$previousTemp = $env:TEMP
$previousTmp  = $env:TMP
$env:TEMP = $TempDirectory
$env:TMP  = $TempDirectory
$logPath = Join-Path $logDirectory 'unity-create-project.log'

try {
    $unityArguments = @(
        '-batchmode', '-quit',
        '-createProject', ('"' + $GameDirectory + '"'),
        '-cloneFromTemplate', ('"' + $template + '"'),
        '-buildTarget', 'Win64', '-force-d3d11',
        '-logFile', ('"' + $logPath + '"')
    )
    Write-Host "Creating Universal 3D project at $GameDirectory"
    $process = Start-Process -FilePath $UnityExe -ArgumentList $unityArguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        throw "Unity exited with $($process.ExitCode). See $logPath"
    }
    $versionFile = Join-Path $GameDirectory 'ProjectSettings\ProjectVersion.txt'
    $manifestFile = Join-Path $GameDirectory 'Packages\manifest.json'
    if (-not (Test-Path -LiteralPath $versionFile) -or -not (Test-Path -LiteralPath $manifestFile)) {
        throw "Unity returned success but the expected project structure is missing; inspect $logPath"
    }
    $echoesRoot = Join-Path $GameDirectory 'Assets\Echoes'
    foreach ($folder in @('Scenes', 'Scripts', 'Prefabs', 'Art', 'Audio', 'UI', 'Tests')) {
        New-Item -ItemType Directory -Force -Path (Join-Path $echoesRoot $folder) | Out-Null
    }
    Write-Host 'UNITY_PROJECT_INIT=PASS'
    Write-Host "UNITY_PROJECT_PATH=$GameDirectory"
    Write-Host "EDITOR_LOG=$logPath"
    Write-Host 'PHASE1_GAMEPLAY_STATUS=NOT_STARTED'
    Write-Host 'NEXT=Run tools\Invoke-Echoes.ps1 -Task Setup to generate the MainMenu and Valley scenes, then test, build and playtest.'
}
finally {
    $env:TEMP = $previousTemp
    $env:TMP  = $previousTmp
}
