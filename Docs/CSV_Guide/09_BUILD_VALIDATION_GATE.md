# 9단원 — CSV 빌드 검증 게이트와 회귀 테스트

## 1. 목표

에디터에서 CSV 오류를 확인하는 것에 그치지 않고, 잘못된 게임 데이터가 상용 플레이어 빌드에 포함되는 것을 자동으로 차단한다.

9단원은 다음 두 단계로 구성된다.

1. Unity 플레이어 빌드 직전 통합 검증
2. 실제 런타임 CSV를 사용하는 에디터 회귀 테스트

검증과 테스트는 모두 Editor 전용 코드이므로 플레이어 런타임 성능과 메모리에 영향을 주지 않는다.

## 2. 빌드 전 자동 검증

개발자가 Unity의 `Build` 또는 `Build And Run`을 실행하면 `GameDataBuildPreprocessor`가 다른 빌드 작업보다 먼저 실행된다.

검증 대상은 8단원의 CSV 데이터 검증기와 동일하다.

- `GameRoot.prefab`에 연결된 필수 CSV
- CSV 헤더와 값 형식
- CSV 내부 중복 ID와 CSV 간 참조
- Dialogue Visual Catalog의 초상화 및 배경 Sprite 연결
- Inventory Controller의 퀘스트 보상 및 소모 아이템 연결

### 오류가 있는 경우

빌드를 즉시 중단하고 Unity Console에 모든 검증 문제를 출력한다.

`Tools > Ruin Conservator > CSV 데이터 검증기`에서 오류 목록과 관련 에셋을 확인한 뒤 다시 빌드한다.

### 경고만 있는 경우

경고를 Unity Console에 출력하지만 빌드는 계속한다.

현재 초상화와 배경은 런타임 폴백이 있으므로 연결 누락을 경고로 분류한다. 출시 후보 빌드에서는 경고도 모두 확인해야 한다.

## 3. 에디터 회귀 테스트

Unity Test Runner의 Edit Mode에서 `GameDataCsvRegressionTests`를 실행한다.

테스트 위치:

`Assets/_Game/Scripts/Editor/GameDataCsvRegressionTests.cs`

검증 항목:

1. 실제 런타임 CSV 12개로 Repository를 생성할 수 있는가
2. 필수 CSV가 누락되면 `(missing)` 오류를 반환하는가
3. 확정 스키마와 다른 헤더를 첫 번째 레코드에서 거부하는가

테스트는 `Docs`의 예시 파일이 아니라 `Assets/_Game/Data/Csv/`의 실제 게임 데이터를 읽는다. 따라서 기획자가 CSV를 교체한 뒤 발생하는 회귀를 바로 찾을 수 있다.

## 4. 기획 데이터 반영 순서

1. 엑셀에서 각 시트를 UTF-8 CSV로 내보낸다.
2. `Assets/_Game/Data/Csv/`의 파일을 교체한다.
3. 8단원 CSV 데이터 검증기에서 오류를 수정한다.
4. Unity Test Runner의 Edit Mode 회귀 테스트를 실행한다.
5. 플레이 모드에서 대화와 퀘스트 흐름을 확인한다.
6. 플레이어 빌드를 실행한다.

3단계를 놓쳐도 6단계의 빌드 검증 게이트가 오류 데이터를 차단한다.

## 5. CI 적용

Unity를 Batch Mode로 실행해 Edit Mode 테스트 또는 플레이어 빌드를 수행하면 동일한 검증 규칙이 적용된다.

플레이어 빌드 과정에서 데이터 오류가 발견되면 `BuildFailedException`이 발생하므로 CI 작업도 실패 상태로 종료된다. 별도의 CSV 검증 스크립트를 복제하지 않고 Unity 프로젝트의 단일 검증 규칙을 사용한다.

## 6. 완료 기준

- 프로젝트 Editor 어셈블리가 오류 없이 컴파일된다.
- `GameDataCsvRegressionTests`가 모두 통과한다.
- CSV 데이터 검증기의 오류가 0개다.
- 데이터 오류가 있는 상태에서 플레이어 빌드가 중단된다.
- 오류를 수정한 뒤 플레이어 빌드가 정상적으로 시작된다.
