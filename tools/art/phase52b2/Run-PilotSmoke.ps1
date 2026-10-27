[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$PlayerDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [int]$Width=1280,[int]$Height=720,
    [int]$TimeoutSeconds=300
)
# Scripted whole-game pass (New Game -> three restorations -> ending -> save / Continue) on the isolated pilot build, with actual screenshots.
# It teleports for quest set-up (it is NOT a walking test) and uses an isolated save folder. Automated, not a human playtest.
$ErrorActionPreference='Stop'
$exe=(Resolve-Path (Join-Path $PlayerDirectory 'EchoesOfTheValley.exe')).Path
$root=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $root){throw "Preserve prior evidence; fresh output required: $root"}
$shots=Join-Path $root 'screenshots';New-Item -ItemType Directory -Path $shots | Out-Null
$log=Join-Path $root 'player.log'
$arguments=@('-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-echoes-smoke','-echoes-shots',('"'+$shots+'"'),'-echoes-save-dir',('"'+(Join-Path $root 'save')+'"'),'-logFile',('"'+$log+'"'))
$started=Get-Date
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory (Split-Path $exe) -PassThru
$natural=$process.WaitForExit($TimeoutSeconds*1000)
if(-not $natural){$process.Kill()}
$seconds=[math]::Round(((Get-Date)-$started).TotalSeconds,1)
$exceptions=@(Select-String -LiteralPath $log -Pattern 'Exception|NullReference|error CS|Shader error|LogError|LogException|Assertion failed' -CaseSensitive:$false).Count
$passLine=@(Select-String -LiteralPath $log -Pattern 'SMOKE.*PASS|\[Echoes\].*PASS' | Select-Object -First 3 | ForEach-Object {$_.Line.Trim()})
$result=[ordered]@{width=$Width;height=$Height;naturalExit=$natural;exitCode=$(if($natural){$process.ExitCode}else{124});seconds=$seconds;screenshots=@(Get-ChildItem $shots -Filter *.png).Count;exceptionLines=$exceptions;passLines=$passLine;automated=$true;teleportsForSetup=$true}
$result|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $root 'process-result.json') -Encoding UTF8
$result|ConvertTo-Json -Compress|Write-Output
if(-not $natural -or $process.ExitCode -ne 0 -or $exceptions -ne 0){exit 1}
