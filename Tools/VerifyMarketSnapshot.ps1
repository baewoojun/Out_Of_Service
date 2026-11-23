param([Parameter(Mandatory=$true)][string]$Source,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$sourceRoot=(Resolve-Path -LiteralPath $Source).Path
$destinationRoot=(Resolve-Path -LiteralPath $Destination).Path
$manifest=Get-Content -LiteralPath (Join-Path $sourceRoot 'Review/market-publish-dependencies.json') -Raw | ConvertFrom-Json
$missing=@();$different=@();$guids=@{};$duplicates=@();$oversize=@()
foreach($relative in $manifest.dependencies){
    foreach($path in @($relative,($relative+'.meta'))){
        $origin=Join-Path $sourceRoot $path;$target=Join-Path $destinationRoot $path
        if(-not(Test-Path -LiteralPath $target)){$missing+=$path;continue}
        if((Get-FileHash -LiteralPath $origin -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash){$different+=$path}
    }
}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $destinationRoot 'Assets') -Recurse -File -Filter '*.meta'){
    $match=[regex]::Match([IO.File]::ReadAllText($file.FullName),'(?m)^guid:\s*([a-f0-9]{32})\s*$')
    if($match.Success){$guid=$match.Groups[1].Value;if($guids.ContainsKey($guid)){$duplicates+=@{guid=$guid;first=$guids[$guid];second=$file.FullName}}else{$guids[$guid]=$file.FullName}}
}
foreach($path in (git -C $destinationRoot ls-files --others --exclude-standard)){
    $file=Get-Item -LiteralPath (Join-Path $destinationRoot $path)
    if($file.Length -gt 99MB){$oversize+=@{path=$path;bytes=$file.Length}}
}
$report=[ordered]@{verified_at=(Get-Date -Format o);scene=$manifest.scene;scene_sha256=(Get-FileHash -LiteralPath (Join-Path $destinationRoot $manifest.scene)).Hash;dependencies=$manifest.dependencies.Count;missing=$missing;different_content=$different;unique_asset_guids=$guids.Count;duplicate_guids=$duplicates;new_files_over_99MiB=$oversize;source_verification='Scene assets and their meta files hash-identical to the standalone-tested source project.';full_checkout_build='Not performed: team package/project settings retained; standalone build was performed in source project.'}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $destinationRoot 'Review/market-snapshot-validation-2026-11-23.json') -Encoding utf8
$report | ConvertTo-Json -Depth 6
if($missing.Count -or $different.Count -or $duplicates.Count -or $oversize.Count){throw 'Snapshot verification failed'}
