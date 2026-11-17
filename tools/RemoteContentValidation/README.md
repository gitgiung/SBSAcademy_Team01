# 원격 다운로드 기반 검증

이 도구는 원본 게임과 별개의 최소 Unity 프로젝트에서 **원본 ResourceManager 코드**와 표준 Addressables 공급자를 실행한다. 유료 호스팅·Google Play Console·APK가 필요하지 않다. 새로운 카탈로그 운영/앱 버전 선택/게임 에셋 이전을 구현한 도구는 아니다.

## 실행

Unity 6000.5.0f1, PowerShell, PATH의 Node.js가 필요하다. 에디터 위치가 다른 환경에서는 RunUnity.ps1의 unityExe를 맞춘다.

프로젝트 루트 PowerShell:

```powershell
& tools/RemoteContentValidation/RunUnity.ps1 -ResultName result
```

검증 프로젝트는 `.utmp/remote-download/project`, 실제 번들과 hash는 `.utmp/remote-download/fixture`, 결과는 `.utmp/remote-download/result.xml`과 `.log`에 생긴다. 처음에는 실제 검증 번들과 **검증 프로젝트만의** 빈 Addressables 로컬 카탈로그를 빌드한다. 원본 Assets/AddressableAssetsData나 ProjectSettings를 저장하지 않는다. 테스트가 끝나면 도구가 시작한 HTTP 서버만 종료한다.

번들 재생성이 필요 없을 때:

```powershell
& tools/RemoteContentValidation/RunUnity.ps1 -ResultName repeat -SkipFixture
```

`SkipFixture`는 첫 실행이나 Unity/번들 변경 시 사용하지 않는다. 주소는 `http://127.0.0.1:18763`이며 서버는 loopback의 검증 파일 하나만 제공한다. 포트 사용 중이면 실행을 중단한다. 원격 기기용 주소·운영 서버로 사용하지 않는다. Unity 라이선스/UPM 접근이 차단된 실행 환경이면 해당 환경 문제를 먼저 해결해야 한다.

## 정상 결과

- PlayMode 결과 `Passed`, 실패·생략 0. 파일에 기록된 실제 total/passed를 확인한다.
- 처음에는 번들 크기만큼 다운로드가 필요하고 다운로드 후에는 추가 용량 0이다.
- 다운로드만으로 `ResourceManager.Load<TextAsset>`가 값을 반환하면 안 된다. `PreloadAsync` 후에 실제 번들 내용이 일치해야 한다.
- Clear/진행 콜백 예외/대기자 이탈/HTTP 404/실패 후 같은 라벨 재시도를 검사한다.
- Clear는 메모리 요청을 정리하지만 디스크 캐시를 지우지 않는다. 명시적으로 검증 번들 캐시를 삭제하면 다운로드 용량이 다시 생긴다.

테스트는 고유 라벨과 자체 ResourceManager를 사용한다. 캐시 삭제는 `remote-download-fixture`의 해당 hash만 대상으로 하고 전체 Unity 캐시를 비우지 않는다. HTTP 실패 검사 구간에서는 Addressables의 예상 오류 로그만 허용하며, 실제 실패 콜백과 캐시 미생성을 단언한다. 다른 시나리오의 오류는 테스트를 실패시킨다.

원본 프로젝트 Test Runner에서 바로 실행하면 이 HTTP 통합 클래스는 격리 환경 안내와 함께 생략된다. 테스트를 위해 원본 Addressables 설정이나 사용자의 Play From Title 옵션을 바꾸지 않는다.

## 오류 확인

1. XML이 없다면 같은 이름의 Unity 로그에서 첫 컴파일/라이선스/UPM 오류를 확인한다.
2. 번들 빌드가 실패하면 `fixture-build.log`를 확인한다. `SkipFixture`로 오류를 건너뛰지 않는다.
3. HTTP 서버 시작 실패는 `server-error.log`, 다운로드 실패는 Unity 로그의 URL·HTTP 상태와 방화벽/포트 점유를 확인한다.
4. 테스트 실패는 XML의 실패 이름·메시지를 확인한다. 예상되지 않은 로그를 일괄 무시하지 않는다.

이 검증은 Windows Editor에서 실제 HTTP 번들과 Unity 디스크 캐시를 확인한다. Android APK/AAB 생성·Android 번들·실기기·운영 CDN·앱 재설치/프로세스 재실행·강제 업데이트·릴리스 복구 운영을 검증했다고 해석하면 안 된다. 같은 세션 안의 후속 요청으로 캐시 재사용을 확인하며, 실제 앱 재실행은 후속 단계다.
