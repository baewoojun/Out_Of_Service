# Original label art, drawn from Korean retail category conventions; no copied packaging scan.
function Draw-RetailLabel($g,$i,$variant,$displayName,$brandName,$family,$format) {
  if ($i -notin @(10,11,12,14,17,27,28)) { return }
  $palettes = @{
    10=@('#E6F0F4','#E6EDF1','#F5D8E0','#DCEFE9','#EEF1F7','#E1EDF1');
    11=@('#BC1023','#147044','#172853','#E17915','#E9F1F5','#D2A239');
    12=@('#0759A8','#1E643C','#172946','#98BD45','#D6920A','#122C5D');
    14=@('#A4AD49','#B39258','#3E6F3D','#605229','#B99E69','#F0DDB7');
    17=@('#47382D','#B79B6C','#272321','#623B2C','#987B53','#263F32');
    27=@('#28558A','#96B1CD','#D5E9B3','#1E8038','#B7D0E0','#DCC8EA');
    28=@('#ECF2E5','#EFF0E8','#F3ECE0','#E2ECF2','#F0E4E0','#EEE8DF')
  }
  $color=[Drawing.ColorTranslator]::FromHtml($palettes[$i][$variant]);$g.Clear($color)
  $dark=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(25,47,59));$white=[Drawing.SolidBrush]::new([Drawing.Color]::White)
  $accent=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(28,80,123));$pale=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(245,244,230))
  $title=[Drawing.Font]::new($family,58,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
  $smallType=[Drawing.Font]::new($family,23,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
  $ink=if($i -eq 10 -or $i -eq 28 -or ($i -eq 27 -and $variant -ne 0 -and $variant -ne 3) -or ($i -eq 11 -and $variant -eq 4)){$dark}else{$white}
  if($i -eq 10){
    $g.FillPolygon($accent,[Drawing.Point[]]@([Drawing.Point]::new(40,190),[Drawing.Point]::new(190,75),[Drawing.Point]::new(310,180),[Drawing.Point]::new(400,100),[Drawing.Point]::new(480,190)))
    $g.FillRectangle($white,0,194,512,180)
    $g.DrawString('깨끗한 물 · 먹는샘물',$smallType,$dark,[Drawing.RectangleF]::new(10,365,492,45),$format)
  } elseif($i -eq 11){
    $g.FillEllipse($pale,-100,340,720,105)
    $g.DrawString('ORIGINAL  •  SPARKLING',$smallType,$ink,[Drawing.RectangleF]::new(10,90,492,55),$format)
  } elseif($i -eq 12){
    $g.FillPolygon($white,[Drawing.Point[]]@([Drawing.Point]::new(0,380),[Drawing.Point]::new(512,320),[Drawing.Point]::new(512,365),[Drawing.Point]::new(0,430)))
    $g.DrawString('ION SUPPLY DRINK',$smallType,$ink,[Drawing.RectangleF]::new(10,98,492,55),$format)
  } elseif($i -eq 14){
    for($leaf=0;$leaf -lt 5;$leaf++){ $g.FillEllipse($pale,60+$leaf*77,335+($leaf%2)*20,45,80) }
  } elseif($i -eq 17){
    $g.FillEllipse($pale,135,320,242,75);$g.FillEllipse($dark,155,329,202,45)
    $g.DrawString('ROASTED COFFEE',$smallType,$ink,[Drawing.RectangleF]::new(10,90,492,55),$format)
  } elseif($i -eq 27){
    $g.FillEllipse($white,38,126,436,258)
    $ink=$dark
    $g.DrawString('깨끗한 세탁 · 산뜻한 향기',$smallType,$dark,[Drawing.RectangleF]::new(10,363,492,55),$format)
  } else {
    $g.FillRectangle($white,0,160,512,180)
    $g.DrawString('부드러운 감촉  /  생활의 편안함',$smallType,$dark,[Drawing.RectangleF]::new(10,350,492,55),$format)
  }
  $g.DrawString($brandName,$smallType,$ink,[Drawing.RectangleF]::new(18,22,476,50),$format)
  $g.DrawString($displayName,$title,$ink,[Drawing.RectangleF]::new(18,185,476,150),$format)
  $unit=if($i -eq 28){if($variant -eq 1 -or $variant -eq 3){'미용티슈  •  250매'}elseif($variant -eq 2){'키친타월  •  2롤'}else{'두루마리  •  6롤'}}elseif($i -eq 27){'액체세제  •  2 L'}else{'500 mL'}
  $g.DrawString($unit,$smallType,$ink,[Drawing.RectangleF]::new(10,440,492,45),$format)
  $title.Dispose();$smallType.Dispose();$dark.Dispose();$white.Dispose();$accent.Dispose();$pale.Dispose()
}
