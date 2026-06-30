# Dialogue + Quest 제작 가이드

이 문서는 기획자가 Unity 인스펙터에서 대화와 퀘스트를 직접 만들 수 있도록 정리한 작업 매뉴얼입니다.

목표는 단순합니다.

- NPC와 대화할 수 있게 만든다.
- 대화 중 선택지를 띄운다.
- 선택지로 퀘스트를 수락, 진행, 완료, 보상 수령하게 만든다.
- 퀘스트 상태에 따라 NPC 대화를 바꾼다.
- 대사 상황에 맞는 초상화를 출력한다.

---

## 1. 전체 구조 이해하기

대화와 퀘스트는 크게 4가지 데이터로 만듭니다.

| 데이터 | 역할 |
| --- | --- |
| `DialogueActorSO` | 대화에 등장하는 화자 정보입니다. 이름, 기본 초상화, 표정별 초상화 목록을 가집니다. |
| `DialogueGraphSO` | 실제 대화 내용입니다. 여러 개의 노드, 선택지, 액션을 가집니다. |
| `QuestDataSO` | 퀘스트 정보입니다. 제목, 설명, 목표, 보상을 가집니다. |
| `NpcDialogueInteractable` | NPC 오브젝트에 붙는 컴포넌트입니다. 어떤 대화를 실행할지 연결합니다. |

플레이 중 흐름은 이렇게 됩니다.

```text
플레이어가 NPC에게 상호작용
-> NpcDialogueInteractable이 현재 퀘스트 상태를 확인
-> 사용할 DialogueGraphSO 선택
-> DialogueController가 대화 UI 출력
-> 선택지를 누르면 Progress Flag 또는 Quest 상태 변경
-> QuestController가 목표 완료/보상 상태 갱신
```

---

## 2. ID 작성 규칙

이 시스템은 여러 데이터를 ID 문자열로 연결합니다. ID가 한 글자라도 다르면 연결되지 않습니다.

권장 규칙은 다음과 같습니다.

```text
영어 소문자 + 언더바
```

좋은 예:

```text
elder_intro
quest_find_relic_piece
talked_to_elder
found_relic_piece
normal
happy
worried
```

피해야 할 예:

```text
장로대화
Quest Find Relic
FindRelicPiece
found relic piece
```

중요한 규칙:

- `Quest Id`는 전체 게임에서 중복되면 안 됩니다.
- `Node Id`는 같은 `DialogueGraphSO` 안에서 중복되면 안 됩니다.
- `Progress Flag Id`는 쓰는 곳과 검사하는 곳의 철자가 완전히 같아야 합니다.
- 한 번 사용한 ID는 가능하면 바꾸지 마세요. 이미 연결된 대화/퀘스트가 끊길 수 있습니다.

---

## 3. DialogueActorSO 만들기

`DialogueActorSO`는 화자 데이터입니다.

예를 들어 장로 NPC를 만들려면:

```text
Create > Ruin Conservator > Dialogue > Actor
```

파일 이름 예:

```text
ElderDialogueActor.asset
```

필드 설명:

| 필드 | 설명 |
| --- | --- |
| `Actor Id` | 화자의 고유 ID입니다. 예: `elder` |
| `Display Name` | 대화창에 표시될 이름입니다. 예: `장로` |
| `Portrait` | 기본 초상화입니다. 노드에서 별도 초상화를 선택하지 않으면 이것이 나옵니다. |
| `Portraits` | 상황별 초상화 목록입니다. 노드에서 드롭다운으로 선택할 수 있습니다. |

`Portraits` 예:

```text
Element 0
Portrait Id: normal
Display Name: 평상시
Portrait: 장로_기본.png

Element 1
Portrait Id: happy
Display Name: 기쁨
Portrait: 장로_웃음.png

Element 2
Portrait Id: worried
Display Name: 걱정
Portrait: 장로_걱정.png
```

나중에 `DialogueGraphSO`의 각 노드에서 `Portrait Id`를 고르면 해당 초상화가 출력됩니다.

`Portrait Id`를 `Default`로 두면 `DialogueActorSO`의 기본 `Portrait`가 출력됩니다.

---

## 4. DialogueGraphSO 만들기

`DialogueGraphSO`는 NPC가 말할 실제 대화 묶음입니다.

