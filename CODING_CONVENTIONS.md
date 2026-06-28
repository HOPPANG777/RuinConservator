# 📝 Unity Game Development Coding Conventions (v3.0)

본 프로젝트의 **코드 일관성, 런타임 성능, 유지보수 효율, 그리고 안정성**을 극대화하기 위해 정의된 가이드라인입니다.

모든 팀원은 코드를 작성하거나 리뷰할 때 반드시 이 문서를 기준으로 합니다.

> 이 문서는 실제 프로젝트 코드(`GameRoot`, `Controller`, `SO`, `Service` 패턴)를 기반으로 작성되었습니다.
> 새 코드를 작성할 때는 "이 문서"와 "이미 작성된 동일 계열 코드" 두 가지를 함께 기준으로 삼으세요.

---

## 0. 사용 기술 스택 (Tech Stack)

본 프로젝트는 아래 라이브러리를 표준으로 사용합니다. **각 라이브러리는 정해진 용도에만** 사용하며, 같은 일을 하는 두 가지 방식을 혼용하지 않습니다.

| 라이브러리         | 용도                          | 사용 기준 (요약)                                                                      |
| ------------------ | ----------------------------- | ------------------------------------------------------------------------------------- |
| **UniTask**        | 비동기 처리 (`async`/`await`) | 씬·리소스 로딩, I/O, 네트워크 등 **반드시 비동기여야 하는 곳**에만 (→ 2.1)            |
| **DOTween Pro**    | 트위닝 / 연출 애니메이션      | UI·Transform·색상 등 **값 보간 연출**에 사용. 게임 로직 타이밍 제어 용도 아님 (→ 2.2) |
| **Easy Save 3**    | 데이터 영속화 (저장/로드)     | 세이브 데이터, 설정값 등 **디스크 저장이 필요한 모든 것** (→ 2.3)                     |
| **Odin Inspector** | 인스펙터 직렬화 / 에디터 UI   | 직렬화 필드 그룹화, 버튼, 검증 등 (→ 4)                                               |

> ⚠️ 위 4개 라이브러리는 프로젝트 종속성입니다. 동일 기능의 다른 에셋(예: 다른 트윈 라이브러리, `PlayerPrefs` 기반 세이브 등)을 추가로 도입하지 않습니다.

---

## 1. 핵심 설계 원칙 (Core Architecture)

### 1.1 의존성 및 결합도 (DI & Low Coupling)

- **의존성 주입(DI) 권장**: 클래스 내부에서 직접 `new`로 협력 객체를 생성하거나 `GameObject.Find` 계열 함수를 사용하는 것을 지양합니다.
- **낮은 결합도**: 각 클래스는 자신의 역할에만 집중하며, 협력 대상의 세부 구현이 아닌 **인터페이스(Interface)에 의존**합니다.
- **주입 방식은 대상에 따라 둘로 나눕니다.**
  - `MonoBehaviour` **컨트롤러**: 생성자를 쓸 수 없으므로, `GameRoot`가 `Start()`에서 호출하는 `Init(의존성...)` **메서드**로 주입받습니다. (프로젝트 표준)
  - **순수 C# 객체(POCO)**: 생성자 또는 `Construct(의존성...)` 메서드로 주입받습니다.

```csharp
// MonoBehaviour 컨트롤러는 Init()으로 의존성을 받습니다.
public void Init(IProgressService progressService)
{
    _progressService = progressService;
    RebuildQuestDatabase();
}
```

### 1.2 상속 구조 (Inheritance Limit)

- **1단계 상속 원칙**: 상속은 최대 1단계까지만 허용합니다. (예: `UnitBase` → `UnitPlayer`)
- **구성(Composition) 중심**: 2단계 이상의 깊은 상속 대신, 기능을 작은 클래스로 분리해 조립하는 방식을 사용합니다. ('클래스 폭발' 방지)

### 1.3 매니저 & 컨트롤러 구조 (Manager & Controller)

