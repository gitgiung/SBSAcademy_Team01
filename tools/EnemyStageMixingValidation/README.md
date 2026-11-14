# 몬스터 단계 혼합 검증

- `./tools/EnemyStageMixingValidation/Run.ps1`: 실제 계산/Director 소스를 .NET에서 실행한다. Unity 경계와 스포너·풀은 대역이므로 엔진 수명/물리/임포트를 대신하지 않는다.
- `./tools/EnemyStageMixingValidation/ValidateAssets.ps1`: 24개 원본과 직렬화된 GUID/fileID 슬롯, 승인 초기값, MainScene 상속, 기존 수동 배열 보존, 새 GUID 중복을 검사한다.
- `./tools/EnemyStageMixingValidation/RunCompile.ps1`: 로컬 Unity 6000.5.0f1 Roslyn과 기존 응답 캐시로 실제 런타임/Editor 어셈블리 6개를 별도 출력에 컴파일한다. 캐시가 없으면 먼저 Unity 임포트가 필요하다.
- `./tools/EnemyStageMixingValidation/RunUnity.ps1`: 별도 `.utmp/stagemix`에 실제 Combat/Enemy 소스와 24개 프리팹을 복사하고 Unity EditMode 테스트를 실행한다. 원본 프로젝트는 열지 않는다. Windows Unity Licensing Client와 연결되는 실행 환경이 필요하다.

출력은 모두 `.utmp`에 있으며 Git에 포함하지 않는다. 실제 운영 씬/프레임/풀의 통합 Play나 Android 빌드를 검증하는 도구가 아니다. 사용법과 수동 확인은 `docs/AI/EnemySpawning.md`를 참조한다.
