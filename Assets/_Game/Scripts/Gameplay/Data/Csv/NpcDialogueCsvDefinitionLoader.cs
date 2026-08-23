using System;
using System.Collections.Generic;

//==============================================================================
// 기능: class::NpcDialogueCsvDefinitionLoader
// 설명: 선택 NPC 대화 CSV를 기본 연결과 퀘스트 상태별 경로 Definition으로 변환하는 로더
//==============================================================================
internal static class NpcDialogueCsvDefinitionLoader
{
    // NpcDialogueBindings CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] BINDING_HEADERS = { "npc_id", "default_graph_id", "actor_override_id" };

    // NpcQuestDialogueRoutes CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] ROUTE_HEADERS = { "npc_id", "route_order", "quest_id", "quest_state", "graph_id" };

    /// <summary>
    /// 선택 NPC 대화 CSV를 기본 연결과 퀘스트 상태별 경로 Definition으로 변환합니다.
    /// </summary>
    /// <param name="source">선택 NPC 대화 CSV 원본이 포함된 입력 묶음입니다.</param>
    /// <param name="bindings">변환된 NPC 기본 대화 연결 Definition 목록입니다.</param>
    /// <param name="routes">변환된 NPC 퀘스트 상태별 대화 경로 Definition 목록입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>입력이 없거나 모든 NPC 대화 행을 정상적으로 변환했으면 true입니다.</returns>
    internal static bool TryLoad(
        GameDataCsvSource source,
        out List<NpcDialogueBindingDefinition> bindings,
        out List<NpcQuestDialogueRouteDefinition> routes,
        out GameDataCsvLoadError loadError)
    {
        bindings = new List<NpcDialogueBindingDefinition>();
        routes = new List<NpcQuestDialogueRouteDefinition>();
        loadError = null;

        if (source == null)
        {
            loadError = new GameDataCsvLoadError("(missing)", 0, 0, string.Empty, "게임 데이터 CSV 원본 묶음이 없습니다.");
            return false;
        }

        if (!CsvDefinitionReadUtility.TryParseOptionalDocument(source.NpcDialogueBindings, BINDING_HEADERS, out CsvDocument bindingDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseOptionalDocument(source.NpcQuestDialogueRoutes, ROUTE_HEADERS, out CsvDocument routeDocument, out loadError))
        {
            return false;
        }

        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);

        if (!TryReadBindings(bindingDocument, bindings, npcIds, out loadError))
        {
            return false;
        }

        return TryReadRoutes(routeDocument, npcIds, routes, out loadError);
    }

    /// <summary>
    /// NpcDialogueBindings 문서가 있으면 기본 연결 Definition으로 변환합니다.
    /// </summary>
    private static bool TryReadBindings(
        CsvDocument document,
        List<NpcDialogueBindingDefinition> bindings,
        ISet<string> npcIds,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (document == null)
        {
            return true;
        }

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "npc_id", out string npcId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "default_graph_id", out string defaultGraphId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "actor_override_id", out string actorOverrideId, out loadError))
            {
                return false;
            }

            if (!npcIds.Add(npcId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "npc_id", "중복 NPC 기본 대화 연결이 있습니다.");
                return false;
            }

            bindings.Add(new NpcDialogueBindingDefinition(npcId, defaultGraphId, actorOverrideId));
        }

        return true;
    }

    /// <summary>
    /// NpcQuestDialogueRoutes 문서가 있으면 퀘스트 상태별 대화 경로 Definition으로 변환합니다.
    /// </summary>
    private static bool TryReadRoutes(
        CsvDocument document,
        ISet<string> npcIds,
        List<NpcQuestDialogueRouteDefinition> routes,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (document == null)
        {
            return true;
        }

        Dictionary<string, HashSet<int>> routeOrdersByNpcId = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "npc_id", out string npcId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(row, document.SourceName, "route_order", out int routeOrder, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "quest_id", out string questId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredValue(row, document.SourceName, "quest_state", out string questStateText, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "graph_id", out string graphId, out loadError))
            {
                return false;
            }

            if (!npcIds.Contains(npcId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "npc_id", "NPC 기본 대화 연결을 찾을 수 없습니다.");
                return false;
            }

            if (!TryParseQuestState(questStateText, out QuestState questState))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "quest_state", "지원하지 않는 퀘스트 상태입니다.");
                return false;
            }

            if (!routeOrdersByNpcId.TryGetValue(npcId, out HashSet<int> routeOrders))
            {
                routeOrders = new HashSet<int>();
                routeOrdersByNpcId.Add(npcId, routeOrders);
            }

            if (!routeOrders.Add(routeOrder))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "route_order", "동일 NPC에 중복 대화 경로 순서가 있습니다.");
                return false;
            }

            routes.Add(new NpcQuestDialogueRouteDefinition(npcId, routeOrder, questId, questState, graphId));
        }

        return true;
    }

    /// <summary>
    /// 퀘스트 상태 문자열을 명시적인 QuestState로 변환합니다.
    /// </summary>
    private static bool TryParseQuestState(string value, out QuestState questState)
    {
        questState = QuestState.QUEST_NOT_STARTED;

        switch (value)
        {
            case "QUEST_NOT_STARTED": questState = QuestState.QUEST_NOT_STARTED; return true;
            case "QUEST_IN_PROGRESS": questState = QuestState.QUEST_IN_PROGRESS; return true;
            case "QUEST_COMPLETED": questState = QuestState.QUEST_COMPLETED; return true;
            case "QUEST_REWARDED": questState = QuestState.QUEST_REWARDED; return true;
            default: return false;
        }
    }
}
