# 비스트 보스 전체 선명도

현재 진입점: `node tools/BeastClarity/bake.mjs`. Run/Charge/Swipe/SwipeBoth의 기존 `bake.mjs`도 이 공통 파이프라인으로 연결한다. 이전 구현은 각 폴더의 `bake-before-clarity.mjs`에 기록용으로 남긴다. 이전 저해상도 bake를 직접 실행하지 않는다. Move는 이미 적용한 `BeastMoveArt/bake.mjs`를 별도로 유지한다.

## 적용

- Run64: 승인한 identity-v3 원본에서 264px 재추출. West는 기존 run-source 원본 행을 재추출. 기존 팔/다리 순서 및 방향 유지.
- Body-charge88: charge-source 원본에서 264px 재추출. 준비/루프 클립 변경 없음.
- Swipe64와 실제 클립이 읽는 FrameSources24: swipe-source 원본에서 352px 재추출. North6 왼쪽 정리 패치는 그대로 확대 적용.
- SwipeBoth128: smooth-sources에서 352px 재추출. 현재 East 원본/SE→SW 반전 유지. 기존 저해상도 계산으로 발 정렬 위치를 구한 뒤 원본을 두 배 해상도로 렌더한다.
- 368개 재추출 PNG는 PPU200, Rect264 또는352. 이전 PPU100/Rect132 또는176과 월드 캔버스가 같다. 픽셀 샘플링 반올림에 의한 미세 윤곽 차이는 있다.
- 비스트 폴더 전체 PNG577개의 Default/Standalone/Android 압축을 None으로 통일. 나머지209 PNG(이미 고해상도인 Move64 포함)는 바이트 그대로 보존. 더 큰 원본이 없는 Idle/rotations/Sprint/Swipe-left는 인위적인 확대나 재생성 없이 기존 픽셀을 유지한다.
- 새 그림 생성 없이 기존 소스를 재추출했다. 원본 자체의 묘사 한계는 남는다. 비압축/픽셀 수 증가로 GPU 메모리가 늘어나며 기기 메모리 측정은 하지 않았다.

## 검증

`node tools/BeastClarity/validate.mjs`: 전체577 PNG의 월드크기/압축, meta 허용필드외 동일, 재추출368장의 규격/알파/외곽 여백, 나머지209 PNG 바이트 동일 확인. 폴더 내 PNG/meta 외226 파일(클립, 이벤트 포함) 바이트 동일. 전후 비교에는 `.utmp/beast-clarity/before`가 필요하며 영속 결과는 `validation.json`, 대상 목록은 `rebuilt-files.json`이다.

`comparison.png`: 위 이전, 아래 개선. 왼쪽부터 Run South, Body-charge East, Swipe South, SwipeBoth East. 같은 표시 크기의 PNG 비교이며 Unity 렌더가 아니다. Unity 컴파일/Play/Android 실행 미실시.

확인: 임포트 완료 → 각 Animation 클립 재생 → Tools > Play From Title 해제 → MainScene에서 일반 추격/돌진/할퀴기/양손 공격. 크기와 타격 시점이 유지되고 얼굴·몸 무늬가 더 또렷해야 한다. 크기 문제는 PPU/Rect, 구 이미지나 Missing은 Reimport/클립 Sprite 참조/Console 첫 오류를 확인한다.
