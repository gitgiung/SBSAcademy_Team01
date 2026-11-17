# 원격 다운로드 기반 검증

이 도구는 원본 게임과 별개의 최소 Unity 프로젝트에서 **원본 ResourceManager·Managers·LoadScene·UI_LoadScene 코드**와 표준 Addressables 공급자를 실행한다. 유료 호스팅·Google Play Console·APK가 필요하지 않다. 원격 릴리스·카탈로그와 다운로드 동의/재시도 연결을 검사하지만 실제 게임 에셋 이전·영속 롤백·운영 CDN 도구는 아니다.

## 실행

Unity 6000.5.0f1, PowerShell, PATH의 Node.js가 필요하다. 에디터 위치가 다른 환경에서는 RunUnity.ps1의 unityExe를 맞춘다.

프로젝트 루트 PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\RemoteContentValidation\RunUnity.ps1 -ResultName result
```

검증 프로젝트는 `.utmp/remote-download/project`, 실제 번들과 hash는 `.utmp/remote-download/fixture`, 결과는 `.utmp/remote-download/result.xml`과 `.log`에 생긴다. 처음에는 실제 검증 번들과 **검증 프로젝트만의** 빈 Addressables 로컬 카탈로그를 빌드한다. 원본 Assets/AddressableAssetsData나 ProjectSettings를 저장하지 않는다. 테스트가 끝나면 도구가 시작한 HTTP 서버만 종료한다.

번들 재생성이 필요 없을 때:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\RemoteContentValidation\RunUnity.ps1 -ResultName repeat -SkipFixture
```

`SkipFixture`는 첫 실행이나 Unity/번들/새 카탈로그 fixture 변경 시 사용하지 않는다. 주소는 `http://127.0.0.1:18763`이며 서버는 loopback의 검증 번들·릴리스 JSON·카탈로그와 고정 오류 시나리오만 제공한다. 포트 사용 중이면 실행을 중단한다. 원격 기기용 주소·운영 서버로 사용하지 않는다. Unity 라이선스/UPM 접근이 차단된 실행 환경이면 해당 환경 문제를 먼저 해결해야 한다. 위 명령은 해당 실행에만 PowerShell 정책 예외를 적용하고 영구 실행 정책은 변경하지 않는다.

## 정상 결과

- PlayMode 결과 `Passed`, 실패·생략 0. 파일에 기록된 실제 total/passed를 확인한다.
- 처음에는 번들 크기만큼 다운로드가 필요하고 다운로드 후에는 추가 용량 0이다.
- 다운로드만으로 `ResourceManager.Load<TextAsset>`가 값을 반환하면 안 된다. `PreloadAsync` 후에 실제 번들 내용이 일치해야 한다.
- Clear/진행 콜백 예외/대기자 이탈/HTTP 404/실패 후 같은 라벨 재시도를 검사한다.
- Clear는 메모리 요청을 정리하지만 디스크 캐시를 지우지 않는다. 명시적으로 검증 번들 캐시를 삭제하면 다운로드 용량이 다시 생긴다.
- 플랫폼·앱 호환·메타데이터 버전·URL·카탈로그 SHA-256 검사와 실제 로컬 카탈로그 등록/정리를 확인한다.
- Managers의 동의 전 대기, 명시 재시도, 동일 준비 공유·호출자 이탈, Clear 후 동일 릴리스 재준비를 확인한다.
- LoadScene이 동의 전 기존 게임 준비로 우회하지 않고, UI 버튼이 승인/재시도 API에 연결되는지 확인한다. 버튼의 onClick 호출 검사이며 실제 손가락 입력·화면 배치 검증은 아니다.
- 수신 상한 이전 청크 보관 거부, Content-Length 및 chunked 초과 응답과 redirect 거부를 확인한다.

테스트는 고유 라벨과 자체 ResourceManager를 사용한다. 캐시 삭제는 `remote-download-fixture`의 해당 hash만 대상으로 하고 전체 Unity 캐시를 비우지 않는다. HTTP 실패 검사 구간에서는 Addressables의 예상 오류 로그만 허용하며, 실제 실패 콜백과 캐시 미생성을 단언한다. 다른 시나리오의 오류는 테스트를 실패시킨다.

원본 프로젝트 Test Runner에서 바로 실행하면 HTTP 통합 시나리오는 격리 환경 안내와 함께 생략되고, 순수 릴리스 파싱·수신 상한 테스트는 실행할 수 있다. 테스트를 위해 원본 Addressables 설정이나 사용자의 Play From Title 옵션을 바꾸지 않는다. 현재 전체 결과는 **40/40 통과, 실패·생략 0**이며 새 실행에서는 XML의 실제 수를 기준으로 한다.

## 오류 확인

1. XML이 없다면 같은 이름의 Unity 로그에서 첫 컴파일/라이선스/UPM 오류를 확인한다.
2. 번들 빌드가 실패하면 `fixture-build.log`를 확인한다. `SkipFixture`로 오류를 건너뛰지 않는다.
3. HTTP 서버 시작 실패는 `server-error.log`, 다운로드 실패는 Unity 로그의 URL·HTTP 상태와 방화벽/포트 점유를 확인한다.
4. 테스트 실패는 XML의 실패 이름·메시지를 확인한다. 예상되지 않은 로그를 일괄 무시하지 않는다.

