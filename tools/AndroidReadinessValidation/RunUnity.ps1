param([string]$ResultName = 'result', [switch]$Graphics)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$validationRoot = Join-Path $env:TEMP 'sbs-android-readiness'
New-Item -ItemType Directory -Force "$validationRoot/Assets", "$validationRoot/Packages", "$validationRoot/ProjectSettings" | Out-Null
# 원본과 다른 프로젝트만 실행합니다. 원본의 Library/설정/전역 Play From Title은 사용하지 않습니다.
$sourceRoot = Join-Path $projectRoot 'Assets/0.Scripts'
foreach ($file in Get-ChildItem $sourceRoot -File -Recurse) {
    $relative = $file.FullName.Substring($sourceRoot.Length + 1)
    if ($relative -match '(^|[\\/])Editor([\\/]|\.meta$)') { continue }
    $destination = Join-Path "$validationRoot/Assets/Runtime" $relative
    New-Item -ItemType Directory -Force (Split-Path $destination) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
}
New-Item -ItemType Directory -Force "$validationRoot/Assets/Joystick", "$validationRoot/Assets/Tests" | Out-Null
New-Item -ItemType Directory -Force "$validationRoot/Assets/Resources/Data" | Out-Null
Copy-Item "$projectRoot/Assets/Resources/Data/*.json" "$validationRoot/Assets/Resources/Data/" -Force
Copy-Item "$projectRoot/Assets/Joystick Pack/Scripts/Base/*.cs" "$validationRoot/Assets/Joystick/" -Force
Copy-Item "$projectRoot/Assets/Joystick Pack/Scripts/Joysticks/*.cs" "$validationRoot/Assets/Joystick/" -Force
Copy-Item "$projectRoot/Assets/Tests/PlayMode/SceneFlow/MobileReadinessTests.cs" "$validationRoot/Assets/Tests/" -Force
Copy-Item "$projectRoot/Assets/Tests/PlayMode/SceneFlow/SceneFlow.PlayModeTests.asmdef" "$validationRoot/Assets/Tests/" -Force
Copy-Item "$projectRoot/ProjectSettings/ProjectVersion.txt" "$validationRoot/ProjectSettings/" -Force
$dependencies = [ordered]@{}
foreach ($name in @('com.unity.ext.nunit','com.unity.test-framework','com.unity.ugui','com.unity.inputsystem','com.unity.addressables','com.unity.cinemachine','com.unity.nuget.newtonsoft-json','com.unity.visualscripting')) {
    $cache = Get-ChildItem "$projectRoot/Library/PackageCache" -Directory | Where-Object Name -Like "$name@*" | Select-Object -First 1
    if ($null -eq $cache) { throw "Missing package cache: $name" }
    $dependencies[$name] = 'file:' + $cache.FullName.Replace('\','/')
}
foreach ($module in @('animation','audio','imgui','jsonserialize','particlesystem','physics','physics2d','ui','uielements','imageconversion','assetbundle','unitywebrequest','unitywebrequestassetbundle','unitywebrequesttexture','tilemap')) {
    $dependencies["com.unity.modules.$module"] = '1.0.0'
}
@{dependencies=$dependencies} | ConvertTo-Json -Depth 5 | Set-Content "$validationRoot/Packages/manifest.json"
$log = "$validationRoot/$ResultName.log"
$xml = "$validationRoot/$ResultName.xml"
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe'
$graphicsOption = if ($Graphics) { '' } else { '-nographics' }
$process = Start-Process -FilePath $unity -ArgumentList "-batchmode $graphicsOption -projectPath `"$validationRoot`" -runTests -testPlatform PlayMode -testFilter MobileReadinessTests -testResults `"$xml`" -logFile `"$log`"" -WindowStyle Hidden -PassThru
Write-Output "Validation PID=$($process.Id), project=$validationRoot"
$process.WaitForExit()
if (Test-Path $xml) {
    [xml]$results = Get-Content $xml
    $results.'test-run' | Select-Object result,total,passed,failed,skipped
    foreach ($failure in $results.SelectNodes('//test-case[failure]')) { Write-Output "$($failure.fullname): $($failure.failure.message.InnerText)" }
}
if ($process.ExitCode -ne 0 -or !(Test-Path $xml) -or $results.'test-run'.result -ne 'Passed') {
    Get-Content $log | Select-String 'error CS|Scripts have compiler errors|Licensing.*(error|not yet initialized)|Aborting batchmode|Another Unity instance' | Select-Object -Last 12
    throw "Unity validation failed or incomplete. Log: $log"
}
