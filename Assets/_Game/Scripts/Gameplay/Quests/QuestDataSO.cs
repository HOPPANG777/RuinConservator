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
// 기능: class::QuestRewardData
// 설명: Progress flag 기반 보상 지급 데이터
//==============================================================================
[System.Serializable]
public class QuestRewardData
{
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

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 보상을 Progress 상태에 반영합니다.
    /// </summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    public void Apply(IProgressService progressService)
    {
        if (progressService == null || string.IsNullOrWhiteSpace(_rewardProgressFlagId))
        {
            return;
        }

        progressService.SetFlag(_rewardProgressFlagId, _rewardFlagValue);
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
    /// 등록된 보상을 지급합니다.
    /// </summary>
    /// <param name="progressService">진행 상태 서비스입니다.</param>
    public void ApplyRewards(IProgressService progressService)
    {
        if (_rewards == null)
        {
            return;
        }

        for (int i = 0; i < _rewards.Count; i++)
        {
            QuestRewardData reward = _rewards[i];

            if (reward != null)
            {
                reward.Apply(progressService);
            }
        }
    }
}
