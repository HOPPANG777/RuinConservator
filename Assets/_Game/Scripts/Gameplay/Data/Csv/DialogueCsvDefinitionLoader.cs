using System;
using System.Collections.Generic;

//==============================================================================
// 기능: class::DialogueCsvDefinitionLoader
// 설명: 런타임과 에디터에서 다섯 개 Dialogue CSV를 화자와 그래프 Definition 목록으로 조립하는 로더
//==============================================================================
public static class DialogueCsvDefinitionLoader
{
    // DialogueActors CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] ACTOR_HEADERS = { "actor_id", "display_name_key", "default_portrait_id" };

    // DialoguePortraits CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] PORTRAIT_HEADERS = { "actor_id", "portrait_id", "display_name_key", "sprite_id" };

    // DialogueGraphs CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] GRAPH_HEADERS = { "graph_id", "start_node_id", "default_actor_id", "left_actor_id", "right_actor_id", "background_id" };

    // DialogueNodes CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] NODE_HEADERS = { "graph_id", "node_id", "actor_id", "portrait_id", "background_id", "body_text_key", "next_node_id" };

    // DialogueChoices CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] CHOICE_HEADERS = { "graph_id", "node_id", "choice_order", "choice_text_key", "next_node_id", "action_type", "action_id" };

    /// <summary>
    /// 다섯 개 Dialogue CSV를 화자와 그래프 Definition 목록으로 변환합니다.
    /// </summary>
    /// <param name="source">Dialogue CSV 원본이 포함된 게임 데이터 입력 묶음입니다.</param>
    /// <param name="actorDefinitions">변환된 대화 화자 Definition 목록입니다.</param>
    /// <param name="graphDefinitions">변환된 대화 그래프 Definition 목록입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 Dialogue CSV를 정상적으로 조립했으면 true입니다.</returns>
    public static bool TryLoad(
        GameDataCsvSource source,
        out List<DialogueActorDefinition> actorDefinitions,
        out List<DialogueGraphDefinition> graphDefinitions,
        out GameDataCsvLoadError loadError)
    {
        actorDefinitions = new List<DialogueActorDefinition>();
        graphDefinitions = new List<DialogueGraphDefinition>();
        loadError = null;

        if (source == null)
        {
            loadError = new GameDataCsvLoadError("(missing)", 0, 0, string.Empty, "게임 데이터 CSV 원본 묶음이 없습니다.");
            return false;
        }

        if (!CsvDefinitionReadUtility.TryParseRequiredDocument(source.DialogueActors, ACTOR_HEADERS, out CsvDocument actorDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(source.DialoguePortraits, PORTRAIT_HEADERS, out CsvDocument portraitDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(source.DialogueGraphs, GRAPH_HEADERS, out CsvDocument graphDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(source.DialogueNodes, NODE_HEADERS, out CsvDocument nodeDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(source.DialogueChoices, CHOICE_HEADERS, out CsvDocument choiceDocument, out loadError))
        {
            return false;
        }

        if (!TryLoadActors(actorDocument, portraitDocument, out actorDefinitions, out loadError))
        {
            return false;
        }

        return TryLoadGraphs(
            graphDocument,
            nodeDocument,
            choiceDocument,
            out graphDefinitions,
            out loadError);
    }

    /// <summary>
    /// 화자 행과 초상화 행을 부모 화자별 Definition으로 조립합니다.
    /// </summary>
    /// <param name="actorDocument">파싱된 DialogueActors 문서입니다.</param>
    /// <param name="portraitDocument">파싱된 DialoguePortraits 문서입니다.</param>
    /// <param name="definitions">조립된 대화 화자 Definition 목록입니다.</param>
    /// <param name="loadError">조립을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 화자와 초상화를 정상적으로 조립했으면 true입니다.</returns>
    private static bool TryLoadActors(
        CsvDocument actorDocument,
        CsvDocument portraitDocument,
        out List<DialogueActorDefinition> definitions,
        out GameDataCsvLoadError loadError)
    {
        definitions = new List<DialogueActorDefinition>();
        loadError = null;
        List<DialogueActorRowData> actorRows = new List<DialogueActorRowData>();
        HashSet<string> actorIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < actorDocument.Rows.Count; i++)
        {
            CsvRow row = actorDocument.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                row,
                actorDocument.SourceName,
                "actor_id",
                out string actorId,
                out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row,
                    actorDocument.SourceName,
                    "display_name_key",
                    out string displayNameKey,
                    out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(
                    row,
                    actorDocument.SourceName,
                    "default_portrait_id",
                    out string defaultPortraitId,
                    out loadError))
            {
                return false;
            }

            if (!actorIds.Add(actorId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(actorDocument.SourceName, row, "actor_id", "중복 화자 ID가 있습니다.");
                return false;
            }

            actorRows.Add(new DialogueActorRowData(row, actorId, displayNameKey, defaultPortraitId));
        }

        Dictionary<string, List<DialoguePortraitDefinition>> portraitsByActorId =
            new Dictionary<string, List<DialoguePortraitDefinition>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> portraitIdsByActorId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        for (int i = 0; i < portraitDocument.Rows.Count; i++)
        {
            CsvRow row = portraitDocument.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, portraitDocument.SourceName, "actor_id", out string actorId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, portraitDocument.SourceName, "portrait_id", out string portraitId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalLocalizationKey(
                    row,
                    portraitDocument.SourceName,
                    "display_name_key",
                    out string displayNameKey,
                    out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, portraitDocument.SourceName, "sprite_id", out string spriteId, out loadError))
            {
                return false;
            }

            if (!actorIds.Contains(actorId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(portraitDocument.SourceName, row, "actor_id", "초상화의 부모 화자 ID를 찾을 수 없습니다.");
                return false;
            }

            if (!portraitsByActorId.TryGetValue(actorId, out List<DialoguePortraitDefinition> portraits))
            {
                portraits = new List<DialoguePortraitDefinition>();
                portraitsByActorId.Add(actorId, portraits);
                portraitIdsByActorId.Add(actorId, new HashSet<string>(StringComparer.Ordinal));
            }

            if (!portraitIdsByActorId[actorId].Add(portraitId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(portraitDocument.SourceName, row, "portrait_id", "동일 화자에 중복 초상화 ID가 있습니다.");
                return false;
            }

            portraits.Add(new DialoguePortraitDefinition(actorId, portraitId, displayNameKey, spriteId));
        }

        for (int i = 0; i < actorRows.Count; i++)
        {
            DialogueActorRowData actorRow = actorRows[i];
            portraitsByActorId.TryGetValue(actorRow.ActorId, out List<DialoguePortraitDefinition> portraits);
            portraits ??= new List<DialoguePortraitDefinition>();

            if (!string.IsNullOrEmpty(actorRow.DefaultPortraitId)
                && !ContainsPortraitId(portraits, actorRow.DefaultPortraitId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    actorDocument.SourceName,
                    actorRow.Row,
                    "default_portrait_id",
                    "기본 초상화 ID를 찾을 수 없습니다.");
                return false;
            }

            definitions.Add(new DialogueActorDefinition(
                actorRow.ActorId,
                actorRow.DisplayNameKey,
                actorRow.DefaultPortraitId,
                portraits));
        }

        return true;
    }

    /// <summary>
    /// 그래프, 노드, 선택지 행을 부모 관계에 따라 중첩 Definition으로 조립합니다.
    /// </summary>
    /// <param name="graphDocument">파싱된 DialogueGraphs 문서입니다.</param>
    /// <param name="nodeDocument">파싱된 DialogueNodes 문서입니다.</param>
    /// <param name="choiceDocument">파싱된 DialogueChoices 문서입니다.</param>
    /// <param name="definitions">조립된 대화 그래프 Definition 목록입니다.</param>
    /// <param name="loadError">조립을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 그래프, 노드, 선택지를 정상적으로 조립했으면 true입니다.</returns>
    private static bool TryLoadGraphs(
        CsvDocument graphDocument,
        CsvDocument nodeDocument,
        CsvDocument choiceDocument,
        out List<DialogueGraphDefinition> definitions,
        out GameDataCsvLoadError loadError)
    {
        definitions = new List<DialogueGraphDefinition>();
        loadError = null;

        if (!TryReadGraphRows(
            graphDocument,
            out List<DialogueGraphRowData> graphRows,
            out HashSet<string> graphIds,
            out loadError)
            || !TryReadNodeRows(
                nodeDocument,
                graphIds,
                out List<DialogueNodeRowData> nodeRows,
                out Dictionary<string, HashSet<string>> nodeIdsByGraphId,
                out loadError)
            || !TryReadChoices(
                choiceDocument,
                nodeIdsByGraphId,
                out Dictionary<string, Dictionary<string, List<DialogueChoiceDefinition>>> choicesByGraphAndNodeId,
                out loadError))
        {
            return false;
        }

        Dictionary<string, List<DialogueNodeDefinition>> nodesByGraphId =
            new Dictionary<string, List<DialogueNodeDefinition>>(StringComparer.Ordinal);

        for (int i = 0; i < nodeRows.Count; i++)
        {
            DialogueNodeRowData nodeRow = nodeRows[i];
            List<DialogueChoiceDefinition> choices = GetChoices(choicesByGraphAndNodeId, nodeRow.GraphId, nodeRow.NodeId);
            choices.Sort(CompareDialogueChoices);

            if (!nodesByGraphId.TryGetValue(nodeRow.GraphId, out List<DialogueNodeDefinition> nodes))
            {
                nodes = new List<DialogueNodeDefinition>();
                nodesByGraphId.Add(nodeRow.GraphId, nodes);
            }

            nodes.Add(new DialogueNodeDefinition(
                nodeRow.GraphId,
                nodeRow.NodeId,
                nodeRow.ActorId,
                nodeRow.PortraitId,
                nodeRow.BackgroundId,
                nodeRow.BodyTextKey,
                nodeRow.NextNodeId,
                choices));
        }

        for (int i = 0; i < graphRows.Count; i++)
        {
            DialogueGraphRowData graphRow = graphRows[i];
            nodesByGraphId.TryGetValue(graphRow.GraphId, out List<DialogueNodeDefinition> nodes);
            nodes ??= new List<DialogueNodeDefinition>();

            if (!ContainsNodeId(nodes, graphRow.StartNodeId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    graphDocument.SourceName,
                    graphRow.Row,
                    "start_node_id",
                    "같은 그래프에서 시작 노드 ID를 찾을 수 없습니다.");
                return false;
            }

            definitions.Add(new DialogueGraphDefinition(
                graphRow.GraphId,
                graphRow.StartNodeId,
                graphRow.DefaultActorId,
                graphRow.LeftActorId,
                graphRow.RightActorId,
                graphRow.BackgroundId,
                nodes));
        }

        return true;
    }

    /// <summary>
    /// DialogueGraphs 문서의 행을 그래프 조립용 데이터로 변환합니다.
    /// </summary>
    /// <param name="document">파싱된 DialogueGraphs 문서입니다.</param>
    /// <param name="rows">변환된 그래프 행 데이터 목록입니다.</param>
    /// <param name="graphIds">중복 검사와 부모 조회에 사용할 그래프 ID 집합입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 그래프 행을 정상적으로 읽었으면 true입니다.</returns>
    private static bool TryReadGraphRows(
        CsvDocument document,
        out List<DialogueGraphRowData> rows,
        out HashSet<string> graphIds,
        out GameDataCsvLoadError loadError)
    {
        rows = new List<DialogueGraphRowData>();
        graphIds = new HashSet<string>(StringComparer.Ordinal);
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "graph_id", out string graphId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "start_node_id", out string startNodeId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "default_actor_id", out string defaultActorId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "left_actor_id", out string leftActorId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "right_actor_id", out string rightActorId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "background_id", out string backgroundId, out loadError))
            {
                return false;
            }

            if (!graphIds.Add(graphId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "graph_id", "중복 대화 그래프 ID가 있습니다.");
                return false;
            }

            rows.Add(new DialogueGraphRowData(
                row,
                graphId,
                startNodeId,
                defaultActorId,
                leftActorId,
                rightActorId,
                backgroundId));
        }

        return true;
    }

    /// <summary>
    /// DialogueNodes 문서의 행을 노드 조립용 데이터로 변환합니다.
    /// </summary>
    /// <param name="document">파싱된 DialogueNodes 문서입니다.</param>
    /// <param name="graphIds">부모 그래프 존재 여부를 확인할 ID 집합입니다.</param>
    /// <param name="rows">변환된 노드 행 데이터 목록입니다.</param>
    /// <param name="nodeIdsByGraphId">그래프별 노드 ID 집합입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 노드 행을 정상적으로 읽었으면 true입니다.</returns>
    private static bool TryReadNodeRows(
        CsvDocument document,
        ISet<string> graphIds,
        out List<DialogueNodeRowData> rows,
        out Dictionary<string, HashSet<string>> nodeIdsByGraphId,
        out GameDataCsvLoadError loadError)
    {
        rows = new List<DialogueNodeRowData>();
        nodeIdsByGraphId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "graph_id", out string graphId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "node_id", out string nodeId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "actor_id", out string actorId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "portrait_id", out string portraitId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "background_id", out string backgroundId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(row, document.SourceName, "body_text_key", out string bodyTextKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "next_node_id", out string nextNodeId, out loadError))
            {
                return false;
            }

            if (!graphIds.Contains(graphId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "graph_id", "노드의 부모 그래프 ID를 찾을 수 없습니다.");
                return false;
            }

            if (!nodeIdsByGraphId.TryGetValue(graphId, out HashSet<string> nodeIds))
            {
                nodeIds = new HashSet<string>(StringComparer.Ordinal);
                nodeIdsByGraphId.Add(graphId, nodeIds);
            }

            if (!nodeIds.Add(nodeId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "node_id", "동일 그래프에 중복 노드 ID가 있습니다.");
                return false;
            }

            rows.Add(new DialogueNodeRowData(
                row,
                graphId,
                nodeId,
                actorId,
                portraitId,
                backgroundId,
                bodyTextKey,
                nextNodeId));
        }

