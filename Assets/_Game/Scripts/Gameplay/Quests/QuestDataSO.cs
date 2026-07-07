using System.Collections.Generic;
using UnityEngine;

//==============================================================================
// 기능: class::QuestObjectiveData
// 설명: Progress flag 기반 퀘스트 목표 조건 데이터
//==============================================================================
[System.Serializable]
public class QuestObjectiveData
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Objective")]
    [Tooltip("목표 고유 ID입니다.")]
    [SerializeField]
    private string _objectiveId;

    [Header("Objective")]
    [Tooltip("퀘스트 창에 표시할 목표 설명입니다.")]
    [TextArea(2, 4)]
    [SerializeField]
    private string _description;

    [Header("Condition")]
    [Tooltip("목표 달성 여부를 확인할 Progress flag ID입니다.")]
    [SerializeField]
    private string _requiredProgressFlagId;

    [Header("Condition")]
    [Tooltip("목표 달성으로 인정할 flag 값입니다.")]
    [SerializeField]
    private bool _requiredFlagValue = true;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>목표 고유 ID입니다.</summary>
    public string ObjectiveId => _objectiveId;

    /// <summary>퀘스트 창에 표시할 목표 설명입니다.</summary>
    public string Description => _description;

    /// <summary>목표 달성 여부를 확인할 Progress flag ID입니다.</summary>
    public string RequiredProgressFlagId => _requiredProgressFlagId;

    /// <summary>목표 달성으로 인정할 flag 값입니다.</summary>
    public bool RequiredFlagValue => _requiredFlagValue;

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 현재 Progress 상태 기준으로 목표 달성 여부를 확인합니다.
    /// </summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    /// <returns>목표가 달성되었으면 true입니다.</returns>
    public bool IsSatisfied(IProgressService progressService)
    {
        if (progressService == null || string.IsNullOrWhiteSpace(_requiredProgressFlagId))
        {
            return false;
        }

        return progressService.GetFlag(_requiredProgressFlagId) == _requiredFlagValue;
    }
}

