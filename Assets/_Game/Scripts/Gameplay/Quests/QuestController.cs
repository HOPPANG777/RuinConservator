using System.Collections.Generic;
using UnityEngine;

//==============================================================================
// 기능: class::QuestController
// 설명: CSV 퀘스트 정의를 사용해 상태 전환, 목표, 소모 및 보상을 처리하는 컨트롤러
//==============================================================================
[DisallowMultipleComponent]
public sealed class QuestController : MonoBehaviour, IQuestService
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 퀘스트 상태를 Progress Flag로 공유할 때 사용하는 키 접두사입니다.
    private const string QUEST_FLAG_PREFIX = "quest_";

    // 퀘스트 수락 여부 Progress Flag의 키 접미사입니다.
    private const string ACCEPTED_FLAG_SUFFIX = "_accepted";

    // 퀘스트 완료 여부 Progress Flag의 키 접미사입니다.
    private const string COMPLETED_FLAG_SUFFIX = "_completed";

    // 퀘스트 보상 수령 여부 Progress Flag의 키 접미사입니다.
    private const string REWARDED_FLAG_SUFFIX = "_rewarded";

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 퀘스트 ID별 런타임 상태입니다.
    private readonly Dictionary<string, QuestState> _questStates = new Dictionary<string, QuestState>();

    // Dictionary를 수정하며 순회하지 않도록 재사용하는 퀘스트 ID 버퍼입니다.
    private readonly List<string> _questIdBuffer = new List<string>();

    // 퀘스트 정의를 ID로 조회할 읽기 전용 게임 데이터 저장소입니다.
    private IGameDataRepository _gameDataRepository;

    // 목표와 Progress Flag 보상을 처리할 진행 상태 서비스입니다.
    private IProgressService _progressService;

    // 자동 소모와 아이템 보상을 처리할 인벤토리 서비스입니다.
    private IInventoryService _inventoryService;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 게임 데이터와 퀘스트 협력 서비스를 주입합니다.
    /// </summary>
    /// <param name="gameDataRepository">CSV에서 생성된 읽기 전용 게임 데이터 저장소입니다.</param>
    /// <param name="progressService">목표와 상태 Flag에 사용할 진행 상태 서비스입니다.</param>
    /// <param name="inventoryService">아이템 소모와 보상에 사용할 인벤토리 서비스입니다.</param>
    public void Init(
        IGameDataRepository gameDataRepository,
        IProgressService progressService,
        IInventoryService inventoryService = null)
    {
        _gameDataRepository = gameDataRepository;
        _progressService = progressService;
        _inventoryService = inventoryService;

        if (_gameDataRepository == null)
        {
            Debug.LogError("[QuestController] 게임 데이터 저장소가 주입되지 않았습니다.");
        }
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 퀘스트 ID로 읽기 전용 퀘스트 정의를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <param name="questDefinition">조회된 퀘스트 정의입니다.</param>
    /// <returns>퀘스트 정의를 찾으면 true입니다.</returns>
    public bool TryGetQuestDefinition(string questId, out QuestDefinition questDefinition)
    {
        questDefinition = null;

        if (_gameDataRepository == null || string.IsNullOrWhiteSpace(questId))
        {
            return false;
        }

        return _gameDataRepository.TryGetQuest(questId, out questDefinition);
    }

    /// <summary>
    /// 퀘스트 ID의 현재 런타임 상태를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <returns>현재 퀘스트 상태이며 등록되지 않은 ID는 미시작 상태입니다.</returns>
    public QuestState GetQuestState(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            return QuestState.QUEST_NOT_STARTED;
        }

        return _questStates.TryGetValue(questId, out QuestState state)
            ? state
            : QuestState.QUEST_NOT_STARTED;
    }

    /// <summary>
    /// 퀘스트를 진행 중 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">수락할 퀘스트 ID입니다.</param>
    /// <returns>상태 변경에 성공하면 true입니다.</returns>
    public bool TryAcceptQuest(string questId)
    {
        if (!TryGetQuestDefinition(questId, out QuestDefinition questDefinition))
        {
            return false;
        }

        if (GetQuestState(questId) != QuestState.QUEST_NOT_STARTED)
        {
            return false;
        }

        SetQuestState(questDefinition.QuestId, QuestState.QUEST_IN_PROGRESS);
        RefreshQuestProgress(questDefinition);
        return true;
    }

    /// <summary>
    /// 퀘스트 목표를 확인하고 자동 소모 아이템이 없으면 완료 상태로 갱신합니다.
    /// </summary>
    /// <param name="questId">갱신할 퀘스트 ID입니다.</param>
    /// <returns>이번 호출로 완료 상태가 되었으면 true입니다.</returns>
    public bool RefreshQuestProgress(string questId)
    {
        return TryGetQuestDefinition(questId, out QuestDefinition questDefinition)
            && RefreshQuestProgress(questDefinition);
    }

    /// <summary>
    /// 진행 중인 모든 퀘스트의 목표 달성 여부를 갱신합니다.
    /// </summary>
    /// <returns>이번 호출로 완료 상태가 된 퀘스트 수입니다.</returns>
    public int RefreshAllInProgressQuests()
    {
        _questIdBuffer.Clear();

        foreach (KeyValuePair<string, QuestState> pair in _questStates)
        {
            if (pair.Value == QuestState.QUEST_IN_PROGRESS)
            {
                _questIdBuffer.Add(pair.Key);
            }
        }

        int completedCount = 0;

        for (int i = 0; i < _questIdBuffer.Count; i++)
        {
            if (RefreshQuestProgress(_questIdBuffer[i]))
            {
                completedCount++;
            }
        }

        return completedCount;
    }

    /// <summary>
    /// 목표가 달성된 퀘스트를 완료하고 필요한 아이템을 자동 소모합니다.
    /// </summary>
    /// <param name="questId">완료할 퀘스트 ID입니다.</param>
    /// <returns>완료 상태가 되었거나 이미 완료 상태면 true입니다.</returns>
    public bool TryCompleteQuest(string questId)
    {
        if (!TryGetQuestDefinition(questId, out QuestDefinition questDefinition))
        {
            return false;
        }

        QuestState currentState = GetQuestState(questId);

        if (currentState == QuestState.QUEST_COMPLETED)
        {
            return true;
        }

        if (currentState != QuestState.QUEST_IN_PROGRESS
            || !AreObjectivesSatisfied(questDefinition)
            || !TryConsumeRequiredItems(questDefinition))
        {
            return false;
        }

        SetQuestState(questDefinition.QuestId, QuestState.QUEST_COMPLETED);
        return true;
    }

    /// <summary>
    /// 완료된 퀘스트의 보상을 지급하고 보상 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">보상을 받을 퀘스트 ID입니다.</param>
    /// <returns>이번 호출로 보상을 지급했으면 true입니다.</returns>
    public bool TryClaimReward(string questId)
    {
        if (!TryGetQuestDefinition(questId, out QuestDefinition questDefinition))
        {
            return false;
        }

        QuestState currentState = GetQuestState(questId);

        if (currentState == QuestState.QUEST_REWARDED)
        {
            return false;
        }

        if (currentState == QuestState.QUEST_IN_PROGRESS)
        {
            if (!CanApplyRewards(questDefinition) || !TryCompleteQuest(questId))
            {
                return false;
            }

            currentState = GetQuestState(questId);
        }

        if (currentState != QuestState.QUEST_COMPLETED || !TryApplyRewards(questDefinition))
        {
            return false;
        }

        SetQuestState(questDefinition.QuestId, QuestState.QUEST_REWARDED);
        return true;
    }

    /// <summary>
    /// 모든 런타임 퀘스트 상태와 공유 Progress Flag를 초기화합니다.
    /// </summary>
    public void Reset()
    {
        foreach (KeyValuePair<string, QuestState> pair in _questStates)
        {
            SyncQuestProgressFlags(pair.Key, QuestState.QUEST_NOT_STARTED);
        }

        _questStates.Clear();
        _questIdBuffer.Clear();
    }

    //==============================================================================
    // 목표 판정
    //==============================================================================

    /// <summary>
    /// 퀘스트 목표를 확인하고 자동 소모 항목이 없으면 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questDefinition">갱신할 퀘스트 정의입니다.</param>
    /// <returns>이번 호출로 완료 상태가 되었으면 true입니다.</returns>
    private bool RefreshQuestProgress(QuestDefinition questDefinition)
    {
        if (questDefinition == null
            || GetQuestState(questDefinition.QuestId) != QuestState.QUEST_IN_PROGRESS
            || !AreObjectivesSatisfied(questDefinition)
            || questDefinition.ConsumeItems.Count > 0)
        {
            return false;
        }

        SetQuestState(questDefinition.QuestId, QuestState.QUEST_COMPLETED);
        return true;
    }

    /// <summary>
    /// 퀘스트의 모든 Progress Flag 목표가 달성되었는지 확인합니다.
    /// </summary>
    /// <param name="questDefinition">목표를 확인할 퀘스트 정의입니다.</param>
    /// <returns>목표가 하나 이상이고 모두 달성되었으면 true입니다.</returns>
    private bool AreObjectivesSatisfied(QuestDefinition questDefinition)
    {
        if (_progressService == null || questDefinition == null || questDefinition.Objectives.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < questDefinition.Objectives.Count; i++)
        {
            QuestObjectiveDefinition objective = questDefinition.Objectives[i];

            if (objective == null
                || string.IsNullOrWhiteSpace(objective.RequiredProgressFlagId)
                || _progressService.GetFlag(objective.RequiredProgressFlagId) != objective.RequiredFlagValue)
            {
                return false;
            }
        }

        return true;
    }

    //==============================================================================
    // 자동 소모
    //==============================================================================

    /// <summary>
    /// 필요한 아이템을 모두 보유했는지 선검증한 뒤 인벤토리에서 소모합니다.
    /// </summary>
    /// <param name="questDefinition">자동 소모 항목을 가진 퀘스트 정의입니다.</param>
    /// <returns>소모 항목이 없거나 모든 아이템 소모에 성공하면 true입니다.</returns>
    private bool TryConsumeRequiredItems(QuestDefinition questDefinition)
    {
        if (questDefinition == null || questDefinition.ConsumeItems.Count == 0)
        {
            return true;
        }

        if (_inventoryService == null || !CanConsumeRequiredItems(questDefinition))
        {
            return false;
        }

        for (int i = 0; i < questDefinition.ConsumeItems.Count; i++)
        {
            QuestConsumeItemDefinition consumeItem = questDefinition.ConsumeItems[i];

            if (consumeItem == null
                || !_inventoryService.TryConsumeItem(consumeItem.ItemId, consumeItem.ItemAmount))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 같은 아이템 ID의 소모 수량을 합산해 필요한 전체 수량을 보유했는지 확인합니다.
    /// </summary>
    /// <param name="questDefinition">확인할 퀘스트 정의입니다.</param>
    /// <returns>모든 자동 소모 항목이 유효하고 필요한 수량을 보유했으면 true입니다.</returns>
    private bool CanConsumeRequiredItems(QuestDefinition questDefinition)
    {
        for (int i = 0; i < questDefinition.ConsumeItems.Count; i++)
        {
            QuestConsumeItemDefinition consumeItem = questDefinition.ConsumeItems[i];

            if (consumeItem == null
                || string.IsNullOrWhiteSpace(consumeItem.ItemId)
                || consumeItem.ItemAmount <= 0)
            {
                return false;
            }

            if (!IsFirstConsumeItemId(questDefinition, consumeItem.ItemId, i))
            {
                continue;
            }

            if (!TryGetRequiredConsumeAmount(questDefinition, consumeItem.ItemId, out int requiredAmount)
                || _inventoryService.GetItemCount(consumeItem.ItemId) < requiredAmount)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 현재 위치 이전에 같은 자동 소모 아이템 ID가 있는지 확인합니다.
    /// </summary>
    /// <param name="questDefinition">확인할 퀘스트 정의입니다.</param>
    /// <param name="itemId">확인할 아이템 ID입니다.</param>
    /// <param name="currentIndex">현재 자동 소모 항목 인덱스입니다.</param>
    /// <returns>이전에 같은 아이템 ID가 없으면 true입니다.</returns>
    private static bool IsFirstConsumeItemId(
        QuestDefinition questDefinition,
        string itemId,
        int currentIndex)
    {
        for (int i = 0; i < currentIndex; i++)
        {
            QuestConsumeItemDefinition consumeItem = questDefinition.ConsumeItems[i];

            if (consumeItem != null && consumeItem.ItemId == itemId)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 같은 아이템 ID의 자동 소모 수량 합계를 오버플로 없이 계산합니다.
    /// </summary>
    /// <param name="questDefinition">계산할 퀘스트 정의입니다.</param>
    /// <param name="itemId">합산할 아이템 ID입니다.</param>
    /// <param name="requiredAmount">계산된 전체 필요 수량입니다.</param>
    /// <returns>수량을 안전하게 계산했으면 true입니다.</returns>
    private static bool TryGetRequiredConsumeAmount(
        QuestDefinition questDefinition,
        string itemId,
        out int requiredAmount)
    {
        requiredAmount = 0;

        for (int i = 0; i < questDefinition.ConsumeItems.Count; i++)
        {
            QuestConsumeItemDefinition consumeItem = questDefinition.ConsumeItems[i];

            if (consumeItem == null || consumeItem.ItemId != itemId)
            {
                continue;
            }

            if (int.MaxValue - requiredAmount < consumeItem.ItemAmount)
            {
                return false;
            }

            requiredAmount += consumeItem.ItemAmount;
        }

        return true;
    }

    //==============================================================================
    // 보상
    //==============================================================================

    /// <summary>
    /// 모든 보상에 필요한 서비스와 아이템 원본 데이터가 준비되었는지 확인합니다.
    /// </summary>
    /// <param name="questDefinition">확인할 퀘스트 정의입니다.</param>
    /// <returns>모든 보상을 지급할 수 있으면 true입니다.</returns>
    private bool CanApplyRewards(QuestDefinition questDefinition)
    {
        if (questDefinition == null)
        {
            return false;
        }

        for (int i = 0; i < questDefinition.Rewards.Count; i++)
        {
            QuestRewardDefinition reward = questDefinition.Rewards[i];

            if (reward == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(reward.ProgressFlagId)
                && (_progressService == null || !reward.FlagValue.HasValue))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(reward.ItemId)
                && (_inventoryService == null
                    || !reward.ItemAmount.HasValue
                    || reward.ItemAmount.Value <= 0
                    || !_inventoryService.TryGetItem(reward.ItemId, out _)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 선검증된 Progress Flag와 아이템 보상을 순서대로 지급합니다.
    /// </summary>
    /// <param name="questDefinition">보상을 지급할 퀘스트 정의입니다.</param>
    /// <returns>모든 보상을 지급했으면 true입니다.</returns>
    private bool TryApplyRewards(QuestDefinition questDefinition)
    {
        if (!CanApplyRewards(questDefinition))
        {
            return false;
        }

        for (int i = 0; i < questDefinition.Rewards.Count; i++)
        {
            QuestRewardDefinition reward = questDefinition.Rewards[i];

            if (!string.IsNullOrWhiteSpace(reward.ProgressFlagId))
            {
                _progressService.SetFlag(reward.ProgressFlagId, reward.FlagValue.Value);
            }

            if (!string.IsNullOrWhiteSpace(reward.ItemId))
            {
                if (!_inventoryService.TryGetItem(reward.ItemId, out ItemDataSO itemData)
                    || !_inventoryService.TryAddItem(itemData, reward.ItemAmount.Value))
                {
                    return false;
                }
            }
        }

        return true;
    }

    //==============================================================================
    // 상태 동기화
    //==============================================================================

    /// <summary>
    /// 퀘스트 런타임 상태를 저장하고 공유 Progress Flag를 동기화합니다.
    /// </summary>
    /// <param name="questId">상태를 변경할 퀘스트 ID입니다.</param>
    /// <param name="nextState">변경할 상태입니다.</param>
    private void SetQuestState(string questId, QuestState nextState)
    {
        if (string.IsNullOrWhiteSpace(questId) || GetQuestState(questId) == nextState)
        {
            return;
        }

        _questStates[questId] = nextState;
        SyncQuestProgressFlags(questId, nextState);
    }

    /// <summary>
    /// 퀘스트 상태를 다른 시스템에서 조회할 수 있는 Progress Flag로 동기화합니다.
    /// </summary>
    /// <param name="questId">동기화할 퀘스트 ID입니다.</param>
    /// <param name="state">현재 퀘스트 상태입니다.</param>
    private void SyncQuestProgressFlags(string questId, QuestState state)
    {
        if (_progressService == null || string.IsNullOrWhiteSpace(questId))
        {
            return;
        }

        string flagPrefix = string.Concat(QUEST_FLAG_PREFIX, questId);
        _progressService.SetFlag(
            string.Concat(flagPrefix, ACCEPTED_FLAG_SUFFIX),
            state != QuestState.QUEST_NOT_STARTED);
        _progressService.SetFlag(
            string.Concat(flagPrefix, COMPLETED_FLAG_SUFFIX),
            state == QuestState.QUEST_COMPLETED || state == QuestState.QUEST_REWARDED);
        _progressService.SetFlag(
            string.Concat(flagPrefix, REWARDED_FLAG_SUFFIX),
            state == QuestState.QUEST_REWARDED);
    }
}