- **싱글턴은 오직 1개**: 프로젝트 전역 싱글턴은 `GameRoot` **하나만** 둡니다. (`Singleton<T>` 상속은 `GameRoot` 전용)
- **컨트롤러는** `GameRoot`**의 멤버**: 흔히 "매니저"라 부르는 전역 관리 클래스들은 **싱글턴으로 만들지 않고**, `GameRoot` 내부 멤버(`[SerializeField]` 필드)로 보관되는 **Controller**로 작성합니다.
- **접근 경로 단일화**: 어떤 컨트롤러도 외부에서 직접 생성/참조하지 않습니다. 접근은 **반드시** `GameRoot.Instance`를 경유합니다.
  - 예: `GameRoot.Instance.Quest`, `GameRoot.Instance.Progress`
- **외부에는 인터페이스(계약)만 노출**: `GameRoot`는 구현체 필드를 직렬화하되, 외부 공개 프로퍼티는 **인터페이스 타입**으로 노출하여 결합도를 낮춥니다.

```csharp
[Header("Controller")]
[Tooltip("퀘스트 상태 전환, 목표 갱신, 보상 지급을 관리하는 컨트롤러입니다.")]
[SerializeField] private QuestController _questController;

// 외부에는 구현체가 아닌 인터페이스(IQuestService) 계약만 노출합니다.
public IQuestService Quest => _questController;
```

---

## 2. 프로그래밍 가이드 및 최적화

### 2.1 시간 / 대기 / 비동기 처리 (Update·for 우선, UniTask는 "필수 비동기"에만)

- **Coroutine 금지**: 유니티 기본 코루틴(`StartCoroutine`)은 사용하지 않습니다.
- **대기/타이밍은 Update로 대체**: "몇 초 뒤 실행", "N프레임 후 실행", "조건 만족 시 진행" 같은 로직은 아래 방식으로 작성합니다.
  - **Update + 타이머**: `elapsed += Time.deltaTime` 누적 후 분기
  - **Update + 상태 머신(State Machine)**: `State`에 따라 단계별 로직 분리 (가독성/디버깅 최적)
  - **for/while 분해**: "프레임에 걸쳐야 할 이유"가 없으면 한 프레임에서 끝냅니다.

  ```csharp
  // "1.5초 뒤 1회 실행"
  private float _elapsed;
  private bool _fired;

  private void Update()
  {
      if (_fired) return;

      _elapsed += Time.deltaTime;
      if (_elapsed < 1.5f) return;

      _fired = true;
      DoSomethingOnce();
  }
  ```

- **UniTask는 "반드시 비동기여야 하는 곳"에만 사용**: 엔진/플랫폼/외부 리소스 특성상 `await`가 필요한 경우입니다.
  - **씬/리소스 로딩**: `SceneManager.LoadSceneAsync`, Addressables 로드 등
  - **네트워크/웹 요청**: API 호출, 서버 통신 등
  - **파일 I/O**: 저장/로드, 로컬 파일 접근 등 (→ Easy Save와 함께, 2.3)
  - **비동기 초기화**: 의존 대상 완료까지 기다려야 하는 초기화 흐름 등
- **프레임 대기용 await 남용 금지**: `await UniTask.Yield()` / `DelayFrame()`로 "Update를 대체"하지 않습니다. 단, 위에 해당하는 **비동기 작업의 진행을 프레임 단위로 모니터링**하는 용도(예: 로딩 진행도 갱신)는 허용합니다.
- **Cancellation 필수**: 오브젝트 생명주기와 함께 취소되어야 하는 비동기는 `this.GetCancellationTokenOnDestroy()` 또는 명시적 `CancellationToken`을 받아 취소 가능하게 설계합니다.
  ```csharp
  // 취소 토큰을 받아 안전하게 중단 가능한 비동기 메서드
  public async UniTask FadeInAsync(CancellationToken token)
  {
      await FadeAsync(0.0f, 1.0f, token);
  }
  ```

### 2.2 트위닝 / 연출 애니메이션 (DOTween Pro)

