[CmdletBinding()]
param([string]$EvidenceName='verification')
$ErrorActionPreference='Stop'
$pilotRepo=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$copyRoot=Join-Path 'D:/Unity/Temp/Echoes' ('phase52-'+$EvidenceName+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
if(Test-Path -LiteralPath $copyRoot){throw "Fresh verification target required: $copyRoot"}
$copyGame=Join-Path $copyRoot 'game'
New-Item -ItemType Directory -Path $copyGame | Out-Null
foreach($directoryName in @('Assets','Packages','ProjectSettings')){
    Copy-Item -LiteralPath (Join-Path $pilotRepo ('game/'+$directoryName)) -Destination $copyGame -Recurse
}
$testLogs=Join-Path $pilotRepo ('artifacts/visual-qa/phase-5.2/logs/'+$EvidenceName)
if(Test-Path -LiteralPath $testLogs){throw "Preserve existing logs; choose a fresh EvidenceName: $testLogs"}
New-Item -ItemType Directory -Path $testLogs | Out-Null
[IO.File]::WriteAllText((Join-Path $testLogs 'isolated-project.txt'),$copyGame)
$env:BEE_BUILD_THREADS='4'
foreach($platformName in @('EditMode','PlayMode')){
    $testXml=Join-Path $testLogs ('tests-'+$platformName+'.xml')
    $testLog=Join-Path $testLogs ('tests-'+$platformName+'.log')
    $testArgs=@('-batchmode','-projectPath',('"'+$copyGame+'"'),'-runTests','-testPlatform',$platformName,'-testResults',('"'+$testXml+'"'),'-logFile',('"'+$testLog+'"'))
    $testProcess=Start-Process -FilePath 'D:/Unity/Downloads/6000.3.27f1/Editor/Unity.exe' -ArgumentList $testArgs -WindowStyle Hidden -PassThru -Wait
    Write-Output "PHASE52_TEST=$platformName EXIT=$($testProcess.ExitCode) PROJECT=$copyGame"
    if(Test-Path -LiteralPath $testXml){[xml]$result=Get-Content -LiteralPath $testXml;$run=$result.'test-run';Write-Output "RESULT=$($run.result) TOTAL=$($run.total) PASSED=$($run.passed) FAILED=$($run.failed)";foreach($failed in $result.SelectNodes('//test-case[@result="Failed"]')){Write-Output ($failed.fullname+': '+$failed.failure.message.InnerText)}}
    if($testProcess.ExitCode -ne 0){exit $testProcess.ExitCode}
}
