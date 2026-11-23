param([switch]$RefreshFiles)
Add-Type -AssemblyName System.Drawing
function CannedPrintSize([int]$v){if($v -eq 0){@(768,240)}elseif($v -eq 1){@(768,512)}elseif($v -eq 3){@(512,496)}else{@(512,576)}}
function Draw-CannedRefresh($g,[int]$family,[int]$v,[string]$name){
    if($family -ne 4){return}
    $state=$g.Save();$g.ResetTransform();$g.SmoothingMode='AntiAlias';$g.InterpolationMode='HighQualityBicubic'
    $owned=[Collections.Generic.List[IDisposable]]::new()
    function CannedInk($hex){$b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex));$owned.Add($b);return $b}
    function CannedText($text,$x,$y,$w,$h,$brush,$face='Malgun Gothic'){PackCaption $g $text ([Drawing.RectangleF]::new($x,$y,$w,$h)) 90 $brush $face}
    function CannedPhoto($key,$x,$y,$w,$h,$crop=$null){
        $shape=[Drawing.Drawing2D.GraphicsPath]::new();$shape.AddEllipse($x,$y,$w,$h)
        try{
            if($null -eq $crop){RetailPhoto $g $key ([Drawing.RectangleF]::new($x,$y,$w,$h)) $shape}
            else{
                if(!$stockPhotos.ContainsKey($key)){$stockPhotos[$key]=[Drawing.Image]::FromFile((Join-Path $recipeFolder ('Ingredients/'+$key+'.jpg')))}
                $saved=$g.Save();$g.SetClip($shape)
                try{$g.DrawImage($stockPhotos[$key],[Drawing.RectangleF]::new($x,$y,$w,$h),[Drawing.RectangleF]::new($crop[0],$crop[1],$crop[2],$crop[3]),[Drawing.GraphicsUnit]::Pixel)}finally{$g.Restore($saved)}
            }
        }finally{$shape.Dispose()}
    }
    $cream=CannedInk '#F7F0DE';$navy=CannedInk '#243C4A';$gold=CannedInk '#D5A747';$white=[Drawing.Brushes]::White
    try{
        if($v -eq 0){
            $g.Clear($gold.Color);$g.FillRectangle($navy,0,0,768,43)
            CannedText '골목찬방  |  바다의 한 끼' 26 9 716 26 $cream
            CannedText '살코기참치' 32 64 704 91 $navy 'Batang'
            $g.FillRectangle($cream,0,178,768,62)
            CannedText '담백한 살코기' 37 194 429 28 $navy
            CannedText '150 g' 570 192 154 32 $navy
        }elseif($v -eq 1){
            $g.Clear($navy.Color);$g.FillRectangle($gold,0,0,768,19)
            CannedText '골목찬방' 28 45 293 31 $cream
            CannedText '돈육햄' 25 112 716 111 $cream 'Batang'
            CannedPhoto 'detail_ham' 389 259 347 195
            CannedText '부드럽게 즐기는' 28 289 338 30 $cream
            CannedText '한 끼의 든든함' 28 337 338 33 $cream
            CannedText '햄  ·  200 g' 26 457 337 27 $gold
        }elseif($v -eq 2){
            $g.Clear($cream.Color);$g.FillRectangle($navy,0,0,512,62)
            CannedText '골목찬방' 26 17 460 29 $cream
            CannedText '골뱅이' 27 112 458 108 $navy 'Batang'
            CannedText '바다의 쫄깃한 식감' 29 247 454 34 $navy
            $g.FillRectangle($gold,28,327,456,3)
            CannedText 'WHELK' 30 367 452 52 $navy 'Georgia'
            CannedText '골뱅이 통조림  ·  230 g' 27 516 458 27 $navy
        }elseif($v -eq 3){
            $g.Clear($navy.Color);$g.FillRectangle($gold,0,0,512,17)
            CannedText '골목찬방' 25 35 462 28 $cream
            CannedText '꽁치조림' 27 93 458 81 $cream 'Batang'
            CannedPhoto 'refresh_braised_saury' 30 222 452 214
            CannedText '생선 통조림  ·  300 g' 29 456 454 24 $cream
        }elseif($v -eq 4){
            $orange=CannedInk '#C97527';$g.Clear($cream.Color)
            $g.FillRectangle($orange,0,0,512,72);CannedText '골목찬방' 29 21 454 31 $white
            CannedText '황도' 36 118 440 112 $orange 'Batang'
            CannedText '달콤한 복숭아 한 조각' 30 266 452 28 $navy
            CannedPhoto 'refresh_canned_peach' 38 333 436 172 @(111,126,666,331)
            CannedText '과일 통조림  ·  400 g' 28 531 456 25 $navy
        }else{
            $green=CannedInk '#42633D';$g.Clear($gold.Color)
            $g.FillRectangle($green,0,0,512,69);CannedText '골목찬방' 29 20 454 31 $cream
            CannedText '옥수수' 24 114 464 100 $green 'Batang'
            CannedText '톡톡 터지는 달콤함' 29 253 454 29 $green
            CannedPhoto 'sweet_corn' 31 326 450 174
            CannedText '스위트콘  ·  340 g' 27 531 458 25 $green
        }
    }finally{foreach($b in $owned){$b.Dispose()};$g.Restore($state)}
}
if($RefreshFiles){
    $ErrorActionPreference='Stop'
    . (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
    . (Join-Path $PSScriptRoot 'DrawPackInformation.ps1')
    $folder=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'
    $names='살코기참치','돈육햄','골뱅이','꽁치조림','황도','옥수수';$info=[Drawing.Bitmap]::new(2048,1024)
    try{for($v=0;$v -lt 6;$v++){
        $size=CannedPrintSize $v;$b=[Drawing.Bitmap]::new($size[0],$size[1]);$g=[Drawing.Graphics]::FromImage($b)
        try{Draw-CannedRefresh $g 4 $v $names[$v];Write-PackInformation $info $b $folder 4 $v $names[$v] '골목찬방' '통조림';$b.Save((Join-Path $folder ('pack_04_'+$v+'.png')))}finally{$g.Dispose();$b.Dispose()}
    };$info.Save((Join-Path $folder 'RetailInformation4.png'))}finally{$info.Dispose();foreach($photo in $stockPhotos.Values){$photo.Dispose()}}
}
