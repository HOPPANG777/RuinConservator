using System.Collections.Generic;

//==============================================================================
// 기능: enum::DialogueActionType
// 설명: CSV 대화 선택지에서 실행할 액션 종류
//==============================================================================
public enum DialogueActionType
{
    // 선택 시 별도 액션을 실행하지 않습니다.
    NONE = 0,

    // 지정한 Progress Flag를 true로 변경합니다.
    SET_PROGRESS_FLAG_TRUE,

    // 지정한 Progress Flag를 false로 변경합니다.
    SET_PROGRESS_FLAG_FALSE,

    // 지정한 퀘스트를 수락합니다.
    ACCEPT_QUEST,

    // 지정한 퀘스트의 목표 달성 상태를 갱신합니다.
    REFRESH_QUEST_PROGRESS,

    // 지정한 퀘스트의 완료를 확정합니다.
    COMPLETE_QUEST,

    // 지정한 퀘스트의 보상을 수령합니다.
    CLAIM_QUEST_REWARD,

    // 현재 대화를 즉시 종료합니다.
    END_DIALOGUE
}

//==============================================================================
// 기능: class::DialoguePortraitDefinition
// 설명: CSV에서 읽은 화자별 초상화와 Sprite ID 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class DialoguePortraitDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 이 초상화를 소유한 화자 ID입니다.
    private readonly string _actorId;

    // 동일 화자 안에서 사용하는 초상화 ID입니다.
    private readonly string _portraitId;

    // 초상화별 화자 표시명 Localization Key입니다.
    private readonly string _displayNameKey;

    // 실제 Sprite를 조회할 때 사용하는 에셋 ID입니다.
    private readonly string _spriteId;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>이 초상화를 소유한 화자 ID입니다.</summary>
    public string ActorId => _actorId;

    /// <summary>동일 화자 안에서 사용하는 초상화 ID입니다.</summary>
    public string PortraitId => _portraitId;

    /// <summary>초상화별 화자 표시명 Localization Key입니다.</summary>
    public string DisplayNameKey => _displayNameKey;

    /// <summary>실제 Sprite를 조회할 때 사용하는 에셋 ID입니다.</summary>
    public string SpriteId => _spriteId;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 화자별 초상화 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="actorId">이 초상화를 소유한 화자 ID입니다.</param>
    /// <param name="portraitId">동일 화자 안에서 사용하는 초상화 ID입니다.</param>
    /// <param name="displayNameKey">초상화별 화자 표시명 Localization Key입니다.</param>
    /// <param name="spriteId">실제 Sprite를 조회할 때 사용하는 에셋 ID입니다.</param>
    public DialoguePortraitDefinition(
        string actorId,
        string portraitId,
        string displayNameKey,
        string spriteId)
    {
        _actorId = actorId ?? string.Empty;
        _portraitId = portraitId ?? string.Empty;
        _displayNameKey = displayNameKey ?? string.Empty;
        _spriteId = spriteId ?? string.Empty;
    }
}

