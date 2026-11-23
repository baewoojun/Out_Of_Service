param([switch]$RefreshFiles,[string]$OutputDirectory='')
Add-Type -AssemblyName System.Drawing

# Original print design. Ingredient photos retain their existing individual
# credits; official product pictures are used only as construction references.
function Draw-RetailRefresh($g,[int]$i,[int]$v,[string]$name) {
    if($i -notin @(0,2,5,9,15,27,28)){return}
    $saved=$g.Save();$scale=$g.VisibleClipBounds.Width/512
    $g.ScaleTransform($scale,$scale);$g.SmoothingMode='AntiAlias'
    $owned=[Collections.Generic.List[IDisposable]]::new()
    function Ink([string]$hex){$b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex));$owned.Add($b);return $b}
    function Box($x,$y,$w,$h){[Drawing.RectangleF]::new($x,$y,$w,$h)}
    function FrontText($text,$x,$y,$w,$h,$brush,$face='Malgun Gothic',$outline=$null,$angle=0){PackCaption $g $text (Box $x $y $w $h) 70 $brush $face $outline $angle}
    function Picture($photo,$x,$y,$w,$h,$ellipse=$true,$focus=.5){
        $clip=[Drawing.Drawing2D.GraphicsPath]::new()
        try{if($ellipse){$clip.AddEllipse($x,$y,$w,$h)}else{$clip.AddRectangle((Box $x $y $w $h))};RetailPhoto $g $photo (Box $x $y $w $h) $clip $focus}finally{$clip.Dispose()}
    }
    function Line($brush,$width,$x1,$y1,$x2,$y2){$p=[Drawing.Pen]::new($brush.Color,$width);try{$g.DrawLine($p,$x1,$y1,$x2,$y2)}finally{$p.Dispose()}}
    function Badge($text,$x,$y,$diameter,$brush,$textBrush){$g.FillEllipse($brush,$x,$y,$diameter,$diameter);FrontText $text ($x+5) ($y+$diameter*.31) ($diameter-10) ($diameter*.31) $textBrush}
    $white=Ink '#FFFCF2';$black=Ink '#222628';$cream=Ink '#F4E3B6';$navy=Ink '#183747';$gold=Ink '#CEAA55'
    try {
        if($i -eq 0){
            $base=Ink @('#A62426','#E7BE61','#A94422','#E9E0BF','#253A29','#E8EEE2')[$v]
            $second=Ink @('#E8C367','#722E20','#192D3E','#466646','#C99843','#A52231')[$v]
            $g.Clear($base.Color)
            $ink=if($v -in @(1,3,5)){$navy}else{$white}
            FrontText '골목제면' 27 23 123 28 $ink
            $multi=$v%3 -ne 1
            $g.FillRectangle($second,366,16,123,58);FrontText $(if($multi){'5개 묶음'}else{'한 봉지'}) 376 27 102 30 $white
            if($v -eq 0){
                $g.FillPolygon($second,[Drawing.Point[]]@([Drawing.Point]::new(0,255),[Drawing.Point]::new(512,310),[Drawing.Point]::new(512,443),[Drawing.Point]::new(0,399)))
                Picture 'ramen' 205 221 340 283
                FrontText '얼큰' 28 104 263 130 $white 'Batang' $black.Color -3
                FrontText '장국면' 32 240 272 78 $cream 'Malgun Gothic' $black.Color -3
                FrontText '진하게 끓인 장국의 풍미' 28 341 235 29 $white
            }elseif($v -eq 1){
                Line $second 2 25 84 487 84
                FrontText '닭육수면' 23 112 466 99 $second 'Batang'
                FrontText '담백하고 깊은 국물' 50 226 412 30 $navy
                Picture 'chicken_soup' 31 270 452 255
            }elseif($v -eq 2){
                $g.FillRectangle($second,0,90,512,172)
                FrontText '바다' 25 106 192 106 $cream 'Batang'
                FrontText '짬뽕' 215 120 272 118 $white 'Malgun Gothic' $black.Color -4
                Picture 'jjamppong' -30 274 573 314
                FrontText '칼칼한 해물 국물' 39 246 380 29 $white
            }elseif($v -eq 3){
                $g.FillRectangle($second,0,78,20,349)
                FrontText '맑은' 39 108 167 64 $second 'Batang'
                FrontText '버섯면' 39 180 416 103 $navy 'Batang'
                Picture 'shiitake' 49 310 414 210
                FrontText '버섯의 향을 담은 담백한 국물' 34 274 444 25 $second
            }elseif($v -eq 4){
                FrontText '직화의 고소함을 담다' 27 80 279 27 $cream
                FrontText '구운' 25 130 208 83 $gold 'Batang'
                FrontText '짜장' 209 156 276 118 $white 'Batang' $black.Color
                Picture 'review_jjajang' 4 283 504 282
            }else{
                $g.FillPolygon($second,[Drawing.Point[]]@([Drawing.Point]::new(0,90),[Drawing.Point]::new(512,47),[Drawing.Point]::new(512,194),[Drawing.Point]::new(0,249)))
                FrontText '새콤' 22 103 170 75 $white 'Malgun Gothic' $navy.Color -6
                FrontText '비빔면' 192 124 299 92 $white 'Malgun Gothic' $navy.Color -6
                Picture 'bibim_guksu' 69 274 474 295
                FrontText '가볍게 비벼 즐기는 한 그릇' 20 245 400 25 $second
            }
            $g.FillRectangle($second,0,470,512,42)
            FrontText @('120 g × 5','110 g','120 g × 5','110 g × 5','130 g','125 g × 5')[$v] 326 480 169 21 $white
            FrontText '면류  |  조리예' 20 480 172 20 $white
        }elseif($i -eq 2){
            $accent=Ink @('#9F2734','#84612C','#63503D','#AF743D','#4D7044','#544367')[$v]
            $g.Clear($white.Color)
            $g.FillEllipse($accent,-42,-160,596,390)
            FrontText '골목찬방' 160 27 192 29 $white
            FrontText $name 63 83 386 99 $white 'Malgun Gothic'
            FrontText @('갓 지은 듯 고슬고슬','구수한 찰보리 한 그릇','여러 곡물의 고소함','현미의 담백한 식감','향긋하게 즐기는 곤드레','흑미를 더한 한 그릇')[$v] 36 245 440 27 $accent
            # Individually inspected food-only crops: no real brand, foil lid,
            # decorative bowl or outdoor scenery is copied into our artwork.
            $key=@('cooked_rice','refresh_barley_rice','refresh_mixed_rice','refresh_brown_rice','refresh_gondre_rice','refresh_black_rice')[$v]
            $photo=$stockPhotos[$key]
            if(!$photo){$photo=[Drawing.Image]::FromFile((Join-Path $recipeFolder ('Ingredients/'+$key+'.jpg')));$stockPhotos[$key]=$photo}
            $crops=@(@(118,100,271,130),@(450,245,330,174),@(240,300,760,400),@(89,162,295,155),@(165,174,157,83),@(365,415,440,232))
            $crop=$crops[$v]
            $g.FillEllipse((Ink '#DDDAD1'),85,278,342,178)
            $clip=[Drawing.Drawing2D.GraphicsPath]::new();$clip.AddEllipse(91,274,330,174);$st=$g.Save();$g.SetClip($clip)
            try{$g.DrawImage($photo,(Box 91 274 330 174),[Drawing.RectangleF]::new($crop[0],$crop[1],$crop[2],$crop[3]),[Drawing.GraphicsUnit]::Pixel)}finally{$g.Restore($st);$clip.Dispose()}
            FrontText '전자레인지 조리  ·  210 g' 84 466 344 26 $navy
        }elseif($i -eq 5){
            $base=Ink @('#DB5724','#F0D453','#B42E25','#E8BD70','#347848','#F5BD38')[$v]
            $accent=Ink @('#7E291F','#204370','#E8C444','#193C62','#ECE4C5','#A0422B')[$v]
            $g.Clear($base.Color);$g.FillRectangle($gold,0,0,512,24);$g.FillRectangle($gold,0,485,512,27)
            FrontText '바삭제과' 27 39 134 25 $(if($v -in @(1,3,5)){$navy}else{$white})
            $photo=$printRecipes[5].Split('|')[0].Split(' ')[$v]
            if($v -eq 0){
                Picture $photo -68 258 580 282
                FrontText '새우' 32 112 220 124 $white 'Malgun Gothic' $accent.Color -6
                FrontText '바삭' 210 214 255 121 $white 'Malgun Gothic' $accent.Color -6
                Badge '소금구이' 361 71 115 $cream $accent
            }elseif($v -eq 1){
                $g.FillEllipse($accent,-150,89,784,246)
                FrontText '소금감자' 31 133 450 107 $white 'Malgun Gothic' $navy.Color
                FrontText '얇게 썰어 바삭하게' 98 253 316 26 $cream
                Picture $photo 81 314 461 236
                Badge '오리지널' 29 355 104 $accent $white
            }elseif($v -eq 2){
                $g.FillPolygon($accent,[Drawing.Point[]]@([Drawing.Point]::new(512,64),[Drawing.Point]::new(86,310),[Drawing.Point]::new(512,450)))
                FrontText '옥수수' 29 110 390 94 $white 'Malgun Gothic' $navy.Color -5
                FrontText '콘' 34 214 161 155 $cream 'Malgun Gothic' $navy.Color -5
                Picture $photo 219 243 319 296
            }elseif($v -eq 3){
                $g.FillRectangle($accent,0,87,512,189)
                FrontText '오징어볼' 30 118 452 107 $white 'Malgun Gothic' $navy.Color 3
                Picture $photo -16 289 546 284
                FrontText '동그랗게 즐기는 바삭한 한입' 34 245 444 24 $white
            }elseif($v -eq 4){
                $g.FillEllipse($accent,-41,80,594,281)
                FrontText '양파링' 55 141 402 117 $base 'Malgun Gothic' $white.Color
                Picture $photo 24 323 468 214
                FrontText '양파의 풍미  ·  고소한 링 스낵' 35 286 442 24 $navy
            }else{
                FrontText '치즈' 31 94 250 97 $accent 'Malgun Gothic' $white.Color -4
                FrontText '퍼프' 156 195 312 114 $accent 'Malgun Gothic' $white.Color -4
                Picture $photo -32 308 572 241
                Badge '치즈맛' 369 79 107 $accent $white
            }
            FrontText $printRecipes[5].Split('|')[3].Split(' ')[$v] 394 462 97 22 $navy
        }elseif($i -eq 9){
            $accent=Ink @('#35523D','#87432C','#846547','#B47735','#6A773F','#49503B')[$v]
            $g.Clear($cream.Color);$g.FillRectangle($accent,0,0,512,72)
            FrontText '매일 한 줌  /  바삭상점' 31 24 450 29 $white
            $photo=$printRecipes[9].Split('|')[0].Split(' ')[$v]
            if($v -eq 1){$photo='refresh_almond_kernels'}
            if($v -in @(0,3,5)){
                FrontText $name 28 112 456 91 $accent 'Batang'
                Picture $photo 34 244 444 209 $false
                FrontText @('하루의 간편한 견과 습관','','','고소하게 볶은 땅콩','','껍질째 즐기는 견과')[$v] 31 211 450 23 $navy
            }else{
                Picture $photo -40 168 592 416 $false
                $g.FillRectangle($white,42,108,428,138)
                FrontText $name 58 128 396 75 $accent 'Batang'
                FrontText '고소함을 밀봉한 지퍼백' 63 211 386 22 $navy
            }
            $g.FillRectangle($accent,0,465,512,47);FrontText '개봉 후 밀봉 보관' 20 480 265 22 $white
            FrontText $printRecipes[9].Split('|')[3].Split(' ')[$v] 363 478 128 25 $white
        }elseif($i -eq 15){
            $base=Ink @('#D4BA87','#242B29','#E5DDC7','#B78C58','#352C29','#426163')[$v]
            $accent=Ink @('#574333','#C4A36A','#36554E','#482E27','#D5B88C','#E2CC9F')[$v]
            $g.Clear($base.Color)
            FrontText '오후다방' 22 24 155 30 $accent 'Batang'
            FrontText 'ROASTED COFFEE' 28 81 456 30 $accent
            $g.FillRectangle($accent,44,150,424,232)
            $paper=if($v -in @(0,2,3)){$white}else{$navy}
            FrontText $name 63 179 386 79 $paper 'Batang'
            FrontText @('NUTTY  /  MILD','DARK  /  FULL BODY','DAILY  /  BALANCED','FRUITY  /  LIGHT','INTENSE  /  DARK','DECAFFEINATED')[$v] 66 280 380 29 $paper
            FrontText 'WHOLE BEAN   ·   200 g' 66 335 380 24 $paper
            for($k=0;$k -lt 4;$k++){Badge $(if($k -le $v%4){'●'}else{'○'}) (173+$k*39) 412 25 $accent $base}
            FrontText '원두커피  /  개봉 후 밀봉하여 보관' 30 462 452 23 $accent
        }elseif($i -eq 27){
            $base=Ink @('#1D649A','#846CA3','#719332','#1E875A','#668EAD','#8C6CA3')[$v]
            $g.Clear($white.Color)
            $g.FillEllipse($base,-48,-141,647,410)
            FrontText '살림결' 33 34 141 30 $white
            FrontText $name 27 112 458 97 $white 'Malgun Gothic' $navy.Color -3
            if($v -in @(0,3)){
                $g.FillEllipse($base,27,304,150,150);FrontText '액체' 46 332 112 44 $white;FrontText '세탁세제' 40 384 125 29 $white
                FrontText '깨끗한 빨래의 시작' 191 314 287 42 $navy
                FrontText '일반 · 드럼 겸용' 191 373 285 27 $base
                FrontText '2 L' 348 450 134 35 $base
            }else{
                $photo=@('-','photo_honeysuckle_1','lemon','-','cotton','rosemary')[$v]
                Picture $photo 248 287 242 171
                FrontText $(if($v -in @(1,4)){'섬유유연제'}else{'주방세제'}) 24 296 200 51 $base
                FrontText $(if($v -in @(1,4)){'부드러운 감촉'}else{'산뜻한 설거지'}) 24 369 200 29 $navy
                FrontText $(if($v -in @(1,4)){'1.5 L'}else{'500 mL'}) 31 453 214 32 $base
            }
            Line $base 6 21 496 491 496
        }else{
            $base=Ink @('#4D7770','#8D6C8F','#407991','#718473','#7E9294','#4C7A65')[$v]
            $g.Clear($white.Color)
            # Original embossed-leaf motif, not repeated botanical photos.
            for($n=0;$n -lt 9;$n++){
                $pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(32,$base.Color),3)
                try{$g.DrawArc($pen,-72+$n*83,-99,190,615,260,155)}finally{$pen.Dispose()}
            }
            $g.FillRectangle($base,0,0,512,62);FrontText '살림결  |  DAILY SOFT' 30 18 452 28 $white
            FrontText $name 34 123 444 93 $base 'Batang'
            $detail=@('부드러운 3겹','미용티슈','흡수력 좋은 키친타월','가볍게 휴대하는 티슈','편안한 2겹','도톰한 3겹')[$v]
            FrontText $detail 34 253 444 46 $navy
            $g.FillEllipse($base,291,327,179,151)
            FrontText @('6롤','250매','2롤','10매 × 6','6롤','6롤')[$v] 307 376 147 41 $white
            FrontText '부드러운 감촉' 34 369 235 31 $base
            FrontText '매일 사용하는 생활 위생용품' 25 472 262 22 $navy
        }
    }finally{foreach($item in $owned){$item.Dispose()};$g.Restore($saved)}
}