//==============================================================================
// 기능: class::QuestConsumeItemData
// 설명: 퀘스트 완료 확정 시 자동 소모할 인벤토리 아이템 데이터
//==============================================================================
[System.Serializable]
public class QuestConsumeItemData
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 아이템 자동 소모에 필요한 최소 수량입니다.
    private const int QUEST_CONSUME_MIN_ITEM_AMOUNT = 1;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Consume Item")]
    [Tooltip("퀘스트 완료 확정 시 자동 소모할 인벤토리 아이템입니다.")]
    [SerializeField]
    private ItemDataSO _consumeItemData;

    [Header("Consume Item")]
    [Tooltip("퀘스트 완료 확정 시 자동 소모할 아이템 수량입니다.")]
    [SerializeField]
    private int _consumeItemAmount = QUEST_CONSUME_MIN_ITEM_AMOUNT;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>퀘스트 완료 확정 시 자동 소모할 인벤토리 아이템입니다.</summary>
    public ItemDataSO ConsumeItemData => _consumeItemData;

    /// <summary>퀘스트 완료 확정 시 자동 소모할 아이템 수량입니다.</summary>
    public int ConsumeItemAmount => _consumeItemAmount;

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 자동 소모 대상 아이템 ID를 조회합니다.
    /// </summary>
    /// <param name="itemId">자동 소모 대상 아이템 ID입니다.</param>
    /// <returns>아이템 원본 데이터와 ID가 유효하면 true입니다.</returns>
    public bool TryGetConsumeItemId(out string itemId)
    {
        itemId = string.Empty;

        if (_consumeItemData == null)
        {
            return false;
        }

        return _consumeItemData.TryGetItemId(out itemId);
    }

    /// <summary>
    /// 현재 자동 소모 데이터가 유효한지 확인합니다.
    /// </summary>
    /// <returns>아이템 ID와 소모 수량이 유효하면 true입니다.</returns>
    public bool IsValid()
    {
        if (_consumeItemAmount < QUEST_CONSUME_MIN_ITEM_AMOUNT)
        {
            return false;
        }

        if (TryGetConsumeItemId(out string consumeItemId) == false)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(consumeItemId) == false;
    }

    /// <summary>
    /// 지정한 아이템 ID와 같은 자동 소모 항목인지 확인합니다.
    /// </summary>
    /// <param name="itemId">비교할 아이템 ID입니다.</param>
    /// <returns>자동 소모 아이템 ID와 같으면 true입니다.</returns>
    public bool IsMatchingItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (TryGetConsumeItemId(out string consumeItemId) == false)
        {
            return false;
        }

        return consumeItemId == itemId;
    }

    /// <summary>
    /// 인벤토리에서 아이템을 자동 소모할 수 있는지 확인합니다.
    /// </summary>
    /// <param name="inventoryService">인벤토리 서비스입니다.</param>
    /// <returns>서비스, 아이템 ID, 수량, 보유 수량이 모두 유효하면 true입니다.</returns>
    public bool CanConsume(IInventoryService inventoryService)
    {
        if (inventoryService == null || IsValid() == false)
        {
            return false;
        }

        if (TryGetConsumeItemId(out string consumeItemId) == false)
        {
            return false;
        }

        return inventoryService.GetItemCount(consumeItemId) >= _consumeItemAmount;
    }

    /// <summary>
    /// 인벤토리에서 아이템을 자동 소모합니다.
    /// </summary>
    /// <param name="inventoryService">인벤토리 서비스입니다.</param>
    /// <returns>소모에 성공하면 true입니다. 수량 부족은 예외가 아니라 false입니다.</returns>
    public bool TryConsume(IInventoryService inventoryService)
    {
        if (CanConsume(inventoryService) == false)
        {
            return false;
        }

        if (TryGetConsumeItemId(out string consumeItemId) == false)
        {
            return false;
        }

        return inventoryService.TryConsumeItem(consumeItemId, _consumeItemAmount);
    }
}

