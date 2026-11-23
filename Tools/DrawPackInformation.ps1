function Write-PackInformation($canvas,$front,$folder,$familyId,$variant,$product,$brand,$group) {
    $data=$printRecipes[$familyId].Split('|')
    $amount=$data[3].Split(' ')[$variant]
    $ink=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#'+$data[1].Split(' ')[$variant]))
    $cold=$familyId -in 20..23
    if($familyId -eq 23 -and $variant -in @(0,3,4)){$group='냉동과일'}
    $storage=if($cold){'영하 18°C 이하 냉동'}elseif($familyId -in 24..26){'냉장 보관'}elseif($familyId -ge 27){'건조한 곳에 보관'}else{'직사광선을 피해 보관'}
    $detail=if($cold){'해동 후 다시 얼리지 마세요.'}elseif($familyId -eq 27){'사용 후 뚜껑을 닫아 주세요.'}elseif($familyId -eq 29){'개봉 후 밀봉해 주세요.'}else{'개봉 전 포장 상태를 확인하세요.'}
    $paper=[Drawing.Bitmap]::new(512,512)
    $g=[Drawing.Graphics]::FromImage($paper)
    try {
        $g.Clear([Drawing.Color]::FromArgb(248,246,236));$g.SmoothingMode='AntiAlias';$g.TextRenderingHint='AntiAliasGridFit'
        function Label($value,$x,$y,$w,$h,$paint){PackCaption $g $value ([Drawing.RectangleF]::new($x,$y,$w,$h)) 36 $paint}
        function InfoCopy($value,$x,$y,$w,$h,$size=14,$bold=$false,$paint=[Drawing.Brushes]::Black){
            $f=[Drawing.Font]::new('Malgun Gothic',$size,$(if($bold -or $size -le 15){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular}),[Drawing.GraphicsUnit]::Pixel)
            $hint=$g.TextRenderingHint
            try{$g.TextRenderingHint='SingleBitPerPixelGridFit';$g.DrawString($value,$f,$paint,[Drawing.RectangleF]::new($x,$y,$w,$h))}finally{$g.TextRenderingHint=$hint;$f.Dispose()}
        }
        $black=[Drawing.Brushes]::Black;$white=[Drawing.Brushes]::White
        $luma=(.2126*$ink.Color.R+.7152*$ink.Color.G+.0722*$ink.Color.B)/255
        $contrast=if($luma -gt .50){$black}else{$white}
        $textInk=if($luma -gt .47){$black}else{$ink}
        $food=$familyId -lt 27
        $g.FillRectangle($ink,0,0,376,59)
        Label $product 12 9 350 38 $contrast
        InfoCopy $brand 15 65 200 25 17 $true $textInk
        InfoCopy $amount 239 64 123 25 18 $true
        $g.DrawLine([Drawing.Pens]::DimGray,14,94,361,94)
        if($food){
            $ingredient=@('밀가루, 감자전분, 식용유, 조미분말, 건조채소','밀가루, 전분, 조미분말, 건더기','쌀, 잡곡, 정제수','육수, 채소, 간장, 조미원료','어육 또는 농산물, 정제수, 식염','곡물, 식용유, 조미분말','밀가루, 식용유지, 설탕, 유가공품','코코아, 설탕, 유지, 유가공품','곡물가공품, 설탕, 식염','견과류 및 건조과실','먹는샘물','정제수, 탄산가스, 당류, 향료','정제수, 당류, 혼합미네랄','과즙, 정제수','정제수, 볶은 곡물 또는 찻잎','원두커피','커피, 설탕, 식물성크림','커피추출액, 유가공품, 정제수','농산물가공품, 당류','발효원료, 곡물, 식염, 정제수','밀가루, 육류 또는 채소, 조미원료','유가공품, 설탕, 유지','곡물, 육류 또는 채소, 조미원료','냉동 과일 또는 채소','대두 또는 채소, 조미원료','유가공품, 정제수, 식염','원유 또는 유가공품')[$familyId]
            InfoCopy ('식품유형  '+$group) 15 101 346 24 14 $true
            InfoCopy ('원재료  '+$ingredient) 15 129 346 52 13
            InfoCopy '소비기한  포장 상단 또는 하단 별도 표시일까지' 15 184 346 23 12
            InfoCopy ('보관방법  '+$storage) 15 211 346 25 13 $true
            $g.FillRectangle($black,15,245,168,24);InfoCopy '영양정보' 21 245 150 24 15 $true $white
            $nutrients=@('열량','탄수화물','당류','단백질','지방','나트륨')
            # Fictional art data, never copied nutrition claims from a real SKU.
            $energy=if($familyId -eq 10){0}elseif($familyId -in 11..14){40+5*$variant}else{180+25*(($familyId+$variant)%8)}
            $values=@(($energy.ToString()+' kcal'),((12+$familyId%7).ToString()+' g'),((2+$variant).ToString()+' g'),((3+$variant%4).ToString()+' g'),((1+$variant).ToString()+' g'),((20+10*$variant).ToString()+' mg'))
            if($familyId -eq 10){$values=@('0 kcal','0 g','0 g','0 g','0 g','0 mg')}
            for($r=0;$r -lt 6;$r++){$y=273+$r*20;InfoCopy $nutrients[$r] 20 $y 90 21 12;InfoCopy $values[$r] 107 $y 75 21 12;$g.DrawLine([Drawing.Pens]::LightGray,15,$y+19,183,$y+19)}
            $heading=if($familyId -in @(0,1,2,3,20,22)){'간편 조리 안내'}elseif($familyId -in 5..9){'더 맛있게 즐기는 법'}else{'제품 보관 안내'}
            InfoCopy $heading 197 247 167 27 14 $true $textInk
            $instructions=if($familyId -in @(0,1)){'1  물을 준비합니다.  2  면과 스프를 넣고 조리합니다.  3  포장 앞면의 조리 표시를 확인하세요.'}elseif($familyId -in @(2,3,20,22)){'포장지의 조리 표시를 확인하세요. 가열 후 뜨거운 내용물에 주의하세요.'}elseif($cold){'필요한 양만 꺼내 사용하고 남은 제품은 즉시 밀봉하여 냉동 보관하세요.'}else{'개봉 후에는 밀봉해 보관하세요. 포장에 이상이 있으면 구입처로 문의하세요.'}
            InfoCopy $instructions 197 280 164 111 13
            InfoCopy $detail 15 402 346 29 12
        }else{
            $heading=@('제품 사용 안내','소재와 사용 안내','급여 및 보관 안내')[$familyId-27]
            InfoCopy $heading 15 105 346 29 20 $true $textInk
            $rows=if($familyId -eq 27){@('용도  의류 세탁 또는 주방 세정','사용 전 제품 앞면의 용도를 확인하세요.','적당량을 사용하고 사용 후 밀봉하세요.','다른 세정제와 임의로 혼합하지 마세요.','영유아의 손이 닿지 않는 곳에 보관하세요.')}elseif($familyId -eq 28){@('용도  가정용 위생용품','재질  펄프 / 포장 필름','물기와 습기를 피해 보관하세요.','오염된 제품은 일반쓰레기로 버리세요.','포장 필름은 내용물과 분리해 배출하세요.')}else{@('용도  반려동물용 사료 또는 간식','원료  육류, 곡물 및 부원료','제품 앞면의 대상 동물을 확인하세요.','개봉 후에는 밀봉하여 서늘하게 보관하세요.','변질이나 포장 이상이 있으면 사용하지 마세요.')}
            for($r=0;$r -lt 5;$r++){InfoCopy $rows[$r] 17 (149+$r*43) 343 39 15;$g.DrawLine([Drawing.Pens]::LightGray,15,188+$r*43,360,188+$r*43)}
            InfoCopy $storage 15 377 345 31 15 $true
        }
        $g.FillRectangle([Drawing.Brushes]::White,15,437,212,59)
        $rand=[Random]::new(1800+6*$familyId+$variant)
        for($x=24;$x -lt 216;){$bar=$rand.Next(1,4);$g.FillRectangle($black,$x,444,$bar,34);$x+=$bar+$rand.Next(1,4)}
        InfoCopy ('880  42{0:D2}  {1:D3}' -f $familyId,($variant+1)) 26 478 200 19 12
        $g.DrawEllipse([Drawing.Pens]::DimGray,253,441,34,34)
        InfoCopy '분리배출' 293 441 76 20 11 $true
        InfoCopy $(if($familyId -in @(4,17)){'캔류'}elseif($familyId -in @(6,8,16,18)){'종이'}else{'포장재질 확인'}) 239 477 132 21 12
        InfoCopy '구입처 교환  /  홈웨이 고객센터' 15 498 345 14 10
        $g.FillRectangle($ink,384,0,128,512)
        $chars=$product.ToCharArray();$step=[math]::Min(53,290/$chars.Count)
        InfoCopy $brand 391 15 115 45 17 $true $contrast
        for($n=0;$n -lt $chars.Count;$n++){Label ([string]$chars[$n]) 406 (86+$n*$step) 82 ($step-5) $contrast}
        Label $amount 392 411 112 29 $contrast
        InfoCopy '제품 표시를 확인하세요' 393 463 109 39 12 $false $contrast
        $atlas=[Drawing.Graphics]::FromImage($canvas)
        try{$atlas.DrawImageUnscaled($paper,($variant%4)*512,[int][Math]::Floor($variant/4)*512)}finally{$atlas.Dispose()}
        if($familyId -in @(20,23) -or ($familyId -eq 22 -and $variant -ne 1)){
            $wrap=[Drawing.Bitmap]::new(1024,512);$wg=[Drawing.Graphics]::FromImage($wrap)
            try{
                $wg.DrawImage($front,[Drawing.Rectangle]::new(0,0,512,512));$wg.DrawImageUnscaled($paper,512,0)
                $zip=($familyId -eq 23 -and $variant -in @(0,3,4)) -or ($familyId -eq 20 -and $variant -in @(0,2))
                $pillow=($familyId -eq 22 -and $variant -in @(0,3,5)) -or ($familyId -eq 20 -and $variant -eq 1) -or ($familyId -eq 23 -and !$zip)
                $light=[Drawing.Pen]::new([Drawing.Color]::FromArgb(98,252,248,238),2)
                $shade=[Drawing.Pen]::new([Drawing.Color]::FromArgb(80,54,53,48),2)
                try {
                    $wg.FillRectangle($ink,512,0,512,512)
                    # The information block sits below the pressing jaws, clear of the zipper and the side folds.
                    $wg.DrawImage($paper,[Drawing.Rectangle]::new(535,80,337,389),0,0,376,512,[Drawing.GraphicsUnit]::Pixel)
                    $wg.DrawImage($paper,[Drawing.Rectangle]::new(905,122,112,285),384,0,128,512,[Drawing.GraphicsUnit]::Pixel)
                    if($pillow){$wg.DrawLine($shade,530,18,530,500);$wg.DrawLine($light,533,18,533,500)}
                    if($zip){
                        foreach($y in @(39,48)){$wg.DrawLine($shade,0,$y,1023,$y);$wg.DrawLine($light,0,($y+2),1023,($y+2))}
                        $wg.DrawLine($light,0,11,1023,11)
                    }else{
                        $end=19+4*($variant%3)
                        $wg.DrawLine($shade,0,$end,1023,$end)
                        for($x=1;$x -lt 1024;$x+=5+($variant%2)){$wg.DrawLine($light,$x,1,($x+1),($end-2))}
                    }
                    $wg.DrawLine($shade,0,500,1023,500)
                }finally{$light.Dispose();$shade.Dispose()}
                $wrap.Save((Join-Path $folder ('FrozenFilm_{0}_{1}.png' -f $familyId,$variant)),[Drawing.Imaging.ImageFormat]::Png)
            }finally{$wg.Dispose();$wrap.Dispose()}
        }
    }finally{$ink.Dispose();$g.Dispose();$paper.Dispose()}
    Write-CompactInformation $folder $familyId $variant $product $brand $group $amount $storage
}

