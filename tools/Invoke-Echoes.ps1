# Runs the Unity batch tasks and the player smoke test for Echoes of the Valley.
# Run from anywhere: powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Invoke-Echoes.ps1 -Task <Task>
#   Compile   import and compile scripts only (fast check, exit 1 on compiler errors)
#   Setup     compile, configure the project and (re)generate prefabs, MainMenu and Valley (idempotent)
#   TestEdit  EditMode tests          TestPlay  PlayMode tests
#   Preview   render editor camera shots of both scenes to artifacts\logs\preview-shots (no Play Mode, no overlay UI)
#   Build     Windows x64 Mono development build into artifacts\build\win64
#   Smoke     start the built player with the scripted smoke run (-echoes-smoke) and report its exit code
# Logs, test results and screenshots go to the git-ignored artifacts\logs folder.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Compile', 'Setup', 'TestEdit', 'TestPlay', 'Preview', 'Build', 'Smoke')]
    [string] $Task,
    [string] $UnityExe = 'D:\Unity\Downloads\6000.3.27f1\Editor\Unity.exe',
    [string] $TempDirectory = 'D:\Unity\Temp\Echoes',
    [int] $SmokeTimeoutSeconds = 300
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'game'
$logDir = Join-Path $repoRoot 'artifacts\logs'
$buildDir = Join-Path $repoRoot 'artifacts\build\win64'
$player = Join-Path $buildDir 'EchoesOfTheValley.exe'

if (-not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) { throw "Unity editor not found: $UnityExe" }
New-Item -ItemType Directory -Force -Path $logDir, $TempDirectory | Out-Null
$env:TEMP = $TempDirectory
$env:TMP = $TempDirectory

function Invoke-Unity([string[]] $Arguments, [string] $Log) {
    $all = @('-batchmode', '-projectPath', ('"' + $project + '"')) + $Arguments + @('-logFile', ('"' + $Log + '"'))
    $process = Start-Process -FilePath $UnityExe -ArgumentList $all -Wait -PassThru -WindowStyle Hidden
    return $process.ExitCode
}

function Show-LogSummary([string] $Log) {
    if (-not (Test-Path -LiteralPath $Log)) { Write-Host "LOG_MISSING=$Log"; return }
    $hits = @(Select-String -LiteralPath $Log -Pattern 'error CS\d+', 'Exception:', 'Build completed with a result of .Failed', '\[Echoes\] BUILD' -ErrorAction SilentlyContinue)
    Write-Host ("LOG=$Log MATCHED_LINES=" + $hits.Count)
    $hits | Select-Object -First 15 | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
}

function Show-TestSummary([string] $Xml) {
    if (-not (Test-Path -LiteralPath $Xml)) { Write-Host "RESULTS_MISSING=$Xml"; return }
    [xml] $doc = Get-Content -LiteralPath $Xml -Raw
    $run = $doc.'test-run'
    Write-Host ("TESTS result=" + $run.result + " total=" + $run.total + " passed=" + $run.passed + " failed=" + $run.failed + " skipped=" + $run.skipped + " inconclusive=" + $run.inconclusive)
    foreach ($case in $doc.SelectNodes('//test-case[@result="Failed"]')) {
        Write-Host ('  FAILED ' + $case.fullname)
        Write-Host ('    ' + (($case.failure.message.InnerText) -replace '\s+', ' '))
    }
}