//==============================================================================
// 기능: class::DialogueActorDefinition
// 설명: CSV에서 읽은 대화 화자와 초상화 목록 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class DialogueActorDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 화자 고유 ID입니다.
    private readonly string _actorId;

    // 화면에 표시할 화자 이름 Localization Key입니다.
    private readonly string _displayNameKey;

    // 노드에서 초상화를 지정하지 않았을 때 사용할 기본 초상화 ID입니다.
    private readonly string _defaultPortraitId;

    // 이 화자가 사용할 수 있는 초상화 정의 목록입니다.
    private readonly List<DialoguePortraitDefinition> _portraits;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>화자 고유 ID입니다.</summary>
    public string ActorId => _actorId;

    /// <summary>화면에 표시할 화자 이름 Localization Key입니다.</summary>
    public string DisplayNameKey => _displayNameKey;

    /// <summary>노드에서 초상화를 지정하지 않았을 때 사용할 기본 초상화 ID입니다.</summary>
    public string DefaultPortraitId => _defaultPortraitId;

    /// <summary>이 화자가 사용할 수 있는 초상화 정의 목록입니다.</summary>
    public IReadOnlyList<DialoguePortraitDefinition> Portraits => _portraits;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 대화 화자 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="actorId">화자 고유 ID입니다.</param>
    /// <param name="displayNameKey">화자 이름 Localization Key입니다.</param>
    /// <param name="defaultPortraitId">기본 초상화 ID입니다.</param>
    /// <param name="portraits">이 화자가 사용할 수 있는 초상화 정의 목록입니다.</param>
    public DialogueActorDefinition(
        string actorId,
        string displayNameKey,
        string defaultPortraitId,
        IReadOnlyList<DialoguePortraitDefinition> portraits)
    {
        _actorId = actorId ?? string.Empty;
        _displayNameKey = displayNameKey ?? string.Empty;
        _defaultPortraitId = defaultPortraitId ?? string.Empty;
        _portraits = portraits != null
            ? new List<DialoguePortraitDefinition>(portraits)
            : new List<DialoguePortraitDefinition>();
    }
}

//==============================================================================
// 기능: class::DialogueChoiceDefinition
// 설명: CSV에서 읽은 대화 선택지와 선택 후 이동 및 액션 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class DialogueChoiceDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 이 선택지가 속한 대화 그래프 ID입니다.
    private readonly string _graphId;

    // 이 선택지가 속한 대화 노드 ID입니다.
    private readonly string _nodeId;

    // 동일 노드 안에서 선택지를 표시할 순서입니다.
    private readonly int _choiceOrder;

    // 선택지 버튼에 표시할 문장 Localization Key입니다.
    private readonly string _choiceTextKey;

    // 선택 후 이동할 다음 대화 노드 ID입니다.
    private readonly string _nextNodeId;

    // 선택 시 실행할 액션 종류입니다.
    private readonly DialogueActionType _actionType;

    // 선택 액션에서 사용할 Progress Flag 또는 Quest ID입니다.
    private readonly string _actionId;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>이 선택지가 속한 대화 그래프 ID입니다.</summary>
    public string GraphId => _graphId;

    /// <summary>이 선택지가 속한 대화 노드 ID입니다.</summary>
    public string NodeId => _nodeId;

    /// <summary>동일 노드 안에서 선택지를 표시할 순서입니다.</summary>
    public int ChoiceOrder => _choiceOrder;

    /// <summary>선택지 버튼에 표시할 문장 Localization Key입니다.</summary>
    public string ChoiceTextKey => _choiceTextKey;

    /// <summary>선택 후 이동할 다음 대화 노드 ID입니다.</summary>
    public string NextNodeId => _nextNodeId;

    /// <summary>선택 시 실행할 액션 종류입니다.</summary>
    public DialogueActionType ActionType => _actionType;

    /// <summary>선택 액션에서 사용할 Progress Flag 또는 Quest ID입니다.</summary>
    public string ActionId => _actionId;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 대화 선택지 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="graphId">이 선택지가 속한 대화 그래프 ID입니다.</param>
    /// <param name="nodeId">이 선택지가 속한 대화 노드 ID입니다.</param>
    /// <param name="choiceOrder">동일 노드 안에서 선택지를 표시할 순서입니다.</param>
    /// <param name="choiceTextKey">선택지 문장 Localization Key입니다.</param>
    /// <param name="nextNodeId">선택 후 이동할 다음 대화 노드 ID입니다.</param>
    /// <param name="actionType">선택 시 실행할 액션 종류입니다.</param>
    /// <param name="actionId">액션에서 사용할 Progress Flag 또는 Quest ID입니다.</param>
    public DialogueChoiceDefinition(
        string graphId,
        string nodeId,
        int choiceOrder,
        string choiceTextKey,
        string nextNodeId,
        DialogueActionType actionType,
        string actionId)
    {
        _graphId = graphId ?? string.Empty;
        _nodeId = nodeId ?? string.Empty;
        _choiceOrder = choiceOrder;
        _choiceTextKey = choiceTextKey ?? string.Empty;
        _nextNodeId = nextNodeId ?? string.Empty;
        _actionType = actionType;
        _actionId = actionId ?? string.Empty;
    }
}

