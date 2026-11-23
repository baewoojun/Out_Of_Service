param([switch]$RefreshFiles)
Add-Type -AssemblyName System.Drawing

# Original vector artwork. Product photographs are reference only, never package textures.
function Draw-BeverageLabel($g,[int]$category,[int]$variant,[string]$name) {
    if($category -notin @(10,11,12,14)){return}
    $state=$g.Save()
    $g.ScaleTransform(($g.VisibleClipBounds.Width/768),($g.VisibleClipBounds.Height/512))
    $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint=[Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $owned=[Collections.Generic.List[IDisposable]]::new()
    function B([string]$color) { $b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($color)); $owned.Add($b); return $b }
    function T([string]$text,[int]$size,$brush,[float]$x,[float]$y,[float]$w,[float]$h,[bool]$bold=$false,[string]$face='Malgun Gothic') {
        $f=[Drawing.Font]::new($face,$size,($(if($bold){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular})),[Drawing.GraphicsUnit]::Pixel)
        $sf=[Drawing.StringFormat]::new();$sf.Alignment=[Drawing.StringAlignment]::Center;$sf.LineAlignment=[Drawing.StringAlignment]::Center
        try{$g.DrawString($text,$f,$brush,[Drawing.RectangleF]::new($x,$y,$w,$h),$sf)}finally{$f.Dispose();$sf.Dispose()}
    }
    $white=B '#FFFDF7';$navy=B '#173B56';$muted=B '#547483'
    try {
        if($category -eq 10){
            $colors=@('#1169A5','#17668A','#377B75','#234F7D','#167C9B','#326C4F')
            $accent=B $colors[$variant];$pale=B '#E8F1F0';$green=B '#86A79A'
            $g.Clear([Drawing.ColorTranslator]::FromHtml('#F7FAF5'))
            $g.FillRectangle($accent,0,0,768,22)
            if($variant%3 -eq 0){
                $g.FillPolygon($pale,[Drawing.Point[]]@([Drawing.Point]::new(0,180),[Drawing.Point]::new(160,50),[Drawing.Point]::new(260,140),[Drawing.Point]::new(414,27),[Drawing.Point]::new(600,175),[Drawing.Point]::new(768,65),[Drawing.Point]::new(768,220),[Drawing.Point]::new(0,220)))
                $g.FillPolygon($green,[Drawing.Point[]]@([Drawing.Point]::new(350,170),[Drawing.Point]::new(414,88),[Drawing.Point]::new(498,180)))
            }elseif($variant%3 -eq 1){
                for($j=0;$j -lt 5;$j++){
                    $pen=[Drawing.Pen]::new($green.Color,3)
                    try{$g.DrawArc($pen,(-110+$j*25),(35+$j*12),980,200,184,165)}finally{$pen.Dispose()}
                }
            }else{
                $g.FillEllipse($pale,258,44,252,340)
                $g.FillEllipse($green,359,63,50,70)
                $g.FillEllipse($white,370,64,35,50)
            }
            T '샘결  |  NATURAL MINERAL WATER' 22 $navy 10 27 748 42
            T $name 115 $accent 20 145 728 157 $true
            T @('자연에서 온 맑은 한 모금','깊은 바위층의 깨끗한 물','산뜻하게 채우는 하루','물 본연의 맑은 맛','가볍고 깨끗하게','숲을 닮은 맑은 물')[$variant] 25 $muted 20 310 728 50
            $g.FillRectangle($accent,0,403,768,109)
            T ('먹는샘물   ·   '+$(if($variant%2 -eq 0){'2 L'}else{'500 mL'})) 30 $white 20 407 728 52 $true
            T 'SAMGYEOL     /     깨끗한 일상의 시작' 18 $white 20 466 728 32
        }elseif($category -eq 11){
            $colors=@('#A90E29','#116B46','#161C24','#E0690E','#EAF3F1','#B89545');$accent=B $colors[$variant]
            $g.Clear($accent.Color)
            $ink=if($variant -eq 4){$navy}else{$white}
            if($variant -in @(0,2)){
                $deep=B $(if($variant -eq 2){'#94122C'}else{'#810C25'})
                $g.FillPolygon($deep,[Drawing.Point[]]@([Drawing.Point]::new(0,320),[Drawing.Point]::new(768,150),[Drawing.Point]::new(768,270),[Drawing.Point]::new(0,440)))
                T 'BAMBIT' 35 $white 30 38 708 54 $true
                T $(if($variant -eq 0){'밤빛 콜라'}else{'밤빛 ZERO'}) 100 $white 5 120 758 178 $true
                T $(if($variant -eq 0){'ORIGINAL COLA'}else{'ZERO SUGAR COLA'}) 31 $white 20 299 728 56 $true
                T '톡 쏘는 짜릿함' 25 $white 20 359 728 46
            }elseif($variant -eq 1){
                $lime=B '#C4D96B';$pale=B '#DAE4BB'
                $g.FillEllipse($lime,502,30,219,219);$g.FillEllipse($white,516,44,191,191);$g.FillEllipse($pale,530,58,163,163)
                for($j=0;$j -lt 8;$j++){$a=$j*[math]::PI/4;$pen=[Drawing.Pen]::new($white.Color,5);try{$g.DrawLine($pen,611,139,[float](611+[math]::Cos($a)*73),[float](139+[math]::Sin($a)*73))}finally{$pen.Dispose()}}
                T 'SAMGYEOL / CITRUS' 23 $white 20 40 480 46
                T '청귤소다' 105 $white 10 170 748 140 $true
                T 'CITRUS SPARKLING' 30 $white 15 324 738 48
            }elseif($variant -eq 3){
                $orange=B '#FBC648';$leaf=B '#33733C'
                $g.FillEllipse($orange,511,48,205,205);$g.FillEllipse($leaf,557,26,100,40)
                T '샘결 과일탄산' 26 $white 20 42 450 54
                T '감귤소다' 105 $white 10 172 748 138 $true
                T 'MANDARIN  /  SPARKLING' 29 $white 15 324 738 50
            }elseif($variant -eq 4){
                $blue=B '#4B8EA4';$g.FillEllipse($white,-80,140,920,300)
                T '부드러운 탄산' 29 $blue 20 58 728 60
                T '밀크소다' 103 $navy 10 163 748 145 $true
                T 'MILK SODA' 33 $blue 20 327 728 54 $true
            }else{
                $brown=B '#493727';$g.FillRectangle($brown,0,48,768,63)
                T 'SAMGYEOL CRAFT SODA' 25 $white 20 51 728 55
                T '진저소다' 106 $brown 10 170 748 140 $true
                T 'GINGER SPARKLING' 29 $brown 20 324 728 50
            }
            T ('탄산음료  /  '+$(if($variant -eq 5){'250 mL'}else{'500 mL'})) 24 $ink 20 450 728 45
        }elseif($category -eq 12){
            $colors=@('#1467B3','#347348','#492B65','#AFCB51','#E29519','#152338');$accent=B $colors[$variant]
            $g.Clear($accent.Color);$pale=B '#DCECE8'
            $g.FillPolygon($pale,[Drawing.Point[]]@([Drawing.Point]::new(0,390),[Drawing.Point]::new(768,220),[Drawing.Point]::new(768,305),[Drawing.Point]::new(0,475)))
            T 'SAMGYEOL ACTIVE' 25 $white 20 30 728 52 $true
            T @('BLUE WAVE','GREEN WAVE','BERRY WAVE','LIME RESET','ORANGE RUN','POWER BLACK')[$variant] 62 $white 10 105 748 100 $true
            T $name 60 $white 20 211 728 100 $true
            T 'ION BALANCE' 30 $navy 20 355 728 60 $true
            T ('혼합음료   /   '+$(if($variant -eq 5){'250 mL'}else{'500 mL'})) 22 $white 20 453 728 42
        }else{
            $colors=@('#D6C281','#B49767','#608367','#877044','#B9A17C','#C9B586');$accent=B $colors[$variant]
            $g.Clear([Drawing.ColorTranslator]::FromHtml('#F3EEDB'));$g.FillRectangle($accent,0,0,208,512)
            $grain=B '#766036';$leaf=B '#2F6342'
            for($j=0;$j -lt 5;$j++){
                $x=45+$j*29;$y=280+($j%2)*30
                $pen=[Drawing.Pen]::new($grain.Color,3);try{$g.DrawLine($pen,$x,480,$x+26,$y)}finally{$pen.Dispose()}
                for($k=0;$k -lt 4;$k++){$g.FillEllipse($(if($variant -eq 2){$leaf}else{$grain}),$x-5+$k*5,$y+$k*26,22,40)}
            }
            T '다담茶房' 31 $navy 218 32 530 65 $true 'Batang'
            T $name 84 $navy 213 138 545 170 $true 'Batang'
            T @('부드럽게 볶은 옥수수','구수한 보리의 향','향긋하게 우린 녹차','은은하게 우린 열매','통보리의 깊은 풍미','매일 마시는 곡물차')[$variant] 23 $navy 219 327 529 76
            T '차음료   /   500 mL' 24 $navy 219 447 529 45
        }
    }finally{foreach($item in $owned){$item.Dispose()};$g.Restore($state)}
}

if($RefreshFiles){
    $names=@{10='솔샘|바위샘|산들샘|깊은샘|맑은샘|숲속샘';11='밤빛콜라|청귤소다|제로콜라|감귤소다|밀크소다|진저소다';12='블루웨이브|그린웨이브|베리웨이브|라임리셋|오렌지런|파워블랙';14='옥수수차|구운보리|세작녹차|헛개열매|통보리|혼합곡차'}
    foreach($category in @(10,11,12,14)){for($v=0;$v -lt 6;$v++){
        $bitmap=[Drawing.Bitmap]::new(768,512);$g=[Drawing.Graphics]::FromImage($bitmap)
        try{Draw-BeverageLabel $g $category $v $names[$category].Split('|')[$v];$bitmap.Save((Join-Path $PSScriptRoot ('../Assets/OutOfService/Art/Packs/pack_{0:D2}_{1}.png' -f $category,$v)),[Drawing.Imaging.ImageFormat]::Png)}finally{$g.Dispose();$bitmap.Dispose()}
    }}
    Write-Output 'Refreshed 24 original beverage labels.'
}
