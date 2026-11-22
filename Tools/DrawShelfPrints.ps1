# Original retail print separations. The photos used here are credited individually.
$recipeFolder=Join-Path $PSScriptRoot 'RetailPhotoSources'
$printRecipes=@(Get-Content -Encoding utf8 (Join-Path $recipeFolder ShelfPrintRecipes.txt) | Where-Object {$_ -and !$_.StartsWith('#')})
$stockPhotos=@{}

function PackCaption($g,[string]$value,$box,[float]$size,$paint,$fontName='Malgun Gothic',$outline=$null,[float]$angle=0) {
    if(!$value){return}
    $path=[Drawing.Drawing2D.GraphicsPath]::new()
    $font=[Drawing.FontFamily]::new($fontName)
    $pen=$null
    $m=[Drawing.Drawing2D.Matrix]::new()
    try {
        $path.AddString($value,$font,[int][Drawing.FontStyle]::Bold,$size,[Drawing.PointF]::new(0,0),[Drawing.StringFormat]::GenericTypographic)
        $b=$path.GetBounds()
        if($b.Width -lt 1){return}
        $factor=[math]::Min($box.Height/$b.Height,$box.Width/$b.Width)
        $m.Translate($box.X+$box.Width*.5,$box.Y+$box.Height*.5)
        $m.Rotate($angle)
        $m.Scale($factor,$factor)
        $m.Translate(-$b.X-$b.Width*.5,-$b.Y-$b.Height*.5)
        $path.Transform($m)
        if($null -ne $outline){
            $pen=[Drawing.Pen]::new($outline,6.0);$pen.LineJoin='Round'
            $g.DrawPath($pen,$path)
        }
        $g.FillPath($paint,$path)
    }finally{if($pen){$pen.Dispose()};$font.Dispose();$m.Dispose();$path.Dispose()}
}

function Write-HardwareCards($folder){
    $names=@('접시머리 나사','목공 나사','LED 전구','LED 전구','투명 보안경','작업 보안경')
    $specs=@('3 × 25 mm','4 × 32 mm','8 W   |   E26','10 W   |   E26','클리어 렌즈','스모크 렌즈')
    $details=@('5개입','5개입','주광색 6500 K','전구색 3000 K','일반형   1개','랩어라운드   1개')
    foreach($n in 0..5){
        $b=[Drawing.Bitmap]::new(300,440);$g=[Drawing.Graphics]::FromImage($b)
        $ink=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml(@('#8e3826','#415765','#00668b','#ad7737','#2d4640','#54415e')[$n]))
        $g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::FloralWhite)
        $g.FillRectangle($ink,0,0,300,122)
        PackCaption $g '바른공방' ([Drawing.RectangleF]::new(12,9,91,22)) 22 ([Drawing.Brushes]::White)
        PackCaption $g $names[$n] ([Drawing.RectangleF]::new(12,45,276,60)) 48 ([Drawing.Brushes]::White)
        if($n -in @(2,3)){
            $g.FillEllipse($ink,93,143,114,107);$g.FillRectangle($ink,126,231,48,49)
            for($j=0;$j -lt 3;$j++){$g.FillRectangle([Drawing.Brushes]::SlateGray,128,277+$j*7,44,4)}
        }
        PackCaption $g $specs[$n] ([Drawing.RectangleF]::new(9,330,282,40)) 37 $ink
        PackCaption $g $details[$n] ([Drawing.RectangleF]::new(16,384,268,28)) 24 ([Drawing.Brushes]::Black)
        $b.Save((Join-Path $folder ("HardwareCard"+$n+".png")),[Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose();$b.Dispose();$ink.Dispose()
    }
}

