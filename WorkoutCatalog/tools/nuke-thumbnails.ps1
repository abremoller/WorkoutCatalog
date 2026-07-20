[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Root
)

if (-not (Test-Path -LiteralPath $Root -PathType Container))
{
    throw "Root folder not found: $Root"
}

Write-Host "Scanning for thumb.jpg under: $Root" -ForegroundColor Cyan

$thumbs = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter 'thumb.jpg' -ErrorAction SilentlyContinue

Write-Host ("Found {0} thumbnail(s)." -f $thumbs.Count) -ForegroundColor Yellow

foreach ($file in $thumbs)
{
    Remove-Item -LiteralPath $file.FullName -Force -ErrorAction SilentlyContinue
}

Write-Host "Done." -ForegroundColor Green