이 검증은 Windows Editor에서 실제 HTTP 번들·릴리스·카탈로그와 Unity 디스크 캐시를 확인한다. Managers의 주변 서비스(Data/UI/Sound/Scene 전환)는 경계 코드로 대체하므로 원본 게임 전체 통합 검증은 아니다. Android APK/AAB 생성·Android 번들·실기기·운영 CDN·앱 재설치/프로세스 재실행·강제 업데이트·영속 릴리스 복구 운영은 미검증이다. 같은 세션 안의 후속 요청과 Clear 후 재준비로 캐시 재사용을 확인하며, 실제 앱 재실행은 후속 단계다.

## 원본 Editor에서 로딩 안내를 직접 확인하기

이 절차는 **Windows Editor용 검증 콘텐츠**를 기존 로딩 화면에 연결하는 선택적 확인이다. 실제 게임 리소스를 원격으로 이전하거나 게임 밸런스를 바꾸지 않는다.

1. 위 검증 명령을 `SkipFixture` 없이 한 번 실행해 최신 fixture를 만든다. 정상 결과를 확인한다.
2. 별도의 프로젝트 루트 PowerShell에서 서버를 실행하고 그 창을 유지한다.

```powershell
node .\tools\RemoteContentValidation\Serve.mjs .\.utmp\remote-download\fixture 18763
```

3. Unity Play 종료/컴파일 완료 후 `Tools > Remote Content > Settings`를 선택한다. 없을 때만 `Assets/Resources/RemoteContentSettings.asset`이 **꺼진 상태로** 생성된다. 기본 설정은 여전히 로컬 플레이이며 메뉴를 열기만 해서는 네트워크 연결하지 않는다.
4. Windows Editor에서 `Use Remote Content`와 `Allow Development Http`를 켜고, `Release Url`에 `http://127.0.0.1:18763/release.json`, `App Compatibility`에 `1`을 넣는다. 이 값은 개발용 검증 예시이며 운영 주소가 아니다. Android 기기의 127.0.0.1은 PC가 아니며 이 번들도 Windows용이므로 APK에 그대로 사용하지 않는다.
5. 원본 Editor가 Packed Play Mode이고 로컬 Addressables 빌드가 없다면 `Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script`로 현재 **Windows 타깃**의 로컬 기본 카탈로그를 생성해야 한다. 원본 설정의 Build Remote Catalog는 꺼진 채 유지한다. 설정 자동 변경이나 유료 서비스 가입은 필요 없다.
6. 씬 파일은 MainScene만 열고 `Tools > Play From Title`을 켠 뒤 Play하여 기존 타이틀 시작 영역을 누른다. 기존 LoadScene에서 버전/목록/용량 확인 후, 추가 다운로드가 있다면 용량과 `다운로드` 버튼을 표시해야 한다. 버튼을 누르기 전에는 콘텐츠 번들을 내려받지 않는다. 성공하면 기존 로비로 진행한다. 이미 같은 fixture가 캐시돼 있으면 동의 없이 진행할 수 있다.
7. 처음 받는 흐름을 다시 보고 싶다면 검증 도구를 다시 실행해 해당 fixture hash 캐시를 정리한 뒤 확인한다. 같은 내용의 fixture를 다시 굽는 것만으로는 캐시가 지워지지 않는다. 전체 Unity 캐시 삭제는 필요 없다. PC 검증 프로젝트와 원본 Editor는 번들 캐시를 공유할 수 있어 이미 받은 콘텐츠가 0바이트로 나오는 것은 정상이다.
8. 실패 재시도 확인은 Play 시작 전에 서버를 끄고 진입한 뒤, 오류 안내의 `다시 시도` 버튼이 나타나는지 본다. 서버를 다시 켜고 버튼을 누르면 같은 실행에서 준비가 재개돼야 한다. 릴리스를 이미 선택한 뒤 다른 버전으로 바꾸려면 Play를 종료하고 다시 실행한다.
9. 확인 후 Play 종료 → `Use Remote Content`를 끄면 기존 로컬 흐름으로 돌아간다. MainScene 직접 Play 확인 시에는 기존처럼 `Tools > Play From Title`을 해제한다. 서버 창은 Ctrl+C로 종료한다.

표시가 없다면 설정 Use Remote Content와 Resources의 파일 이름, 실제 진입이 타이틀→로딩인지부터 확인한다. 네트워크 오류는 서버/18763 포트/HTTP 개발 허용을, 초기화 오류는 로컬 Addressables 기본 빌드와 현재 타깃을 확인한다. 카탈로그 SHA 오류는 manifest와 catalog.bin이 같은 fixture 빌드인지 확인한다. Console 첫 오류를 보존하고 씬을 다시 저장하거나 GiungScene을 수정하지 않는다.
