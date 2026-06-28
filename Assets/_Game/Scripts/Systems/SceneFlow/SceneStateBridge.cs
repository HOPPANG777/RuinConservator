using UnityEngine;
using UnityEngine.SceneManagement;

//==============================================================================
// 기능: class::SceneStateBridge
// 설명: StageFlow Capture/Restore 이벤트와 ProgressController, 씬 오브젝트를 중개
//==============================================================================
// NOTE:
//  - 싱글톤이 아닙니다. GameRoot.Init()에서 StageFlow/Progress 참조를 주입받습니다.
//  - StageFlowController와 직접 이벤트 구독으로 연결됩니다.
//==============================================================================
[DisallowMultipleComponent]
public class SceneStateBridge : MonoBehaviour
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 씬 전환 이벤트를 구독할 StageFlowController 참조입니다.
    private StageFlowController _stageFlowController;

    // 진행 상태 저장소 참조입니다.
    private IProgressService _progressService;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출해 StageFlow/Progress 참조를 주입하고 이벤트를 구독합니다.
    /// </summary>
    /// <param name="stageFlowController">씬 전환 컨트롤러입니다.</param>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    public void Init(StageFlowController stageFlowController, IProgressService progressService)
    {
        // 이미 구독 중이면 중복 구독을 방지합니다.
        UnsubscribeEvents();

        _stageFlowController = stageFlowController;
        _progressService = progressService;

        // 필수 참조가 없으면 구독하지 않습니다.
        if (_stageFlowController == null || _progressService == null)
        {
            return;
        }

        _stageFlowController.OnStageCaptureRequested += HandleStageCapture;
        _stageFlowController.OnStageRestoreRequested += HandleStageRestore;
    }

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 오브젝트 파괴 시 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    //==============================================================================
    // 이벤트 핸들러
    //==============================================================================

    /// <summary>
    /// 씬 언로드 직전 현재 스테이지의 ISceneStateContributor 상태를 수집합니다.
    /// </summary>
    /// <param name="stageNumber">떠나는 스테이지 번호입니다.</param>
    private void HandleStageCapture(int stageNumber)
    {
        // 유효하지 않은 스테이지 번호는 무시합니다.
        if (stageNumber < 0 || _progressService == null)
        {
            return;
        }

        ISceneStateContributor[] contributors = FindSceneStateContributorsInActiveScene();

        for (int i = 0; i < contributors.Length; i++)
        {
            ISceneStateContributor contributor = contributors[i];

            // ObjectId가 없으면 저장 키를 만들 수 없으므로 건너뜁니다.
            if (string.IsNullOrWhiteSpace(contributor.ObjectId))
            {
                continue;
            }

            string stateJson = contributor.CaptureState();

            // 빈 상태는 저장하지 않습니다.
            if (string.IsNullOrWhiteSpace(stateJson))
            {
                continue;
            }

            _progressService.SetObjectState(stageNumber, contributor.ObjectId, stateJson);
        }

        Debug.Log($"[SceneStateBridge] Capture 완료: stage={stageNumber}, contributors={contributors.Length}");
    }

    /// <summary>
    /// 새 스테이지 로드 직후 저장된 오브젝트 상태를 적용하고 스폰 지점에 배치합니다.
    /// </summary>
    /// <param name="data">씬 전환 handoff 데이터입니다.</param>
    private void HandleStageRestore(SceneTransferData data)
    {
        // handoff 데이터가 없으면 복구할 대상을 알 수 없으므로 종료합니다.
        if (data == null || _progressService == null)
        {
            return;
        }

        int stageNumber = data.enteringStageNumber;

        // 유효한 스테이지 번호일 때만 오브젝트 상태를 복원합니다.
        if (stageNumber >= 0)
        {
            ApplyObjectStates(stageNumber);
        }

        ApplySpawnPoint(data.targetSpawnPointId);

        Debug.Log($"[SceneStateBridge] Restore 완료: stage={stageNumber}, spawn={data.targetSpawnPointId}");
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 이벤트 구독을 해제합니다.
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (_stageFlowController == null)
        {
            return;
        }

        _stageFlowController.OnStageCaptureRequested -= HandleStageCapture;
        _stageFlowController.OnStageRestoreRequested -= HandleStageRestore;
    }

    /// <summary>
    /// 현재 액티브 씬에서 ISceneStateContributor 구현체를 수집합니다.
    /// </summary>
    /// <returns>액티브 씬에 있는 contributor 배열입니다.</returns>
    private ISceneStateContributor[] FindSceneStateContributorsInActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        // contributor 수를 미리 알 수 없으므로 임시 리스트 대신 고정 배열 + 카운트를 사용합니다.
        ISceneStateContributor[] buffer = new ISceneStateContributor[64];
        int count = 0;

        for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
        {
            MonoBehaviour[] behaviours = rootObjects[rootIndex].GetComponentsInChildren<MonoBehaviour>(true);

            for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
            {
                if (behaviours[behaviourIndex] is ISceneStateContributor contributor)
                {
                    // 버퍼가 가득 차면 더 이상 추가하지 않습니다.
                    if (count >= buffer.Length)
                    {
                        Debug.LogWarning("[SceneStateBridge] ISceneStateContributor가 64개를 초과했습니다.");
                        break;
                    }

                    buffer[count] = contributor;
                    count++;
                }
            }
        }

        // 실제 사용 개수만큼 잘라 반환합니다.
        ISceneStateContributor[] result = new ISceneStateContributor[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = buffer[i];
        }

        return result;
    }

    /// <summary>
    /// 지정 스테이지의 저장된 오브젝트 상태를 씬 contributor에 적용합니다.
    /// </summary>
    /// <param name="stageNumber">복원할 스테이지 번호입니다.</param>
    private void ApplyObjectStates(int stageNumber)
    {
        ISceneStateContributor[] contributors = FindSceneStateContributorsInActiveScene();

        for (int i = 0; i < contributors.Length; i++)
        {
            ISceneStateContributor contributor = contributors[i];

            if (string.IsNullOrWhiteSpace(contributor.ObjectId))
            {
                continue;
            }

            if (!_progressService.TryGetObjectState(stageNumber, contributor.ObjectId, out string stateJson))
            {
                continue;
            }

            contributor.ApplyState(stateJson);
        }
    }

    /// <summary>
    /// targetSpawnPointId에 해당하는 SpawnPoint를 찾아 플레이어(또는 카메라 타겟)를 배치합니다.
    /// </summary>
    /// <param name="targetSpawnPointId">목적지 스폰 지점 ID입니다.</param>
    private void ApplySpawnPoint(string targetSpawnPointId)
    {
        // 스폰 ID가 없으면 배치할 필요가 없습니다.
        if (string.IsNullOrWhiteSpace(targetSpawnPointId))
        {
            return;
        }

        SpawnPoint spawnPoint = FindSpawnPoint(targetSpawnPointId);

        if (spawnPoint == null)
        {
            Debug.LogWarning($"[SceneStateBridge] SpawnPoint를 찾을 수 없습니다: {targetSpawnPointId}");
            return;
        }

        UnitPlayer player = Object.FindFirstObjectByType<UnitPlayer>();

        if (player != null)
        {
            player.transform.SetPositionAndRotation(spawnPoint.Position, spawnPoint.Rotation);

            if (GameRoot.Instance != null && GameRoot.Instance.CameraController != null)
            {
                GameRoot.Instance.CameraController.Follow(player);
            }

            return;
        }

        // 플레이어가 없으면 카메라 기본 타겟을 스폰 위치로 이동합니다.
        if (GameRoot.Instance != null && GameRoot.Instance.CameraController != null)
        {
            Debug.Log($"[SceneStateBridge] 플레이어 없음 — 스폰 위치 확인: {spawnPoint.Position}");
        }
    }

    /// <summary>
    /// 액티브 씬에서 SpawnPointId가 일치하는 SpawnPoint를 찾습니다.
    /// </summary>
    /// <param name="spawnPointId">찾을 스폰 지점 ID입니다.</param>
    /// <returns>일치하는 SpawnPoint, 없으면 null입니다.</returns>
    private SpawnPoint FindSpawnPoint(string spawnPointId)
    {
        SpawnPoint[] spawnPoints = Object.FindObjectsByType<SpawnPoint>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i].SpawnPointId == spawnPointId)
            {
                return spawnPoints[i];
            }
        }

        return null;
    }
}