생성 방법:

```text
Create > Ruin Conservator > Dialogue > Graph
```

파일 이름 예:

```text
ElderQuestStartDialogue.asset
```

상단 필드:

| 필드 | 설명 |
| --- | --- |
| `Graph Id` | 대화 묶음의 고유 ID입니다. 예: `elder_quest_start` |
| `Default Actor` | 이 대화에서 기본으로 사용할 화자입니다. 보통 NPC의 `DialogueActorSO`를 넣습니다. |
| `Start Node Id` | 대화가 시작될 노드 ID입니다. 보통 `start`를 사용합니다. |
| `Nodes` | 실제 대사 노드 목록입니다. |

---

## 5. Dialogue Node 만들기

`Nodes`에 Element를 추가하면 대사 한 칸이 생깁니다.

노드 필드:

| 필드 | 설명 |
| --- | --- |
| `Node Id` | 이 노드의 ID입니다. 예: `start`, `ask_accept`, `accept_end` |
| `Actor Override` | 이 노드에서만 다른 화자를 쓰고 싶을 때 넣습니다. 비워두면 `Default Actor`를 씁니다. |
| `Portrait Id` | 이 노드에서 사용할 초상화입니다. `DialogueActorSO`의 `Portraits` 목록에서 고릅니다. |
| `Body Text` | 화면에 출력할 대사 본문입니다. |
| `Next Node Id` | 선택지가 없을 때 클릭으로 이동할 다음 노드 ID입니다. 비워두면 대화가 끝납니다. |
| `Choices` | 선택지 목록입니다. 하나 이상 있으면 클릭으로 다음 대사로 넘어가지 않고 선택지 버튼을 눌러야 합니다. |

가장 단순한 대화 예:

```text
Node 0
Node Id: start
Portrait Id: normal
Body Text: 어서 오게. 유적을 조사하러 온 건가?
Next Node Id: ask_help
Choices: 비움

Node 1
Node Id: ask_help
Portrait Id: worried
Body Text: 마을 밖에서 이상한 석편을 봤네. 확인해줄 수 있겠나?
Next Node Id:
Choices: 있음
```

동작:

- `start` 노드는 클릭하면 `ask_help`로 이동합니다.
- `ask_help` 노드는 선택지가 있으면 선택지 버튼이 나타납니다.

---

## 6. Choices 만들기

선택지는 `DialogueNodeData` 안의 `Choices` 리스트에 추가합니다.

선택지 필드:

| 필드 | 설명 |
| --- | --- |
| `Choice Text` | 버튼에 표시될 문장입니다. |
| `Next Node Id` | 선택 후 이동할 다음 노드 ID입니다. 비워두면 액션 실행 후 대화가 끝납니다. |
| `Action Type` | 선택지를 눌렀을 때 실행할 기능입니다. |
| `Action Id` | 액션에서 사용할 ID입니다. Progress Flag 액션이면 flag ID, Quest 액션이면 quest ID로 씁니다. |
| `Quest Data` | Quest 액션에서 직접 사용할 `QuestDataSO`입니다. 가능하면 이 칸에 퀘스트 에셋을 직접 넣는 것을 추천합니다. |

선택지가 있는 노드는 중요하게 기억해야 합니다.

```text
Choices가 비어 있음
-> 화면 클릭으로 Next Node Id로 이동

Choices가 하나 이상 있음
-> 화면 클릭으로 넘어가지 않음
-> 선택지 버튼을 눌러야 이동
```

---

## 7. Action Type 설명

선택지의 `Action Type`은 대화와 퀘스트를 연결하는 핵심입니다.

| Action Type | 하는 일 | 주로 쓰는 상황 |
| --- | --- | --- |
| `NONE` | 아무 액션도 하지 않습니다. | 그냥 다음 대사로 이동 |
| `SET_PROGRESS_FLAG_TRUE` | `Action Id`에 적은 Progress Flag를 `true`로 저장합니다. | 목표 달성 처리 |
| `SET_PROGRESS_FLAG_FALSE` | `Action Id`에 적은 Progress Flag를 `false`로 저장합니다. | 조건 초기화, 선택 취소 |
| `ACCEPT_QUEST` | 퀘스트를 수락 상태로 바꿉니다. | "도와줄게" 선택지 |
| `REFRESH_QUEST_PROGRESS` | 퀘스트 목표 달성 여부를 다시 검사합니다. | 목표 조건을 확인시키고 싶을 때 |
| `COMPLETE_QUEST` | 목표 조건이 만족된 퀘스트를 완료 상태로 바꿉니다. | 완료 보고 선택지 |
| `CLAIM_QUEST_REWARD` | 완료된 퀘스트의 보상을 지급하고 보상 수령 상태로 바꿉니다. | "보상을 받는다" 선택지 |
| `END_DIALOGUE` | 즉시 대화를 종료합니다. | "나중에 올게" 선택지 |