- **용도**: UI alpha, `Transform`(이동·회전·스케일), 색상 등 **값을 시간에 따라 보간하는 시각적 연출**에 DOTween Pro를 사용합니다. 직접 `Lerp`를 손으로 굴리는 코드를 줄여 가독성을 높입니다.
- **게임 로직 타이밍 제어 용도가 아님**: "몇 초 뒤 게임 상태 전환" 같은 **게임 로직 흐름**은 DOTween의 `OnComplete`에 의존하지 말고 2.1(Update/상태 머신/UniTask)로 처리합니다. DOTween은 어디까지나 "보이는 연출" 담당입니다.
- **안정성 규칙 (필수)**: 트윈은 대상 오브젝트보다 오래 살아남아 `MissingReferenceException`을 일으킬 수 있습니다. 아래 중 하나로 **반드시 생명주기에 묶습니다.**
  - 대상 컴포넌트에 `SetLink(gameObject)`로 연결 (권장), 또는
  - 트윈 핸들을 필드로 보관했다가 `OnDestroy`/재생 직전에 `_tween?.Kill()` 호출
- **무한/반복 트윈은 핸들 보관 필수**: `SetLoops(-1)` 같은 트윈은 반드시 핸들을 보관하고 명시적으로 `Kill()` 합니다.
- **재생 전 중복 제거**: 같은 대상에 새 트윈을 시작하기 전 이전 트윈을 정리합니다. (`target.DOKill()` 또는 보관 핸들 `Kill()`)
- **UniTask 연동**: 트윈 완료를 기다려야 하면 `await tween.ToUniTask(cancellationToken: token)`로 취소 토큰과 함께 await 합니다.

```csharp
// UI 페이드 연출 예시 (생명주기 연결 + 토큰 연동)
private Tween _fadeTween;

public async UniTask FadeInAsync(CanvasGroup canvasGroup, float duration, CancellationToken token)
{
    // 이전 페이드가 남아 있으면 정리합니다.
    _fadeTween?.Kill();

    _fadeTween = canvasGroup
        .DOFade(1.0f, duration)
        .SetUpdate(true)        // 일시정지(Time.timeScale=0) 중에도 동작
        .SetLink(gameObject);   // 오브젝트 파괴 시 자동 Kill

    await _fadeTween.ToUniTask(cancellationToken: token);
}
```

### 2.3 데이터 저장 / 로드 (Easy Save 3)

- **모든 영속화는 Easy Save(ES3)로 통일**: 세이브 데이터, 옵션/설정값 등 디스크에 남아야 하는 데이터는 `ES3` API로 저장/로드합니다. `PlayerPrefs`나 수동 파일 쓰기를 새로 도입하지 않습니다.
- **저장/로드는 컨트롤러 책임**: 저장 로직을 게임플레이 클래스 곳곳에 흩지 말고, 데이터를 소유한 **Controller**(예: `ProgressController`)가 자신의 상태를 직렬화/역직렬화하도록 캡슐화합니다.
- **키는 상수로 관리**: 저장 키 문자열은 매직 스트링으로 흩어 쓰지 말고 `const`로 한곳에서 관리합니다. (오타·키 충돌 방지)
- **세이브 슬롯은 파일 경로로 분리**: 슬롯별 저장은 `ES3Settings`의 파일 경로로 구분합니다.
- **I/O는 비동기 + 예외 처리**: 큰 저장/로드는 메인 스레드 정지를 피하기 위해 `ES3.SaveAsync`/`LoadAsync`(UniTask)로 처리하고, 파일 손상·부재에 대비해 `ES3.KeyExists` 확인과 `try/catch`로 방어합니다.

```csharp
public class ProgressController : MonoBehaviour, IProgressService
{
    // 저장 키는 상수로 관리하여 오타와 키 충돌을 방지합니다.
    private const string SAVE_KEY_FLAGS = "progress.flags";

    /// <summary>전역 플래그를 디스크에 저장합니다.</summary>
    public void Save()
    {
        ES3.Save(SAVE_KEY_FLAGS, _globalFlags);
    }

    /// <summary>저장된 전역 플래그가 있으면 불러옵니다.</summary>
    public void Load()
    {
        // 키가 없으면(최초 실행) 기본 상태를 유지합니다.
        if (ES3.KeyExists(SAVE_KEY_FLAGS) == false)
        {
            return;
        }

        _globalFlags = ES3.Load(SAVE_KEY_FLAGS, _globalFlags);
    }
}
```

