# 실제 스킬 원격 콘텐츠와 APK 확인

대상은 전투 표와 수살(10025)의 Texture/Frames/아이콘 네 파일이다. `Assets/RemoteContent/SkillPilot`로 meta와 함께 옮겼다. 원격 모드는 동의·다운로드 후 네 에셋의 타입과 표/프레임을 준비해야 로비로 진행한다. 누락을 로컬 파일로 대체하지 않는다. Editor 로컬 플레이는 이전한 파일을 읽고, 로컬 APK는 빌드 중 생성하는 참조 asset으로 포함한다. 게임 수치·MP는 변경하지 않았다.

## PC에서 직접 확인

1. 프로젝트 루트 PowerShell에서 다음 검증을 실행한다. 처음/fixture 변경 후에는 `-SkipFixture`를 붙이지 않는다. 최신 전체 결과는 `docs/AI/SkillPilotBuildValidation.md`를 본다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\RemoteContentValidation\RunUnity.ps1 -ResultName skill-check
```

2. 명령 종료 후 별도 PowerShell에서 서버를 실행하고 창을 유지한다.

```powershell
node .\tools\RemoteContentValidation\Serve.mjs .\.utmp\remote-download\fixture 18763
```

3. Unity Play 종료/컴파일 완료 → `Tools > Remote Content > Settings` → `Use Remote Content`, `Allow Development Http`를 켠다. `Release Url`은 `http://127.0.0.1:18763/pilot/release.json`, `App Compatibility`는 `skill-pilot-v1`로 설정한다. 기존 합성 콘텐츠 `/release.json`과 구분한다. 현재 타깃은 Windows다.
4. 로컬 기본 Addressables 카탈로그가 없으면 `Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script`로 현재 Windows용 기본 카탈로그를 빌드한다. 원본의 `Build Remote Catalog`는 꺼진 채 유지한다.
5. **MainScene만** 열고 `Tools > Play From Title`을 켠 뒤 Play → 타이틀 시작 영역을 누른다. 추가 용량이 있으면 `다운로드` 버튼이 나타난다. 승인 후 스킬 데이터/그림 준비를 거쳐 로비로 진행해야 한다. 같은 콘텐츠 캐시가 있으면 용량 0으로 동의 없이 진행한다. 수살 12프레임·아이콘·표 연결의 자동 검증과 실제 화면 육안 검증은 구분한다.
6. 첫 다운로드를 다시 확인하려면 Play 종료 → 서버 창 Ctrl+C → 1번 검증 명령 실행(같은 fixture면 `-SkipFixture` 가능) → 명령 종료 후 2번 서버 재실행 → Play 순서다. 검증 종료 시 해당 pilot 번들 hash 캐시를 정리하며 전체 Unity 캐시는 지우지 않는다. 파일을 다시 굽는 것만으로 캐시가 지워지지는 않는다.
7. 실패 확인은 Play 전에 서버를 끄고 진입한다. `다시 시도`가 나타나면 서버를 다시 켜고 버튼을 누른다. 성공 후 로비로 진행해야 한다. 설정/릴리스를 바꿀 때는 Play를 종료하고 다시 시작한다.
8. 확인 후 Play 종료 → `Use Remote Content` 끄기 → MainScene 직접 확인 시 `Tools > Play From Title` 해제 → 서버 Ctrl+C. 원본 설정은 자동으로 바꾸지 않았다.

오류는 Console 첫 오류, Windows 타깃, URL의 `/pilot/`, 호환 값, 개발 HTTP 허용, 18763 포트, 기본 카탈로그 순서로 확인한다. 검증 오류는 `.utmp/remote-download/<ResultName>.xml/.log`와 `fixture-build.log`, `server-error.log`를 본다. 씬 저장·전체 캐시 삭제는 필요 없다.

## Android APK 재생성

Unity 6000.5.0f1의 Android SDK/NDK/OpenJDK, Node.js, PowerShell이 필요하다. 원본과 별개 짧은 임시 복사본에서 ARM64 IL2CPP Development APK를 빌드한다. Google Play Console·기기·유료 서버는 필요 없다. 원본 씬·패키지·Project Settings와 원격 설정은 저장하지 않는다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\SkillPilotValidation\BuildRemote.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\SkillPilotValidation\BuildComparison.ps1
```

첫 명령은 `.utmp/skill-pilot/server/Android`에 카탈로그·번들·release.json과 보관용 content state/Build Layout을 내보낸다. 두 번째는 같은 코드의 `SkillPilot-local-clean.apk`와 `SkillPilot-remote-clean.apk`, 각각 `.report.json`을 만든다. 보고서 `result=Succeeded`, `apkBytes`와 실제 파일 길이를 확인한다. `bytes`는 심볼 등 전체 빌드 출력 합계이므로 APK 크기가 아니다. Gradle APK만 새로 패키징해 증분 ZIP 잔여 데이터로 크기가 부풀지 않게 한다.

이전 기준선 복사본이 남아 있을 때만 `BuildRemote.ps1 -ReuseBaseline`을 쓸 수 있다. 일반 재생성은 새 복사본을 사용한다. 임시 복사본 위치는 `.utmp/skill-pilot/remote-path.txt`, 빌드 로그는 `android-content.log`, `local-clean.log`, `remote-clean.log`다. 원본 프로젝트를 두 번째 Unity로 열지 않는다.

원격 APK의 주소는 개발 전용 `http://127.0.0.1:18763/content/Android/release.json`이다. 다운로드하려면 향후 기기에서 PC loopback으로 연결하는 별도 구성이 필요하며 현재 기기 없는 빌드만 수행했다. 운영 HTTPS CDN 주소로 설정된 배포 APK가 아니다. Windows fixture 번들을 APK에 사용하면 안 된다.

개발용 Android 파일 제공 서버는 다음 명령이다. 위 PC fixture 서버와 포트가 같으므로 동시에 실행하지 않는다. 보관용 content state/Build Layout은 HTTP로 제공하지 않는다.

```powershell
node .\tools\SkillPilotValidation\Serve.mjs .\.utmp\skill-pilot\server 18763
```

AAB, 설치/실행, 터치·화면 배치, 앱 재시작·오프라인·단독 패치/롤백 운영은 후속 검증이다. 네 파일 시범의 결과를 전체 설치 용량 절감으로 해석하지 않는다.
