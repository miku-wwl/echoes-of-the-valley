# Runs the Unity batch tasks, the player smoke test and the release packaging for Echoes of the Valley.
# Run from anywhere: powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Invoke-Echoes.ps1 -Task <Task>
#   Compile        import and compile scripts only (fast check, exit 1 on compiler errors)
#   Setup          compile, configure the project and (re)generate audio, prefabs, MainMenu and Valley (idempotent)
#   TestEdit       EditMode tests          TestPlay  PlayMode tests
#   Preview        render editor camera shots of both scenes to artifacts\logs\preview-shots (no Play Mode, no overlay UI)
#   DryRun         rehearse the smoke scenario in the Editor's Play Mode (batch mode) before spending a build; screenshots may be skipped there
#   Build          Windows x64 Mono DEVELOPMENT build into artifacts\build\win64
#   Smoke          start the development build with the scripted full-game smoke run (-echoes-smoke: New Game to the ending) and report its exit code
#   BuildRelease   Windows x64 Mono NON-development build into artifacts\build\win64-release (no Development Build watermark, no debug overlay)
#   Package        stage the release build with a player README (without Burst debug folders or .pdb files) and zip it to
#                  artifacts\release\<ReleaseName>.zip with a SHA-256; nothing under artifacts is ever committed
#   VerifyRelease  check the zip, extract it to a temporary folder, check the required files, scan for local paths, launch the EXTRACTED player
#                  cold and then through the full smoke run (-echoes-expect-release), and write artifacts\release\release-metadata.json
#   Release        BuildRelease, Package and VerifyRelease in one go
# Logs, test results and screenshots go to the git-ignored artifacts\logs folder.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Compile', 'Setup', 'TestEdit', 'TestPlay', 'Preview', 'DryRun', 'Build', 'Smoke', 'BuildRelease', 'Package', 'VerifyRelease', 'Release')]
    [string] $Task,
    [string] $UnityExe = 'D:\Unity\Downloads\6000.3.27f1\Editor\Unity.exe',
    [string] $TempDirectory = 'D:\Unity\Temp\Echoes',
    [int] $SmokeTimeoutSeconds = 480,   # the full-game run waits for Clover's eggs in real time; the player gives up itself after 420 s
    [string] $ReleaseName = 'EchoesOfTheValley-Windows-x64-RC1',
    [string] $BuildDirectory = '',
    [string] $LogDirectory = '',
    [string] $BuildVersion = '',
    [string] $ReleaseDirectory = '',
    [string] $PlayerReadmePath = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'game'
$logDir = Join-Path $repoRoot 'artifacts\logs'
$buildDir = Join-Path $repoRoot 'artifacts\build\win64'
$player = Join-Path $buildDir 'EchoesOfTheValley.exe'
$releaseBuildDir = Join-Path $repoRoot 'artifacts\build\win64-release'
if ($BuildDirectory) {
    $buildDir = [IO.Path]::GetFullPath($BuildDirectory)
    $releaseBuildDir = $buildDir
    $player = Join-Path $buildDir 'EchoesOfTheValley.exe'
}
if ($LogDirectory) { $logDir = [IO.Path]::GetFullPath($LogDirectory) }
$releaseDir = Join-Path $repoRoot 'artifacts\release'
if ($ReleaseDirectory) { $releaseDir = [IO.Path]::GetFullPath($ReleaseDirectory) }
$stageDir = Join-Path $releaseDir $ReleaseName
$zipPath = Join-Path $releaseDir ($ReleaseName + '.zip')
$playerReadme = Join-Path $PSScriptRoot 'release-readme.txt'
if ($PlayerReadmePath) { $playerReadme = [IO.Path]::GetFullPath($PlayerReadmePath) }

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

# A function's return value is everything it wrote to the pipeline; the exit code is always the last item.
function Get-Code($Value) { return [int] @($Value)[-1] }

function Get-BuildId([string] $Log) {
    if (-not (Test-Path -LiteralPath $Log)) { return '' }
    $hit = Select-String -LiteralPath $Log -Pattern '\[Echoes\] BUILD .* id=([0-9a-f]{32})' | Select-Object -Last 1
    if ($hit) { return $hit.Matches[0].Groups[1].Value }
    return ''
}

