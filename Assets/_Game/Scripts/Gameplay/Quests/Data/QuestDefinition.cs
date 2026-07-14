using System.Collections.Generic;

//==============================================================================
// 기능: class::QuestObjectiveDefinition
// 설명: CSV에서 읽은 Progress Flag 기반 퀘스트 목표 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class QuestObjectiveDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 이 목표가 속한 퀘스트 고유 ID입니다.
    private readonly string _questId;

    // 동일 퀘스트 안에서 목표를 표시하고 판정할 순서입니다.
    private readonly int _objectiveOrder;

    // 동일 퀘스트 안에서 사용하는 목표 고유 ID입니다.
    private readonly string _objectiveId;

    // 퀘스트 UI에 표시할 목표 설명 Localization Key입니다.
    private readonly string _descriptionTextKey;

    // 목표 달성 여부를 확인할 Progress Flag ID입니다.
    private readonly string _requiredProgressFlagId;

    // 목표 달성으로 인정할 Progress Flag 값입니다.
    private readonly bool _requiredFlagValue;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>이 목표가 속한 퀘스트 고유 ID입니다.</summary>
    public string QuestId => _questId;

    /// <summary>동일 퀘스트 안에서 목표를 표시하고 판정할 순서입니다.</summary>
    public int ObjectiveOrder => _objectiveOrder;

    /// <summary>동일 퀘스트 안에서 사용하는 목표 고유 ID입니다.</summary>
    public string ObjectiveId => _objectiveId;

    /// <summary>퀘스트 UI에 표시할 목표 설명 Localization Key입니다.</summary>
    public string DescriptionTextKey => _descriptionTextKey;

    /// <summary>목표 달성 여부를 확인할 Progress Flag ID입니다.</summary>
    public string RequiredProgressFlagId => _requiredProgressFlagId;

    /// <summary>목표 달성으로 인정할 Progress Flag 값입니다.</summary>
    public bool RequiredFlagValue => _requiredFlagValue;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 퀘스트 목표 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="questId">이 목표가 속한 퀘스트 고유 ID입니다.</param>
    /// <param name="objectiveOrder">목표를 표시하고 판정할 순서입니다.</param>
    /// <param name="objectiveId">동일 퀘스트 안에서 사용하는 목표 고유 ID입니다.</param>
    /// <param name="descriptionTextKey">목표 설명 Localization Key입니다.</param>
    /// <param name="requiredProgressFlagId">목표 달성 여부를 확인할 Progress Flag ID입니다.</param>
    /// <param name="requiredFlagValue">목표 달성으로 인정할 Progress Flag 값입니다.</param>
    public QuestObjectiveDefinition(
        string questId,
        int objectiveOrder,
        string objectiveId,
        string descriptionTextKey,
        string requiredProgressFlagId,
        bool requiredFlagValue)
    {
        _questId = questId ?? string.Empty;
        _objectiveOrder = objectiveOrder;
        _objectiveId = objectiveId ?? string.Empty;
        _descriptionTextKey = descriptionTextKey ?? string.Empty;
        _requiredProgressFlagId = requiredProgressFlagId ?? string.Empty;
        _requiredFlagValue = requiredFlagValue;
    }
}

