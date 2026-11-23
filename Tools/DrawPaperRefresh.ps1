param([switch]$RefreshFiles)
Add-Type -AssemblyName System.Drawing
function PaperPrintSize([int]$v){if($v -eq 1){@(768,288)}elseif($v -eq 2){@(512,352)}elseif($v -eq 3){@(512,256)}else{@(768,208)}}
function Draw-PaperRefresh($g,[int]$family,[int]$v,[string]$name){
    if($family -ne 28){return}
    $state=$g.Save();$g.ResetTransform();$g.SmoothingMode='AntiAlias'
    $size=PaperPrintSize $v;$w=$size[0];$h=$size[1]
    $color=[Drawing.ColorTranslator]::FromHtml(@('#42655F','#80596E','#315E75','#5B7059','#5F7476','#3B6753')[$v])
    $ink=[Drawing.SolidBrush]::new($color);$dark=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#263C40'))
    try{
        $g.Clear([Drawing.Color]::FromArgb(250,248,239))
        $g.FillRectangle($ink,0,0,$w,37)
        PackCaption $g '살림결  |  DAILY SOFT' ([Drawing.RectangleF]::new(22,8,$w-44,22)) 35 ([Drawing.Brushes]::White)
        $titleY=if($v -eq 2){69}else{54};$titleH=if($v -eq 2){71}else{52}
        PackCaption $g $name ([Drawing.RectangleF]::new(26,$titleY,$w-52,$titleH)) 72 $ink 'Batang'
        $description=@('부드러운 3겹 화장지','부드러운 미용티슈','흡수력 좋은 키친타월','간편하게 휴대하는 티슈','편안한 2겹 화장지','도톰한 3겹 화장지')[$v]
        PackCaption $g $description ([Drawing.RectangleF]::new(25,($titleY+$titleH+13),$w-50,27)) 40 $dark
        $count=@('6롤','250매','2롤','10매 × 6개','6롤','6롤')[$v]
        $g.FillRectangle($ink,($w-164),($h-48),143,31)
        PackCaption $g $count ([Drawing.RectangleF]::new(($w-158),($h-43),131,21)) 35 ([Drawing.Brushes]::White)
        PackCaption $g '생활 위생용품  ·  부드러운 감촉' ([Drawing.RectangleF]::new(25,($h-39),($w-208),17)) 30 $dark
        if($v -eq 2){
            $pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(52,$color),2)
            try{for($k=0;$k -lt 7;$k++){$g.DrawArc($pen,40+$k*65,215,70,45,0,170)}}finally{$pen.Dispose()}
        }
    }finally{$ink.Dispose();$dark.Dispose();$g.Restore($state)}
}
if($RefreshFiles){
    $ErrorActionPreference='Stop'
    . (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
    . (Join-Path $PSScriptRoot 'DrawPackInformation.ps1')
    $folder=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'
    $names='순면감촉','보들티슈','흡수타월','포켓티슈','순한롤','도톰한롤'
    $info=[Drawing.Bitmap]::new(2048,1024)
    try{for($v=0;$v -lt 6;$v++){
        $size=PaperPrintSize $v;$b=[Drawing.Bitmap]::new($size[0],$size[1]);$g=[Drawing.Graphics]::FromImage($b)
        try{Draw-PaperRefresh $g 28 $v $names[$v];Write-PackInformation $info $b $folder 28 $v $names[$v] '살림결' '화장지 · 위생용품';$b.Save((Join-Path $folder ('pack_28_'+$v+'.png')))}finally{$g.Dispose();$b.Dispose()}
    };$info.Save((Join-Path $folder 'RetailInformation28.png'))}finally{$info.Dispose()}
}
