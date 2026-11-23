param([switch]$RefreshFiles)
Add-Type -AssemblyName System.Drawing

function Draw-ChocolateRefresh($g,[int]$family,[int]$variant,[string]$name){
    if($family -ne 7){return}
    $state=$g.Save();$g.ResetTransform();$g.SmoothingMode='AntiAlias';$g.InterpolationMode='HighQualityBicubic'
    $owned=[Collections.Generic.List[IDisposable]]::new()
    function ChocolateInk($hex){$b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex));$owned.Add($b);return $b}
    function ChocolateText($text,$x,$y,$w,$h,$ink,$face='Malgun Gothic') {PackCaption $g $text ([Drawing.RectangleF]::new($x,$y,$w,$h)) 80 $ink $face}
    function ChocolatePhoto($key,$x,$y,$w,$h) {RetailPhoto $g $key ([Drawing.RectangleF]::new($x,$y,$w,$h)) $null}
    $cream=ChocolateInk '#F5EDD8';$brown=ChocolateInk '#352723';$gold=ChocolateInk '#C9A566';$white=[Drawing.Brushes]::White
    try{
        if($variant -in @(0,4)){
            $red=ChocolateInk $(if($variant -eq 0){'#7D302D'}else{'#292C30'})
            $g.Clear($red.Color)
            $g.FillRectangle($gold,20,26,344,2);$g.FillRectangle($gold,20,738,344,2)
            ChocolateText '바삭제과' 37 56 310 31 $cream
            ChocolateText $(if($variant -eq 0){'밀크'}else{'다크'}) 29 141 326 91 $cream 'Batang'
            ChocolateText '초콜릿' 30 251 324 82 $cream 'Batang'
            ChocolateText $(if($variant -eq 0){'부드럽게 녹는 달콤함'}else{'깊고 진한 카카오의 여운'}) 24 369 336 27 $gold
            ChocolatePhoto $(if($variant -eq 0){'refresh_plain_milk_chocolate'}else{'refresh_plain_dark_chocolate'}) 24 430 336 233
            ChocolateText '초콜릿  |  70 g' 39 693 306 25 $cream
        }elseif($variant -eq 2){
            $g.Clear($brown.Color);$g.FillRectangle($gold,0,0,512,12)
            ChocolateText '바삭제과' 24 32 190 26 $cream
            ChocolateText '카카오' 20 92 271 65 $cream 'Batang'
            ChocolateText '큐브' 36 166 235 62 $gold 'Batang'
            ChocolatePhoto 'refresh_plain_dark_chocolate' 302 76 192 174
            ChocolateText '작게 나눈 진한 한 조각' 24 259 338 25 $cream
            ChocolateText '86 g' 378 262 110 27 $gold
        }else{
            $accent=ChocolateInk @('#000000','#A33C26','#000000','#E0C995','#000000','#365044')[$variant]
            $g.Clear($accent.Color)
            $ink=if($variant -eq 3){$brown}else{$cream}
            $g.FillRectangle($gold,0,0,18,256);$g.FillRectangle($gold,750,0,18,256)
            ChocolateText '바삭제과' 34 25 170 24 $ink
            if($variant -eq 1){
                ChocolateText '땅콩바' 30 77 361 94 $cream 'Batang'
                ChocolatePhoto 'photo_chocolatepeanut_1' 412 35 315 180
                ChocolateText '고소한 땅콩과 초콜릿' 35 199 324 24 $cream
            }elseif($variant -eq 3){
                ChocolateText '크런치바' 27 72 419 87 $brown
                ChocolatePhoto 'refresh_plain_milk_chocolate' 473 28 253 137
                ChocolatePhoto 'puffed_rice' 560 152 164 68
                ChocolateText '바삭한 쌀 크런치' 33 196 367 24 $brown
            }else{
                ChocolateText '아몬드바' 28 74 418 87 $cream 'Batang'
                ChocolatePhoto 'refresh_plain_dark_chocolate' 470 29 256 131
                ChocolatePhoto 'refresh_almond_kernels' 557 147 168 75
                ChocolateText '구운 아몬드의 고소함' 34 197 370 24 $cream
            }
            ChocolateText @('','36 g','','34 g','','46 g')[$variant] 616 229 109 18 $ink
        }
    }finally{foreach($b in $owned){$b.Dispose()};$g.Restore($state)}
}

if($RefreshFiles){
    $ErrorActionPreference='Stop'
    . (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
    . (Join-Path $PSScriptRoot 'DrawPackInformation.ps1')
    $folder=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'
    $names='밀크초콜릿','땅콩바','카카오큐브','크런치바','다크초콜릿','아몬드바'
    $info=[Drawing.Bitmap]::new(2048,1024)
    try{for($v=0;$v -lt 6;$v++){
        $w=if($v -in @(0,4)){384}elseif($v -eq 2){512}else{768}
        $h=if($v -in @(0,4)){768}elseif($v -eq 2){320}else{256}
        $b=[Drawing.Bitmap]::new($w,$h);$g=[Drawing.Graphics]::FromImage($b)
        try{Draw-ChocolateRefresh $g 7 $v $names[$v];Write-PackInformation $info $b $folder 7 $v $names[$v] '바삭제과' '초콜릿';$b.Save((Join-Path $folder ('pack_07_'+$v+'.png')))}finally{$g.Dispose();$b.Dispose()}
    };$info.Save((Join-Path $folder 'RetailInformation7.png'))}finally{$info.Dispose();foreach($photo in $stockPhotos.Values){$photo.Dispose()}}
}
