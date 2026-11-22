$ErrorActionPreference = 'Stop'
$ids = @('food_pears_asian_01','yellow_onion','bananas')
$base = Join-Path $PSScriptRoot '..\Assets\OutOfService\Art\Models'
New-Item -ItemType Directory -Force -Path $base | Out-Null

function Save-Verified($entry, $destination) {
    Invoke-WebRequest -Uri $entry.url -OutFile $destination -TimeoutSec 120
    $actual = (Get-FileHash -LiteralPath $destination -Algorithm MD5).Hash.ToLowerInvariant()
    if ($actual -ne $entry.md5.ToLowerInvariant()) {
        throw "Checksum mismatch: $destination"
    }
}

foreach ($id in $ids) {
    $folder = Join-Path $base $id
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $response = Invoke-RestMethod -Uri ("https://api.polyhaven.com/files/$id") -TimeoutSec 30
    $fbx = $response.fbx.'1k'.fbx
    if ($null -eq $fbx) { throw "No 1k FBX for $id" }
    $modelPath = Join-Path $folder "$id.fbx"
    Save-Verified $fbx $modelPath
    foreach ($texture in $fbx.include.PSObject.Properties) {
        $entry = $texture.Value
        $fileName = [IO.Path]::GetFileName($texture.Name)
        Save-Verified $entry (Join-Path $folder $fileName)
    }
    Write-Output "Downloaded $id with $($fbx.include.PSObject.Properties.Count) textures"
}
