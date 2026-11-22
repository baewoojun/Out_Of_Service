Add-Type -AssemblyName System.Drawing
$path=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'
$labels=@('한우 구이용','돼지 삼겹살','닭 정육','손질 고등어','연어 슬라이스','흰다리새우')
for($i=0;$i -lt 6;$i++){
 $b=[Drawing.Bitmap]::new(300,190);$g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::Ivory)
 $ink=[Drawing.Brushes]::Black;$accent=if($i -lt 3){[Drawing.Brushes]::Maroon}else{[Drawing.Brushes]::MidnightBlue}
 $g.FillRectangle($accent,0,0,300,34)
 $font=[Drawing.Font]::new('Malgun Gothic',14);$head=[Drawing.Font]::new('Malgun Gothic',24,[Drawing.FontStyle]::Bold)
 $g.DrawString('바른장터  신선센터',$font,[Drawing.Brushes]::White,8,0)
 $g.DrawString($labels[$i],$head,$ink,6,34)
 $g.DrawString(('냉장보관   '+@(320,450,350,280,200,240)[$i]+' g'),$font,$ink,7,92)
 for($n=0;$n -lt 43;$n++){$g.FillRectangle($ink,10+6*$n,141,1+(($n*7+$i)%4),32)}
 $b.Save((Join-Path $path "FreshTicket$i.png"));$g.Dispose();$b.Dispose();$font.Dispose();$head.Dispose()
}
foreach($style in 0..3){
 $b=[Drawing.Bitmap]::new(128,128);$g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::WhiteSmoke)
 $weft=[Drawing.Pen]::new([Drawing.Color]::FromArgb(42,80,70,59))
 for($n=0;$n -lt 128;$n+=4){$g.DrawLine($weft,0,$n,128,$n);$g.DrawLine($weft,$n,0,$n,128)}
 if($style -in @(1,3)){foreach($n in @(4,8,64,68)){$g.FillRectangle([Drawing.Brushes]::SlateGray,0,$n,128,2);if($style -eq 3){$g.FillRectangle([Drawing.Brushes]::SlateGray,$n,0,2,128)}}}
 $b.Save((Join-Path $path "Woven$style.png"));$weft.Dispose();$g.Dispose();$b.Dispose()
}

# Neutral surface maps modulate the existing skin colour without a unique material per fruit.
foreach($kind in @(2,4,6,7,9)){
 $b=[Drawing.Bitmap]::new(128,128);$g=[Drawing.Graphics]::FromImage($b)
 $g.Clear([Drawing.Color]::FromArgb(246,248,238));$rng=[Random]::new(410+$kind)
 $brush=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(75,123,106,72))
 for($n=0;$n -lt 850;$n++){
   $x=$rng.Next(128);$y=$rng.Next(128)
   $w=if($kind -eq 4){$rng.Next(1,5)}else{1}
   $h=if($kind -eq 7){$rng.Next(2,7)}else{$w}
   $g.FillEllipse($brush,$x,$y,$w,$h)
 }
 if($kind -eq 9){
   $pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(110,255,255,204),2)
   $g.DrawLine($pen,64,0,64,128)
   foreach($y in @(20,43,66,90,112)){$g.DrawLine($pen,64,$y,12,$y-19);$g.DrawLine($pen,64,$y,115,$y-19)}
   $pen.Dispose()
 }
 $b.Save((Join-Path $path "ProduceSkin$kind.png"));$g.Dispose();$brush.Dispose();$b.Dispose()
}

# Laminated orchard placard and the matching timber fixture grain.
$b=[Drawing.Bitmap]::new(768,320);$g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::FromArgb(39,44,38))
$g.TextRenderingHint='AntiAliasGridFit';$font=[Drawing.Font]::new('Malgun Gothic',42,[Drawing.FontStyle]::Bold)
$small=[Drawing.Font]::new('Malgun Gothic',19);$g.DrawString('산지에서 온 제철 과일',$font,[Drawing.Brushes]::FloralWhite,49,15)
foreach($c in 0..1){$x=24+$c*366;$g.FillRectangle([Drawing.Brushes]::Ivory,$x,120,351,176);$g.FillRectangle([Drawing.Brushes]::Firebrick,$x,120,351,42)
 $g.DrawString(@('사과  ·  1개','사과  ·  6입 상자')[$c],$small,[Drawing.Brushes]::White,$x+12,121)
 $g.DrawString(@('1,490원','8,490원')[$c],$font,[Drawing.Brushes]::Black,$x+12,174)}
