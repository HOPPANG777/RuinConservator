# Dialogue·Quest CSV 작성 가이드

## 1. 현재 데이터 구조

Dialogue와 Quest의 기획 데이터는 ScriptableObject가 아니라 CSV를 원본으로 사용합니다.

```text
Excel 작성본
-> UTF-8 CSV 내보내기
-> GameDataController의 TextAsset
-> GameDataCsvLoader
-> GameDataRepository
-> DialogueController / QuestController
```

`DialogueActorSO`, `DialogueGraphSO`, `QuestDataSO`는 더 이상 사용하지 않습니다.

Unity 에셋 참조가 필요한 다음 데이터만 ScriptableObject로 유지합니다.

- `DialogueVisualCatalogSO`: CSV의 초상화·배경 ID를 Unity Sprite에 연결
- `ItemDataSO`: 아이템 아이콘과 인벤토리 표시 데이터 보관
- `SceneCatalogSO`: 씬 참조 보관

## 2. 작업 파일

기획자는 다음 엑셀 교보재를 기준으로 작성합니다.

`Docs/CSV_Guide/RuinConservator_CSV_Authoring_Guide.xlsx`

게임이 실제로 읽는 CSV 위치:

`Assets/_Game/Data/Csv/`

파일 이름과 헤더는 코드 계약이므로 임의로 변경하지 않습니다.

## 3. Dialogue CSV

| 파일 | 역할 |
| --- | --- |
| `dialogue_actors.csv` | 화자 ID, 이름 문장 키, 기본 초상화 정의 |
| `dialogue_portraits.csv` | 화자별 표정과 Sprite ID 정의 |
| `dialogue_graphs.csv` | 대화 그래프 시작 노드와 기본 화자·배경 정의 |
| `dialogue_nodes.csv` | 대사 노드, 화자, 표정, 본문 문장 키, 다음 노드 정의 |
| `dialogue_choices.csv` | 선택지 순서, 문장 키, 다음 노드, 액션 정의 |

주요 ID 규칙:

- `actor_id`: 프로젝트 전체에서 고유한 화자 ID
- `graph_id`: 프로젝트 전체에서 고유한 대화 그래프 ID
- `node_id`: 같은 그래프 안에서 고유한 노드 ID
- `portrait_id`: 같은 화자 안에서 고유한 표정 ID
- `sprite_id`: `DialogueVisualCatalogSO`의 Portrait Sprites와 일치하는 ID
- `background_id`: `DialogueVisualCatalogSO`의 Background Sprites와 일치하는 ID
- `*_text_key`: `localization.csv`에 존재하는 문장 키

ID는 저장 데이터와 참조에 사용하므로 출시 후 의미 없이 변경하지 않습니다.

## 4. Quest CSV

| 파일 | 역할 |
| --- | --- |
| `quests.csv` | 퀘스트 ID, 제목·설명 문장 키, 담당 NPC 정의 |
| `quest_objectives.csv` | 목표 순서, 진행 플래그와 완료 조건 정의 |
| `quest_rewards.csv` | 보상 플래그 또는 지급 아이템 정의 |
| `quest_consume_items.csv` | 완료 시 소모할 아이템과 수량 정의 |

주요 ID 규칙:

- `quest_id`: 프로젝트 전체에서 고유한 퀘스트 ID
- `objective_id`: 같은 퀘스트 안에서 고유한 목표 ID
- `reward_id`: 같은 퀘스트 안에서 고유한 보상 ID
- `item_id`: `InventoryController`의 Item Database에 등록된 `ItemDataSO.ItemId`와 일치하는 ID
- `progress_flag_id`: `ProgressController`에서 사용하는 진행 플래그 ID

아이템 보상은 `item_id`와 `item_amount`를 함께 작성합니다. 아이템 보상이 없으면 두 칸을 모두 비웁니다.

## 5. NPC 대화 연결 CSV

| 파일 | 역할 |
| --- | --- |
| `npc_dialogue_bindings.csv` | NPC ID와 기본 대화 그래프·화자 연결 |
| `npc_quest_dialogue_routes.csv` | 퀘스트 상태별 대화 그래프 우선순위 정의 |

NPC GameObject에는 `NpcDialogueInteractable`을 추가하고 `Npc Id`만 입력합니다.

```text
NpcDialogueInteractable.Interact()
-> GameRoot.Dialogue
-> npc_id로 CSV 경로 조회
-> 현재 퀘스트 상태에 맞는 graph_id 선택
-> 대화 시작
```

`Npc Id`는 `npc_dialogue_bindings.csv`의 `npc_id`와 정확히 같아야 합니다.