//==============================================================================
// 기능: class::QuestRewardData
// 설명: Progress flag 및 인벤토리 아이템 기반 보상 지급 데이터
//==============================================================================
[System.Serializable]
public class QuestRewardData
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 아이템 보상 지급에 필요한 최소 수량입니다.
    private const int QUEST_REWARD_MIN_ITEM_AMOUNT = 1;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Reward")]
    [Tooltip("보상 고유 ID입니다.")]
    [SerializeField]
    private string _rewardId;

    [Header("Reward")]
    [Tooltip("퀘스트 창에 표시할 보상 설명입니다.")]
    [TextArea(2, 4)]
    [SerializeField]
    private string _description;

    [Header("Reward")]
    [Tooltip("보상 지급 시 변경할 Progress flag ID입니다. 비워두면 설명용 보상으로만 사용합니다.")]
    [SerializeField]
    private string _rewardProgressFlagId;

    [Header("Reward")]
    [Tooltip("보상 지급 시 저장할 flag 값입니다.")]
    [SerializeField]
    private bool _rewardFlagValue = true;

    [Header("Item Reward")]
    [Tooltip("보상으로 지급할 인벤토리 아이템입니다. 비워두면 아이템 보상을 지급하지 않습니다.")]
    [SerializeField]
    private ItemDataSO _rewardItemData;

    [Header("Item Reward")]
    [Tooltip("보상 아이템 지급 개수입니다. 1 이상일 때만 지급합니다.")]
    [SerializeField]
    private int _rewardItemAmount = QUEST_REWARD_MIN_ITEM_AMOUNT;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>보상 고유 ID입니다.</summary>
    public string RewardId => _rewardId;

    /// <summary>퀘스트 창에 표시할 보상 설명입니다.</summary>
    public string Description => _description;

    /// <summary>보상 지급 시 변경할 Progress flag ID입니다.</summary>
    public string RewardProgressFlagId => _rewardProgressFlagId;

    /// <summary>보상 지급 시 저장할 flag 값입니다.</summary>
    public bool RewardFlagValue => _rewardFlagValue;

    /// <summary>보상으로 지급할 인벤토리 아이템입니다.</summary>
    public ItemDataSO RewardItemData => _rewardItemData;

    /// <summary>보상 아이템 지급 개수입니다.</summary>
    public int RewardItemAmount => _rewardItemAmount;

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 현재 보상 데이터가 지급 가능한지 확인합니다.
    /// </summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    /// <param name="inventoryService">인벤토리 서비스입니다.</param>
    /// <returns>보상 지급에 필요한 서비스와 데이터가 유효하면 true입니다.</returns>
    public bool CanApply(IProgressService progressService, IInventoryService inventoryService)
    {
        if (HasProgressReward() && progressService == null)
        {
            return false;
        }

        if (HasItemReward() == false)
        {
            return true;
        }

        if (inventoryService == null)
        {
            return false;
        }

        if (_rewardItemAmount < QUEST_REWARD_MIN_ITEM_AMOUNT)
        {
            return false;
        }

        return _rewardItemData.HasValidItemId;
    }

    /// <summary>
    /// 보상을 Progress 상태와 인벤토리에 반영합니다.
    /// </summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    /// <param name="inventoryService">인벤토리 서비스입니다.</param>
    /// <returns>보상 지급에 성공하면 true입니다.</returns>
    public bool TryApply(IProgressService progressService, IInventoryService inventoryService)
    {
        if (CanApply(progressService, inventoryService) == false)
        {
            return false;
        }

        if (HasItemReward() && inventoryService.TryAddItem(_rewardItemData, _rewardItemAmount) == false)
        {
            return false;
        }

        if (HasProgressReward())
        {
            progressService.SetFlag(_rewardProgressFlagId, _rewardFlagValue);
        }

        return true;
    }

    /// <summary>
    /// Progress flag 보상이 설정되어 있는지 확인합니다.
    /// </summary>
    /// <returns>Progress flag ID가 비어 있지 않으면 true입니다.</returns>
    private bool HasProgressReward()
    {
        return string.IsNullOrWhiteSpace(_rewardProgressFlagId) == false;
    }

    /// <summary>
    /// 아이템 보상이 설정되어 있는지 확인합니다.
    /// </summary>
    /// <returns>아이템 원본 데이터가 있으면 true입니다.</returns>
    private bool HasItemReward()
    {
        return _rewardItemData != null;
    }
}

