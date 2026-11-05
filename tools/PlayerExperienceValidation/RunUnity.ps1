param([string]$ResultName = 'result')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$validationRoot = Join-Path $projectRoot '.utmp/GrowthUnity'
New-Item -ItemType Directory -Force -Path "$validationRoot/Assets/Runtime", "$validationRoot/Assets/Tests", "$validationRoot/Packages", "$validationRoot/ProjectSettings" | Out-Null
$files = @(
 'Assets/0.Scripts/Game/Player/Data/PlayerData.cs',
 'Assets/0.Scripts/Game/Player/PlayerLoadout.cs',
 'Assets/0.Scripts/Game/Player/PlayerView.cs',
 'Assets/0.Scripts/Game/Player/PlayerCombat.cs',
 'Assets/0.Scripts/Game/Player/Bullet.cs',
 'Assets/0.Scripts/Game/Player/ExperiencePickup.cs',
 'Assets/0.Scripts/Game/Enemy/NormalEnemy/IExperiencePickup.cs',
 'Assets/0.Scripts/Game/Enemy/NormalEnemy/IEnemySpawnPool.cs',
 'Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemyHealth.cs',
 'Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemyFollow.cs',
 'Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemySpawner.cs',
 'Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemySpawnMinute.cs',
 'Assets/0.Scripts/Game/Enemy/NormalEnemy/EnemySpawnDirector.cs',
 'Assets/0.Scripts/Game/Spawning/SceneEnemyPool.cs',
 'Assets/0.Scripts/Game/Combat/CollisionMovement2D.cs',
 'Assets/0.Scripts/Game/Combat/GroundShadow.cs',
 'Assets/0.Scripts/Game/Combat/IDamageable.cs',
 'Assets/0.Scripts/Game/Encounter/BossEncounterController.cs',
 'Assets/0.Scripts/Framework/Managers/Core/PoolManager.cs',
 'Assets/0.Scripts/Framework/Managers/Core/PooledObject.cs',
 'Assets/0.Scripts/Framework/Managers/Core/IPoolable.cs',
 'Assets/0.Scripts/Framework/Data/Data.Contents.cs',
 'Assets/0.Scripts/Framework/Data/CombatSkillTable.cs',
 'Assets/0.Scripts/Framework/Utils/InitBase.cs',
 'Assets/0.Scripts/Framework/Managers/Core/UIManager.cs',
 'Assets/0.Scripts/Framework/UI/Popup/UI_Popup.cs',
 'Assets/0.Scripts/Framework/UI/Popup/UI_CombatPopup.cs',
 'Assets/0.Scripts/Game/UI/CombatPopupController.cs',
 'Assets/0.Scripts/Game/UI/LevelUpSelectionPanel.cs',
 'Assets/0.Scripts/Game/UI/LevelUpSkillDraft.cs',
 'Assets/0.Scripts/Game/UI/LevelUpSkillReward.cs',
 'Assets/0.Scripts/Game/UI/PlayerStatusPanel.cs',
 'Assets/0.Scripts/Game/UI/StatusPanelUI.cs'
)
$files += Get-ChildItem "$projectRoot/Assets/0.Scripts/Game/Player/Data/PlayerExperience*.cs" | ForEach-Object { $_.FullName }
$files += Get-ChildItem "$projectRoot/Assets/0.Scripts/Game/Enemy/NormalEnemy/Survival*.cs" | ForEach-Object { $_.FullName }
foreach ($file in $files) {
 $source = if ([IO.Path]::IsPathRooted($file)) { $file } else { Join-Path $projectRoot $file }
 Copy-Item -LiteralPath $source -Destination "$validationRoot/Assets/Runtime/" -Force
}
Copy-Item "$PSScriptRoot/Unity/Boundary.cs" "$validationRoot/Assets/Runtime/" -Force
Copy-Item "$PSScriptRoot/Unity/GrowthPopupTests.cs" "$validationRoot/Assets/Tests/" -Force
Copy-Item "$PSScriptRoot/Unity/BasicBulletTests.cs" "$validationRoot/Assets/Tests/" -Force
Copy-Item "$PSScriptRoot/Unity/ExperiencePickupColorTests.cs" "$validationRoot/Assets/Tests/" -Force
Copy-Item "$PSScriptRoot/Unity/SurvivalIntegrationTests.cs" "$validationRoot/Assets/Tests/" -Force
Copy-Item "$projectRoot/Assets/TextMesh Pro/Resources" "$validationRoot/Assets/" -Recurse -Force
Copy-Item "$projectRoot/ProjectSettings/ProjectVersion.txt" "$validationRoot/ProjectSettings/" -Force
'{"dependencies":{"com.unity.test-framework":"1.7.0","com.unity.ugui":"2.5.0","com.unity.modules.ui":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.animation":"1.0.0","com.unity.modules.physics2d":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}' | Set-Content "$validationRoot/Packages/manifest.json"
'{"name":"GrowthValidation.Runtime","references":["Unity.TextMeshPro","UnityEngine.UI"]}' | Set-Content "$validationRoot/Assets/Runtime/GrowthValidation.Runtime.asmdef"
'{"name":"GrowthValidation.Tests","references":["GrowthValidation.Runtime","Unity.TextMeshPro","UnityEngine.UI"],"optionalUnityReferences":["TestAssemblies"]}' | Set-Content "$validationRoot/Assets/Tests/GrowthValidation.Tests.asmdef"
$unityExe = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe'
$unityArguments = "-batchmode -nographics -projectPath `"$validationRoot`" -runTests -testPlatform PlayMode -testResults `"$validationRoot/$ResultName.xml`" -logFile `"$validationRoot/$ResultName.log`""
$unityProcess = Start-Process -FilePath $unityExe -ArgumentList $unityArguments -WindowStyle Hidden -PassThru -Wait
if ($unityProcess.ExitCode -ne 0) { throw "격리 Unity 검증 종료: $($unityProcess.ExitCode)" }
if (!(Test-Path "$validationRoot/$ResultName.xml")) { throw 'Unity 결과 XML 없음: 로그 확인 필요' }
[xml]$results = Get-Content "$validationRoot/$ResultName.xml"
$results.'test-run' | Select-Object result, total, passed, failed, skipped
if ($results.'test-run'.result -ne 'Passed') { throw 'Unity 테스트 실패' }
