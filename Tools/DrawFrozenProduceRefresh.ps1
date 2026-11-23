param([switch]$RefreshFiles)
Add-Type -AssemblyName System.Drawing
function Draw-FrozenProduceRefresh($g,[int]$family,[int]$v,[string]$name){
    if($family -ne 23){return}
    $state=$g.Save();$g.ResetTransform();$g.SmoothingMode='AntiAlias';$g.InterpolationMode='HighQualityBicubic'
    $owned=[Collections.Generic.List[IDisposable]]::new()
    function FrozenInk($hex){$b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex));$owned.Add($b);return $b}
    function FrozenText($text,$x,$y,$w,$h,$brush,$face='Malgun Gothic'){PackCaption $g $text ([Drawing.RectangleF]::new($x,$y,$w,$h)) 90 $brush $face}
    function FrozenPhoto($key,$x,$y,$w,$h,$ellipse=$true){$clip=[Drawing.Drawing2D.GraphicsPath]::new();try{if($ellipse){$clip.AddEllipse($x,$y,$w,$h)}else{$clip.AddRectangle([Drawing.RectangleF]::new($x,$y,$w,$h))};RetailPhoto $g $key ([Drawing.RectangleF]::new($x,$y,$w,$h)) $clip}finally{$clip.Dispose()}}
    $cream=FrozenInk '#F7F3E6';$dark=FrozenInk '#243C39';$white=[Drawing.Brushes]::White
    $main=FrozenInk @('#23394D','#3B6749','#274B42','#A63B48','#E8B540','#576953')[$v]
    try{
        if($v -eq 0){
            $g.Clear($main.Color);FrozenText '서리식탁  |  FREEZER COLLECTION' 30 97 452 26 $cream
            FrozenText '블루베리' 26 133 460 101 $cream 'Batang'
            FrozenText '한 알씩 꺼내 쓰는 냉동 과일' 26 258 460 28 $cream
            FrozenPhoto '=Blueberries' 32 327 448 224
            $g.FillRectangle($cream,0,581,512,59);FrozenText '냉동 보관  −18°C 이하     1 kg' 21 598 470 25 $main
        }elseif($v -eq 1){
            $g.Clear($cream.Color);$g.FillRectangle($main,0,0,512,112);FrozenText '서리식탁' 26 57 460 33 $cream
            FrozenText '브로콜리' 24 154 464 93 $main
            FrozenText '손질한 채소를 간편하게' 30 276 452 29 $main
            FrozenPhoto '=Broccoli' 62 348 426 229
            $g.FillRectangle($main,0,596,512,44);FrozenText '냉동 채소  |  1 kg' 22 607 468 24 $cream
        }elseif($v -eq 2){
            $orange=FrozenInk '#D88738';$g.Clear($main.Color)
            FrozenText '서리식탁' 26 59 460 32 $cream
            $g.FillRectangle($orange,27,119,458,124);FrozenText '채소믹스' 38 141 436 77 $cream
            FrozenText '볶음밥 · 수프 · 카레' 26 274 460 28 $cream
            FrozenPhoto 'detail_mixedveg' 34 345 444 220 $false
            FrozenText '손질 냉동채소     1 kg' 26 601 460 27 $cream
        }elseif($v -eq 3){
            $g.Clear($cream.Color);FrozenText '서리식탁' 29 96 454 29 $main
            FrozenText '딸기' 34 131 444 110 $main 'Batang'
            FrozenText '한 입의 새콤달콤함' 28 273 456 30 $main
            FrozenPhoto 'strawberry' 62 345 390 201
            $g.FillRectangle($main,0,582,512,58);FrozenText '냉동 과일  −18°C 이하   1 kg' 22 599 468 25 $cream
        }elseif($v -eq 4){
            $g.Clear($main.Color);$g.FillRectangle($dark,0,0,512,132);FrozenText '서리식탁' 28 96 456 30 $cream
            FrozenText '망고' 29 147 454 106 $dark 'Batang'
            FrozenText '달콤한 과일을 간편하게' 29 281 454 29 $dark
            FrozenPhoto 'mango' 44 352 424 204
            FrozenText '냉동 과일  |  1 kg' 27 603 458 26 $dark
        }else{
            $g.Clear($cream.Color);$g.FillRectangle($main,0,0,122,640)
            FrozenText '서리식탁' 149 60 337 29 $main
            FrozenText '시금치' 141 146 351 97 $main 'Batang'
            FrozenText '손질 냉동채소' 141 276 351 30 $main
            FrozenPhoto 'spinach' 58 353 424 218
            FrozenText '−18°C 이하 보관   1 kg' 144 603 342 25 $main
        }
    }finally{foreach($b in $owned){$b.Dispose()};$g.Restore($state)}
}
if($RefreshFiles){
    $ErrorActionPreference='Stop'
    . (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
    . (Join-Path $PSScriptRoot 'DrawPackInformation.ps1')
    $folder=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs';$names='블루베리','브로콜리','채소믹스','딸기','망고','시금치'
    $info=[Drawing.Bitmap]::new(2048,1024)
    try{foreach($v in 0..5){$b=[Drawing.Bitmap]::new(512,640);$g=[Drawing.Graphics]::FromImage($b)
        try{Draw-FrozenProduceRefresh $g 23 $v $names[$v];Write-PackInformation $info $b $folder 23 $v $names[$v] '서리식탁' '냉동채소';$b.Save((Join-Path $folder ('pack_23_'+$v+'.png')))}finally{$g.Dispose();$b.Dispose()}
    };$info.Save((Join-Path $folder 'RetailInformation23.png'))}finally{$info.Dispose();foreach($p in $stockPhotos.Values){$p.Dispose()}}
}