# Starts a built player with the scripted full-game run and waits for it. Returns the exit code, whether it ended on its own, the
# number of exception lines in its log, the screenshot count and any remote network connections seen while it ran.
function Invoke-PlayerSmoke([string] $PlayerExe, [string] $Shots, [string] $PlayerLog, [string] $SaveDir, [string[]] $ExtraArguments) {
    New-Item -ItemType Directory -Force -Path $Shots | Out-Null
    Remove-Item -LiteralPath $PlayerLog -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $Shots '*.png') -ErrorAction SilentlyContinue
    $arguments = @('-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
        '-echoes-smoke', '-echoes-shots', ('"' + $Shots + '"'), '-echoes-save-dir', ('"' + $SaveDir + '"'),
        '-logFile', ('"' + $PlayerLog + '"')) + $ExtraArguments
    $process = Start-Process -FilePath $PlayerExe -ArgumentList $arguments -WorkingDirectory (Split-Path -Parent $PlayerExe) -PassThru

    $remote = New-Object System.Collections.Generic.HashSet[string]
    $deadline = (Get-Date).AddSeconds($SmokeTimeoutSeconds)
    while (-not $process.HasExited -and (Get-Date) -lt $deadline) {
        try {
            foreach ($line in @(& netstat -ano -n 2>$null)) {
                $columns = $line.Trim() -split '\s+'
                if ($columns.Count -ge 4 -and $columns[$columns.Count - 1] -eq [string] $process.Id) {
                    $foreign = $columns[2]   # TCP: proto local foreign state pid; UDP: proto local foreign pid
                    if ($foreign -ne '*:*' -and $foreign -notmatch '^(0\.0\.0\.0|127\.|\[::\]|\[::1\])') { [void] $remote.Add($foreign) }
                }
            }
        }
        catch { }
        Start-Sleep -Seconds 5
    }

    if ($process.HasExited) {
        $exitCode = $process.ExitCode
        $natural = $true
        Write-Host "PLAYER_EXIT=$exitCode (natural exit)"
    }
    else {
        $process.Kill()
        $exitCode = 124
        $natural = $false
        Write-Host "PLAYER_EXIT=TIMEOUT_KILLED after $SmokeTimeoutSeconds s (NOT a natural exit)"
    }

    $exceptions = -1
    if (Test-Path -LiteralPath $PlayerLog) {
        Select-String -LiteralPath $PlayerLog -Pattern '\[Echoes\]' | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
        $bad = @(Select-String -LiteralPath $PlayerLog -Pattern 'Exception|NullReference|error CS|Shader error|LogError|LogException|Assertion failed' -CaseSensitive:$false)
        $exceptions = $bad.Count
        Write-Host ('PLAYER_LOG_EXCEPTION_LINES=' + $exceptions)
        $bad | Select-Object -First 8 | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
    }
    else { Write-Host "PLAYER_LOG_MISSING=$PlayerLog" }
    $shotCount = @(Get-ChildItem -LiteralPath $Shots -Filter *.png -ErrorAction SilentlyContinue).Count
    Write-Host ('SCREENSHOTS=' + $shotCount + " in $Shots")
    Write-Host ('REMOTE_NETWORK_CONNECTIONS_SEEN=' + $remote.Count + $(if ($remote.Count -gt 0) { ' (' + ($remote -join ', ') + ')' } else { '' }))
    return @{ ExitCode = $exitCode; Natural = $natural; Exceptions = $exceptions; Shots = $shotCount; Remote = @($remote) }
}

