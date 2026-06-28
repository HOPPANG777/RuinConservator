using System.Collections.Generic;
using UnityEngine;

//==============================================================================
// 기능: class::QuestController
// 설명: 퀘스트 상태 전환, 목표 갱신, 보상 지급을 담당하는 컨트롤러
//==============================================================================
[DisallowMultipleComponent]
public class QuestController : MonoBehaviour, IQuestService
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Quest")]
    [Tooltip("런타임에서 ID로 조회할 수 있는 퀘스트 데이터 목록입니다.")]
    [SerializeField]
    private List<QuestDataSO> _questDatabase = new List<QuestDataSO>();

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 퀘스트 ID로 원본 데이터를 찾기 위한 캐시입니다.
    private readonly Dictionary<string, QuestDataSO> _questById = new Dictionary<string, QuestDataSO>();

    // 퀘스트 ID별 런타임 상태입니다.
    private readonly Dictionary<string, QuestState> _questStates = new Dictionary<string, QuestState>();

    // 목표/보상 판정에 사용할 Progress 서비스입니다.
    private IProgressService _progressService;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다.
    /// </summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    public void Init(IProgressService progressService)
    {
        _progressService = progressService;
        RebuildQuestDatabase();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 퀘스트 ID로 퀘스트 데이터를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <param name="questData">조회된 퀘스트 데이터입니다.</param>
    /// <returns>퀘스트 데이터를 찾으면 true입니다.</returns>
    public bool TryGetQuestData(string questId, out QuestDataSO questData)
    {
        EnsureQuestDatabase();
        questData = null;

        if (string.IsNullOrWhiteSpace(questId))
        {
            return false;
        }

        return _questById.TryGetValue(questId, out questData);
    }

    /// <summary>
    /// 퀘스트 상태를 조회합니다.
    /// </summary>
    /// <param name="questData">조회할 퀘스트 데이터입니다.</param>
    /// <returns>현재 퀘스트 상태입니다.</returns>
    public QuestState GetQuestState(QuestDataSO questData)
    {
        if (questData == null)
        {
            return QuestState.QUEST_NOT_STARTED;
        }

        return GetQuestState(questData.QuestId);
    }

    /// <summary>
    /// 퀘스트 상태를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <returns>현재 퀘스트 상태입니다.</returns>
    public QuestState GetQuestState(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            return QuestState.QUEST_NOT_STARTED;
        }

        if (_questStates.TryGetValue(questId, out QuestState state))
        {
            return state;
        }

        return QuestState.QUEST_NOT_STARTED;
    }

    /// <summary>
    /// 퀘스트를 수락 상태로 변경합니다.
    /// </summary>
    /// <param name="questData">수락할 퀘스트 데이터입니다.</param>
    /// <returns>상태 변경에 성공하면 true입니다.</returns>
    public bool TryAcceptQuest(QuestDataSO questData)
    {
        if (!TryResolveQuest(questData, null, out QuestDataSO resolvedQuest))
        {
            return false;
        }

        QuestState currentState = GetQuestState(resolvedQuest);

        if (currentState != QuestState.QUEST_NOT_STARTED)
        {
            return false;
        }

        SetQuestState(resolvedQuest, QuestState.QUEST_IN_PROGRESS);
        RefreshQuestProgress(resolvedQuest);

        return true;
    }

    /// <summary>
    /// 퀘스트를 수락 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">수락할 퀘스트 ID입니다.</param>
    /// <returns>상태 변경에 성공하면 true입니다.</returns>
    public bool TryAcceptQuest(string questId)
    {
        if (!TryResolveQuest(null, questId, out QuestDataSO questData))
        {
            return false;
        }

        return TryAcceptQuest(questData);
    }

    /// <summary>
    /// 퀘스트 목표 달성 여부를 확인하고 완료 상태로 갱신합니다.
    /// </summary>
    /// <param name="questData">갱신할 퀘스트 데이터입니다.</param>
    /// <returns>이번 호출로 완료 상태가 되었으면 true입니다.</returns>
    public bool RefreshQuestProgress(QuestDataSO questData)
    {
        if (!TryResolveQuest(questData, null, out QuestDataSO resolvedQuest))
        {
            return false;
        }

        if (GetQuestState(resolvedQuest) != QuestState.QUEST_IN_PROGRESS)
        {
            return false;
        }

        if (resolvedQuest.AreObjectivesSatisfied(_progressService) == false)
        {
            return false;
        }

        SetQuestState(resolvedQuest, QuestState.QUEST_COMPLETED);
        return true;
    }

    /// <summary>
    /// 퀘스트 목표 달성 여부를 확인하고 완료 상태로 갱신합니다.
    /// </summary>
    /// <param name="questId">갱신할 퀘스트 ID입니다.</param>
    /// <returns>이번 호출로 완료 상태가 되었으면 true입니다.</returns>
    public bool RefreshQuestProgress(string questId)
    {
        if (!TryResolveQuest(null, questId, out QuestDataSO questData))
        {
            return false;
        }

        return RefreshQuestProgress(questData);
    }

    /// <summary>
    /// 진행 중인 모든 퀘스트의 목표 달성 여부를 갱신합니다.
    /// </summary>
    /// <returns>이번 호출로 완료 상태가 된 퀘스트 수입니다.</returns>
    public int RefreshAllInProgressQuests()
    {
        EnsureQuestDatabase();

        int completedCount = 0;
        List<string> questIds = new List<string>(_questStates.Keys);

        for (int i = 0; i < questIds.Count; i++)
        {
            string questId = questIds[i];

            if (GetQuestState(questId) != QuestState.QUEST_IN_PROGRESS)
            {
                continue;
            }

            if (TryGetQuestData(questId, out QuestDataSO questData) && RefreshQuestProgress(questData))
            {
                completedCount++;
            }
        }

        return completedCount;
    }

    /// <summary>
    /// 완료 가능한 퀘스트를 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questData">완료할 퀘스트 데이터입니다.</param>
    /// <returns>완료 상태가 되었거나 이미 완료 상태면 true입니다.</returns>
    public bool TryCompleteQuest(QuestDataSO questData)
    {
        if (!TryResolveQuest(questData, null, out QuestDataSO resolvedQuest))
        {
            return false;
        }

        QuestState currentState = GetQuestState(resolvedQuest);

        if (currentState == QuestState.QUEST_COMPLETED)
        {
            return true;
        }

        if (currentState != QuestState.QUEST_IN_PROGRESS)
        {
            return false;
        }

        if (resolvedQuest.AreObjectivesSatisfied(_progressService) == false)
        {
            return false;
        }

        SetQuestState(resolvedQuest, QuestState.QUEST_COMPLETED);
        return true;
    }

    /// <summary>
    /// 완료 가능한 퀘스트를 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">완료할 퀘스트 ID입니다.</param>
    /// <returns>완료 상태가 되었거나 이미 완료 상태면 true입니다.</returns>
    public bool TryCompleteQuest(string questId)
    {
        if (!TryResolveQuest(null, questId, out QuestDataSO questData))
        {
            return false;
        }

        return TryCompleteQuest(questData);
    }

    /// <summary>
    /// 완료된 퀘스트 보상을 지급하고 보상 수령 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questData">보상을 받을 퀘스트 데이터입니다.</param>
    /// <returns>이번 호출로 보상을 지급했으면 true입니다.</returns>
    public bool TryClaimReward(QuestDataSO questData)
    {
        if (!TryResolveQuest(questData, null, out QuestDataSO resolvedQuest))
        {
            return false;
        }

        QuestState currentState = GetQuestState(resolvedQuest);

        if (currentState == QuestState.QUEST_REWARDED)
        {
            return false;
        }

        if (currentState == QuestState.QUEST_IN_PROGRESS)
        {
            RefreshQuestProgress(resolvedQuest);
            currentState = GetQuestState(resolvedQuest);
        }

        if (currentState != QuestState.QUEST_COMPLETED)
        {
            return false;
        }

        resolvedQuest.ApplyRewards(_progressService);
        SetQuestState(resolvedQuest, QuestState.QUEST_REWARDED);

        return true;
    }

    /// <summary>
    /// 완료된 퀘스트 보상을 지급하고 보상 수령 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">보상을 받을 퀘스트 ID입니다.</param>
    /// <returns>이번 호출로 보상을 지급했으면 true입니다.</returns>
    public bool TryClaimReward(string questId)
    {
        if (!TryResolveQuest(null, questId, out QuestDataSO questData))
        {
            return false;
        }

        return TryClaimReward(questData);
    }

    /// <summary>
    /// 런타임 퀘스트 상태를 초기화합니다.
    /// </summary>
    public void Reset()
    {
        EnsureQuestDatabase();
        _questStates.Clear();

        foreach (QuestDataSO questData in _questById.Values)
        {
            SyncQuestProgressFlags(questData, QuestState.QUEST_NOT_STARTED);
        }
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 퀘스트 데이터베이스 캐시가 없으면 다시 구성합니다.
    /// </summary>
    private void EnsureQuestDatabase()
    {
        if (_questById.Count == 0)
        {
            RebuildQuestDatabase();
        }
    }

    /// <summary>
    /// 인스펙터에 등록된 퀘스트 데이터 목록을 ID 캐시로 재구성합니다.
    /// </summary>
    private void RebuildQuestDatabase()
    {
        _questById.Clear();

        if (_questDatabase == null)
        {
            return;
        }

        for (int i = 0; i < _questDatabase.Count; i++)
        {
            QuestDataSO questData = _questDatabase[i];

            if (questData == null || string.IsNullOrWhiteSpace(questData.QuestId))
            {
                continue;
            }

            if (_questById.ContainsKey(questData.QuestId))
            {
                Debug.LogWarning($"[QuestController] 중복 QuestId가 있습니다. questId={questData.QuestId}");
                continue;
            }

            _questById.Add(questData.QuestId, questData);
        }
    }

    /// <summary>
    /// 직접 전달된 퀘스트 데이터 또는 ID로 퀘스트 데이터를 해석합니다.
    /// </summary>
    /// <param name="questData">직접 전달된 퀘스트 데이터입니다.</param>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <param name="resolvedQuest">해석된 퀘스트 데이터입니다.</param>
    /// <returns>퀘스트 데이터를 찾으면 true입니다.</returns>
    private bool TryResolveQuest(QuestDataSO questData, string questId, out QuestDataSO resolvedQuest)
    {
        resolvedQuest = null;

        if (questData != null)
        {
            resolvedQuest = questData;
            RegisterQuestData(resolvedQuest);
            return !string.IsNullOrWhiteSpace(resolvedQuest.QuestId);
        }

        return TryGetQuestData(questId, out resolvedQuest);
    }

    /// <summary>
    /// 직접 전달된 퀘스트 데이터를 런타임 조회 캐시에 등록합니다.
    /// </summary>
    /// <param name="questData">등록할 퀘스트 데이터입니다.</param>
    private void RegisterQuestData(QuestDataSO questData)
    {
        if (questData == null || string.IsNullOrWhiteSpace(questData.QuestId))
        {
            return;
        }

        if (_questById.ContainsKey(questData.QuestId))
        {
            return;
        }

        _questById.Add(questData.QuestId, questData);
    }

    /// <summary>
    /// 퀘스트 상태를 저장하고 Progress flag를 동기화합니다.
    /// </summary>
    /// <param name="questData">상태를 변경할 퀘스트 데이터입니다.</param>
    /// <param name="nextState">변경할 상태입니다.</param>
    private void SetQuestState(QuestDataSO questData, QuestState nextState)
    {
        if (questData == null || string.IsNullOrWhiteSpace(questData.QuestId))
        {
            return;
        }

        QuestState previousState = GetQuestState(questData);

        if (previousState == nextState)
        {
            return;
        }

        _questStates[questData.QuestId] = nextState;
        SyncQuestProgressFlags(questData, nextState);
    }

    /// <summary>
    /// 퀘스트 상태를 다른 시스템에서 읽기 쉬운 Progress flag로도 동기화합니다.
    /// </summary>
    /// <param name="questData">동기화할 퀘스트 데이터입니다.</param>
    /// <param name="state">현재 퀘스트 상태입니다.</param>
    private void SyncQuestProgressFlags(QuestDataSO questData, QuestState state)
    {
        if (_progressService == null || questData == null || string.IsNullOrWhiteSpace(questData.QuestId))
        {
            return;
        }

        string questId = questData.QuestId;
        _progressService.SetFlag($"quest_{questId}_accepted", state != QuestState.QUEST_NOT_STARTED);
        _progressService.SetFlag($"quest_{questId}_completed", state == QuestState.QUEST_COMPLETED || state == QuestState.QUEST_REWARDED);
        _progressService.SetFlag($"quest_{questId}_rewarded", state == QuestState.QUEST_REWARDED);
    }
}
