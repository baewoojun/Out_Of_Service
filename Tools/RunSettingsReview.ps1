param([string]$Version='SettingsV1-2026-11-23')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$dest=Join-Path $project ('Review/'+$Version)
New-Item -ItemType Directory -Force $dest | Out-Null
$profile=Join-Path $dest 'settings-profile.json'
if(Test-Path -LiteralPath $profile){throw 'Choose a fresh version directory; do not overwrite previous verification.'}
$exe=Join-Path $project 'Builds/MarketReview/Market.exe'
foreach($mode in @('settingsReview','settingsReloadReview')){
    $stamp=Get-Date
    $log=Join-Path $dest ($mode+'.log')
    $process=Start-Process -FilePath $exe -ArgumentList @(('-'+$mode),'-settingsProfile',('"'+$profile+'"'),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',('"'+$log+'"')) -WindowStyle Normal -PassThru -Wait
    $folder=if($mode -eq 'settingsReview'){'SettingsReview'}else{'SettingsReloadReview'}
    $source=Join-Path $project ('Builds/MarketReview/'+$folder)
    $result=Join-Path $source 'result.txt'
    if(-not(Test-Path -LiteralPath $result) -or (Get-Item -LiteralPath $result).LastWriteTime -lt $stamp){throw "Missing or stale result: $mode"}
    Copy-Item -LiteralPath $source -Destination $dest -Recurse -Force
    $text=Get-Content -LiteralPath $result -Raw
    Write-Output "$mode exit=$($process.ExitCode): $text"
    if($process.ExitCode -ne 0 -or $text -notmatch '^PASS'){throw "Settings verification failed: $mode"}
}
