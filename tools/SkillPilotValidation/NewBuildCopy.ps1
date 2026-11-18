param([string]$Phase = 'baseline')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$copyRoot = Join-Path ([IO.Path]::GetTempPath()) ('sbsp-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $copyRoot | Out-Null
foreach ($folder in @('Assets','Packages','ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination $copyRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path "$copyRoot/Assets/Editor" | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/BuildPlayer.cs" -Destination "$copyRoot/Assets/Editor/SkillPilotBuildPlayer.cs"
New-Item -ItemType Directory -Force -Path "$projectRoot/.utmp/skill-pilot" | Out-Null
[IO.File]::WriteAllText("$projectRoot/.utmp/skill-pilot/$Phase-path.txt", $copyRoot)
Write-Output $copyRoot
