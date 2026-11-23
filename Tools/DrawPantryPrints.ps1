# Original print separations for flexible film, circular lids and farm labels.
Add-Type -AssemblyName System.Drawing
$folder = Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'
$noodles = [Drawing.Bitmap]::new((Join-Path $folder 'ramen_front_atlas_v2.png'))
$bag = '얼큰장국면','닭육수면','바다짬뽕','맑은버섯면','구운짜장','새콤비빔면'
$cup = '육개장사발','해물큰컵','매운컵면','김치사발','가쓰오우동','볶음큰컵'
$rice = '쌀밥','찰보리밥','잡곡밥','현미밥','곤드레밥','흑미밥'
$notes = '고추와 대파','진한 닭육수','얼큰한 해물','담백한 국물','볶음 춘장','새콤한 양념'
$hues = '#9B2928','#98651C','#115466','#496345','#47302B','#9B343D'
$riceHues = '#A43935','#937437','#684857','#9D623E','#48634B','#534257'
$cupWeights='86 g','110 g','65 g','86 g','111 g','70 g'
$cream = '#FFF4D9'
$ink = '#232622'
$palette = @{}
function Brush([string]$color) {
    if (!$palette.ContainsKey($color)) {
        $palette[$color] = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($color))
    }
    $palette[$color]
}
function Sheet([int]$w,[int]$h,[string]$base) {
    $script:bitmap = [Drawing.Bitmap]::new($w,$h)
    $script:g = [Drawing.Graphics]::FromImage($bitmap)
    $g.Clear((Brush $base).Color)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
}
function TypeLine($value,$x,$y,$size,$color,$face='Malgun Gothic') {
    $font=[Drawing.Font]::new($face,$size,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
    $g.DrawString([string]$value,$font,(Brush $color),[single]$x,[single]$y,[Drawing.StringFormat]::GenericTypographic)
    $font.Dispose()
}
function SavePrint($file) {
    $bitmap.Save((Join-Path $folder ($file+'.png')),[Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bitmap.Dispose()
}
function Dish($i,$x,$y,$w,$h) {
    $cw=[int]($noodles.Width/3); $ch=[int]($noodles.Height/2)
    $src=[Drawing.Rectangle]::new(($i%3)*$cw,[int][Math]::Floor($i/3)*$ch,$cw,$ch)
    $g.DrawImage($noodles,[Drawing.Rectangle]::new($x,$y,$w,$h),$src,[Drawing.GraphicsUnit]::Pixel)
}
for($i=0;$i -lt 6;$i++){
    $tone=$hues[$i]
    Sheet 512 512 $tone
    Dish $i 0 125 512 359
    $g.FillRectangle((Brush $tone),0,0,512,157)
    $g.FillRectangle((Brush '#D8AC65'),0,148,512,4)
    TypeLine '골목제면소' 24 13 20 $cream
    TypeLine $bag[$i] 19 43 76 $cream $(if($i%2){'Malgun Gothic'}else{'Batang'})
    $g.FillRectangle((Brush $ink),0,458,512,54)
    TypeLine $notes[$i] 20 469 25 $cream
    if($i%3 -eq 1){TypeLine '120 g' 408 468 24 $cream}
    else{TypeLine '120 g × 5' 365 468 24 $cream}
    SavePrint "NoodleFilm$i"

    Sheet 512 224 $tone
    $g.FillRectangle((Brush $cream),0,42,512,125)
    TypeLine '골목제면소  /  뜨거운 물 조리' 34 8 20 $cream
    TypeLine $cup[$i] 48 55 73 $tone
    TypeLine '용기면' 37 178 24 $cream
    TypeLine $cupWeights[$i] 406 178 24 $cream
    SavePrint "NoodleBand$i"

    Sheet 384 384 $tone
    Dish @(0,2,1,0,2,5)[$i] 4 125 376 213
    $g.FillEllipse((Brush $tone),38,27,308,146)
    TypeLine '골목제면소' 129 48 23 $cream
    TypeLine $cup[$i] $(if($cup[$i].Length -gt 3){70}else{91}) 86 53 $cream
    $g.FillRectangle((Brush $tone),0,304,384,80)
    TypeLine '여기까지 개봉' 121 315 22 $cream
    SavePrint "NoodleLid$i"

    $tone=$riceHues[$i]
    Sheet 384 384 '#F6EFDE'
    $g.FillEllipse((Brush $tone),-96,-133,500,269)
    TypeLine '골목찬방' 127 43 29 $cream
    TypeLine '갓 지은 한 공기' 106 84 22 $cream
    $sz=if($rice[$i].Length -gt 3){57}else{69}
    $left=[int](192-$rice[$i].Length*$sz*.5)
    TypeLine $rice[$i] $left 131 $sz $tone 'Batang'
    # Rice grains form a wreath around the cooking line; each recipe has its own grain mix.
    $rng=[Random]::new(673+$i)
    for($n=0;$n -lt 69;$n++){
        $a=$rng.NextDouble()*2*[Math]::PI;$r=155+$rng.NextDouble()*18
        $x=192+[Math]::Cos($a)*$r;$y=192+[Math]::Sin($a)*$r
        if($y -gt 140){$g.FillEllipse((Brush @('#CFBC96','#C8AA72','#9E8278','#BDA178','#949F7B','#8B798E')[$i]),[single]$x,[single]$y,9,4)}
    }
    $g.FillEllipse((Brush '#D9CEB6'),87,230,210,71)
    $g.FillEllipse((Brush '#FFFCF2'),85,225,214,71)
    $g.FillEllipse((Brush '#C7BBA4'),92,227,200,57)
    $grains=@('#FEF8DE','#C3A16D','#92786A','#C09A6D','#8A9863','#786579')
    for($n=0;$n -lt 1150;$n++){
        $a=$rng.NextDouble()*2*[Math]::PI;$r=[Math]::Sqrt($rng.NextDouble())
        $x=192+95*[Math]::Cos($a)*$r;$y=251+26*[Math]::Sin($a)*$r-6*(1-$r)
        $g.FillEllipse((Brush $(if($rng.NextDouble() -lt $(if($i -eq 5){.7}else{.32})){$grains[$i]}else{'#F3E4C3'})),[single]$x,[single]$y,7,3)
    }
    TypeLine '210 g  |  전자레인지' 92 306 21 $ink
    SavePrint "RiceFoil$i"
    Sheet 192 300 $tone
    TypeLine '골목찬방' 19 21 29 $cream
    $g.FillRectangle((Brush $cream),0,79,192,106)
    TypeLine $rice[$i] 10 98 $(if($rice[$i].Length -gt 3){41}else{53}) $tone 'Batang'
    TypeLine '세 공기 묶음' 12 207 27 $cream
    TypeLine '210 g × 3' 13 250 28 $cream
    SavePrint "RiceBundle$i"
    Sheet 512 168 '#F7F2E8'
    TypeLine $rice[$i] 28 16 71 $tone 'Batang'
    TypeLine '골목찬방   |   210 g' 34 106 30 $ink
    SavePrint "RiceSingle$i"
    Sheet 512 224 '#C5A16E'
    TypeLine '골목제면소   ─   상온' 21 13 26 $ink
    TypeLine $bag[$i] 17 58 66 $ink
    TypeLine '봉지면  /  취급주의' 21 160 31 $ink
    TypeLine '↑↑' 399 144 48 $ink
    SavePrint "NoodleCase$i"
    for($group=0;$group -lt 3;$group++){
        $item=@($bag[$i],$cup[$i],$rice[$i])[$group]
        $weight=@($(if($i%3 -eq 1){'120 g'}else{'120 g × 5'}),$cupWeights[$i],$(if($i%2){'210 g'}else{'210 g × 3'}))[$group]
        $price=if($group -eq 0){if($i%3 -eq 1){'1,180'}else{'4,980'}}elseif($group -eq 1){'1,390'}elseif($i%2){'1,580'}else{'4,280'}
        Sheet 320 104 '#F2F0E6'
        TypeLine $item 9 4 22 $ink
        TypeLine $weight 175 7 16 $ink
        TypeLine ($price+'원') 9 36 44 $ink
        for($n=0;$n -lt 20;$n++){$g.FillRectangle((Brush $ink),223+$n*4,53,1+(($n+$i)%3),22)}
        SavePrint "PantryTicket${group}_$i"
    }
}
$mush='표고버섯','새송이버섯','양송이버섯','느타리버섯','팽이버섯'
$farmInk='#533C2C','#526C46','#3E5053','#634E64','#184E85'
for($i=0;$i -lt 5;$i++){
    $tone=$farmInk[$i]
    Sheet 320 210 $(if($i -eq 4){$tone}else{'#FAF5E7'})
    $fg=if($i -eq 4){$cream}else{$ink}
    $g.FillRectangle((Brush $tone),0,0,320,43)
    TypeLine '들마루 농산  ·  버섯선별장' 12 7 20 $cream
    TypeLine $mush[$i] 11 53 47 $fg
    TypeLine ('국산  /  '+@(300,300,200,200,150)[$i]+' g') 14 122 25 $fg
    $g.FillRectangle((Brush '#F8F6EB'),0,164,320,46)
    TypeLine '냉장보관' 12 172 22 $ink
    for($b=0;$b -lt 40;$b++){ $g.FillRectangle((Brush $ink),147+$b*4,171,1+(($b*7+$i)%3),27) }
    SavePrint "FarmMushroom$i"
}
foreach($b in $palette.Values){$b.Dispose()}
$noodles.Dispose()