$b.Save((Join-Path $path 'OrchardBoard.png'));$g.Dispose();$b.Dispose();$font.Dispose();$small.Dispose()
$b=[Drawing.Bitmap]::new(512,96);$g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::Wheat)
$rng=[Random]::new(514)
for($n=0;$n -lt 450;$n++){ $y=$rng.Next(96);$x=$rng.Next(512);$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb($rng.Next(9,40),89,52,27))
$g.DrawBezier($pen,$x,$y,$x+35,$y-2,$x+88,$y+2,$x+$rng.Next(100,210),$y);$pen.Dispose()}
$b.Save((Join-Path $path 'ProduceOak.png'));$g.Dispose();$b.Dispose()

# Packed salads and apple blush: authored for close views, no external artwork.
foreach($k in 0..2){
 $b=[Drawing.Bitmap]::new(440,210);$g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::FloralWhite)
 $g.TextRenderingHint='AntiAliasGridFit'
 $ink=[Drawing.SolidBrush]::new(@([Drawing.Color]::DarkOliveGreen,[Drawing.Color]::DarkSlateBlue,[Drawing.Color]::Sienna)[$k])
 $h=[Drawing.Font]::new('Malgun Gothic',31,[Drawing.FontStyle]::Bold);$t=[Drawing.Font]::new('Malgun Gothic',16)
 $g.FillRectangle($ink,0,0,440,43);$g.DrawString('바른장터   FRESH DELI',$t,[Drawing.Brushes]::White,10,3)
 $g.DrawString(@('그린 샐러드','적채 믹스','당근 채소믹스')[$k],$h,$ink,8,52)
 $g.DrawString(@('어린잎 · 양상추','적양배추 · 어린잎','당근 · 양상추')[$k],$t,[Drawing.Brushes]::Black,10,116)
 $g.DrawString(@('냉장  120 g','냉장  135 g','냉장  150 g')[$k],$t,$ink,10,164)
 foreach($j in 0..23){$g.FillRectangle([Drawing.Brushes]::Black,293+5*$j,160,1+($j*7%3),32)}
 $b.Save((Join-Path $path "SaladSleeve$k.png"));$g.Dispose();$h.Dispose();$t.Dispose();$ink.Dispose();$b.Dispose()
 $b=[Drawing.Bitmap]::new(256,128);$rng=[Random]::new(525+$k)
 for($x=0;$x -lt 256;$x++){for($y=0;$y -lt 128;$y++){
  $u=$x*2*[Math]::PI/256
  $blush=[Math]::Min(1.0,[Math]::Max(0.0,.39+.35*[Math]::Cos($u+$k)+.17*[Math]::Sin($u*13+.75*[Math]::Sin($y/23))+.12*[Math]::Cos($u*23)+.18*($y/128)))
  $b.SetPixel($x,$y,[Drawing.Color]::FromArgb([int](139+52*$blush),[int](36+77*$blush),[int](28+28*$blush))) }}
 $g=[Drawing.Graphics]::FromImage($b);$speck=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(125,244,213,153))
 for($n=0;$n -lt 400;$n++){$g.FillEllipse($speck,$rng.Next(256),$rng.Next(128),1,1)}
 $b.Save((Join-Path $path "AppleBlush$k.png"));$g.Dispose();$speck.Dispose();$b.Dispose()
}

foreach($i in 0..8){
 $b=[Drawing.Bitmap]::new(384,114);$g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::Ivory)
 $ink=if($i -eq 4){[Drawing.Brushes]::MidnightBlue}elseif($i -gt 4){[Drawing.Brushes]::DarkOliveGreen}else{[Drawing.Brushes]::SaddleBrown}
 $g.FillRectangle($ink,0,0,384,10);$h=[Drawing.Font]::new('Malgun Gothic',26,[Drawing.FontStyle]::Bold);$t=[Drawing.Font]::new('Malgun Gothic',13)
 $g.DrawString(@('표고버섯','새송이버섯','양송이버섯','느타리버섯','팽이버섯','모둠 쌈채소','간편대파','깐감자','카레용 채소')[$i],$h,$ink,8,12)
 $g.DrawString(('바른장터  냉장  '+@(300,300,200,200,150,120,180,250,200)[$i]+' g'),$t,[Drawing.Brushes]::Black,9,78)
 for($n=0;$n -lt 22;$n++){$g.FillRectangle([Drawing.Brushes]::Black,273+$n*4,77,1+$n%3,23)}
 $b.Save((Join-Path $path "FreshProduceLabel$i.png"));$g.Dispose();$b.Dispose();$h.Dispose();$t.Dispose()
}
