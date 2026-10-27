[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$PlayerDirectory,
    [string]$EvidenceRoot='artifacts/visual-qa/phase-5.2-b2/player'
)
# Runs every Windows-player evidence capture for the B2 pilot, one player at a time, recording each result honestly.
# All input is AUTOMATED (PlayerController.InputOverride / camera APIs); no human played. Teleports only set up still positions after the timed route.
$ErrorActionPreference='Continue'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Set-Location $repo
$summary=[ordered]@{}
function Step([string]$name,[scriptblock]$body){
    Write-Output "=== $name $(Get-Date -Format HH:mm:ss)"
    try{& $body;$summary[$name]=if($LASTEXITCODE -eq 0 -or $null -eq $LASTEXITCODE){'EXIT_0'}else{'EXIT_'+$LASTEXITCODE}}catch{$summary[$name]='ERROR: '+$_.Exception.Message;Write-Output $_.Exception.Message}
}
$capture=Join-Path $repo 'tools/art/phase52/Capture-PilotMotion.ps1'
$smoke=Join-Path $repo 'tools/art/phase52b2/Run-PilotSmoke.ps1'
Step 'tour-720-recorded-video'   {& powershell -NoProfile -ExecutionPolicy Bypass -File $capture -PlayerDirectory $PlayerDirectory -OutputDirectory "$EvidenceRoot/after720" -Video}
Step 'tour-1080'                 {& powershell -NoProfile -ExecutionPolicy Bypass -File $capture -PlayerDirectory $PlayerDirectory -OutputDirectory "$EvidenceRoot/after1080" -Width 1920 -Height 1080}
Step 'tour-720-rc2-baseline'     {& powershell -NoProfile -ExecutionPolicy Bypass -File $capture -PlayerDirectory $PlayerDirectory -OutputDirectory "$EvidenceRoot/baseline720" -Baseline}
Step 'tour-720-unrecorded'       {& powershell -NoProfile -ExecutionPolicy Bypass -File $capture -PlayerDirectory $PlayerDirectory -OutputDirectory "$EvidenceRoot/unrecorded720"}
Step 'tour-1080-unrecorded'      {& powershell -NoProfile -ExecutionPolicy Bypass -File $capture -PlayerDirectory $PlayerDirectory -OutputDirectory "$EvidenceRoot/unrecorded1080" -Width 1920 -Height 1080}
Step 'smoke-720'                 {& powershell -NoProfile -ExecutionPolicy Bypass -File $smoke -PlayerDirectory $PlayerDirectory -OutputDirectory "$EvidenceRoot/smoke720"}
Step 'smoke-1080'                {& powershell -NoProfile -ExecutionPolicy Bypass -File $smoke -PlayerDirectory $PlayerDirectory -OutputDirectory "$EvidenceRoot/smoke1080" -Width 1920 -Height 1080}
foreach($folder in 'after720','after1080','baseline720','unrecorded720','unrecorded1080'){
    $path=Join-Path $EvidenceRoot $folder
    if(Test-Path (Join-Path $path 'tour.json')){& python (Join-Path $repo 'tools/art/phase52b2/analyze_tour.py') $path $(if($folder -eq 'after720'){'--sheet'}) | Out-Null}
}
& python (Join-Path $repo 'tools/art/phase52b2/make_b2_comparison.py')
$summary|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $EvidenceRoot 'evidence-run-summary.json') -Encoding UTF8
$summary|ConvertTo-Json