`Quest Data`와 `Action Id`의 관계:

- `Quest Data`에 퀘스트 에셋을 넣으면 그것을 우선 사용합니다.
- `Quest Data`가 비어 있으면 `Action Id`를 `Quest Id`로 사용합니다.
- 기획 작업에서는 실수를 줄이기 위해 `Quest Data` 직접 연결을 추천합니다.

---

## 8. QuestDataSO 만들기

퀘스트 에셋 생성:

```text
Create > Ruin Conservator > Quest > Quest Data
```

파일 이름 예:

```text
Quest_FindRelicPiece.asset
```

퀘스트 기본 필드:

| 필드 | 설명 |
| --- | --- |
| `Quest Id` | 퀘스트 고유 ID입니다. 예: `find_relic_piece` |
| `Title` | 퀘스트 제목입니다. 예: `사라진 석편` |
| `Description` | 퀘스트 설명입니다. |
| `Owner Npc Id` | 퀘스트를 주는 NPC ID입니다. 예: `elder` |
| `Objectives` | 퀘스트 완료 조건 목록입니다. |
| `Rewards` | 퀘스트 보상 목록입니다. |

---

## 9. Objective 만들기

퀘스트 목표는 `Progress Flag`로 판단합니다.

목표 필드:

| 필드 | 설명 |
| --- | --- |
| `Objective Id` | 목표 고유 ID입니다. 예: `talk_to_elder` |
| `Description` | 퀘스트 창에 표시할 목표 설명입니다. |
| `Required Progress Flag Id` | 완료 여부를 검사할 Progress Flag ID입니다. |
| `Required Flag Value` | 이 값이 되어야 목표 완료로 인정합니다. 보통 `true`입니다. |

예:

```text
Objective Id: find_piece
Description: 마을 밖에서 이상한 석편을 찾기
Required Progress Flag Id: found_relic_piece
Required Flag Value: true
```

뜻:

```text
found_relic_piece 플래그가 true가 되면 이 목표는 완료된다.
```

중요:

- `Required Progress Flag Id`만 적는다고 자동 완료되지 않습니다.
- 어딘가에서 같은 이름의 flag를 `true`로 바꿔야 합니다.
- 현재 대화 선택지에서 flag를 바꾸려면 `SET_PROGRESS_FLAG_TRUE`를 사용합니다.

예:

```text
Choice Text: 석편을 찾았어.
Action Type: SET_PROGRESS_FLAG_TRUE
Action Id: found_relic_piece
Next Node Id: report_found
```

이 선택지를 누르면:

```text
found_relic_piece = true
```

가 저장되고, 진행 중인 퀘스트 목표가 다시 검사됩니다.

---

## 10. Reward 만들기

보상도 현재는 `Progress Flag` 기반으로 처리합니다.

보상 필드:

| 필드 | 설명 |
| --- | --- |
| `Reward Id` | 보상 고유 ID입니다. 예: `elder_trust` |
| `Description` | 퀘스트 창에 표시할 보상 설명입니다. |
| `Reward Progress Flag Id` | 보상을 받았을 때 변경할 flag ID입니다. 비워두면 설명용 보상으로만 사용됩니다. |
| `Reward Flag Value` | 저장할 flag 값입니다. 보통 `true`입니다. |

예:

```text
Reward Id: elder_trust
Description: 장로의 신뢰를 얻었다.
Reward Progress Flag Id: elder_trust_gained
Reward Flag Value: true
```

보상을 받으면:

```text
elder_trust_gained = true
```

가 저장됩니다.

---

## 11. QuestController에 퀘스트 등록하기