switch ($Task) {
    'Compile' {
        $log = Join-Path $logDir 'compile.log'
        $code = Invoke-Unity @('-quit') $log
        Write-Host "TASK=Compile UNITY_EXIT=$code"
        Show-LogSummary $log
        exit $code
    }
    'Setup' {
        $log = Join-Path $logDir 'setup.log'
        $code = Invoke-Unity @('-quit', '-executeMethod', 'Echoes.EditorTools.ProjectSetup.GenerateAll') $log
        Write-Host "TASK=Setup UNITY_EXIT=$code"
        Show-LogSummary $log
        exit $code
    }
    { $_ -in 'TestEdit', 'TestPlay' } {
        $platform = if ($Task -eq 'TestEdit') { 'EditMode' } else { 'PlayMode' }
        $log = Join-Path $logDir ("tests-$platform.log")
        $xml = Join-Path $logDir ("tests-$platform.xml")
        Remove-Item -LiteralPath $xml -ErrorAction SilentlyContinue
        $code = Invoke-Unity @('-runTests', '-testPlatform', $platform, '-testResults', ('"' + $xml + '"')) $log
        Write-Host "TASK=$Task UNITY_EXIT=$code"
        Show-LogSummary $log
        Show-TestSummary $xml
        exit $code
    }
    'Preview' {
        $log = Join-Path $logDir 'preview.log'
        $shots = Join-Path $logDir 'preview-shots'
        New-Item -ItemType Directory -Force -Path $shots | Out-Null
        $code = Invoke-Unity @('-quit', '-executeMethod', 'Echoes.EditorTools.PreviewTools.CaptureAll', '-echoesShots', ('"' + $shots + '"')) $log
        Write-Host ("TASK=Preview UNITY_EXIT=$code SHOTS=" + (@(Get-ChildItem -LiteralPath $shots -Filter *.png -ErrorAction SilentlyContinue).Count) + " in $shots")
        Show-LogSummary $log
        exit $code
    }
    'Build' {
        # Unity's Bee backend compiles with one .NET process per core by default. On this 16-core machine that
        # exhausted the commit limit ("Failed to create CoreCLR, HRESULT 0x8007000E") while WSL, VS Code and
        # browsers held most memory, so cap it.
        $env:BEE_BUILD_THREADS = '4'
        $os = Get-CimInstance Win32_OperatingSystem
        Write-Host ('MEMORY free commit {0:N1} GB, free physical {1:N1} GB, BEE_BUILD_THREADS={2}' -f ($os.FreeVirtualMemory / 1MB), ($os.FreePhysicalMemory / 1MB), $env:BEE_BUILD_THREADS)
        $log = Join-Path $logDir 'build.log'
        $code = Invoke-Unity @('-quit', '-executeMethod', 'Echoes.EditorTools.BuildTools.BuildWindows', '-echoesBuildPath', ('"' + $buildDir + '"')) $log
        Write-Host "TASK=Build UNITY_EXIT=$code PLAYER_EXISTS=$(Test-Path -LiteralPath $player)"
        Show-LogSummary $log
        exit $code
    }
    'Smoke' {
        if (-not (Test-Path -LiteralPath $player -PathType Leaf)) { throw "Player not built yet: $player" }
        $shots = Join-Path $logDir 'smoke-shots'
        $playerLog = Join-Path $logDir 'player-smoke.log'
        New-Item -ItemType Directory -Force -Path $shots | Out-Null
        Remove-Item -LiteralPath $playerLog -ErrorAction SilentlyContinue
        $saveDir = Join-Path $logDir 'smoke-save'
        $arguments = @('-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
            '-echoes-smoke', '-echoes-shots', ('"' + $shots + '"'), '-echoes-save-dir', ('"' + $saveDir + '"'),
            '-logFile', ('"' + $playerLog + '"'))
        $process = Start-Process -FilePath $player -ArgumentList $arguments -PassThru
        $exited = $process.WaitForExit($SmokeTimeoutSeconds * 1000)
        if ($exited) {
            $exitCode = $process.ExitCode
            Write-Host "TASK=Smoke PLAYER_EXIT=$exitCode (natural exit)"
        }
        else {
            $process.Kill()
            $exitCode = 124
            Write-Host "TASK=Smoke PLAYER_EXIT=TIMEOUT_KILLED after $SmokeTimeoutSeconds s (NOT a natural exit)"
        }
        if (Test-Path -LiteralPath $playerLog) {
            Select-String -LiteralPath $playerLog -Pattern '\[Echoes\]' | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
            $bad = @(Select-String -LiteralPath $playerLog -Pattern 'Exception|NullReference|error CS|Shader error' -CaseSensitive:$false)
            Write-Host ('PLAYER_LOG_EXCEPTION_LINES=' + $bad.Count)
            $bad | Select-Object -First 8 | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
        }
        else { Write-Host "PLAYER_LOG_MISSING=$playerLog" }
        Write-Host ('SCREENSHOTS=' + (@(Get-ChildItem -LiteralPath $shots -Filter *.png -ErrorAction SilentlyContinue).Count) + " in $shots")
        exit $exitCode
    }
}
