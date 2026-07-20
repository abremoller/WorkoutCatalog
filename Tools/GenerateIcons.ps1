Add-Type -AssemblyName System.Drawing

function New-Bitmap([int]$sz, [System.Drawing.Color]$bg, [scriptblock]$draw) {
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode    = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

    # Rounded-rect background
    $r    = [Math]::Max(2, [int]($sz * 0.15))
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0,        0,        $r*2, $r*2, 180, 90)
    $path.AddArc($sz-$r*2, 0,        $r*2, $r*2, 270, 90)
    $path.AddArc($sz-$r*2, $sz-$r*2, $r*2, $r*2,   0, 90)
    $path.AddArc(0,        $sz-$r*2, $r*2, $r*2,  90, 90)
    $path.CloseAllFigures()
    $g.FillPath((New-Object System.Drawing.SolidBrush($bg)), $path)
    $path.Dispose()

    & $draw $g $sz
    $g.Dispose()
    $bmp
}

function Save-Ico([string]$file, [System.Drawing.Bitmap[]]$bitmaps) {
    $pngs = $bitmaps | ForEach-Object {
        $ms = New-Object System.IO.MemoryStream
        $_.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        , $ms.ToArray()
    }
    $ms2 = New-Object System.IO.MemoryStream
    $bw  = New-Object System.IO.BinaryWriter($ms2)

    $bw.Write([uint16]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]$bitmaps.Count)

    $off = 6 + $bitmaps.Count * 16
    for ($i = 0; $i -lt $bitmaps.Count; $i++) {
        $w = if ($bitmaps[$i].Width  -ge 256) { 0 } else { $bitmaps[$i].Width  }
        $h = if ($bitmaps[$i].Height -ge 256) { 0 } else { $bitmaps[$i].Height }
        $bw.Write([byte]$w);  $bw.Write([byte]$h)
        $bw.Write([byte]0);   $bw.Write([byte]0)
        $bw.Write([uint16]1); $bw.Write([uint16]32)
        $bw.Write([uint32]$pngs[$i].Length)
        $bw.Write([uint32]$off)
        $off += $pngs[$i].Length
    }
    $pngs | ForEach-Object { $bw.Write($_) }
    $bw.Flush()
    [IO.File]::WriteAllBytes($file, $ms2.ToArray())
    $bw.Close()
}

# ── 1. VideoAudioMerger  (blue #1565C0) ── film strip + waveform ─────────────
$vmBitmaps = @(16, 32, 48, 256) | ForEach-Object {
    $sz = $_
    New-Bitmap $sz ([System.Drawing.Color]::FromArgb(21, 101, 192)) {
        param($g, $sz)
        $bg  = [System.Drawing.Color]::FromArgb(21, 101, 192)
        $wb  = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $dbr = New-Object System.Drawing.SolidBrush($bg)

        # Film strip rectangle (upper 40%)
        $fw = [int]($sz * 0.70);  $fh = [int]($sz * 0.34)
        $fx = [int](($sz - $fw) / 2);  $fy = [int]($sz * 0.08)
        $g.FillRectangle($wb, $fx, $fy, $fw, $fh)

        # Sprocket holes (only when big enough)
        if ($sz -ge 24) {
            $hs  = [int]([Math]::Max(2, $sz * 0.07))
            $hy  = $fy + [int](($fh - $hs) / 2)
            $hx0 = [int]($fx + $sz * 0.04)
            $hx1 = [int]($fx + $sz * 0.18)
            $hx2 = [int]($fx + $sz * 0.31)
            $hx3 = [int]($fx + $sz * 0.44)
            foreach ($hx in @($hx0, $hx1, $hx2, $hx3)) {
                $g.FillRectangle($dbr, $hx, $hy, $hs, $hs)
            }
        }

        # Audio waveform bars (lower section)
        $barW    = [Math]::Max(2, [int]($sz * 0.08))
        $gap     = [Math]::Max(1, [int]($sz * 0.04))
        $heights = @([int]($sz*0.13), [int]($sz*0.22), [int]($sz*0.28),
                     [int]($sz*0.22), [int]($sz*0.13))
        $totalW  = 5*$barW + 4*$gap
        $bx      = [int](($sz - $totalW) / 2)
        $by      = [int]($sz * 0.93)
        for ($i = 0; $i -lt 5; $i++) {
            $bh = $heights[$i]
            $g.FillRectangle($wb, $bx + $i*($barW+$gap), $by - $bh, $barW, $bh)
        }

        $wb.Dispose(); $dbr.Dispose()
    }
}
$vmPath = Join-Path $PSScriptRoot "..\VideoAudioMerger\app.ico"
Save-Ico $vmPath $vmBitmaps
Write-Host "  VideoAudioMerger\app.ico"

# ── 2. WorkoutCatalog  (orange #E65100) ── dumbbell ──────────────────────────
$wcBitmaps = @(16, 32, 48, 256) | ForEach-Object {
    $sz = $_
    New-Bitmap $sz ([System.Drawing.Color]::FromArgb(230, 81, 0)) {
        param($g, $sz)
        $wb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

        # Two filled circles connected by a bar
        $cr  = [int]($sz * 0.26)          # circle radius
        $br  = [int]([Math]::Max(2, $sz * 0.09))  # bar half-height
        $cx1 = [int]($sz * 0.22)
        $cx2 = [int]($sz * 0.78)
        $cy  = [int]($sz / 2)

        $g.FillRectangle($wb, $cx1, $cy - $br, $cx2 - $cx1, $br * 2)
        $g.FillEllipse($wb, $cx1 - $cr, $cy - $cr, $cr*2, $cr*2)
        $g.FillEllipse($wb, $cx2 - $cr, $cy - $cr, $cr*2, $cr*2)

        $wb.Dispose()
    }
}
$wcPath = Join-Path $PSScriptRoot "..\WorkoutCatalog\app.ico"
Save-Ico $wcPath $wcBitmaps
Write-Host "  WorkoutCatalog\app.ico"

# ── 3. WorkoutCatalog.Crawler  (purple #4A148C) ── magnifying glass ───────────
$crBitmaps = @(16, 32, 48, 256) | ForEach-Object {
    $sz = $_
    New-Bitmap $sz ([System.Drawing.Color]::FromArgb(74, 20, 140)) {
        param($g, $sz)
        $pw  = [float][Math]::Max(1.5, $sz * 0.09)
        $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, $pw)
        $pen.LineJoin   = [System.Drawing.Drawing2D.LineJoin]::Round
        $pen.EndCap     = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.StartCap   = [System.Drawing.Drawing2D.LineCap]::Round

        $cr = [int]($sz * 0.28)
        $cx = [int]($sz * 0.38)
        $cy = [int]($sz * 0.38)
        $g.DrawEllipse($pen, $cx - $cr, $cy - $cr, $cr*2, $cr*2)

        $hx1 = [int]($cx + $cr * 0.70)
        $hy1 = [int]($cy + $cr * 0.70)
        $hx2 = [int]($sz * 0.87)
        $hy2 = [int]($sz * 0.87)
        $g.DrawLine($pen, $hx1, $hy1, $hx2, $hy2)

        $pen.Dispose()
    }
}
$crPath = Join-Path $PSScriptRoot "..\WorkoutCatalog.Crawler\app.ico"
Save-Ico $crPath $crBitmaps
Write-Host "  WorkoutCatalog.Crawler\app.ico"

Write-Host "Done."
