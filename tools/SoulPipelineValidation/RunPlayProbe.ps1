param(
  [string]$CopyRoot = 'C:\sv\soul',
  [switch]$Capture,
  [string]$LogFile = ''
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$unityExe = 'C:/Program Files/Unity/Hub/Editor/6000.5.0f1/Editor/Unity.exe'
$unitySrc = Join-Path $PSScriptRoot 'Unity'
$probeFiles = 'SoulPipelinePlayProbe.cs', 'SoulPipelineCaptureProbe.cs'
foreach ($f in $probeFiles + 'SoulVerifyLaunch.cs') {
  if (!(Test-Path (Join-Path $unitySrc $f))) { throw "검증 소스 없음: tools/SoulPipelineValidation/Unity/$f" }
}

# -Capture: 그래픽 장치를 켜고 적용 전/후 화면을 C:\sv\capture\*.png로 저장한다.
if ($Capture) {
  $ResultPath = 'C:\sv\soul-capture.txt'
  if (!$LogFile) { $LogFile = 'C:\sv\soul-capture.log' }
  $mode = '-soulCapture'
} else {
  $ResultPath = 'C:\sv\soul-probe.txt'
  if (!$LogFile) { $LogFile = 'C:\sv\soul-play-probe.log' }
  $mode = '-soulProbe'
}

$gameDir = "$CopyRoot\Assets\0.Scripts\Game\SoulPipeline"
New-Item -ItemType Directory -Force -Path $gameDir, "$CopyRoot\Assets\Editor" | Out-Null
robocopy "$projectRoot\Assets\0.Scripts\Game\SoulPipeline" $gameDir /E /NFL /NDL /NJH /NJS /nc /ns /np /XF $probeFiles | Out-Null
robocopy "$projectRoot\Assets\Resources\SoulPipeline" "$CopyRoot\Assets\Resources\SoulPipeline" /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
foreach ($f in $probeFiles) { Copy-Item (Join-Path $unitySrc $f) (Join-Path $gameDir $f) -Force }
Copy-Item (Join-Path $unitySrc 'SoulVerifyLaunch.cs') "$CopyRoot\Assets\Editor\SoulVerifyLaunch.cs" -Force

if (Test-Path $ResultPath) { Remove-Item $ResultPath -Force }
$args = @('-batchmode')
if (!$Capture) { $args += '-nographics' }
$args += @('-projectPath', $CopyRoot, $mode, '-logFile', $LogFile)
$p = Start-Process -FilePath $unityExe -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
Write-Output "UNITY_EXIT:$($p.ExitCode)"
if (!(Test-Path $ResultPath)) { throw "결과 없음: $ResultPath / 로그 $LogFile" }
Get-Content -LiteralPath $ResultPath -Encoding UTF8
if ($p.ExitCode -ne 0) { throw "실패. 로그: $LogFile" }
