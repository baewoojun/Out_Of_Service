$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dest=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/MallDetails/damum-homewares.png'
$bmp=New-Object Drawing.Bitmap 1536,512
$g=[Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode='AntiAlias';$g.TextRenderingHint='AntiAliasGridFit'
$ink=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(35,54,46))
$paper=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(240,237,222))
$accent=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(60,85,70))
$pen=New-Object Drawing.Pen ([Drawing.Color]::FromArgb(50,64,56)),4
$font=New-Object Drawing.Font 'Malgun Gothic',34,([Drawing.FontStyle]::Bold),([Drawing.GraphicsUnit]::Pixel)
$small=New-Object Drawing.Font 'Malgun Gothic',20,([Drawing.FontStyle]::Regular),([Drawing.GraphicsUnit]::Pixel)
$brand=New-Object Drawing.Font 'Malgun Gothic',22,([Drawing.FontStyle]::Bold),([Drawing.GraphicsUnit]::Pixel)
$names=@('양수냄비','편수냄비','프라이팬','궁중팬','밀폐용기','도자기 머그')
$specs=@('25 cm / 유리뚜껑','18 cm / 스테인리스','28 cm / 논스틱','30 cm / 깊은 볶음팬','직사각 / 잠금뚜껑','350 mL / 생활도자기')
$prices=@('18,900원','14,900원','12,900원','16,900원','5,900원','3,900원')
for($i=0;$i -lt 6;$i++){
 $x=($i%3)*512;$y=[Math]::Floor($i/3)*256
 $g.FillRectangle($paper,$x,$y,512,256);$g.FillRectangle($accent,$x,$y,512,40)
 $g.DrawString('담음  DAMUM  |  LIVING',$brand,([Drawing.Brushes]::White),$x+18,$y+4)
 $g.DrawString($names[$i],$font,$ink,$x+195,$y+58)
 $g.DrawString($specs[$i],$small,$ink,$x+195,$y+110)
 $g.DrawString($prices[$i],$font,$ink,$x+195,$y+153)
 if($i -le 1){
  $g.DrawArc($pen,$x+36,$y+86,112,108,0,180);$g.DrawLine($pen,$x+36,$y+132,$x+36,$y+95);$g.DrawLine($pen,$x+148,$y+132,$x+148,$y+95)
  $g.DrawEllipse($pen,$x+35,$y+83,114,25);$g.DrawArc($pen,$x+70,$y+62,42,35,180,180)
  if($i -eq 0){$g.DrawArc($pen,$x+16,$y+110,27,35,90,180);$g.DrawArc($pen,$x+142,$y+110,27,35,-90,180)}else{$g.DrawLine($pen,$x+147,$y+110,$x+183,$y+80)}
 }elseif($i -le 3){
  $g.DrawEllipse($pen,$x+24,$y+95,125,75);$g.DrawArc($pen,$x+24,$y+([int](105+($i-2)*20)),125,75,0,180);$g.DrawLine($pen,$x+141,$y+112,$x+184,$y+68);$g.DrawLine($pen,$x+150,$y+120,$x+191,$y+77)
 }elseif($i -eq 4){
  $g.DrawRectangle($pen,$x+27,$y+102,135,71);$g.DrawRectangle($pen,$x+23,$y+94,143,16);$g.DrawRectangle($pen,$x+20,$y+112,12,35);$g.DrawRectangle($pen,$x+156,$y+112,12,35)
 }else{
  $g.DrawEllipse($pen,$x+37,$y+76,91,24);$g.DrawLine($pen,$x+37,$y+88,$x+42,$y+177);$g.DrawLine($pen,$x+128,$y+88,$x+122,$y+177);$g.DrawArc($pen,$x+42,$y+166,80,23,0,180);$g.DrawArc($pen,$x+118,$y+103,43,57,-90,180)
 }
 $g.DrawString('생활에 필요한 모양을 담다',$small,$ink,$x+18,$y+219)
 for($bar=0;$bar -lt 34;$bar++){ $w=1+(($bar*7+$i)%3);$g.FillRectangle($ink,$x+355+$bar*4,$y+214,$w,24) }
}
$bmp.Save($dest,[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$bmp.Dispose();$ink.Dispose();$paper.Dispose();$accent.Dispose();$pen.Dispose();$font.Dispose();$small.Dispose();$brand.Dispose()
Write-Output 'Six original DAMUM product cards generated. Fictional names and prices; no retail photograph pixels.'
