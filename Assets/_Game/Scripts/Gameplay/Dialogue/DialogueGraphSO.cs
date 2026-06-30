using System.Collections.Generic;
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field)]
public sealed class DialoguePortraitIdAttribute : PropertyAttribute
{
}

//==============================================================================
// 기능: enum::DialogueChoiceActionType
// 설명: 선택지 선택 시 즉시 실행할 간단한 대화 액션 종류
//==============================================================================
public enum DialogueChoiceActionType
{
    NONE = 0,
    SET_PROGRESS_FLAG_TRUE,
    SET_PROGRESS_FLAG_FALSE,
    ACCEPT_QUEST,
    REFRESH_QUEST_PROGRESS,
    COMPLETE_QUEST,
    CLAIM_QUEST_REWARD,
    END_DIALOGUE
}

//==============================================================================
// 기능: class::DialogueChoiceData
// 설명: 대화 노드에 표시할 선택지와 선택 후 이동/액션 데이터
//==============================================================================
[System.Serializable]
public class DialogueChoiceData
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Choice")]
    [Tooltip("선택지 버튼에 표시할 문장입니다.")]
    [SerializeField]
    private string _choiceText;

    [Header("Choice")]
    [Tooltip("선택 후 이동할 다음 대화 노드 ID입니다. 비워두면 대화를 종료합니다.")]
    [SerializeField]
    private string _nextNodeId;

    [Header("Action")]
    [Tooltip("선택지를 고를 때 실행할 간단한 액션입니다.")]
    [SerializeField]
    private DialogueChoiceActionType _actionType;

    [Header("Action")]
    [Tooltip("액션에서 사용할 ID입니다. Progress Flag 액션에서는 flagId, Quest 액션에서는 questId로 사용합니다.")]
    [SerializeField]
    private string _actionId;

    [Header("Action")]
    [Tooltip("Quest 액션에서 직접 사용할 퀘스트 데이터입니다. 비워두면 Action Id를 questId로 사용합니다.")]
    [SerializeField]
    private QuestDataSO _questData;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>선택지 버튼에 표시할 문장입니다.</summary>
    public string ChoiceText => _choiceText;

    /// <summary>선택 후 이동할 다음 노드 ID입니다.</summary>
    public string NextNodeId => _nextNodeId;

    /// <summary>선택 시 실행할 액션 종류입니다.</summary>
    public DialogueChoiceActionType ActionType => _actionType;

    /// <summary>선택 시 실행할 액션 ID입니다.</summary>
    public string ActionId => _actionId;

    /// <summary>Quest 액션에서 직접 사용할 퀘스트 데이터입니다.</summary>
    public QuestDataSO QuestData => _questData;
}

//==============================================================================
// 기능: class::DialogueNodeData
// 설명: 한 번에 출력되는 대화 한 문단과 선택지 묶음
//==============================================================================
[System.Serializable]
public class DialogueNodeData
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Node")]
    [Tooltip("대화 그래프 안에서 고유한 노드 ID입니다.")]
    [SerializeField]
    private string _nodeId;

    [Header("Node")]
    [Tooltip("이 노드에서만 사용할 화자입니다. 비워두면 그래프 또는 NPC 기본 화자를 사용합니다.")]
    [SerializeField]
    private DialogueActorSO _actorOverride;

    [Header("Node")]
    [Tooltip("이 노드에서 사용할 초상화 ID입니다. 비워두면 화자의 기본 초상화를 사용합니다.")]
    [DialoguePortraitId]
    [SerializeField]
    private string _portraitId;

    [Header("Node")]
    [Tooltip("대화창에 출력할 본문입니다.")]
    [TextArea(3, 8)]
    [SerializeField]
    private string _bodyText;

    [Header("Node")]
    [Tooltip("선택지가 없을 때 입력으로 이동할 다음 노드 ID입니다. 비워두면 대화를 종료합니다.")]
    [SerializeField]
    private string _nextNodeId;

    [Header("Choices")]
    [Tooltip("이 노드에서 표시할 선택지 목록입니다.")]
    [SerializeField]
    private List<DialogueChoiceData> _choices = new List<DialogueChoiceData>();

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>대화 그래프 안에서 고유한 노드 ID입니다.</summary>
    public string NodeId => _nodeId;

    /// <summary>이 노드에서만 사용할 화자입니다.</summary>
    public DialogueActorSO ActorOverride => _actorOverride;

    /// <summary>이 노드에서 사용할 초상화 ID입니다.</summary>
    public string PortraitId => _portraitId;

    /// <summary>대화창에 출력할 본문입니다.</summary>
    public string BodyText => _bodyText;

    /// <summary>선택지가 없을 때 이동할 다음 노드 ID입니다.</summary>
    public string NextNodeId => _nextNodeId;

    /// <summary>이 노드에서 표시할 선택지 목록입니다.</summary>
    public IReadOnlyList<DialogueChoiceData> Choices => _choices;

    /// <summary>선택지가 하나 이상 있는지 여부입니다.</summary>
    public bool HasChoices => _choices != null && _choices.Count > 0;
}

