[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$PlayerDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [int]$Width=1280,[int]$Height=720,
    [switch]$Baseline,[switch]$Video
)
$ErrorActionPreference='Stop'
$pilotPlayer=(Resolve-Path (Join-Path $PlayerDirectory 'EchoesOfTheValley.exe')).Path
$captureRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $captureRoot){throw "Preserve prior evidence; fresh output required: $captureRoot"}
New-Item -ItemType Directory -Path $captureRoot | Out-Null
$captureArgs=@('-echoes-pilot-tour','-echoes-tour-output',('"'+$captureRoot+'"'),'-echoes-save-dir',('"'+(Join-Path $captureRoot 'save')+'"'),'-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-logFile',('"'+(Join-Path $captureRoot 'player.log')+'"'))
if($Baseline){$captureArgs+='-echoes-pilot-baseline'}
if($Video){$captureArgs+='-echoes-pilot-record-frames'}
$capturePlayer=Start-Process -FilePath $pilotPlayer -ArgumentList $captureArgs -WorkingDirectory (Split-Path $pilotPlayer) -PassThru
$readyDeadline=(Get-Date).AddSeconds(70)
while(-not(Test-Path -LiteralPath (Join-Path $captureRoot 'ready.flag'))){
    if($capturePlayer.HasExited){throw "Player exited before route readiness: $($capturePlayer.ExitCode)"}
    if((Get-Date)-gt $readyDeadline){throw 'Player did not signal route readiness'}
    Start-Sleep -Milliseconds 200
}
if(-not $capturePlayer.WaitForExit(90000)){throw 'Player did not exit after the route and still captures'}
if($Video){
    & python (Join-Path $PSScriptRoot 'encode_player_frames.py') $captureRoot
    if($LASTEXITCODE-ne 0){throw 'Actual framebuffer recording validation failed'}
}
$exceptionLines=@(Select-String -LiteralPath (Join-Path $captureRoot 'player.log') -Pattern 'Exception|NullReference|error CS|LogError|Assertion failed').Count
$result=[ordered]@{exitCode=$capturePlayer.ExitCode;exceptionLines=$exceptionLines;baseline=$Baseline.IsPresent;width=$Width;height=$Height;video=$Video.IsPresent;forcedTermination=$false}
$result|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $captureRoot 'process-result.json') -Encoding UTF8
$result|ConvertTo-Json -Compress|Write-Output
if($capturePlayer.ExitCode-ne 0 -or $exceptionLines-ne 0){exit 1}