//==============================================================================
// 기능: class::QuestRewardDefinition
// 설명: CSV에서 읽은 Progress Flag 및 아이템 퀘스트 보상 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class QuestRewardDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 이 보상이 속한 퀘스트 고유 ID입니다.
    private readonly string _questId;

    // 동일 퀘스트 안에서 보상을 표시하고 적용할 순서입니다.
    private readonly int _rewardOrder;

    // 동일 퀘스트 안에서 사용하는 보상 고유 ID입니다.
    private readonly string _rewardId;

    // 퀘스트 UI에 표시할 보상 설명 Localization Key입니다.
    private readonly string _descriptionTextKey;

    // 보상 지급 시 변경할 Progress Flag ID입니다.
    private readonly string _progressFlagId;

    // Progress Flag에 저장할 선택적 값입니다.
    private readonly bool? _flagValue;

    // 보상으로 지급할 인벤토리 아이템 ID입니다.
    private readonly string _itemId;

    // 보상으로 지급할 선택적 아이템 수량입니다.
    private readonly int? _itemAmount;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>이 보상이 속한 퀘스트 고유 ID입니다.</summary>
    public string QuestId => _questId;

    /// <summary>동일 퀘스트 안에서 보상을 표시하고 적용할 순서입니다.</summary>
    public int RewardOrder => _rewardOrder;

    /// <summary>동일 퀘스트 안에서 사용하는 보상 고유 ID입니다.</summary>
    public string RewardId => _rewardId;

    /// <summary>퀘스트 UI에 표시할 보상 설명 Localization Key입니다.</summary>
    public string DescriptionTextKey => _descriptionTextKey;

    /// <summary>보상 지급 시 변경할 Progress Flag ID입니다.</summary>
    public string ProgressFlagId => _progressFlagId;

    /// <summary>Progress Flag에 저장할 선택적 값입니다.</summary>
    public bool? FlagValue => _flagValue;

    /// <summary>보상으로 지급할 인벤토리 아이템 ID입니다.</summary>
    public string ItemId => _itemId;

    /// <summary>보상으로 지급할 선택적 아이템 수량입니다.</summary>
    public int? ItemAmount => _itemAmount;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 퀘스트 보상 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="questId">이 보상이 속한 퀘스트 고유 ID입니다.</param>
    /// <param name="rewardOrder">보상을 표시하고 적용할 순서입니다.</param>
    /// <param name="rewardId">동일 퀘스트 안에서 사용하는 보상 고유 ID입니다.</param>
    /// <param name="descriptionTextKey">보상 설명 Localization Key입니다.</param>
    /// <param name="progressFlagId">보상 지급 시 변경할 Progress Flag ID입니다.</param>
    /// <param name="flagValue">Progress Flag에 저장할 선택적 값입니다.</param>
    /// <param name="itemId">보상으로 지급할 인벤토리 아이템 ID입니다.</param>
    /// <param name="itemAmount">보상으로 지급할 선택적 아이템 수량입니다.</param>
    public QuestRewardDefinition(
        string questId,
        int rewardOrder,
        string rewardId,
        string descriptionTextKey,
        string progressFlagId,
        bool? flagValue,
        string itemId,
        int? itemAmount)
    {
        _questId = questId ?? string.Empty;
        _rewardOrder = rewardOrder;
        _rewardId = rewardId ?? string.Empty;
        _descriptionTextKey = descriptionTextKey ?? string.Empty;
        _progressFlagId = progressFlagId ?? string.Empty;
        _flagValue = flagValue;
        _itemId = itemId ?? string.Empty;
        _itemAmount = itemAmount;
    }
}

//==============================================================================
// 기능: class::QuestConsumeItemDefinition
// 설명: CSV에서 읽은 퀘스트 완료 시 자동 소모 아이템 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class QuestConsumeItemDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 이 자동 소모 항목이 속한 퀘스트 고유 ID입니다.
    private readonly string _questId;

    // 동일 퀘스트 안에서 아이템을 소모할 순서입니다.
    private readonly int _consumeOrder;

    // 퀘스트 완료 확정 시 자동 소모할 인벤토리 아이템 ID입니다.
    private readonly string _itemId;

    // 퀘스트 완료 확정 시 자동 소모할 아이템 수량입니다.
    private readonly int _itemAmount;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>이 자동 소모 항목이 속한 퀘스트 고유 ID입니다.</summary>
    public string QuestId => _questId;

    /// <summary>동일 퀘스트 안에서 아이템을 소모할 순서입니다.</summary>
    public int ConsumeOrder => _consumeOrder;

    /// <summary>퀘스트 완료 확정 시 자동 소모할 인벤토리 아이템 ID입니다.</summary>
    public string ItemId => _itemId;

    /// <summary>퀘스트 완료 확정 시 자동 소모할 아이템 수량입니다.</summary>
    public int ItemAmount => _itemAmount;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 퀘스트 완료 시 자동 소모할 아이템 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="questId">이 자동 소모 항목이 속한 퀘스트 고유 ID입니다.</param>
    /// <param name="consumeOrder">동일 퀘스트 안에서 아이템을 소모할 순서입니다.</param>
    /// <param name="itemId">자동 소모할 인벤토리 아이템 ID입니다.</param>
    /// <param name="itemAmount">자동 소모할 아이템 수량입니다.</param>
    public QuestConsumeItemDefinition(
        string questId,
        int consumeOrder,
        string itemId,
        int itemAmount)
    {
        _questId = questId ?? string.Empty;
        _consumeOrder = consumeOrder;
        _itemId = itemId ?? string.Empty;
        _itemAmount = itemAmount;
    }
}

