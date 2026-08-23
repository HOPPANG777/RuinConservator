# Ruin Conservator Dialogue & Quest CSV 교보재 생성 명세

> 스키마 상태: **CSV Schema v1 확정 (2026-07-14)**  
> 기준 Excel: [`CSV_Guide/RuinConservator_CSV_Authoring_Guide - 복사본.xlsx`](CSV_Guide/RuinConservator_CSV_Authoring_Guide%20-%20복사본.xlsx)  
> 이 문서와 기준 Excel이 충돌할 경우, 실제 기획 입력 양식인 기준 Excel의 시트 이름·헤더·컬럼 순서를 우선한다.

## 1. 문서 목적

이 문서는 다른 AI 또는 문서 제작자에게 전달하여, 게임 기획자가 Excel에서 대화와 퀘스트 데이터를 작성하고 시트별 CSV 파일로 내보낼 수 있는 **예시 교보재와 템플릿 통합 문서**를 생성하기 위한 원본 명세다.

이 문서만 전달받아도 다음 결과물을 만들 수 있어야 한다.

1. 기획자용 Excel 통합 문서 `RuinConservator_CSV_Authoring_Guide.xlsx`
2. 각 데이터 시트를 UTF-8 CSV로 내보낸 예시 파일들
3. 정상 입력 예시와 대표적인 잘못된 입력 예시
4. 시트별 컬럼 설명, 필수 여부, 입력 규칙, 참조 대상 설명
5. 기획자가 따라 할 수 있는 짧은 실습 시나리오

이 프로젝트에서 CSV는 ScriptableObject를 생성하기 위한 중간 파일이 아니다. CSV가 대화 및 퀘스트 원본 데이터이며, 게임 실행 시 CSV를 일반 C# 데이터 객체로 읽어 `graph_id`, `quest_id` 등의 ID로 조회하는 구조를 목표로 한다.

---

## 2. 기획자에게 먼저 설명해야 할 핵심 개념

### 2.1 Excel 파일과 CSV 파일의 차이

- `.xlsx`는 여러 시트, 셀 색상, 설명, 드롭다운, 고정 행 등을 포함할 수 있는 **기획 작업용 원본 파일**이다.
- `.csv`는 시트 한 장의 표 데이터만 저장하는 **게임 입력용 텍스트 파일**이다.
- CSV 하나에는 Excel 시트 하나만 들어간다.
- 따라서 기획자는 하나의 `.xlsx`에서 여러 시트를 관리하고, 게임에 반영할 때 각 데이터 시트를 별도의 CSV로 내보낸다.
- `README`, `CodeLists`, 오류 예시 시트는 설명용이므로 CSV로 내보내지 않는다.

### 2.2 데이터가 서로 연결되는 방법

Unity 에셋을 직접 연결하지 않고 문자열 ID로 연결한다.

예시:

```text
DialogueChoices.action_id = QUEST_RESTORE_MURAL
                         ↓
Quests.quest_id          = QUEST_RESTORE_MURAL
```

```text
DialogueNodes.actor_id = ACTOR_CURATOR
                       ↓
DialogueActors.actor_id = ACTOR_CURATOR
```

ID의 철자와 대소문자가 다르면 서로 다른 데이터로 판단한다.

### 2.3 한 행의 의미

- 데이터 시트의 첫 번째 행은 반드시 영문 컬럼 헤더다.
- 두 번째 행부터 실제 데이터다.
- 하나의 행은 하나의 데이터 항목이다.
- 한 셀에 여러 목표나 선택지를 `|`, `/`, 줄바꿈으로 합쳐 적지 않는다.
- 목표, 선택지, 보상처럼 개수가 여러 개인 데이터는 별도 시트에서 여러 행으로 작성한다.

---

## 3. Excel 통합 문서 구성

다음 시트를 순서대로 생성한다.

| 순서 | Excel 시트 이름 | CSV 파일 이름 | CSV 내보내기 |
|---:|---|---|---:|
| 1 | `README` | 없음 | 아니요 |
| 2 | `CodeLists` | 없음 | 아니요 |
| 3 | `DialogueActors` | `dialogue_actors.csv` | 예 |
| 4 | `DialoguePortraits` | `dialogue_portraits.csv` | 예 |
| 5 | `DialogueGraphs` | `dialogue_graphs.csv` | 예 |
| 6 | `DialogueNodes` | `dialogue_nodes.csv` | 예 |
| 7 | `DialogueChoices` | `dialogue_choices.csv` | 예 |
| 8 | `Quests` | `quests.csv` | 예 |
| 9 | `QuestObjectives` | `quest_objectives.csv` | 예 |
| 10 | `QuestRewards` | `quest_rewards.csv` | 예 |
| 11 | `QuestConsumeItems` | `quest_consume_items.csv` | 예 |
| 12 | `Localization` | `localization.csv` | 예 |
| 13 | `NpcDialogueBindings` | `npc_dialogue_bindings.csv` | 선택 |
| 14 | `NpcQuestDialogueRoutes` | `npc_quest_dialogue_routes.csv` | 선택 |
| 15 | `InvalidExamples` | 없음 | 아니요 |

`NpcDialogueBindings`와 `NpcQuestDialogueRoutes`는 NPC의 기본 대화와 퀘스트 상태별 대화까지 CSV로 관리할 때 사용한다. 초기 개발에서 NPC 컴포넌트에 문자열 ID를 직접 입력한다면 생략할 수 있다.