퀘스트를 만들었으면 GameRoot의 `QuestController`에 등록해야 합니다.

작업:

```text
GameRoot 프리팹 또는 씬의 GameRoot 선택
-> QuestController 찾기
-> Quest Database 리스트에 QuestDataSO 추가
```

권장:

- 모든 `QuestDataSO`는 `Quest Database`에 등록하세요.
- 대화 선택지에서 `Quest Data`를 직접 연결해도 동작할 수 있지만, 상태 조회와 유지 관리를 위해 데이터베이스에도 넣어두는 편이 안전합니다.

---

## 12. NPC에 대화 연결하기

NPC 오브젝트에는 `NpcDialogueInteractable` 컴포넌트가 필요합니다.

필드:

| 필드 | 설명 |
| --- | --- |
| `Dialogue Graph` | 기본 대화입니다. 퀘스트 조건에 맞는 대화가 없으면 이것을 사용합니다. |
| `Actor Override` | 이 NPC가 사용할 기본 화자입니다. 비워두면 `DialogueGraphSO`의 `Default Actor`를 사용합니다. |
| `Dialogue Controller` | 직접 연결할 DialogueController입니다. 보통 비워두면 `GameRoot.Dialogue`를 사용합니다. |
| `Quest Dialogue Routes` | 퀘스트 상태에 따라 다른 대화를 보여주는 목록입니다. |
| `Highlight Object` | 상호작용 가능할 때 켜질 하이라이트 오브젝트입니다. |
| `Hide Highlight On Awake` | 시작할 때 하이라이트를 꺼둘지 여부입니다. |

가장 단순한 연결:

```text
Dialogue Graph: ElderDefaultDialogue
Actor Override: ElderDialogueActor
Quest Dialogue Routes: 비움
```

이렇게 하면 NPC에게 말을 걸 때 항상 `ElderDefaultDialogue`이 실행됩니다.

---

## 13. 퀘스트 상태별 NPC 대화 바꾸기

퀘스트 상태에 따라 NPC 대화를 바꾸려면 `Quest Dialogue Routes`를 사용합니다.

`Quest Dialogue Routes`의 각 Element:

| 필드 | 설명 |
| --- | --- |
| `Quest Data` | 상태를 확인할 퀘스트 에셋입니다. 추천 방식입니다. |
| `Quest Id` | `Quest Data`가 비어 있을 때 사용할 퀘스트 ID입니다. |
| `Quest State` | 이 상태일 때 사용할 대화 조건입니다. |
| `Dialogue Graph` | 조건이 맞으면 실행할 대화입니다. |

Quest State:

| 상태 | 의미 |
| --- | --- |
| `QUEST_NOT_STARTED` | 아직 수락하지 않음 |
| `QUEST_IN_PROGRESS` | 수락했고 진행 중 |
| `QUEST_COMPLETED` | 목표는 완료했지만 보상은 아직 받지 않음 |
| `QUEST_REWARDED` | 보상까지 받음 |

예:

```text
Quest Dialogue Routes

Element 0
Quest Data: Quest_FindRelicPiece
Quest State: QUEST_COMPLETED
Dialogue Graph: ElderQuestCompleteDialogue

Element 1
Quest Data: Quest_FindRelicPiece
Quest State: QUEST_IN_PROGRESS
Dialogue Graph: ElderQuestInProgressDialogue

Element 2
Quest Data: Quest_FindRelicPiece
Quest State: QUEST_NOT_STARTED
Dialogue Graph: ElderQuestStartDialogue
```

중요:

- 리스트는 위에서 아래로 검사합니다.
- 더 구체적으로 보여주고 싶은 상태를 위쪽에 두세요.
- 조건이 하나도 맞지 않으면 기본 `Dialogue Graph`를 사용합니다.

---

## 14. 실전 예시: "사라진 석편" 퀘스트 만들기

목표:

```text
장로가 퀘스트를 준다.
플레이어가 석편을 찾았다고 보고한다.
퀘스트가 완료된다.
장로에게 보상을 받는다.
보상 후에는 다른 대사를 한다.
```

### 14-1. Actor 만들기

`ElderDialogueActor.asset`

```text
Actor Id: elder
Display Name: 장로
Portrait: 장로 기본 초상화

Portraits:
  normal / 평상시 / 장로 기본
  worried / 걱정 / 장로 걱정
  happy / 기쁨 / 장로 웃음
```