//==============================================================================
// 기능: class::DialogueNodeDefinition
// 설명: CSV에서 읽은 대화 한 문단과 선택지 목록 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class DialogueNodeDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 이 노드가 속한 대화 그래프 ID입니다.
    private readonly string _graphId;

    // 동일 그래프 안에서 사용하는 대화 노드 ID입니다.
    private readonly string _nodeId;

    // 이 노드에서 말하는 화자 ID입니다.
    private readonly string _actorId;

    // 이 노드에서 사용할 화자의 초상화 ID입니다.
    private readonly string _portraitId;

    // 이 노드에서만 덮어쓸 대화창 배경 에셋 ID입니다.
    private readonly string _backgroundId;

    // 대화창에 표시할 본문 Localization Key입니다.
    private readonly string _bodyTextKey;

    // 선택지가 없을 때 이동할 다음 대화 노드 ID입니다.
    private readonly string _nextNodeId;

    // 이 노드에서 표시할 선택지 정의 목록입니다.
    private readonly List<DialogueChoiceDefinition> _choices;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>이 노드가 속한 대화 그래프 ID입니다.</summary>
    public string GraphId => _graphId;

    /// <summary>동일 그래프 안에서 사용하는 대화 노드 ID입니다.</summary>
    public string NodeId => _nodeId;

    /// <summary>이 노드에서 말하는 화자 ID입니다.</summary>
    public string ActorId => _actorId;

    /// <summary>이 노드에서 사용할 화자의 초상화 ID입니다.</summary>
    public string PortraitId => _portraitId;

    /// <summary>이 노드에서만 덮어쓸 대화창 배경 에셋 ID입니다.</summary>
    public string BackgroundId => _backgroundId;

    /// <summary>대화창에 표시할 본문 Localization Key입니다.</summary>
    public string BodyTextKey => _bodyTextKey;

    /// <summary>선택지가 없을 때 이동할 다음 대화 노드 ID입니다.</summary>
    public string NextNodeId => _nextNodeId;

    /// <summary>이 노드에서 표시할 선택지 정의 목록입니다.</summary>
    public IReadOnlyList<DialogueChoiceDefinition> Choices => _choices;

    /// <summary>이 노드에 선택지가 하나 이상 있는지 여부입니다.</summary>
    public bool HasChoices => _choices.Count > 0;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 대화 노드 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="graphId">이 노드가 속한 대화 그래프 ID입니다.</param>
    /// <param name="nodeId">동일 그래프 안에서 사용하는 대화 노드 ID입니다.</param>
    /// <param name="actorId">이 노드에서 말하는 화자 ID입니다.</param>
    /// <param name="portraitId">이 노드에서 사용할 화자의 초상화 ID입니다.</param>
    /// <param name="backgroundId">이 노드에서만 덮어쓸 대화창 배경 에셋 ID입니다.</param>
    /// <param name="bodyTextKey">대화 본문 Localization Key입니다.</param>
    /// <param name="nextNodeId">선택지가 없을 때 이동할 다음 대화 노드 ID입니다.</param>
    /// <param name="choices">이 노드에서 표시할 선택지 정의 목록입니다.</param>
    public DialogueNodeDefinition(
        string graphId,
        string nodeId,
        string actorId,
        string portraitId,
        string backgroundId,
        string bodyTextKey,
        string nextNodeId,
        IReadOnlyList<DialogueChoiceDefinition> choices)
    {
        _graphId = graphId ?? string.Empty;
        _nodeId = nodeId ?? string.Empty;
        _actorId = actorId ?? string.Empty;
        _portraitId = portraitId ?? string.Empty;
        _backgroundId = backgroundId ?? string.Empty;
        _bodyTextKey = bodyTextKey ?? string.Empty;
        _nextNodeId = nextNodeId ?? string.Empty;
        _choices = choices != null
            ? new List<DialogueChoiceDefinition>(choices)
            : new List<DialogueChoiceDefinition>();
    }
}

