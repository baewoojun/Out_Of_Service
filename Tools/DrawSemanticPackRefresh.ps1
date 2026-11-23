param([switch]$RefreshFiles)
Add-Type -AssemblyName System.Drawing

function Draw-SemanticPackRefresh($g,[int]$family,[int]$variant,[string]$name){
    if($family -ne 25 -or $variant -ne 3){return}
    $state=$g.Save();$g.ScaleTransform($g.VisibleClipBounds.Width/512,$g.VisibleClipBounds.Width/512)
    $green=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#315E52'))
    $cream=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#F1E8CD'))
    try{
        $g.Clear([Drawing.Color]::FloralWhite)
        $g.FillRectangle($green,0,0,512,153)
        PackCaption $g '서리식탁' ([Drawing.RectangleF]::new(27,24,184,29)) 50 ([Drawing.Brushes]::White)
        PackCaption $g '스트링치즈' ([Drawing.RectangleF]::new(27,74,455,60)) 70 ([Drawing.Brushes]::White)
        # Original diagram of the selling unit, replacing the unrelated braided-cheese photograph.
        for($k=0;$k -lt 5;$k++){
            $x=37+$k*91;$g.FillRectangle($cream,$x,193,64,216)
            $g.FillEllipse($cream,$x,182,64,28);$g.FillEllipse($cream,$x,393,64,28)
            $g.DrawRectangle([Drawing.Pens]::Silver,$x-4,174,72,256)
        }
        PackCaption $g '하나씩 꺼내 먹는 개별 포장' ([Drawing.RectangleF]::new(24,445,464,27)) 40 $green
        PackCaption $g '냉장 보관  ·  20 g × 5개' ([Drawing.RectangleF]::new(24,482,464,20)) 40 $green
    }finally{$green.Dispose();$cream.Dispose();$g.Restore($state)}
}

function Write-StringCheesePrints($folder){
    foreach($kind in @('Header','Footer')){
        $height=if($kind -eq 'Header'){192}else{80}
        $b=[Drawing.Bitmap]::new(576,$height);$g=[Drawing.Graphics]::FromImage($b)
        $green=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#315E52'))
        try{
            $g.SmoothingMode='AntiAlias';$g.Clear($green.Color)
            if($kind -eq 'Header'){
                PackCaption $g '서리식탁  |  치즈를 간편하게' ([Drawing.RectangleF]::new(20,20,536,32)) 50 ([Drawing.Brushes]::White)
                PackCaption $g '스트링치즈' ([Drawing.RectangleF]::new(22,77,532,85)) 90 ([Drawing.Brushes]::Ivory)
            }else{
                PackCaption $g '냉장 보관   20 g × 5개   /   100 g' ([Drawing.RectangleF]::new(18,22,540,35)) 48 ([Drawing.Brushes]::White)
            }
            $b.Save((Join-Path $folder ('StringCheese'+$kind+'0.png')))
        }finally{$g.Dispose();$b.Dispose();$green.Dispose()}
    }
}

if($RefreshFiles){
    $ErrorActionPreference='Stop'
    . (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
    . (Join-Path $PSScriptRoot 'DrawPackInformation.ps1')
    $folder=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs'
    $groups=@(
        @{id=16;variants=@(4);brand='오후다방';group='믹스커피';names=@('모카스틱','로스트미니','라테믹스','헤이즐넛','블랙스틱','밀크모카')},
        @{id=17;variants=@(1);brand='오후다방';group='커피음료';names=@('컵라테','캔아메리카노','캔모카','에스프레소','카라멜라테','더블블랙')},
        @{id=18;variants=@(3);brand='오후다방';group='전통차';names=@('유자청','생강차','대추차','율무차','쌍화차','매실청')},
        @{id=25;variants=@(3);brand='서리식탁';group='치즈 · 냉장식품';names=@('체다슬라이스','모차렐라','무염버터','스트링치즈','고다치즈','크림치즈')}
    )
    try{
        foreach($set in $groups){
            $old=[Drawing.Bitmap]::new((Join-Path $folder ('RetailInformation'+$set.id+'.png')))
            $info=[Drawing.Bitmap]::new($old);$old.Dispose()
            try{
                foreach($v in $set.variants){
                    $b=[Drawing.Bitmap]::new(768,768);$g=[Drawing.Graphics]::FromImage($b)
                    try{
                        $g.SmoothingMode='AntiAlias';$g.ScaleTransform(1.5,1.5)
                        Draw-ShelfPrint $g $set.id $v $set.names[$v] $set.brand
                        $g.ResetTransform();Draw-SemanticPackRefresh $g $set.id $v $set.names[$v]
                        $b.Save((Join-Path $folder ('pack_{0:D2}_{1}.png' -f $set.id,$v)))
                    }finally{$g.Dispose();$b.Dispose()}
                }
                for($v=0;$v -lt 6;$v++){
                    $front=[Drawing.Bitmap]::new((Join-Path $folder ('pack_{0:D2}_{1}.png' -f $set.id,$v)))
                    try{Write-PackInformation $info $front $folder $set.id $v $set.names[$v] $set.brand $set.group}finally{$front.Dispose()}
                }
                $info.Save((Join-Path $folder ('RetailInformation'+$set.id+'.png')))
            }finally{$info.Dispose()}
        }
        Write-StringCheesePrints $folder
    }finally{foreach($photo in $stockPhotos.Values){$photo.Dispose()}}
}
