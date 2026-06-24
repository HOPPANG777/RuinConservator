using UnityEngine;
using UnityEngine.InputSystem;

//==============================================================================
// 기능: class::PlayerInteractionController
// 설명: 플레이어 주변 상호작용 대상 탐지, 하이라이트, 입력 처리
//==============================================================================
// NOTE:
//  - IInteractable / IHighlightable 인터페이스만 참조합니다.
//  - 대화 등으로 입력이 막히면 SetCanInteract(false)를 호출합니다.
//==============================================================================
public class PlayerInteractionController : MonoBehaviour
{
    //==============================================================================
    // 직렬화 필드 (인스펙터 노출)
    //==============================================================================

    [Header("Interaction")]
    [Tooltip("상호작용 탐지 기준 위치입니다. 비어 있으면 본 Transform을 사용합니다.")]
    [SerializeField]
    private Transform _origin;

    [Header("Interaction")]
    [Tooltip("상호작용 대상이 속한 레이어입니다.")]
    [SerializeField]
    private LayerMask _layer;

    [Header("Interaction")]
    [Tooltip("상호작용을 탐지할 반경입니다.")]
    [SerializeField]
    private float _interactRange = 1.0f;

    [Header("Interaction")]
    [Tooltip("상호작용 입력 액션입니다.")]
    [SerializeField]
    private InputActionReference _interactAction;

    [Header("Interaction")]
    [Tooltip("상호작용 입력 및 탐지 허용 여부입니다. 대화 중에는 false로 설정합니다.")]
    [SerializeField]
    private bool _canInteract = true;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // OverlapCircleNonAlloc 결과를 담는 고정 버퍼입니다. (GC 할당 방지)
    private readonly Collider2D[] _detectResults = new Collider2D[10];

    // 현재 포커스된 상호작용 대상입니다.
    private IInteractable _currentTarget;

    // 현재 하이라이트 중인 대상입니다. (_currentTarget과 다를 수 있음)
    private IHighlightable _currentHighlightTarget;

    // OverlapCircleNonAlloc에 사용할 레이어 필터입니다.
    private ContactFilter2D _contactFilter;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 탐지 필터와 입력 액션을 초기화합니다.
    /// </summary>
    private void Awake()
    {
        _contactFilter.useLayerMask = true;
        _contactFilter.layerMask = _layer;
        _contactFilter.useTriggers = true;
    }

    /// <summary>
    /// 상호작용 입력 액션을 활성화합니다.
    /// </summary>
    private void OnEnable()
    {
        if (_interactAction != null)
        {
            _interactAction.action.Enable();
        }
    }

    /// <summary>
    /// 상호작용 입력 액션을 비활성화하고 포커스를 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (_interactAction != null)
        {
            _interactAction.action.Disable();
        }

