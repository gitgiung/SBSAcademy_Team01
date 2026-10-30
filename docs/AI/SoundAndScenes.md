# 사운드와 씬 정리 계약

## 사운드

- `Play(type, key, pitch)`는 ResourceManager에 준비된 AudioClip을 사용한다. 먼저 Addressables 로딩을 완료해야 한다. 사운드 매니저는 별도 에셋 캐시나 핸들을 소유하지 않는다.
- 누락 주소·null 클립은 Warning을 남기고 현재 재생 상태를 유지한다. 실패를 캐시하지 않으므로 로딩 후 같은 주소를 재요청할 수 있다.
- Bgm은 단일 AudioSource에서 반복하고 Effect는 PlayOneShot을 사용한다. 두 경로 모두 pitch를 적용한다. 같은 BGM을 다시 Play하면 기존처럼 처음부터 재생한다.
- `Stop(type)`은 정지하고 할당된 클립은 유지한다. `Play(type)`은 현재 할당된 클립을 재생한다. `Clear()`는 모든 소스 정지, clip 해제, pitch=1을 적용한다. 초기화 전 Clear는 객체를 만들지 않는다.
- 잘못된 ESound 값은 Warning 후 반환한다. 소스나 루트가 외부에서 파괴됐으면 다음 사용 때 복구한다. Managers에 연결된 루트는 복구 후에도 Managers 자식이며 함께 파괴된다.

## 씬

- `Managers.Scene.LoadScene(type)`과 새 `LoadSceneAsync(type)`는 대상 씬이 Build Profiles에 등록됐는지 먼저 검증한다. 미등록 대상은 InvalidOperationException으로 거절하며 현재 상태를 정리하지 않는다.
- 유효한 대상은 `Managers.Clear()` 후 Unity의 Single 전환을 수행한다. 비동기 API는 AsyncOperation을 반환하므로 코루틴에서 yield할 수 있다. LoadScene의 준비 완료 후 MainTestScene 진입도 이 경로를 사용한다.
- 정리 순서는 **활성 씬의 BaseScene.Clear → Pool.Clear → UI.Clear → Sound.Clear**다. 풀 콜백에서 기존 UI를 참조할 수 있으므로 순서를 유지한다. 씬 훅에서 Managers.Clear를 다시 불러도 중첩 정리는 생략한다.
- CurrentScene은 활성 씬의 활성 BaseScene을 선택한다. 추가 로드된 다른 씬이나 DontDestroyOnLoad의 객체를 임의 선택하지 않는다. 한 활성 씬에는 대표 BaseScene 하나를 두는 기존 구조를 전제로 한다.
- 씬 훅에서 예외가 나도 공통 정리를 수행한 뒤 예외를 전달하며 전환은 진행하지 않는다. 리소스 캐시·데이터·게임 준비 상태는 전환에 걸쳐 유지한다. 최종 Managers 파괴 시 기존 Resource 해제 계약이 적용된다.
- BaseScene.Clear는 해당 씬의 구독·진행 상태 등 씬 소유 정리만 담당한다. Managers.Clear를 다시 호출할 필요는 없다. TitleScene과 LoadScene은 현재 정리할 추가 상태가 없어 빈 구현을 유지한다.
- Unity SceneManager를 직접 호출하는 외부 경로에는 이 공통 정리가 자동 적용되지 않는다. 전환 호출은 Managers.Scene을 사용한다. 동시 전환 큐·취소·다중 씬별 정리·BGM 연속 재생 정책은 추가하지 않았다.

## 직접 확인 순서

1. Unity 컴파일을 기다리고 Console의 새 빨간 오류가 없는지 확인한다.
2. `Tools → Play From Title`을 테스트 중 잠시 끈다.
3. `Window → General → Test Runner → PlayMode`에서 `SoundSceneLifecycleTests`를 선택하고 `Run Selected`를 누른다. 누락 클립 재시도, pitch, Clear, 잘못된 enum, 소스·루트 복구, 씬 훅과 동기·비동기 전환, 정리 예외 테스트 **13개**가 모두 녹색이어야 한다.
4. `Run All`로 기존 초기화·리소스·UI·풀링 회귀도 확인한다. 전투 팝업 후속 작업 포함 현재 총 **113개 통과**가 정상이다. 테스트 후 Play From Title 설정을 원래대로 돌린다.
5. `Assets/9.Scenes/TitleTestScene.unity`에서 Play를 누르고 타이틀의 시작 터치 영역(`StartTouchArea`)을 누른다. 로딩을 거쳐 MainTestScene에 진입하고 Hierarchy의 `DontDestroyOnLoad → @Managers → @SoundRoot → Bgm/Effect`가 한 세트인지 확인한다.
6. 실제 클립 연결 후에는 Inspector에서 AudioSource의 clip·pitch·loop와 재생을 확인한다. 씬 전환 후 소스는 멈추고 clip이 비워져야 한다. 현재 실제 주소·클립을 임의 등록하지 않았으며 자동 테스트의 합성 AudioClip 재생은 청감 검증을 대신하지 않는다.

문제가 생기면 실패한 테스트 이름과 상세 결과, Console 첫 빨간 오류와 스택을 확인한다. `Sound clip not found`는 주소·타입·사전 로딩 완료 여부를, 씬 로드 실패는 `File → Build Profiles → Scene List`의 등록을 확인한다. Android 빌드·기기·실제 음질과 플랫폼별 오디오 지연은 별도 검증이 필요하다.

## 2026-10-30 검증

Unity 6000.5.0f1 컴파일 및 전체 PlayMode **85/85 통과**, 실패·생략 0, 종료 코드 0. 신규 13개를 포함한다. 수정 전 10개 중 9개 실패와 루트 복구 추가 테스트 0/1을 재현했다. 최종 실행에서 동기·비동기 전환, 리소스·준비 상태 유지, 씬 훅 예외 후 사운드 정리와 가드 해제도 확인했다. 사용자 프로젝트와 분리한 임시 복사본을 사용했고 원본 씬·설정은 수정하지 않았다.