---

## 4. 공통 작성 규칙

### 4.1 ID 규칙

- 시스템 ID는 영문 대문자, 숫자, 밑줄만 사용한다.
- 공백, 한글, 쉼표, 하이픈은 사용하지 않는다.
- 권장 정규식은 `^[A-Z][A-Z0-9_]*$`다.
- ID는 화면에 출력하는 문장이 아니므로 기획 중 의미가 분명한 이름을 사용한다.
- 출시 후 저장 데이터에서 사용된 `quest_id`는 가급적 변경하지 않는다.
- 로컬라이징 키는 영문 소문자와 마침표를 사용한다.

좋은 예:

```text
DLG_CURATOR_INTRO
NODE_ACCEPT_QUEST
ACTOR_CURATOR
QUEST_RESTORE_MURAL
FLAG_MURAL_INSPECTED
dialogue.curator.intro.001
```

나쁜 예:

```text
대화1
Quest 01
test-node
NPC,Curator
```

### 4.2 빈 셀

- 선택 컬럼에 값이 필요 없으면 셀을 완전히 비운다.
- `없음`, `null`, `NULL`, `-`, `0` 같은 임의 표기를 사용하지 않는다.
- 필수 컬럼은 빈 셀을 허용하지 않는다.

### 4.3 Boolean

- Boolean은 소문자 `true` 또는 `false`만 사용한다.
- `TRUE`, `False`, `O`, `X`, `1`, `0`, `예`, `아니오`는 사용하지 않는다.

### 4.4 숫자

- 순서와 수량은 0 이상의 정수만 사용한다.
- `choice_order`, `objective_order`, `reward_order`, `consume_order`는 같은 부모 안에서 중복되면 안 된다.
- 순서는 0부터 시작해 1씩 증가시키는 것을 권장한다.

### 4.5 문장과 줄바꿈

- 대사나 설명 안에 쉼표가 있으면 CSV 변환 시 셀 전체가 큰따옴표로 감싸져야 한다.
- 셀 안의 큰따옴표는 CSV에서 `""`로 표현된다.
- 대사 내부 줄바꿈은 실제 셀 줄바꿈 대신 문자 `\n`을 권장한다.
- 게임 로더는 `\n`을 실제 줄바꿈으로 변환한다.

예시:

```csv
dialogue.curator.warning,"잠깐, 그 유물은 만지면 안 돼!\n먼저 장갑을 착용해.","Wait, do not touch that relic!\nPut on gloves first.",
```

### 4.6 Excel 작성 규칙

- 데이터 영역에 셀 병합을 사용하지 않는다.
- 수식, 매크로, 숨김 행, 숨김 열을 사용하지 않는다.
- 데이터 중간에 빈 행을 삽입하지 않는다.
- 컬럼 순서와 영문 헤더를 임의로 변경하지 않는다.
- 설명은 데이터 시트 상단에 추가 행으로 넣지 않고 `README` 또는 셀 메모에 작성한다.
- 필터와 첫 번째 행 고정을 적용한다.
- ID 컬럼의 셀 서식은 텍스트로 지정한다.

---

## 5. Dialogue 시트 명세

### 5.1 DialogueActors

CSV 파일: `dialogue_actors.csv`

화자 한 명의 기본 정보를 정의한다. 화면 표시 문장은 로컬라이징 키로 연결한다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `actor_id` | ID | 예 | 화자 고유 ID |
| `display_name_key` | Localization Key | 예 | 화면에 표시할 화자 이름 키 |
| `default_portrait_id` | ID | 아니요 | 노드에서 표정을 지정하지 않았을 때 사용할 초상화 ID |

예시:

```csv
actor_id,display_name_key,default_portrait_id
ACTOR_PLAYER,actor.player.name,NORMAL
ACTOR_CURATOR,actor.curator.name,NORMAL
```

검증 규칙:

- `actor_id`는 시트 전체에서 고유해야 한다.
- `default_portrait_id`가 있다면 동일 `actor_id`의 `DialoguePortraits.portrait_id`에 존재해야 한다.
- `display_name_key`는 `Localization.text_key`에 존재해야 한다.

### 5.2 DialoguePortraits

CSV 파일: `dialogue_portraits.csv`

화자의 표정 및 초상화 에셋 ID를 정의한다. CSV는 Sprite 객체를 직접 저장하지 않고 `sprite_id`만 저장한다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `actor_id` | ID | 예 | 소유 화자 ID |
| `portrait_id` | ID | 예 | 해당 화자 안에서 고유한 표정 ID |
| `display_name_key` | Localization Key | 아니요 | 표정에 따라 화자 표시명을 바꿀 때 사용 |
| `sprite_id` | Asset ID | 예 | 런타임 에셋 Resolver가 사용할 Sprite ID |

예시:

```csv
actor_id,portrait_id,display_name_key,sprite_id
ACTOR_PLAYER,NORMAL,,SPR_PLAYER_NORMAL
ACTOR_CURATOR,NORMAL,,SPR_CURATOR_NORMAL
ACTOR_CURATOR,WORRIED,,SPR_CURATOR_WORRIED
ACTOR_CURATOR,HAPPY,,SPR_CURATOR_HAPPY
```

고유 키는 `(actor_id, portrait_id)` 조합이다.

