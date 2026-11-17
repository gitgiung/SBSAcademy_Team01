param([string]$ResultName = 'result', [switch]$SkipFixture)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$validationRoot = Join-Path $projectRoot '.utmp/remote-download/project'
$resultRoot = Join-Path $projectRoot '.utmp/remote-download'
$unityExe = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe'
New-Item -ItemType Directory -Force -Path "$validationRoot/Assets/Runtime", "$validationRoot/Assets/Tests", "$validationRoot/Assets/Editor", "$validationRoot/Packages", "$validationRoot/ProjectSettings" | Out-Null
$runtimeFiles = @('ResourceManager.cs', 'PoolManager.cs', 'PooledObject.cs', 'IPoolable.cs')
if (Test-Path "$projectRoot/Assets/0.Scripts/Framework/Managers/Core/ResourceManager.Downloads.cs") { $runtimeFiles += 'ResourceManager.Downloads.cs' }
foreach ($file in $runtimeFiles) {
    Copy-Item -LiteralPath "$projectRoot/Assets/0.Scripts/Framework/Managers/Core/$file" -Destination "$validationRoot/Assets/Runtime/$file" -Force
}
Copy-Item -LiteralPath "$PSScriptRoot/Boundary.cs" -Destination "$validationRoot/Assets/Runtime/Boundary.cs" -Force
Copy-Item -LiteralPath "$PSScriptRoot/BuildFixture.cs" -Destination "$validationRoot/Assets/Editor/BuildFixture.cs" -Force
Get-ChildItem "$projectRoot/Assets/Tests/PlayMode/RemoteContent" -File | Where-Object Extension -ne '.meta' | Copy-Item -Destination "$validationRoot/Assets/Tests" -Force
Copy-Item -LiteralPath "$projectRoot/ProjectSettings/ProjectVersion.txt" -Destination "$validationRoot/ProjectSettings/ProjectVersion.txt" -Force
'{"dependencies":{"com.unity.addressables":"2.9.1","com.unity.test-framework":"1.7.0","com.unity.modules.assetbundle":"1.0.0","com.unity.modules.jsonserialize":"1.0.0","com.unity.modules.unitywebrequest":"1.0.0","com.unity.modules.unitywebrequestassetbundle":"1.0.0"}}' | Set-Content "$validationRoot/Packages/manifest.json"

function Run-IsolatedUnity([string[]]$Extra, [string]$LogName) {
    $arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $validationRoot + '"'), '-logFile', ('"' + "$resultRoot/$LogName.log" + '"')) + $Extra
    $process = Start-Process -FilePath $unityExe -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "격리 Unity 종료 $($process.ExitCode): $LogName.log 확인" }
}
if (!$SkipFixture) { Run-IsolatedUnity @('-executeMethod', 'RemoteDownloadFixture.Build', '-quit') 'fixture-build' }
$fixtureRoot = Join-Path $resultRoot 'fixture'
$port = 18763
$previousFixture = $env:SBSA_REMOTE_FIXTURE
$previousOrigin = $env:SBSA_REMOTE_ORIGIN
$env:SBSA_REMOTE_FIXTURE = $fixtureRoot
$env:SBSA_REMOTE_ORIGIN = "http://127.0.0.1:$port"
$server = $null
try {
    $server = Start-Process -FilePath (Get-Command node).Source -ArgumentList @(('"' + "$PSScriptRoot/Serve.mjs" + '"'), ('"' + $fixtureRoot + '"'), "$port") -WindowStyle Hidden -PassThru -RedirectStandardOutput "$resultRoot/server.log" -RedirectStandardError "$resultRoot/server-error.log"
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($server.HasExited) { throw 'HTTP 검증 서버 시작 실패: server-error.log 확인' }
        try { Invoke-WebRequest -Uri "$env:SBSA_REMOTE_ORIGIN/missing" -Method Head -TimeoutSec 1 | Out-Null }
        catch { if ($_.Exception.Response.StatusCode -eq 404) { $ready = $true; break } }
        Start-Sleep -Milliseconds 100
    }
    if (!$ready) { throw 'HTTP 검증 서버 응답 없음' }
    Run-IsolatedUnity @('-runTests', '-testPlatform', 'PlayMode', '-testResults', ('"' + "$resultRoot/$ResultName.xml" + '"')) $ResultName
    if (!(Test-Path "$resultRoot/$ResultName.xml")) { throw 'Unity 테스트 결과 XML 없음' }
    [xml]$results = Get-Content "$resultRoot/$ResultName.xml"
    $results.'test-run' | Select-Object result, total, passed, failed, skipped
    if ($results.'test-run'.result -ne 'Passed') { throw '리소스 다운로드 테스트 실패' }
}
finally {
    if ($null -ne $server -and !$server.HasExited) { Stop-Process -Id $server.Id }
    $env:SBSA_REMOTE_FIXTURE = $previousFixture
    $env:SBSA_REMOTE_ORIGIN = $previousOrigin
}