        return true;
    }

    /// <summary>
    /// DialogueChoices 문서의 행을 부모 노드별 선택지 Definition으로 변환합니다.
    /// </summary>
    /// <param name="document">파싱된 DialogueChoices 문서입니다.</param>
    /// <param name="nodeIdsByGraphId">그래프별 부모 노드 ID 집합입니다.</param>
    /// <param name="choicesByGraphAndNodeId">그래프와 노드별 선택지 Definition 목록입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 선택지 행을 정상적으로 읽었으면 true입니다.</returns>
    private static bool TryReadChoices(
        CsvDocument document,
        IReadOnlyDictionary<string, HashSet<string>> nodeIdsByGraphId,
        out Dictionary<string, Dictionary<string, List<DialogueChoiceDefinition>>> choicesByGraphAndNodeId,
        out GameDataCsvLoadError loadError)
    {
        choicesByGraphAndNodeId = new Dictionary<string, Dictionary<string, List<DialogueChoiceDefinition>>>(StringComparer.Ordinal);
        Dictionary<string, Dictionary<string, HashSet<int>>> ordersByGraphAndNodeId =
            new Dictionary<string, Dictionary<string, HashSet<int>>>(StringComparer.Ordinal);
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "graph_id", out string graphId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "node_id", out string nodeId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(row, document.SourceName, "choice_order", out int choiceOrder, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(row, document.SourceName, "choice_text_key", out string choiceTextKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "next_node_id", out string nextNodeId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredValue(row, document.SourceName, "action_type", out string actionTypeText, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "action_id", out string actionId, out loadError))
            {
                return false;
            }

            if (!TryParseActionType(actionTypeText, out DialogueActionType actionType))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "action_type", "지원하지 않는 대화 선택지 액션 종류입니다.");
                return false;
            }

            if (!nodeIdsByGraphId.TryGetValue(graphId, out HashSet<string> nodeIds) || !nodeIds.Contains(nodeId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "node_id", "선택지의 부모 대화 노드를 찾을 수 없습니다.");
                return false;
            }

            GetOrCreateChoiceCollections(
                graphId,
                nodeId,
                choicesByGraphAndNodeId,
                ordersByGraphAndNodeId,
                out List<DialogueChoiceDefinition> choices,
                out HashSet<int> choiceOrders);

            if (!choiceOrders.Add(choiceOrder))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "choice_order", "동일 노드에 중복 선택지 순서가 있습니다.");
                return false;
            }

            choices.Add(new DialogueChoiceDefinition(
                graphId,
                nodeId,
                choiceOrder,
                choiceTextKey,
                nextNodeId,
                actionType,
                actionId));
        }

        return true;
    }

    /// <summary>
    /// 그래프와 노드별 선택지 및 순서 검사 컬렉션을 조회하거나 생성합니다.
    /// </summary>
    /// <param name="graphId">선택지가 속한 그래프 ID입니다.</param>
    /// <param name="nodeId">선택지가 속한 노드 ID입니다.</param>
    /// <param name="choicesByGraphAndNodeId">그래프와 노드별 선택지 목록입니다.</param>
    /// <param name="ordersByGraphAndNodeId">그래프와 노드별 선택지 순서 집합입니다.</param>
    /// <param name="choices">조회하거나 생성한 선택지 목록입니다.</param>
    /// <param name="choiceOrders">조회하거나 생성한 선택지 순서 집합입니다.</param>
    private static void GetOrCreateChoiceCollections(
        string graphId,
        string nodeId,
        Dictionary<string, Dictionary<string, List<DialogueChoiceDefinition>>> choicesByGraphAndNodeId,
        Dictionary<string, Dictionary<string, HashSet<int>>> ordersByGraphAndNodeId,
        out List<DialogueChoiceDefinition> choices,
        out HashSet<int> choiceOrders)
    {
        if (!choicesByGraphAndNodeId.TryGetValue(graphId, out Dictionary<string, List<DialogueChoiceDefinition>> choicesByNodeId))
        {
            choicesByNodeId = new Dictionary<string, List<DialogueChoiceDefinition>>(StringComparer.Ordinal);
            choicesByGraphAndNodeId.Add(graphId, choicesByNodeId);
            ordersByGraphAndNodeId.Add(graphId, new Dictionary<string, HashSet<int>>(StringComparer.Ordinal));
        }

        if (!choicesByNodeId.TryGetValue(nodeId, out choices))
        {
            choices = new List<DialogueChoiceDefinition>();
            choicesByNodeId.Add(nodeId, choices);
            ordersByGraphAndNodeId[graphId].Add(nodeId, new HashSet<int>());
        }

        choiceOrders = ordersByGraphAndNodeId[graphId][nodeId];
    }

    /// <summary>
    /// 선택지 액션 문자열을 명시적인 DialogueActionType으로 변환합니다.
    /// </summary>
    /// <param name="value">CSV에서 읽은 액션 문자열입니다.</param>
    /// <param name="actionType">변환된 대화 액션 종류입니다.</param>
    /// <returns>지원하는 액션 문자열이면 true입니다.</returns>
    private static bool TryParseActionType(string value, out DialogueActionType actionType)
    {
        actionType = DialogueActionType.NONE;

        switch (value)
        {
            case "NONE": actionType = DialogueActionType.NONE; return true;
            case "SET_PROGRESS_FLAG_TRUE": actionType = DialogueActionType.SET_PROGRESS_FLAG_TRUE; return true;
            case "SET_PROGRESS_FLAG_FALSE": actionType = DialogueActionType.SET_PROGRESS_FLAG_FALSE; return true;
            case "ACCEPT_QUEST": actionType = DialogueActionType.ACCEPT_QUEST; return true;
            case "REFRESH_QUEST_PROGRESS": actionType = DialogueActionType.REFRESH_QUEST_PROGRESS; return true;
            case "COMPLETE_QUEST": actionType = DialogueActionType.COMPLETE_QUEST; return true;
            case "CLAIM_QUEST_REWARD": actionType = DialogueActionType.CLAIM_QUEST_REWARD; return true;
            case "END_DIALOGUE": actionType = DialogueActionType.END_DIALOGUE; return true;
            default: return false;
        }
    }

    /// <summary>
    /// 지정한 초상화 목록에 초상화 ID가 존재하는지 확인합니다.
    /// </summary>
    /// <param name="portraits">검색할 초상화 목록입니다.</param>
    /// <param name="portraitId">찾을 초상화 ID입니다.</param>
    /// <returns>초상화 ID가 존재하면 true입니다.</returns>
    private static bool ContainsPortraitId(IReadOnlyList<DialoguePortraitDefinition> portraits, string portraitId)
    {
        for (int i = 0; i < portraits.Count; i++)
        {
            if (portraits[i].PortraitId == portraitId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 지정한 노드 목록에 노드 ID가 존재하는지 확인합니다.
    /// </summary>
    /// <param name="nodes">검색할 노드 목록입니다.</param>
    /// <param name="nodeId">찾을 노드 ID입니다.</param>
    /// <returns>노드 ID가 존재하면 true입니다.</returns>
    private static bool ContainsNodeId(IReadOnlyList<DialogueNodeDefinition> nodes, string nodeId)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].NodeId == nodeId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 그래프와 노드별 선택지 목록을 조회하며 없으면 빈 목록을 반환합니다.
    /// </summary>
    /// <param name="choicesByGraphAndNodeId">그래프와 노드별 선택지 목록입니다.</param>
    /// <param name="graphId">조회할 그래프 ID입니다.</param>
    /// <param name="nodeId">조회할 노드 ID입니다.</param>
    /// <returns>해당 노드의 선택지 목록 또는 새 빈 목록입니다.</returns>
    private static List<DialogueChoiceDefinition> GetChoices(
        IReadOnlyDictionary<string, Dictionary<string, List<DialogueChoiceDefinition>>> choicesByGraphAndNodeId,
        string graphId,
        string nodeId)
    {
        if (choicesByGraphAndNodeId.TryGetValue(graphId, out Dictionary<string, List<DialogueChoiceDefinition>> choicesByNodeId)
            && choicesByNodeId.TryGetValue(nodeId, out List<DialogueChoiceDefinition> choices))
        {
            return choices;
        }

        return new List<DialogueChoiceDefinition>();
    }

    /// <summary>
    /// 대화 선택지를 표시 순서 오름차순으로 비교합니다.
    /// </summary>
    /// <param name="left">왼쪽 선택지입니다.</param>
    /// <param name="right">오른쪽 선택지입니다.</param>
    /// <returns>왼쪽 순서가 작으면 음수, 같으면 0, 크면 양수입니다.</returns>
    private static int CompareDialogueChoices(DialogueChoiceDefinition left, DialogueChoiceDefinition right)
    {
        return left.ChoiceOrder.CompareTo(right.ChoiceOrder);
    }
}

//==============================================================================
// 기능: class::DialogueActorRowData
// 설명: 화자 Definition 조립 전 DialogueActors 행 값을 보관하는 내부 데이터
//==============================================================================
internal sealed class DialogueActorRowData
{
    // 오류 위치를 보존할 원본 CSV 행입니다.
    private readonly CsvRow _row;

    // 화자 고유 ID입니다.
    private readonly string _actorId;

    // 화자 표시 이름 문장 키입니다.
    private readonly string _displayNameKey;

    // 화자의 기본 초상화 ID입니다.
    private readonly string _defaultPortraitId;

    /// <summary>오류 위치를 보존할 원본 CSV 행입니다.</summary>
    internal CsvRow Row => _row;

    /// <summary>화자 고유 ID입니다.</summary>
    internal string ActorId => _actorId;

    /// <summary>화자 표시 이름 문장 키입니다.</summary>
    internal string DisplayNameKey => _displayNameKey;

    /// <summary>화자의 기본 초상화 ID입니다.</summary>
    internal string DefaultPortraitId => _defaultPortraitId;

    /// <summary>
    /// DialogueActors 행 데이터를 생성합니다.
    /// </summary>
    internal DialogueActorRowData(CsvRow row, string actorId, string displayNameKey, string defaultPortraitId)
    {
        _row = row;
        _actorId = actorId;
        _displayNameKey = displayNameKey;
        _defaultPortraitId = defaultPortraitId;
    }
}

//==============================================================================
// 기능: class::DialogueGraphRowData
// 설명: 그래프 Definition 조립 전 DialogueGraphs 행 값을 보관하는 내부 데이터
//==============================================================================
internal sealed class DialogueGraphRowData
{
    // 오류 위치를 보존할 원본 CSV 행입니다.
    private readonly CsvRow _row;

    // 대화 그래프 ID입니다.
    private readonly string _graphId;

    // 대화 시작 노드 ID입니다.
    private readonly string _startNodeId;

    // 그래프 기본 화자 ID입니다.
    private readonly string _defaultActorId;

    // 왼쪽 참가자 화자 ID입니다.
    private readonly string _leftActorId;

    // 오른쪽 참가자 화자 ID입니다.
    private readonly string _rightActorId;

    // 그래프 기본 배경 에셋 ID입니다.
    private readonly string _backgroundId;

    /// <summary>오류 위치를 보존할 원본 CSV 행입니다.</summary>
    internal CsvRow Row => _row;

    /// <summary>대화 그래프 ID입니다.</summary>
    internal string GraphId => _graphId;

    /// <summary>대화 시작 노드 ID입니다.</summary>
    internal string StartNodeId => _startNodeId;

    /// <summary>그래프 기본 화자 ID입니다.</summary>
    internal string DefaultActorId => _defaultActorId;

    /// <summary>왼쪽 참가자 화자 ID입니다.</summary>
    internal string LeftActorId => _leftActorId;

    /// <summary>오른쪽 참가자 화자 ID입니다.</summary>
    internal string RightActorId => _rightActorId;

    /// <summary>그래프 기본 배경 에셋 ID입니다.</summary>
    internal string BackgroundId => _backgroundId;

    /// <summary>
    /// DialogueGraphs 행 데이터를 생성합니다.
    /// </summary>
    internal DialogueGraphRowData(
        CsvRow row,
        string graphId,
        string startNodeId,
        string defaultActorId,
        string leftActorId,
        string rightActorId,
        string backgroundId)
    {
        _row = row;
        _graphId = graphId;
        _startNodeId = startNodeId;
        _defaultActorId = defaultActorId;
        _leftActorId = leftActorId;
        _rightActorId = rightActorId;
        _backgroundId = backgroundId;
    }
}

//==============================================================================
// 기능: class::DialogueNodeRowData
// 설명: 노드 Definition 조립 전 DialogueNodes 행 값을 보관하는 내부 데이터
//==============================================================================
internal sealed class DialogueNodeRowData
{
    // 오류 위치를 보존할 원본 CSV 행입니다.
    private readonly CsvRow _row;

    // 부모 대화 그래프 ID입니다.
    private readonly string _graphId;

    // 대화 노드 ID입니다.
    private readonly string _nodeId;

    // 노드 화자 ID입니다.
    private readonly string _actorId;

    // 노드 초상화 ID입니다.
    private readonly string _portraitId;

    // 노드 배경 에셋 ID입니다.
    private readonly string _backgroundId;

    // 대화 본문 문장 키입니다.
    private readonly string _bodyTextKey;

    // 다음 대화 노드 ID입니다.
    private readonly string _nextNodeId;

    /// <summary>오류 위치를 보존할 원본 CSV 행입니다.</summary>
    internal CsvRow Row => _row;

    /// <summary>부모 대화 그래프 ID입니다.</summary>
    internal string GraphId => _graphId;

    /// <summary>대화 노드 ID입니다.</summary>
    internal string NodeId => _nodeId;

    /// <summary>노드 화자 ID입니다.</summary>
    internal string ActorId => _actorId;

    /// <summary>노드 초상화 ID입니다.</summary>
    internal string PortraitId => _portraitId;

    /// <summary>노드 배경 에셋 ID입니다.</summary>
    internal string BackgroundId => _backgroundId;

    /// <summary>대화 본문 문장 키입니다.</summary>
    internal string BodyTextKey => _bodyTextKey;

    /// <summary>다음 대화 노드 ID입니다.</summary>
    internal string NextNodeId => _nextNodeId;

    /// <summary>
    /// DialogueNodes 행 데이터를 생성합니다.
    /// </summary>
    internal DialogueNodeRowData(
        CsvRow row,
        string graphId,
        string nodeId,
        string actorId,
        string portraitId,
        string backgroundId,
        string bodyTextKey,
        string nextNodeId)
    {
        _row = row;
        _graphId = graphId;
        _nodeId = nodeId;
        _actorId = actorId;
        _portraitId = portraitId;
        _backgroundId = backgroundId;
        _bodyTextKey = bodyTextKey;
        _nextNodeId = nextNodeId;
    }
}
