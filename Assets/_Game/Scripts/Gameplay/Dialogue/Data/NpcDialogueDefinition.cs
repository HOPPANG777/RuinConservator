//==============================================================================
// 기능: class::NpcDialogueBindingDefinition
// 설명: CSV에서 읽은 NPC별 기본 대화 그래프 연결 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class NpcDialogueBindingDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 대화를 연결할 NPC 고유 ID입니다.
    private readonly string _npcId;

    // 아무 퀘스트 상태 조건도 맞지 않을 때 실행할 기본 대화 그래프 ID입니다.
    private readonly string _defaultGraphId;

    // NPC별로 덮어쓸 대화 화자 ID입니다.
    private readonly string _actorOverrideId;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>대화를 연결할 NPC 고유 ID입니다.</summary>
    public string NpcId => _npcId;

    /// <summary>아무 퀘스트 상태 조건도 맞지 않을 때 실행할 기본 대화 그래프 ID입니다.</summary>
    public string DefaultGraphId => _defaultGraphId;

    /// <summary>NPC별로 덮어쓸 대화 화자 ID입니다.</summary>
    public string ActorOverrideId => _actorOverrideId;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// NPC별 기본 대화 연결 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="npcId">대화를 연결할 NPC 고유 ID입니다.</param>
    /// <param name="defaultGraphId">조건이 없을 때 실행할 기본 대화 그래프 ID입니다.</param>
    /// <param name="actorOverrideId">NPC별로 덮어쓸 대화 화자 ID입니다.</param>
    public NpcDialogueBindingDefinition(
        string npcId,
        string defaultGraphId,
        string actorOverrideId)
    {
        _npcId = npcId ?? string.Empty;
        _defaultGraphId = defaultGraphId ?? string.Empty;
        _actorOverrideId = actorOverrideId ?? string.Empty;
    }
}

//==============================================================================
// 기능: class::NpcQuestDialogueRouteDefinition
// 설명: CSV에서 읽은 NPC의 퀘스트 상태별 대화 그래프 연결 정의 데이터
//==============================================================================
[System.Serializable]
public sealed class NpcQuestDialogueRouteDefinition
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 퀘스트 상태별 대화를 연결할 NPC 고유 ID입니다.
    private readonly string _npcId;

    // 동일 NPC의 대화 조건을 검사할 우선순위입니다.
    private readonly int _routeOrder;

    // 대화 분기에 사용할 퀘스트 고유 ID입니다.
    private readonly string _questId;

    // 대화 그래프를 선택할 때 비교할 퀘스트 상태입니다.
    private readonly QuestState _questState;

    // 조건이 일치할 때 실행할 대화 그래프 ID입니다.
    private readonly string _graphId;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>퀘스트 상태별 대화를 연결할 NPC 고유 ID입니다.</summary>
    public string NpcId => _npcId;

    /// <summary>동일 NPC의 대화 조건을 검사할 우선순위입니다.</summary>
    public int RouteOrder => _routeOrder;

    /// <summary>대화 분기에 사용할 퀘스트 고유 ID입니다.</summary>
    public string QuestId => _questId;

    /// <summary>대화 그래프를 선택할 때 비교할 퀘스트 상태입니다.</summary>
    public QuestState QuestState => _questState;

    /// <summary>조건이 일치할 때 실행할 대화 그래프 ID입니다.</summary>
    public string GraphId => _graphId;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// NPC의 퀘스트 상태별 대화 연결 정의 데이터를 생성합니다.
    /// </summary>
    /// <param name="npcId">퀘스트 상태별 대화를 연결할 NPC 고유 ID입니다.</param>
    /// <param name="routeOrder">동일 NPC의 대화 조건을 검사할 우선순위입니다.</param>
    /// <param name="questId">대화 분기에 사용할 퀘스트 고유 ID입니다.</param>
    /// <param name="questState">대화 그래프를 선택할 때 비교할 퀘스트 상태입니다.</param>
    /// <param name="graphId">조건이 일치할 때 실행할 대화 그래프 ID입니다.</param>
    public NpcQuestDialogueRouteDefinition(
        string npcId,
        int routeOrder,
        string questId,
        QuestState questState,
        string graphId)
    {
        _npcId = npcId ?? string.Empty;
        _routeOrder = routeOrder;
        _questId = questId ?? string.Empty;
        _questState = questState;
        _graphId = graphId ?? string.Empty;
    }
}
