param([switch]$PluginOnly)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$compileRoot = Join-Path $projectRoot '.utmp/AndroidReadiness/Compile'
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Data'
$compilerExe = Join-Path $unityData 'DotNetSdk/dotnet.exe'
$compilerDll = Join-Path $unityData 'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$cache = Get-ChildItem "$projectRoot/Library/Bee/artifacts" -Filter 'Assembly-CSharp.rsp' -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $cache) { throw 'Unity compiler response cache is required.' }
New-Item -ItemType Directory -Force $compileRoot | Out-Null
$assemblyOrder = @('AllIn1SpriteShaderAssembly')
if (!$PluginOnly) { $assemblyOrder += @('Combat.Runtime','Enemy.Runtime','Boss.Runtime','Encounter.Runtime','Assembly-CSharp','Assembly-CSharp-Editor') }
Push-Location $projectRoot
try {
    foreach ($assemblyName in $assemblyOrder) {
        $playerCompile = $assemblyName -eq 'AllIn1SpriteShaderAssembly'
        $lines = Get-Content (Join-Path $cache.DirectoryName "$assemblyName.rsp") | Where-Object {
            $source = $_.Trim('"')
            (!$source.EndsWith('.cs') -or (Test-Path -LiteralPath $source)) -and
            (!$playerCompile -or ($_ -notmatch '^-define:UNITY_EDITOR' -and $_ -notmatch '^-(r|reference):.*UnityEditor' -and $_ -notmatch '^-define:UNITY_STANDALONE'))
        } | ForEach-Object {
            if ($_ -match '^[-/]out:') { '-out:"' + (Join-Path $compileRoot "$assemblyName.dll") + '"' }
            elseif ($_ -match '^[-/]refout:') { '-refout:"' + (Join-Path $compileRoot "$assemblyName.ref.dll") + '"' }
            elseif (!$playerCompile -and $_ -match '^[-/](?:r|reference):.*[/\\]([^/\\"]+?)(\.ref)?\.dll"?$' -and
                $Matches[1] -ne $assemblyName -and $assemblyOrder -contains $Matches[1] -and
                (Test-Path (Join-Path $compileRoot ($Matches[1] + '.ref.dll')))) {
                '-r:"' + (Join-Path $compileRoot ($Matches[1] + '.ref.dll')) + '"'
            } else { $_ }
        }
        if ($playerCompile) { $lines += '-define:UNITY_ANDROID' }
        if ($assemblyName -eq 'Encounter.Runtime') { $lines += '-r:"' + (Join-Path $compileRoot 'Combat.Runtime.ref.dll') + '"' }
        # 캐시 작성 이후 추가된 소스를 포함하되 기존 asmdef 경계는 유지합니다.
        $extra = @()
        if ($assemblyName -eq 'Assembly-CSharp') {
            $extra = @(Get-ChildItem 'Assets/0.Scripts' -Filter '*.cs' -Recurse | Where-Object {
                $_.FullName -notmatch '[\\/]Editor[\\/]|[\\/]Game[\\/](Combat|Encounter)[\\/]|[\\/]Enemy[\\/](NormalEnemy|Boss)[\\/]'
            })
        } elseif ($assemblyName -eq 'Encounter.Runtime') { $extra = @(Get-ChildItem 'Assets/0.Scripts/Game/Encounter' -Filter '*.cs') }
        elseif ($assemblyName -eq 'Assembly-CSharp-Editor') { $extra = @(Get-ChildItem 'Assets/0.Scripts/Framework/Editor' -Filter '*.cs') }
        foreach ($file in $extra) {
            $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\','/')
            if (!(($lines -join "`n").Replace('\','/').Contains($relative))) { $lines += '"' + $file.FullName + '"' }
        }
        $rsp = Join-Path $compileRoot "$assemblyName.rsp"
        $lines | Set-Content -LiteralPath $rsp
        $log = Join-Path $compileRoot "$assemblyName.log"
        & $compilerExe $compilerDll "@$rsp" *> $log
        $result = $LASTEXITCODE
        Get-Content -LiteralPath $log
        Write-Output "$assemblyName exit=$result (player-conditions=$playerCompile)"
        if ($result -ne 0) { throw "$assemblyName compilation failed." }
    }
} finally { Pop-Location }
