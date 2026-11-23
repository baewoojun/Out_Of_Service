# Offset-print graphics and panels authored for the fictional retailer.
# Called by GenerateMarketPacks after the family artwork is drawn.
function Draw-ConsumerPanel($g,$i,$v,$name,$brand,$family,$format) {
    $families=@{
        3='국물요리|정성으로 끓인|실온 보관';4='식탁 통조림|간편하게 즐기는|실온 보관';
        5='바삭상점|바삭하게 즐기는|스낵';6='오븐 베이크|작은 오븐의|비스킷';7='카카오 공방|달콤한 한 조각|초콜릿';
        8='아침곡물|오늘 아침 한 그릇|시리얼';9='한 줌 견과|매일 챙기는|견과';
        13='과수원 주스|과일의 풍미를 담은|과일 음료';
        15='로스터리|천천히 볶은|원두커피';16='커피스틱|간편하게 즐기는|커피믹스';
        18='다담차방|향을 담은|전통차';19='찬방 양념|우리 식탁의|조미식품';
        20='서리교자|한입 가득|냉동 만두';21='여름다방|시원하게 즐기는|아이스크림';
        22='저녁한끼|갓 차린 듯|냉동 간편식';23='산지소분|필요한 만큼 꺼내 쓰는|냉동 보관';
        24='들콩찬방|오늘의 냉장 반찬|냉장 보관';25='목장식탁|부드럽고 고소한|냉장 보관';
        29='꼬리식탁|우리 집 반려동물|반려동물용'
    }
    if(!$families.ContainsKey($i)){return}
    $parts=$families[$i].Split('|')
    $cold=$i -in 20..25
    $light=$i -in @(4,6,8,9,13,16,18,23,24,25)
    $dark=[Drawing.Brushes]::MidnightBlue
    $white=[Drawing.Brushes]::White
    $ink=if($light){$dark}else{$white}
    $pal=if($i -eq 24){@('#508428','#67A33A','#A43630','#63926E','#CE5129','#528A54')}
         elseif($i -eq 20){@('#244936','#B1622A','#A42C25','#47762E','#6A573B','#945225')}
         elseif($i -eq 21){@('#82AC54','#B88B54','#679AB0','#AD8A58','#613B37','#914666')}
         elseif($i -eq 7 -or $i -eq 15){@('#4D2D24','#674A32','#383136','#745142','#35282C','#875736')}
         else{@('#C34132','#53794D','#D4872E','#376C83','#9B483F','#696094')}
    $tint=[Drawing.ColorTranslator]::FromHtml($pal[$v]);$brush=[Drawing.SolidBrush]::new($tint)
    $paper=[Drawing.Color]::FromArgb(250,246,231)
    $typeface=if($i -in @(3,18,19,20,24)){'Batang'}else{'Malgun Gothic'}
    $big=[Drawing.Font]::new($typeface,($(if($name.Length -gt 5){62}else{72})),[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
    $caption=[Drawing.Font]::new($family,24,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
    $body=[Drawing.Font]::new($family,18,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
    $g.Clear($(if($light){$paper}else{$tint}))
    if($i -eq 24 -and $v -in @(0,5)){
        $g.FillRectangle($brush,0,287,512,225)
        $g.DrawString($parts[0],$caption,$brush,23,21)
        $g.DrawString($(if($v -eq 0){'단단한 부침용'}else{'부드러운 한 끼'}),$caption,$dark,25,79)
        $g.DrawString($name,$big,$dark,[Drawing.RectangleF]::new(8,114,496,156),$format)
        $g.DrawString($(if($v -eq 0){'부침두부   300 g × 2'}else{'연두부   250 g'}),$caption,$white,20,302)
        $g.DrawString('냉장 보관   |   개봉 후 바로 조리',$body,$white,21,359)
        $g.FillRectangle($white,274,403,211,76)
        Draw-ScanLines $g 283 412 45
    } else {
        $banner=if($light){$brush}else{$white}
        $g.FillRectangle($banner,0,0,512,68)
        $g.DrawString($parts[0],$caption,$(if($light){$white}else{$brush}),20,14)
        $g.DrawString($parts[1],$body,$ink,23,83)
        # Preserve the established illustration only on the representative SKU.
        # Other flavours carry a dedicated typography panel, not the wrong food photograph.
        $sourceName=switch ("$i/$v") {
            '9/1' {'Almonds.jpg'}
            '23/0' {'Blueberries.jpg'}
            '23/1' {'Broccoli.jpg'}
        }
        if($sourceName){
            $photo=[Drawing.Image]::FromFile((Join-Path $PSScriptRoot ('RetailPhotoSources/'+$sourceName)))
            try{
                $destination=[Drawing.Rectangle]::new(0,278,512,179)
                $height=[int][math]::Min($photo.Height,$photo.Width*179/512)
                $cy=if($i -eq 23 -and $v -eq 0){.70}else{.53}
                $top=[int](($photo.Height-$height)*$cy)
                $region=[Drawing.Rectangle]::new(0,$top,$photo.Width,$height)
                $g.DrawImage($photo,$destination,$region,[Drawing.GraphicsUnit]::Pixel)
            }finally{$photo.Dispose()}
        }elseif($v -eq 0 -and $i -notin @(21,23,25)){
            $source=if($i -lt 16){$atlas1}else{$atlas2}
            $index=$i%16;$cell=[int]($source.Width/4)
            $region=[Drawing.Rectangle]::new(($index%4)*$cell+6,[int][math]::Floor($index/4)*$cell+6,$cell-12,$cell-12)
            $g.DrawImage($source,[Drawing.Rectangle]::new(190,276,285,169),$region,[Drawing.GraphicsUnit]::Pixel)
        }else{
            $mark=if($cold){'COLD  STORAGE'}elseif($i -in @(15,16,18)){'BLEND   /   SELECT'}else{'MARKET   SELECTION'}
            $g.DrawString($mark,$caption,$ink,23,325)
            $g.FillRectangle($banner,22,374,(80+22*$v),5)
        }
        $g.DrawString($name,$big,$ink,[Drawing.RectangleF]::new(6,117,500,137),$format)
        $g.DrawString($parts[2],$caption,$ink,21,254)
        $g.FillRectangle($banner,0,457,512,55)
        $amount=$weights[$i]
        if($i -eq 4 -and $v -lt 2){$amount=@('85 g','200 g')[$v]}
        if($i -eq 25 -and $v -eq 0){$amount='180 g'}
        if($i -eq 20){$amount='455 g'}elseif($i -eq 21){$amount=@('75 mL','160 mL','100 mL','474 mL','75 mL','70 mL')[$v]}
        $g.DrawString(($brand+'    ·    '+$amount),$body,$(if($light){$white}else{$brush}),19,469)
    }
    $body.Dispose();$caption.Dispose();$big.Dispose();$brush.Dispose()
}
function Draw-ScanLines($g,$x,$y,$height){
    $pos=$x
    foreach($n in @(2,1,3,2,1,1,3,1,2,2,1,3,1,2,3,1,1,2,1,3,2,1,3,2,1,1,3,1,2,2,1,2)){
        $g.FillRectangle([Drawing.Brushes]::Black,$pos,$y,$n*2,$height)
        $pos+=$n*2+2
    }
}
function Write-SidePanel($folder,$family){
    $img=[Drawing.Bitmap]::new(256,512)
    $g=[Drawing.Graphics]::FromImage($img);$g.Clear([Drawing.Color]::Ivory)
    $b=[Drawing.Brushes]::Black;$f=[Drawing.Font]::new($family,17,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
    $g.DrawString('상품 표시 사항',$f,$b,9,19)
    $j=0
    foreach($row in @('포장재질','제품 보관 방법','제조원 및 판매원','소비기한 별도 표시','포장 분리배출','소비자 상담실')){
        $y=68+$j*39;$g.FillRectangle($b,10,$y,233,1);$g.DrawString($row,$f,$b,13,$y+5);$j++
    }
    Draw-ScanLines $g 21 371 68
    $g.DrawString('8  809630  070118',$f,$b,19,448)
    $img.Save((Join-Path $folder 'pack_information.png'))
    $f.Dispose();$g.Dispose();$img.Dispose()
}