### 2.4 루프 최적화 (Loops)

- **for문 우선**: 리스트/배열 순회 시, 불필요한 할당과 성능 저하를 줄이기 위해 `foreach` 대신 `for`문을 우선 고려합니다.
- **예외**: `Dictionary` 순회 등 인덱서가 없는 컬렉션은 `foreach`를 사용합니다. (가독성 우선)

### 2.5 방어적 프로그래밍 & 안정성 (Stability)

런타임 크래시를 막고 디버깅을 쉽게 하기 위한 필수 규칙입니다. 본 프로젝트 코드는 아래 스타일을 따릅니다.

- **가드절(Early Return)**: 유효하지 않은 입력은 메서드 앞부분에서 즉시 `return`하여 중첩(들여쓰기)을 줄입니다.
- **참조 null 체크**: `[SerializeField]`로 받은 참조는 사용 전 `null`을 확인합니다. 연결 누락 시 크래시 대신 로그를 남기고 안전하게 종료합니다.
- **문자열 ID 검증**: 키/ID로 쓰는 문자열은 `string.IsNullOrWhiteSpace`로 검증합니다.
- **Try 패턴**: 실패가 정상 흐름인 조회/상태 변경은 예외 대신 `bool TryXxx(..., out 결과)` 형태로 작성합니다.
- **로그 접두사 통일**: 로그에는 `[클래스명]` 접두사를 붙여 출처를 명확히 합니다.
- **로그는 조건부 컴파일 고려**: 빈번한 디버그 로그는 `[System.Diagnostics.Conditional("UNITY_EDITOR")]` 등으로 빌드에서 제외해 성능 영향을 줄입니다.
- **종료 시점 싱글턴 접근 금지**: `OnDestroy`/`OnApplicationQuit` 단계에서 다른 싱글턴/컨트롤러에 접근하면 유령 오브젝트가 생길 수 있으므로 지양합니다.

```csharp
// 가드절 + Try 패턴 + 로그 접두사
public bool TryAcceptQuest(QuestDataSO questData)
{
    if (questData == null)
    {
        Debug.LogWarning("[QuestController] 수락할 퀘스트 데이터가 null입니다.");
        return false;
    }

    if (GetQuestState(questData) != QuestState.QUEST_NOT_STARTED)
    {
        return false;
    }

    SetQuestState(questData, QuestState.QUEST_IN_PROGRESS);
    return true;
}
```

### 2.6 주석 규칙 (Comments & Documentation)

모든 **클래스 / 메서드 / 필드**에는 역할을 설명하는 주석을 최소 한 줄 이상 작성합니다.

- **기본 한 줄 주석**: 필드, 짧은 로직 설명에 사용합니다.
  ```csharp
  // 퀘스트 ID로 원본 데이터를 찾기 위한 캐시입니다.
  ```
- **TODO / FIXME 주석**: 추가 작업이나 버그 수정이 필요한 곳에 명시합니다.
  ```csharp
  // TODO: 예외 처리 추가 필요
  // FIXME: 메모리 누수 발생 가능성 있음
  ```
- **문서화 주석(XML)**: 클래스의 공개 API(메서드/프로퍼티)에 사용합니다. 인터페이스에 작성해 두면 구현체와 호출부 양쪽에서 IntelliSense로 보입니다.
  ```csharp
  /// <summary>
  /// 퀘스트 ID로 퀘스트 데이터를 조회합니다.
  /// </summary>
  /// <param name="questId">조회할 퀘스트 ID입니다.</param>
  /// <param name="questData">조회된 퀘스트 데이터입니다.</param>
  /// <returns>퀘스트 데이터를 찾으면 true입니다.</returns>
  bool TryGetQuestData(string questId, out QuestDataSO questData);
  ```