## 6. 로컬라이징

화자 이름, 대사, 선택지, 퀘스트 제목·설명·목표·보상 문장은 `localization.csv`에서 관리합니다.

CSV 본문에는 완성 문장을 직접 넣지 않고 `text_key`를 작성합니다.

```text
dialogue_nodes.csv
body_text_key = dialogue.curator.intro.001

localization.csv
text_key = dialogue.curator.intro.001
ko = 복원실에 온 걸 환영합니다.
```

`localization.csv`에 없는 문장 키를 참조하면 Repository 생성과 빌드 검증이 실패합니다.

## 7. Sprite 연결

CSV에는 이미지 경로나 Unity GUID를 입력하지 않습니다. 안정적인 문자열 ID만 입력합니다.

```text
dialogue_portraits.csv의 sprite_id
-> DialogueVisualCatalogSO
-> Portrait Sprites
-> Unity Sprite
```

```text
dialogue_graphs.csv 또는 dialogue_nodes.csv의 background_id
-> DialogueVisualCatalogSO
-> Background Sprites
-> Unity Sprite
```

이미지 파일을 교체하더라도 기존 ID를 유지하면 CSV를 수정할 필요가 없습니다.

## 8. 아이템 연결

퀘스트 CSV의 `item_id`는 Unity의 `ItemDataSO` 에셋을 직접 참조하지 않습니다.

```text
quest_rewards.csv / quest_consume_items.csv의 item_id
-> InventoryController Item Database
-> 같은 ItemId의 ItemDataSO
-> 아이콘과 인벤토리 표시 데이터
```

새 퀘스트 아이템을 추가할 때:

1. `ItemDataSO`를 생성합니다.
2. `Item Id`를 CSV의 `item_id`와 동일하게 입력합니다.
3. 아이콘과 표시 데이터를 입력합니다.
4. `InventoryController`의 Item Database에 등록합니다.
5. CSV 데이터 검증기를 실행합니다.

## 9. Dialogue Choice 액션

선택지의 `action_type`은 코드에서 지원하는 정확한 값만 사용합니다.

대표 액션:

- `NONE`: 별도 액션 없이 다음 노드로 이동
- `END_DIALOGUE`: 대화 종료
- `SET_PROGRESS_FLAG`: 진행 플래그 변경
- `ACCEPT_QUEST`: 퀘스트 수락
- `COMPLETE_QUEST`: 목표와 소모 아이템을 검사하고 퀘스트 완료
- `CLAIM_QUEST_REWARD`: 완료한 퀘스트 보상 수령

액션에 따라 필요한 `action_id`가 다릅니다. 엑셀 교보재의 Action 안내 시트를 기준으로 작성합니다.

## 10. 작업 순서

1. 엑셀 원본에서 Dialogue, Quest, NPC, Localization 시트를 수정합니다.
2. 각 시트를 UTF-8 CSV로 내보냅니다.
3. `Assets/_Game/Data/Csv/`의 같은 이름 파일을 교체합니다.
4. Unity의 에셋 가져오기가 끝날 때까지 기다립니다.
5. `Tools > Ruin Conservator > CSV 데이터 검증기`를 실행합니다.
6. 오류를 모두 수정합니다.
7. Unity Test Runner의 Edit Mode에서 `GameDataCsvRegressionTests`를 실행합니다.
8. Play 버튼을 누르고 사전 검증을 통과한 뒤 실제 대화와 퀘스트 흐름을 확인합니다.
9. 플레이어 빌드를 실행합니다.

Play Mode 진입 직전과 플레이어 빌드 직전에 동일한 검증기가 자동 실행됩니다. 오류가 있으면 각각 Play Mode 진입 또는 빌드를 중단하며, 경고만 있으면 작업을 계속할 수 있습니다.

## 11. 완료 체크리스트

- 모든 필수 CSV 파일이 `GameDataController`에 연결되어 있는가?
- CSV 파일명과 헤더가 확정 스키마와 일치하는가?
- 중복 ID가 없는가?
- 모든 `text_key`가 `localization.csv`에 존재하는가?
- 모든 `sprite_id`와 `background_id`가 비주얼 카탈로그에 등록되어 있는가?
- 모든 퀘스트 `item_id`가 Inventory Item Database에 등록되어 있는가?
- `NpcDialogueInteractable.Npc Id`가 NPC CSV와 일치하는가?
- CSV 데이터 검증기의 오류가 0개인가?
- Edit Mode 회귀 테스트가 통과하는가?
- 대화 시작, 선택지, 퀘스트 수락·완료·보상 흐름을 Play Mode에서 확인했는가?
