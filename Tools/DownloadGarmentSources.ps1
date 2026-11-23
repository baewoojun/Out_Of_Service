param([switch]$Install)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$target=Join-Path $projectRoot 'Review/GarmentSources'
$null=New-Item -ItemType Directory -Path $target -Force
$records=@()
foreach($id in @('cotton_jersey','fabric_pattern_07')) {
    $info=Invoke-RestMethod ('https://api.polyhaven.com/info/'+$id)
    $files=Invoke-RestMethod ('https://api.polyhaven.com/files/'+$id)
    $colour=if($id -eq 'cotton_jersey'){'Diffuse'}else{'col_1'}
    $originals=@()
    foreach($map in @($colour,'nor_gl','arm')) {
        $entry=$files.$map.'2k'.jpg
        if(!$entry.url){throw "Missing original: $id $map"}
        $path=Join-Path $target ($id+'_'+$map+'_2k.jpg')
        if(!(Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm MD5).Hash.ToLowerInvariant() -ne $entry.md5) {Invoke-WebRequest -Uri $entry.url -OutFile $path}
        $actual=(Get-FileHash -LiteralPath $path -Algorithm MD5).Hash.ToLowerInvariant()
        if($actual -ne $entry.md5){throw "Source checksum mismatch: $path"}
        $originals+=@{path=('Assets/OutOfService/Art/Textures/SurveyedPBR/'+(Split-Path $path -Leaf));url=$entry.url;md5=$actual;bytes=(Get-Item -LiteralPath $path).Length}
    }
    $records+=@{name=$info.name;id=$id;authors=$info.authors;source=('https://polyhaven.com/a/'+$id);license='CC0-1.0';license_url='https://polyhaven.com/license';checked_date='2026-11-23';dimensions_mm=$info.dimensions;original_files=$originals;usage=if($id -eq 'cotton_jersey'){'T-shirts, hanging knit tops and soft upholstery; not labelled as Oxford woven fabric'}else{'Casual checked shirts on 2F and customer clothing in B1 laundry'};modifications='Original photos retained. Compressed Unity mask conversion, reduced micro-normal strength, physical-scale garment UVs. Original garment model and labels; no real clothing brand graphics copied.'}
}
$records | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $target 'garment-sources.json') -Encoding utf8
if($Install) {
    $destination=Join-Path $projectRoot 'Assets/OutOfService/Art/Textures/SurveyedPBR'
    foreach($record in $records){foreach($original in $record.original_files){Copy-Item -LiteralPath (Join-Path $target (Split-Path $original.path -Leaf)) -Destination $destination -Force}}
    Copy-Item -LiteralPath (Join-Path $target 'garment-sources.json') -Destination $destination -Force
}
$records | Select-Object id,authors,dimensions_mm | ConvertTo-Json -Depth 4
