using UnityEngine;

//==============================================================================
// 파일: Singleton.cs
// 설명: 프로젝트 내 유니티 MonoBehaviour 기반 싱글톤 컴포넌트의 베이스 클래스
//==============================================================================

/// <summary>
/// MonoBehaviour 기반의 제네릭 싱글톤 베이스 클래스입니다.
/// </summary>
/// <remarks>
/// ⚠️ [코딩 컨벤션 1.3] 프로젝트 전역 싱글톤은 오직 'GameRoot' 하나만 두는 것이 원칙입니다.
/// 특정 컨트롤러(도메인 관리) 클래스들은 본 클래스를 상속받지 말고, GameRoot의 멤버 필드로 등록하여 사용하세요.
/// </remarks>
/// <typeparam name="T">MonoBehaviour를 상속받은 싱글톤 타입</typeparam>
[DisallowMultipleComponent]
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    //==============================================================================
    // 내부 상태 (Static Fields)
    //==============================================================================

    /// <summary>싱글톤 인스턴스의 static 참조 필드입니다.</summary>
    private static T _instance;

    /// <summary>애플리케이션이 종료(Quit) 중인지 여부를 나타내는 플래그입니다.</summary>
    private static bool _isQuitting;

    /// <summary>씬 내에서 기존 인스턴스 검색을 실행했는지 여부입니다.</summary>
    private static bool _searchedInScene;

    /// <summary>인스턴스가 없을 때 자동 생성(Create On Demand)을 허용할지 여부입니다.</summary>
    private static bool _autoCreateIfNotFound = true;

    /// <summary>자동 생성 시 씬에 배치될 GameObject의 이름입니다.</summary>
    private static string _autoCreatedObjectName = $"[Singleton] {typeof(T).Name}";

    /// <summary>씬이 변경되어도 파괴되지 않고 유지(DontDestroyOnLoad)할지 여부입니다.</summary>
    private static bool _persistAcrossScenes = true;

    /// <summary>멀티스레드 및 레이스 컨디션 방지를 위한 동기화 lock 객체입니다.</summary>
    private static readonly object _lockObject = new object();

    //==============================================================================
    // 타입별 정책 설정 API
    //==============================================================================

    /// <summary>
    /// 파생 클래스의 static 생성자에서 싱글톤의 세부 작동 정책을 설정합니다.
    /// </summary>
    /// <param name="allowAutoCreate">인스턴스가 없을 때 자동 생성 여부</param>
    /// <param name="autoCreatedObjectName">자동 생성 시 런타임 씬 오브젝트 이름</param>
    /// <param name="persistAcrossScenes">DontDestroyOnLoad 적용 여부</param>
    protected static void ConfigureSingleton(
        bool allowAutoCreate,
        string autoCreatedObjectName = null,
        bool persistAcrossScenes = true)
    {
        _autoCreateIfNotFound = allowAutoCreate;
        _persistAcrossScenes = persistAcrossScenes;

        if (!string.IsNullOrWhiteSpace(autoCreatedObjectName))
        {
            _autoCreatedObjectName = autoCreatedObjectName;
        }
    }

    //==============================================================================
    // 외부 접근 API (Properties & Methods)
    //==============================================================================

    /// <summary>
    /// 싱글톤 고유 인스턴스에 접근하는 프로퍼티입니다. (캡슐화 및 정책 적용)
    /// </summary>
    public static T Instance
    {
        get
        {
            // 종료 프로세스 진행 중에는 메모리 릭 및 유령 오브젝트 생성을 방지하기 위해 생성을 무시합니다.
            if (_isQuitting)
            {
                Log($"Instance 요청 무시 (애플리케이션 종료 중): {typeof(T).Name}");
                return null;
            }

            if (_instance != null)
            {
                return _instance;
            }

            lock (_lockObject)
            {
                if (_instance != null)
                {
                    return _instance;
                }

                // 1) 씬에 이미 배치되어 있는 기존 컴포넌트를 최초 1회 탐색합니다.
                if (!_searchedInScene)
                {
                    _searchedInScene = true;

#if UNITY_2023_1_OR_NEWER
                    _instance = UnityEngine.Object.FindFirstObjectByType<T>();
#else
                    _instance = UnityEngine.Object.FindObjectOfType<T>();
#endif

                    if (_instance != null)
                    {
                        Log($"씬에서 기존 인스턴스 발견: {GetHierarchyPath(_instance.gameObject)}");

                        if (_persistAcrossScenes)
                        {
                            DontDestroyOnLoad(_instance.gameObject);
                        }
                        return _instance;
                    }
                }

                // 2) 씬에 존재하지 않을 경우, 타입별 정책에 따라 자동 생성 여부를 결정합니다.
                if (!_autoCreateIfNotFound)
                {
                    LogWarning($"Instance 요청 실패 (자동 생성 제한 타입): {typeof(T).Name} - 프리팹 또는 씬에 미리 배치되어야 합니다.");
                    return null;
                }

                return CreateAutoInstance();
            }
        }
    }

    /// <summary>
    /// 현재 싱글톤 인스턴스가 런타임 상에 메모리 할당 및 유효하게 존재하는지 여부만 빠르게 확인합니다.
    /// </summary>
    public static bool HasInstance
    {
        get
        {
            return _instance != null && !_isQuitting;
        }
    }

    /// <summary>
    /// 인스턴스를 안전하게 가져오려고 시도하며, 자동 생성을 유발하지 않고 유효성 여부를 반환합니다.
    /// </summary>
    /// <param name="value">출력될 싱글톤 인스턴스 참조</param>
    /// <returns>인스턴스 존재 여부 (True / False)</returns>
    public static bool TryGetInstance(out T value)
    {
        if (_isQuitting)
        {
            value = null;
            return false;
        }

        value = _instance;
        if (value != null)
        {
            return true;
        }

        if (!_searchedInScene)
        {
            _searchedInScene = true;

#if UNITY_2023_1_OR_NEWER
            value = UnityEngine.Object.FindFirstObjectByType<T>();
#else
            value = UnityEngine.Object.FindObjectOfType<T>();
#endif
            _instance = value;
        }

        return value != null;
    }

    //==============================================================================
    // 유니티 생명주기 (Unity Lifecycle)
    //==============================================================================

    /// <summary>
    /// 객체 생성 시 싱글톤 유일성을 검증하고 등록 및 중복 제거를 처리합니다.
    /// </summary>
    protected virtual void Awake()
    {
        if (_isQuitting)
        {
            Destroy(gameObject);
            return;
        }

        if (_instance == null)
        {
            _instance = this as T;
            _searchedInScene = true;

            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            Log($"싱글톤 등록 완료: {GetHierarchyPath(gameObject)}");
        }
        else if (_instance != this)
        {
            LogWarning($"중복 싱글톤 감지 및 예외 파괴 완료: {GetHierarchyPath(gameObject)} / 원본 인스턴스: {GetHierarchyPath(_instance.gameObject)}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 유니티 애플리케이션 종료 시점에 런타임 유령 오브젝트가 생성되는 부작용을 방지합니다.
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    /// <summary>
    /// 객체 파괴 시 싱글톤 인스턴스의 static 참조를 초기화하여 메모리를 해제합니다.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            Log($"싱글톤 참조 해제(파괴): {GetHierarchyPath(gameObject)}");
            _instance = null;
        }
    }

    //==============================================================================
    // 내부 유틸리티 로직 (Private Utilities)
    //==============================================================================

    /// <summary>
    /// 씬 내에 인스턴스가 없을 때 런타임 상에서 동적으로 고유 게임오브젝트와 컴포넌트를 생성합니다.
    /// </summary>
    /// <returns>생성된 싱글톤 컴포넌트 인스턴스</returns>
    private static T CreateAutoInstance()
    {
        GameObject autoGameObject = new GameObject(_autoCreatedObjectName);
        T createdComponent = autoGameObject.AddComponent<T>();

        Log($"런타임 동적 자동 생성 완료: {GetHierarchyPath(autoGameObject)}");
        return createdComponent;
    }

    /// <summary>
    /// 디버깅 가독성을 향상시키기 위해 해당 게임오브젝트의 하이러키 전체 경로 문자열을 빌드합니다.
    /// </summary>
    /// <param name="go">조회할 게임오브젝트</param>
    /// <returns>하이라키 부모-자식 경로 구조 문자열</returns>
    private static string GetHierarchyPath(GameObject go)
    {
        if (go == null)
        {
            return "(null)";
        }

        Transform currentTransform = go.transform;
        string hierarchyPath = currentTransform.name;

        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
            hierarchyPath = $"{currentTransform.name}/{hierarchyPath}";
        }

        return hierarchyPath;
    }

    /// <summary>
    /// 에디터 혹은 개발 빌드 환경에서만 조건부로 출력되는 싱글톤 내부 전용 안전 로그 함수입니다.
    /// </summary>
    /// <param name="message">출력할 로그 내용</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void Log(string message)
    {
        Debug.Log($"[{typeof(T).Name}] {message}");
    }

    /// <summary>
    /// 에디터 혹은 개발 빌드 환경에서만 조건부로 출력되는 싱글톤 내부 전용 예외 경고 로그 함수입니다.
    /// </summary>
    /// <param name="message">출력할 경고 내용</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[{typeof(T).Name}] {message}");
    }
}