### 5.3 DialogueGraphs

CSV 파일: `dialogue_graphs.csv`

대화 묶음의 시작점과 기본 화자, 화면 참가자를 정의한다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `graph_id` | ID | 예 | 대화 그래프 고유 ID |
| `start_node_id` | ID | 예 | 대화 시작 노드 ID |
| `default_actor_id` | ID | 아니요 | 노드 화자가 비어 있을 때 사용할 기본 화자 |
| `left_actor_id` | ID | 아니요 | 대화 UI 왼쪽 참가자 |
| `right_actor_id` | ID | 아니요 | 대화 UI 오른쪽 참가자 |
| `background_id` | Asset ID | 아니요 | 그래프 기본 대화창 배경 ID |

예시:

```csv
graph_id,start_node_id,default_actor_id,left_actor_id,right_actor_id,background_id
DLG_CURATOR_INTRO,NODE_START,ACTOR_CURATOR,ACTOR_PLAYER,ACTOR_CURATOR,BG_MUSEUM_HALL
DLG_CURATOR_IN_PROGRESS,NODE_PROGRESS,ACTOR_CURATOR,ACTOR_PLAYER,ACTOR_CURATOR,BG_MUSEUM_HALL
DLG_CURATOR_COMPLETE,NODE_COMPLETE,ACTOR_CURATOR,ACTOR_PLAYER,ACTOR_CURATOR,BG_MUSEUM_HALL
```

검증 규칙:

- `graph_id`는 시트 전체에서 고유해야 한다.
- `start_node_id`는 같은 `graph_id`의 `DialogueNodes.node_id`에 반드시 존재해야 한다.
- Actor ID가 입력됐다면 `DialogueActors.actor_id`에 존재해야 한다.
- 시작 노드가 없을 때 첫 번째 행으로 대신 실행하는 암묵적 fallback을 허용하지 않는다.

### 5.4 DialogueNodes

CSV 파일: `dialogue_nodes.csv`

한 행은 대화창에 한 번 표시되는 대사 한 문단을 의미한다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `graph_id` | ID | 예 | 이 노드가 속한 그래프 ID |
| `node_id` | ID | 예 | 그래프 안에서 고유한 노드 ID |
| `actor_id` | ID | 아니요 | 이 노드에서 말하는 화자. 비우면 그래프 기본 화자 사용 |
| `portrait_id` | ID | 아니요 | 이 노드에서 사용할 화자의 표정 ID |
| `background_id` | Asset ID | 아니요 | 이 노드에서만 덮어쓸 배경 ID |
| `body_text_key` | Localization Key | 예 | 대화 본문 키 |
| `next_node_id` | ID | 아니요 | 선택지가 없을 때 이동할 다음 노드. 비우면 대화 종료 |

예시:

```csv
graph_id,node_id,actor_id,portrait_id,background_id,body_text_key,next_node_id
DLG_CURATOR_INTRO,NODE_START,ACTOR_CURATOR,WORRIED,,dialogue.curator.intro.001,
DLG_CURATOR_INTRO,NODE_ACCEPTED,ACTOR_CURATOR,NORMAL,,dialogue.curator.intro.accepted,
DLG_CURATOR_IN_PROGRESS,NODE_PROGRESS,ACTOR_CURATOR,WORRIED,,dialogue.curator.progress.001,
DLG_CURATOR_COMPLETE,NODE_COMPLETE,ACTOR_CURATOR,HAPPY,,dialogue.curator.complete.001,
```

고유 키는 `(graph_id, node_id)` 조합이다. 서로 다른 그래프에서는 `NODE_START` 같은 노드 ID를 재사용해도 된다.

검증 규칙:

- `graph_id`는 `DialogueGraphs.graph_id`에 존재해야 한다.
- `next_node_id`가 있다면 같은 그래프의 노드에 존재해야 한다.
- `actor_id`가 비어 있지 않다면 `DialogueActors.actor_id`에 존재해야 한다.
- `portrait_id`가 있다면 최종 결정된 화자에게 해당 표정이 존재해야 한다.
- 선택지가 하나 이상 연결된 노드는 `next_node_id`를 비우는 것을 원칙으로 한다.
- 선택지가 없는 노드에서 `next_node_id`가 비어 있으면 해당 대사 출력 후 대화를 종료한다.

### 5.5 DialogueChoices

CSV 파일: `dialogue_choices.csv`

대화 노드에 표시할 선택지와 선택 직후 실행할 액션을 정의한다. 선택지가 없는 노드는 이 시트에 행을 만들지 않는다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `graph_id` | ID | 예 | 선택지가 속한 그래프 ID |
| `node_id` | ID | 예 | 선택지가 속한 노드 ID |
| `choice_order` | 정수 | 예 | 화면 표시 순서. 0부터 시작 권장 |
| `choice_text_key` | Localization Key | 예 | 선택지 표시 문장 키 |
| `next_node_id` | ID | 아니요 | 선택 후 이동할 같은 그래프의 노드 ID |
| `action_type` | Enum | 예 | 선택 시 실행할 액션 종류 |
| `action_id` | ID | 조건부 | 액션 대상 Flag ID 또는 Quest ID |

예시:

```csv
graph_id,node_id,choice_order,choice_text_key,next_node_id,action_type,action_id
DLG_CURATOR_INTRO,NODE_START,0,dialogue.curator.choice.accept,NODE_ACCEPTED,ACCEPT_QUEST,QUEST_RESTORE_MURAL
DLG_CURATOR_INTRO,NODE_START,1,dialogue.curator.choice.decline,,END_DIALOGUE,
DLG_CURATOR_IN_PROGRESS,NODE_PROGRESS,0,dialogue.curator.choice.refresh,,REFRESH_QUEST_PROGRESS,QUEST_RESTORE_MURAL
DLG_CURATOR_COMPLETE,NODE_COMPLETE,0,dialogue.curator.choice.reward,,CLAIM_QUEST_REWARD,QUEST_RESTORE_MURAL
```

고유 키는 `(graph_id, node_id, choice_order)` 조합이다.

허용하는 `action_type`:

| 값 | `action_id` 의미 | `action_id` 필수 |
|---|---|---:|
| `NONE` | 사용하지 않음 | 아니요 |
| `SET_PROGRESS_FLAG_TRUE` | true로 변경할 Progress Flag ID | 예 |
| `SET_PROGRESS_FLAG_FALSE` | false로 변경할 Progress Flag ID | 예 |
| `ACCEPT_QUEST` | 수락할 Quest ID | 예 |
| `REFRESH_QUEST_PROGRESS` | 진행도를 갱신할 Quest ID | 예 |
| `COMPLETE_QUEST` | 완료 확정할 Quest ID | 예 |
| `CLAIM_QUEST_REWARD` | 보상을 받을 Quest ID | 예 |
| `END_DIALOGUE` | 사용하지 않음 | 아니요 |

추가 규칙:

- 숫자 enum 값은 입력하지 않고 위 문자열을 정확히 입력한다.
- Quest 액션의 `action_id`는 `Quests.quest_id`에 존재해야 한다.
- Progress Flag 액션의 `action_id`는 프로젝트의 Flag 명명 규칙을 따라야 한다.
- `END_DIALOGUE`이면 `next_node_id`와 `action_id`는 비운다.
- `NONE`이고 `next_node_id`도 비어 있으면 선택 직후 대화를 종료한다.
- 액션과 다음 노드 이동을 함께 설정할 수 있다. 액션 실행 후 지정한 노드로 이동한다.

---

## 6. Quest 시트 명세

### 6.1 Quests

CSV 파일: `quests.csv`

퀘스트 한 개의 기본 정보를 정의한다. 목표, 보상, 자동 소모 아이템은 별도 시트에서 연결한다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `quest_id` | ID | 예 | 퀘스트 고유 ID이자 저장 키 |
| `title_text_key` | Localization Key | 예 | 퀘스트 제목 키 |
| `description_text_key` | Localization Key | 예 | 퀘스트 설명 키 |
| `owner_npc_id` | ID | 아니요 | 퀘스트의 주요 NPC ID |

예시:

```csv
quest_id,title_text_key,description_text_key,owner_npc_id
QUEST_RESTORE_MURAL,quest.restore_mural.title,quest.restore_mural.description,NPC_CURATOR
```

검증 규칙:

- `quest_id`는 시트 전체에서 고유해야 한다.
- 저장 데이터에서 사용하는 키이므로 출시 후 의미 없이 변경하지 않는다.
- 제목과 설명 키는 `Localization.text_key`에 존재해야 한다.

### 6.2 QuestObjectives

CSV 파일: `quest_objectives.csv`

Progress Flag를 이용한 퀘스트 완료 목표를 정의한다. 퀘스트 하나에 여러 행을 연결할 수 있다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `quest_id` | ID | 예 | 소속 퀘스트 ID |
| `objective_order` | 정수 | 예 | 목표 표시 및 판정 순서 |
| `objective_id` | ID | 예 | 퀘스트 안에서 고유한 목표 ID |
| `description_text_key` | Localization Key | 예 | 목표 설명 키 |
| `required_progress_flag_id` | ID | 예 | 목표 판정에 사용할 Flag ID |
| `required_flag_value` | Boolean | 예 | 목표 달성으로 인정할 Flag 값 |

예시:

```csv
quest_id,objective_order,objective_id,description_text_key,required_progress_flag_id,required_flag_value
QUEST_RESTORE_MURAL,0,OBJECTIVE_INSPECT_MURAL,quest.restore_mural.objective.inspect,FLAG_MURAL_INSPECTED,true
QUEST_RESTORE_MURAL,1,OBJECTIVE_REPORT_DAMAGE,quest.restore_mural.objective.report,FLAG_MURAL_DAMAGE_REPORTED,true
```

고유 키는 `(quest_id, objective_id)` 조합이며, `(quest_id, objective_order)`도 중복되면 안 된다.

### 6.3 QuestRewards

CSV 파일: `quest_rewards.csv`

퀘스트 보상 수령 시 적용할 Progress Flag와 아이템 보상을 정의한다. 한 행에서 Flag와 아이템을 동시에 지급할 수 있다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `quest_id` | ID | 예 | 소속 퀘스트 ID |
| `reward_order` | 정수 | 예 | 보상 표시 및 적용 순서 |
| `reward_id` | ID | 예 | 퀘스트 안에서 고유한 보상 ID |
| `description_text_key` | Localization Key | 예 | 보상 설명 키 |
| `progress_flag_id` | ID | 아니요 | 지급 시 변경할 Flag ID |
| `flag_value` | Boolean | 조건부 | Flag에 저장할 값 |
| `item_id` | ID | 아니요 | 지급할 인벤토리 Item ID |
| `item_amount` | 양의 정수 | 조건부 | 지급 수량 |

