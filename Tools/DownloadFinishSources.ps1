$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$target=Join-Path $projectRoot 'Assets/OutOfService/Art/Textures/SurveyedPBR'
$null=New-Item -ItemType Directory -Path $target -Force
$records=@()
foreach($id in @('white_oak_veneer','painted_plaster_wall','worn_concrete_floor','metal_plate_02')) {
    $info=Invoke-RestMethod ('https://api.polyhaven.com/info/'+$id)
    $files=Invoke-RestMethod ('https://api.polyhaven.com/files/'+$id)
    $maps=if($id -eq 'white_oak_veneer'){@('Diffuse','nor_gl','arm')}else{@('arm')}
    $originals=@()
    foreach($map in $maps) {
        $entry=$files.$map.'2k'.jpg
        if(!$entry.url){throw "Missing 2K source: $id $map"}
        $path=Join-Path $target ($id+'_'+$map+'_2k.jpg')
        if(!(Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm MD5).Hash.ToLowerInvariant() -ne $entry.md5) {
            Invoke-WebRequest -Uri $entry.url -OutFile $path
        }
        $actual=(Get-FileHash -LiteralPath $path -Algorithm MD5).Hash.ToLowerInvariant()
        if($actual -ne $entry.md5){throw "Source checksum mismatch: $path"}
        $originals+=@{path=$path.Substring($projectRoot.Length+1).Replace('\','/');url=$entry.url;md5=$actual;bytes=(Get-Item -LiteralPath $path).Length}
    }
    $records+=@{name=$info.name;id=$id;authors=$info.authors;source=('https://polyhaven.com/a/'+$id);license='CC0-1.0';license_url='https://polyhaven.com/license';checked_date='2026-11-23';dimensions_mm=$info.dimensions;original_files=$originals;usage=if($id -eq 'white_oak_veneer'){'Shared cafe tables/chairs, benches, shopfront timber and display shelves'}else{'Existing plaster / staff floor / stockroom metal; physical roughness and occlusion'};modifications='Original photos retained. Unity mask conversion: R metal, G occlusion, A smoothness. Tinted shared materials; physical-scale UVs. No product labels copied.'}
}
$records | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $target 'sources.json') -Encoding utf8
$records | Select-Object id,authors,dimensions_mm | ConvertTo-Json -Depth 4