if($RefreshFiles){
    $ErrorActionPreference='Stop'
    . (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
    . (Join-Path $PSScriptRoot 'DrawPaperRefresh.ps1')
    $source=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'GenerateMarketPacks.ps1')
    $block=[regex]::Match($source,'(?s)\$assortment\s*=\s*@\((.*?)\r?\n\)').Groups[1].Value
    $names=@([regex]::Matches($block,"'([^']+)'\s*,?")|ForEach-Object {$_.Groups[1].Value})
    if($names.Count -ne 30){throw 'Unexpected package catalog'}
    if(!$OutputDirectory){$OutputDirectory=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'}
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    try{foreach($i in @(0,2,5,9,15,27,28)){for($v=0;$v -lt 6;$v++){
        $size=if($i -eq 28){PaperPrintSize $v}else{@(768,768)}
        $b=[Drawing.Bitmap]::new($size[0],$size[1]);$g=[Drawing.Graphics]::FromImage($b)
        try{Draw-RetailRefresh $g $i $v $names[$i].Split('|')[$v];Draw-PaperRefresh $g $i $v $names[$i].Split('|')[$v];$b.Save((Join-Path $OutputDirectory ('pack_{0:D2}_{1}.png' -f $i,$v)),[Drawing.Imaging.ImageFormat]::Png)}finally{$g.Dispose();$b.Dispose()}
    }}}finally{foreach($photo in $stockPhotos.Values){$photo.Dispose()}}
    Write-Output 'Refreshed 42 original packaging faces.'
}
