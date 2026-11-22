param([switch]$FreshOnly)
Add-Type -AssemblyName System.Drawing
$folder=Join-Path $PSScriptRoot '../Assets/OutOfService/Art/Prices'
New-Item -ItemType Directory -Force $folder | Out-Null
$names=@('봉지라면','컵라면','즉석밥','국 · 찌개','통조림','스낵','비스킷','초콜릿','시리얼','견과류','생수','탄산음료','스포츠음료','과일주스','차음료','원두커피','믹스커피','커피음료','전통차','양념 · 소스','냉동식품','화장지','세탁세제','생활용품','반려동물 사료')
$prices=@(4280,1150,6980,3980,2480,1490,2980,1980,6980,7980,480,1680,1780,3980,1480,12900,17900,1280,6980,5980,8980,6980,12900,3980,15900)
$f1=[Drawing.Font]::new('Malgun Gothic',16,[Drawing.FontStyle]::Regular)
$f2=[Drawing.Font]::new('Malgun Gothic',36,[Drawing.FontStyle]::Bold)
$f3=[Drawing.Font]::new('Malgun Gothic',9)
$freshNames=@('표고버섯 300 g','새송이버섯 300 g','양송이버섯 200 g','느타리버섯 200 g','팽이버섯 150 g','모둠 쌈채소 120 g','간편대파 180 g','깐감자 250 g','카레용 채소 200 g')
$freshPrices=@(3490,2490,2990,1980,990,2490,1980,2790,2990)
for($row=0;$row -lt ($names.Count+$freshNames.Count);$row++){
 $fresh=$row -ge $names.Count
 if($FreshOnly -and !$fresh){continue}
 if($fresh){$i=$row-$names.Count;$title=$freshNames[$i];$amount=$freshPrices[$i];$file="fresh_$i.png"}
 else{$i=$row;$title=$names[$i];$amount=$prices[$i];$file='price_{0:D2}.png' -f $i}
 $b=[Drawing.Bitmap]::new(384,128);$g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::White)
 $g.TextRenderingHint=[Drawing.Text.TextRenderingHint]::AntiAliasGridFit
 $g.DrawString($title,$f1,[Drawing.Brushes]::Black,10,2)
 $g.DrawString(('{0:N0}원' -f $amount),$f2,[Drawing.Brushes]::Black,8,35)
 $g.DrawString('바른장터  정상가',$f3,[Drawing.Brushes]::DarkRed,10,105)
 for($bar=0;$bar -lt 28;$bar++){ $g.FillRectangle([Drawing.Brushes]::Black,270+$bar*3,65,1+($bar%3),35) }
 $b.Save((Join-Path $folder $file),[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$b.Dispose()
}
$f1.Dispose();$f2.Dispose();$f3.Dispose()
