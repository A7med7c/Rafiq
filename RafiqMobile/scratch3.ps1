Add-Type -AssemblyName System.Drawing
$imgPath = 'C:\Users\Ahmed Ragab\source\repos\Rafiq\RafiqAngular\public\images\RafiqLogo.png'
$img = [System.Drawing.Image]::FromFile($imgPath)
Write-Output "W: $($img.Width) H: $($img.Height)"
