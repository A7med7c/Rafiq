Add-Type -AssemblyName System.Drawing
$imgPath = 'C:\Users\Ahmed Ragab\source\repos\Rafiq\RafiqAngular\public\images\RafiqLogo.png'
$img = [System.Drawing.Image]::FromFile($imgPath)
$bmp = new-object System.Drawing.Bitmap($img)
$width = $bmp.Width
$height = $bmp.Height

$minX = $width
$maxX = 0

for ($x = 0; $x -lt $width; $x++) {
    for ($y = 0; $y -lt $height; $y++) {
        $pixel = $bmp.GetPixel($x, $y)
        if ($pixel.A -gt 0) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
        }
    }
}

$visibleCenter = ($minX + $maxX) / 2.0
$percentage = ($visibleCenter / $width) * 100

Write-Output "Width: $width, minX: $minX, maxX: $maxX, VisibleCenter: $visibleCenter, Percentage: $percentage"