- **구분선 / 영역 주석**: 파일 내 큰 영역(직렬화 필드 / 내부 상태 / 공개 API / 내부 로직 등)을 나눌 때 사용합니다.
  ```csharp
  //==============================================================================
  // 공개 API
  //==============================================================================
  ```
- **파일 헤더 주석**: 클래스 파일 상단에 기능/설명을 명시합니다.
  ```csharp
  //==============================================================================
  // 기능: MyNameClass::Create
  // 설명: 객체를 생성하는 함수
  //==============================================================================
  // 세부 구현 사항
  //
  //
  //
  //==============================================================================
  ```

---

## 3. 네이밍 규칙 (Naming Conventions)

### 3.1 Manager / Controller 네이밍

- **Manager**: 싱글턴 클래스에만 붙입니다. (본 프로젝트에서는 사실상 `GameRoot`만 해당)
- **Controller**: 싱글턴이 아니지만 특정 로직(시스템/도메인)을 관리하는 클래스. `GameRoot` 멤버로 두고 `GameRoot.Instance` 경유로만 접근합니다.
- **Service (I-인터페이스)**: 컨트롤러가 외부에 노출하는 계약은 `IXxxService`로 명명합니다. (예: `IQuestService`, `IProgressService`)
- **Presenter / View**: 표시(UI) 전담 클래스는 `View`, 표시 연출/흐름 제어는 `Presenter` 접미사를 사용합니다.
- **데이터(ScriptableObject)**: `XxxDataSO`, `XxxGraphSO` 등 `SO` 접미사를 붙입니다.

### 3.2 표기법 (Case)

| 분류                   | 표기법 (Case)     | 예시                | 비고                   |
| ---------------------- | ----------------- | ------------------- | ---------------------- |
| **클래스 / 구조체**    | `PascalCase`      | `QuestController`   | 명사형, 역할 명시      |
| **인터페이스**         | `IPascalCase`     | `IQuestService`     | `I` 접두사 필수        |
| **메서드**             | `PascalCase`      | `TryAcceptQuest()`  | 동사형 사용            |
| **프로퍼티**           | `PascalCase`      | `QuestId`           | -                      |
| **열거형(Enum) 상수**  | `SCREAMING_SNAKE` | `QUEST_IN_PROGRESS` | 의미가 드러나도록 명명 |
| **상수(const)**        | `SCREAMING_SNAKE` | `SAVE_KEY_FLAGS`    | `static readonly` 포함 |
| **멤버 변수(Private)** | `_camelCase`      | `_moveSpeed`        | `_` 접두사 사용        |
| **지역 / 매개 변수**   | `camelCase`       | `targetPos`         | -                      |

---

## 4. 인스펙터 및 필드 규칙

### 4.1 멤버 변수 및 프로퍼티

- **Private 필드**: `_` 접두사를 사용하며, 외부 노출이 필요하면 프로퍼티로 감쌉니다.
- **읽기 전용 노출**: 외부에서 값만 읽으면 되는 경우 표현식 프로퍼티(`public int Hp => _hp;`)로 간결하게 노출합니다.
- **외부 쓰기 제어**: 외부 변경이 필요하면 `get; private set;` 또는 명시적 set 로직으로 캡슐화를 유지합니다.

### 4.2 인스펙터 속성 (Attributes)

- **[SerializeField]**: Private 필드에 사용해 인스펙터에 노출합니다. `public` 필드 직접 노출은 지양합니다.
- **[Tooltip]**: 모든 직렬화 필드에 작성하여 역할을 명시합니다. (장황해지지 않도록 "한 문장" 기본)
- **그룹화**:
  - **Odin Inspector 사용 시**: `[TabGroup("Group")]`, `[BoxGroup("Group")]`, `[FoldoutGroup("Group")]` 사용
  - **Odin 미사용(순수 Unity) 시**: `[Header("Group")]` 사용
- **검증/편의 기능**: Odin의 `[Required]`(필수 참조 누락 경고), `[Button]`(에디터 테스트 버튼)을 적극 활용해 인스펙터 설정 실수를 조기에 잡습니다.

---

