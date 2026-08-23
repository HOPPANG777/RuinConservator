using System.Collections.Generic;

//==============================================================================
// 기능: interface::IGameDataRepository
// 설명: CSV에서 구성한 게임 정의 데이터를 ID로 조회하는 읽기 전용 계약
//==============================================================================
public interface IGameDataRepository
{
    /// <summary>등록된 대화 화자 개수입니다.</summary>
    int DialogueActorCount { get; }

    /// <summary>등록된 대화 그래프 개수입니다.</summary>
    int DialogueGraphCount { get; }

    /// <summary>등록된 퀘스트 개수입니다.</summary>
    int QuestCount { get; }

    /// <summary>등록된 로컬라이징 문장 개수입니다.</summary>
    int LocalizationEntryCount { get; }

    /// <summary>
    /// 화자 ID로 대화 화자 정의를 조회합니다.
    /// </summary>
    /// <param name="actorId">조회할 화자 ID입니다.</param>
    /// <param name="actorDefinition">조회된 화자 정의입니다.</param>
    /// <returns>화자 정의를 찾으면 true입니다.</returns>
    bool TryGetDialogueActor(string actorId, out DialogueActorDefinition actorDefinition);

    /// <summary>
    /// 화자 ID와 초상화 ID로 초상화 정의를 조회합니다.
    /// </summary>
    /// <param name="actorId">초상화를 소유한 화자 ID입니다.</param>
    /// <param name="portraitId">조회할 초상화 ID입니다.</param>
    /// <param name="portraitDefinition">조회된 초상화 정의입니다.</param>
    /// <returns>초상화 정의를 찾으면 true입니다.</returns>
    bool TryGetDialoguePortrait(
        string actorId,
        string portraitId,
        out DialoguePortraitDefinition portraitDefinition);

    /// <summary>
    /// 그래프 ID로 대화 그래프 정의를 조회합니다.
    /// </summary>
    /// <param name="graphId">조회할 대화 그래프 ID입니다.</param>
    /// <param name="graphDefinition">조회된 대화 그래프 정의입니다.</param>
    /// <returns>대화 그래프 정의를 찾으면 true입니다.</returns>
    bool TryGetDialogueGraph(string graphId, out DialogueGraphDefinition graphDefinition);

    /// <summary>
    /// 그래프 ID와 노드 ID로 대화 노드 정의를 조회합니다.
    /// </summary>
    /// <param name="graphId">노드가 속한 대화 그래프 ID입니다.</param>
    /// <param name="nodeId">조회할 대화 노드 ID입니다.</param>
    /// <param name="nodeDefinition">조회된 대화 노드 정의입니다.</param>
    /// <returns>대화 노드 정의를 찾으면 true입니다.</returns>
    bool TryGetDialogueNode(
        string graphId,
        string nodeId,
        out DialogueNodeDefinition nodeDefinition);

    /// <summary>
    /// 퀘스트 ID로 퀘스트 정의를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <param name="questDefinition">조회된 퀘스트 정의입니다.</param>
    /// <returns>퀘스트 정의를 찾으면 true입니다.</returns>
    bool TryGetQuest(string questId, out QuestDefinition questDefinition);

    /// <summary>
    /// 문장 키로 로컬라이징 정의를 조회합니다.
    /// </summary>
    /// <param name="textKey">조회할 로컬라이징 문장 키입니다.</param>
    /// <param name="localizationEntry">조회된 로컬라이징 정의입니다.</param>
    /// <returns>로컬라이징 정의를 찾으면 true입니다.</returns>
    bool TryGetLocalizationEntry(
        string textKey,
        out LocalizationEntryDefinition localizationEntry);

    /// <summary>
    /// NPC ID로 기본 대화 연결 정의를 조회합니다.
    /// </summary>
    /// <param name="npcId">조회할 NPC ID입니다.</param>
    /// <param name="bindingDefinition">조회된 NPC 기본 대화 연결 정의입니다.</param>
    /// <returns>NPC 기본 대화 연결 정의를 찾으면 true입니다.</returns>
    bool TryGetNpcDialogueBinding(
        string npcId,
        out NpcDialogueBindingDefinition bindingDefinition);

    /// <summary>
    /// NPC ID로 우선순위가 정렬된 퀘스트 상태별 대화 경로를 조회합니다.
    /// </summary>
    /// <param name="npcId">조회할 NPC ID입니다.</param>
    /// <param name="routeDefinitions">우선순위가 정렬된 대화 경로 목록입니다.</param>
    /// <returns>NPC 대화 경로가 하나 이상 있으면 true입니다.</returns>
    bool TryGetNpcQuestDialogueRoutes(
        string npcId,
        out IReadOnlyList<NpcQuestDialogueRouteDefinition> routeDefinitions);
}