        ClearTarget();
    }

    /// <summary>
    /// 매 프레임 상호작용 대상 갱신 및 입력을 처리합니다.
    /// </summary>
    private void Update()
    {
        // 상호작용이 금지된 상태면 대상을 해제하고 종료합니다.
        if (_canInteract == false)
        {
            ClearTarget();
            return;
        }

        UpdateInteractionTarget();
        HandleInteractionInput();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 상호작용 입력 및 탐지 허용 여부를 설정합니다.
    /// </summary>
    /// <param name="canInteract">허용 여부입니다.</param>
    public void SetCanInteract(bool canInteract)
    {
        _canInteract = canInteract;

        // 상호작용이 금지되면 현재 대상과 하이라이트를 즉시 해제합니다.
        if (_canInteract == false)
        {
            ClearTarget();
        }
    }

    //==============================================================================
    // 내부 로직 — 탐지
    //==============================================================================

    /// <summary>
    /// 범위 내 상호작용 대상을 갱신합니다.
    /// </summary>
    private void UpdateInteractionTarget()
    {
        FindNearestInteractable();
    }

    /// <summary>
    /// 범위 내 가장 가까운 IInteractable을 찾아 대상을 변경합니다.
    /// </summary>
    private void FindNearestInteractable()
    {
        Vector2 originPosition = GetOriginPosition();

        // NonAlloc으로 주변 콜라이더를 수집합니다.
        int hitCount = Physics2D.OverlapCircle(
            originPosition,
            _interactRange,
            _contactFilter,
            _detectResults);

        IInteractable nearestInteractable = null;
        IHighlightable nearestHighlight = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = _detectResults[i];

            // 비어 있는 슬롯은 건너뜁니다.
            if (hitCollider == null)
            {
                continue;
            }

            IInteractable interactable = hitCollider.GetComponent<IInteractable>();

            // 상호작용 불가 오브젝트는 후보에서 제외합니다.
            if (interactable == null)
            {
                continue;
            }

            Vector2 targetPosition = hitCollider.transform.position;
            float sqrDistance = (targetPosition - originPosition).sqrMagnitude;

            // 더 가까운 대상만 후보로 갱신합니다.
            if (sqrDistance >= nearestSqrDistance)
            {
                continue;
            }

            nearestSqrDistance = sqrDistance;
            nearestInteractable = interactable;
            nearestHighlight = hitCollider.GetComponent<IHighlightable>();
        }

        // 범위 안에 대상이 없으면 포커스를 해제합니다.
        if (nearestInteractable == null)
        {
            if (_currentTarget != null)
            {
                ClearTarget();
            }

            return;
        }

        // 같은 대상이면 하이라이트/전환을 반복하지 않습니다.
        if (nearestInteractable == _currentTarget)
        {
            return;
        }

        ChangeTarget(nearestInteractable, nearestHighlight);
    }

    /// <summary>
    /// 상호작용 대상을 변경하고 하이라이트를 갱신합니다.
    /// </summary>
    /// <param name="newTarget">새 상호작용 대상입니다.</param>
    /// <param name="newHighlightTarget">새 하이라이트 대상입니다. null이면 하이라이트하지 않습니다.</param>
    private void ChangeTarget(IInteractable newTarget, IHighlightable newHighlightTarget)
    {
        // 이전 하이라이트를 끕니다.
        if (_currentHighlightTarget != null)
        {
            _currentHighlightTarget.SetHighlight(false);
        }

        _currentTarget = newTarget;
        _currentHighlightTarget = newHighlightTarget;

        // 새 대상에 하이라이트를 켭니다.
        if (_currentHighlightTarget != null)
        {
            _currentHighlightTarget.SetHighlight(true);
        }
    }

    /// <summary>
    /// 현재 상호작용 대상과 하이라이트를 모두 해제합니다.
    /// </summary>
    private void ClearTarget()
    {
        if (_currentHighlightTarget != null)
        {
            _currentHighlightTarget.SetHighlight(false);
        }

        _currentTarget = null;
        _currentHighlightTarget = null;
    }

    //==============================================================================
    // 내부 로직 — 입력
    //==============================================================================

    /// <summary>
    /// 상호작용 입력을 감지해 TryInteract를 호출합니다.
    /// </summary>
    private void HandleInteractionInput()
    {
        // 입력 액션이 연결되지 않았으면 처리할 수 없습니다.
        if (_interactAction == null)
        {
            return;
        }

        // 이번 프레임에 상호작용 입력이 들어왔을 때만 실행합니다.
        if (_interactAction.action.WasPressedThisFrame())
        {
            TryInteract();
        }
    }

    /// <summary>
    /// 현재 포커스된 대상에 상호작용을 시도합니다.
    /// </summary>
    private void TryInteract()
    {
        // 포커스된 대상이 없으면 아무 것도 하지 않습니다.
        if (_currentTarget == null)
        {
            return;
        }

        _currentTarget.Interact();
    }

    //==============================================================================
    // 유틸
    //==============================================================================

    /// <summary>
    /// 상호작용 탐지 기준 월드 좌표를 반환합니다.
    /// </summary>
    /// <returns>탐지 원의 중심 좌표입니다.</returns>
    private Vector2 GetOriginPosition()
    {
        if (_origin != null)
        {
            return _origin.position;
        }

        return transform.position;
    }
}