### 14-2. Quest 만들기

`Quest_FindRelicPiece.asset`

```text
Quest Id: find_relic_piece
Title: 사라진 석편
Description: 마을 밖에서 사라진 석편을 찾아 장로에게 보고하자.
Owner Npc Id: elder
```

Objectives:

```text
Element 0
Objective Id: find_piece
Description: 마을 밖에서 석편 찾기
Required Progress Flag Id: found_relic_piece
Required Flag Value: true
```

Rewards:

```text
Element 0
Reward Id: elder_trust
Description: 장로의 신뢰
Reward Progress Flag Id: elder_trust_gained
Reward Flag Value: true
```

### 14-3. 퀘스트 시작 대화 만들기

`ElderQuestStartDialogue.asset`

Graph:

```text
Graph Id: elder_find_relic_start
Default Actor: ElderDialogueActor
Start Node Id: start
```

Node 0:

```text
Node Id: start
Portrait Id: worried
Body Text: 마을 밖에서 오래된 석편이 사라졌네. 자네가 확인해줄 수 있겠나?
Next Node Id:
Choices:
  Element 0:
    Choice Text: 좋아, 내가 찾아볼게.
    Next Node Id: accepted
    Action Type: ACCEPT_QUEST
    Action Id: 비워도 됨
    Quest Data: Quest_FindRelicPiece

  Element 1:
    Choice Text: 지금은 어려울 것 같아.
    Next Node Id:
    Action Type: END_DIALOGUE
```

Node 1:

```text
Node Id: accepted
Portrait Id: normal
Body Text: 고맙네. 석편을 찾으면 다시 와주게.
Next Node Id:
Choices: 비움
```

### 14-4. 진행 중 대화 만들기

`ElderQuestInProgressDialogue.asset`

```text
Graph Id: elder_find_relic_in_progress
Default Actor: ElderDialogueActor
Start Node Id: start
```

Node 0:

```text
Node Id: start
Portrait Id: worried
Body Text: 석편은 아직 찾지 못했나? 마을 밖 유적 근처를 살펴보게.
Next Node Id:
Choices: 비움
```

### 14-5. 목표 완료 처리 대화 만들기

플레이어가 석편을 찾는 방식이 아직 별도 오브젝트가 아니라 대화 선택지라면, 테스트용으로 이렇게 만들 수 있습니다.

```text
Choice Text: 석편을 찾았어.
Action Type: SET_PROGRESS_FLAG_TRUE
Action Id: found_relic_piece
Next Node Id: found_report
```

이 선택지를 누르면 퀘스트 목표 조건인 `found_relic_piece`가 `true`가 됩니다.

다른 오브젝트나 상호작용에서 이 flag를 켜는 기능이 생기면, 그쪽에서 같은 ID를 사용하면 됩니다.

### 14-6. 완료 대화 만들기

`ElderQuestCompleteDialogue.asset`

Node 0:

```text
Node Id: start
Portrait Id: happy
Body Text: 정말 석편을 찾아왔군. 큰일을 해냈네.
Next Node Id:
Choices:
  Element 0:
    Choice Text: 보상을 받을게.
    Next Node Id: reward_done
    Action Type: CLAIM_QUEST_REWARD
    Quest Data: Quest_FindRelicPiece
```

Node 1:

```text
Node Id: reward_done
Portrait Id: happy
Body Text: 자네 덕분에 한시름 놓았네.
Next Node Id:
Choices: 비움
```

### 14-7. 보상 후 대화 만들기

`ElderQuestRewardedDialogue.asset`

```text
Node Id: start
Portrait Id: normal
Body Text: 석편은 잘 보관하고 있네. 다시 한 번 고맙네.
Next Node Id:
Choices: 비움
```

### 14-8. NPC Route 설정

장로 NPC의 `NpcDialogueInteractable`에서:

```text
Dialogue Graph: ElderQuestStartDialogue
Actor Override: ElderDialogueActor
```

Quest Dialogue Routes:

```text
Element 0
Quest Data: Quest_FindRelicPiece
Quest State: QUEST_REWARDED
Dialogue Graph: ElderQuestRewardedDialogue

Element 1
Quest Data: Quest_FindRelicPiece
Quest State: QUEST_COMPLETED
Dialogue Graph: ElderQuestCompleteDialogue

Element 2
Quest Data: Quest_FindRelicPiece
Quest State: QUEST_IN_PROGRESS
Dialogue Graph: ElderQuestInProgressDialogue

Element 3
Quest Data: Quest_FindRelicPiece
Quest State: QUEST_NOT_STARTED
Dialogue Graph: ElderQuestStartDialogue
```

이제 상태별 흐름은 다음처럼 됩니다.

```text
처음 말 걸기
-> ElderQuestStartDialogue
-> ACCEPT_QUEST 선택
-> QUEST_IN_PROGRESS

다시 말 걸기
-> ElderQuestInProgressDialogue

found_relic_piece가 true가 됨
-> QUEST_COMPLETED

다시 말 걸기
-> ElderQuestCompleteDialogue
-> CLAIM_QUEST_REWARD 선택
-> QUEST_REWARDED

다시 말 걸기
-> ElderQuestRewardedDialogue
```

---

## 15. 대화/퀘스트 연결 패턴

### 패턴 A: 그냥 대화만 하는 NPC

```text
NpcDialogueInteractable
Dialogue Graph: 일반 대화 그래프
Quest Dialogue Routes: 비움
```

선택지 없이 노드끼리 `Next Node Id`로 연결하면 됩니다.

### 패턴 B: 선택지로 퀘스트 수락

선택지:

```text
Choice Text: 도와줄게.
Action Type: ACCEPT_QUEST
Quest Data: 받을 퀘스트
Next Node Id: accepted
```

### 패턴 C: 선택지로 목표 완료 flag 켜기

선택지:

```text
Choice Text: 장로와 대화를 마쳤다.
Action Type: SET_PROGRESS_FLAG_TRUE
Action Id: talked_to_elder
Next Node Id: done
```

퀘스트 목표:

```text
Required Progress Flag Id: talked_to_elder
Required Flag Value: true
```

### 패턴 D: 완료된 퀘스트 보상 받기

선택지:

```text
Choice Text: 보상을 받을게.
Action Type: CLAIM_QUEST_REWARD
Quest Data: 해당 퀘스트
Next Node Id: reward_done
```

### 패턴 E: 선택지만 보여주는 노드

퀘스트 수락처럼 버튼만 보여주고 싶다면:

```text
Body Text: 비워둠
Choices: 원하는 선택지 추가
```

이 노드는 선택지 버튼을 눌러야 진행됩니다.

---

## 16. 검수 체크리스트

대화나 퀘스트를 만든 뒤 아래를 확인하세요.

### DialogueActorSO

- `Actor Id`가 비어 있지 않은가?
- `Display Name`이 의도한 이름인가?
- 기본 `Portrait`가 들어가 있는가?
- `Portraits`의 `Portrait Id`가 중복되지 않는가?
- 각 `Portraits` 항목에 Sprite가 들어가 있는가?

### DialogueGraphSO

- `Graph Id`가 비어 있지 않은가?
- `Default Actor`가 연결되어 있는가?
- `Start Node Id`와 실제 시작 노드의 `Node Id`가 같은가?
- 모든 `Node Id`가 중복되지 않는가?
- `Next Node Id`가 실제 존재하는 노드 ID인가?
- 선택지가 있는 노드에서 의도대로 `Next Node Id`와 `Action Type`을 넣었는가?
- 선택지의 `Quest Data` 또는 `Action Id`가 올바른가?

### QuestDataSO

- `Quest Id`가 비어 있지 않고 중복되지 않는가?
- `Objectives`가 최소 1개 이상 있는가?
- `Required Progress Flag Id`가 실제로 켜지는 flag와 철자가 같은가?
- `Required Flag Value`가 보통 `true`로 되어 있는가?
- 보상이 필요하다면 `Rewards`에 `Reward Progress Flag Id`를 넣었는가?

### NPC

- NPC에 `NpcDialogueInteractable`이 붙어 있는가?
- 기본 `Dialogue Graph`가 연결되어 있는가?
- `Actor Override` 또는 Graph의 `Default Actor`가 연결되어 있는가?
- 퀘스트 상태별 대화를 쓴다면 `Quest Dialogue Routes`가 설정되어 있는가?
- Route의 `Quest Data`, `Quest State`, `Dialogue Graph`가 모두 들어가 있는가?
- Route 순서가 의도한 우선순위인가?

