$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dest=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/MallDetails'
$names=@('수분 로션','순한 토너','보습 크림','순한 세안','진정 세럼','장벽 에센스')
$english=@('DAILY LOTION','MILD TONER','SOFT CREAM','GENTLE WASH','CALM SERUM','BARRIER ESSENCE')
for($i=0;$i -lt 6;$i++){
    $bmp=New-Object Drawing.Bitmap 256,256
    $g=[Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode='AntiAlias';$g.TextRenderingHint='AntiAliasGridFit'
    $g.Clear([Drawing.Color]::FromArgb(234,231,214))
    $ink=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(39,66,50))
    $brand=New-Object Drawing.Font 'Malgun Gothic',42,([Drawing.FontStyle]::Bold),([Drawing.GraphicsUnit]::Pixel)
    $name=New-Object Drawing.Font 'Malgun Gothic',29,([Drawing.FontStyle]::Bold),([Drawing.GraphicsUnit]::Pixel)
    $small=New-Object Drawing.Font 'Malgun Gothic',16,([Drawing.FontStyle]::Regular),([Drawing.GraphicsUnit]::Pixel)
    $g.DrawString('모아',$brand,$ink,72,15)
    $g.DrawString('MOA SKIN CARE',$small,$ink,61,70)
    $g.FillRectangle($ink,24,108,208,2)
    $g.DrawString($names[$i],$name,$ink,60,126)
    $g.DrawString($english[$i],$small,$ink,65,172)
    $g.DrawString(@('200 mL','150 mL','50 g','150 mL','30 mL','60 mL')[$i],$small,$ink,98,216)
    $bmp.Save((Join-Path $dest ('moa-skincare-'+$i+'.png')),[Drawing.Imaging.ImageFormat]::Png)
    $small.Dispose();$name.Dispose();$brand.Dispose();$ink.Dispose();$g.Dispose();$bmp.Dispose()
}
Write-Output 'Six original parody skincare labels written; no retail image pixels used.'
$bmp=New-Object Drawing.Bitmap 512,352
$g=[Drawing.Graphics]::FromImage($bmp)
$g.TextRenderingHint='AntiAliasGridFit';$g.Clear([Drawing.Color]::FromArgb(245,241,226))
$center=New-Object Drawing.StringFormat
$center.Alignment=[Drawing.StringAlignment]::Center
$center.LineAlignment=[Drawing.StringAlignment]::Center
$texts=@('전시 상품 안내','일반 세탁기','상부 투입 · 12 kg','429,000원')
$ys=@(12,70,150,227);$sizes=@(27,47,27,53)
for($n=0;$n -lt 4;$n++){
    $font=New-Object Drawing.Font 'Malgun Gothic',$sizes[$n],([Drawing.FontStyle]::Bold),([Drawing.GraphicsUnit]::Pixel)
    $rect=New-Object Drawing.RectangleF 12,$ys[$n],488,65
    $g.DrawString($texts[$n],$font,([Drawing.Brushes]::Black),$rect,$center);$font.Dispose()
}
$bmp.Save((Join-Path $dest 'top-load-price.png'),[Drawing.Imaging.ImageFormat]::Png)
$center.Dispose();$g.Dispose();$bmp.Dispose()
