param([Parameter(Mandatory=$true)][string]$Source,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$sourceRoot=(Resolve-Path -LiteralPath $Source).Path
$destinationRoot=(Resolve-Path -LiteralPath $Destination).Path
if($sourceRoot -eq $destinationRoot -or !(Test-Path -LiteralPath (Join-Path $destinationRoot '.git'))){throw 'Use a separate existing Git checkout'}
$manifest=Get-Content -LiteralPath (Join-Path $sourceRoot 'Review/market-publish-dependencies.json') -Raw | ConvertFrom-Json
$selected=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach($path in $manifest.dependencies){$null=$selected.Add($path)}
foreach($base in @('Assets/OutOfService','Assets/StreamingAssets','Tools')){
    foreach($file in Get-ChildItem -LiteralPath (Join-Path $sourceRoot $base) -Recurse -File){
        $relative=$file.FullName.Substring($sourceRoot.Length+1).Replace('\','/')
        if($relative -notlike '*/Generated/Batches/*'){$null=$selected.Add($relative)}
    }
}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'Review') -Filter '*.json' -File){
    if($file.Name -match 'handoff|validation|audit|assets|sources|references|register|placement|layout|publish-dependencies'){$null=$selected.Add('Review/'+$file.Name)}
}
foreach($relative in @($selected)){
    if(!(Test-Path -LiteralPath (Join-Path $sourceRoot $relative) -PathType Leaf)){throw ('Missing source '+$relative)}
    if($relative.StartsWith('Assets/')){
        if(Test-Path -LiteralPath (Join-Path $sourceRoot ($relative+'.meta'))){$null=$selected.Add($relative+'.meta')}
        $parent=[System.IO.Path]::GetDirectoryName($relative).Replace('\','/')
        while($parent -ne 'Assets'){
            $meta=$parent+'.meta'
            if((Test-Path -LiteralPath (Join-Path $sourceRoot $meta)) -and !(Test-Path -LiteralPath (Join-Path $destinationRoot $meta))){$null=$selected.Add($meta)}
            $parent=[System.IO.Path]::GetDirectoryName($parent).Replace('\','/')
        }
    }
}
[long]$total=0
foreach($relative in ($selected | Sort-Object)){
    $origin=Join-Path $sourceRoot $relative
    $target=[System.IO.Path]::GetFullPath((Join-Path $destinationRoot $relative))
    if(!$target.StartsWith($destinationRoot+[System.IO.Path]::DirectorySeparatorChar,[System.StringComparison]::OrdinalIgnoreCase)){throw 'Destination escaped checkout'}
    $null=[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($target))
    Copy-Item -LiteralPath $origin -Destination $target -Force
    $total+=(Get-Item -LiteralPath $origin).Length
}
$missing=@($manifest.dependencies | Where-Object {!(Test-Path -LiteralPath (Join-Path $destinationRoot $_)) -or !(Test-Path -LiteralPath (Join-Path $destinationRoot ($_+'.meta')))})
if($missing.Count){throw ('Missing exported dependencies: '+($missing -join ', '))}
$report=[ordered]@{
    scene=$manifest.scene;scene_sha256=(Get-FileHash -LiteralPath (Join-Path $destinationRoot $manifest.scene) -Algorithm SHA256).Hash
    unity=$manifest.unity;selected_file_count=$selected.Count;selected_bytes=$total;saved_scene_dependencies=$manifest.dependencies.Count;missing_dependencies=$missing
    preserved='Team scenes, characters, package manifest, build scene list and project settings are not overwritten.'
    excluded='Library/Temp/Builds/Logs/UserSettings, local reference photos and obsolete unreferenced generated batches'
    shared_settings='Saved PC renderer/pipeline/volume dependencies are included; inspect their diff before merging.'
}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destinationRoot 'Review/market-upload-manifest-2026-11-22.json') -Encoding utf8
$report | ConvertTo-Json -Depth 5
