# Inventory System 제작 가이드

이 문서는 기획자와 개발자가 인벤토리 아이템, 퀘스트 보상, 자동 소모, 인벤토리 UI를 같은 기준으로 만들 수 있도록 정리한 작업 매뉴얼입니다.

목표는 단순합니다.

- 아이템 원본 데이터를 만든다.
- 아이템을 타입과 카테고리 규칙에 맞게 분류한다.
- GameRoot의 InventoryController에 아이템 DB를 등록한다.
- 퀘스트 보상으로 아이템을 지급한다.
- 퀘스트 완료 조건에서 필요한 아이템을 자동 소모한다.
- 인벤토리 UI에서 슬롯, 상세 정보, new 표시, 수동 배치 순서를 확인한다.

참고:

- 인벤토리는 현재 `Tab` 키로 열고 닫을 수 있습니다.
- 인벤토리, 도감, 지도 사이를 화살표로 전환하는 구조는 추후 도감/지도 UI가 생기면 별도 문서로 다시 작성합니다.

---

## 1. 전체 구조 이해하기

인벤토리 시스템은 크게 8가지 코드로 나뉩니다.

| 코드 | 역할 |
| --- | --- |
| `ItemType` | 아이템의 용도별 타입입니다. 퀘스트, 술, 유물, 기타를 구분합니다. |
| `InventoryCategory` | 인벤토리 UI 카테고리입니다. 전체, 술, 유물, 기타를 구분합니다. |
| `ItemDataSO` | 아이템 원본 데이터입니다. 이름, 설명, 아이콘, 가격, 타입, 최대 중첩 수를 가집니다. |
| `InventorySlotData` | 런타임 슬롯 저장 데이터입니다. `ItemId`, 수량, new 여부, 수동 배치 순서를 가집니다. |
| `IInventoryService` | 외부에서 인벤토리에 접근하는 계약입니다. |
| `InventoryController` | 아이템 추가, 소모, 조회, 정렬, 슬롯 이동을 담당합니다. |
| `InventoryView` / `InventorySlotView` | 인벤토리 UI 표시만 담당합니다. |
| `InventoryPresenter` | UI 열기/닫기, 카테고리 전환, 슬롯 선택, 드래그 입력만 담당합니다. |

플레이 중 흐름은 이렇게 됩니다.

```text
GameRoot가 InventoryController 초기화
-> GameRoot가 InventoryPresenter에 IInventoryService 주입
-> 퀘스트 보상 또는 테스트 코드가 TryAddItem 호출
-> InventoryController가 ItemId 기준으로 슬롯 생성/중첩
-> 플레이어가 Tab 또는 HUD 버튼으로 인벤토리 열기
-> InventoryPresenter가 슬롯 목록을 조회
-> InventoryView가 슬롯 6x5와 선택 아이템 상세 정보 출력
```

중요한 구조:

```text
저장 데이터
-> ItemId 중심
-> ItemDataSO 직접 참조 저장 금지

표시 데이터
-> ItemId로 ItemDataSO를 다시 조회
-> 이름, 설명, 아이콘, 가격 출력
```

---

## 2. ID 작성 규칙

이 시스템은 아이템을 `ItemId` 문자열로 저장하고 조회합니다. `ItemId`가 한 글자라도 다르면 같은 아이템으로 인식되지 않습니다.

권장 규칙은 다음과 같습니다.

```text
영어 소문자 + 언더바
```

좋은 예:

```text
quest_old_key
alcohol_rice_wine
tool_brewing_pot
relic_broken_tablet
tool_small_brush
misc_coin
```

피해야 할 예:

```text
낡은열쇠
Old Key
old key
OldKey
```

중요한 규칙:

- `ItemId`는 전체 아이템 DB에서 중복되면 안 됩니다.
- 한 번 저장에 사용한 `ItemId`는 가능하면 바꾸지 마세요.
- `ItemId`가 비어 있으면 인벤토리 DB 등록, 지급, 소모가 실패합니다.
- 저장/로드는 `ItemId`를 기준으로 해야 하며, `ItemDataSO` 참조를 저장 데이터에 직접 넣지 않습니다.