//==============================================================================
// 기능: class::QuestDataSO
// 설명: 퀘스트 원본 데이터 ScriptableObject
//==============================================================================
[CreateAssetMenu(fileName = "QuestData", menuName = "Ruin Conservator/Quest/Quest Data")]
public class QuestDataSO : ScriptableObject
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Quest")]
    [Tooltip("퀘스트 고유 ID입니다. 저장 키로 사용되므로 변경에 주의합니다.")]
    [SerializeField]
    private string _questId;

    [Header("Quest")]
    [Tooltip("퀘스트 창에 표시할 제목입니다.")]
    [SerializeField]
    private string _title;

    [Header("Quest")]
    [Tooltip("퀘스트 창에 표시할 설명입니다.")]
    [TextArea(3, 8)]
    [SerializeField]
    private string _description;

    [Header("Quest")]
    [Tooltip("이 퀘스트와 주로 연결된 NPC ID입니다.")]
    [SerializeField]
    private string _ownerNpcId;

    [Header("Objectives")]
    [Tooltip("퀘스트 완료를 위해 만족해야 하는 목표 목록입니다.")]
    [SerializeField]
    private List<QuestObjectiveData> _objectives = new List<QuestObjectiveData>();

    [Header("Rewards")]
    [Tooltip("퀘스트 보상 목록입니다.")]
    [SerializeField]
    private List<QuestRewardData> _rewards = new List<QuestRewardData>();

    [Header("Consume Items")]
    [Tooltip("퀘스트 완료 확정 시 자동 소모할 인벤토리 아이템 목록입니다.")]
    [SerializeField]
    private List<QuestConsumeItemData> _consumeItems = new List<QuestConsumeItemData>();

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>퀘스트 고유 ID입니다.</summary>
    public string QuestId => _questId;

    /// <summary>퀘스트 창에 표시할 제목입니다.</summary>
    public string Title => _title;

    /// <summary>퀘스트 창에 표시할 설명입니다.</summary>
    public string Description => _description;

    /// <summary>이 퀘스트와 주로 연결된 NPC ID입니다.</summary>
    public string OwnerNpcId => _ownerNpcId;

    /// <summary>퀘스트 목표 목록입니다.</summary>
    public IReadOnlyList<QuestObjectiveData> Objectives => _objectives;

    /// <summary>퀘스트 보상 목록입니다.</summary>
    public IReadOnlyList<QuestRewardData> Rewards => _rewards;

    /// <summary>퀘스트 완료 확정 시 자동 소모할 인벤토리 아이템 목록입니다.</summary>
    public IReadOnlyList<QuestConsumeItemData> ConsumeItems => _consumeItems;

    /// <summary>자동 소모 아이템이 하나 이상 설정되어 있는지 여부입니다.</summary>
    public bool HasConsumeItems => HasAnyConsumeItem();

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 모든 목표가 달성되었는지 확인합니다.
    /// </summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    /// <returns>목표가 모두 달성되었으면 true입니다.</returns>
    public bool AreObjectivesSatisfied(IProgressService progressService)
    {
        if (_objectives == null || _objectives.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < _objectives.Count; i++)
        {
            QuestObjectiveData objective = _objectives[i];

            if (objective == null || objective.IsSatisfied(progressService) == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 완료 확정 시 필요한 자동 소모 아이템을 모두 보유 중인지 확인합니다.
    /// </summary>
    /// <param name="inventoryService">인벤토리 서비스입니다.</param>
    /// <returns>자동 소모 데이터가 없거나, 필요한 수량을 모두 보유 중이면 true입니다.</returns>
    public bool CanConsumeRequiredItems(IInventoryService inventoryService)
    {
        if (HasAnyConsumeItem() == false)
        {
            return true;
        }

        if (inventoryService == null)
        {
            return false;
        }

        if (AreConsumeItemsValid() == false)
        {
            return false;
        }

        for (int i = 0; i < _consumeItems.Count; i++)
        {
            QuestConsumeItemData consumeItem = _consumeItems[i];

            if (consumeItem == null)
            {
                continue;
            }

            if (consumeItem.TryGetConsumeItemId(out string consumeItemId) == false)
            {
                return false;
            }

            if (IsFirstConsumeItemId(consumeItemId, i) == false)
            {
                continue;
            }

            if (TryGetRequiredConsumeItemAmount(consumeItemId, out int requiredAmount) == false)
            {
                return false;
            }

            if (inventoryService.GetItemCount(consumeItemId) < requiredAmount)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 완료 확정 시 필요한 자동 소모 아이템을 인벤토리에서 차감합니다.
    /// </summary>
    /// <param name="inventoryService">인벤토리 서비스입니다.</param>
    /// <returns>자동 소모 데이터가 없거나, 모든 아이템 소모에 성공하면 true입니다.</returns>
    public bool TryConsumeRequiredItems(IInventoryService inventoryService)
    {
        if (CanConsumeRequiredItems(inventoryService) == false)
        {
            return false;
        }

        if (HasAnyConsumeItem() == false)
        {
            return true;
        }

        for (int i = 0; i < _consumeItems.Count; i++)
        {
            QuestConsumeItemData consumeItem = _consumeItems[i];

            if (consumeItem != null && consumeItem.TryConsume(inventoryService) == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 등록된 보상을 지급할 수 있는지 확인합니다.
    /// </summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    /// <param name="inventoryService">인벤토리 서비스입니다.</param>
    /// <returns>모든 보상 지급 조건이 유효하면 true입니다.</returns>
    public bool CanApplyRewards(IProgressService progressService, IInventoryService inventoryService)
    {
        if (_rewards == null)
        {
            return true;
        }

        for (int i = 0; i < _rewards.Count; i++)
        {
            QuestRewardData reward = _rewards[i];

            if (reward != null && reward.CanApply(progressService, inventoryService) == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 등록된 보상을 Progress 상태와 인벤토리에 지급합니다.
    /// </summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    /// <param name="inventoryService">인벤토리 서비스입니다.</param>
    /// <returns>등록된 모든 보상 지급에 성공하면 true입니다.</returns>
    public bool TryApplyRewards(IProgressService progressService, IInventoryService inventoryService)
    {
        if (CanApplyRewards(progressService, inventoryService) == false)
        {
            return false;
        }

        if (_rewards == null)
        {
            return true;
        }

        for (int i = 0; i < _rewards.Count; i++)
        {
            QuestRewardData reward = _rewards[i];

            if (reward != null && reward.TryApply(progressService, inventoryService) == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 자동 소모 아이템이 하나 이상 설정되어 있는지 확인합니다.
    /// </summary>
    /// <returns>자동 소모 아이템 데이터가 하나 이상 있으면 true입니다.</returns>
    private bool HasAnyConsumeItem()
    {
        if (_consumeItems == null)
        {
            return false;
        }

        for (int i = 0; i < _consumeItems.Count; i++)
        {
            if (_consumeItems[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 자동 소모 아이템 목록의 모든 데이터가 유효한지 확인합니다.
    /// </summary>
    /// <returns>null이 아닌 모든 자동 소모 데이터가 유효하면 true입니다.</returns>
    private bool AreConsumeItemsValid()
    {
        if (_consumeItems == null)
        {
            return true;
        }

        for (int i = 0; i < _consumeItems.Count; i++)
        {
            QuestConsumeItemData consumeItem = _consumeItems[i];

            if (consumeItem != null && consumeItem.IsValid() == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 같은 아이템 ID의 자동 소모 항목 중 첫 번째 항목인지 확인합니다.
    /// </summary>
    /// <param name="itemId">확인할 아이템 ID입니다.</param>
    /// <param name="currentIndex">현재 자동 소모 항목 인덱스입니다.</param>
    /// <returns>이전에 같은 아이템 ID가 없으면 true입니다.</returns>
    private bool IsFirstConsumeItemId(string itemId, int currentIndex)
    {
        for (int i = 0; i < currentIndex; i++)
        {
            QuestConsumeItemData consumeItem = _consumeItems[i];

            if (consumeItem != null && consumeItem.IsMatchingItemId(itemId))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 같은 아이템 ID로 설정된 자동 소모 필요 수량 합계를 계산합니다.
    /// </summary>
    /// <param name="itemId">계산할 아이템 ID입니다.</param>
    /// <param name="requiredAmount">자동 소모에 필요한 전체 수량입니다.</param>
    /// <returns>필요 수량을 안전하게 계산했으면 true입니다.</returns>
    private bool TryGetRequiredConsumeItemAmount(string itemId, out int requiredAmount)
    {
        requiredAmount = 0;

        if (_consumeItems == null)
        {
            return true;
        }

        for (int i = 0; i < _consumeItems.Count; i++)
        {
            QuestConsumeItemData consumeItem = _consumeItems[i];

            if (consumeItem != null && consumeItem.IsMatchingItemId(itemId))
            {
                if (int.MaxValue - requiredAmount < consumeItem.ConsumeItemAmount)
                {
                    return false;
                }

                requiredAmount += consumeItem.ConsumeItemAmount;
            }
        }

        return true;
    }
}
