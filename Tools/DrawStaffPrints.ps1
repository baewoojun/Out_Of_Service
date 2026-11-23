param([switch]$Install)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$project=Split-Path $PSScriptRoot -Parent
$target=Join-Path $project 'Review/StaffRoomDraft'
$null=New-Item -ItemType Directory -Path $target -Force
foreach($kind in @('shift','maintenance')) {
 $bmp=[Drawing.Bitmap]::new(512,724);$g=[Drawing.Graphics]::FromImage($bmp)
 $g.Clear([Drawing.Color]::FromArgb(237,235,219));$g.TextRenderingHint=[Drawing.Text.TextRenderingHint]::AntiAliasGridFit
 function Text([string]$s,[int]$size,[int]$x,[int]$y,[int]$width=450,[bool]$bold=$false) {
  $f=[Drawing.Font]::new('Malgun Gothic',$size,$(if($bold){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular}),[Drawing.GraphicsUnit]::Pixel)
  $g.DrawString($s,$f,[Drawing.Brushes]::Black,[Drawing.RectangleF]::new($x,$y,$width,130));$f.Dispose()
 }
 $g.FillRectangle([Drawing.Brushes]::Firebrick,0,0,512,18)
 Text '홈모아 직원 안내' 28 32 38 450 $true
 if($kind -eq 'shift') {
  Text '야간 근무 교대표' 37 32 93 450 $true
  Text '11월  /  마감조' 21 32 153
  $rows=@('구역        인원    확인','신선식품      2      □','가공식품      2      □','생활·가전     1      □','패션·잡화     1      □','고객센터      1      □')
  for($i=0;$i -lt $rows.Count;$i++) {$y=205+$i*53;$g.DrawLine([Drawing.Pens]::Gray,32,$y+42,480,$y+42);Text $rows[$i] 22 40 $y}
  Text '교대 전 냉장고 문과 전원을 확인하세요.' 18 32 558
  Text '분실물은 고객센터에 인계 바랍니다.' 18 32 604
 } else {
  Text '전원 점검 중' 50 32 119 450 $true
  Text '잠시 이용을 멈춰 주세요.' 27 32 220
  $g.DrawLine([Drawing.Pens]::Gray,32,300,480,300)
  Text '음료 자동판매기' 28 32 335 450 $true
  Text '담당: 시설관리실' 22 32 405
  Text '점검 종료 후 안내문을 제거합니다.' 20 32 475
  Text '불편을 드려 죄송합니다.' 22 32 590
 }
 $bmp.Save((Join-Path $target ($kind+'.png')),[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bmp.Dispose()
}
if($Install) {
 $dest=Join-Path $project 'Assets/OutOfService/Art/Prints'
 foreach($kind in @('shift','maintenance')){Copy-Item -LiteralPath (Join-Path $target ($kind+'.png')) -Destination (Join-Path $dest ('staff-'+$kind+'.png')) -Force}
}
Write-Output 'Two original staff-room prints authored.'
