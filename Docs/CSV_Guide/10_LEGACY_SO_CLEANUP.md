# 10단원 — Dialogue·Quest 레거시 SO 정리

## 1. 목표

CSV 전환이 완료된 Dialogue와 Quest에서 더 이상 사용하지 않는 ScriptableObject 코드와 테스트 에셋을 제거한다.

두 데이터 입력 방식이 동시에 남아 있으면 다음 문제가 발생한다.

- 기획자가 CSV와 SO 중 어느 쪽을 수정해야 하는지 혼동함
- 같은 ID와 내용이 서로 다른 원본에 중복됨
- 사용하지 않는 직렬화 코드와 에디터 Drawer가 빌드·컴파일 대상에 남음
- 오래된 작성 문서가 현재 런타임과 다른 작업 방법을 안내함

10단원 이후 Dialogue와 Quest의 기획 원본은 CSV 한 종류입니다.

## 2. 제거된 런타임 타입

- `DialogueActorSO`
- `DialogueGraphSO`
- `DialoguePortraitIdAttribute`
- `QuestDataSO`

위 타입은 현재 `DialogueController`, `QuestController`, `NpcDialogueInteractable`, `GameRoot`에서 참조하지 않습니다.

## 3. 제거된 에디터 코드

- `DialoguePortraitIdDrawer`

이 Drawer는 `DialogueGraphSO` 노드에서 `DialogueActorSO`의 표정을 선택할 때 사용했습니다. 현재 표정과 Sprite 연결은 `dialogue_portraits.csv`와 `DialogueVisualCatalogSO`가 담당하므로 필요하지 않습니다.

## 4. 제거된 테스트 에셋

- `PlayerDialogueActor.asset`
- `TestDialogueActor.asset`
- `TestDialogueGraph.asset`
- `QuestData.asset`

참조 감사에서 위 에셋은 씬·프리팹·현재 런타임 에셋에 연결되지 않고 서로끼리만 참조하는 것이 확인됐습니다.

삭제된 파일은 Git 이력에서 복구할 수 있지만 현재 게임 데이터로 다시 사용하면 안 됩니다.

## 5. 유지하는 ScriptableObject

CSV 전환은 모든 ScriptableObject를 제거하는 작업이 아닙니다. Unity 오브젝트 참조가 필요한 데이터는 SO가 적합합니다.

### DialogueVisualCatalogSO

CSV의 안정적인 문자열 ID를 Unity Sprite에 연결합니다.

```text
sprite_id / background_id
-> DialogueVisualCatalogSO
-> Sprite
```

### ItemDataSO

아이템 아이콘, 표시 이름, 설명, 가격, 타입처럼 Unity 인벤토리 UI가 사용하는 에셋 데이터를 보관합니다.

퀘스트 CSV는 `item_id`만 기록하고, `InventoryController`의 Item Database가 같은 ID의 `ItemDataSO`를 연결합니다.

### SceneCatalogSO

씬 이름과 Unity SceneAsset 참조를 연결하는 카탈로그이므로 유지합니다.

## 6. 현재 데이터 흐름

```text
Dialogue / Quest / NPC / Localization CSV
-> GameDataCsvLoader
-> GameDataRepository
-> DialogueController / QuestController
```

```text
CSV visual ID
-> DialogueVisualCatalogSO
-> Unity Sprite
```

```text
Quest CSV item_id
-> InventoryController Item Database
-> ItemDataSO
```

## 7. 문서 정리

다음 문서를 현재 CSV 구조로 갱신했습니다.

- `Assets/_Game/Docs/DIALOGUE_QUEST_AUTHORING_GUIDE.md`
- `Assets/_Game/Docs/INVENTORY_SYSTEM_AUTHORING_GUIDE.md`
- `Assets/_Game/Docs/INTERACTION_AUTHORING_GUIDE.md`

Dialogue와 Quest 작성자는 SO 생성 메뉴가 아니라 엑셀 교보재와 CSV 데이터 검증기를 사용합니다.

## 8. 회귀 방지

`GameDataCsvRegressionTests`에 다음 검사를 추가합니다.

- 레거시 SO 타입이 `Assembly-CSharp`에 존재하지 않는지 확인
- 제거된 레거시 테스트 에셋 경로가 다시 생성되지 않았는지 확인

기존 검사도 계속 유지합니다.

- 실제 런타임 CSV 12개 Repository 생성
- 필수 CSV 누락 거부
- 잘못된 CSV 헤더 거부

## 9. 현재 CSV 아이템 연결

현재 교보재 퀘스트가 참조하는 다음 아이템 ID를 `ItemDataSO`로 연결했습니다.

- `ITEM_COIN` → `Coin.asset`
- `ITEM_REPAIR_KIT` → `RepairKit.asset`

두 에셋은 `GameRoot.prefab`의 `InventoryController` Item Database에 등록되어 있습니다.

아이콘 원화는 아직 저장소에 없으므로 임의 Sprite를 사용하지 않았습니다. CSV 데이터 검증기는 아이콘 누락을 경고로 표시하며, 실제 아트가 준비되면 각 `ItemDataSO.Icon`에 연결합니다.

## 10. 완료 기준

- 레거시 Dialogue·Quest SO 타입 참조가 코드에 없다.
- 레거시 테스트 에셋 GUID가 씬과 프리팹에 없다.
- Dialogue·Quest 작성 문서가 CSV 방식만 안내한다.
- 프로젝트 런타임 및 Editor 어셈블리가 컴파일된다.
- CSV 로더 회귀 테스트가 통과한다.
- Unity CSV 데이터 검증기의 오류가 0개다.