function Invoke-BuildRelease {
    $env:BEE_BUILD_THREADS = '4'
    $os = Get-CimInstance Win32_OperatingSystem
    Write-Host ('MEMORY free commit {0:N1} GB, free physical {1:N1} GB, BEE_BUILD_THREADS={2}' -f ($os.FreeVirtualMemory / 1MB), ($os.FreePhysicalMemory / 1MB), $env:BEE_BUILD_THREADS)
    if (Test-Path -LiteralPath $releaseBuildDir) { throw "Build output already exists; preserve it and choose a fresh -BuildDirectory: $releaseBuildDir" }
    $log = Join-Path $logDir 'build-release.log'
    $arguments = @('-quit', '-executeMethod', 'Echoes.EditorTools.BuildTools.BuildWindowsRelease', '-echoesBuildPath', ('"' + $releaseBuildDir + '"'))
    if ($BuildVersion) { $arguments += @('-echoesBuildVersion', ('"' + $BuildVersion + '"')) }
    $code = Invoke-Unity $arguments $log
    $exe = Join-Path $releaseBuildDir 'EchoesOfTheValley.exe'
    Write-Host "TASK=BuildRelease UNITY_EXIT=$code PLAYER_EXISTS=$(Test-Path -LiteralPath $exe) BUILD_ID=$(Get-BuildId $log)"
    Show-LogSummary $log
    return $code
}

# Copies the release build (minus anything that must not ship) into a named folder with the player README and zips that folder.
function New-ReleasePackage {
    $exe = Join-Path $releaseBuildDir 'EchoesOfTheValley.exe'
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Release build missing: run -Task BuildRelease first ($exe)" }
    if (-not (Test-Path -LiteralPath $playerReadme -PathType Leaf)) { throw "Player README missing: $playerReadme" }

    New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
    if ((Test-Path -LiteralPath $stageDir) -or (Test-Path -LiteralPath $zipPath)) { throw "Package output already exists; choose a fresh -ReleaseName or -ReleaseDirectory: $zipPath" }
    New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

    foreach ($item in Get-ChildItem -LiteralPath $releaseBuildDir -Force) {
        if ($item.Name -like '*_BurstDebugInformation_DoNotShip') { Write-Host ('EXCLUDED ' + $item.Name); continue }
        Copy-Item -LiteralPath $item.FullName -Destination $stageDir -Recurse -Force
    }
    foreach ($pdb in @(Get-ChildItem -LiteralPath $stageDir -Recurse -Force -Filter *.pdb)) { Write-Host ('EXCLUDED ' + $pdb.FullName.Substring($stageDir.Length)); Remove-Item -LiteralPath $pdb.FullName -Force }
    Copy-Item -LiteralPath $playerReadme -Destination (Join-Path $stageDir 'README.txt') -Force
    $scrubbed = Remove-BuildPathsFromAssemblies $stageDir $repoRoot
    Write-Host "BUILD_PATHS_SCRUBBED=$scrubbed"

    # Windows PowerShell 5.1's ZipFile.CreateFromDirectory writes entry names with backslashes, which the zip specification does not allow
    # (macOS, Linux and some tools then extract files with a backslash in their names). Write every entry with forward slashes instead.
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipStream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    $archive = New-Object System.IO.Compression.ZipArchive($zipStream, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in @(Get-ChildItem -LiteralPath $stageDir -Recurse -File -Force | Sort-Object FullName)) {
            $relative = $file.FullName.Substring($stageDir.Length).TrimStart('\').Replace('\', '/')
            $entry = $archive.CreateEntry($ReleaseName + '/' + $relative, [System.IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $file.LastWriteTime
            $source = [System.IO.File]::OpenRead($file.FullName)
            $target = $entry.Open()
            try { $source.CopyTo($target) }
            finally { $target.Dispose(); $source.Dispose() }
        }
    }
    finally { $archive.Dispose(); $zipStream.Dispose() }
    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    $files = @(Get-ChildItem -LiteralPath $stageDir -Recurse -File -Force)
    $unpacked = ($files | Measure-Object -Property Length -Sum).Sum
    Write-Host ("TASK=Package ZIP=$zipPath BYTES=" + (Get-Item -LiteralPath $zipPath).Length + " SHA256=$hash FILES=" + $files.Count + " UNPACKED_BYTES=$unpacked")
    return 0
}

# Fast byte search for the scan below (PowerShell is far too slow to search a hundred megabytes itself).
Add-Type -TypeDefinition @'
public static class EchoesByteSearch
{
    public static int IndexOf(byte[] haystack, byte[] needle, int from)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length) return -1;
        byte first = needle[0];
        int last = haystack.Length - needle.Length;
        for (int i = from; i <= last; i++)
        {
            if (haystack[i] != first) continue;
            int k = 1;
            while (k < needle.Length && haystack[i + k] == needle[k]) k++;
            if (k == needle.Length) return i;
        }
        return -1;
    }

    public static bool Contains(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length) return false;
        byte first = needle[0];
        int last = haystack.Length - needle.Length;
        for (int i = 0; i <= last; i++)
        {
            if (haystack[i] != first) continue;
            int k = 1;
            while (k < needle.Length && haystack[i + k] == needle[k]) k++;
            if (k == needle.Length) return true;
        }
        return false;
    }
}
'@

