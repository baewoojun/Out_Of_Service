param([switch]$Install)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot=Split-Path $PSScriptRoot -Parent
$target=Join-Path $projectRoot 'Review/GarmentSources/sooim-hangtag.png'
$bmp=[Drawing.Bitmap]::new(256,384)
$g=[Drawing.Graphics]::FromImage($bmp)
$g.Clear([Drawing.Color]::FromArgb(238,235,224))
$g.TextRenderingHint=[Drawing.Text.TextRenderingHint]::AntiAliasGridFit
function Label([string]$text,[int]$size,[int]$y,[bool]$bold=$false) {
 $f=[Drawing.Font]::new('Malgun Gothic',$size,$(if($bold){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular}),[Drawing.GraphicsUnit]::Pixel)
 $format=[Drawing.StringFormat]::new();$format.Alignment=[Drawing.StringAlignment]::Center
 $g.DrawString($text,$f,[Drawing.Brushes]::Black,[Drawing.RectangleF]::new(12,$y,232,60),$format)
 $f.Dispose();$format.Dispose()
}
$g.FillEllipse([Drawing.Brushes]::DimGray,121,13,14,14)
Label 'SOOIM' 38 44 $true
Label '수임 / EVERYDAY' 16 96
$g.DrawLine([Drawing.Pens]::Gray,28,130,228,130)
Label 'COTTON SHIRT' 18 150 $true
Label 'M  /  95' 36 183 $true
Label '₩ 29,900' 24 241
for($i=0;$i -lt 70;$i++) {if(($i*17)%7 -lt 4){$g.FillRectangle([Drawing.Brushes]::Black,28+$i*2.8,297,1.4+(($i*3)%2),32)}}
Label '880 1000 329 955' 12 335
$bmp.Save($target,[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bmp.Dispose()
if($Install) {
 $destination=Join-Path $projectRoot 'Assets/OutOfService/Art/Prints'
 $null=New-Item -ItemType Directory -Path $destination -Force
 Copy-Item -LiteralPath $target -Destination $destination -Force
}
Write-Output $target
