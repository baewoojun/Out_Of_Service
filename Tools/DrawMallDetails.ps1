$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dest=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/MallDetails'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$bmp=New-Object Drawing.Bitmap 2048,2048
$g=[Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode='AntiAlias'; $g.TextRenderingHint='AntiAliasGridFit'
$font=New-Object Drawing.Font 'Malgun Gothic',38,([Drawing.FontStyle]::Bold),([Drawing.GraphicsUnit]::Pixel)
$small=New-Object Drawing.Font 'Malgun Gothic',24,([Drawing.FontStyle]::Regular),([Drawing.GraphicsUnit]::Pixel)
$large=New-Object Drawing.Font 'Malgun Gothic',78,([Drawing.FontStyle]::Bold),([Drawing.GraphicsUnit]::Pixel)
$dark=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(30,51,42))
$red=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(143,48,43))
$paper=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(239,235,218))
$pen=New-Object Drawing.Pen ([Drawing.Color]::FromArgb(112,126,115)),2
$cells=@(
 @('잎새 화원','오늘의 절화','3,500','한 송이 · 꽃의 종류별 가격'),
 @('계절 꽃다발','마음을 담아','18,000','예약 · 포장 가능합니다'),
 @('초록을 곁에','실내 관엽 식물','12,000','흙이 마르면 물을 주세요'),
 @('꽃 오래 보기','매일 물 갈기','LEAF','줄기 끝을 사선으로 잘라 주세요'),
 @('바늘 세탁·수선','보관 번호','042','셔츠 2 · 바지 1 / 인수 확인'),
 @('세탁 접수표','보관 번호','137','수선 완료 · 찾으실 때 지참'),
 @('바늘 수선실','옷에 맞춰 드려요','수선','기장 · 지퍼 · 단추 / 상담 접수'),
 @('모퉁이 커피','오늘도 한 잔','COFFEE','진한 향 · 부드러운 하루'),
 @('모퉁이 로스터스','원두 · 중간 볶음','BLEND','견과와 초콜릿 향 / 200 g'),
 @('마신 자리는 깨끗이','반납하는 곳','RETURN','컵 · 쟁반을 올려 주세요'),
 @('이용 안내','셀프 서비스','SELF','물 · 냅킨 · 빨대'),
 @('한끼 식당','따뜻한 한 상','7,900','오늘의 정식 · 포장 가능'),
 @('주문서','주문 번호','028','커피 2 / 포장 1 / 결제 완료'),
 @('잎새 포장실','꽃 주문서','016','계절 꽃다발 / 오늘 수령'),
 @('바늘 세탁','취급 안내','CARE','세탁 완료 / 비닐을 벗겨 보관'),
 @('한끼 식당','식기 반납','감사합니다','그릇과 수저를 나눠 주세요')
)
for($i=0;$i -lt 16;$i++){
 $x=($i%4)*512;$y=[math]::Floor($i/4)*512
 $g.FillRectangle($paper,$x,$y,512,512)
 $g.FillRectangle($dark,($x+14),($y+14),484,73)
 $g.DrawString($cells[$i][0],$font,[Drawing.Brushes]::White,($x+32),($y+28))
 $g.DrawString($cells[$i][1],$font,$dark,($x+30),($y+115))
 $g.DrawString($cells[$i][2],$large,$red,($x+28),($y+197))
 $g.DrawLine($pen,($x+30),($y+315),($x+482),($y+315))
 $g.DrawString($cells[$i][3],$small,$dark,($x+30),($y+340))
 $g.DrawString('우리 동네 몰  |  B1',$small,$dark,($x+30),($y+402))
 for($b=0;$b -lt 55;$b++){if(($b*7+$i)%5 -lt 3){$g.FillRectangle($dark,($x+31+$b*3),($y+460),2,22)}}
}
$bmp.Save((Join-Path $dest 'mall-detail-atlas.png'),[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$bmp.Dispose();$font.Dispose();$small.Dispose();$large.Dispose();$dark.Dispose();$red.Dispose();$paper.Dispose();$pen.Dispose()
Write-Output 'Original 16-cell mall signage atlas written.'
