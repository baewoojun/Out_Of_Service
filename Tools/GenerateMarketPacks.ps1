Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'DrawRetailLabels.ps1')
. (Join-Path $PSScriptRoot 'DrawBeverageLabels.ps1')
. (Join-Path $PSScriptRoot 'DrawRetailRefresh.ps1')
. (Join-Path $PSScriptRoot 'DrawPetRefresh.ps1')
. (Join-Path $PSScriptRoot 'DrawSemanticPackRefresh.ps1')
. (Join-Path $PSScriptRoot 'DrawChocolateRefresh.ps1')
. (Join-Path $PSScriptRoot 'DrawPaperRefresh.ps1')
. (Join-Path $PSScriptRoot 'DrawCannedRefresh.ps1')
. (Join-Path $PSScriptRoot 'DrawDairyRefresh.ps1')
. (Join-Path $PSScriptRoot 'DrawFrozenProduceRefresh.ps1')
. (Join-Path $PSScriptRoot 'DrawRetailDetailPrints.ps1')

. (Join-Path $PSScriptRoot 'DrawStorePackPanels.ps1')
. (Join-Path $PSScriptRoot 'DrawShelfPrints.ps1')
. (Join-Path $PSScriptRoot 'DrawPackInformation.ps1')

$target = Join-Path $PSScriptRoot '..\Assets\OutOfService\Art\Packs'
New-Item -ItemType Directory -Path $target -Force | Out-Null
$atlas1 = [System.Drawing.Bitmap]::new((Join-Path $target 'catalog_photo_atlas_01.png'))
$atlas2 = [System.Drawing.Bitmap]::new((Join-Path $target 'catalog_photo_atlas_02.png'))
$ramenAtlas = [System.Drawing.Bitmap]::new((Join-Path $target 'ramen_front_atlas_v2.png'))

$names = @(
    '봉지라면','컵라면','즉석밥','국 · 찌개','통조림',
    '스낵','비스킷','초콜릿','시리얼','견과류',
    '생수','탄산음료','스포츠음료','과일주스','차음료',
    '원두커피','믹스커피','커피음료','전통차','양념 · 소스',
    '냉동만두','아이스크림','냉동간편식','냉동채소',
    '두부 · 반찬','치즈 · 냉장식품',
    '우유 · 요구르트','세제 · 섬유유연제','화장지 · 위생용품','반려동물 사료'
)
$colors = @(
    '#BB172A','#E86D1E','#1F5FAD','#A03425','#2D6580',
    '#D84B31','#BD7A31','#4F2D29','#E3A629','#778043',
    '#5FADD4','#B6303D','#2F7FAD','#E39B25','#4F8B62',
    '#4A3026','#85583B','#7D5138','#7E9454','#A23D2B',
    '#2C73A3','#79B7CE','#A63146','#3C985F','#69AFC0','#D6A929',
    '#3B89B6','#346A9B','#B3D8ED','#A7783A'
)
$assortment=@(
'얼큰장국면|닭육수면|바다짬뽕|맑은버섯면|구운짜장|새콤비빔면',
'육개장사발|해물큰컵|매운컵면|김치사발|가쓰오우동|볶음큰컵',
'쌀밥|찰보리밥|잡곡밥|현미밥|곤드레밥|흑미밥',
'사골곰탕|육개장|순두부찌개|김치찌개|들깨미역국|된장국',
'살코기참치|돈육햄|골뱅이|꽁치조림|황도|옥수수',
'새우바삭|소금감자|옥수수콘|오징어볼|양파링|치즈퍼프',
'카카오쿠키|버터샌드|곡물크래커|소금크래커|우유비스킷|딸기웨하스',
'밀크초콜릿|땅콩바|카카오큐브|크런치바|다크초콜릿|아몬드바',
'아침콘|초코격자|오곡링|귀리그래놀라|베리링|코코볼',
'하루한줌|볶은아몬드|호두믹스|볶음땅콩|캐슈넛|피스타치오',
'솔샘|바위샘|산들샘|깊은샘|맑은샘|숲속샘',
'밤빛콜라|청귤소다|제로콜라|감귤소다|밀크소다|진저소다',
'블루웨이브|그린웨이브|베리웨이브|라임리셋|오렌지런|파워블랙',
'햇오렌지|진한포도|맑은배|감귤착즙|적포도|사과착즙',
'옥수수차|구운보리|세작녹차|헛개열매|통보리|혼합곡차',
'고소한원두|깊은원두|아침블렌드|산미블렌드|밤의로스트|디카페인',
'모카스틱|로스트미니|라테믹스|헤이즐넛|블랙스틱|밀크모카',
'컵라테|캔아메리카노|캔모카|에스프레소|카라멜라테|더블블랙',
'유자청|생강차|대추차|율무차|쌍화차|매실청',
'양조간장|찰고추장|통참기름|구수한쌈장|현미식초|요리맛술',
'고기교자|납작만두|김치교자|부추만두|얇은피만두|바삭군만두',
'멜론바|바닐라콘|밀크컵|바닐라통|초코바|팥아이스',
'김치볶음밥|치즈피자|감자핫도그|등심돈까스|한입떡갈비|치즈스틱',
'블루베리|브로콜리|채소믹스|딸기|망고|시금치',
'부침두부|콩나물|포기김치|백김치|깍두기|연두부',
'체다슬라이스|모차렐라|무염버터|스트링치즈|고다치즈|크림치즈',
'목장우유|저지방우유|딸기요거트|흰우유|바나나우유|플레인요거트',
'맑은빨래|포근유연제|레몬주방|효소세탁|코튼향|허브주방',
'순면감촉|보들티슈|흡수타월|포켓티슈|순한롤|도톰한롤',
'연어한끼|닭고기한끼|바삭간식|소고기한끼|참치한끼|오리큐브'
)
$weights = @('120 g','86 g','210 g','500 g','150 g','90 g','104 g','70 g','500 g','200 g',
    '500 mL','500 mL','500 mL','1 L','500 mL','200 g','100 sticks','250 mL','1 kg','500 mL',
    '1.05 kg','75 mL','400 g','500 g','300 g','200 g','1 L','2 L','30 rolls','2 kg')