## 5. 표준 코드 예시 (Best Practice)

아래는 위 규칙들을 종합 적용한 표준 컨트롤러 예시입니다. (DI/Init, 인터페이스 계약, 가드절, Try 패턴, Easy Save, DOTween, UniTask, Odin 그룹화 포함)

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: interface::IExampleService
// 설명: Example 시스템의 외부 공개 계약
//==============================================================================
public interface IExampleService
{
    /// <summary>점수를 추가합니다.</summary>
    /// <param name="amount">추가할 점수입니다.</param>
    /// <returns>추가에 성공하면 true입니다.</returns>
    bool TryAddScore(int amount);
}

//==============================================================================
// 기능: class::ExampleController
// 설명: 표준 컨벤션을 종합 적용한 컨트롤러 예시
//==============================================================================
[DisallowMultipleComponent]
public class ExampleController : MonoBehaviour, IExampleService
{
    //==============================================================================
    // 상수
    //==============================================================================

    // Easy Save 저장 키입니다. 매직 스트링 방지를 위해 상수로 관리합니다.
    private const string SAVE_KEY_SCORE = "example.score";

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Refs")]
    [Tooltip("점수 변동 시 연출할 팝업 CanvasGroup입니다.")]
    [Required]
    [SerializeField]
    private CanvasGroup _popupCanvasGroup;

    [TabGroup("Settings")]
    [Tooltip("팝업 페이드 연출 시간(초)입니다.")]
    [SerializeField]
    private float _popupFadeDuration = 0.3f;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 점수입니다.
    private int _score;

    // 진행 중인 팝업 페이드 트윈 핸들입니다.
    private Tween _popupTween;

    // 점수 변동을 기록할 진행 상태 서비스입니다.
    private IProgressService _progressService;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>현재 점수입니다.</summary>
    public int Score => _score;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>GameRoot에서 호출하는 초기화 함수입니다.</summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    public void Init(IProgressService progressService)
    {
        _progressService = progressService;
        Load();
    }

    //==============================================================================
    // 공개 API (IExampleService)
    //==============================================================================

    /// <summary>점수를 추가하고 팝업 연출을 재생합니다.</summary>
    /// <param name="amount">추가할 점수입니다.</param>
    /// <returns>추가에 성공하면 true입니다.</returns>
    public bool TryAddScore(int amount)
    {
        // 0 이하 가산은 의미가 없으므로 무시합니다.
        if (amount <= 0)
        {
            return false;
        }

        _score += amount;
        PlayPopup();
        return true;
    }

    //==============================================================================
    // 저장 / 로드 (Easy Save)
    //==============================================================================

    /// <summary>현재 점수를 디스크에 저장합니다.</summary>
    public void Save()
    {
        ES3.Save(SAVE_KEY_SCORE, _score);
    }

    /// <summary>저장된 점수가 있으면 불러옵니다.</summary>
    public void Load()
    {
        if (ES3.KeyExists(SAVE_KEY_SCORE) == false)
        {
            return;
        }

        _score = ES3.Load(SAVE_KEY_SCORE, _score);
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>점수 팝업을 페이드 인 → 아웃으로 연출합니다.</summary>
    private void PlayPopup()
    {
        // 참조 누락 시 크래시 대신 안전하게 종료합니다.
        if (_popupCanvasGroup == null)
        {
            Debug.LogWarning("[ExampleController] 팝업 CanvasGroup이 연결되지 않았습니다.");
            return;
        }

        // 이전 트윈이 남아 있으면 정리해 연출 충돌을 막습니다.
        _popupTween?.Kill();

        _popupCanvasGroup.alpha = 0.0f;
        _popupTween = _popupCanvasGroup
            .DOFade(1.0f, _popupFadeDuration)
            .SetLoops(2, LoopType.Yoyo) // 인 → 아웃
            .SetLink(gameObject);       // 오브젝트 파괴 시 자동 Kill
    }
}
```

> **참고**: 본 문서는 팀 협의를 통해 업데이트될 수 있으며, 수정 시 팀원 전체 공유가 필요합니다.
