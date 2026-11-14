# Unity Roslyn 및 실제 엔진 참조로 컴파일하며 Library의 출력은 수정하지 않습니다.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputRoot = Join-Path $projectRoot '.utmp/EnemyStageMixing/Compile'
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Data'
$cache = Get-ChildItem "$projectRoot/Library/Bee/artifacts" -Recurse -Filter 'Enemy.Runtime.rsp' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (!$cache) { throw 'Unity 컴파일 응답 캐시가 없습니다.' }
New-Item -ItemType Directory -Force $outputRoot | Out-Null
$assemblies = @('Combat.Runtime', 'Enemy.Runtime', 'Boss.Runtime', 'Encounter.Runtime', 'Assembly-CSharp', 'Assembly-CSharp-Editor')
Push-Location $projectRoot
try {
    foreach ($assembly in $assemblies) {
        $lines = @(Get-Content (Join-Path $cache.DirectoryName "$assembly.rsp") | Where-Object {
            !$_.Trim('"').EndsWith('.cs') -or (Test-Path -LiteralPath $_.Trim('"'))
        } | ForEach-Object {
            if ($_ -match '^[-/]out:') { '-out:"' + (Join-Path $outputRoot "$assembly.dll") + '"' }
            elseif ($_ -match '^[-/]refout:') { '-refout:"' + (Join-Path $outputRoot "$assembly.ref.dll") + '"' }
            elseif ($_ -match '^[-/](?:r|reference):.*[/\\]([^/\\"]+?)(\.ref)?\.dll"?$' -and $Matches[1] -ne $assembly -and $assemblies -contains $Matches[1] -and (Test-Path (Join-Path $outputRoot ($Matches[1] + '.ref.dll')))) {
                '-r:"' + (Join-Path $outputRoot ($Matches[1] + '.ref.dll')) + '"'
            } else { $_ }
        })
        $extra = @()
        if ($assembly -eq 'Enemy.Runtime') { $extra = @(Get-ChildItem 'Assets/0.Scripts/Game/Enemy/NormalEnemy' -Filter '*.cs') }
        elseif ($assembly -eq 'Encounter.Runtime') { $extra = @(Get-ChildItem 'Assets/0.Scripts/Game/Encounter' -Filter '*.cs') }
        elseif ($assembly -eq 'Assembly-CSharp') {
            $extra = @(Get-ChildItem 'Assets/0.Scripts' -Filter '*.cs' -Recurse | Where-Object { $_.FullName -notmatch '[\\/]Editor[\\/]|[\\/]Game[\\/](Combat|Encounter)[\\/]|[\\/]Enemy[\\/](NormalEnemy|Boss)[\\/]' })
        } elseif ($assembly -eq 'Assembly-CSharp-Editor') {
            $extra = @(Get-ChildItem 'Assets/Editor','Assets/0.Scripts/Framework/Editor' -Filter '*.cs')
        }
        foreach ($file in $extra) {
            $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
            if (!(($lines -join "`n").Replace('\', '/').Contains($relative))) { $lines += '"' + $file.FullName + '"' }
        }
        $rsp = Join-Path $outputRoot "$assembly.rsp"
        $lines | Set-Content -LiteralPath $rsp
        $log = Join-Path $outputRoot "$assembly.log"
        & "$unityData/DotNetSdk/dotnet.exe" "$unityData/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll" "@$rsp" *> $log
        $result = $LASTEXITCODE
        Get-Content -LiteralPath $log
        Write-Output "$assembly exit=$result"
        if ($result -ne 0) { throw "$assembly 컴파일 실패" }
    }
} finally { Pop-Location }
