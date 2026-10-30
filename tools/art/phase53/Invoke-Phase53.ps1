[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidateSet('Generate','Build')][string]$Task,[string]$BuildDirectory='')
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
if (@(Get-Process -Name Unity -ErrorAction SilentlyContinue).Count) { throw 'Another Unity job is running; preserve it' }
$logs=Join-Path $repo 'artifacts/visual-qa/phase-5.3/logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$log=Join-Path $logs ($Task+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
$method=if($Task -eq 'Generate'){'Echoes.EditorTools.Phase53Builder.Generate'}else{'Echoes.EditorTools.Phase53Builder.BuildWindowsPilot'}
$unityArgs=@('-batchmode','-quit','-projectPath',('"'+(Join-Path $repo 'game')+'"'),'-executeMethod',$method,'-logFile',('"'+$log+'"'))
if($Task -eq 'Build'){
    if(-not $BuildDirectory){throw 'Choose a fresh build output'}
    $target=if([IO.Path]::IsPathRooted($BuildDirectory)){$BuildDirectory}else{Join-Path $repo $BuildDirectory}
    if(Test-Path -LiteralPath $target){throw 'Build output already exists; preserve it'}
    $unityArgs+=@('-echoesBuildPath',('"'+$target+'"'))
}
$env:BEE_BUILD_THREADS='4'
$p=Start-Process -FilePath 'D:/Unity/Downloads/6000.3.27f1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru -Wait
Write-Output "PHASE53_TASK=$Task EXIT=$($p.ExitCode) LOG=$log"
exit $p.ExitCode
