param([string]$Base='origin/main')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    $baseCommit=git rev-parse $Base
    if($LASTEXITCODE){throw 'Cannot resolve merge base reference'}
    function AssetText([string]$reference,[string]$path){
        $lines=git show ($reference+':'+$path)
        if($LASTEXITCODE){throw "Cannot read $reference asset $path"}
        return ($lines -join "`n")
    }
    function PipelineConfig([string]$value){
        # Unity regenerates shader prefilter caches for each build target.
        $value=[regex]::Replace($value,'(?m)^  m_Prefilter[^\n]*\n','')
        return [regex]::Replace($value,'(?m)^  - \{fileID: 11400000, guid: 9d6769b045b140ef9f4ba9e79c4a4753, type: 2\}\n','')
    }
    function GlobalConfig([string]$value){
        # The runtime settings list is a build-generated subset of existing refs.
        return [regex]::Replace($value,'(?m)(    m_RuntimeSettings:\n)      m_List:\n(?:      - rid: [0-9]+\n)+','${1}      m_List: []'+"`n")
    }
    $pipeline='Assets/Settings/PC_RPAsset.asset'
    $globalSettings='Assets/Settings/UniversalRenderPipelineGlobalSettings.asset'
    $shared='Assets/Settings/PC_Renderer.asset'
    $pipelineSame=(PipelineConfig (AssetText $Base $pipeline)) -ceq (PipelineConfig (AssetText '' $pipeline))
    $globalSame=(GlobalConfig (AssetText $Base $globalSettings)) -ceq (GlobalConfig (AssetText '' $globalSettings))
    $sharedSame=(AssetText $Base $shared) -ceq (AssetText '' $shared)
    $changed=@(git diff --cached --name-only $Base)
    if($LASTEXITCODE){throw 'Cannot compare staged merge with main'}
    $unexpected=@($changed | Where-Object {$_ -notmatch '^(\.gitattributes$|Assets/OutOfService(?:/|\.meta$)|Assets/StreamingAssets(?:/|\.meta$)|Assets/Scenes/MarketPrototype_v7\.unity(?:\.meta)?$|Review/|Tools/|Assets/Settings/(PC_RPAsset|UniversalRenderPipelineGlobalSettings)\.asset$)'})
    $guidLines=git grep --cached -n -e '^guid: ' -- '*.meta'
    if($LASTEXITCODE){throw 'Cannot inspect the complete index GUIDs'}
    $records=@(foreach($line in $guidLines){if($line -match '^(.*):\d+:guid:\s*([a-fA-F0-9]{32})\s*$'){[pscustomobject]@{path=$Matches[1];guid=$Matches[2]}}})
    $collisions=@($records | Group-Object guid | Where-Object Count -gt 1 | ForEach-Object {@{guid=$_.Name;paths=@($_.Group.path)}})
    $report=[ordered]@{verified_at=(Get-Date -Format o);base_main=$baseCommit;default_renderer_byte_content_preserved=$sharedSame;pipeline_render_settings_preserved=$pipelineSame;global_settings_preserved_except_generated_runtime_list=$globalSame;unexpected_team_changes=$unexpected;combined_unique_guids=@($records.guid | Sort-Object -Unique).Count;meta_files=$records.Count;collisions=$collisions;scope='Complete staged index, including sparse-excluded existing team assets. This is not a full integrated project build.'}
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath 'Review/market-combined-guid-validation-2026-11-23.json' -Encoding utf8
    $report | ConvertTo-Json -Depth 6
    if(-not $sharedSame -or -not $pipelineSame -or -not $globalSame -or $unexpected.Count -or $collisions.Count){throw 'Merged market isolation validation failed'}
} finally {Pop-Location}
