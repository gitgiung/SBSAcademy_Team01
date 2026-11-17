param([switch]$ReuseBaseline, [string]$Origin = 'http://127.0.0.1:18763')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$resultRoot = Join-Path $projectRoot '.utmp/skill-pilot'
if ($ReuseBaseline) {
    $copyRoot = Get-Content "$resultRoot/baseline-path.txt"
    $resolvedCopy = [IO.Path]::GetFullPath($copyRoot)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (!$resolvedCopy.StartsWith($resolvedTemp) -or !(Split-Path $resolvedCopy -Leaf).StartsWith('sbsp-')) { throw '검증 복사본 경로가 아닙니다.' }
    # 이전 전 baseline 복사본에만 남아 있는 네 파일을 정확한 경로로 정리한다. 원본은 건드리지 않는다.
    foreach ($old in @('Assets/Resources/Data/CombatSkillTable.json','Assets/Resources/LightSkills/Fx/WaterArrow.png','Assets/Resources/LightSkills/Fx/Frames/WaterArrow.json','Assets/Resources/SkillIcons/Skill_10025.png')) {
        foreach ($relative in @($old, ($old+'.meta'))) {
            $target = [IO.Path]::GetFullPath((Join-Path $resolvedCopy $relative))
            if (!$target.StartsWith($resolvedCopy + [IO.Path]::DirectorySeparatorChar)) { throw '정리 대상 경로 오류' }
            if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
        }
    }
    Copy-Item -LiteralPath "$projectRoot/Assets" -Destination $copyRoot -Recurse -Force
} else {
    & "$PSScriptRoot/NewBuildCopy.ps1" -Phase remote | Out-Null
    $copyRoot = Get-Content "$resultRoot/remote-path.txt"
}
[IO.File]::WriteAllText("$resultRoot/remote-path.txt", $copyRoot)
Copy-Item -LiteralPath "$PSScriptRoot/BuildPlayer.cs" -Destination "$copyRoot/Assets/Editor/SkillPilotBuildPlayer.cs" -Force
Copy-Item -LiteralPath "$PSScriptRoot/ExportContent.cs" -Destination "$copyRoot/Assets/Editor/SkillPilotExport.cs" -Force
$env:SBSA_PILOT_BUILD_COPY = $copyRoot
$env:SBSA_PILOT_OUTPUT = "$resultRoot/SkillPilot-remote.apk"
$env:SBSA_PILOT_CONTENT_OUTPUT = "$resultRoot/server"
$env:SBSA_PILOT_ORIGIN = $Origin
try {
    foreach ($stage in @(@('SkillPilotExport.Export','android-content'), @('SkillPilotBuildPlayer.Build','remote-build'))) {
        $unityArgs = @('-batchmode','-nographics','-buildTarget','Android','-projectPath',('"'+$copyRoot+'"'),'-executeMethod',$stage[0],'-quit','-logFile',('"'+"$resultRoot/$($stage[1]).log"+'"'))
        $process = Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru -Wait
        if ($process.ExitCode -ne 0) { throw "검증 복사본 $($stage[1]) 실패: $($process.ExitCode)" }
    }
} finally {
    Remove-Item Env:SBSA_PILOT_BUILD_COPY,Env:SBSA_PILOT_OUTPUT,Env:SBSA_PILOT_CONTENT_OUTPUT,Env:SBSA_PILOT_ORIGIN -ErrorAction SilentlyContinue
}
