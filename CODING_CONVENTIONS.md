---

# 📝 Unity Game Development Coding Conventions (v2.2)

본 프로젝트의 코드 일관성, 런타임 성능, 그리고 유지보수 효율을 극대화하기 위해 정의된 가이드라인입니다. 

모든 팀원은 코드를 작성하거나 리뷰할 때 반드시 이 문서를 기준으로 합니다.

## 1. 핵심 설계 원칙 (Core Architecture)

### 1.1 의존성 및 결합도 (DI & Low Coupling)

- **의존성 주입(DI) 권장**: 클래스 내부에서 직접 `new`를 통해 객체를 생성하거나 `GameObject.Find` 계열 함수를 사용하는 것을 지양합니다.
- **주입 설계**: 생성자나 `Construct` 메서드를 통해 외부에서 주입받도록 설계하세요.
- **낮은 결합도**: 각 클래스는 자신의 역할에만 집중하며, 다른 클래스의 세부 구현을 알지 못하도록 인터페이스(Interface)를 적극 활용합니다.

### 1.2 상속 구조 (Inheritance Limit)

- **1단계 상속 원칙**: 상속은 최대 1단계까지만 허용합니다 (예: `UnitBase` → `Player`).
- **구성(Composition) 중심**: 2단계 이상의 깊은 상속 구조 대신, 기능을 작은 단위의 클래스로 분리하여 필요한 기능을 조립하는 방식을 사용합니다. 이는 '클래스 폭발' 현상을 방지합니다.

### 1.3 매니저 & 컨트롤러 구조 (Manager & Controller)

- **싱글턴은 오직 1개만 둔다**: 프로젝트 전역 싱글턴은 `GameRoot`(혹은 동등한 역할의 단일 매니저) **하나만** 둡니다.
- **컨트롤러는 `GameRoot`의 멤버로 둔다**: 흔히 “매니저”로 두는 전역 관리 클래스들은 **싱글턴으로 만들지 않고**, `GameRoot` 내부 멤버(필드/프로퍼티)로 보관되는 **Controller**로 작성합니다.
- **컨트롤러 접근 경로 단일화**: 어떤 컨트롤러도 외부에서 직접 생성/참조하지 않습니다. 컨트롤러 접근은 **반드시** `GameRoot.Instance`를 통해서만 합니다.
  - 예: `GameRoot.Instance.AudioController`, `GameRoot.Instance.StageController`
- **의도/책임 명확화**: “싱글톤 스크립트는 하나만” 유지하여 전역 상태의 책임소재를 명확하게 하고, 전역 접근 경로를 단일화합니다.
- **역할 기준 네이밍**: 특정 로직을 “관리”하는 클래스는 반드시 `Manager` 또는 `Controller` 접미사를 붙입니다.

---

## 2. 프로그래밍 가이드 및 최적화

### 2.1 시간/대기/비동기 처리 (Update/for 우선, UniTask는 “필수 비동기”에만)

- **Coroutine 금지**: 유니티 기본 코루틴(`StartCoroutine`)은 사용하지 않습니다.
- **대기/타이밍은 Update로 대체**: “몇 초 뒤 실행”, “N프레임 후 실행”, “조건이 만족되면 다음 단계 진행” 같은 로직은 아래 방식으로 작성합니다.
  - **Update + 타이머**: `elapsed += Time.deltaTime` 형태로 시간 누적 후 분기
  - **Update + 상태 머신(State Machine)**: `State`에 따라 단계별 로직 분리 (가독성/디버깅 최적)
  - **for/while 분해**: 반복 작업이 “프레임에 걸쳐야 하는 이유”가 없다면 한 프레임에서 끝내고, 그렇지 않다면 Update에서 단계별로 쪼갭니다.