---

## 3. ItemType 이해하기

`ItemType`은 아이템의 용도별 타입입니다.

코드 enum 값은 0부터 시작하지만, 문서 기준 번호는 1부터 설명합니다.

| 문서 번호 | enum 값 | 의미 | 카테고리 |
| --- | --- | --- | --- |
| 1 | `ITEM_QUEST = 0` | 퀘스트 진행, NPC 전달, 조건 확인용 아이템 | 전체에만 표시 |
| 2 | `ITEM_ALCOHOL_COLLECTIBLE = 1` | 술 수집형 아이템 | 전체, 술 |
| 3 | `ITEM_ALCOHOL_TOOL = 2` | 술 제조 도구 아이템 | 전체, 술 |
| 4 | `ITEM_RELIC_COLLECTIBLE = 3` | 유물 수집형 아이템 | 전체, 유물 |
| 5 | `ITEM_RELIC_TOOL = 4` | 유물 발굴 도구 아이템 | 전체, 유물 |
| 6 | `ITEM_MISC = 5` | 기타 아이템 | 전체, 기타 |

주의:

- 퀘스트 아이템은 문서 기준 “전체”에만 포함됩니다.
- 퀘스트 아이템을 별도 탭으로 보여줄지는 나중에 필요할 때 결정합니다.

---

## 4. InventoryCategory 이해하기

인벤토리 UI 카테고리는 다음 규칙으로 필터링합니다.

| 카테고리 | 포함 타입 |
| --- | --- |
| 전체 | 문서 기준 1, 2, 3, 4, 5, 6 |
| 술 | 문서 기준 2, 3 |
| 유물 | 문서 기준 4, 5 |
| 기타 | 문서 기준 6 |

예:

```text
ITEM_QUEST
-> 전체에서만 보임

ITEM_ALCOHOL_TOOL
-> 전체, 술에서 보임

ITEM_RELIC_COLLECTIBLE
-> 전체, 유물에서 보임

ITEM_MISC
-> 전체, 기타에서 보임
```

---

## 5. ItemDataSO 만들기

아이템 에셋 생성:

```text
Create > Ruin Conservator > Inventory > Item Data
```

파일 이름 예:

```text
Item_OldKey.asset
Item_RiceWine.asset
Item_BrewingPot.asset
Item_BrokenTablet.asset
```

필드 설명:

| 필드 | 설명 |
| --- | --- |
| `Item Id` | 저장과 조회에 사용하는 아이템 고유 ID입니다. 필수입니다. |
| `Display Name` | 인벤토리 UI에 표시할 이름입니다. |
| `Description` | 2페이지 상세 영역에 표시할 설명입니다. |
| `Icon` | 슬롯과 상세 영역에 표시할 아이콘입니다. |
| `Price` | 상세 영역에 표시할 가격입니다. 0 이상입니다. |
| `Item Type` | 아이템 타입입니다. 카테고리 필터와 정렬에 사용합니다. |
| `Max Stack Count` | 한 슬롯에 중첩 가능한 최대 수량입니다. 기본값은 문서 기준 99입니다. |

예:

```text
Item Id: quest_old_key
Display Name: 낡은 열쇠
Description: 오래된 창고 문을 열 수 있을 것 같은 열쇠다.
Icon: 낡은 열쇠 아이콘
Price: 0
Item Type: ITEM_QUEST
Max Stack Count: 99
```

예:

```text
Item Id: tool_brewing_pot
Display Name: 양조 냄비
Description: 술 제조에 사용하는 기본 도구다.
Icon: 냄비 아이콘
Price: 120
Item Type: ITEM_ALCOHOL_TOOL
Max Stack Count: 1
```

---

## 6. InventoryController에 아이템 등록하기

