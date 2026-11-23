param([switch]$RefreshFiles)
Add-Type -AssemblyName System.Drawing
function DairyPrintSize([int]$v){if($v -in @(0,1)){@(384,896)}elseif($v -eq 3){@(384,672)}elseif($v -eq 4){@(512,384)}elseif($v -eq 5){@(512,320)}else{@(512,576)}}
function Draw-DairyRefresh($g,[int]$family,[int]$v,[string]$name){
    if($family -ne 26){return}
    $state=$g.Save();$g.ResetTransform();$g.SmoothingMode='AntiAlias'
    $size=DairyPrintSize $v;$height=$size[1]
    if($v -in @(2,4,5)){
        $hue=if($v -eq 2){'#A84A63'}elseif($v -eq 4){'#587145'}else{'#3E6386'}
        $ink=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hue))
        try{
            $g.Clear([Drawing.Color]::FromArgb(250,245,222));$g.FillRectangle($ink,0,0,512,67)
            PackCaption $g '서리식탁' ([Drawing.RectangleF]::new(27,17,458,32)) 60 ([Drawing.Brushes]::Ivory)
            if($v -eq 2){
                PackCaption $g '딸기' ([Drawing.RectangleF]::new(26,108,460,85)) 100 $ink
                PackCaption $g '요거트' ([Drawing.RectangleF]::new(26,212,460,77)) 90 $ink
                $clip=[Drawing.Drawing2D.GraphicsPath]::new();$clip.AddEllipse(57,324,398,154)
                try{RetailPhoto $g 'strawberry' ([Drawing.RectangleF]::new(57,324,398,154)) $clip}finally{$clip.Dispose()}
            }elseif($v -eq 4){
                PackCaption $g '바나나우유' ([Drawing.RectangleF]::new(22,105,468,79)) 90 $ink
                $yellow=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#D8AF42'))
                try{
                    # Original banana cross-section motif instead of a botanical specimen photo.
                    for($i=0;$i -lt 3;$i++){$x=178+$i*54;$g.FillEllipse($yellow,$x,226,49,47);$g.FillEllipse([Drawing.Brushes]::Ivory,$x+6,232,37,35)}
                }finally{$yellow.Dispose()}
            }else{
                PackCaption $g '플레인' ([Drawing.RectangleF]::new(20,91,472,57)) 90 $ink
                PackCaption $g '요거트' ([Drawing.RectangleF]::new(20,159,472,60)) 90 $ink
            }
            $g.FillRectangle($ink,0,$height-59,512,59)
            $weight=if($v -eq 2){'150 mL'}elseif($v -eq 4){'240 mL'}else{'100 g'}
            PackCaption $g ('냉장 보관  |  '+$weight) ([Drawing.RectangleF]::new(25,$height-44,462,27)) 45 ([Drawing.Brushes]::Ivory)
        }finally{$ink.Dispose();$g.Restore($state)}
        return
    }
    $ink=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml(@('#315E4A','#3E7682','#355B82')[$(if($v -eq 3){2}else{$v})]))
    $pale=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#E3EBD9'))
    try{
        $g.Clear([Drawing.Color]::FromArgb(248,246,231))
        $g.FillRectangle($ink,0,0,384,104)
        PackCaption $g '서리식탁' ([Drawing.RectangleF]::new(25,28,334,43)) 70 ([Drawing.Brushes]::Ivory)
        PackCaption $g '매일의 신선함' ([Drawing.RectangleF]::new(24,139,336,31)) 48 $ink
        $title=if($v -eq 1){'저지방'}elseif($v -eq 3){'흰우유'}else{'목장'}
        PackCaption $g $title ([Drawing.RectangleF]::new(23,208,338,92)) 110 $ink
        if($v -ne 3){PackCaption $g '우유' ([Drawing.RectangleF]::new(25,322,334,125)) 130 $ink}
        $ground=$height-241
        $g.FillEllipse($pale,-176,$ground,650,285)
        $g.FillEllipse($ink,142,$ground+102,444,233)
        # Original farm silhouette, not a copied brand emblem.
        $g.FillRectangle($ink,62,$ground+41,100,83)
        $roof=[Drawing.PointF[]]@([Drawing.PointF]::new(47,$ground+42),[Drawing.PointF]::new(111,$ground-5),[Drawing.PointF]::new(176,$ground+42))
        $g.FillPolygon($ink,$roof);$g.FillRectangle([Drawing.Brushes]::Ivory,100,$ground+76,24,48)
        $g.FillRectangle([Drawing.Brushes]::Ivory,74,$ground+62,14,16)
        $g.FillRectangle($ink,0,$height-85,384,85)
        $weight=if($v -eq 3){'200 mL'}else{'1,000 mL'}
        PackCaption $g $weight ([Drawing.RectangleF]::new(22,$height-70,340,33)) 55 ([Drawing.Brushes]::Ivory)
        PackCaption $g '냉장 보관  ·  우유' ([Drawing.RectangleF]::new(26,$height-29,332,18)) 32 ([Drawing.Brushes]::Ivory)
    }finally{$ink.Dispose();$pale.Dispose();$g.Restore($state)}
}
function Write-DairySealPrints([string]$folder){
    foreach($v in @(2,4,5)){
        $b=[Drawing.Bitmap]::new(256,256);$g=[Drawing.Graphics]::FromImage($b)
        $ink=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($(if($v -eq 2){'#9E4057'}elseif($v -eq 4){'#557747'}else{'#386C85'})))
        $line=[Drawing.Pen]::new($ink,3)
        try{
            $g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::FromArgb(234,231,211))
            $g.DrawEllipse($line,12,12,232,232)
            PackCaption $g '서리식탁' ([Drawing.RectangleF]::new(45,45,166,28)) 45 $ink
            $name=if($v -eq 2){'딸기요거트'}elseif($v -eq 4){'바나나우유'}else{'플레인요거트'}
            PackCaption $g $name ([Drawing.RectangleF]::new(20,100,216,39)) 48 $ink
            PackCaption $g '냉장보관' ([Drawing.RectangleF]::new(54,158,148,23)) 35 $ink
            PackCaption $g '신선한 하루' ([Drawing.RectangleF]::new(63,189,130,17)) 25 $ink
            $b.Save((Join-Path $folder ('DairySeal26_'+$v+'.png')))
        }finally{$line.Dispose();$ink.Dispose();$g.Dispose();$b.Dispose()}
    }
}
if($RefreshFiles){
    $ErrorActionPreference='Stop'
    . (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
    . (Join-Path $PSScriptRoot 'DrawPackInformation.ps1')
    $folder=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'
    $names='목장우유','저지방우유','딸기요거트','흰우유','바나나우유','플레인요거트'
    Write-DairySealPrints $folder
    $old=[Drawing.Bitmap]::new((Join-Path $folder 'RetailInformation26.png'));$info=[Drawing.Bitmap]::new($old);$old.Dispose()
    try{foreach($v in 0..5){
        $size=DairyPrintSize $v;$b=[Drawing.Bitmap]::new($size[0],$size[1]);$g=[Drawing.Graphics]::FromImage($b)
        try{Draw-DairyRefresh $g 26 $v $names[$v];Write-PackInformation $info $b $folder 26 $v $names[$v] '서리식탁' '우유 · 요구르트';$b.Save((Join-Path $folder ('pack_26_'+$v+'.png')))}finally{$g.Dispose();$b.Dispose()}
    };$info.Save((Join-Path $folder 'RetailInformation26.png'))}finally{$info.Dispose()}
}