---

## 17. 자주 생기는 문제

### 대화가 시작되지 않는다

확인할 것:

- NPC에 `NpcDialogueInteractable`이 있는가?
- NPC가 플레이어 상호작용 범위와 레이어에 들어오는가?
- `Dialogue Graph`가 비어 있지 않은가?
- `DialogueGraphSO` 안에 시작 노드가 있는가?
- `Start Node Id`가 실제 `Node Id`와 일치하는가?

### 클릭해도 다음 대사로 넘어가지 않는다

확인할 것:

- 현재 노드에 `Choices`가 있으면 클릭으로 넘어가지 않습니다. 선택지 버튼을 눌러야 합니다.
- 선택지가 없는 노드라면 `Next Node Id`가 실제 노드 ID와 같은지 확인하세요.
- `Next Node Id`가 비어 있으면 대화가 끝나는 것이 정상입니다.

### 선택지 버튼이 안 보인다

확인할 것:

- `DialogueView`에 `Choice Root`가 연결되어 있는가?
- `DialogueView`에 `Choice Button Prefab`이 연결되어 있는가?
- 버튼 프리팹 안에 `Button` 컴포넌트와 `TMP_Text`가 있는가?
- 해당 노드의 `Choices` 리스트에 Element가 실제로 들어가 있는가?

### 퀘스트가 수락되지 않는다

확인할 것:

- 선택지 `Action Type`이 `ACCEPT_QUEST`인가?
- `Quest Data`가 연결되어 있는가?
- `Quest Data`가 비어 있다면 `Action Id`가 정확한 `Quest Id`인가?
- `Quest Id`가 비어 있지 않은가?
- 이미 수락한 퀘스트를 다시 수락하려는 것은 아닌가?

### 퀘스트 목표가 완료되지 않는다

확인할 것:

- 퀘스트가 `QUEST_IN_PROGRESS` 상태인가?
- `Objective`의 `Required Progress Flag Id`와 실제로 켠 flag ID가 완전히 같은가?
- `Required Flag Value`가 의도한 값인가?
- `SET_PROGRESS_FLAG_TRUE` 선택지를 눌렀는가?
- 목표가 여러 개라면 모든 목표가 만족되었는가?

### 퀘스트 완료 대화로 바뀌지 않는다

확인할 것:

- `Quest Dialogue Routes`에 `QUEST_COMPLETED` Route가 있는가?
- Route의 `Quest Data`가 올바른 퀘스트인가?
- 목표 완료 후 퀘스트 상태가 실제로 `QUEST_COMPLETED`가 되었는가?
- Route 순서가 의도대로 되어 있는가?

### 초상화가 원하는 것으로 나오지 않는다

확인할 것:

- `DialogueActorSO`의 `Portraits`에 해당 `Portrait Id`가 있는가?
- 해당 `Portraits` Element에 Sprite가 들어가 있는가?
- 노드의 `Portrait Id`가 올바르게 선택되어 있는가?
- 노드에 `Actor Override`를 넣었다면, 그 Actor의 `Portraits` 목록에 해당 ID가 있는가?
- `Portrait Id`를 `Default`로 두면 기본 `Portrait`가 나오는 것이 정상입니다.

---

## 18. 추천 작업 순서

새 NPC 퀘스트 대화를 만들 때는 아래 순서로 작업하면 실수가 적습니다.

```text
1. DialogueActorSO 만들기
2. Actor에 기본 초상화와 상황별 Portraits 등록
3. QuestDataSO 만들기
4. Quest Id, 목표 flag, 보상 flag 정하기
5. QuestController의 Quest Database에 QuestDataSO 등록
6. 대화 Graph 만들기
7. start 노드부터 Node Id와 Next Node Id 연결
8. 선택지와 Action Type 연결
9. NPC의 NpcDialogueInteractable에 기본 Dialogue Graph 연결
10. Quest Dialogue Routes로 상태별 대화 연결
11. 플레이해서 각 상태를 검수
```

이 순서만 지키면 대부분의 대화/퀘스트는 코드 수정 없이 만들 수 있습니다.