아이템 에셋을 만들었으면 GameRoot의 `InventoryController`에 등록해야 합니다.

작업:

```text
GameRoot 프리팹 또는 씬의 GameRoot 선택
-> InventoryController 찾기
-> Item Database 리스트에 ItemDataSO 추가
```

중요:

- 보상으로 지급할 아이템은 가능하면 `Item Database`에 등록하세요.
- UI에서 `ItemId`로 이름, 설명, 아이콘, 가격을 조회하려면 원본 데이터가 등록되어 있어야 합니다.
- 같은 `ItemId`를 가진 다른 `ItemDataSO`를 중복 등록하지 마세요.

---

## 7. 슬롯 저장 데이터 이해하기

`InventorySlotData`는 실제 저장 대상이 되는 런타임 슬롯 데이터입니다.

필드:

| 필드 | 설명 |
| --- | --- |
| `ItemId` | 저장과 조회에 사용하는 아이템 ID입니다. |
| `Quantity` | 현재 슬롯 수량입니다. |
| `IsNewItem` | 최초 획득 후 아직 확인하지 않은 아이템인지 여부입니다. |
| `ManualSlotOrder` | 플레이어가 드래그로 지정한 수동 배치 순서입니다. |

중요:

- `InventorySlotData`에는 `ItemDataSO`나 `Sprite`를 직접 넣지 않습니다.
- 저장 데이터는 가볍게 유지하고, 표시는 `ItemId`로 원본 데이터를 다시 찾습니다.
- `ManualSlotOrder`가 없으면 기본 정렬을 사용합니다.

---

## 8. 아이템 추가 규칙

아이템 추가는 `IInventoryService.TryAddItem(ItemDataSO itemData, int amount)`로 처리합니다.

동작:

```text
itemData 검증
-> ItemId 검증
-> 같은 ItemId 슬롯이 있으면 최대 중첩 수까지 채움
-> 남은 수량이 있으면 새 슬롯 생성
-> 처음 획득한 아이템이면 new 표시
```

예:

```text
Max Stack Count: 99
현재 rice_wine 80개 보유
TryAddItem(rice_wine, 30)

결과:
기존 슬롯 99개
새 슬롯 11개
```

실패하는 경우:

- `itemData`가 비어 있음
- `ItemId`가 비어 있음
- `amount`가 1보다 작음

---

## 9. 정렬 규칙

인벤토리 정렬은 두 단계로 나뉩니다.

### 최초 표시

```text
도구 아이템 우선
-> 그 외 최근 획득순
```

도구 우선순위:

| 우선순위 | 타입 |
| --- | --- |
| 0 | `ITEM_ALCOHOL_TOOL` |
| 1 | `ITEM_RELIC_TOOL` |
| 2 | 그 외 타입 |

### 드래그 이후

플레이어가 슬롯을 드래그로 이동하면 `ManualSlotOrder`가 저장됩니다.

이후 정렬은 다음 기준을 우선합니다.

```text
수동 배치 순서
-> 도구 우선순위
-> 최근 획득순
```

---

## 10. 아이템 자동 소모 규칙

문서 기준 현재 인벤토리에는 수동 드랍/삭제 UI가 없습니다.

아이템 소모는 아래 같은 조건에서만 코드가 호출합니다.

```text
퀘스트 완료
재료 사용
술 제조
유물 발굴
```

현재 구현된 자동 소모:

```text
quest_consume_items.csv의 아이템 소모 정의
-> QuestController.TryCompleteQuest()
-> IInventoryService.TryConsumeItem()
```

중요:

- 수량이 부족하면 예외가 아니라 `false`를 반환합니다.
- 소모 전에 전체 필요 수량을 먼저 검사합니다.
- 같은 아이템이 여러 소모 항목에 들어가면 합산해서 검사합니다.
- 소모에 실패하면 퀘스트 완료 상태로 넘어가지 않습니다.

---

## 11. 퀘스트 완료 시 아이템 소모 설정

