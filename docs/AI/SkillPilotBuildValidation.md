# 실제 스킬 콘텐츠·Android APK 검증 (2026-11-17)

요청 범위는 실제 스킬 에셋 원격 이전과 **기기 없는 APK 빌드**다. 브랜치 `addressable-resources-loading`, 시작/종료 조회 HEAD `9d80a66`. 커밋·체크아웃·운영 배포는 하지 않았다.

## 결과

전투 표, 수살(10025)의 WaterArrow Texture/Frames JSON/아이콘을 `Assets/RemoteContent/SkillPilot`로 이전했다. 네 원본·meta의 SHA-256이 시작 기록과 일치한다. 사용자 변경 MainScene·NanumGothic Combat SDF·Addressable 정렬 설정·원격 설정 및 meta 6개도 일치한다. 원본 패키지/Project Settings는 diff가 없고, GiungScene은 작업·검증 대상에서 제외했다.

`skill-pilot-v1`은 앱 호환과 프로필을 함께 검사한다. 동의 후 필수 번들 준비 → 명시 타입 로딩 → 표/프레임 검증·게시 → Ready 순서다. 잘못된 프로필, 동의 없이 다운로드를 유발하는 불완전 필수 라벨, 중복 표, 지원하지 않는 수살 형태, 잘못된 프레임을 차단한다. Clear는 생성 Sprite/정적 표를 정리한다. 실제 Sprite 번들에서 Object 로딩이 Texture2D를 반환하던 문제도 해당 타입 요청으로 수정했다.

## 실행한 검증

- 격리 Windows Editor HTTP PlayMode 최종 `skill-verified.xml`: **48/48 Passed, 실패·생략 0**. 기존 40개와 실제 표·12프레임·원격 아이콘 동일성·Clear/재준비·엄격한 원격 누락·동의/프로필/잘못된 데이터 회귀를 포함한다. 주변 Data/UI/Sound/Scene 서비스는 경계 코드로 대체하므로 전체 게임 실플레이 결과는 아니다.
- 원본 소스 C# 7개 어셈블리 컴파일 모두 exit 0: `.utmp/skill-pilot/compile-latest.log`.
- 전투 표 데이터 검사 **30/31**. 기존 실패 `shipped JSON keeps combat values and blank passive columns: expected , actual SkillIcons/SkillIcon_10001`이 남는다. 그림 키가 채워진 기존 데이터와 빈 문자열을 기대하는 검사 간 차이다. 이번에 표 수치나 그림 키를 수정하지 않았다.
- 새 기능 회귀는 구현 전 실패를 확인했다. 검증 중 테스트 공용 캐시가 늦은 다운로드 완료와 겹치는 문제를 고유 캐시 이름으로 분리했고, Android 네이티브 컴파일 동시 실행 중 짧은 HTTP 제한에 걸린 뒤 최종 검증은 빌드 종료 후 단독 실행했다.
- 독립 리뷰 지적 4개(필수 프로필, 동의 밖 메모리 로딩 전송, 미지원 스킬 형태, content state 경로)를 수정하고 회귀/실제 콘텐츠 빌드로 확인했다.

## 실제 Android 출력

Unity 6000.5.0f1 / Addressables 2.9.1, ARM64 IL2CPP, Development APK, debug 서명. 짧은 임시 전체 복사본에서 빌드했다. `aapt2 dump badging`: 임시 ID `com.sbsacademy.team01.skillpilot`, minSdk 26, targetSdk 36, `arm64-v8a`, debuggable.

| APK | 실제 파일 바이트 | BuildReport |
| --- | ---: | --- |
| 이전 전 baseline.apk | 76,430,118 | Succeeded |
| 같은 코드 로컬 포함형 SkillPilot-local-clean.apk | 76,437,686 | Succeeded |
| 같은 코드 원격형 SkillPilot-remote-clean.apk | 76,433,550 | Succeeded |

같은 코드 기준 차이는 **4,136바이트(약 4.04KiB)**다. 전체 게임 용량 절감이나 설치 후 크기 측정이 아니다. 기능 코드 추가도 있으므로 이전 baseline보다 무조건 작아졌다고 주장하지 않는다. 중간 증분 APK에 대체된 ZIP 데이터가 남아 파일이 커지는 것을 확인해 최종 비교는 Gradle APK 출력만 정리 후 다시 패키징했다. 네이티브 컴파일 캐시는 유지했다. 최종 로컬/원격 APK의 압축 엔트리 합계는 각각 76,361,977 / 76,357,839바이트이며 큰 잔여 데이터 차이가 없다.

BuildReport `packedSourceAssets`: baseline에는 기존 Resources 네 경로, 로컬형에는 이전한 네 경로와 `Assets/Resources/__GeneratedSkillPilotLocal.asset`이 있다. 원격형에는 이 다섯 경로가 모두 없다. 원격 APK에 파일 이름이 포함된 C# 스크립트 경로를 에셋 중복으로 세지 않았다.

Android 표준 Addressables Build Layout에는 실제 네 명시 주소/에셋과 Lz4HC 번들 네 개(9,004 / 6,894 / 1,495 / 5,158바이트, 합계 **22,551바이트**)가 있고 DuplicatedAssets는 비어 있다. 카탈로그 SHA-256은 release.json과 일치한다. `addressables_content_state.bin`, `content-buildlayout.json`을 같은 Android 출력에 보관했다. 서버 폴더에는 이전 시도 번들도 남아 있으므로 현재 네 번들의 기준은 현재 catalog/Build Layout이다. Windows는 격리 테스트용 실제 에셋 fixture이며 운영용 Windows 업데이트 릴리스를 만든 것은 아니다.

출력 루트는 `.utmp/skill-pilot`이며 Git 제외다. APK는 `SkillPilot-remote-clean.apk`, 보고서는 같은 이름의 `.report.json`, Android 콘텐츠는 `server/Android`다. 개발 서버 공개 경로의 release.json 응답 200, 비공개 content state/Build Layout 응답 404도 확인했다. 재생성 명령과 직접 확인은 `tools/SkillPilotValidation/README.md`에 있다.

## 직접 확인과 남은 범위

PC는 검증 도구 실행 → 서버 실행 → `Tools > Remote Content > Settings`에서 Windows `/pilot/release.json`과 `skill-pilot-v1` 설정 → MainScene에서 `Tools > Play From Title` 활성화 → Play/시작 → 다운로드 승인 → 로비 순서다. 캐시가 있으면 0바이트가 정상이다. 첫 다운로드 재확인과 오류 복구는 README의 순서를 따른다. 작업 후 원격 설정을 끄고 직접 MainScene 확인 시 Play From Title도 해제한다. 오류는 Console 첫 오류·URL/호환 계열·HTTP 허용·타깃/기본 카탈로그·서버 포트 순으로 확인한다.

원격 APK는 PC loopback 개발 주소를 사용한다. 현재 기기 연결·설치·앱 실행·기기 다운로드·앱 프로세스 재실행·터치/화면 비율을 검증하지 않았다. AAB/Google Play Console·운영 HTTPS CDN·별도 패치 배포/오프라인/롤백 정책도 후속이다. 원본 Editor 실제 MainScene 플레이/로딩 화면 육안 확인도 이번 자동 검사에 포함되지 않는다.
