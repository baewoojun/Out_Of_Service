Add-Type -AssemblyName System.Drawing

$target = Join-Path $PSScriptRoot '..\Assets\OutOfService\Art\Signs'
New-Item -ItemType Directory -Path $target -Force | Out-Null

$aisles = @(
    @('aisle_10','10','면 · 간편식','라면 / 즉석밥 / 통조림'),
    @('aisle_11','11','과자 · 간식','스낵 / 비스킷 / 시리얼'),
    @('aisle_12','12','음료 · 생수','생수 / 탄산 / 주스'),
    @('aisle_13','13','커피 · 조미료','커피 / 차 / 양념')
)
$departments = @(
    @('food','식품 · 생활 · 가전','1F  FOOD / LIVING / APPLIANCES'),
    @('service','고객센터','교환 · 환불 · 안내'),
    @('bakery','베이커리','빵 · 디저트'),
    @('produce','채소 · 과일','청과 / 버섯 / 포장채소'),
    @('frozen','냉장 · 냉동','유제품 / 냉동식품'),
    @('living','생활용품','세제 / 위생 / 반려동물'),
    @('fashion','패션 · 고객센터','2F  FASHION & SERVICE'),
    @('clothing','여성 · 남성의류','여성 / 남성 / 아동'),
    @('accessories','신발 · 잡화','가방 / 신발 / 액세서리'),
    @('fitting','탈의실','FITTING ROOM'),
    @('hardware','공구 · 생활용품','공구 / 전기 / 청소'),
    @('meat','정육','국내산 / 수입육 / 양념육'),
    @('seafood','수산','생선 / 해산물 / 손질'),
    @('beverages','음료 · 생수','생수 / 탄산 / 주스 / 차'),
    @('frozen_food','냉동식품','만두 / 아이스크림 / 간편식')
)

function Draw-Centered($graphics, $text, $font, $brush, $x, $y, $width, $height) {
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $format.Trimming = [System.Drawing.StringTrimming]::EllipsisCharacter
    $graphics.DrawString($text, $font, $brush, [System.Drawing.RectangleF]::new($x,$y,$width,$height), $format)
    $format.Dispose()
}

function Make-Sign($id, $line1, $line2, $line3, $isAisle) {
    $bitmap = [System.Drawing.Bitmap]::new(1600, 420)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear([System.Drawing.Color]::FromArgb(245,243,237))
    $red = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(177,12,27))
    $white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $black = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(31,34,37))
    $gray = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(66,68,70))
    $fontFamily = [System.Drawing.FontFamily]::new('Malgun Gothic')
    if ($isAisle) {
        $graphics.FillRectangle($red, 0, 0, 1600, 160)
        $headerFont = [System.Drawing.Font]::new($fontFamily, 65, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $titleFont = [System.Drawing.Font]::new($fontFamily, 100, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $detailFont = [System.Drawing.Font]::new($fontFamily, 55, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        Draw-Centered $graphics "Homeway   |   $line1" $headerFont $white 0 10 1600 140
        Draw-Centered $graphics $line2 $titleFont $black 0 165 1600 140
        Draw-Centered $graphics $line3 $detailFont $gray 0 315 1600 90
    } else {
        $graphics.FillRectangle($red, 0, 0, 1600, 315)
        $titleFont = [System.Drawing.Font]::new($fontFamily, 132, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $detailFont = [System.Drawing.Font]::new($fontFamily, 57, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        Draw-Centered $graphics $line1 $titleFont $white 0 22 1600 265
        Draw-Centered $graphics $line2 $detailFont $black 0 325 1600 78
    }
    $bitmap.Save((Join-Path $target "$id.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    $red.Dispose(); $white.Dispose(); $black.Dispose(); $gray.Dispose()
    $fontFamily.Dispose()
    if ($headerFont) { $headerFont.Dispose() }
    $titleFont.Dispose(); $detailFont.Dispose()
}

foreach ($entry in $aisles) { Make-Sign $entry[0] $entry[1] $entry[2] $entry[3] $true }
foreach ($entry in $departments) { Make-Sign $entry[0] $entry[1] $entry[2] '' $false }
 $brand = [System.Drawing.Bitmap]::new(1600, 240)
 $brandGraphics = [System.Drawing.Graphics]::FromImage($brand)
 $brandGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
 $brandGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
 $brandGraphics.Clear([System.Drawing.Color]::FromArgb(179, 11, 29))
 $brandFont = [System.Drawing.Font]::new('Arial', 155, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
 $brandBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
 Draw-Centered $brandGraphics 'Homeway' $brandFont $brandBrush 0 5 1600 230
 $brand.Save((Join-Path $target 'entrance_brand.png'), [System.Drawing.Imaging.ImageFormat]::Png)
 $brandGraphics.Dispose(); $brand.Dispose(); $brandFont.Dispose(); $brandBrush.Dispose()
 $produceNames = @('사과','배','감귤','양파','감자','대파','토마토','오이','고구마')
 for ($i = 0; $i -lt $produceNames.Count; $i++) {
     $tag = [System.Drawing.Bitmap]::new(600, 400)
     $tg = [System.Drawing.Graphics]::FromImage($tag)
     $tg.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
     $tg.Clear([System.Drawing.Color]::FromArgb(249, 248, 241))
     $r = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(177, 12, 27))
     $w = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
     $d = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(30, 32, 35))
     $tg.FillRectangle($r, 0, 0, 600, 105)
     $smallFont = [System.Drawing.Font]::new('Malgun Gothic', 45, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
     $nameFont = [System.Drawing.Font]::new('Malgun Gothic', 74, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
     $priceFont = [System.Drawing.Font]::new('Arial', 115, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
     $unit=@('낱개 · 1개 기준','4입 상자','낱개 · 100 g','낱개 · 100 g','낱개 · 100 g','묶음 · 1단','6입 팩','3입 묶음','낱개 · 100 g')[$i]
     Draw-Centered $tg $unit $smallFont $w 0 4 600 98
     Draw-Centered $tg $produceNames[$i] $nameFont $d 0 115 600 105
     $price = @('1,490','7,980','590','290','390','2,490','3,980','1,990','490')[$i]
     Draw-Centered $tg $price $priceFont $r 0 223 600 156
     $tag.Save((Join-Path $target "produce_price_$i.png"), [System.Drawing.Imaging.ImageFormat]::Png)
     $tg.Dispose(); $tag.Dispose(); $r.Dispose(); $w.Dispose(); $d.Dispose()
     $smallFont.Dispose(); $nameFont.Dispose(); $priceFont.Dispose()
 }
Write-Output "Generated $($aisles.Count + $departments.Count) market sign textures in $target"