퀘스트 완료 시 소모할 아이템은 `quest_consume_items.csv`에 작성합니다.

필드:

| 필드 | 설명 |
| --- | --- |
| `quest_id` | 아이템을 소모할 퀘스트 ID입니다. |
| `consume_order` | 같은 퀘스트 안에서 소모 항목을 정렬하는 순서입니다. |
| `item_id` | 퀘스트 완료 확정 시 자동 소모할 아이템 ID입니다. |
| `item_amount` | 소모할 수량입니다. 1 이상이어야 합니다. |

예:

```text
quest_id,consume_order,item_id,item_amount
QUEST_OPEN_OLD_STOREHOUSE,0,quest_old_key,1
```

동작:

```text
퀘스트 목표가 모두 만족됨
-> TryCompleteQuest 실행
-> old_key 1개 보유 확인
-> 보유 중이면 old_key 1개 소모
-> QUEST_COMPLETED 상태로 변경
```

부족하면:

```text
old_key 0개
-> TryCompleteQuest false
-> QUEST_IN_PROGRESS 유지
```

---

## 12. 퀘스트 보상으로 아이템 지급하기

`quest_rewards.csv`에는 Progress flag 보상과 아이템 보상을 함께 작성할 수 있습니다.

보상 필드:

| 필드 | 설명 |
| --- | --- |
| `reward_id` | 같은 퀘스트 안에서 고유한 보상 ID입니다. |
| `description_text_key` | 퀘스트 창에 표시할 보상 설명 문장 키입니다. |
| `progress_flag_id` | 보상 지급 시 변경할 Progress flag입니다. 비워둘 수 있습니다. |
| `flag_value` | 저장할 flag 값입니다. Progress flag가 없으면 비웁니다. |
| `item_id` | 보상으로 지급할 아이템 ID입니다. 비워둘 수 있습니다. |
| `item_amount` | 보상 아이템 지급 수량입니다. 1 이상이어야 합니다. |

예:

```text
quest_id,reward_order,reward_id,description_text_key,progress_flag_id,flag_value,item_id,item_amount
QUEST_RESTORE_MURAL,0,REWARD_RICE_WINE,quest.restore_mural.reward.rice_wine,,,item_rice_wine,3
```

보상을 받으면:

```text
IInventoryService.TryAddItem(item_rice_wine, 3)
```

가 호출됩니다.

---

## 13. 인벤토리 UI 구조

인벤토리 UI는 세 부분으로 나뉩니다.

| UI 코드 | 역할 |
| --- | --- |
| `InventoryPresenter` | 열기/닫기, 카테고리 전환, 슬롯 선택, 드래그 이동 입력 처리 |
| `InventoryView` | 슬롯 목록과 선택 아이템 상세 정보 표시 |
| `InventorySlotView` | 슬롯 한 칸의 아이콘, 수량, new 표시, 선택 표시, 드래그 입력 |

역할 분리:

```text
InventoryPresenter
-> 입력과 선택만 처리
-> 도메인 변경은 InventoryController(IInventoryService)로 위임

InventoryView
-> 표시만 처리
-> 서비스 직접 호출 없음

InventoryController
-> 아이템 추가/소모/조회/수동 배치 순서 변경 담당
```

---

## 14. InventoryView 설정하기

`InventoryView`는 인벤토리 팝업 내부 UI를 표시합니다.

필드:

| 필드 | 설명 |
| --- | --- |
| `Root Canvas Group` | 인벤토리 UI 전체 표시/입력 차단 제어 |
| `Slot Page Canvas Group` | 1페이지 슬롯 목록 영역 |
| `Detail Page Canvas Group` | 2페이지 선택 아이템 상세 영역 |
| `Slot Scroll Rect` | 30개 초과 슬롯 스크롤 |
| `Slot Content Root` | 슬롯들이 배치될 콘텐츠 |
| `Slot Grid Layout` | 6열 슬롯 배치 |
| `Slot View Prefab` | 슬롯 한 칸 프리팹 |
| `Selected Item Icon Image` | 선택 아이템 아이콘 |
| `Selected Item Name Text` | 선택 아이템 이름 |
| `Selected Item Price Text` | 선택 아이템 가격 |
| `Selected Item Description Text` | 선택 아이템 설명 |

