# 폴더 구조 가이드

이 구조는 현재 구현된 `Progress`, `SceneFlow`, `Player` 코드를 기준으로 정리하고, 기획 문서의 마을 퀘스트, 대화, 인벤토리, 유적 재건, 미니게임 확장을 담을 자리를 미리 나눈 것입니다.

## Assets/_Game

```
Assets/_Game/
├── Data/                 # ScriptableObject 에셋 인스턴스
├── Docs/                 # 프로젝트 내부 개발 문서
├── Prefabs/              # 공용 프리팹
├── Scenes/               # Unity 씬
├── Scripts/              # 런타임 스크립트
└── Sprites/              # 2D 이미지 리소스
```

## Scripts

```
Scripts/
├── Core/                 # 앱 진입점, 공용 기반 코드
├── Systems/              # 여러 기능이 공유하는 전역 시스템
├── Gameplay/             # 실제 플레이 규칙과 콘텐츠 기능
├── Presentation/         # 카메라, UI, 화면 연출
└── Tests/                # 수동 테스트/검증용 스크립트
```

## Gameplay 확장 기준

- `Dialogue`: NPC 대화 노드, 선택지, 대화 UI 연동 로직
- `Quests`: 퀘스트 상태, 목표, 보상, NPC 연동 로직
- `Inventory`: 퀘스트 아이템, 수집형 아이템, 마을 콘텐츠 아이템
- `Village`: 마을 NPC, 상점, 거래, 마을 콘텐츠 진입 로직
- `Relics`: 유적/기억/기술 수집 및 재건 진행 로직
- `Minigames`: 낚시, 농사, 채집 등 마을 콘텐츠 미니게임
- `Skills`: 탐험과 퍼즐 해금에 쓰이는 스킬 데이터와 계약
- `Interaction`: 상호작용 대상, 하이라이트, 카메라 타겟 계약
- `Combat`: 공격, 피해, 체력, 사망 관련 계약
- `World`: 씬 안의 위치/스폰/맵 배치 보조 코드