function RetailPhoto($g,$key,$box,$shape,[float]$focus=.5) {
    if($key -eq '-'){return}
    if(!$stockPhotos.ContainsKey($key)){
        $source=if($key.StartsWith('=')){Join-Path $recipeFolder ($key.Substring(1)+'.jpg')}else{Join-Path (Join-Path $recipeFolder Ingredients) ($key+'.jpg')}
        $stockPhotos[$key]=[Drawing.Image]::FromFile($source)
    }
    $image=$stockPhotos[$key]
    $crop=[math]::Min($image.Width/$box.Width,$image.Height/$box.Height)
    $w=$box.Width*$crop;$h=$box.Height*$crop
    $src=[Drawing.RectangleF]::new(($image.Width-$w)*.5,($image.Height-$h)*$focus,$w,$h)
    $state=$g.Save()
    if($shape){$g.SetClip($shape)}
    try{$g.DrawImage($image,$box,$src,[Drawing.GraphicsUnit]::Pixel)}
    finally{$g.Restore($state)}
}

function Draw-ShelfPrint($g,$i,$v,$name,$brand){
    $cols=$printRecipes[$i].Split('|')
    $photo=$cols[0].Split(' ')[$v];$hue=$cols[1].Split(' ')[$v]
    $style=$cols[2].Split(' ')[$v];$amount=$cols[3].Split(' ')[$v]
    if($style -eq 'tofu'){return} # Split two-pack and its perforation have a separate print.
    $c=[Drawing.ColorTranslator]::FromHtml('#'+$hue)
    $base=[Drawing.SolidBrush]::new($c)
    $dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb([int]($c.R*.36),[int]($c.G*.38),[int]($c.B*.4)))
    $vanilla=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(253,246,224))
    $pale=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb([int](180+$c.R*.285),[int](178+$c.G*.29),[int](172+$c.B*.30)))
    $white=[Drawing.Brushes]::White;$black=[Drawing.Brushes]::Black
    $shape=[Drawing.Drawing2D.GraphicsPath]::new()
    $hair=[Drawing.Pen]::new($pale,2)
    $g.SmoothingMode='AntiAlias';$g.InterpolationMode='HighQualityBicubic'
    $g.Clear($c)
    function PrintBox($x,$y,$w,$h){[Drawing.RectangleF]::new($x,$y,$w,$h)}
    $box=PrintBox 0 198 512 314
    $title=PrintBox 25 80 462 101
    $text=$white;$edge=$dark;$type='Malgun Gothic';$tilt=0
    $brandBox=PrintBox 24 22 162 28
    $brandInk=$white
    $caption=@('봉지면','용기면','전자레인지용','국·탕·찌개','통조림','스낵','오븐 과자','초콜릿','아침 시리얼','견과류','먹는샘물','탄산음료','스포츠 음료','과일 주스','차 음료','로스팅 원두','스틱 커피','커피 음료','전통차','양념','냉동만두','아이스크림','냉동 간편식','냉동 과채','냉장 반찬','치즈·유가공품','냉장 유제품','생활 세정용품','위생용품','반려동물용')[$i]
    try{
        switch($style){
            'burst' {
                for($n=0;$n -lt 14;$n++){
                    $a=$n*[math]::PI/7
                    $ray=[Drawing.PointF[]]@([Drawing.PointF]::new(320,344),[Drawing.PointF]::new(320+720*[math]::Cos($a),344+720*[math]::Sin($a)),[Drawing.PointF]::new(320+720*[math]::Cos($a+.1),344+720*[math]::Sin($a+.1)))
                    $g.FillPolygon($dark,$ray)
                }
                $shape.AddEllipse(-34,195,617,398);$box=PrintBox -34 195 617 398
                $title=PrintBox 22 71 467 127;$tilt=-5
            }
            'pan' {
                $shape.AddEllipse(-76,160,665,535);$box=PrintBox -76 160 665 438
                $g.FillEllipse($vanilla,-82,155,680,540)
                $title=PrintBox 27 58 455 102
            }
            'paper' {
                $g.Clear($vanilla.Color);$g.FillRectangle($base,0,0,23,512)
                $shape.AddBezier(29,271,126,171,384,173,512,211)
                $shape.AddLine(512,211,512,512);$shape.AddLine(512,512,29,512);$shape.CloseFigure()
                $box=PrintBox 29 180 483 332;$title=PrintBox 42 73 437 99
                $text=$dark;$edge=$null;$brandInk=$base;$type='Batang'
            }
            'dark' {
                $g.Clear($dark.Color);$shape.AddPolygon([Drawing.PointF[]]@([Drawing.PointF]::new(0,247),[Drawing.PointF]::new(512,139),[Drawing.PointF]::new(512,512),[Drawing.PointF]::new(0,512)))
                $g.DrawLine($hair,0,235,512,126);$box=PrintBox 0 140 512 372
                $title=PrintBox 26 65 435 107;$text=$vanilla;$type='Batang'
            }
            'slash' {
                $g.FillPolygon($vanilla,[Drawing.Point[]]@([Drawing.Point]::new(0,58),[Drawing.Point]::new(512,18),[Drawing.Point]::new(512,190),[Drawing.Point]::new(0,250)))
                $shape.AddEllipse(40,195,561,415);$box=PrintBox 40 195 561 415
                $title=PrintBox 23 83 463 100;$tilt=-6;$text=$dark;$edge=$null
            }
            'window' {
                $shape.AddBezier(22,173,155,136,395,132,491,181)
                $shape.AddLine(491,181,491,444);$shape.AddBezier(491,444,375,489,113,489,22,449);$shape.CloseFigure()
                $box=PrintBox 20 144 473 338;$g.FillPath($pale,$shape);$g.DrawPath($hair,$shape)
                $title=PrintBox 25 67 459 70
            }
            'oval' {
                $g.Clear($pale.Color);$box=PrintBox -20 154 552 366;$shape.AddRectangle($box)
                $g.FillEllipse($base,-40,-91,588,316);$title=PrintBox 51 73 409 89
            }
            'seal' {
                $g.Clear($dark.Color)
                for($n=0;$n -lt 11;$n++){$g.DrawArc($hair,-170+$n*43,55,480,536,-95,145)}
                $box=PrintBox 219 146 343 394;$shape.AddEllipse($box)
                $title=PrintBox 23 82 420 91;$type='Batang'
            }
            'round' {
                $g.Clear($vanilla.Color)
                $g.FillEllipse($base,-40,-170,592,430);$shape.AddEllipse(62,214,388,220)
                $box=PrintBox 62 214 388 220;$brandBox=PrintBox 151 54 210 26
                $title=PrintBox 58 100 396 97
            }
            'ice' {
                $box=PrintBox 223 -9 316 532;$shape.AddEllipse($box)
                $title=PrintBox 16 142 323 122;$tilt=-5
            }
            'stamp' {
                $g.Clear($vanilla.Color);$g.FillRectangle($base,0,0,512,53)
                $box=PrintBox 174 192 338 329;$shape.AddRectangle($box)
                $g.DrawRectangle($hair,13,69,485,391);$title=PrintBox 22 90 455 93
                $text=$dark;$edge=$null;$type='Batang'
            }
            {$_ -in @('water','ribbon','sport','clean','dairy')} {
                if($style -in @('water','dairy','clean')){$g.Clear($pale.Color)}
                for($n=0;$n -lt 4;$n++){
                    $a=95+$v*9+$n*45
                    $g.DrawBezier($hair,-20,$a,143,($a+87),302,($a-80),530,($a+5))
                }
                if($photo -ne '-'){$box=PrintBox 278 138 268 265;$shape.AddEllipse($box)}
                $g.FillPolygon($base,[Drawing.Point[]]@([Drawing.Point]::new(0,119),[Drawing.Point]::new(512,76),[Drawing.Point]::new(512,210),[Drawing.Point]::new(0,267)))
                $title=PrintBox 22 135 437 83;$tilt=-4
                if($style -in @('water','clean','dairy')){$brandInk=$dark}
            }
            default {
                $g.Clear($vanilla.Color);$g.FillRectangle($base,0,0,512,65)
                $box=PrintBox 199 186 333 325;$shape.AddEllipse($box)
                $title=PrintBox 26 94 451 90;$text=$dark;$edge=$null
            }
        }
        # Merchandising families use different print dies. Put the title across the
        # chip pouch, in a biscuit tin seal or on a frozen meal's lower paper sleeve.
        $die='standard'
        if($photo -ne '-'){
            if(($i -eq 5 -and $v -in @(1,2,4)) -or ($i -eq 7 -and $v -in @(1,3))){$die='racingBand'}
            elseif(($i -eq 6 -and $v -in @(1,2,4)) -or ($i -eq 20 -and $v -in @(0,3))){$die='medallion'}
            elseif(($i -in @(20,22,23) -and $v -in @(1,2,4,5))){$die='deliSleeve'}
            if($die -ne 'standard'){
                $shape.Reset();$box=PrintBox 0 0 512 512;$shape.AddRectangle($box)
            }
            RetailPhoto $g $photo $box $shape
            switch($die){
                'racingBand' {
                    $g.FillPolygon($base,[Drawing.Point[]]@([Drawing.Point]::new(0,83),[Drawing.Point]::new(512,15),[Drawing.Point]::new(512,161),[Drawing.Point]::new(0,291)))
                    $g.DrawLine($hair,0,298,512,167)
                    $brandBox=PrintBox 24 93 135 22;$title=PrintBox 24 122 454 93;$tilt=-10
                    $brandInk=$white;$text=$white;$edge=$dark
                }
                'medallion' {
                    $g.FillEllipse($base,17,25,257,243);$g.DrawEllipse($hair,24,32,243,229)
                    $brandBox=PrintBox 63 69 165 23;$title=PrintBox 40 104 210 88;$tilt=0
                    $brandInk=$vanilla;$text=$white;$edge=$null;$type='Batang'
                }
                'deliSleeve' {
                    $g.FillPolygon($vanilla,[Drawing.Point[]]@([Drawing.Point]::new(0,338),[Drawing.Point]::new(512,276),[Drawing.Point]::new(512,512),[Drawing.Point]::new(0,512)))
                    $g.FillRectangle($base,0,345,17,120)
                    $brandBox=PrintBox 29 343 155 24;$title=PrintBox 30 369 446 66;$tilt=0
                    $brandInk=$base;$text=$dark;$edge=$null;$type='Batang'
                }
            }
        }
        elseif($i -eq 23){
            # Individually frozen kernels, carrot cubes and peas viewed through the print window.
            $rng=[Random]::new(240);$green=[Drawing.Brushes]::OliveDrab;$orange=[Drawing.Brushes]::Coral
            $g.FillRectangle($pale,0,242,512,270)
            for($n=0;$n -lt 250;$n++){$x=$rng.Next(-6,508);$y=$rng.Next(250,514);switch($n%3){
                0{$g.FillEllipse($green,$x,$y,24,22)} 1{$g.FillRectangle($orange,$x,$y,24,20)} 2{$g.FillEllipse([Drawing.Brushes]::Gold,$x,$y,20,13)}
            }}
        }elseif($i -in @(10,12)){
            for($n=0;$n -lt 4;$n++){$y=310+$n*15;$g.DrawBezier($hair,0,$y,188,($y-95),295,($y+88),512,($y-18))}
        }
        if($i -eq 10){
            $g.Clear($pale.Color)
            switch($v%3){
                0 {
                    for($n=0;$n -lt 7;$n++){
                        $y=295+$n*12
                        $g.DrawBezier($hair,-60,$y,155,($y-96),322,($y+72),565,($y-24))
                    }
                    $g.FillPolygon($base,[Drawing.Point[]]@([Drawing.Point]::new(0,338),[Drawing.Point]::new(123,214),[Drawing.Point]::new(190,272),[Drawing.Point]::new(278,199),[Drawing.Point]::new(512,373),[Drawing.Point]::new(512,460),[Drawing.Point]::new(0,460)))
                    $title=PrintBox 39 75 395 120
                }
                1 {
                    $g.FillRectangle($base,0,0,113,512)
                    $g.DrawEllipse($hair,155,86,284,284);$g.DrawEllipse($hair,166,97,262,262)
                    $title=PrintBox 128 159 350 107
                }
                2 {
                    $g.FillEllipse($base,-85,238,692,420);$g.FillEllipse($pale,-80,215,684,163)
                    $title=PrintBox 28 129 445 109
                }
            }
            $text=$dark;$brandInk=$dark;$edge=$null;$tilt=0
        }
        if($i -eq 1 -and $v -in @(1,2,4)){
            $g.Clear($dark.Color);$box=PrintBox 188 69 285 286;$shape.Reset();$shape.AddEllipse($box)
            RetailPhoto $g $photo $box $shape
            $g.FillPolygon($base,[Drawing.Point[]]@([Drawing.Point]::new(0,183),[Drawing.Point]::new(512,301),[Drawing.Point]::new(512,404),[Drawing.Point]::new(0,306)))
            $title=PrintBox 38 226 426 88;$text=$white;$edge=$dark;$tilt=8
        }
        if($i -eq 20 -and $v -in @(2,4) -or $i -eq 23 -and $v -in @(4,5)){
            $g.Clear($base.Color);$box=PrintBox 125 73 402 384;$shape.Reset();$shape.AddEllipse($box)
            RetailPhoto $g $photo $box $shape
            $g.FillRectangle($vanilla,0,0,124,462)
            $brandBox=PrintBox 139 19 148 29;$brandInk=$white
            $chars=$name.ToCharArray();$step=[math]::Min(75,350/$chars.Count)
            for($j=0;$j -lt $chars.Count;$j++){
                PackCaption $g ([string]$chars[$j]) (PrintBox 15 (43+$j*$step) 92 ($step-4)) 66 $dark 'Batang'
            }
            $name=''
        }
        if($i -eq 0 -and $v -eq 4){
            $g.Clear($dark.Color);$g.FillEllipse($vanilla,17,188,517,309)
            $box=PrintBox 24 193 502 304;$shape.Reset();$shape.AddEllipse($box);RetailPhoto $g $photo $box $shape
            $title=PrintBox 25 62 462 135;$text=$vanilla;$edge=$null;$brandInk=$white
        }
        if($i -eq 28 -and $v -in @(0,4,5)){
            $g.Clear($pale.Color);$g.FillEllipse($base,178,-96,465,461)
            $ply=@('3겹','', '', '', '2겹','도톰한 3겹')[$v]
            PackCaption $g $ply (PrintBox 226 39 267 83) 63 $white
            $title=PrintBox 24 235 457 108;$text=$dark;$edge=$null;$brandInk=$base
        }
        if($i -eq 18 -and $v -eq 4){
            $g.Clear($dark.Color);$box=PrintBox 255 165 267 297;$shape.Reset();$shape.AddEllipse($box);RetailPhoto $g $photo $box $shape
            $g.DrawRectangle($hair,15,14,482,436);$title=PrintBox 23 97 411 115;$text=$vanilla;$edge=$null;$brandInk=$white
        }
        if($i -eq 4 -and $v -eq 1){
            $g.Clear($dark.Color);$box=PrintBox 258 105 284 334;$shape.Reset();$shape.AddEllipse($box);RetailPhoto $g $photo $box $shape
            $title=PrintBox 26 118 273 158;$text=$vanilla;$brandInk=$white;$edge=$null
        }
        PackCaption $g $brand $brandBox 30 $brandInk
        PackCaption $g $name $title 80 $text $type $edge $tilt
        if($style -eq 'round'){
            PackCaption $g ($amount+'   ·   '+$caption) (PrintBox 111 407 290 29) 24 $dark
        }else{
            $sealY=462
            $g.FillRectangle($dark,0,$sealY,512,50)
            PackCaption $g $caption (PrintBox 15 474 168 22) 22 $vanilla
            PackCaption $g $amount (PrintBox 363 471 130 27) 25 $white
        }
    }finally{$base.Dispose();$dark.Dispose();$vanilla.Dispose();$pale.Dispose();$hair.Dispose();$shape.Dispose()}
}