//==============================================================================
// 기능: class::DialogueGraphDefinition
// 설명: CSV에서 읽은 대화 그래프와 노드 목록 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class DialogueGraphDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 대화 그래프 고유 ID입니다.
    private readonly string _graphId;

    // 대화를 시작할 노드 ID입니다.
    private readonly string _startNodeId;

    // 노드에서 화자를 지정하지 않았을 때 사용할 기본 화자 ID입니다.
    private readonly string _defaultActorId;

    // 대화 UI 왼쪽에 표시할 화자 ID입니다.
    private readonly string _leftActorId;

    // 대화 UI 오른쪽에 표시할 화자 ID입니다.
    private readonly string _rightActorId;

    // 이 그래프에서 사용할 기본 대화창 배경 에셋 ID입니다.
    private readonly string _backgroundId;

    // 이 그래프에 포함된 대화 노드 정의 목록입니다.
    private readonly List<DialogueNodeDefinition> _nodes;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>대화 그래프 고유 ID입니다.</summary>
    public string GraphId => _graphId;

    /// <summary>대화를 시작할 노드 ID입니다.</summary>
    public string StartNodeId => _startNodeId;

    /// <summary>노드에서 화자를 지정하지 않았을 때 사용할 기본 화자 ID입니다.</summary>
    public string DefaultActorId => _defaultActorId;

    /// <summary>대화 UI 왼쪽에 표시할 화자 ID입니다.</summary>
    public string LeftActorId => _leftActorId;

    /// <summary>대화 UI 오른쪽에 표시할 화자 ID입니다.</summary>
    public string RightActorId => _rightActorId;

    /// <summary>이 그래프에서 사용할 기본 대화창 배경 에셋 ID입니다.</summary>
    public string BackgroundId => _backgroundId;

    /// <summary>이 그래프에 포함된 대화 노드 정의 목록입니다.</summary>
    public IReadOnlyList<DialogueNodeDefinition> Nodes => _nodes;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 대화 그래프 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="graphId">대화 그래프 고유 ID입니다.</param>
    /// <param name="startNodeId">대화를 시작할 노드 ID입니다.</param>
    /// <param name="defaultActorId">노드의 기본 화자 ID입니다.</param>
    /// <param name="leftActorId">대화 UI 왼쪽 화자 ID입니다.</param>
    /// <param name="rightActorId">대화 UI 오른쪽 화자 ID입니다.</param>
    /// <param name="backgroundId">그래프 기본 대화창 배경 에셋 ID입니다.</param>
    /// <param name="nodes">이 그래프에 포함된 대화 노드 정의 목록입니다.</param>
    public DialogueGraphDefinition(
        string graphId,
        string startNodeId,
        string defaultActorId,
        string leftActorId,
        string rightActorId,
        string backgroundId,
        IReadOnlyList<DialogueNodeDefinition> nodes)
    {
        _graphId = graphId ?? string.Empty;
        _startNodeId = startNodeId ?? string.Empty;
        _defaultActorId = defaultActorId ?? string.Empty;
        _leftActorId = leftActorId ?? string.Empty;
        _rightActorId = rightActorId ?? string.Empty;
        _backgroundId = backgroundId ?? string.Empty;
        _nodes = nodes != null
            ? new List<DialogueNodeDefinition>(nodes)
            : new List<DialogueNodeDefinition>();
    }
}
