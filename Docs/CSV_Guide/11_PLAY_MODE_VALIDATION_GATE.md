# 11단원 — Play Mode CSV 사전 검증 게이트

## 1. 목표

잘못된 CSV 또는 끊어진 Unity 에셋 참조가 있는 상태로 플레이 모드에 진입해 런타임 초기화에서 뒤늦게 실패하는 경로를 차단한다.

11단원은 다음 세 단계로 구성된다.

1. Play 버튼을 누르면 실제 `GameRoot.prefab` 기준 통합 검증 실행
2. 오류가 있으면 Play Mode 진입 취소
3. 차단 정책을 Edit Mode 테스트로 고정

검증 게이트는 `Assets/_Game/Scripts/Editor/`에만 존재하므로 빌드된 게임의 성능과 메모리에 영향을 주지 않는다.

## 2. 동작 순서

개발자가 Unity 에디터에서 Play 버튼을 누르면 다음 순서로 처리된다.

1. `GameDataPlayModeGate`가 `ExitingEditMode` 상태를 감지한다.
2. 8단원의 `GameDataEditorValidator`로 전체 데이터를 검증한다.
3. 검증 결과를 Unity Console에 출력한다.
4. 오류가 1개 이상이면 플레이 모드 전환을 취소한다.
5. 오류가 없으면 경고 개수와 함께 통과 로그를 출력하고 플레이를 계속한다.

별도의 CSV 파서나 검증 규칙을 복제하지 않고 에디터 수동 검증, Play Mode 검증, 빌드 검증이 같은 통합 검증기를 사용한다.

## 3. 오류와 경고 정책

### 오류

게임 데이터 저장소를 안전하게 만들 수 없거나 필수 참조가 깨진 상태다. 플레이 모드 진입을 차단한다.

주요 예시:

- 필수 CSV가 `GameDataController`에 연결되지 않음
- CSV 헤더가 확정 스키마와 다름
- 필수 값의 형식이 잘못됨
- 중복 ID가 존재함
- Dialogue 또는 Quest가 존재하지 않는 ID를 참조함
- Quest 보상 또는 소모 아이템 ID에 대응하는 `ItemDataSO`가 없음

### 경고

런타임 폴백으로 실행할 수 있지만 출시 전 확인해야 하는 상태다. 반복 작업을 방해하지 않도록 플레이 모드 진입은 허용한다.

주요 예시:

- Dialogue 배경 Sprite가 아직 연결되지 않음
- 아이템 아이콘 Sprite가 아직 연결되지 않음

## 4. 사용 방법

1. 엑셀에서 시트를 UTF-8 CSV로 내보낸다.
2. `Assets/_Game/Data/Csv/`의 같은 이름 파일을 교체한다.
3. Unity가 파일 임포트를 마칠 때까지 기다린다.
4. Play 버튼을 누른다.
5. 진입이 중단되면 `Tools > Ruin Conservator > CSV 데이터 검증기`를 연다.
6. 표시된 오류를 수정하고 다시 Play 버튼을 누른다.

경고만 표시되면 플레이 모드는 정상적으로 시작된다. 다만 출시 후보 빌드에서는 모든 경고를 확인해야 한다.

## 5. 자동 테스트

Unity Test Runner의 Edit Mode에서 `GameDataPlayModeGateTests`를 실행한다.

테스트 항목:

1. 검증 보고서를 만들지 못하면 진입을 차단하는가
2. 오류가 있으면 진입을 차단하는가
3. 경고만 있으면 진입을 허용하는가

프로젝트 실제 CSV의 파싱과 참조 회귀는 9단원의 `GameDataCsvRegressionTests`가 계속 담당한다.

## 6. 영속화 범위

Quest 상태, Progress Flag, Inventory 상태를 저장하는 기능은 이번 단원에 포함하지 않는다.

프로젝트 코딩 컨벤션은 모든 영속화를 Easy Save 3로 통일하도록 규정하지만 현재 프로젝트에는 Easy Save 3가 설치되어 있지 않다. 따라서 `PlayerPrefs`, 직접 파일 쓰기, 임시 JSON 저장을 추가하지 않는다. ES3를 프로젝트에 도입한 뒤 CSV의 문자열 ID를 저장 키로 사용하는 별도 단원에서 구현한다.

## 7. 완료 기준

- 프로젝트 Editor 어셈블리가 오류 없이 컴파일된다.
- `GameDataPlayModeGateTests` 3개가 모두 통과한다.
- 실제 CSV 통합 검증 오류가 0개다.
- 검증 오류가 있으면 Play Mode 진입이 취소된다.
- 경고만 있으면 Play Mode 진입이 허용된다.
- 빌드된 플레이어에 11단원 코드가 포함되지 않는다.