예시:

```csv
quest_id,reward_order,reward_id,description_text_key,progress_flag_id,flag_value,item_id,item_amount
QUEST_RESTORE_MURAL,0,REWARD_REPUTATION,quest.restore_mural.reward.reputation,FLAG_CURATOR_TRUSTED,true,,
QUEST_RESTORE_MURAL,1,REWARD_COINS,quest.restore_mural.reward.coins,,,ITEM_COIN,100
```

조건부 규칙:

- `progress_flag_id`가 있으면 `flag_value`가 필수다.
- `progress_flag_id`가 없으면 `flag_value`를 비운다.
- `item_id`가 있으면 `item_amount`는 1 이상의 정수여야 한다.
- `item_id`가 없으면 `item_amount`를 비운다.
- 최소한 Flag 보상 또는 아이템 보상 중 하나는 있어야 한다.
- `item_id`는 프로젝트의 Item 데이터 저장소에 존재해야 한다.

### 6.4 QuestConsumeItems

CSV 파일: `quest_consume_items.csv`

퀘스트 완료 확정 시 인벤토리에서 자동 소모할 아이템을 정의한다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `quest_id` | ID | 예 | 소속 퀘스트 ID |
| `consume_order` | 정수 | 예 | 소모 처리 순서 |
| `item_id` | ID | 예 | 자동 소모할 인벤토리 Item ID |
| `item_amount` | 양의 정수 | 예 | 자동 소모할 수량 |

예시:

```csv
quest_id,consume_order,item_id,item_amount
QUEST_RESTORE_MURAL,0,ITEM_REPAIR_KIT,1
```

검증 규칙:

- `quest_id`는 `Quests.quest_id`에 존재해야 한다.
- `item_id`는 프로젝트의 Item 데이터 저장소에 존재해야 한다.
- `item_amount`는 1 이상이어야 한다.
- 같은 퀘스트에 같은 `item_id`를 여러 행으로 중복 작성하지 않는다.

---

## 7. Localization 시트 명세

CSV 파일: `localization.csv`

화면에 표시되는 모든 문장을 키로 관리한다. 초기에는 한국어만 작성해도 되지만, 향후 번역을 위해 언어 컬럼을 유지한다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `text_key` | Localization Key | 예 | 문장 고유 키 |
| `ko` | 문자열 | 예 | 한국어 원문 |
| `en` | 문자열 | 아니요 | 영어 번역 |
| `ja` | 문자열 | 아니요 | 일본어 번역 |

예시:

```csv
text_key,ko,en,ja
actor.player.name,플레이어,Player,
actor.curator.name,큐레이터,Curator,
dialogue.curator.intro.001,"벽화가 심하게 손상됐어.\n조사를 도와주겠나?","The mural is badly damaged.\nWill you help investigate?",
dialogue.curator.intro.accepted,고맙네. 먼저 손상된 부분을 자세히 살펴봐 줘.,Thank you. Inspect the damaged area first.,
dialogue.curator.progress.001,아직 조사 결과가 부족하네.,We still need more findings.,
dialogue.curator.complete.001,훌륭하군. 덕분에 복원 계획을 세울 수 있겠어.,Excellent. Now we can plan the restoration.,
dialogue.curator.choice.accept,도와드리겠습니다.,I'll help.,
dialogue.curator.choice.decline,지금은 어렵습니다.,Not right now.,
dialogue.curator.choice.refresh,조사 결과를 보고한다.,Report the findings.,
dialogue.curator.choice.reward,보상을 받는다.,Receive the reward.,
quest.restore_mural.title,손상된 벽화 조사,Investigate the Damaged Mural,
quest.restore_mural.description,박물관 홀의 손상된 벽화를 조사하고 큐레이터에게 보고하자.,Inspect the damaged mural and report to the curator.,
quest.restore_mural.objective.inspect,손상된 벽화를 조사한다.,Inspect the damaged mural.,
quest.restore_mural.objective.report,벽화의 손상 상태를 보고한다.,Report the mural's condition.,
quest.restore_mural.reward.reputation,큐레이터의 신뢰를 얻는다.,Gain the curator's trust.,
quest.restore_mural.reward.coins,보존 활동 지원금 100을 받는다.,Receive 100 conservation funds.,
```

검증 규칙:

- `text_key`는 시트 전체에서 고유해야 한다.
- 다른 시트에서 참조한 모든 Text Key가 존재해야 한다.
- `ko`는 비어 있으면 안 된다.
- 번역이 준비되지 않은 언어는 빈 셀로 둔다. 원문이나 임시 문구를 복사해 채우지 않는다.

---

## 8. 선택 사항: NPC 대화 연결 시트

### 8.1 NpcDialogueBindings

CSV 파일: `npc_dialogue_bindings.csv`

NPC의 기본 대화와 화자 오버라이드를 CSV에서 관리할 때 사용한다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `npc_id` | ID | 예 | NPC 고유 ID |
| `default_graph_id` | ID | 예 | 아무 조건도 맞지 않을 때 실행할 기본 Graph ID |
| `actor_override_id` | ID | 아니요 | NPC별 화자 오버라이드 ID |

예시:

