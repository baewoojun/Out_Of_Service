# Original artwork for the fictional Deulnyeok dairy range.
Add-Type -AssemblyName System.Drawing
$out=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'
function Brush($hex){[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex))}
function Block($hex,$x,$y,$w,$h){$b=Brush $hex;$g.FillRectangle($b,$x,$y,$w,$h);$b.Dispose()}
function Oval($hex,$x,$y,$w,$h){$b=Brush $hex;$g.FillEllipse($b,$x,$y,$w,$h);$b.Dispose()}
function TypeLine($s,$size,$color,$x,$y,$family='Malgun Gothic'){
 $font=[Drawing.Font]::new($family,$size,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
 $b=Brush $color;$g.DrawString($s,$font,$b,$x,$y);$b.Dispose();$font.Dispose()
}
function Berry($x,$y,$scale){
 $g.TranslateTransform($x,$y);$g.ScaleTransform($scale,$scale)
 $path=[Drawing.Drawing2D.GraphicsPath]::new()
 $path.AddBezier(2,22,1,-4,25,-4,40,7);$path.AddBezier(40,7,56,-5,82,0,77,27)
 $path.AddBezier(77,27,70,51,48,74,38,77);$path.AddBezier(38,77,20,70,4,42,2,22)
 $b=Brush '#C92F45';$g.FillPath($b,$path);$b.Dispose();$path.Dispose()
 foreach($pt in @(@(21,18),@(45,22),@(62,18),@(30,38),@(53,41),@(40,57))){Oval '#FFDA96' $pt[0] $pt[1] 3 6}
 Oval '#447041' 24 -2 29 10;Oval '#5D8848' 42 1 22 8
 $g.ResetTransform()
}
for($i=0;$i -lt 4;$i++){
 $bmp=[Drawing.Bitmap]::new(384,512);$g=[Drawing.Graphics]::FromImage($bmp)
 $g.SmoothingMode='AntiAlias';$g.TextRenderingHint='AntiAliasGridFit';$g.Clear([Drawing.Color]::FloralWhite)
 switch($i){
  0 {
   Block '#164C3E' 0 0 384 68;TypeLine '들녘  l  목장우유' 30 '#FFF9E8' 21 10
   TypeLine '매일 아침' 34 '#3D725D' 24 86;TypeLine '흰우유' 98 '#173E31' 9 120
   TypeLine 'FRESH WHOLE MILK' 23 '#366950' 28 253
   Oval '#CBD99A' -160 321 540 240;Oval '#82A878' 142 340 390 220;Oval '#42785E' -100 390 420 160
   Block '#164C3E' 0 453 384 59;TypeLine '원유 100%                 1 L' 26 '#FFFFFF' 20 462
  }
  1 {
   $g.Clear([Drawing.ColorTranslator]::FromHtml('#FFE2DB'));Block '#9D2441' 0 0 384 53
   TypeLine '들녘 과일우유' 24 '#FFFFFF' 19 8;TypeLine '딸기가' 71 '#882B43' 12 62;TypeLine '퐁당' 80 '#882B43' 12 143
   Berry 176 210 2.2;Berry 76 300 1.45
   TypeLine 'STRAWBERRY MILK' 22 '#932C44' 16 427;TypeLine '딸기우유     200 mL' 24 '#932C44' 16 463
  }
  2 {
   $g.Clear([Drawing.ColorTranslator]::FromHtml('#FFEDC9'))
   Block '#AA312A' 0 28 384 13;Block '#AA312A' 0 52 384 5
   TypeLine '들녘 발효연구소' 29 '#783123' 29 82;TypeLine '하루' 91 '#97362D' 22 132
   TypeLine '발효유' 78 '#97362D' 22 234;Block '#AA312A' 0 350 384 68
   TypeLine 'CULTURED MILK' 36 '#FFF2D9' 23 358;TypeLine '냉장보관      100 mL' 24 '#783123' 25 440
  }
  3 {
   $g.Clear([Drawing.ColorTranslator]::FromHtml('#243E66'));TypeLine '들녘 크리머리' 27 '#F5ECD5' 21 16
   TypeLine 'Plain' 99 '#FFFAE9' 14 66 'Georgia';TypeLine '플레인 요거트' 44 '#FFFAE9' 18 198
   Oval '#93B9D2' 30 292 315 102;Oval '#FFFBEA' 43 297 285 75;Oval '#ECDFC9' 107 312 137 28
   TypeLine '떠먹는 요구르트   85 g' 26 '#F5ECD5' 20 445
  }
 }
 $bmp.Save((Join-Path $out "DairyFacing$i.png"),[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bmp.Dispose()
}
