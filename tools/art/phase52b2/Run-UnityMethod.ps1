[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Method,
    [Parameter(Mandatory=$true)][string]$LogFile,
    [string]$ProjectPath='',
    [string[]]$ExtraArguments=@()
)
# Runs one Unity Editor static method in batch mode (one Unity instance at a time; nothing under game/Assets may change meanwhile).
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
if(-not $ProjectPath){$ProjectPath=Join-Path $repo 'game'}
$log=[IO.Path]::GetFullPath($LogFile)
New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
$arguments=@('-batchmode','-quit','-projectPath',('"'+$ProjectPath+'"'),'-executeMethod',$Method,'-logFile',('"'+$log+'"'))+$ExtraArguments
$env:BEE_BUILD_THREADS='4'
$process=Start-Process -FilePath 'D:/Unity/Downloads/6000.3.27f1/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
Write-Output "UNITY_METHOD=$Method EXIT=$($process.ExitCode) LOG=$log"
exit $process.ExitCode