//==============================================================================
// 기능: class::QuestDefinition
// 설명: CSV에서 읽은 퀘스트와 목표, 보상, 자동 소모 아이템 목록 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class QuestDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 퀘스트 고유 ID이자 저장 데이터에서 사용할 키입니다.
    private readonly string _questId;

    // 퀘스트 UI에 표시할 제목 Localization Key입니다.
    private readonly string _titleTextKey;

    // 퀘스트 UI에 표시할 설명 Localization Key입니다.
    private readonly string _descriptionTextKey;

    // 이 퀘스트와 주로 연결된 NPC 고유 ID입니다.
    private readonly string _ownerNpcId;

    // 퀘스트 완료 판정에 사용할 목표 정의 목록입니다.
    private readonly List<QuestObjectiveDefinition> _objectives;

    // 퀘스트 완료 후 지급할 보상 정의 목록입니다.
    private readonly List<QuestRewardDefinition> _rewards;

    // 퀘스트 완료 확정 시 자동 소모할 아이템 정의 목록입니다.
    private readonly List<QuestConsumeItemDefinition> _consumeItems;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>퀘스트 고유 ID이자 저장 데이터에서 사용할 키입니다.</summary>
    public string QuestId => _questId;

    /// <summary>퀘스트 UI에 표시할 제목 Localization Key입니다.</summary>
    public string TitleTextKey => _titleTextKey;

    /// <summary>퀘스트 UI에 표시할 설명 Localization Key입니다.</summary>
    public string DescriptionTextKey => _descriptionTextKey;

    /// <summary>이 퀘스트와 주로 연결된 NPC 고유 ID입니다.</summary>
    public string OwnerNpcId => _ownerNpcId;

    /// <summary>퀘스트 완료 판정에 사용할 목표 정의 목록입니다.</summary>
    public IReadOnlyList<QuestObjectiveDefinition> Objectives => _objectives;

    /// <summary>퀘스트 완료 후 지급할 보상 정의 목록입니다.</summary>
    public IReadOnlyList<QuestRewardDefinition> Rewards => _rewards;

    /// <summary>퀘스트 완료 확정 시 자동 소모할 아이템 정의 목록입니다.</summary>
    public IReadOnlyList<QuestConsumeItemDefinition> ConsumeItems => _consumeItems;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 퀘스트 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="questId">퀘스트 고유 ID입니다.</param>
    /// <param name="titleTextKey">퀘스트 제목 Localization Key입니다.</param>
    /// <param name="descriptionTextKey">퀘스트 설명 Localization Key입니다.</param>
    /// <param name="ownerNpcId">이 퀘스트와 주로 연결된 NPC 고유 ID입니다.</param>
    /// <param name="objectives">퀘스트 완료 판정에 사용할 목표 정의 목록입니다.</param>
    /// <param name="rewards">퀘스트 완료 후 지급할 보상 정의 목록입니다.</param>
    /// <param name="consumeItems">퀘스트 완료 확정 시 자동 소모할 아이템 정의 목록입니다.</param>
    public QuestDefinition(
        string questId,
        string titleTextKey,
        string descriptionTextKey,
        string ownerNpcId,
        IReadOnlyList<QuestObjectiveDefinition> objectives,
        IReadOnlyList<QuestRewardDefinition> rewards,
        IReadOnlyList<QuestConsumeItemDefinition> consumeItems)
    {
        _questId = questId ?? string.Empty;
        _titleTextKey = titleTextKey ?? string.Empty;
        _descriptionTextKey = descriptionTextKey ?? string.Empty;
        _ownerNpcId = ownerNpcId ?? string.Empty;
        _objectives = objectives != null
            ? new List<QuestObjectiveDefinition>(objectives)
            : new List<QuestObjectiveDefinition>();
        _rewards = rewards != null
            ? new List<QuestRewardDefinition>(rewards)
            : new List<QuestRewardDefinition>();
        _consumeItems = consumeItems != null
            ? new List<QuestConsumeItemDefinition>(consumeItems)
            : new List<QuestConsumeItemDefinition>();
    }
}
