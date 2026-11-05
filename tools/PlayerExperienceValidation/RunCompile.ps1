# Unity의 기존 컴파일 응답 파일을 읽되 출력은 .utmp로 격리합니다. Library DLL을 덮어쓰지 않습니다.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$compileRoot = Join-Path $projectRoot '.utmp/PlayerExperience/Compile'
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Data'
$compilerExe = Join-Path $unityData 'DotNetSdk/dotnet.exe'
$compilerDll = Join-Path $unityData 'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$runtimeResponse = Get-ChildItem "$projectRoot/Library/Bee/artifacts" -Filter 'Assembly-CSharp.rsp' -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $runtimeResponse) { throw 'Unity 컴파일 응답 파일 없음: 에디터 임포트 후 실행하세요.' }
New-Item -ItemType Directory -Force -Path $compileRoot | Out-Null
Push-Location $projectRoot
try {
    foreach ($assemblyName in @('Assembly-CSharp','Assembly-CSharp-Editor')) {
        $responseFile = Join-Path $runtimeResponse.DirectoryName "$assemblyName.rsp"
        $compileLines = Get-Content -LiteralPath $responseFile | Where-Object {
            $sourcePath = $_.Trim('"')
            # 삭제된 과거 테스트 등 캐시에만 남은 소스를 컴파일하지 않습니다.
            !($sourcePath.EndsWith('.cs')) -or (Test-Path -LiteralPath $sourcePath)
        } | ForEach-Object {
            if ($_ -match '^[-/]out:') { '-out:"' + (Join-Path $compileRoot "$assemblyName.dll") + '"' }
            elseif ($_ -match '^[-/]refout:') { '-refout:"' + (Join-Path $compileRoot "$assemblyName.ref.dll") + '"' }
            elseif ($assemblyName -eq 'Assembly-CSharp-Editor' -and $_ -match '^[-/](?:r|reference):.*[/\\]Assembly-CSharp(\.ref)?\.dll"?$') {
                '-r:"' + (Join-Path $compileRoot 'Assembly-CSharp.ref.dll') + '"'
            } else { $_ }
        }
        if ($assemblyName -eq 'Assembly-CSharp') {
            foreach ($newSource in Get-ChildItem 'Assets/0.Scripts/Game/Player/Data/PlayerExperience*.cs') {
                if (!(($compileLines -join "`n").Replace('\','/').Contains('/' + $newSource.Name))) {
                    $compileLines += '"' + $newSource.FullName + '"'
                }
            }
        }
        $isolatedResponse = Join-Path $compileRoot "$assemblyName.rsp"
        $compileLines | Set-Content -LiteralPath $isolatedResponse
        $compileLog = Join-Path $compileRoot "$assemblyName.log"
        & $compilerExe $compilerDll "@$isolatedResponse" *> $compileLog
        $compileExit = $LASTEXITCODE
        Get-Content -LiteralPath $compileLog
        Write-Output "$assemblyName exit=$compileExit"
        if ($compileExit -ne 0) { throw "$assemblyName 컴파일 실패" }
    }
} finally { Pop-Location }
