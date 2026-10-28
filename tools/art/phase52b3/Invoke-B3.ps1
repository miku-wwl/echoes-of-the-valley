[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidateSet('Generate','Build','Preview')][string]$Task,
    [string]$BuildDirectory='',
    [string]$PreviewDirectory='',
    [string]$LogDirectory='artifacts/visual-qa/phase-5.2-b3/logs'
)
# Phase 5.2-B3 Unity tasks (one Unity instance at a time; nothing under game/Assets may change while it runs).
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$logs=[IO.Path]::GetFullPath($LogDirectory)
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$method=@{Generate='Echoes.EditorTools.Phase52B3Builder.Generate';Build='Echoes.EditorTools.Phase52B3Builder.BuildWindowsPilot';Preview='Echoes.EditorTools.Phase52B3Preview.Capture'}[$Task]
$unityArgs=@('-batchmode','-quit','-projectPath',('"'+(Join-Path $repo 'game')+'"'),'-executeMethod',$method,'-logFile',('"'+(Join-Path $logs ($Task+'.log'))+'"'))
if($Task -eq 'Build'){
    if(-not $BuildDirectory){throw 'Build requires a fresh -BuildDirectory'}
    $target=[IO.Path]::GetFullPath($BuildDirectory)
    if(Test-Path -LiteralPath $target){throw "Build target already exists: $target"}
    $unityArgs+=@('-echoesBuildPath',('"'+$target+'"'))
}
if($Task -eq 'Preview'){
    if(-not $PreviewDirectory){throw 'Preview requires -PreviewDirectory'}
    $unityArgs+=@('-echoesPreviewDir',('"'+[IO.Path]::GetFullPath($PreviewDirectory)+'"'))
}
$env:BEE_BUILD_THREADS='4'
$process=Start-Process -FilePath 'D:/Unity/Downloads/6000.3.27f1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru -Wait
Write-Output "PHASE52B3_TASK=$Task EXIT=$($process.ExitCode) LOG=$(Join-Path $logs ($Task+'.log'))"
exit $process.ExitCode
