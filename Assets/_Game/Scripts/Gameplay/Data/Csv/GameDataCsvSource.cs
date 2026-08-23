//==============================================================================
// 기능: class::GameDataCsvFile
// 설명: CSV 원본 이름과 전체 문자열을 함께 전달하는 순수 C# 입력 데이터
//==============================================================================
public sealed class GameDataCsvFile
{
    // CSV 오류에 표시할 원본 이름입니다.
    private readonly string _sourceName;

    // 파싱할 전체 CSV 문자열입니다.
    private readonly string _text;

    /// <summary>CSV 오류에 표시할 원본 이름입니다.</summary>
    public string SourceName => _sourceName;

    /// <summary>파싱할 전체 CSV 문자열입니다.</summary>
    public string Text => _text;

    /// <summary>
    /// CSV 원본 이름과 전체 문자열을 생성합니다.
    /// </summary>
    /// <param name="sourceName">CSV 오류에 표시할 원본 이름입니다.</param>
    /// <param name="text">파싱할 전체 CSV 문자열입니다.</param>
    public GameDataCsvFile(string sourceName, string text)
    {
        _sourceName = sourceName ?? string.Empty;
        _text = text ?? string.Empty;
    }
}

//==============================================================================
// 기능: class::GameDataCsvSource
// 설명: 게임 데이터 저장소 한 개를 구성하는 필수 및 선택 CSV 원본 묶음
//==============================================================================
public sealed class GameDataCsvSource
{
    // 대화 화자 CSV 원본입니다.
    private readonly GameDataCsvFile _dialogueActors;

    // 대화 초상화 CSV 원본입니다.
    private readonly GameDataCsvFile _dialoguePortraits;

    // 대화 그래프 CSV 원본입니다.
    private readonly GameDataCsvFile _dialogueGraphs;

    // 대화 노드 CSV 원본입니다.
    private readonly GameDataCsvFile _dialogueNodes;

    // 대화 선택지 CSV 원본입니다.
    private readonly GameDataCsvFile _dialogueChoices;

    // 퀘스트 CSV 원본입니다.
    private readonly GameDataCsvFile _quests;

    // 퀘스트 목표 CSV 원본입니다.
    private readonly GameDataCsvFile _questObjectives;

    // 퀘스트 보상 CSV 원본입니다.
    private readonly GameDataCsvFile _questRewards;

    // 퀘스트 소모 아이템 CSV 원본입니다.
    private readonly GameDataCsvFile _questConsumeItems;

    // 로컬라이징 CSV 원본입니다.
    private readonly GameDataCsvFile _localization;

    // 선택 입력인 NPC 기본 대화 연결 CSV 원본입니다.
    private readonly GameDataCsvFile _npcDialogueBindings;

    // 선택 입력인 NPC 퀘스트 상태별 대화 경로 CSV 원본입니다.
    private readonly GameDataCsvFile _npcQuestDialogueRoutes;

    /// <summary>대화 화자 CSV 원본입니다.</summary>
    public GameDataCsvFile DialogueActors => _dialogueActors;

    /// <summary>대화 초상화 CSV 원본입니다.</summary>
    public GameDataCsvFile DialoguePortraits => _dialoguePortraits;

    /// <summary>대화 그래프 CSV 원본입니다.</summary>
    public GameDataCsvFile DialogueGraphs => _dialogueGraphs;

    /// <summary>대화 노드 CSV 원본입니다.</summary>
    public GameDataCsvFile DialogueNodes => _dialogueNodes;

    /// <summary>대화 선택지 CSV 원본입니다.</summary>
    public GameDataCsvFile DialogueChoices => _dialogueChoices;

    /// <summary>퀘스트 CSV 원본입니다.</summary>
    public GameDataCsvFile Quests => _quests;

    /// <summary>퀘스트 목표 CSV 원본입니다.</summary>
    public GameDataCsvFile QuestObjectives => _questObjectives;

    /// <summary>퀘스트 보상 CSV 원본입니다.</summary>
    public GameDataCsvFile QuestRewards => _questRewards;

    /// <summary>퀘스트 소모 아이템 CSV 원본입니다.</summary>
    public GameDataCsvFile QuestConsumeItems => _questConsumeItems;

    /// <summary>로컬라이징 CSV 원본입니다.</summary>
    public GameDataCsvFile Localization => _localization;

    /// <summary>선택 입력인 NPC 기본 대화 연결 CSV 원본입니다.</summary>
    public GameDataCsvFile NpcDialogueBindings => _npcDialogueBindings;

    /// <summary>선택 입력인 NPC 퀘스트 상태별 대화 경로 CSV 원본입니다.</summary>
    public GameDataCsvFile NpcQuestDialogueRoutes => _npcQuestDialogueRoutes;

    /// <summary>
    /// 게임 데이터 저장소를 구성할 CSV 원본 묶음을 생성합니다.
    /// </summary>
    /// <param name="dialogueActors">대화 화자 CSV 원본입니다.</param>
    /// <param name="dialoguePortraits">대화 초상화 CSV 원본입니다.</param>
    /// <param name="dialogueGraphs">대화 그래프 CSV 원본입니다.</param>
    /// <param name="dialogueNodes">대화 노드 CSV 원본입니다.</param>
    /// <param name="dialogueChoices">대화 선택지 CSV 원본입니다.</param>
    /// <param name="quests">퀘스트 CSV 원본입니다.</param>
    /// <param name="questObjectives">퀘스트 목표 CSV 원본입니다.</param>
    /// <param name="questRewards">퀘스트 보상 CSV 원본입니다.</param>
    /// <param name="questConsumeItems">퀘스트 소모 아이템 CSV 원본입니다.</param>
    /// <param name="localization">로컬라이징 CSV 원본입니다.</param>
    /// <param name="npcDialogueBindings">선택 입력인 NPC 기본 대화 연결 CSV 원본입니다.</param>
    /// <param name="npcQuestDialogueRoutes">선택 입력인 NPC 퀘스트 상태별 대화 경로 CSV 원본입니다.</param>
    public GameDataCsvSource(
        GameDataCsvFile dialogueActors,
        GameDataCsvFile dialoguePortraits,
        GameDataCsvFile dialogueGraphs,
        GameDataCsvFile dialogueNodes,
        GameDataCsvFile dialogueChoices,
        GameDataCsvFile quests,
        GameDataCsvFile questObjectives,
        GameDataCsvFile questRewards,
        GameDataCsvFile questConsumeItems,
        GameDataCsvFile localization,
        GameDataCsvFile npcDialogueBindings = null,
        GameDataCsvFile npcQuestDialogueRoutes = null)
    {
        _dialogueActors = dialogueActors;
        _dialoguePortraits = dialoguePortraits;
        _dialogueGraphs = dialogueGraphs;
        _dialogueNodes = dialogueNodes;
        _dialogueChoices = dialogueChoices;
        _quests = quests;
        _questObjectives = questObjectives;
        _questRewards = questRewards;
        _questConsumeItems = questConsumeItems;
        _localization = localization;
        _npcDialogueBindings = npcDialogueBindings;
        _npcQuestDialogueRoutes = npcQuestDialogueRoutes;
    }
}