슬롯 표시 규칙:

```text
기본 6x5 = 30칸
30개 초과
-> 6열 유지
-> 필요한 만큼 행 추가
-> ScrollRect로 스크롤
```

중요:

- 슬롯 UI는 매번 새로 만들지 않고 리스트로 재사용합니다.
- 슬롯 수가 늘어날 때만 필요한 만큼 추가 생성합니다.

---

## 15. InventorySlotView 설정하기

`InventorySlotView`는 슬롯 프리팹에 붙습니다.

필드:

| 필드 | 설명 |
| --- | --- |
| `Button` | 클릭 가능한 슬롯 영역입니다. 같은 오브젝트에 있으면 자동 보강됩니다. |
| `Canvas Group` | 드래그 중 투명도와 Raycast 상태를 제어합니다. 같은 오브젝트에 있으면 자동 보강됩니다. |
| `Icon Image` | 아이템 아이콘입니다. |
| `Quantity Text` | 수량 텍스트입니다. |
| `New Marker Root` | new 표시 오브젝트입니다. |
| `Selected Marker Root` | 선택 표시 오브젝트입니다. |

프리팹에 필요한 것:

```text
InventorySlotView
Button
CanvasGroup
아이콘 Image
수량 TMP_Text
new 표시 오브젝트
선택 표시 오브젝트
```

---

## 16. InventoryPresenter 설정하기

`InventoryPresenter`는 UI 입력만 담당합니다.

필드:

| 필드 | 설명 |
| --- | --- |
| `Inventory View` | 표시를 담당하는 `InventoryView`입니다. |
| `Popup Panel Root` | 외부 클릭 닫기 판정에 사용할 팝업 패널입니다. |
| `UI Event Camera` | Screen Space Camera UI에서 좌표 변환에 사용할 카메라입니다. Overlay면 비워둡니다. |
| `Hud Open Button` | HUD에서 인벤토리를 여는 버튼입니다. |
| `Close Button` | X 닫기 버튼입니다. |
| `Close When Click Outside` | 외부 클릭 닫기 사용 여부입니다. |
| `Toggle With Tab Key` | Tab 열기/닫기 사용 여부입니다. |
| `Close With Escape Key` | Esc 닫기 사용 여부입니다. |
| `Default Category` | 처음 열 때 표시할 카테고리입니다. |
| `All Category Button` | 전체 카테고리 버튼입니다. |
| `Alcohol Category Button` | 술 카테고리 버튼입니다. |
| `Relic Category Button` | 유물 카테고리 버튼입니다. |
| `Misc Category Button` | 기타 카테고리 버튼입니다. |

입력 규칙:

```text
HUD 버튼
-> 인벤토리 열기

Tab
-> 인벤토리 열기/닫기

X 버튼
-> 인벤토리 닫기

Esc
-> 인벤토리 닫기

팝업 바깥 클릭
-> 인벤토리 닫기
```

---

## 17. GameRoot 연결하기

인벤토리 시스템은 `GameRoot`에서 초기화합니다.

GameRoot에 필요한 연결:

```text
InventoryController
InventoryPresenter
```

초기화 흐름:

```text
InventoryController.Init()
-> InventoryPresenter.Init(_inventoryController)
-> QuestController.Init(_progressController, _inventoryController)
```

중요:

- 외부 코드가 `InventoryController`를 직접 들고 다니지 않도록 합니다.
- 외부 접근은 가능하면 `GameRoot.Instance.Inventory` 또는 주입받은 `IInventoryService`를 사용합니다.
- UI 입력은 `InventoryPresenter`가 받고, 실제 데이터 변경은 `InventoryController`가 담당합니다.

