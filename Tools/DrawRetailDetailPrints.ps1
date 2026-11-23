param([switch]$RefreshFiles)
Add-Type -AssemblyName System.Drawing

function Write-RetailDetailPrints([string]$folder){
    # Original shop print, no real retailer logo or product-packaging pixels.
    function DetailWord($g,$text,$x,$y,$w,$h,$colour){
        $b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($colour))
        try{PackCaption $g $text ([Drawing.RectangleF]::new($x,$y,$w,$h)) 60 $b}finally{$b.Dispose()}
    }
    $rice='쌀밥','찰보리밥','잡곡밥','현미밥','곤드레밥','흑미밥'
    $hues='#9F2734','#84612C','#63503D','#AF743D','#4D7044','#544367'
    for($v=0;$v -lt 6;$v++){
        $b=[Drawing.Bitmap]::new(256,3840);$g=[Drawing.Graphics]::FromImage($b)
        try{
            $g.SmoothingMode='AntiAlias';$g.Clear([Drawing.ColorTranslator]::FromHtml($hues[$v]))
            # Four separate faces: front, top, reverse and bottom. No repeated
            # full-face logo wrapped over every fold of the paper band.
            # A panel is 41 x 156 mm in the model. Author the same aspect ratio
            # so Hangul stays proportionate instead of stretching 3x vertically.
            $g.FillRectangle([Drawing.Brushes]::Ivory,0,3160,256,340)
            DetailWord $g '골목찬방' 18 3010 220 47 '#FFFAEB'
            DetailWord $g $rice[$v] 14 3260 228 115 $hues[$v]
            DetailWord $g '세 공기 묶음' 18 3615 220 35 '#FFFAEB'
            DetailWord $g '210 g × 3' 18 3700 220 39 '#FFFAEB'
            DetailWord $g '골목찬방' 18 2100 220 47 '#FFFAEB'
            DetailWord $g '간편하게' 18 2320 220 42 '#FFFAEB'
            DetailWord $g '세 공기' 18 2400 220 57 '#FFFAEB'
            DetailWord $g '전자레인지 조리' 18 2610 220 29 '#FFFAEB'
            $g.FillRectangle([Drawing.Brushes]::Ivory,0,960,256,960)
            DetailWord $g $rice[$v] 18 1120 220 60 $hues[$v]
            DetailWord $g '3개입' 18 1220 220 42 $hues[$v]
            DetailWord $g '개별 용기를 꺼내어' 15 1430 226 27 '#3C403A'
            DetailWord $g '뚜껑의 조리법을' 15 1495 226 28 '#3C403A'
            DetailWord $g '확인하세요' 15 1560 226 28 '#3C403A'
            DetailWord $g '포장띠  종이류' 18 1770 220 26 '#3C403A'
            DetailWord $g '상온 보관' 18 230 220 38 '#FFFAEB'
            DetailWord $g '구입처 교환 · 고객센터' 18 405 220 22 '#FFFAEB'
            DetailWord $g '포장띠를 벗겨' 18 580 220 31 '#FFFAEB'
            DetailWord $g '조리하세요' 18 650 220 31 '#FFFAEB'
            $b.Save((Join-Path $folder ('RiceBundle'+$v+'.png')))
        }finally{$g.Dispose();$b.Dispose()}
    }
    $b=[Drawing.Bitmap]::new(400,560);$g=[Drawing.Graphics]::FromImage($b)
    try{
        $g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::FloralWhite)
        $red=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#A91D28'))
        try{$g.FillRectangle($red,0,0,400,92);$g.FillRectangle($red,22,462,356,4)}finally{$red.Dispose()}
        DetailWord $g 'homeway  오늘의 신선' 20 24 360 42 '#FFFFFF'
        DetailWord $g '파프리카' 22 130 356 74 '#232724'
        DetailWord $g '빨강 · 노랑  /  1개' 22 231 356 36 '#555B54'
        DetailWord $g '1,690' 22 306 356 115 '#1D211D'
        DetailWord $g '원' 321 427 55 29 '#1D211D'
        DetailWord $g '신선식품   ·   낱개 판매' 22 492 356 30 '#4D584A'
        $b.Save((Join-Path $folder 'PaprikaPromotionV1.png'))
    }finally{$g.Dispose();$b.Dispose()}
    for($i=0;$i -lt 3;$i++){
        $b=[Drawing.Bitmap]::new(256,192);$g=[Drawing.Graphics]::FromImage($b)
        try{
            $g.Clear([Drawing.Color]::Ivory);$g.FillRectangle([Drawing.Brushes]::DarkOliveGreen,0,0,256,43)
            DetailWord $g 'MOA  /  TESTER' 10 9 236 25 '#FFFFFF'
            DetailWord $g @('스킨 · 로션','보습 크림','세럼 · 클렌징')[$i] 12 61 232 33 '#333C30'
            DetailWord $g @('14,900원','19,900원','12,900원')[$i] 12 114 232 44 '#293028'
            $b.Save((Join-Path $folder ('BeautyTesterCard'+$i+'.png')))
        }finally{$g.Dispose();$b.Dispose()}
    }
}
if($RefreshFiles){
    . (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
    Write-RetailDetailPrints (Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Packs')
}
