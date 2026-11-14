# 별도 최소 프로젝트에 실제 Enemy/Combat 코드와 24개 프리팹을 복사합니다. 원본 프로젝트를 열지 않습니다.
param([string]$ResultName = 'result')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$validationRoot = Join-Path $projectRoot '.utmp/stagemix'
New-Item -ItemType Directory -Force "$validationRoot/Assets/Runtime", "$validationRoot/Assets/Editor", "$validationRoot/Assets/Tests", "$validationRoot/Assets/1.Prefabs/Normal Enemy", "$validationRoot/Packages", "$validationRoot/ProjectSettings" | Out-Null
Copy-Item "$projectRoot/Assets/0.Scripts/Game/Combat" "$validationRoot/Assets/Runtime/" -Recurse -Force
Copy-Item "$projectRoot/Assets/0.Scripts/Game/Enemy/NormalEnemy" "$validationRoot/Assets/Runtime/" -Recurse -Force
Copy-Item "$projectRoot/Assets/1.Prefabs/Normal Enemy/Temp_enemy" "$validationRoot/Assets/1.Prefabs/Normal Enemy/" -Recurse -Force
Copy-Item "$projectRoot/Assets/Editor/EnemySpawnDirectorEditor.cs" "$validationRoot/Assets/Editor/" -Force
Copy-Item "$projectRoot/Assets/Tests/EnemyStageMixing/Editor/*" "$validationRoot/Assets/Tests/" -Force
Copy-Item "$projectRoot/ProjectSettings/ProjectVersion.txt" "$validationRoot/ProjectSettings/" -Force
'{"dependencies":{"com.unity.test-framework":"1.7.0","com.unity.ugui":"2.5.0","com.unity.modules.ui":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.animation":"1.0.0","com.unity.modules.physics2d":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}' | Set-Content "$validationRoot/Packages/manifest.json"
$unityExe = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe'
$unityArguments = "-batchmode -nographics -projectPath `"$validationRoot`" -runTests -testPlatform EditMode -testFilter EnemyStageMixingTests -testResults `"$validationRoot/$ResultName.xml`" -logFile `"$validationRoot/$ResultName.log`""
$process = Start-Process -FilePath $unityExe -ArgumentList $unityArguments -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "격리 Unity 검증 종료: $($process.ExitCode), 로그 $validationRoot/$ResultName.log" }
if (!(Test-Path "$validationRoot/$ResultName.xml")) { throw 'Unity 결과 XML이 없습니다.' }
[xml]$results = Get-Content "$validationRoot/$ResultName.xml"
$results.'test-run' | Select-Object result, total, passed, failed, skipped
if ($results.'test-run'.result -ne 'Passed') { throw 'Unity 테스트 실패' }