---

## 18. 실전 예시: 퀘스트 보상으로 아이템 지급하기

목표:

```text
장로 퀘스트 보상으로 쌀술 3개 지급
```

### 18-1. 아이템 만들기

`Item_RiceWine.asset`

```text
Item Id: alcohol_rice_wine
Display Name: 쌀술
Description: 맑고 은은한 향이 나는 술이다.
Icon: 쌀술 아이콘
Price: 50
Item Type: ITEM_ALCOHOL_COLLECTIBLE
Max Stack Count: 99
```

### 18-2. InventoryController 등록

```text
GameRoot
-> InventoryController
-> Item Database
-> Item_RiceWine 추가
```

### 18-3. Quest Rewards CSV 설정

`quest_rewards.csv`:

```text
quest_id,reward_order,reward_id,description_text_key,progress_flag_id,flag_value,item_id,item_amount
QUEST_RESTORE_MURAL,0,REWARD_RICE_WINE,quest.restore_mural.reward.rice_wine,,,item_rice_wine,3
```

### 18-4. 대화 선택지 연결

퀘스트 완료 대화 선택지:

```text
choice_text_key: dialogue.reward.claim
action_type: CLAIM_QUEST_REWARD
action_id: QUEST_RESTORE_MURAL
```

결과:

```text
선택지 클릭
-> QuestController.TryClaimReward()
-> QuestDefinition의 보상 적용
-> IInventoryService.TryAddItem(item_rice_wine, 3)
-> 인벤토리에 쌀술 3개 추가
```

---

## 19. 실전 예시: 퀘스트 완료 시 아이템 소모하기

목표:

```text
낡은 열쇠 1개를 가지고 있어야 창고 열기 퀘스트 완료
완료 시 낡은 열쇠 1개 자동 소모
```

### 19-1. 아이템 만들기

`Item_OldKey.asset`

```text
Item Id: quest_old_key
Display Name: 낡은 열쇠
Description: 오래된 창고 문을 열 수 있을 것 같은 열쇠다.
Icon: 낡은 열쇠 아이콘
Price: 0
Item Type: ITEM_QUEST
Max Stack Count: 99
```

### 19-2. Quest Consume Items CSV 설정

`quest_consume_items.csv`:

```text
quest_id,consume_order,item_id,item_amount
QUEST_OPEN_OLD_STOREHOUSE,0,quest_old_key,1
```

### 19-3. 완료 선택지 연결

대화 선택지:

```text
choice_text_key: dialogue.old_storehouse.complete
action_type: COMPLETE_QUEST
action_id: QUEST_OPEN_OLD_STOREHOUSE
```

결과:

```text
낡은 열쇠 1개 이상 보유
-> 열쇠 1개 소모
-> QUEST_COMPLETED

낡은 열쇠 부족
-> TryCompleteQuest false
-> QUEST_IN_PROGRESS 유지
```

---

## 20. 검수 체크리스트

### ItemDataSO

- `Item Id`가 비어 있지 않은가?
- `Item Id`가 다른 아이템과 중복되지 않는가?
- `Display Name`이 표시용 이름으로 들어가 있는가?
- `Description`이 상세 페이지에 표시할 문장인가?
- `Icon`이 들어가 있는가?
- `Price`가 0 이상인가?
- `Item Type`이 의도한 카테고리와 맞는가?
- `Max Stack Count`가 1 이상인가?

### InventoryController

- `Item Database`에 사용할 모든 `ItemDataSO`가 들어가 있는가?
- 중복 `ItemId`가 없는가?
- GameRoot에 `InventoryController`가 연결되어 있는가?

### Quest CSV

- 아이템 보상이 필요하면 `quest_rewards.csv`의 `item_id`와 `item_amount`가 들어가 있는가?
- 완료 시 소모가 필요하면 `quest_consume_items.csv`에 항목이 있는가?
- 소모 수량이 1 이상인가?
- 소모 아이템이 InventoryController의 Item Database에 등록되어 있는가?

