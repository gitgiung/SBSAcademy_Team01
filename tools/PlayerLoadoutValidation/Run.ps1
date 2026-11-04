$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.utmp/PlayerLoadout/dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
& dotnet run --project (Join-Path $PSScriptRoot 'PlayerLoadoutValidation.csproj')
if ($LASTEXITCODE -ne 0) { throw "장착 모델 검증 실패: $LASTEXITCODE" }