$family = [System.Drawing.FontFamily]::new('Malgun Gothic')
$small = [System.Drawing.Font]::new($family, 25, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$large = [System.Drawing.Font]::new($family, 52, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$medium = [System.Drawing.Font]::new($family, 30, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$format = [System.Drawing.StringFormat]::new()
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
$format.Trimming = [System.Drawing.StringTrimming]::EllipsisCharacter

for ($i = 0; $i -lt $names.Count; $i++) {
  $information=[Drawing.Bitmap]::new(2048,1024)
  for ($variant = 0; $variant -lt 6; $variant++) {
    $printWidth=if($i -in @(0,2,5,9,15,27,28,29,10,11,12,14)){768}else{512}
    $printHeight=if($i -in @(0,2,5,9,15,27,28,29)){768}else{512}
    if($i -eq 7){$printWidth=if($variant -in @(0,4)){384}elseif($variant -eq 2){512}else{768};$printHeight=if($variant -in @(0,4)){768}elseif($variant -eq 2){320}else{256}}
    if($i -eq 28){$paperSize=PaperPrintSize $variant;$printWidth=$paperSize[0];$printHeight=$paperSize[1]}
    if($i -eq 4){$canSize=CannedPrintSize $variant;$printWidth=$canSize[0];$printHeight=$canSize[1]}
    if($i -eq 26){$dairySize=DairyPrintSize $variant;$printWidth=$dairySize[0];$printHeight=$dairySize[1]}
    if($i -eq 23){$printWidth=512;$printHeight=640}
    $bitmap = [System.Drawing.Bitmap]::new($printWidth, $printHeight)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::FromArgb(246, 244, 235))
    $primary = [System.Drawing.ColorTranslator]::FromHtml($colors[($i + $variant * 7) % $colors.Count])
    $primaryBrush = [System.Drawing.SolidBrush]::new($primary)
    $lightBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(240, 239, 232))
    $darkBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(34, 39, 41))
    $whiteBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $displayName=$assortment[$i].Split('|')[$variant]
    $brandName=@('골목찬방','바삭제과','샘결음료','오후다방','서리식탁','살림결','꼬리식탁')[$(if($i -lt 5){0}elseif($i -lt 10){1}elseif($i -lt 15){2}elseif($i -lt 19){3}elseif($i -lt 27){4}elseif($i -lt 29){5}else{6})]
    $atlas=if($i -lt 16){$atlas1}else{$atlas2}
    $n=$i%16;$cell=[int]($atlas.Width/4)
    $src=[Drawing.Rectangle]::new(($n%4)*$cell+4,[int][Math]::Floor($n/4)*$cell+4,$cell-8,$cell-8)
    $accent=[Drawing.ColorTranslator]::FromHtml($colors[$i])
    $tint=[Drawing.Color]::FromArgb(255,[int](($accent.R*3+$primary.R)/4),[int](($accent.G*3+$primary.G)/4),[int](($accent.B*3+$primary.B)/4))
    $primaryBrush.Color=$tint
    $g.Clear([Drawing.Color]::FromArgb(250,247,234))
    if($i -in @(0,1,3,5,20,22)){
      $g.Clear($tint)
      $dest=[Drawing.Rectangle]::new(46,182,420,284)
      $titleY=49;$ink=$whiteBrush
    }elseif($i -in @(6,7,8,9,15,16,18,29)){
      $g.FillRectangle($primaryBrush,0,0,174,512)
      $dest=[Drawing.Rectangle]::new(148,54,352,337)
      $titleY=369;$ink=$darkBrush
      $g.FillRectangle($lightBrush,0,360,512,100)
    }else{
      $dest=[Drawing.Rectangle]::new(139,142,360,294)
      $titleY=62;$ink=$darkBrush
      $g.FillRectangle($primaryBrush,0,0,512,53)
    }
    $g.DrawImage($atlas,$dest,$src,[Drawing.GraphicsUnit]::Pixel)
    $g.DrawString($brandName,$small,$(if($i -in @(6,7,8,9,15,16,18,29)){$whiteBrush}else{$ink}),12,7)
    $g.DrawString($displayName,$large,$ink,[Drawing.RectangleF]::new(0,$titleY,512,98),$format)
    $g.FillRectangle($darkBrush,0,465,512,47)
    $g.DrawString(($names[$i]+'   ·   '+$weights[$i]),$small,$whiteBrush,[Drawing.RectangleF]::new(0,465,512,47),$format)
    if($i -eq 0 -or $i -eq 1){
      $cw=[int]($ramenAtlas.Width/3);$ch=[int]($ramenAtlas.Height/2)
      $crop=[Drawing.Rectangle]::new(($variant%3)*$cw,[int][Math]::Floor($variant/3)*$ch,$cw,$ch)
      $g.DrawImage($ramenAtlas,[Drawing.Rectangle]::new(0,0,512,512),$crop,[Drawing.GraphicsUnit]::Pixel)
      $print=if($variant -in @(1,3)){$darkBrush}else{$whiteBrush}
      $g.DrawString($brandName,$small,$print,13,4)
      $g.DrawString($displayName,$large,$print,[Drawing.RectangleF]::new(12,407,488,79),$format)
      $g.DrawString($weights[$i],$small,$print,391,472)
    }
    Draw-RetailLabel $g $i $variant $displayName $brandName $family $format
    Draw-ConsumerPanel $g $i $variant $displayName $brandName $family $format
    Draw-ShelfPrint $g $i $variant $displayName $brandName
    Draw-BeverageLabel $g $i $variant $displayName
    Draw-RetailRefresh $g $i $variant $displayName
    Draw-PetRefresh $g $i $variant $displayName
    Draw-SemanticPackRefresh $g $i $variant $displayName
    Draw-ChocolateRefresh $g $i $variant $displayName
    Draw-PaperRefresh $g $i $variant $displayName
    Draw-CannedRefresh $g $i $variant $displayName
    Draw-DairyRefresh $g $i $variant $displayName
    Draw-FrozenProduceRefresh $g $i $variant $displayName
    Write-PackInformation $information $bitmap $target $i $variant $displayName $brandName $names[$i]
    $bitmap.Save((Join-Path $target ('pack_{0:D2}_{1}.png' -f $i,$variant)), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bitmap.Dispose(); $primaryBrush.Dispose(); $lightBrush.Dispose(); $darkBrush.Dispose(); $whiteBrush.Dispose()
  }
  $information.Save((Join-Path $target ("RetailInformation"+$i+".png")),[Drawing.Imaging.ImageFormat]::Png)
  $information.Dispose()
}
Write-SidePanel $target $family
Write-HardwareCards $target
Write-RetailDetailPrints $target
Write-StringCheesePrints $target
Write-PetCanPrint $target
Write-DairySealPrints $target
foreach($image in $stockPhotos.Values){$image.Dispose()}
$small.Dispose(); $large.Dispose(); $medium.Dispose(); $format.Dispose(); $family.Dispose()
$atlas1.Dispose(); $atlas2.Dispose()
$ramenAtlas.Dispose()
Write-Output "Generated $($names.Count * 6) package fronts"
