param()
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$resultRoot = Join-Path $projectRoot '.utmp/skill-pilot'
$copyRoot = [IO.Path]::GetFullPath((Get-Content "$resultRoot/remote-path.txt"))
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
if (!$copyRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or !(Split-Path $copyRoot -Leaf).StartsWith('sbsp-')) {
    throw '먼저 BuildRemote.ps1로 검증 복사본과 Android 콘텐츠를 생성하세요.'
}
Copy-Item "$PSScriptRoot/BuildPlayer.cs" "$copyRoot/Assets/Editor/SkillPilotBuildPlayer.cs" -Force
Copy-Item "$PSScriptRoot/BuildLocal.cs" "$copyRoot/Assets/Editor/SkillPilotBuildLocal.cs" -Force
$env:SBSA_PILOT_BUILD_COPY = $copyRoot
try {
    foreach ($stage in @(@('SkillPilotBuildLocal.Build','local-clean'), @('SkillPilotBuildLocal.BuildRemote','remote-clean'))) {
        $env:SBSA_PILOT_OUTPUT = "$resultRoot/SkillPilot-$($stage[1]).apk"
        $unityArgs = @('-batchmode','-nographics','-buildTarget','Android','-projectPath',('"'+$copyRoot+'"'),'-executeMethod',$stage[0],'-quit','-logFile',('"'+"$resultRoot/$($stage[1]).log"+'"'))
        $process = Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru -Wait
        if ($process.ExitCode -ne 0) { throw "$($stage[1]) 빌드 실패: $($process.ExitCode)" }
    }
} finally {
    Remove-Item Env:SBSA_PILOT_BUILD_COPY,Env:SBSA_PILOT_OUTPUT -ErrorAction SilentlyContinue
}
