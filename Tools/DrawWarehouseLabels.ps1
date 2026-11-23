param([switch]$Install)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$project=Split-Path $PSScriptRoot -Parent
$target=Join-Path $project 'Review/WarehouseLabelDraft'
$null=New-Item -ItemType Directory -Path $target -Force
$names=@('생활 잡화','상온 가공식품','냉동 간편식','냉동 채소')
for($kind=0;$kind -lt 4;$kind++){
 $bmp=[Drawing.Bitmap]::new(512,320);$g=[Drawing.Graphics]::FromImage($bmp)
 $g.Clear([Drawing.Color]::FromArgb(236,234,218));$g.TextRenderingHint=[Drawing.Text.TextRenderingHint]::AntiAliasGridFit
 function PrintLine([string]$s,[int]$size,[int]$x,[int]$y,[bool]$bold=$false){
  $f=[Drawing.Font]::new('Malgun Gothic',$size,$(if($bold){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular}),[Drawing.GraphicsUnit]::Pixel)
  $g.DrawString($s,$f,[Drawing.Brushes]::Black,[Drawing.PointF]::new($x,$y));$f.Dispose()
 }
 $g.DrawRectangle([Drawing.Pens]::DimGray,8,8,495,303)
 PrintLine 'HOMMOA  /  물류센터' 25 23 17 $true
 PrintLine $names[$kind] 41 23 58 $true
 $g.DrawLine([Drawing.Pens]::Gray,20,116,491,116)
 PrintLine ('입고 구역  '+$(if($kind -lt 2){'A-03  상온'}else{'C-02  냉동'})) 23 23 128
 PrintLine ('취급 주의  /  '+$(if($kind -lt 2){'건조 보관'}else{'냉동 보관'})) 21 23 165
 $random=[Random]::new(710+$kind);$x=25
 while($x -lt 484){$width=$random.Next(1,5);$g.FillRectangle([Drawing.Brushes]::Black,$x,212,$width,56);$x+=$width+$random.Next(1,4)}
 PrintLine ('HM-24'+$kind+'-1028   /   검수  □') 19 25 278
 $bmp.Save((Join-Path $target ('warehouse-'+$kind+'.png')),[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bmp.Dispose()
}
if($Install){
 $dest=Join-Path $project 'Assets/OutOfService/Art/Prints'
 for($kind=0;$kind -lt 4;$kind++){Copy-Item -LiteralPath (Join-Path $target ('warehouse-'+$kind+'.png')) -Destination (Join-Path $dest ('warehouse-'+$kind+'.png')) -Force}
}
Write-Output 'Four original warehouse handling labels authored. Decorative barcode only.'
