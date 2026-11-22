# Rebuild only authored reverse/side print and frozen reverse wrap.
# Existing product fronts and ingredient photography are read, never regenerated.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
. (Join-Path $PSScriptRoot 'DrawPackInformation.ps1')
$source=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'GenerateMarketPacks.ps1')
function Read-Array($key){
 $block=[regex]::Match($source,('(?s)\$'+$key+'\s*=\s*@\((.*?)\r?\n\)')).Groups[1].Value
 @([regex]::Matches($block,"'([^']+)'\s*,?") | ForEach-Object {$_.Groups[1].Value})
}
$families=Read-Array 'names';$assortment=Read-Array 'assortment'
if($families.Count -ne 30 -or $assortment.Count -ne 30){throw 'Unexpected package catalog.'}
$folder=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'
for($i=0;$i -lt 30;$i++){
 $atlas=[Drawing.Bitmap]::new(2048,1024)
 $brand=@('골목찬방','바삭제과','샘결음료','오후다방','서리식탁','살림결','꼬리식탁')[$(if($i -lt 5){0}elseif($i -lt 10){1}elseif($i -lt 15){2}elseif($i -lt 19){3}elseif($i -lt 27){4}elseif($i -lt 29){5}else{6})]
 for($v=0;$v -lt 6;$v++){
  $front=[Drawing.Bitmap]::new((Join-Path $folder ('pack_{0:D2}_{1}.png' -f $i,$v)))
  try{Write-PackInformation $atlas $front $folder $i $v $assortment[$i].Split('|')[$v] $brand $families[$i]}finally{$front.Dispose()}
 }
 $atlas.Save((Join-Path $folder ('RetailInformation'+$i+'.png')));$atlas.Dispose()
}
Write-Output 'Updated 180 original reverse/side print panels; existing front artwork preserved.'
