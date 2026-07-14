# 8단원 — CSV와 Unity 에셋 통합 검증

## 1. 목표

CSV 문법과 데이터 참조뿐 아니라, CSV ID가 실제 Unity 에셋까지 정상적으로 연결되는지 에디터에서 확인한다.

검증기는 `GameRoot.prefab`에 실제로 연결된 데이터를 기준으로 동작한다. 따라서 사용하지 않는 테스트 파일이 아니라 게임 실행에 쓰이는 CSV와 컴포넌트 설정을 검사한다.

## 2. 검증 창 사용 방법

Unity 상단 메뉴에서 다음 항목을 연다.

`Tools > Ruin Conservator > CSV 데이터 검증기`

창을 열면 즉시 한 번 검증한다. CSV나 Unity 에셋을 수정한 뒤에는 `전체 데이터 다시 검증` 버튼을 누른다.

문제가 표시되면 `관련 에셋 선택` 버튼으로 해당 `GameRoot`, `DialogueVisualCatalog`, `ItemDataSO`를 Project 또는 Inspector에서 바로 확인할 수 있다.

## 3. 자동 검증

다음 파일이 추가·수정·이동·삭제되면 에셋 가져오기가 끝난 뒤 자동으로 검증한다.

- `Assets/_Game/Data/Csv/` 아래의 CSV
- `Assets/_Game/Data/` 아래의 비주얼 카탈로그와 아이템 데이터
- `Assets/_Game/Prefabs/GameRoot.prefab`

자동 검증은 문제가 있을 때만 Unity Console에 결과를 남긴다. 에셋 변경이 한 번에 여러 건 발생해도 같은 처리 주기에는 검증을 한 번만 실행한다.

## 4. 오류와 경고의 차이

### 오류

게임 진행을 중단시키거나 데이터 로드를 실패하게 만들 수 있으므로 반드시 수정한다.

- 필수 CSV가 `GameDataController`에 연결되지 않음
- CSV 헤더, 값 형식, 중복 ID 또는 CSV 간 참조가 잘못됨
- `GameRoot`의 필수 컨트롤러 연결이 없음
- 퀘스트의 `item_id`가 `InventoryController`의 Item Database에 없음
- Item Database 항목이 비었거나 `ItemId`가 중복됨

### 경고

런타임 폴백으로 실행은 가능하지만 출시 전에 확인한다.

- `sprite_id`가 `DialogueVisualCatalog`의 Portrait Sprites에 없음
- `background_id`가 Background Sprites에 없음
- 비주얼 카탈로그 항목의 ID 또는 Sprite가 비어 있음
- 비주얼 카탈로그 에셋 ID가 중복됨

## 5. 기획자 작업 순서

1. 엑셀 원본을 수정한다.
2. 각 시트를 UTF-8 CSV로 내보낸다.
3. Unity 프로젝트의 `Assets/_Game/Data/Csv/`에 CSV를 덮어쓴다.
4. Unity가 파일 가져오기를 마칠 때까지 기다린다.
5. Console에 오류 또는 경고가 있으면 메시지의 파일명과 ID를 확인한다.
6. `CSV 데이터 검증기`를 열어 전체 검증 결과를 다시 확인한다.
7. 오류가 0개가 된 뒤 Play Mode에서 실제 대화와 퀘스트 흐름을 확인한다.

## 6. Sprite와 아이템 연결 규칙

CSV에는 Unity 에셋 경로를 직접 쓰지 않고 안정적인 문자열 ID만 작성한다.

- `dialogue_portraits.csv`의 `sprite_id` → `DialogueVisualCatalog`의 Portrait Sprites
- `dialogue_graphs.csv`, `dialogue_nodes.csv`의 `background_id` → `DialogueVisualCatalog`의 Background Sprites
- `quest_rewards.csv`, `quest_consume_items.csv`의 `item_id` → `InventoryController`의 Item Database에 등록된 `ItemDataSO.ItemId`

기획자는 CSV의 ID를 관리하고, Unity 에셋 담당자는 같은 ID로 Sprite 또는 `ItemDataSO`를 한 번 연결한다. 이미지 파일이 바뀌어도 CSV ID를 유지하면 기획 데이터는 수정하지 않아도 된다.

## 7. 완료 기준

- 검증 창의 오류가 0개다.
- 의도적으로 폴백을 사용하는 항목을 제외하면 경고가 0개다.
- Unity Console에 `GameDataEditorValidator` 오류가 없다.
- Play Mode에서 CSV 대화 시작, 선택지 이동, 퀘스트 수락·완료·보상 지급을 확인했다.
