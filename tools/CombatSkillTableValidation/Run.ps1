param([switch]$Export)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.utmp/CombatSkillData/dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
Push-Location $projectRoot
try {
    $arguments = @('run', '--project', (Join-Path $PSScriptRoot 'CombatSkillTableValidation.csproj'))
    if ($Export) { $arguments += @('--', '--export') }
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "전투 스킬 변환 검증 실패: $LASTEXITCODE" }
}
finally { Pop-Location }