function Write-CompactInformation($folder,$familyId,$variant,$product,$brand,$group,$amount,$storage){
    if(!$script:compactPrintAtlases){$script:compactPrintAtlases=@{}}
    $key=$folder+'|'+$familyId
    if($variant -eq 0){$script:compactPrintAtlases[$key]=[Drawing.Bitmap]::new(1536,640)}
    $tile=[Drawing.Bitmap]::new(384,320);$cg=[Drawing.Graphics]::FromImage($tile)
    $data=$printRecipes[$familyId].Split('|');$color=[Drawing.ColorTranslator]::FromHtml('#'+$data[1].Split(' ')[$variant])
    $paint=[Drawing.SolidBrush]::new($color)
    $bright=(.2126*$color.R+.7152*$color.G+.0722*$color.B)/255
    $contrast=if($bright -gt .50){[Drawing.Brushes]::Black}else{[Drawing.Brushes]::White}
    function CompactLine($value,$x,$y,$w,$h,$size,$brush=[Drawing.Brushes]::Black){
        $font=[Drawing.Font]::new('Malgun Gothic',$size,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
        try{$cg.DrawString($value,$font,$brush,[Drawing.RectangleF]::new($x,$y,$w,$h))}finally{$font.Dispose()}
    }
    try{
        $cg.Clear([Drawing.Color]::FromArgb(248,246,236));$cg.TextRenderingHint='SingleBitPerPixelGridFit'
        $cg.FillRectangle($paint,0,0,384,38)
        PackCaption $cg $product ([Drawing.RectangleF]::new(10,3,270,32)) 28 $contrast
        CompactLine $amount 284 9 96 27 16 $contrast
        CompactLine ($brand+'  /  '+$group) 12 46 360 27 15
        CompactLine ('보관  '+$storage) 12 77 360 29 15
        $line=if($familyId -ge 27){'소재·용도  제품 앞면 표시 확인'}else{'소비기한  포장지의 별도 표시일까지'}
        CompactLine $line 12 108 360 25 14
        $cg.DrawLine([Drawing.Pens]::Gray,12,140,371,140)
        $rng=[Random]::new(1800+6*$familyId+$variant)
        for($x=14;$x -lt 201;){$bar=$rng.Next(1,4);$cg.FillRectangle([Drawing.Brushes]::Black,$x,152,$bar,34);$x+=$bar+$rng.Next(1,4)}
        CompactLine ('880 42{0:D2} {1:D3}' -f $familyId,($variant+1)) 14 190 197 20 12
        CompactLine '개봉 후 밀봉 보관' 215 150 163 25 14
        CompactLine '포장재 분리배출' 215 180 163 26 14
        # Independent 4.8:1 strip: no partial line cropped from a vertical label.
        $cg.FillRectangle($paint,0,240,384,80)
        PackCaption $cg $product ([Drawing.RectangleF]::new(12,250,264,38)) 30 $contrast
        CompactLine $amount 282 259 99 30 17 $contrast
        CompactLine $brand 14 292 356 24 13 $contrast
        $ag=[Drawing.Graphics]::FromImage($script:compactPrintAtlases[$key])
        try{$ag.DrawImageUnscaled($tile,($variant%4)*384,[int][Math]::Floor($variant/4)*320)}finally{$ag.Dispose()}
    }finally{$paint.Dispose();$cg.Dispose();$tile.Dispose()}
    if($variant -eq 5){
        $script:compactPrintAtlases[$key].Save((Join-Path $folder ('CompactInformation'+$familyId+'.png')))
        $script:compactPrintAtlases[$key].Dispose();$script:compactPrintAtlases.Remove($key)
    }
}