//==============================================================================
// 기능: class::DialogueGraphSO
// 설명: NPC 대화 노드들을 보관하는 ScriptableObject 데이터
//==============================================================================
[CreateAssetMenu(fileName = "DialogueGraph", menuName = "Ruin Conservator/Dialogue/Graph")]
public class DialogueGraphSO : ScriptableObject
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Graph")]
    [Tooltip("대화 그래프 고유 ID입니다.")]
    [SerializeField]
    private string _graphId;

    [Header("Graph")]
    [Tooltip("노드에서 별도 화자를 지정하지 않았을 때 사용할 기본 화자입니다.")]
    [SerializeField]
    private DialogueActorSO _defaultActor;

    [Header("Graph")]
    [Tooltip("대화를 시작할 노드 ID입니다.")]
    [SerializeField]
    private string _startNodeId = "start";

    [Header("Nodes")]
    [Tooltip("대화 그래프에 포함된 노드 목록입니다.")]
    [SerializeField]
    private List<DialogueNodeData> _nodes = new List<DialogueNodeData>();

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>대화 그래프 고유 ID입니다.</summary>
    public string GraphId => _graphId;

    /// <summary>기본 화자 데이터입니다.</summary>
    public DialogueActorSO DefaultActor => _defaultActor;

    /// <summary>대화를 시작할 노드 ID입니다.</summary>
    public string StartNodeId => _startNodeId;

    /// <summary>대화 그래프에 포함된 노드 목록입니다.</summary>
    public IReadOnlyList<DialogueNodeData> Nodes => _nodes;

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 시작 노드를 조회합니다.
    /// </summary>
    /// <param name="node">조회된 시작 노드입니다.</param>
    /// <returns>시작 노드를 찾으면 true입니다.</returns>
    public bool TryGetStartNode(out DialogueNodeData node)
    {
        if (!string.IsNullOrWhiteSpace(_startNodeId) && TryGetNode(_startNodeId, out node))
        {
            return true;
        }

        if (_nodes != null && _nodes.Count > 0)
        {
            node = _nodes[0];
            return node != null;
        }

        node = null;
        return false;
    }

    /// <summary>
    /// 노드 ID로 대화 노드를 조회합니다.
    /// </summary>
    /// <param name="nodeId">조회할 노드 ID입니다.</param>
    /// <param name="node">조회된 노드입니다.</param>
    /// <returns>노드를 찾으면 true입니다.</returns>
    public bool TryGetNode(string nodeId, out DialogueNodeData node)
    {
        node = null;

        if (string.IsNullOrWhiteSpace(nodeId) || _nodes == null)
        {
            return false;
        }

        for (int i = 0; i < _nodes.Count; i++)
        {
            DialogueNodeData candidate = _nodes[i];

            if (candidate == null)
            {
                continue;
            }

            if (candidate.NodeId == nodeId)
            {
                node = candidate;
                return true;
            }
        }

        return false;
    }
}