```csv
npc_id,default_graph_id,actor_override_id
NPC_CURATOR,DLG_CURATOR_INTRO,ACTOR_CURATOR
```

### 8.2 NpcQuestDialogueRoutes

CSV 파일: `npc_quest_dialogue_routes.csv`

NPC가 퀘스트 상태에 따라 다른 대화를 실행하도록 정의한다. `route_order`가 낮은 행부터 검사한다.

| 컬럼 | 형식 | 필수 | 설명 |
|---|---|---:|---|
| `npc_id` | ID | 예 | 대상 NPC ID |
| `route_order` | 정수 | 예 | 조건 검사 우선순위 |
| `quest_id` | ID | 예 | 상태를 확인할 Quest ID |
| `quest_state` | Enum | 예 | 일치해야 하는 Quest 상태 |
| `graph_id` | ID | 예 | 조건이 맞을 때 실행할 Graph ID |

예시:

```csv
npc_id,route_order,quest_id,quest_state,graph_id
NPC_CURATOR,0,QUEST_RESTORE_MURAL,QUEST_COMPLETED,DLG_CURATOR_COMPLETE
NPC_CURATOR,1,QUEST_RESTORE_MURAL,QUEST_IN_PROGRESS,DLG_CURATOR_IN_PROGRESS
```

허용하는 `quest_state`:

```text
QUEST_NOT_STARTED
QUEST_IN_PROGRESS
QUEST_COMPLETED
QUEST_REWARDED
```

완료 대화를 진행 중 대화보다 먼저 검사하도록 우선순위를 작성한다.

---

## 9. CodeLists 시트 구성

`CodeLists` 시트에는 드롭다운 데이터와 간단한 설명을 제공한다. 이 시트는 CSV로 내보내지 않는다.

### DialogueChoiceActionType 목록

```text
NONE
SET_PROGRESS_FLAG_TRUE
SET_PROGRESS_FLAG_FALSE
ACCEPT_QUEST
REFRESH_QUEST_PROGRESS
COMPLETE_QUEST
CLAIM_QUEST_REWARD
END_DIALOGUE
```

### QuestState 목록

```text
QUEST_NOT_STARTED
QUEST_IN_PROGRESS
QUEST_COMPLETED
QUEST_REWARDED
```

### Boolean 목록

```text
true
false
```

Excel 데이터 유효성 검사를 이용해 다음 컬럼에 드롭다운을 적용한다.

- `DialogueChoices.action_type`
- `NpcQuestDialogueRoutes.quest_state`
- `QuestObjectives.required_flag_value`
- `QuestRewards.flag_value`

ID 참조 컬럼은 가능하다면 해당 시트 ID 범위를 참조하는 드롭다운을 적용한다. 단, 데이터가 늘어났을 때 범위가 자동으로 확장되도록 Excel 표 기능을 사용한다.

---

## 10. README 시트에 포함할 내용

README 시트는 비개발자도 이해할 수 있는 한국어로 작성하고 다음 내용을 포함한다.

1. 이 파일의 목적
2. `.xlsx`는 작업 원본이고 `.csv`는 게임 입력 파일이라는 설명
3. 권장 작업 순서
4. 각 시트와 내보낼 CSV 파일 이름
5. ID를 변경하면 참조와 세이브 데이터가 깨질 수 있다는 경고
6. 빈 셀 작성 규칙
7. Boolean과 Enum 입력 규칙
8. 쉼표, 큰따옴표, `\n` 작성 예시
9. Excel에서 `CSV UTF-8(쉼표로 분리)`로 저장하는 방법
10. CSV 내보내기 후 파일을 Excel로 다시 열어 저장하지 말라는 주의
11. 오류가 발생했을 때 확인할 체크리스트

권장 작업 순서:

```text
1. DialogueActors와 DialoguePortraits 작성
2. Quests 작성
3. QuestObjectives, QuestRewards, QuestConsumeItems 작성
4. DialogueGraphs 작성
5. DialogueNodes 작성
6. DialogueChoices 작성
7. Localization 문장 작성
8. ID와 참조 관계 검사
9. 각 데이터 시트를 UTF-8 CSV로 내보내기
10. 게임 데이터 검증 도구 실행
```

---

## 11. InvalidExamples 시트에 포함할 오류 예시

오류 행은 빨간색 계열로 표시하고, 옆 컬럼에 오류 원인과 수정 방법을 적는다. 실제 데이터 시트에는 오류 행을 넣지 않는다.

반드시 포함할 오류 사례:

| 오류 사례 | 잘못된 예 | 수정 방향 |
|---|---|---|
| 중복 Quest ID | `QUEST_RESTORE_MURAL` 두 행 | 한 행만 유지하거나 새 ID 부여 |
| 존재하지 않는 시작 노드 | `start_node_id=NODE_MISSING` | 실제 Node ID 입력 |
| 다른 그래프의 다음 노드 참조 | 현재 그래프에 없는 `next_node_id` | 같은 Graph의 Node ID 사용 |
| 존재하지 않는 Actor | `actor_id=ACTOR_UNKNOWN` | DialogueActors에 등록 |
| 잘못된 Action Type | `AcceptQuest` | `ACCEPT_QUEST` 사용 |
| Quest 액션 ID 누락 | `ACCEPT_QUEST`인데 `action_id`가 빈 셀 | 실제 Quest ID 입력 |
| END_DIALOGUE 충돌 | `END_DIALOGUE`인데 다음 노드 입력 | 다음 노드 비우기 |
| 잘못된 Boolean | `TRUE`, `1`, `O` | `true` 또는 `false` 사용 |
| 잘못된 수량 | `item_amount=0` 또는 음수 | 1 이상의 정수 사용 |
| Item ID 없이 수량만 입력 | `item_id` 빈 셀, `item_amount=100` | 둘 다 비우거나 Item ID 입력 |
| 한 셀에 선택지 여러 개 입력 | `수락|거절` | DialogueChoices에 두 행 작성 |
| Localization Key 누락 | 참조 키가 Localization에 없음 | 해당 키와 문장 추가 |
| ID에 공백 또는 한글 사용 | `퀘스트 1` | `QUEST_001` 형태 사용 |

