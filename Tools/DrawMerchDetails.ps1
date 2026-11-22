# Original fictional shop graphics. No downloaded product packaging is embedded.
Add-Type -AssemblyName System.Drawing
$folder=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/MerchDetails'
New-Item -ItemType Directory -Force $folder | Out-Null
$source=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'GenerateMarketPacks.ps1')
$block=[regex]::Match($source,'(?s)\$assortment=@\((.*?)\r?\n\)').Groups[1].Value
$names=@([regex]::Matches($block,"'([^']+)'\s*,?") | ForEach-Object {$_.Groups[1].Value.Split('|')})
$recipes=@(Get-Content (Join-Path $PSScriptRoot 'RetailPhotoSources/ShelfPrintRecipes.txt') | Where-Object {$_ -and !$_.StartsWith('#')})
$basePrices=@(4980,1480,1290,3980,2480,1490,2980,1980,6980,7980,580,1680,1780,3980,1480,12900,17900,1280,6980,5980,8980,980,6980,9980,2480,4980,2780,12900,6980,15900)
function Font($size,$bold=$false){[Drawing.Font]::new('Malgun Gothic',$size,$(if($bold){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular}),[Drawing.GraphicsUnit]::Pixel)}
function Text($g,$s,$x,$y,$w,$h,$size,$brush=[Drawing.Brushes]::Black,$bold=$false){
 $f=Font $size $bold;$g.DrawString($s,$f,$brush,[Drawing.RectangleF]::new($x,$y,$w,$h));$f.Dispose()
}
function Barcode($g,$x,$y,$w,$h,$seed){
 $r=[Random]::new($seed)
 for($i=0;$i -lt $w;$i+=4){$g.FillRectangle([Drawing.Brushes]::Black,$x+$i,$y,1+$r.Next(2),$h)}
}
$b=[Drawing.Bitmap]::new(3200,1900);$g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::FromArgb(246,246,237));$g.TextRenderingHint='AntiAliasGridFit'
for($family=0;$family -lt 30;$family++){for($v=0;$v -lt 6;$v++){
 $id=$family*6+$v;$x=($id%10)*320;$y=[math]::Floor($id/10)*100
 $amount=$recipes[$family].Split('|')[3].Split(' ')[$v]
 $price=$basePrices[$family]+@(0,100,300,0,500,200)[$v]
 if($family -eq 0){$price=if($v -in @(1,4)){1180}else{4980}}
 if($family -eq 1){$price=1390}
 if($family -eq 2){$price=if($v%2){1580}else{4280};if(!($v%2)){$amount='210g×3'}}
 Text $g $names[$id] ($x+9) ($y+3) 205 27 21
 Text $g $amount ($x+218) ($y+5) 97 23 15
 Text $g ('{0:N0}' -f $price) ($x+8) ($y+26) 207 51 41 ([Drawing.Brushes]::Black) $true
 Text $g '원' ($x+182) ($y+43) 34 30 21
 Barcode $g ($x+232) ($y+43) 70 28 ($id+492)
 Text $g ('홈웨이  '+$family.ToString('D2')+'-'+$v+'   정상가격') ($x+9) ($y+80) 300 18 12 ([Drawing.Brushes]::DimGray)
}}
for($v=0;$v -lt 6;$v++){
 $x=$v*320;$y=1800
 Text $g $names[66+$v] ($x+9) ($y+3) 205 27 21
 Text $g '250mL×6' ($x+218) ($y+5) 97 23 15
 Text $g '6,980원' ($x+8) ($y+26) 223 51 39 ([Drawing.Brushes]::Black) $true
 Barcode $g ($x+232) ($y+43) 70 28 (790+$v)
 Text $g '홈웨이  캔음료 묶음   정상가격' ($x+9) ($y+80) 300 18 12 ([Drawing.Brushes]::DimGray)
}
$b.Save((Join-Path $folder 'price_atlas.png'));$g.Dispose();$b.Dispose()
for($v=0;$v -lt 6;$v++){
 $b=[Drawing.Bitmap]::new(512,200);$g=[Drawing.Graphics]::FromImage($b);$g.TextRenderingHint='AntiAliasGridFit'
 $g.Clear([Drawing.ColorTranslator]::FromHtml(@('#902824','#347749','#252d31','#c66626','#6b9eb3','#9c792c')[$v]))
 Text $g '샘결음료' 22 6 280 35 24 ([Drawing.Brushes]::White)
 Text $g $names[66+$v] 18 47 430 100 64 ([Drawing.Brushes]::White) $true
 Text $g 'CAN  250 mL × 6' 22 150 370 47 29 ([Drawing.Brushes]::White)
 $b.Save((Join-Path $folder ('carrier_'+$v+'.png')));$g.Dispose();$b.Dispose()
}
$b=[Drawing.Bitmap]::new(2048,2048);$g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::FromArgb(242,240,230));$g.TextRenderingHint='AntiAliasGridFit';$g.SmoothingMode='AntiAlias'
$titles=@('상처밴드','면봉','데일리 비타','식염 세정수','휴대용 손세정','구강청결 스프레이')
$sub=@('부드러운 원단 / 20매','순면 / 100개입','건강기능식품 / 60정','위생용품 / 100 mL','생활 위생 / 50 mL','구강용품 / 20 mL')
for($i=0;$i -lt 16;$i++){
 $state=$g.Save();$g.TranslateTransform(($i%4)*512,[math]::Floor($i/4)*512)
 if($i -lt 6){
  $hue=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml(@('#b74031','#407588','#a75f26','#397d70','#586a94','#4d7b61')[$i]))
  $g.FillRectangle([Drawing.Brushes]::White,0,0,512,512)
  $g.FillRectangle($hue,0,0,512,75);Text $g '하루봄' 22 12 400 60 41 ([Drawing.Brushes]::White) $true
  Text $g $titles[$i] 22 103 476 160 72 $hue $true
  Text $g $sub[$i] 22 291 470 65 25
  if($i -eq 0){$g.FillRectangle($hue,72,246,335,27);$g.FillRectangle([Drawing.Brushes]::BurlyWood,200,236,82,47)}
  elseif($i -eq 1){for($k=0;$k -lt 3;$k++){$g.FillRectangle($hue,108,240+$k*14,270,3)}}
  else{$g.DrawEllipse([Drawing.Pens]::LightGray,283,197,140,140);Text $g @('','','60','100','50','20')[$i] 302 222 130 80 47 $hue $true}
  $g.FillRectangle($hue,0,394,512,7);Barcode $g 330 425 150 46 (120+$i)
  Text $g '홈웨이 약국' 22 422 280 39 28;Text $g '포장 단위별 진열 상품' 22 466 295 29 18
  $hue.Dispose()
 }elseif($i -eq 6){
  $g.FillRectangle([Drawing.Brushes]::DarkSlateGray,0,0,512,512)
  Text $g 'HOMEWAY' 30 32 440 65 49 ([Drawing.Brushes]::White) $true
  Text $g '계산대 마감' 32 178 448 100 67 ([Drawing.Brushes]::Gainsboro) $true
  Text $g 'CLOSED' 34 301 442 62 40 ([Drawing.Brushes]::LightGray)
  Text $g '영업 종료 · 이용해 주셔서 감사합니다' 34 431 444 60 22 ([Drawing.Brushes]::LightGray)
 }elseif($i -eq 7){
  Text $g '홈웨이' 50 18 420 60 39 ([Drawing.Brushes]::Black) $true
  Text $g '영수증   /   고객 보관용' 32 87 460 45 25
  for($j=0;$j -lt 5;$j++){Text $g @('봉지면  5개입          4,980','생수  2 L                  580','세탁세제  2 L       12,900','--------------------------------','합계                    18,460')[$j] 32 (155+$j*43) 470 45 24}
  Barcode $g 62 420 370 58 285
 }elseif($i -eq 8){
  $g.FillRectangle([Drawing.Brushes]::DarkSlateGray,0,0,512,512)
  Text $g '모퉁이 커피' 28 25 460 67 46 ([Drawing.Brushes]::White) $true
  $menu=@('아메리카노                 3,000','카페라테                    3,800','바닐라 라테                4,200','레몬 티                     3,600','원두는 매장에서 갈아 드립니다')
  for($j=0;$j -lt 5;$j++){Text $g $menu[$j] 30 (125+$j*70) 455 62 $(if($j -eq 4){21}else{30}) ([Drawing.Brushes]::White)}
 }elseif($i -eq 9){
  $g.FillRectangle([Drawing.Brushes]::Firebrick,0,0,512,512)
  Text $g '이번 주' 32 25 446 75 53 ([Drawing.Brushes]::White) $true
  Text $g '알뜰 장보기' 32 145 460 135 70 ([Drawing.Brushes]::White) $true
  Text $g '행사 상품을 만나보세요' 32 353 448 70 29 ([Drawing.Brushes]::White)
  Text $g 'HOMEWAY' 32 448 448 44 32 ([Drawing.Brushes]::White) $true
 }elseif($i -eq 10){
  $g.FillRectangle([Drawing.Brushes]::Black,0,0,512,512)
  Text $g '표준   울   세척   탈수' 18 18 480 80 38 ([Drawing.Brushes]::LightGray)
  Text $g '1:25' 147 140 340 200 125 ([Drawing.Brushes]::DarkSeaGreen)
  Text $g '온도       헹굼       예약' 18 399 480 76 36 ([Drawing.Brushes]::LightGray)
 }elseif($i -eq 11){
  Text $g '홈웨이 가전' 20 15 474 65 42 ([Drawing.Brushes]::Firebrick) $true
  Text $g '드럼 세탁기' 20 108 470 80 57 ([Drawing.Brushes]::Black) $true
  Text $g '12 kg · 절전 모터' 20 215 470 60 37
  Text $g '549,000원' 20 315 470 110 68 ([Drawing.Brushes]::Black) $true
  Text $g '전시 상품 · 직원에게 문의하세요' 20 458 470 40 24
 }elseif($i -eq 12){
  Text $g '결제 단말기' 28 28 460 66 41
  $g.FillRectangle([Drawing.Brushes]::DarkSlateGray,40,116,432,165)
  Text $g '대기' 130 149 330 100 63 ([Drawing.Brushes]::LightGray)
  Text $g 'IC 카드    ▽' 100 355 370 80 50
 }elseif($i -eq 13){
  Text $g '1 kg' 22 25 460 67 44
  Text $g '0.000' 23 139 470 170 128 ([Drawing.Brushes]::DarkGreen)
  Text $g '전자 저울  /  영점' 26 385 460 77 44
 }elseif($i -eq 14){
  $g.FillRectangle([Drawing.Brushes]::SaddleBrown,0,0,512,512)
  Text $g '오늘의 한끼' 28 25 460 77 49 ([Drawing.Brushes]::White) $true
  Text $g "덮밥     8,000`n국수     6,500`n정식     9,000" 34 140 444 290 55 ([Drawing.Brushes]::White)
 }else{
  Text $g '상품 보충용' 30 30 450 80 50 ([Drawing.Brushes]::Black) $true
  Text $g '취급 주의 ↑↑' 30 168 450 80 54
  Barcode $g 38 318 430 98 17;Text $g 'HOMEWAY / 물류' 30 449 450 48 28
 }
 $g.Restore($state)
}
$b.Save((Join-Path $folder 'detail_atlas.png'));$g.Dispose();$b.Dispose()
