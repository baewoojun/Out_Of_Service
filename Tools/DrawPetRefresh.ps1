param([switch]$RefreshFiles,[string]$OutputDirectory='')
Add-Type -AssemblyName System.Drawing

function Draw-PetRefresh($g,[int]$family,[int]$v,[string]$name){
    if($family -ne 29){return}
    $state=$g.Save();$g.ScaleTransform($g.VisibleClipBounds.Width/512,$g.VisibleClipBounds.Width/512)
    $g.SmoothingMode='AntiAlias'
    $owned=[Collections.Generic.List[IDisposable]]::new()
    function PetInk($hex){$b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex));$owned.Add($b);return $b}
    function PetText($text,$x,$y,$w,$h,$ink,$font='Malgun Gothic'){PackCaption $g $text ([Drawing.RectangleF]::new($x,$y,$w,$h)) 64 $ink $font}
    function PetPhoto($key,$x,$y,$w,$h){
        $shape=[Drawing.Drawing2D.GraphicsPath]::new();$shape.AddEllipse($x,$y,$w,$h)
        try{RetailPhoto $g $key ([Drawing.RectangleF]::new($x,$y,$w,$h)) $shape}finally{$shape.Dispose()}
    }
    function Paw($x,$y,$s,$ink){
        $g.FillEllipse($ink,$x+$s*.20,$y+$s*.39,$s*.58,$s*.46)
        for($j=0;$j -lt 4;$j++){$g.FillEllipse($ink,$x+$s*.03+$j*$s*.23,$y+$(if($j -in @(0,3)){$s*.19}else{0}),$s*.22,$s*.28)}
    }
    $white=PetInk '#FFFDF5';$dark=PetInk '#26342E';$cream=PetInk '#E5DFCB'
    $accent=PetInk @('#40616B','#5E713E','#9B623D','#70443E','#355B61','#8B5B40')[$v]
    try{
        $g.Clear($white.Color)
        if($v -in @(0,1,3)){
            $g.FillRectangle($accent,0,0,512,96)
            PetText '꼬리식탁' 28 29 231 42 $white
            PetText $(if($v -eq 0){'CAT'}else{'DOG'}) 350 31 133 36 $white
            PetText $name 25 131 462 79 $dark
            PetText $(if($v -eq 0){'성묘용  ·  건식 사료'}else{'성견용  ·  건식 사료'}) 29 230 454 29 $accent
            PetPhoto 'refresh_kibble' 26 282 298 162
            Paw 379 301 80 $accent
            PetText '2 kg' 349 398 135 40 $dark
            $g.FillRectangle($accent,0,468,512,44)
            PetText 'DAILY RECIPE   /   반려동물 전용' 22 481 468 19 $white
        }elseif($v -eq 2){
            $g.Clear($accent.Color)
            Paw 355 33 109 $cream
            PetText '꼬리식탁  |  DOG SNACK' 28 29 294 27 $white
            PetText '바삭' 29 117 242 90 $white 'Batang'
            PetText '간식' 192 198 277 95 $cream 'Batang'
            PetPhoto 'dog_biscuit' 19 286 474 175
            PetText '한입 보상 간식' 25 244 207 27 $white
            PetText '반려견용' 25 476 177 23 $white
            PetText '200 g' 346 472 140 28 $white
        }elseif($v -eq 4){
            $g.FillRectangle($accent,0,0,512,140)
            PetText '꼬리식탁' 29 28 225 38 $white
            PetText 'CAT / WET FOOD' 28 91 456 28 $white
            PetText '참치한끼' 24 177 464 83 $dark
            PetPhoto 'refresh_wet_petfood' 58 312 396 132
            PetText '촉촉하게 즐기는 한 캔' 22 275 468 25 $accent
            PetText '성묘용  ·  습식' 30 464 290 26 $dark
            PetText '85 g' 365 458 117 35 $accent
        }else{
            $g.FillRectangle($accent,0,0,512,58)
            PetText '꼬리식탁  |  DOG TREATS' 26 18 460 24 $white
            Paw 361 92 108 $accent
            PetText '한입씩' 30 126 259 43 $accent 'Batang'
            PetText '오리큐브' 24 212 466 92 $dark
            $g.FillRectangle($cream,0,344,512,91)
            PetText '작게 나누어 주는 보상 간식' 27 371 458 35 $dark
            PetText '반려견용  ·  지퍼 보관' 27 467 324 24 $accent
            PetText '150 g' 364 461 121 33 $dark
        }
    }finally{foreach($item in $owned){$item.Dispose()};$g.Restore($state)}
}

function Write-PetCanPrint($folder){
    $b=[Drawing.Bitmap]::new(768,256);$g=[Drawing.Graphics]::FromImage($b)
    $teal=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#355B61'))
    try{
        $g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::Ivory)
        $g.FillRectangle($teal,0,0,768,55)
        PackCaption $g '꼬리식탁  ·  CAT WET FOOD' ([Drawing.RectangleF]::new(22,12,724,32)) 40 ([Drawing.Brushes]::White)
        PackCaption $g '참치한끼' ([Drawing.RectangleF]::new(19,75,421,89)) 90 $teal
        PackCaption $g '성묘용  ·  습식' ([Drawing.RectangleF]::new(23,191,303,28)) 35 $teal
        PackCaption $g '85 g' ([Drawing.RectangleF]::new(343,189,109,34)) 38 $teal
        RetailPhoto $g 'refresh_wet_petfood' ([Drawing.RectangleF]::new(478,68,279,180)) $null
        $b.Save((Join-Path $folder 'PetCanSide0.png'))
    }finally{$g.Dispose();$b.Dispose();$teal.Dispose()}
}

if($RefreshFiles){
    $ErrorActionPreference='Stop'
    . (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
    if(!$OutputDirectory){$OutputDirectory=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'}
    New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
    $names='연어한끼','닭고기한끼','바삭간식','소고기한끼','참치한끼','오리큐브'
    try{for($v=0;$v -lt 6;$v++){
        $b=[Drawing.Bitmap]::new(768,768);$g=[Drawing.Graphics]::FromImage($b)
        try{Draw-PetRefresh $g 29 $v $names[$v];$b.Save((Join-Path $OutputDirectory ('pack_29_'+$v+'.png')))}finally{$g.Dispose();$b.Dispose()}
    };Write-PetCanPrint $OutputDirectory}finally{foreach($photo in $stockPhotos.Values){$photo.Dispose()}}
}
