[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidateSet('Generate','Build')][string]$Task,
    [string]$BuildDirectory='',
    [string]$LogDirectory='artifacts/visual-qa/phase-5.2/logs/pilot'
)
$ErrorActionPreference='Stop'
$pilotRepo=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$pilotLogs=[IO.Path]::GetFullPath($LogDirectory)
New-Item -ItemType Directory -Force -Path $pilotLogs | Out-Null
$pilotMethod=if($Task -eq 'Generate'){'Echoes.EditorTools.Phase52PilotBuilder.Generate'}else{'Echoes.EditorTools.Phase52PilotBuilder.BuildWindowsPilot'}
$pilotArgs=@('-batchmode','-quit','-projectPath',('"'+(Join-Path $pilotRepo 'game')+'"'),'-executeMethod',$pilotMethod,'-logFile',('"'+(Join-Path $pilotLogs ($Task+'.log'))+'"'))
if($Task -eq 'Build'){
    if(-not $BuildDirectory){throw 'Build requires a fresh -BuildDirectory'}
    $pilotBuild=[IO.Path]::GetFullPath($BuildDirectory)
    if(Test-Path -LiteralPath $pilotBuild){throw "Build target already exists: $pilotBuild"}
    $pilotArgs+=@('-echoesBuildPath',('"'+$pilotBuild+'"'))
}
$env:BEE_BUILD_THREADS='4'
$pilotProcess=Start-Process -FilePath 'D:/Unity/Downloads/6000.3.27f1/Editor/Unity.exe' -ArgumentList $pilotArgs -WindowStyle Hidden -PassThru -Wait
Write-Output "PHASE52_TASK=$Task EXIT=$($pilotProcess.ExitCode)"
exit $pilotProcess.ExitCode