# A .NET assembly carries the path of the PDB it was built with (the CodeView "RSDS" record in its debug directory). Nothing reads it at run
# time, but it names the developer's folders, so the packaged copy keeps only the file name. Only paths under this repository are rewritten:
# the vendor files (UnityPlayer.dll, Microsoft's D3D12Core.dll, Unity's and Mono's assemblies) are code-signed and are never modified.
# The build output itself is not touched.
function Remove-BuildPathsFromAssemblies([string] $Directory, [string] $ProjectRoot) {
    $signature = [System.Text.Encoding]::ASCII.GetBytes('RSDS')
    $scrubbed = 0
    foreach ($dll in @(Get-ChildItem -LiteralPath $Directory -Filter *.dll -Recurse -File)) {
        $bytes = [System.IO.File]::ReadAllBytes($dll.FullName)
        $changed = $false
        $at = [EchoesByteSearch]::IndexOf($bytes, $signature, 0)
        while ($at -ge 0) {
            $start = $at + 24   # RSDS, a 16-byte GUID and a 4-byte age come first
            $end = $start
            while ($end -lt $bytes.Length -and $bytes[$end] -ne 0) { $end++ }
            if ($end -gt $start -and $end -lt $bytes.Length) {
                $old = [System.Text.Encoding]::UTF8.GetString($bytes, $start, $end - $start)
                if ($old.Replace('/', '\').StartsWith($ProjectRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $name = [System.IO.Path]::GetFileName($old)
                    $new = [System.Text.Encoding]::UTF8.GetBytes($name)
                    for ($i = $start; $i -lt $end; $i++) { $bytes[$i] = 0 }
                    [System.Array]::Copy($new, 0, $bytes, $start, [Math]::Min($new.Length, $end - $start))
                    $changed = $true
                    $scrubbed++
                    Write-Host ('SCRUBBED ' + $dll.Name + ': "' + $old + '" -> "' + $name + '"')
                }
            }
            $at = [EchoesByteSearch]::IndexOf($bytes, $signature, $at + 4)
        }
        if ($changed) { [System.IO.File]::WriteAllBytes($dll.FullName, $bytes) }
    }
    return $scrubbed
}

function Test-ReleasePackage {
    $problems = New-Object System.Collections.Generic.List[string]
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) { throw "Release zip missing: run -Task Package first ($zipPath)" }

    # 1. The zip itself: every entry opens and reads to its full size, names use forward slashes and sit under one folder
    $entryCount = 0
    $archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        foreach ($entry in $archive.Entries) {
            if ($entry.FullName.EndsWith('/')) { continue }
            $entryCount++
            if ($entry.FullName.Contains('\')) { $problems.Add('entry uses a backslash: ' + $entry.FullName) }
            if (-not $entry.FullName.StartsWith($ReleaseName + '/')) { $problems.Add('entry outside the release folder: ' + $entry.FullName) }
            $stream = $entry.Open()
            try {
                $buffer = New-Object byte[] 1048576
                $total = 0L
                while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) { $total += $read }
                if ($total -ne $entry.Length) { $problems.Add('entry size mismatch: ' + $entry.FullName) }
            }
            finally { $stream.Dispose() }
        }
    }
    finally { $archive.Dispose() }
    Write-Host "ZIP_ENTRIES_READ=$entryCount"

    # 2. Extract to a temporary folder and compare every file with the staged copy
    $verifyRoot = Join-Path $TempDirectory ('release-verify-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $verifyRoot)
    $extracted = Join-Path $verifyRoot $ReleaseName
    Write-Host "EXTRACTED_TO=$extracted"
    $staged = @(Get-ChildItem -LiteralPath $stageDir -Recurse -File -Force)
    $mismatches = 0
    foreach ($file in $staged) {
        $relative = $file.FullName.Substring($stageDir.Length).TrimStart('\')
        $copy = Join-Path $extracted $relative
        if (-not (Test-Path -LiteralPath $copy -PathType Leaf)) { $problems.Add('missing after extraction: ' + $relative); $mismatches++; continue }
        if ((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash) { $problems.Add('hash differs after extraction: ' + $relative); $mismatches++ }
    }
    Write-Host ('EXTRACTED_FILES=' + @(Get-ChildItem -LiteralPath $extracted -Recurse -File -Force).Count + " STAGED_FILES=$($staged.Count) MISMATCHES=$mismatches")

    # 3. What a player needs is there, and what must not ship is not
    $required = @('EchoesOfTheValley.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', 'README.txt',
        'EchoesOfTheValley_Data\globalgamemanagers', 'EchoesOfTheValley_Data\level0', 'EchoesOfTheValley_Data\level1',
        'EchoesOfTheValley_Data\Managed\Echoes.Runtime.dll', 'MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll')
    foreach ($relative in $required) { if (-not (Test-Path -LiteralPath (Join-Path $extracted $relative))) { $problems.Add('required file missing: ' + $relative) } }
    foreach ($bad in @(Get-ChildItem -LiteralPath $extracted -Recurse -Force | Where-Object { $_.Name -like '*.pdb' -or $_.Name -like '*DoNotShip*' -or $_.Name -like '*.log' })) { $problems.Add('must not ship: ' + $bad.FullName.Substring($extracted.Length)) }
    $exeCopy = Join-Path $extracted 'EchoesOfTheValley.exe'

    # 4. No local paths, user names or developer-machine strings inside the player
    $needles = @('D:\workshop', 'D:/workshop', 'weila', 'mingyue', 'echoes-of-the-valley', 'Unity\Downloads')
    $encodings = @([System.Text.Encoding]::ASCII, [System.Text.Encoding]::Unicode)
    $hits = 0
    foreach ($file in @(Get-ChildItem -LiteralPath $extracted -Recurse -File -Force)) {
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        foreach ($needle in $needles) {
            foreach ($encoding in $encodings) {
                if ([EchoesByteSearch]::Contains($bytes, $encoding.GetBytes($needle))) {
                    $problems.Add("local string '$needle' found in " + $file.FullName.Substring($extracted.Length)); $hits++
                }
            }
        }
    }
    Write-Host "LOCAL_STRING_HITS=$hits (scanned for: $($needles -join ', '))"

    # 5. The extracted player starts cold and idles without errors, then runs the whole game
    $coldLog = Join-Path $logDir 'player-release-cold.log'
    Remove-Item -LiteralPath $coldLog -ErrorAction SilentlyContinue
    $cold = Start-Process -FilePath $exeCopy -ArgumentList @('-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720', '-logFile', ('"' + $coldLog + '"')) -WorkingDirectory $extracted -PassThru
    Start-Sleep -Seconds 12
    $coldAlive = -not $cold.HasExited
    $coldForced = $false
    if ($coldAlive) { [void] $cold.CloseMainWindow(); Start-Sleep -Seconds 2; if (-not $cold.HasExited) { $cold.Kill(); $coldForced = $true } }
    $cold.WaitForExit()
    $coldExitCode = $cold.ExitCode
    $coldBad = if (Test-Path -LiteralPath $coldLog) { @(Select-String -LiteralPath $coldLog -Pattern 'Exception|NullReference|Shader error|LogError|LogException|Assertion failed' -CaseSensitive:$false).Count } else { -1 }
    $coldMenu = (Test-Path -LiteralPath $coldLog) -and (@(Select-String -LiteralPath $coldLog -Pattern 'MainMenu ready').Count -gt 0)
    Write-Host "COLD_START alive_after_12s=$coldAlive main_menu_ready=$coldMenu exception_lines=$coldBad exit_code=$coldExitCode forced=$coldForced"
    if (-not $coldAlive) { $problems.Add('the extracted player did not stay running for 12 s') }
    if (-not $coldMenu) { $problems.Add('the extracted player never reported the main menu ready') }
    if ($coldBad -ne 0) { $problems.Add("the cold start logged $coldBad error lines") }
    if ($coldForced) { $problems.Add('the cold-start player required forced termination') }
    if ($coldExitCode -ne 0) { $problems.Add("the cold-start player exited with code $coldExitCode") }

    # The scripted run is repeated (up to 3 times) only to tell a failing game from the intermittent Unity window-teardown fault seen once
    # in Phase 4 (a null read in UnityPlayer.dll after the run had logged PASS, process exit code 0xC000041D). Every attempt is recorded.
    $shots = Join-Path $logDir 'release-smoke-shots'
    $playerLog = Join-Path $logDir 'player-release-smoke.log'
    $saveDir = Join-Path $logDir 'release-smoke-save'
    $attempts = New-Object System.Collections.Generic.List[object]
    $smoke = $null
    $passLine = 0
    $buildLine = 0
    $endingLine = 0
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Write-Host "SMOKE_ATTEMPT $attempt of 3 on the extracted player"
        $smoke = Invoke-PlayerSmoke $exeCopy $shots $playerLog $saveDir @('-echoes-expect-release')
        $passLine = if (Test-Path -LiteralPath $playerLog) { @(Select-String -LiteralPath $playerLog -Pattern '\[Echoes\]\[SMOKE\] PASS').Count } else { 0 }
        $buildLine = if (Test-Path -LiteralPath $playerLog) { @(Select-String -LiteralPath $playerLog -Pattern 'build: release player').Count } else { 0 }
        $endingLine = if (Test-Path -LiteralPath $playerLog) { @(Select-String -LiteralPath $playerLog -Pattern 'section TheEnding').Count } else { 0 }
        $attempts.Add([pscustomobject]@{ attempt = $attempt; exitCode = $smoke.ExitCode; natural = $smoke.Natural; passLogged = ($passLine -eq 1); exceptionLines = $smoke.Exceptions; screenshots = $smoke.Shots })
        if ($smoke.ExitCode -eq 0 -and $smoke.Natural -and $passLine -eq 1 -and $smoke.Exceptions -eq 0) { break }
        Copy-Item -LiteralPath $playerLog -Destination (Join-Path $logDir ("player-release-smoke-attempt$attempt.log")) -Force -ErrorAction SilentlyContinue
    }
    $exitFaults = @($attempts | Where-Object { $_.passLogged -and $_.exitCode -ne 0 }).Count
    Write-Host ("SMOKE_ATTEMPTS=" + $attempts.Count + " EXIT_FAULTS_AFTER_PASS=$exitFaults")
    if ($smoke.ExitCode -ne 0 -or -not $smoke.Natural -or $passLine -ne 1) { $problems.Add('the full-game smoke run on the extracted player did not pass cleanly in 3 attempts') }
    if ($smoke.Exceptions -ne 0) { $problems.Add('the smoke run logged ' + $smoke.Exceptions + ' error lines') }
    if ($buildLine -ne 1) { $problems.Add('the player did not report itself as a release (non-development) build') }
    if ($endingLine -ne 1) { $problems.Add('the smoke run did not reach the ending') }
    if ($smoke.Remote.Count -gt 0) { $problems.Add('the player made remote network connections: ' + ($smoke.Remote -join ', ')) }
    Write-Host "SMOKE_ON_EXTRACTED_PLAYER pass_line=$passLine release_build_reported=$buildLine reached_ending=$endingLine"

    # 6. Metadata
    $files = @(Get-ChildItem -LiteralPath $extracted -Recurse -File -Force)
    $gitHead = (& git -C $repoRoot rev-parse HEAD 2>$null)
    $gitDirty = [bool] (& git -C $repoRoot status --porcelain 2>$null)
    $metadata = [ordered]@{
        name = $ReleaseName
        unity = '6000.3.27f1'
        buildId = (Get-BuildId (Join-Path $logDir 'build-release.log'))
        developmentBuild = $false
        createdUtc = (Get-Date).ToUniversalTime().ToString('o')
        gitHead = $gitHead
        gitWorkingTreeDirty = $gitDirty
        zip = $zipPath
        zipBytes = (Get-Item -LiteralPath $zipPath).Length
        zipSha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
        unpackedFiles = $files.Count
        unpackedBytes = ($files | Measure-Object -Property Length -Sum).Sum
        extractedTo = $extracted
        zipEntriesRead = $entryCount
        coldStartAlive = $coldAlive
        coldStartExitCode = $coldExitCode
        coldStartForcedTermination = $coldForced
        smokeAttempts = $attempts.ToArray()
        smokeExitFaultsAfterPass = $exitFaults
        smokeExitCode = $smoke.ExitCode
        smokeNaturalExit = $smoke.Natural
        smokeScreenshots = $smoke.Shots
        smokeExceptionLines = $smoke.Exceptions
        remoteConnectionsSeen = $smoke.Remote.Count
        localStringHits = $hits
        problems = @($problems)
    }
    $metadata | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $releaseDir 'release-metadata.json') -Encoding UTF8

    if ($problems.Count -eq 0) { Write-Host 'TASK=VerifyRelease RESULT=PASS'; return 0 }
    Write-Host ('TASK=VerifyRelease RESULT=FAIL problems=' + $problems.Count)
    $problems | ForEach-Object { Write-Host ('  PROBLEM ' + $_) }
    return 1
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
    'DryRun' {
        $log = Join-Path $logDir 'dryrun.log'
        $saveDir = Join-Path $logDir 'dryrun-save'
        $shots = Join-Path $logDir 'dryrun-shots'
        New-Item -ItemType Directory -Force -Path $shots | Out-Null
        Remove-Item -LiteralPath $log -ErrorAction SilentlyContinue
        $arguments = @('-executeMethod', 'Echoes.EditorTools.SmokeDryRun.Run', '-echoes-smoke',
            '-echoes-shots', ('"' + $shots + '"'), '-echoes-save-dir', ('"' + $saveDir + '"'))
        $all = @('-batchmode', '-projectPath', ('"' + $project + '"')) + $arguments + @('-logFile', ('"' + $log + '"'))
        $process = Start-Process -FilePath $UnityExe -ArgumentList $all -PassThru -WindowStyle Hidden
        $exited = $process.WaitForExit($SmokeTimeoutSeconds * 1000)
        if ($exited) { $code = $process.ExitCode; Write-Host "TASK=DryRun UNITY_EXIT=$code (natural exit)" }
        else { $process.Kill(); $code = 124; Write-Host "TASK=DryRun TIMEOUT_KILLED after $SmokeTimeoutSeconds s" }
        if (Test-Path -LiteralPath $log) {
            Select-String -LiteralPath $log -Pattern '\[Echoes\]\[SMOKE\]' | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
            $bad = @(Select-String -LiteralPath $log -Pattern 'Exception|NullReference|error CS' -CaseSensitive:$false)
            Write-Host ('LOG_EXCEPTION_LINES=' + $bad.Count)
            $bad | Select-Object -First 8 | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
        }
        else { Write-Host "LOG_MISSING=$log" }
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
        Write-Host "TASK=Build UNITY_EXIT=$code PLAYER_EXISTS=$(Test-Path -LiteralPath $player) BUILD_ID=$(Get-BuildId $log)"
        Show-LogSummary $log
        exit $code
    }
    'Smoke' {
        if (-not (Test-Path -LiteralPath $player -PathType Leaf)) { throw "Player not built yet: $player" }
        $result = Invoke-PlayerSmoke $player (Join-Path $logDir 'smoke-shots') (Join-Path $logDir 'player-smoke.log') (Join-Path $logDir 'smoke-save') @()
        Write-Host "TASK=Smoke PLAYER_EXIT=$($result.ExitCode)"
        exit $result.ExitCode
    }
    'BuildRelease' { exit (Get-Code (Invoke-BuildRelease)) }
    'Package' { exit (Get-Code (New-ReleasePackage)) }
    'VerifyRelease' { exit (Get-Code (Test-ReleasePackage)) }
    'Release' {
        $code = Get-Code (Invoke-BuildRelease)
        if ($code -ne 0) { exit $code }
        $code = Get-Code (New-ReleasePackage)
        if ($code -ne 0) { exit $code }
        exit (Get-Code (Test-ReleasePackage))
    }
}
