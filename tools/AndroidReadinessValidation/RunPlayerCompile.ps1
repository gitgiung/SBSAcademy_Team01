param()
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Data'
$outputRoot = Join-Path $projectRoot '.utmp/AndroidReadiness/PlayerCompile'
$cache = Get-ChildItem "$projectRoot/Library/Bee/artifacts" -Filter 'Assembly-CSharp.rsp' -Recurse |
    Where-Object {
        $content = Get-Content -LiteralPath $_.FullName
        $content -contains '-define:UNITY_ANDROID' -and $content -notcontains '-define:UNITY_EDITOR'
    } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $cache) { throw 'An Android player compilation response cache is required. Run an Android build first.' }
New-Item -ItemType Directory -Force $outputRoot | Out-Null
# NUnit must never be required by the regular game assembly, even when the
# Editor cache supplies it. Keep all actual Android game sources and references.
$lines = Get-Content -LiteralPath $cache.FullName | Where-Object {
    $_ -notmatch '^[-/](r|reference):.*nunit\.framework\.dll'
} | ForEach-Object {
    if ($_ -match '^[-/]out:') { '-out:"' + (Join-Path $outputRoot 'Assembly-CSharp.dll') + '"' }
    elseif ($_ -match '^[-/]refout:') { '-refout:"' + (Join-Path $outputRoot 'Assembly-CSharp.ref.dll') + '"' }
    else { $_ }
}
# Mobile UI components added since the last Editor build remain in Assembly-CSharp.
foreach ($file in Get-ChildItem "$projectRoot/Assets/0.Scripts/Mobile" -Filter '*.cs') {
    $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
    if (!(($lines -join "`n").Replace('\', '/').Contains($relative))) { $lines += '"' + $file.FullName + '"' }
}
$rsp = Join-Path $outputRoot 'Assembly-CSharp.rsp'
$log = Join-Path $outputRoot 'compile.log'
$lines | Set-Content -LiteralPath $rsp
Push-Location $projectRoot
try {
    & "$unityData/DotNetSdk/dotnet.exe" "$unityData/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll" "@$rsp" *> $log
    $result = $LASTEXITCODE
    Get-Content -LiteralPath $log
    Write-Output "Android Assembly-CSharp without NUnit: exit=$result"
    if ($result -ne 0) { throw 'Android game assembly compilation without NUnit failed.' }
    Add-Type -Path "$unityData/Managed/Unity.Cecil.dll"
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $outputRoot 'Assembly-CSharp.dll'))
    try {
        $nunit = @($assembly.MainModule.AssemblyReferences | Where-Object Name -eq 'nunit.framework')
        $testTypes = @($assembly.MainModule.Types | Where-Object Name -in @(
            'BossAreaFollowupTests', 'CapsuleColliderCompatibilityTests',
            'GroundShadowTests', 'HitFlashExclusionTests', 'HordeLordRulesTests'
        ))
        if ($nunit.Count -ne 0 -or $testTypes.Count -ne 0) { throw 'Test code or NUnit leaked into the Android game assembly.' }
        Write-Output 'Android game assembly: no NUnit reference or EnemyPhysics test types.'
    } finally { $assembly.Dispose() }
} finally { Pop-Location }