- **짧은 예시**: “1.5초 뒤 1회 실행”은 아래처럼 작성합니다.
  ```csharp
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
- **짧은 예시**: “단계별 진행”은 상태로 분리합니다.
  ```csharp
  private enum State { None, Preparing, Playing, Finished }
  private State _state;

  private void Update()
  {
      switch (_state)
      {
          case State.Preparing:
              // 준비 단계 로직...
              break;
          case State.Playing:
              // 진행 단계 로직...
              break;
      }
  }
  ```
- **UniTask는 “반드시 비동기여야 하는 곳”에만 사용**: 아래처럼 엔진/플랫폼/외부 리소스 특성상 `await`가 필요한 경우에만 사용합니다.
  - **씬/리소스 로딩**: `SceneManager.LoadSceneAsync`, Addressables 로드 등
  - **네트워크/웹 요청**: API 호출, 서버 통신 등
  - **파일 I/O**: 저장/로드, 로컬 파일 접근 등
  - **비동기 초기화**: 특정 의존 대상이 완료될 때까지 기다려야 하는 초기화 흐름 등
- **프레임 대기용 await 남용 금지**: `await UniTask.Yield()` / `await UniTask.DelayFrame()`로 “Update를 대체”하지 않습니다. 프레임 기반 흐름 제어는 Update/상태 머신으로 해결합니다.
- **Cancellation 필수 고려**: 오브젝트 생명주기와 함께 취소되어야 하는 비동기라면 `this.GetCancellationTokenOnDestroy()`(또는 명시적 토큰)로 취소 가능하게 설계합니다.

### 2.2 루프 최적화 (Loops)

- **for문 우선**: 리스트나 배열을 순회할 때, 불필요한 할당과 성능 저하 가능성을 줄이기 위해 `foreach` 대신 `for`문을 우선 고려합니다.

### 2.3 주석 규칙 (Comments & Documentation)

모든 **기능(Class), 메서드(Function), 변수(Field)**에는 역할을 설명하는 주석을 최소 한 줄 이상 작성합니다. 목적에 따라 아래의 규격을 준수합니다.

- **기본 한 줄 주석**: 기능, 변수, 짧은 로직 설명 시 사용합니다.
  ```csharp
  // 주석!
  ```
- **TODO / FIXME 주석**: 추가 작업이나 버그 수정이 필요한 곳에 명시합니다.
  ```csharp
  // TODO: 예외 처리 추가 필요
  // FIXME: 메모리 누수 발생 가능성 있음
  ```
- **문서화 주석**: 클래스나 핵심 메서드의 API 문서를 작성할 때 사용합니다.
  ```csharp
  /// <summary>
  /// 사용자 이름을 반환합니다.
  /// </summary>
  /// <param name="id">조회할 사용자의 ID</param>
  /// <returns>해당 ID를 가진 사용자의 이름</returns>
  public string GetUserName(int id);
  ```
- **구분선 주석**: 파일 내에서 큰 기능 단위나 영역을 나눌 때 사용합니다.
  ```csharp
  //==============================================================================
  // 데데이
  //==============================================================================
  ```
- **상세/블록 주석**: 핵심 기능에 대한 상세한 설명이나 세부 구현 사항을 나열할 때 사용합니다.
  ```csharp
  //==============================================================================
  // 기능: MyNameClass::Create
  // 설명: 객체를 생성하는 함수
  //==============================================================================
  // TODO: 세부 구현 사항
  //
  //==============================================================================
  ```

---

## 3. 네이밍 규칙 (Naming Conventions)

### 3.1 Manager / Controller 네이밍 규칙

- **Manager**: 싱글턴인 클래스에만 `Manager`를 붙입니다. (본 프로젝트에서는 사실상 `GameRoot`만 해당)
- **Controller**: 싱글턴이 아니지만 특정 로직(시스템/도메인)을 관리하는 클래스에 `Controller`를 붙입니다. 컨트롤러는 `GameRoot` 멤버로 두고 `GameRoot.Instance` 경유로만 접근합니다.


| 분류                 | 표기법 (Case)        | 예시                 | 비고              |
| ------------------ | ----------------- | ------------------ | --------------- |
| **클래스 / 구조체**      | `PascalCase`      | `PlayerController` | 명사형 사용, 역할 명시   |
| **인터페이스**          | `IPascalCase`     | `IMoveable`        | `I` 접두사 필수      |
| **메서드**            | `PascalCase`      | `TakeDamage()`     | 동사형 사용          |
| **열거형(Enum) 상수**   | `SCREAMING_SNAKE` | `TILE_WALL`        | 태그/의미가 드러나도록 명명 |
| **멤버 변수(Private)** | `_camelCase`      | `_moveSpeed`       | `_` 접두사 사용      |
| **지역/매개 변수**       | `camelCase`       | `targetPos`        | -               |


---

## 4. 인스펙터 및 필드 규칙

### 4.1 멤버 변수 및 프로퍼티

- **Private 필드**: `_` 접두사를 사용하며, 외부 노출이 필요하면 프로퍼티로 감쌉니다.
- **프로퍼티**: `get; set;` 로직을 명시하고 대소문자 규칙(`PascalCase`)을 적용하여 캡슐화를 강화합니다.

### 4.2 인스펙터 속성 (Attributes)

- **[SerializeField]**: Private 필드 바로 윗줄에 작성합니다.
- **[Tooltip]**: 모든 직렬화 필드에 작성하여 역할을 명시합니다. (툴팁이 과하게 장황해지지 않도록 “한 문장”을 기본으로 합니다.)
- **그룹화**:
  - **Odin Inspector 사용 시**: `[TabGroup("GroupName")]` 사용
  - **Odin 미사용 시**: `[Header("GroupName")]` 사용

---

## 5. 표준 코드 예시 (Best Practice)

아래는 위에서 정의된 규칙들을 종합적으로 적용한 표준 코드 예시입니다.

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using Sirenix.OdinInspector;

// 이동 기능을 정의하는 인터페이스
public interface IMoveable
{
    void Move();
}

// 1단계 상속: 모든 유닛의 베이스 클래스
public abstract class UnitBase : MonoBehaviour
{
    // 유닛 식별자
    protected string _unitId;
}

// 플레이어 컨트롤러: UnitBase를 상속하며 기능별 분리 준수
public class PlayerController : UnitBase
{
    // 이동 관련 설정을 모은 탭 그룹
    [TabGroup("Settings")]
    [Tooltip("캐릭터의 이동 속도")]
    [SerializeField]
    private float _moveSpeed;

    // 프로퍼티를 활용한 캡슐화 (get; set; 로직 명시)
    public float MoveSpeed
    {
        get => _moveSpeed;
        private set => _moveSpeed = value;
    }

    // 이동 로직 (의존성 주입 대상)
    private IMoveable _moveLogic;

    // 의존성 주입을 위한 생성 메서드
    public void Construct(IMoveable moveLogic)
    {
        _moveLogic = moveLogic;
    }

    // 컴포넌트 캐싱 및 초기화
    private void Awake()
    {
        _unitId = "Player_01";
    }

    // 비동기 초기화 로직 (정말로 await가 필요한 초기화에만 UniTask 사용)
    public async UniTask InitializeAsync()
    {
        // 1초 비동기 대기
        await UniTask.Delay(1000);
        Debug.Log("플레이어 초기화 완료");
    }

    // 데이터 처리 로직 (for문 활용)
    public void UpdateScore(int[] scores)
    {
        // 최적화를 위해 for문 사용
        for (int i = 0; i < scores.Length; i++)
        {
            Debug.Log($"점수 확인: {scores[i]}");
        }
    }
}
```

> **참고**: 본 문서는 팀 협의를 통해 업데이트될 수 있으며, 수정 시 팀원 전체 공유가 필요합니다.

