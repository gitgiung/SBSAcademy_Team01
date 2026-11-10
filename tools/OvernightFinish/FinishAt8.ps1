# 08:00 마무리: 새 브랜치 확인 → 커밋 → 푸시 시도 → 노션은 에이전트가 담당 → PC 종료
# 에이전트 루프가 이 스크립트와 함께 커밋/노션을 수행한다. 단독 실행 시 커밋만 하고 종료한다.

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $root

$branch = "feature/soul-pipeline-3d"
$log = Join-Path $root "tools\OvernightFinish\finish-log.txt"
function Log($m) {
  $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $m
  Add-Content -Path $log -Value $line
  Write-Output $line
}

Log "FinishAt8 start cwd=$root"

$current = (git branch --show-current).Trim()
if ($current -ne $branch) {
  git checkout -B $branch
  Log "checked out $branch"
} else {
  Log "already on $branch"
}

git add -A
$status = git status --porcelain
if ([string]::IsNullOrWhiteSpace($status)) {
  Log "nothing to commit"
} else {
  git commit -m @"
본선 Soul Pipeline 3D 착수: 평탄맵·휴머노이드·접근슬래시·팀안내

SoulArena3D 병행 본선과 URP UniversalRenderer, KinemIQ 임시 휴머노이드,
Main 레거시 유지 방침을 반영한다. pixelfx 비핵 변경도 작업 트리에 포함해 유실을 막는다.
"@
  Log "commit done $($(git rev-parse --short HEAD))"
}

git push -u origin HEAD
if ($LASTEXITCODE -eq 0) { Log "push ok" } else { Log "push failed exit=$LASTEXITCODE (로컬 커밋은 유지)" }

Log "READY_FOR_NOTION_AND_SHUTDOWN"
# 종료는 에이전트가 노션 업로드 후 수행. 단독 실행이면 60초 후 종료.
if ($env:SOUL_FINISH_SHUTDOWN -eq "1") {
  Log "scheduling shutdown in 90s"
  shutdown /s /t 90 /c "Soul Pipeline overnight finish complete"
}
