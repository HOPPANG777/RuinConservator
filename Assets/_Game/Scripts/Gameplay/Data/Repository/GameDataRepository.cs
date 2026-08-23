using System;
using System.Collections.Generic;

//==============================================================================
// 기능: class::GameDataRepository
// 설명: 검증된 게임 정의 데이터를 ID 인덱스로 보관하는 읽기 전용 저장소
//==============================================================================
public sealed class GameDataRepository : IGameDataRepository
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 저장소 생성 오류에서 사용하는 대화 화자 데이터 종류 이름입니다.
    private const string DATA_TYPE_DIALOGUE_ACTOR = "DialogueActor";

    // 저장소 생성 오류에서 사용하는 대화 초상화 데이터 종류 이름입니다.
    private const string DATA_TYPE_DIALOGUE_PORTRAIT = "DialoguePortrait";

    // 저장소 생성 오류에서 사용하는 대화 그래프 데이터 종류 이름입니다.
    private const string DATA_TYPE_DIALOGUE_GRAPH = "DialogueGraph";

    // 저장소 생성 오류에서 사용하는 대화 노드 데이터 종류 이름입니다.
    private const string DATA_TYPE_DIALOGUE_NODE = "DialogueNode";

    // 저장소 생성 오류에서 사용하는 대화 선택지 데이터 종류 이름입니다.
    private const string DATA_TYPE_DIALOGUE_CHOICE = "DialogueChoice";

    // 저장소 생성 오류에서 사용하는 퀘스트 데이터 종류 이름입니다.
    private const string DATA_TYPE_QUEST = "Quest";

    // 저장소 생성 오류에서 사용하는 퀘스트 목표 데이터 종류 이름입니다.
    private const string DATA_TYPE_QUEST_OBJECTIVE = "QuestObjective";

    // 저장소 생성 오류에서 사용하는 퀘스트 보상 데이터 종류 이름입니다.
    private const string DATA_TYPE_QUEST_REWARD = "QuestReward";

    // 저장소 생성 오류에서 사용하는 퀘스트 소모 아이템 데이터 종류 이름입니다.
    private const string DATA_TYPE_QUEST_CONSUME_ITEM = "QuestConsumeItem";

    // 저장소 생성 오류에서 사용하는 로컬라이징 데이터 종류 이름입니다.
    private const string DATA_TYPE_LOCALIZATION = "Localization";

    // 저장소 생성 오류에서 사용하는 NPC 대화 연결 데이터 종류 이름입니다.
    private const string DATA_TYPE_NPC_DIALOGUE_BINDING = "NpcDialogueBinding";

    // 저장소 생성 오류에서 사용하는 NPC 퀘스트 대화 경로 데이터 종류 이름입니다.
    private const string DATA_TYPE_NPC_QUEST_DIALOGUE_ROUTE = "NpcQuestDialogueRoute";

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 화자 ID별 대화 화자 정의 인덱스입니다.
    private readonly Dictionary<string, DialogueActorDefinition> _dialogueActorById;

    // 화자 ID와 초상화 ID별 초상화 정의 인덱스입니다.
    private readonly Dictionary<string, Dictionary<string, DialoguePortraitDefinition>> _dialoguePortraitByActorId;

    // 그래프 ID별 대화 그래프 정의 인덱스입니다.
    private readonly Dictionary<string, DialogueGraphDefinition> _dialogueGraphById;

    // 그래프 ID와 노드 ID별 대화 노드 정의 인덱스입니다.
    private readonly Dictionary<string, Dictionary<string, DialogueNodeDefinition>> _dialogueNodeByGraphId;

    // 퀘스트 ID별 퀘스트 정의 인덱스입니다.
    private readonly Dictionary<string, QuestDefinition> _questById;

    // 문장 키별 로컬라이징 정의 인덱스입니다.
    private readonly Dictionary<string, LocalizationEntryDefinition> _localizationEntryByKey;

    // NPC ID별 기본 대화 연결 정의 인덱스입니다.
    private readonly Dictionary<string, NpcDialogueBindingDefinition> _npcDialogueBindingById;

    // NPC ID별 우선순위가 정렬된 퀘스트 상태 대화 경로 인덱스입니다.
    private readonly Dictionary<string, IReadOnlyList<NpcQuestDialogueRouteDefinition>> _npcQuestDialogueRoutesById;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>등록된 대화 화자 개수입니다.</summary>
    public int DialogueActorCount => _dialogueActorById.Count;

    /// <summary>등록된 대화 그래프 개수입니다.</summary>
    public int DialogueGraphCount => _dialogueGraphById.Count;

    /// <summary>등록된 퀘스트 개수입니다.</summary>
    public int QuestCount => _questById.Count;

    /// <summary>등록된 로컬라이징 문장 개수입니다.</summary>
    public int LocalizationEntryCount => _localizationEntryByKey.Count;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 비어 있는 게임 데이터 저장소와 대소문자를 구분하는 ID 인덱스를 생성합니다.
    /// </summary>
    private GameDataRepository()
    {
        _dialogueActorById = new Dictionary<string, DialogueActorDefinition>(StringComparer.Ordinal);
        _dialoguePortraitByActorId = new Dictionary<string, Dictionary<string, DialoguePortraitDefinition>>(StringComparer.Ordinal);
        _dialogueGraphById = new Dictionary<string, DialogueGraphDefinition>(StringComparer.Ordinal);
        _dialogueNodeByGraphId = new Dictionary<string, Dictionary<string, DialogueNodeDefinition>>(StringComparer.Ordinal);
        _questById = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
        _localizationEntryByKey = new Dictionary<string, LocalizationEntryDefinition>(StringComparer.Ordinal);
        _npcDialogueBindingById = new Dictionary<string, NpcDialogueBindingDefinition>(StringComparer.Ordinal);
        _npcQuestDialogueRoutesById = new Dictionary<string, IReadOnlyList<NpcQuestDialogueRouteDefinition>>(StringComparer.Ordinal);
    }

    //==============================================================================
    // 생성 API
    //==============================================================================

    /// <summary>
    /// 게임 정의 목록을 검증하고 ID 조회용 읽기 전용 저장소를 생성합니다.
    /// </summary>
    /// <param name="dialogueActors">대화 화자 정의 목록입니다.</param>
    /// <param name="dialogueGraphs">대화 그래프 정의 목록입니다.</param>
    /// <param name="quests">퀘스트 정의 목록입니다.</param>
    /// <param name="localizationEntries">로컬라이징 정의 목록입니다.</param>
    /// <param name="npcDialogueBindings">NPC 기본 대화 연결 정의 목록입니다.</param>
    /// <param name="npcQuestDialogueRoutes">NPC 퀘스트 상태별 대화 경로 정의 목록입니다.</param>
    /// <param name="repository">생성된 게임 데이터 저장소입니다.</param>
    /// <param name="buildError">저장소 생성을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 데이터를 검증하고 저장소를 생성했으면 true입니다.</returns>
    public static bool TryCreate(
        IReadOnlyList<DialogueActorDefinition> dialogueActors,
        IReadOnlyList<DialogueGraphDefinition> dialogueGraphs,
        IReadOnlyList<QuestDefinition> quests,
        IReadOnlyList<LocalizationEntryDefinition> localizationEntries,
        IReadOnlyList<NpcDialogueBindingDefinition> npcDialogueBindings,
        IReadOnlyList<NpcQuestDialogueRouteDefinition> npcQuestDialogueRoutes,
        out GameDataRepository repository,
        out GameDataRepositoryBuildError buildError)
    {
        repository = null;
        buildError = null;

        GameDataRepository candidateRepository = new GameDataRepository();

        if (!candidateRepository.TryAddLocalizationEntries(localizationEntries, out buildError))
        {
            return false;
        }

        if (!candidateRepository.TryAddDialogueActors(dialogueActors, out buildError))
        {
            return false;
        }

        if (!candidateRepository.TryAddQuests(quests, out buildError))
        {
            return false;
        }

        if (!candidateRepository.TryAddDialogueGraphs(dialogueGraphs, out buildError))
        {
            return false;
        }

        if (!candidateRepository.TryAddNpcDialogueBindings(npcDialogueBindings, out buildError))
        {
            return false;
        }

        if (!candidateRepository.TryAddNpcQuestDialogueRoutes(npcQuestDialogueRoutes, out buildError))
        {
            return false;
        }

        if (!candidateRepository.TryValidateReferences(out buildError))
        {
            return false;
        }

        repository = candidateRepository;
        return true;
    }

    //==============================================================================
    // 공개 조회 API
    //==============================================================================

    /// <summary>
    /// 화자 ID로 대화 화자 정의를 조회합니다.
    /// </summary>
    /// <param name="actorId">조회할 화자 ID입니다.</param>
    /// <param name="actorDefinition">조회된 화자 정의입니다.</param>
    /// <returns>화자 정의를 찾으면 true입니다.</returns>
    public bool TryGetDialogueActor(string actorId, out DialogueActorDefinition actorDefinition)
    {
        actorDefinition = null;

        if (string.IsNullOrWhiteSpace(actorId))
        {
            return false;
        }

        return _dialogueActorById.TryGetValue(actorId, out actorDefinition);
    }

    /// <summary>
    /// 화자 ID와 초상화 ID로 초상화 정의를 조회합니다.
    /// </summary>
    /// <param name="actorId">초상화를 소유한 화자 ID입니다.</param>
    /// <param name="portraitId">조회할 초상화 ID입니다.</param>
    /// <param name="portraitDefinition">조회된 초상화 정의입니다.</param>
    /// <returns>초상화 정의를 찾으면 true입니다.</returns>
    public bool TryGetDialoguePortrait(
        string actorId,
        string portraitId,
        out DialoguePortraitDefinition portraitDefinition)
    {
        portraitDefinition = null;

        if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(portraitId))
        {
            return false;
        }

        if (!_dialoguePortraitByActorId.TryGetValue(
            actorId,
            out Dictionary<string, DialoguePortraitDefinition> portraitById))
        {
            return false;
        }

        return portraitById.TryGetValue(portraitId, out portraitDefinition);
    }

    /// <summary>
    /// 그래프 ID로 대화 그래프 정의를 조회합니다.
    /// </summary>
    /// <param name="graphId">조회할 대화 그래프 ID입니다.</param>
    /// <param name="graphDefinition">조회된 대화 그래프 정의입니다.</param>
    /// <returns>대화 그래프 정의를 찾으면 true입니다.</returns>
    public bool TryGetDialogueGraph(string graphId, out DialogueGraphDefinition graphDefinition)
    {
        graphDefinition = null;

        if (string.IsNullOrWhiteSpace(graphId))
        {
            return false;
        }

        return _dialogueGraphById.TryGetValue(graphId, out graphDefinition);
    }

    /// <summary>
    /// 그래프 ID와 노드 ID로 대화 노드 정의를 조회합니다.
    /// </summary>
    /// <param name="graphId">노드가 속한 대화 그래프 ID입니다.</param>
    /// <param name="nodeId">조회할 대화 노드 ID입니다.</param>
    /// <param name="nodeDefinition">조회된 대화 노드 정의입니다.</param>
    /// <returns>대화 노드 정의를 찾으면 true입니다.</returns>
    public bool TryGetDialogueNode(
        string graphId,
        string nodeId,
        out DialogueNodeDefinition nodeDefinition)
    {
        nodeDefinition = null;

        if (string.IsNullOrWhiteSpace(graphId) || string.IsNullOrWhiteSpace(nodeId))
        {
            return false;
        }

        if (!_dialogueNodeByGraphId.TryGetValue(
            graphId,
            out Dictionary<string, DialogueNodeDefinition> nodeById))
        {
            return false;
        }

        return nodeById.TryGetValue(nodeId, out nodeDefinition);
    }

    /// <summary>
    /// 퀘스트 ID로 퀘스트 정의를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <param name="questDefinition">조회된 퀘스트 정의입니다.</param>
    /// <returns>퀘스트 정의를 찾으면 true입니다.</returns>
    public bool TryGetQuest(string questId, out QuestDefinition questDefinition)
    {
        questDefinition = null;

        if (string.IsNullOrWhiteSpace(questId))
        {
            return false;
        }

        return _questById.TryGetValue(questId, out questDefinition);
    }

    /// <summary>
    /// 문장 키로 로컬라이징 정의를 조회합니다.
    /// </summary>
    /// <param name="textKey">조회할 로컬라이징 문장 키입니다.</param>
    /// <param name="localizationEntry">조회된 로컬라이징 정의입니다.</param>
    /// <returns>로컬라이징 정의를 찾으면 true입니다.</returns>
    public bool TryGetLocalizationEntry(
        string textKey,
        out LocalizationEntryDefinition localizationEntry)
    {
        localizationEntry = null;

        if (string.IsNullOrWhiteSpace(textKey))
        {
            return false;
        }

        return _localizationEntryByKey.TryGetValue(textKey, out localizationEntry);
    }

    /// <summary>
    /// NPC ID로 기본 대화 연결 정의를 조회합니다.
    /// </summary>
    /// <param name="npcId">조회할 NPC ID입니다.</param>
    /// <param name="bindingDefinition">조회된 NPC 기본 대화 연결 정의입니다.</param>
    /// <returns>NPC 기본 대화 연결 정의를 찾으면 true입니다.</returns>
    public bool TryGetNpcDialogueBinding(
        string npcId,
        out NpcDialogueBindingDefinition bindingDefinition)
    {
        bindingDefinition = null;

        if (string.IsNullOrWhiteSpace(npcId))
        {
            return false;
        }

        return _npcDialogueBindingById.TryGetValue(npcId, out bindingDefinition);
    }

    /// <summary>
    /// NPC ID로 우선순위가 정렬된 퀘스트 상태별 대화 경로를 조회합니다.
    /// </summary>
    /// <param name="npcId">조회할 NPC ID입니다.</param>
    /// <param name="routeDefinitions">우선순위가 정렬된 대화 경로 목록입니다.</param>
    /// <returns>NPC 대화 경로가 하나 이상 있으면 true입니다.</returns>
    public bool TryGetNpcQuestDialogueRoutes(
        string npcId,
        out IReadOnlyList<NpcQuestDialogueRouteDefinition> routeDefinitions)
    {
        routeDefinitions = null;

        if (string.IsNullOrWhiteSpace(npcId))
        {
            return false;
        }

        return _npcQuestDialogueRoutesById.TryGetValue(npcId, out routeDefinitions);
    }

    //==============================================================================
    // 인덱스 구성
    //==============================================================================

    /// <summary>
    /// 로컬라이징 정의를 문장 키 인덱스에 추가합니다.
    /// </summary>
    /// <param name="localizationEntries">추가할 로컬라이징 정의 목록입니다.</param>
    /// <param name="buildError">인덱스 구성을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 로컬라이징 정의를 추가했으면 true입니다.</returns>
    private bool TryAddLocalizationEntries(
        IReadOnlyList<LocalizationEntryDefinition> localizationEntries,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        if (localizationEntries == null)
        {
            return true;
        }

        for (int i = 0; i < localizationEntries.Count; i++)
        {
            LocalizationEntryDefinition entry = localizationEntries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.TextKey))
            {
                return FailBuild(DATA_TYPE_LOCALIZATION, string.Empty, "문장 키가 비어 있습니다.", out buildError);
            }

            if (string.IsNullOrWhiteSpace(entry.KoreanText))
            {
                return FailBuild(DATA_TYPE_LOCALIZATION, entry.TextKey, "필수 한국어 문장이 비어 있습니다.", out buildError);
            }

            if (!_localizationEntryByKey.TryAdd(entry.TextKey, entry))
            {
                return FailBuild(DATA_TYPE_LOCALIZATION, entry.TextKey, "중복 문장 키가 있습니다.", out buildError);
            }
        }

        return true;
    }

    /// <summary>
    /// 대화 화자와 초상화 정의를 ID 인덱스에 추가합니다.
    /// </summary>
    /// <param name="dialogueActors">추가할 대화 화자 정의 목록입니다.</param>
    /// <param name="buildError">인덱스 구성을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 대화 화자와 초상화 정의를 추가했으면 true입니다.</returns>
    private bool TryAddDialogueActors(
        IReadOnlyList<DialogueActorDefinition> dialogueActors,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        if (dialogueActors == null)
        {
            return true;
        }

        for (int i = 0; i < dialogueActors.Count; i++)
        {
            DialogueActorDefinition actor = dialogueActors[i];

            if (actor == null || string.IsNullOrWhiteSpace(actor.ActorId))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_ACTOR, string.Empty, "화자 ID가 비어 있습니다.", out buildError);
            }

            if (string.IsNullOrWhiteSpace(actor.DisplayNameKey))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_ACTOR, actor.ActorId, "화자 이름 문장 키가 비어 있습니다.", out buildError);
            }

            if (!_dialogueActorById.TryAdd(actor.ActorId, actor))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_ACTOR, actor.ActorId, "중복 화자 ID가 있습니다.", out buildError);
            }

            Dictionary<string, DialoguePortraitDefinition> portraitById = new Dictionary<string, DialoguePortraitDefinition>(StringComparer.Ordinal);
            _dialoguePortraitByActorId.Add(actor.ActorId, portraitById);

            for (int portraitIndex = 0; portraitIndex < actor.Portraits.Count; portraitIndex++)
            {
                DialoguePortraitDefinition portrait = actor.Portraits[portraitIndex];

                if (portrait == null || string.IsNullOrWhiteSpace(portrait.PortraitId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_PORTRAIT, actor.ActorId, "초상화 ID가 비어 있습니다.", out buildError);
                }

                if (!string.Equals(portrait.ActorId, actor.ActorId, StringComparison.Ordinal))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_PORTRAIT, portrait.PortraitId, "초상화의 화자 ID가 부모 화자와 다릅니다.", out buildError);
                }

                if (string.IsNullOrWhiteSpace(portrait.SpriteId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_PORTRAIT, portrait.PortraitId, "Sprite ID가 비어 있습니다.", out buildError);
                }

                if (!portraitById.TryAdd(portrait.PortraitId, portrait))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_PORTRAIT, portrait.PortraitId, "동일 화자에 중복 초상화 ID가 있습니다.", out buildError);
                }
            }

            if (!string.IsNullOrWhiteSpace(actor.DefaultPortraitId)
                && !portraitById.ContainsKey(actor.DefaultPortraitId))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_ACTOR, actor.ActorId, "기본 초상화 ID를 찾을 수 없습니다.", out buildError);
            }
        }

        return true;
    }

    /// <summary>
    /// 퀘스트와 자식 정의의 구조를 검증하고 퀘스트 ID 인덱스에 추가합니다.
    /// </summary>
    /// <param name="quests">추가할 퀘스트 정의 목록입니다.</param>
    /// <param name="buildError">인덱스 구성을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 퀘스트 정의를 추가했으면 true입니다.</returns>
    private bool TryAddQuests(
        IReadOnlyList<QuestDefinition> quests,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        if (quests == null)
        {
            return true;
        }

        for (int i = 0; i < quests.Count; i++)
        {
            QuestDefinition quest = quests[i];

            if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
            {
                return FailBuild(DATA_TYPE_QUEST, string.Empty, "퀘스트 ID가 비어 있습니다.", out buildError);
            }

            if (!_questById.TryAdd(quest.QuestId, quest))
            {
                return FailBuild(DATA_TYPE_QUEST, quest.QuestId, "중복 퀘스트 ID가 있습니다.", out buildError);
            }

            if (!TryValidateQuestChildren(quest, out buildError))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 대화 그래프와 노드 정의를 ID 인덱스에 추가합니다.
    /// </summary>
    /// <param name="dialogueGraphs">추가할 대화 그래프 정의 목록입니다.</param>
    /// <param name="buildError">인덱스 구성을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 그래프와 노드 정의를 추가했으면 true입니다.</returns>
    private bool TryAddDialogueGraphs(
        IReadOnlyList<DialogueGraphDefinition> dialogueGraphs,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        if (dialogueGraphs == null)
        {
            return true;
        }

        for (int i = 0; i < dialogueGraphs.Count; i++)
        {
            DialogueGraphDefinition graph = dialogueGraphs[i];

            if (graph == null || string.IsNullOrWhiteSpace(graph.GraphId))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_GRAPH, string.Empty, "대화 그래프 ID가 비어 있습니다.", out buildError);
            }

            if (string.IsNullOrWhiteSpace(graph.StartNodeId))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_GRAPH, graph.GraphId, "시작 노드 ID가 비어 있습니다.", out buildError);
            }

            if (!_dialogueGraphById.TryAdd(graph.GraphId, graph))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_GRAPH, graph.GraphId, "중복 대화 그래프 ID가 있습니다.", out buildError);
            }

            Dictionary<string, DialogueNodeDefinition> nodeById = new Dictionary<string, DialogueNodeDefinition>(StringComparer.Ordinal);
            _dialogueNodeByGraphId.Add(graph.GraphId, nodeById);

            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                DialogueNodeDefinition node = graph.Nodes[nodeIndex];

                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_NODE, graph.GraphId, "대화 노드 ID가 비어 있습니다.", out buildError);
                }

                if (!string.Equals(node.GraphId, graph.GraphId, StringComparison.Ordinal))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_NODE, node.NodeId, "노드의 그래프 ID가 부모 그래프와 다릅니다.", out buildError);
                }

                if (string.IsNullOrWhiteSpace(node.BodyTextKey))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_NODE, node.NodeId, "대화 본문 문장 키가 비어 있습니다.", out buildError);
                }

                if (!nodeById.TryAdd(node.NodeId, node))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_NODE, node.NodeId, "동일 그래프에 중복 노드 ID가 있습니다.", out buildError);
                }

                if (!TryValidateDialogueChoices(graph.GraphId, node, out buildError))
                {
                    return false;
                }
            }

            if (!nodeById.ContainsKey(graph.StartNodeId))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_GRAPH, graph.GraphId, "시작 노드를 찾을 수 없습니다.", out buildError);
            }
        }

        return true;
    }

    /// <summary>
    /// NPC 기본 대화 연결 정의를 NPC ID 인덱스에 추가합니다.
    /// </summary>
    /// <param name="npcDialogueBindings">추가할 NPC 기본 대화 연결 정의 목록입니다.</param>
    /// <param name="buildError">인덱스 구성을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 NPC 기본 대화 연결 정의를 추가했으면 true입니다.</returns>
    private bool TryAddNpcDialogueBindings(
        IReadOnlyList<NpcDialogueBindingDefinition> npcDialogueBindings,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        if (npcDialogueBindings == null)
        {
            return true;
        }

        for (int i = 0; i < npcDialogueBindings.Count; i++)
        {
            NpcDialogueBindingDefinition binding = npcDialogueBindings[i];

            if (binding == null || string.IsNullOrWhiteSpace(binding.NpcId))
            {
                return FailBuild(DATA_TYPE_NPC_DIALOGUE_BINDING, string.Empty, "NPC ID가 비어 있습니다.", out buildError);
            }

            if (string.IsNullOrWhiteSpace(binding.DefaultGraphId))
            {
                return FailBuild(DATA_TYPE_NPC_DIALOGUE_BINDING, binding.NpcId, "기본 대화 그래프 ID가 비어 있습니다.", out buildError);
            }

            if (!_npcDialogueBindingById.TryAdd(binding.NpcId, binding))
            {
                return FailBuild(DATA_TYPE_NPC_DIALOGUE_BINDING, binding.NpcId, "중복 NPC 대화 연결이 있습니다.", out buildError);
            }
        }

        return true;
    }

    /// <summary>
    /// NPC 퀘스트 대화 경로를 NPC ID별로 묶고 우선순위로 정렬합니다.
    /// </summary>
    /// <param name="npcQuestDialogueRoutes">추가할 NPC 퀘스트 대화 경로 정의 목록입니다.</param>
    /// <param name="buildError">인덱스 구성을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 NPC 퀘스트 대화 경로 정의를 추가했으면 true입니다.</returns>
    private bool TryAddNpcQuestDialogueRoutes(
        IReadOnlyList<NpcQuestDialogueRouteDefinition> npcQuestDialogueRoutes,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        if (npcQuestDialogueRoutes == null)
        {
            return true;
        }

        Dictionary<string, List<NpcQuestDialogueRouteDefinition>> routesByNpcId = new Dictionary<string, List<NpcQuestDialogueRouteDefinition>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<int>> routeOrdersByNpcId = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

        for (int i = 0; i < npcQuestDialogueRoutes.Count; i++)
        {
            NpcQuestDialogueRouteDefinition route = npcQuestDialogueRoutes[i];

            if (route == null || string.IsNullOrWhiteSpace(route.NpcId))
            {
                return FailBuild(DATA_TYPE_NPC_QUEST_DIALOGUE_ROUTE, string.Empty, "NPC ID가 비어 있습니다.", out buildError);
            }

            if (route.RouteOrder < 0)
            {
                return FailBuild(DATA_TYPE_NPC_QUEST_DIALOGUE_ROUTE, route.NpcId, "경로 순서는 0 이상이어야 합니다.", out buildError);
            }

            if (string.IsNullOrWhiteSpace(route.QuestId) || string.IsNullOrWhiteSpace(route.GraphId))
            {
                return FailBuild(DATA_TYPE_NPC_QUEST_DIALOGUE_ROUTE, route.NpcId, "퀘스트 ID 또는 그래프 ID가 비어 있습니다.", out buildError);
            }

            if (!IsQuestStateValid(route.QuestState))
            {
                return FailBuild(DATA_TYPE_NPC_QUEST_DIALOGUE_ROUTE, route.NpcId, "지원하지 않는 퀘스트 상태입니다.", out buildError);
            }

            if (!routesByNpcId.TryGetValue(route.NpcId, out List<NpcQuestDialogueRouteDefinition> routes))
            {
                routes = new List<NpcQuestDialogueRouteDefinition>();
                routesByNpcId.Add(route.NpcId, routes);
                routeOrdersByNpcId.Add(route.NpcId, new HashSet<int>());
            }

            if (!routeOrdersByNpcId[route.NpcId].Add(route.RouteOrder))
            {
                return FailBuild(DATA_TYPE_NPC_QUEST_DIALOGUE_ROUTE, route.NpcId, "동일 NPC에 중복 경로 순서가 있습니다.", out buildError);
            }

            routes.Add(route);
        }

        foreach (KeyValuePair<string, List<NpcQuestDialogueRouteDefinition>> routePair in routesByNpcId)
        {
            routePair.Value.Sort(CompareNpcQuestDialogueRoutes);
            _npcQuestDialogueRoutesById.Add(routePair.Key, routePair.Value.AsReadOnly());
        }

        return true;
    }

    //==============================================================================
    // 구조 및 참조 검증
    //==============================================================================

    /// <summary>
    /// 퀘스트 목표, 보상, 소모 아이템의 부모 관계와 고유 키를 검증합니다.
    /// </summary>
    /// <param name="quest">검증할 퀘스트 정의입니다.</param>
    /// <param name="buildError">검증을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 퀘스트 자식 정의가 유효하면 true입니다.</returns>
    private static bool TryValidateQuestChildren(
        QuestDefinition quest,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;
        HashSet<string> objectiveIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<int> objectiveOrders = new HashSet<int>();
        HashSet<string> rewardIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<int> rewardOrders = new HashSet<int>();
        HashSet<string> consumeItemIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<int> consumeOrders = new HashSet<int>();

        for (int i = 0; i < quest.Objectives.Count; i++)
        {
            QuestObjectiveDefinition objective = quest.Objectives[i];

            if (objective == null || string.IsNullOrWhiteSpace(objective.ObjectiveId))
            {
                return FailBuild(DATA_TYPE_QUEST_OBJECTIVE, quest.QuestId, "목표 ID가 비어 있습니다.", out buildError);
            }

            if (!string.Equals(objective.QuestId, quest.QuestId, StringComparison.Ordinal))
            {
                return FailBuild(DATA_TYPE_QUEST_OBJECTIVE, objective.ObjectiveId, "목표의 퀘스트 ID가 부모 퀘스트와 다릅니다.", out buildError);
            }

            if (objective.ObjectiveOrder < 0
                || !objectiveIds.Add(objective.ObjectiveId)
                || !objectiveOrders.Add(objective.ObjectiveOrder))
            {
                return FailBuild(DATA_TYPE_QUEST_OBJECTIVE, objective.ObjectiveId, "목표 ID나 순서가 중복됐거나 순서가 음수입니다.", out buildError);
            }
        }

        for (int i = 0; i < quest.Rewards.Count; i++)
        {
            QuestRewardDefinition reward = quest.Rewards[i];

            if (reward == null || string.IsNullOrWhiteSpace(reward.RewardId))
            {
                return FailBuild(DATA_TYPE_QUEST_REWARD, quest.QuestId, "보상 ID가 비어 있습니다.", out buildError);
            }

            if (!string.Equals(reward.QuestId, quest.QuestId, StringComparison.Ordinal))
            {
                return FailBuild(DATA_TYPE_QUEST_REWARD, reward.RewardId, "보상의 퀘스트 ID가 부모 퀘스트와 다릅니다.", out buildError);
            }

            if (reward.RewardOrder < 0
                || !rewardIds.Add(reward.RewardId)
                || !rewardOrders.Add(reward.RewardOrder))
            {
                return FailBuild(DATA_TYPE_QUEST_REWARD, reward.RewardId, "보상 ID나 순서가 중복됐거나 순서가 음수입니다.", out buildError);
            }
        }

        for (int i = 0; i < quest.ConsumeItems.Count; i++)
        {
            QuestConsumeItemDefinition consumeItem = quest.ConsumeItems[i];

            if (consumeItem == null || string.IsNullOrWhiteSpace(consumeItem.ItemId))
            {
                return FailBuild(DATA_TYPE_QUEST_CONSUME_ITEM, quest.QuestId, "소모 아이템 ID가 비어 있습니다.", out buildError);
            }

            if (!string.Equals(consumeItem.QuestId, quest.QuestId, StringComparison.Ordinal))
            {
                return FailBuild(DATA_TYPE_QUEST_CONSUME_ITEM, consumeItem.ItemId, "소모 아이템의 퀘스트 ID가 부모 퀘스트와 다릅니다.", out buildError);
            }

            if (consumeItem.ConsumeOrder < 0
                || consumeItem.ItemAmount < 1
                || !consumeItemIds.Add(consumeItem.ItemId)
                || !consumeOrders.Add(consumeItem.ConsumeOrder))
            {
                return FailBuild(DATA_TYPE_QUEST_CONSUME_ITEM, consumeItem.ItemId, "아이템이나 순서가 중복됐거나 수량 또는 순서가 유효하지 않습니다.", out buildError);
            }
        }

        return true;
    }

    /// <summary>
    /// 대화 노드 선택지의 부모 관계와 순서 고유성을 검증합니다.
    /// </summary>
    /// <param name="graphId">선택지가 속한 대화 그래프 ID입니다.</param>
    /// <param name="node">선택지를 소유한 대화 노드입니다.</param>
    /// <param name="buildError">검증을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 선택지의 부모 관계와 순서가 유효하면 true입니다.</returns>
    private static bool TryValidateDialogueChoices(
        string graphId,
        DialogueNodeDefinition node,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;
        HashSet<int> choiceOrders = new HashSet<int>();

        for (int i = 0; i < node.Choices.Count; i++)
        {
            DialogueChoiceDefinition choice = node.Choices[i];

            if (choice == null)
            {
                return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, node.NodeId, "선택지 정의가 null입니다.", out buildError);
            }

            if (!string.Equals(choice.GraphId, graphId, StringComparison.Ordinal)
                || !string.Equals(choice.NodeId, node.NodeId, StringComparison.Ordinal))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, node.NodeId, "선택지의 그래프 또는 노드 ID가 부모 노드와 다릅니다.", out buildError);
            }

            if (choice.ChoiceOrder < 0 || !choiceOrders.Add(choice.ChoiceOrder))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, node.NodeId, "선택지 순서가 중복됐거나 음수입니다.", out buildError);
            }

            if (string.IsNullOrWhiteSpace(choice.ChoiceTextKey))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, node.NodeId, "선택지 문장 키가 비어 있습니다.", out buildError);
            }
        }

        return true;
    }

    /// <summary>
    /// 인덱스 구성이 끝난 전체 게임 데이터의 교차 참조를 검증합니다.
    /// </summary>
    /// <param name="buildError">검증을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 데이터 참조가 유효하면 true입니다.</returns>
    private bool TryValidateReferences(out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        if (!TryValidateDialogueActorReferences(out buildError))
        {
            return false;
        }

        if (!TryValidateDialogueGraphReferences(out buildError))
        {
            return false;
        }

        if (!TryValidateQuestReferences(out buildError))
        {
            return false;
        }

        return TryValidateNpcDialogueReferences(out buildError);
    }

    /// <summary>
    /// 대화 화자와 초상화의 로컬라이징 참조를 검증합니다.
    /// </summary>
    /// <param name="buildError">검증을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 대화 화자 참조가 유효하면 true입니다.</returns>
    private bool TryValidateDialogueActorReferences(out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        foreach (KeyValuePair<string, DialogueActorDefinition> actorPair in _dialogueActorById)
        {
            DialogueActorDefinition actor = actorPair.Value;

            if (!_localizationEntryByKey.ContainsKey(actor.DisplayNameKey))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_ACTOR, actor.ActorId, "화자 이름 문장 키를 찾을 수 없습니다.", out buildError);
            }

            for (int i = 0; i < actor.Portraits.Count; i++)
            {
                DialoguePortraitDefinition portrait = actor.Portraits[i];

                if (!string.IsNullOrWhiteSpace(portrait.DisplayNameKey)
                    && !_localizationEntryByKey.ContainsKey(portrait.DisplayNameKey))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_PORTRAIT, portrait.PortraitId, "초상화 표시 이름 문장 키를 찾을 수 없습니다.", out buildError);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 대화 그래프, 노드, 선택지의 연결 참조를 검증합니다.
    /// </summary>
    /// <param name="buildError">검증을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 대화 그래프 참조가 유효하면 true입니다.</returns>
    private bool TryValidateDialogueGraphReferences(out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        foreach (KeyValuePair<string, DialogueGraphDefinition> graphPair in _dialogueGraphById)
        {
            DialogueGraphDefinition graph = graphPair.Value;

            if (!IsOptionalActorIdValid(graph.DefaultActorId)
                || !IsOptionalActorIdValid(graph.LeftActorId)
                || !IsOptionalActorIdValid(graph.RightActorId))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_GRAPH, graph.GraphId, "그래프가 참조하는 화자 ID를 찾을 수 없습니다.", out buildError);
            }

            Dictionary<string, DialogueNodeDefinition> nodeById = _dialogueNodeByGraphId[graph.GraphId];

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                DialogueNodeDefinition node = graph.Nodes[i];
                string resolvedActorId = string.IsNullOrWhiteSpace(node.ActorId)
                    ? graph.DefaultActorId
                    : node.ActorId;

                if (!_localizationEntryByKey.ContainsKey(node.BodyTextKey))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_NODE, node.NodeId, "대화 본문 문장 키를 찾을 수 없습니다.", out buildError);
                }

                if (!IsOptionalActorIdValid(resolvedActorId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_NODE, node.NodeId, "노드에서 사용할 화자 ID를 찾을 수 없습니다.", out buildError);
                }

                if (!string.IsNullOrWhiteSpace(node.PortraitId)
                    && !TryGetDialoguePortrait(resolvedActorId, node.PortraitId, out _))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_NODE, node.NodeId, "노드에서 사용할 초상화 ID를 찾을 수 없습니다.", out buildError);
                }

                if (!string.IsNullOrWhiteSpace(node.NextNodeId)
                    && !nodeById.ContainsKey(node.NextNodeId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_NODE, node.NodeId, "다음 대화 노드 ID를 찾을 수 없습니다.", out buildError);
                }

                if (node.HasChoices && !string.IsNullOrWhiteSpace(node.NextNodeId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_NODE, node.NodeId, "선택지가 있는 노드는 기본 다음 노드를 가질 수 없습니다.", out buildError);
                }

                if (!TryValidateDialogueChoiceReferences(node, nodeById, out buildError))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 대화 선택지의 로컬라이징, 다음 노드, 액션 대상 참조를 검증합니다.
    /// </summary>
    /// <param name="node">선택지를 소유한 대화 노드입니다.</param>
    /// <param name="nodeById">같은 그래프의 대화 노드 인덱스입니다.</param>
    /// <param name="buildError">검증을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 대화 선택지 참조가 유효하면 true입니다.</returns>
    private bool TryValidateDialogueChoiceReferences(
        DialogueNodeDefinition node,
        IReadOnlyDictionary<string, DialogueNodeDefinition> nodeById,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        for (int i = 0; i < node.Choices.Count; i++)
        {
            DialogueChoiceDefinition choice = node.Choices[i];

            if (!_localizationEntryByKey.ContainsKey(choice.ChoiceTextKey))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, node.NodeId, "선택지 문장 키를 찾을 수 없습니다.", out buildError);
            }

            if (!string.IsNullOrWhiteSpace(choice.NextNodeId)
                && !nodeById.ContainsKey(choice.NextNodeId))
            {
                return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, node.NodeId, "선택지의 다음 노드 ID를 찾을 수 없습니다.", out buildError);
            }

            if (!TryValidateDialogueAction(choice, out buildError))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 대화 선택지 액션 종류에 따른 액션 ID 규칙과 퀘스트 참조를 검증합니다.
    /// </summary>
    /// <param name="choice">검증할 대화 선택지입니다.</param>
    /// <param name="buildError">검증을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>선택지 액션이 유효하면 true입니다.</returns>
    private bool TryValidateDialogueAction(
        DialogueChoiceDefinition choice,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        switch (choice.ActionType)
        {
            case DialogueActionType.NONE:
                if (!string.IsNullOrWhiteSpace(choice.ActionId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, choice.NodeId, "NONE 액션은 액션 ID를 가질 수 없습니다.", out buildError);
                }

                return true;

            case DialogueActionType.SET_PROGRESS_FLAG_TRUE:
            case DialogueActionType.SET_PROGRESS_FLAG_FALSE:
                if (string.IsNullOrWhiteSpace(choice.ActionId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, choice.NodeId, "Progress Flag 액션 ID가 비어 있습니다.", out buildError);
                }

                return true;

            case DialogueActionType.ACCEPT_QUEST:
            case DialogueActionType.REFRESH_QUEST_PROGRESS:
            case DialogueActionType.COMPLETE_QUEST:
            case DialogueActionType.CLAIM_QUEST_REWARD:
                if (string.IsNullOrWhiteSpace(choice.ActionId)
                    || !_questById.ContainsKey(choice.ActionId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, choice.NodeId, "Quest 액션 대상 ID를 찾을 수 없습니다.", out buildError);
                }

                return true;

            case DialogueActionType.END_DIALOGUE:
                if (!string.IsNullOrWhiteSpace(choice.ActionId)
                    || !string.IsNullOrWhiteSpace(choice.NextNodeId))
                {
                    return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, choice.NodeId, "END_DIALOGUE 액션은 액션 ID와 다음 노드를 가질 수 없습니다.", out buildError);
                }

                return true;

            default:
                return FailBuild(DATA_TYPE_DIALOGUE_CHOICE, choice.NodeId, "지원하지 않는 대화 액션 종류입니다.", out buildError);
        }
    }

    /// <summary>
    /// 퀘스트와 목표 및 보상의 로컬라이징과 조건부 필드를 검증합니다.
    /// </summary>
    /// <param name="buildError">검증을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 퀘스트 참조와 조건부 필드가 유효하면 true입니다.</returns>
    private bool TryValidateQuestReferences(out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        foreach (KeyValuePair<string, QuestDefinition> questPair in _questById)
        {
            QuestDefinition quest = questPair.Value;

            if (!_localizationEntryByKey.ContainsKey(quest.TitleTextKey)
                || !_localizationEntryByKey.ContainsKey(quest.DescriptionTextKey))
            {
                return FailBuild(DATA_TYPE_QUEST, quest.QuestId, "퀘스트 제목 또는 설명 문장 키를 찾을 수 없습니다.", out buildError);
            }

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = quest.Objectives[i];

                if (!_localizationEntryByKey.ContainsKey(objective.DescriptionTextKey)
                    || string.IsNullOrWhiteSpace(objective.RequiredProgressFlagId))
                {
                    return FailBuild(DATA_TYPE_QUEST_OBJECTIVE, objective.ObjectiveId, "목표 설명 키 또는 Progress Flag ID가 유효하지 않습니다.", out buildError);
                }
            }

            for (int i = 0; i < quest.Rewards.Count; i++)
            {
                QuestRewardDefinition reward = quest.Rewards[i];

                if (!_localizationEntryByKey.ContainsKey(reward.DescriptionTextKey)
                    || !AreQuestRewardFieldsValid(reward))
                {
                    return FailBuild(DATA_TYPE_QUEST_REWARD, reward.RewardId, "보상 설명 키 또는 조건부 보상 필드가 유효하지 않습니다.", out buildError);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 퀘스트 보상의 Flag 및 아이템 조건부 필드 조합을 검증합니다.
    /// </summary>
    /// <param name="reward">검증할 퀘스트 보상 정의입니다.</param>
    /// <returns>Flag 또는 아이템 보상이 하나 이상 존재하고 조건부 값이 유효하면 true입니다.</returns>
    private static bool AreQuestRewardFieldsValid(QuestRewardDefinition reward)
    {
        bool hasProgressFlag = !string.IsNullOrWhiteSpace(reward.ProgressFlagId);
        bool hasItem = !string.IsNullOrWhiteSpace(reward.ItemId);

        if (!hasProgressFlag && !hasItem)
        {
            return false;
        }

        if (hasProgressFlag != reward.FlagValue.HasValue)
        {
            return false;
        }

        if (hasItem != reward.ItemAmount.HasValue)
        {
            return false;
        }

        return !hasItem || reward.ItemAmount.Value > 0;
    }

    /// <summary>
    /// NPC 기본 대화 연결과 퀘스트 상태별 대화 경로 참조를 검증합니다.
    /// </summary>
    /// <param name="buildError">검증을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 NPC 대화 참조가 유효하면 true입니다.</returns>
    private bool TryValidateNpcDialogueReferences(out GameDataRepositoryBuildError buildError)
    {
        buildError = null;

        foreach (KeyValuePair<string, NpcDialogueBindingDefinition> bindingPair in _npcDialogueBindingById)
        {
            NpcDialogueBindingDefinition binding = bindingPair.Value;

            if (!_dialogueGraphById.ContainsKey(binding.DefaultGraphId)
                || !IsOptionalActorIdValid(binding.ActorOverrideId))
            {
                return FailBuild(DATA_TYPE_NPC_DIALOGUE_BINDING, binding.NpcId, "기본 그래프 또는 화자 오버라이드 ID를 찾을 수 없습니다.", out buildError);
            }
        }

        foreach (KeyValuePair<string, IReadOnlyList<NpcQuestDialogueRouteDefinition>> routePair in _npcQuestDialogueRoutesById)
        {
            if (!_npcDialogueBindingById.ContainsKey(routePair.Key))
            {
                return FailBuild(DATA_TYPE_NPC_QUEST_DIALOGUE_ROUTE, routePair.Key, "NPC 기본 대화 연결을 찾을 수 없습니다.", out buildError);
            }

            IReadOnlyList<NpcQuestDialogueRouteDefinition> routes = routePair.Value;

            for (int i = 0; i < routes.Count; i++)
            {
                NpcQuestDialogueRouteDefinition route = routes[i];

                if (!_questById.ContainsKey(route.QuestId)
                    || !_dialogueGraphById.ContainsKey(route.GraphId))
                {
                    return FailBuild(DATA_TYPE_NPC_QUEST_DIALOGUE_ROUTE, route.NpcId, "경로의 퀘스트 또는 그래프 ID를 찾을 수 없습니다.", out buildError);
                }
            }
        }

        return true;
    }

    //==============================================================================
    // 내부 유틸리티
    //==============================================================================

    /// <summary>
    /// 선택적 화자 ID가 비어 있거나 등록된 화자 ID인지 확인합니다.
    /// </summary>
    /// <param name="actorId">검증할 선택적 화자 ID입니다.</param>
    /// <returns>비어 있거나 등록된 화자 ID이면 true입니다.</returns>
    private bool IsOptionalActorIdValid(string actorId)
    {
        return string.IsNullOrWhiteSpace(actorId) || _dialogueActorById.ContainsKey(actorId);
    }

    /// <summary>
    /// 퀘스트 상태가 현재 저장소에서 지원하는 명시적 상태인지 확인합니다.
    /// </summary>
    /// <param name="questState">검증할 퀘스트 상태입니다.</param>
    /// <returns>지원하는 퀘스트 상태이면 true입니다.</returns>
    private static bool IsQuestStateValid(QuestState questState)
    {
        switch (questState)
        {
            case QuestState.QUEST_NOT_STARTED:
            case QuestState.QUEST_IN_PROGRESS:
            case QuestState.QUEST_COMPLETED:
            case QuestState.QUEST_REWARDED:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// NPC 퀘스트 대화 경로를 경로 우선순위 오름차순으로 비교합니다.
    /// </summary>
    /// <param name="left">왼쪽 비교 대상 경로입니다.</param>
    /// <param name="right">오른쪽 비교 대상 경로입니다.</param>
    /// <returns>왼쪽 우선순위가 작으면 음수, 같으면 0, 크면 양수입니다.</returns>
    private static int CompareNpcQuestDialogueRoutes(
        NpcQuestDialogueRouteDefinition left,
        NpcQuestDialogueRouteDefinition right)
    {
        return left.RouteOrder.CompareTo(right.RouteOrder);
    }

    /// <summary>
    /// 저장소 생성 실패 정보를 생성하고 false를 반환합니다.
    /// </summary>
    /// <param name="dataType">오류가 발생한 데이터 종류입니다.</param>
    /// <param name="dataId">오류가 발생한 데이터 ID입니다.</param>
    /// <param name="message">저장소를 생성할 수 없는 원인입니다.</param>
    /// <param name="buildError">생성된 저장소 생성 오류입니다.</param>
    /// <returns>항상 false입니다.</returns>
    private static bool FailBuild(
        string dataType,
        string dataId,
        string message,
        out GameDataRepositoryBuildError buildError)
    {
        buildError = new GameDataRepositoryBuildError(dataType, dataId, message);
        return false;
    }
}