### Inventory UI

- `InventoryPresenter`가 GameRoot에 연결되어 있는가?
- `InventoryPresenter`에 `InventoryView`가 연결되어 있는가?
- `Popup Panel Root`가 실제 팝업 패널 RectTransform인가?
- HUD 열기 버튼과 X 닫기 버튼이 연결되어 있는가?
- 카테고리 버튼이 연결되어 있는가?
- `InventoryView`에 `Slot View Prefab`이 연결되어 있는가?
- `InventorySlotView` 프리팹에 아이콘, 수량, new 표시, 선택 표시가 연결되어 있는가?

---

## 21. 자주 생기는 문제

### 아이템이 보상으로 들어오지 않는다

확인할 것:

- `quest_rewards.csv`의 `item_id`가 비어 있지 않은가?
- `item_amount`가 1 이상인가?
- `ItemDataSO`의 `Item Id`가 비어 있지 않은가?
- 퀘스트 보상 선택지가 `CLAIM_QUEST_REWARD`인가?
- 이미 보상을 받은 퀘스트를 다시 받으려는 것은 아닌가?

### 인벤토리에는 있는데 아이콘/이름이 안 나온다

확인할 것:

- 해당 `ItemDataSO`가 `InventoryController`의 `Item Database`에 등록되어 있는가?
- 슬롯의 `ItemId`와 `ItemDataSO.ItemId`가 완전히 같은가?
- `Icon`, `Display Name`, `Description` 필드가 비어 있지 않은가?

### 술/유물/기타 탭에서 아이템이 안 보인다

확인할 것:

- `Item Type`이 카테고리 규칙과 맞는가?
- 퀘스트 아이템은 전체 탭에만 보이는 것이 정상입니다.
- 기타 탭에는 `ITEM_MISC`만 보입니다.

### 퀘스트 완료 시 아이템이 소모되지 않는다

확인할 것:

- `quest_consume_items.csv`에 아이템이 들어가 있는가?
- `item_amount`가 1 이상인가?
- 플레이어가 해당 아이템을 충분히 가지고 있는가?
- 완료 선택지가 `COMPLETE_QUEST` 또는 보상 수령 흐름에서 완료를 거치는 구조인가?

### 인벤토리가 Tab으로 열리지 않는다

확인할 것:

- 씬에 `InventoryPresenter`가 있는가?
- GameRoot의 `_inventoryPresenter`에 연결되어 있는가?
- `Toggle With Tab Key`가 켜져 있는가?
- 프로젝트 Input System이 활성화되어 있는가?

### 바깥 클릭으로 닫히지 않는다

확인할 것:

- `Close When Click Outside`가 켜져 있는가?
- `Popup Panel Root`가 실제 팝업 패널 RectTransform으로 연결되어 있는가?
- Screen Space Camera UI라면 `UI Event Camera`가 맞게 연결되어 있는가?

---

## 22. 추천 작업 순서

새 아이템과 인벤토리 보상을 만들 때는 아래 순서로 작업하면 실수가 적습니다.

```text
1. ItemDataSO 만들기
2. Item Id 규칙 확인
3. Item Type 지정
4. Icon, Display Name, Description, Price 입력
5. Max Stack Count 확인
6. InventoryController의 Item Database에 등록
7. quest_rewards.csv 또는 quest_consume_items.csv에 Item Id 작성
8. dialogue_choices.csv에서 CLAIM_QUEST_REWARD 또는 COMPLETE_QUEST 연결
9. 플레이해서 아이템 지급/소모 확인
10. Tab으로 인벤토리 열기
11. 전체/술/유물/기타 카테고리 표시 확인
12. 슬롯 new 표시와 상세 페이지 확인
```

이 순서만 지키면 대부분의 아이템 지급, 확인, 자동 소모 흐름은 코드 수정 없이 만들 수 있습니다.