---

## 12. Excel 시각 디자인 요구사항

교보재는 기획자가 실무에서 반복 사용할 수 있도록 다음 형식을 적용한다.

- 전체 스타일은 읽기 쉬운 업무용 표 형식으로 구성한다.
- 각 데이터 범위를 Excel 표로 만든다.
- 첫 번째 헤더 행을 고정한다.
- 필터를 활성화한다.
- 필수 컬럼 헤더는 진한 파란색, 선택 컬럼 헤더는 연한 파란색으로 구분한다.
- ID 및 참조 컬럼은 연한 노란색 셀 배경을 사용한다.
- 기획자가 직접 작성하는 표시 문장 컬럼은 연한 초록색으로 구분한다.
- Enum과 Boolean 컬럼은 드롭다운을 적용한다.
- 헤더 셀 메모에 한국어 설명, 필수 여부, 예시 값을 넣는다.
- 필수 셀이 비어 있으면 조건부 서식으로 빨간색 표시한다.
- 중복이 금지된 ID는 조건부 서식으로 중복 값을 강조한다.
- 샘플 데이터 행과 기획자 신규 입력용 빈 행을 시각적으로 구분한다.
- CSV로 내보낼 때 데이터가 아닌 색상과 메모는 사라진다는 점을 README에 명시한다.

주의: 데이터 시트의 실제 CSV 헤더와 데이터 값은 이 문서의 명세를 정확히 따라야 하며, 보기 좋게 만들기 위해 임의의 설명 행이나 보조 컬럼을 데이터 영역 안에 추가하면 안 된다.

---

## 13. CSV 내보내기 규칙

1. 데이터 시트 하나를 선택한다.
2. `다른 이름으로 저장`을 선택한다.
3. 파일 형식은 `CSV UTF-8(쉼표로 분리)(*.csv)`를 선택한다.
4. 이 문서에 지정된 정확한 파일 이름으로 저장한다.
5. 여러 시트 중 현재 시트만 저장된다는 Excel 경고는 정상이다.
6. 저장한 CSV는 UTF-8 인코딩이어야 한다. UTF-8 BOM은 허용한다.
7. CSV 파일을 Excel로 다시 열어 덮어쓰지 않는다. Excel이 ID나 문자열 형식을 자동 변환할 수 있다.
8. 수정은 항상 원본 `.xlsx`에서 하고 CSV를 다시 내보낸다.
9. Git에 올리기 전 CSV diff에서 불필요한 전체 행 변경이 없는지 확인한다.

권장 출력 폴더:

```text
Assets/_Game/Data/Csv/
├─ Dialogue/
│  ├─ dialogue_actors.csv
│  ├─ dialogue_portraits.csv
│  ├─ dialogue_graphs.csv
│  ├─ dialogue_nodes.csv
│  └─ dialogue_choices.csv
├─ Quests/
│  ├─ quests.csv
│  ├─ quest_objectives.csv
│  ├─ quest_rewards.csv
│  └─ quest_consume_items.csv
├─ Localization/
│  └─ localization.csv
└─ Npc/
   ├─ npc_dialogue_bindings.csv
   └─ npc_quest_dialogue_routes.csv
```

---

## 14. 전체 샘플 시나리오

교보재에는 다음 흐름이 실제 행으로 연결되어 있어야 한다.

```text
큐레이터와 첫 대화
  ├─ "도와드리겠습니다"
  │    ├─ QUEST_RESTORE_MURAL 수락
  │    └─ NODE_ACCEPTED로 이동
  └─ "지금은 어렵습니다"
       └─ 대화 종료

퀘스트 진행 중
  ├─ FLAG_MURAL_INSPECTED 확인
  ├─ FLAG_MURAL_DAMAGE_REPORTED 확인
  └─ 진행도 갱신

퀘스트 목표 달성 후
  ├─ 완료 대화 표시
  ├─ 보상 수령
  ├─ FLAG_CURATOR_TRUSTED=true
  └─ ITEM_COIN 100개 지급
```

샘플의 모든 참조 ID가 실제 대상 행과 연결되어야 한다. 보여 주기 위한 가짜 ID 또는 누락된 참조를 정상 데이터 시트에 넣지 않는다.

---

## 15. 생성 결과 검수 체크리스트

다른 AI가 결과물을 만든 후 다음 항목을 검사한다.

### 파일 구성

- [ ] `.xlsx` 파일이 존재한다.
- [ ] 필수 시트가 모두 존재한다.
- [ ] CSV로 내보낼 시트와 설명용 시트가 구분되어 있다.
- [ ] 각 CSV 파일 이름이 명세와 정확히 일치한다.

