param(
    [string[]]$Modes=@('fitoutReview','graphicsReview','floorReview','refinementReview'),
    [string]$Version='FitoutV15-2026-11-23'
)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $project 'Builds/MarketReview/Market.exe'
$destination=Join-Path $project ('Review/'+$Version)
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$folders=@{fitoutReview='FitoutReview';graphicsReview='GraphicsReview';floorReview='FloorReview';refinementReview='RefinementReview';produceWalk='ProduceWalk';fashionWalk='FashionWalk';upperRetailWalk='UpperRetailWalk';mallFirstVisit='MallFirstVisit';pepperFirstVisit='PepperFirstVisit'}
foreach($mode in $Modes){
    if(-not $folders.ContainsKey($mode)){throw "Unknown review mode $mode"}
    $stamp=Get-Date
    $log=Join-Path $destination ($mode+'.log')
    $style=if($mode -match 'Walk|FirstVisit'){'Normal'}else{'Hidden'}
    $process=Start-Process -FilePath $exe -ArgumentList @(('-'+$mode),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',('"'+$log+'"')) -WindowStyle $style -PassThru -Wait
    $source=Join-Path $project ('Builds/MarketReview/'+$folders[$mode])
    $result=Join-Path $source 'result.txt'
    if($mode -match 'FirstVisit'){
        $filename=if($mode -eq 'mallFirstVisit'){'mall-first-visit.txt'}else{'pepper-first-visit.txt'}
        $result=Join-Path $project ('Builds/MarketReview/'+$filename)
        if(-not(Test-Path -LiteralPath $result) -or (Get-Item -LiteralPath $result).LastWriteTime -lt $stamp){throw "Missing or stale result for $mode"}
        $text=Get-Content -LiteralPath $result -Raw
        $valid=$process.ExitCode -eq 0 -and $text -match '1920x1080' -and $text -match 'normal camera'
        if($mode -eq 'mallFirstVisit'){$valid=$valid -and $text -match '8/8 route legs'}
        else{$valid=$valid -and $text -match 'distance 6\.\d+ m'}
        $dest=Join-Path $destination $folders[$mode];New-Item -ItemType Directory -Path $dest -Force | Out-Null
        Copy-Item -LiteralPath $result -Destination $dest -Force
        $status=$(if($valid){'PASS'}else{'FAIL'})+': '+$text
        Set-Content -LiteralPath (Join-Path $dest 'result.txt') -Value $status
        Write-Output "$mode exit=$($process.ExitCode): $status"
        if(-not $valid){throw "Review failed: $mode"}
        continue
    }
    if(-not(Test-Path $result)){throw "No result for $mode (exit $($process.ExitCode))"}
    if((Get-Item $result).LastWriteTime -lt $stamp){throw "Stale result for $mode"}
    Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
    $text=Get-Content $result -Raw
    Write-Output "$mode exit=$($process.ExitCode): $text"
    if($process.ExitCode -ne 0 -or $text -notmatch '^PASS'){throw "Review failed: $mode"}
}