### 데이터 구조

- [ ] 모든 데이터 시트의 첫 번째 행이 정확한 영문 헤더다.
- [ ] 임의 컬럼이 추가되거나 필수 컬럼이 누락되지 않았다.
- [ ] 하나의 셀에 여러 선택지, 목표, 보상을 합쳐 쓰지 않았다.
- [ ] 모든 ID 참조가 실제 대상 행과 연결된다.
- [ ] 중복 고유 키가 없다.
- [ ] Graph의 시작 노드와 다음 노드가 존재한다.
- [ ] Quest 액션이 실제 Quest ID를 사용한다.
- [ ] Item 보상과 소모 수량이 1 이상이다.

### 기획 편의성

- [ ] README가 비개발자도 이해할 수 있는 한국어로 작성되었다.
- [ ] Enum과 Boolean에 드롭다운이 적용되었다.
- [ ] 필수 셀과 중복 ID에 조건부 서식이 적용되었다.
- [ ] 각 헤더에 한국어 설명 메모가 있다.
- [ ] 정상 예시와 잘못된 예시가 명확히 분리되었다.

### CSV 품질

- [ ] CSV가 UTF-8로 저장되었다.
- [ ] 쉼표가 포함된 문장이 올바르게 따옴표 처리되었다.
- [ ] 줄바꿈 표기가 `\n`으로 유지되었다.
- [ ] 수식 결과가 아니라 일반 값이 들어 있다.
- [ ] 불필요한 빈 행과 빈 열이 없다.

---

## 16. 다른 AI에 전달할 최종 생성 프롬프트

아래 프롬프트와 이 명세 문서 전체를 함께 전달한다.

```text
첨부한 `Ruin Conservator Dialogue & Quest CSV 교보재 생성 명세`를 유일한 데이터 스키마 기준으로 사용해 주세요.

목표는 게임 기획자가 Excel에서 대화와 퀘스트를 작성한 뒤, 각 시트를 Unity 게임용 UTF-8 CSV로 내보낼 수 있는 실무용 교보재를 만드는 것입니다.

다음 결과물을 생성해 주세요.

1. `RuinConservator_CSV_Authoring_Guide.xlsx`
2. 명세에서 CSV 내보내기 대상으로 지정한 각 시트의 UTF-8 CSV 예시 파일
3. 기획자가 처음부터 따라 할 수 있는 한국어 README 시트
4. 정상 데이터 예시와 별도의 InvalidExamples 시트

중요 요구사항:

- 명세의 시트 이름, CSV 파일 이름, 영문 헤더, 컬럼 순서를 임의로 변경하지 마세요.
- 하나의 셀에 여러 선택지, 목표, 보상을 합치지 마세요.
- 모든 샘플 ID 참조가 실제 대상 데이터와 연결되게 만드세요.
- 데이터 시트의 첫 행은 영문 헤더여야 하며 그 위에 제목이나 설명 행을 추가하지 마세요.
- 설명은 README, 헤더 메모, InvalidExamples 시트에 배치하세요.
- Enum과 Boolean 컬럼에는 CodeLists 기반 드롭다운을 적용하세요.
- 필수 셀 누락과 중복 ID를 표시하는 조건부 서식을 적용하세요.
- 데이터 범위는 Excel 표로 만들고 첫 행 고정과 필터를 적용하세요.
- 수식, 매크로, 셀 병합, 숨김 데이터는 사용하지 마세요.
- 한국어 문장이 쉼표를 포함하면 CSV 규칙에 맞게 큰따옴표 처리하세요.
- 대사 줄바꿈은 실제 셀 줄바꿈 대신 `\n` 문자열을 사용하세요.
- `.xlsx`를 만든 뒤 각 데이터 시트를 실제 UTF-8 CSV로 내보내고, 파일 내용을 다시 읽어 헤더와 행 수를 검증하세요.

교보재의 샘플 시나리오는 명세의 `손상된 벽화 조사` 퀘스트와 큐레이터 대화를 사용하세요. 결과물 생성이 끝나면 생성된 파일 목록, 시트별 행 수, 적용한 데이터 유효성 검사, 발견한 제약 사항을 요약해 주세요.
```

---

## 17. 구현 측과 합의가 필요한 항목

이 문서의 스키마를 실제 게임 코드에 연결하기 전에 개발자와 기획자가 다음 항목을 최종 합의해야 한다.

1. CSV 로더가 RFC 4180의 따옴표와 쉼표 규칙을 완전히 지원하는가
2. `\n` 문자열을 실제 줄바꿈으로 변환하는가
3. ID 비교가 대소문자를 구분하는가
4. 누락된 Localization 번역의 fallback 언어가 무엇인가
5. `sprite_id`, `background_id`, `item_id`를 어느 에셋 저장소에서 해석하는가
6. CSV 검증 실패 시 게임 실행을 막을지, 오류 행만 건너뛸지
7. NPC 대화 라우팅도 CSV로 관리할지, Scene 컴포넌트에는 문자열 ID만 둘지
8. 출시 후 CSV를 앱 업데이트에 포함할지, 외부 패치 파일로 내려받을지

이 합의가 끝난 뒤 컬럼 이름을 확정하고, 확정된 스키마는 코드와 교보재에서 동시에 버전 관리한다.
