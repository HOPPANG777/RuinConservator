using System;
using System.Collections.Generic;

//==============================================================================
// 기능: class::QuestCsvDefinitionLoader
// 설명: 런타임과 에디터에서 네 개 Quest CSV를 목표와 보상이 포함된 Quest Definition 목록으로 조립하는 로더
//==============================================================================
public static class QuestCsvDefinitionLoader
{
    // Quests CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] QUEST_HEADERS = { "quest_id", "title_text_key", "description_text_key", "owner_npc_id" };

    // QuestObjectives CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] OBJECTIVE_HEADERS =
    {
        "quest_id",
        "objective_order",
        "objective_id",
        "description_text_key",
        "required_progress_flag_id",
        "required_flag_value"
    };

    // QuestRewards CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] REWARD_HEADERS =
    {
        "quest_id",
        "reward_order",
        "reward_id",
        "description_text_key",
        "progress_flag_id",
        "flag_value",
        "item_id",
        "item_amount"
    };

    // QuestConsumeItems CSV에서 허용하는 정확한 헤더 목록입니다.
    private static readonly string[] CONSUME_ITEM_HEADERS = { "quest_id", "consume_order", "item_id", "item_amount" };

    /// <summary>
    /// 네 개 Quest CSV를 자식 데이터가 조립된 Quest Definition 목록으로 변환합니다.
    /// </summary>
    /// <param name="source">Quest CSV 원본이 포함된 게임 데이터 입력 묶음입니다.</param>
    /// <param name="definitions">변환된 Quest Definition 목록입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 Quest CSV를 정상적으로 조립했으면 true입니다.</returns>
    public static bool TryLoad(
        GameDataCsvSource source,
        out List<QuestDefinition> definitions,
        out GameDataCsvLoadError loadError)
    {
        definitions = new List<QuestDefinition>();
        loadError = null;

        if (source == null)
        {
            loadError = new GameDataCsvLoadError("(missing)", 0, 0, string.Empty, "게임 데이터 CSV 원본 묶음이 없습니다.");
            return false;
        }

        if (!CsvDefinitionReadUtility.TryParseRequiredDocument(source.Quests, QUEST_HEADERS, out CsvDocument questDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(source.QuestObjectives, OBJECTIVE_HEADERS, out CsvDocument objectiveDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(source.QuestRewards, REWARD_HEADERS, out CsvDocument rewardDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(
                source.QuestConsumeItems,
                CONSUME_ITEM_HEADERS,
                out CsvDocument consumeItemDocument,
                out loadError))
        {
            return false;
        }

        if (!TryReadQuestRows(
            questDocument,
            out List<QuestRowData> questRows,
            out HashSet<string> questIds,
            out loadError)
            || !TryReadObjectives(
                objectiveDocument,
                questIds,
                out Dictionary<string, List<QuestObjectiveDefinition>> objectivesByQuestId,
                out loadError)
            || !TryReadRewards(
                rewardDocument,
                questIds,
                out Dictionary<string, List<QuestRewardDefinition>> rewardsByQuestId,
                out loadError)
            || !TryReadConsumeItems(
                consumeItemDocument,
                questIds,
                out Dictionary<string, List<QuestConsumeItemDefinition>> consumeItemsByQuestId,
                out loadError))
        {
            return false;
        }

        for (int i = 0; i < questRows.Count; i++)
        {
            QuestRowData questRow = questRows[i];
            List<QuestObjectiveDefinition> objectives = GetDefinitions(objectivesByQuestId, questRow.QuestId);
            List<QuestRewardDefinition> rewards = GetDefinitions(rewardsByQuestId, questRow.QuestId);
            List<QuestConsumeItemDefinition> consumeItems = GetDefinitions(consumeItemsByQuestId, questRow.QuestId);
            objectives.Sort(CompareObjectives);
            rewards.Sort(CompareRewards);
            consumeItems.Sort(CompareConsumeItems);

            definitions.Add(new QuestDefinition(
                questRow.QuestId,
                questRow.TitleTextKey,
                questRow.DescriptionTextKey,
                questRow.OwnerNpcId,
                objectives,
                rewards,
                consumeItems));
        }

        return true;
    }

    /// <summary>
    /// Quests 문서의 부모 퀘스트 행을 조립용 데이터로 변환합니다.
    /// </summary>
    /// <param name="document">파싱된 Quests 문서입니다.</param>
    /// <param name="rows">변환된 퀘스트 행 데이터 목록입니다.</param>
    /// <param name="questIds">중복 검사와 부모 조회에 사용할 퀘스트 ID 집합입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 퀘스트 행을 정상적으로 읽었으면 true입니다.</returns>
    private static bool TryReadQuestRows(
        CsvDocument document,
        out List<QuestRowData> rows,
        out HashSet<string> questIds,
        out GameDataCsvLoadError loadError)
    {
        rows = new List<QuestRowData>();
        questIds = new HashSet<string>(StringComparer.Ordinal);
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "quest_id", out string questId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(row, document.SourceName, "title_text_key", out string titleTextKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row,
                    document.SourceName,
                    "description_text_key",
                    out string descriptionTextKey,
                    out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "owner_npc_id", out string ownerNpcId, out loadError))
            {
                return false;
            }

            if (!questIds.Add(questId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "quest_id", "중복 퀘스트 ID가 있습니다.");
                return false;
            }

            rows.Add(new QuestRowData(row, questId, titleTextKey, descriptionTextKey, ownerNpcId));
        }

        return true;
    }

    /// <summary>
    /// QuestObjectives 문서의 행을 부모 퀘스트별 목표 Definition으로 변환합니다.
    /// </summary>
    /// <param name="document">파싱된 QuestObjectives 문서입니다.</param>
    /// <param name="questIds">부모 퀘스트 존재 여부를 확인할 ID 집합입니다.</param>
    /// <param name="definitionsByQuestId">퀘스트별 목표 Definition 목록입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 목표 행을 정상적으로 읽었으면 true입니다.</returns>
    private static bool TryReadObjectives(
        CsvDocument document,
        ISet<string> questIds,
        out Dictionary<string, List<QuestObjectiveDefinition>> definitionsByQuestId,
        out GameDataCsvLoadError loadError)
    {
        definitionsByQuestId = new Dictionary<string, List<QuestObjectiveDefinition>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> idsByQuestId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<int>> ordersByQuestId = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "quest_id", out string questId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(row, document.SourceName, "objective_order", out int objectiveOrder, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "objective_id", out string objectiveId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row,
                    document.SourceName,
                    "description_text_key",
                    out string descriptionTextKey,
                    out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row,
                    document.SourceName,
                    "required_progress_flag_id",
                    out string requiredProgressFlagId,
                    out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredBool(row, document.SourceName, "required_flag_value", out bool requiredFlagValue, out loadError))
            {
                return false;
            }

            if (!TryValidateParentQuest(document, row, questIds, questId, out loadError))
            {
                return false;
            }

            GetOrCreateChildCollections(
                questId,
                definitionsByQuestId,
                idsByQuestId,
                ordersByQuestId,
                out List<QuestObjectiveDefinition> definitions,
                out HashSet<string> ids,
                out HashSet<int> orders);

            if (!ids.Add(objectiveId) || !orders.Add(objectiveOrder))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "objective_id", "동일 퀘스트에 목표 ID 또는 순서가 중복됩니다.");
                return false;
            }

            definitions.Add(new QuestObjectiveDefinition(
                questId,
                objectiveOrder,
                objectiveId,
                descriptionTextKey,
                requiredProgressFlagId,
                requiredFlagValue));
        }

        return true;
    }

    /// <summary>
    /// QuestRewards 문서의 행을 부모 퀘스트별 보상 Definition으로 변환합니다.
    /// </summary>
    /// <param name="document">파싱된 QuestRewards 문서입니다.</param>
    /// <param name="questIds">부모 퀘스트 존재 여부를 확인할 ID 집합입니다.</param>
    /// <param name="definitionsByQuestId">퀘스트별 보상 Definition 목록입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 보상 행을 정상적으로 읽었으면 true입니다.</returns>
    private static bool TryReadRewards(
        CsvDocument document,
        ISet<string> questIds,
        out Dictionary<string, List<QuestRewardDefinition>> definitionsByQuestId,
        out GameDataCsvLoadError loadError)
    {
        definitionsByQuestId = new Dictionary<string, List<QuestRewardDefinition>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> idsByQuestId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<int>> ordersByQuestId = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "quest_id", out string questId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(row, document.SourceName, "reward_order", out int rewardOrder, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "reward_id", out string rewardId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row,
                    document.SourceName,
                    "description_text_key",
                    out string descriptionTextKey,
                    out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "progress_flag_id", out string progressFlagId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalBool(row, document.SourceName, "flag_value", out bool? flagValue, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "item_id", out string itemId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalPositiveInt(row, document.SourceName, "item_amount", out int? itemAmount, out loadError))
            {
                return false;
            }

            if (!TryValidateParentQuest(document, row, questIds, questId, out loadError)
                || !TryValidateRewardFields(document, row, progressFlagId, flagValue, itemId, itemAmount, out loadError))
            {
                return false;
            }

            GetOrCreateChildCollections(
                questId,
                definitionsByQuestId,
                idsByQuestId,
                ordersByQuestId,
                out List<QuestRewardDefinition> definitions,
                out HashSet<string> ids,
                out HashSet<int> orders);

            if (!ids.Add(rewardId) || !orders.Add(rewardOrder))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "reward_id", "동일 퀘스트에 보상 ID 또는 순서가 중복됩니다.");
                return false;
            }

            definitions.Add(new QuestRewardDefinition(
                questId,
                rewardOrder,
                rewardId,
                descriptionTextKey,
                progressFlagId,
                flagValue,
                itemId,
                itemAmount));
        }

        return true;
    }

    /// <summary>
    /// QuestConsumeItems 문서의 행을 부모 퀘스트별 소모 아이템 Definition으로 변환합니다.
    /// </summary>
    /// <param name="document">파싱된 QuestConsumeItems 문서입니다.</param>
    /// <param name="questIds">부모 퀘스트 존재 여부를 확인할 ID 집합입니다.</param>
    /// <param name="definitionsByQuestId">퀘스트별 소모 아이템 Definition 목록입니다.</param>
    /// <param name="loadError">변환을 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>모든 소모 아이템 행을 정상적으로 읽었으면 true입니다.</returns>
    private static bool TryReadConsumeItems(
        CsvDocument document,
        ISet<string> questIds,
        out Dictionary<string, List<QuestConsumeItemDefinition>> definitionsByQuestId,
        out GameDataCsvLoadError loadError)
    {
        definitionsByQuestId = new Dictionary<string, List<QuestConsumeItemDefinition>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> itemIdsByQuestId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<int>> ordersByQuestId = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "quest_id", out string questId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(row, document.SourceName, "consume_order", out int consumeOrder, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "item_id", out string itemId, out loadError)
                || !CsvDefinitionReadUtility.TryGetPositiveInt(row, document.SourceName, "item_amount", out int itemAmount, out loadError))
            {
                return false;
            }

            if (!TryValidateParentQuest(document, row, questIds, questId, out loadError))
            {
                return false;
            }

            GetOrCreateChildCollections(
                questId,
                definitionsByQuestId,
                itemIdsByQuestId,
                ordersByQuestId,
                out List<QuestConsumeItemDefinition> definitions,
                out HashSet<string> itemIds,
                out HashSet<int> orders);

            if (!itemIds.Add(itemId) || !orders.Add(consumeOrder))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "item_id", "동일 퀘스트에 소모 아이템 또는 순서가 중복됩니다.");
                return false;
            }

            definitions.Add(new QuestConsumeItemDefinition(questId, consumeOrder, itemId, itemAmount));
        }

        return true;
    }

    /// <summary>
    /// 자식 데이터가 참조하는 부모 퀘스트 ID가 존재하는지 확인합니다.
    /// </summary>
    private static bool TryValidateParentQuest(
        CsvDocument document,
        CsvRow row,
        ISet<string> questIds,
        string questId,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (questIds.Contains(questId))
        {
            return true;
        }

        loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "quest_id", "부모 퀘스트 ID를 찾을 수 없습니다.");
        return false;
    }

    /// <summary>
    /// 퀘스트 보상의 Flag와 아이템 조건부 필드 조합을 검증합니다.
    /// </summary>
    private static bool TryValidateRewardFields(
        CsvDocument document,
        CsvRow row,
        string progressFlagId,
        bool? flagValue,
        string itemId,
        int? itemAmount,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;
        bool hasFlag = !string.IsNullOrEmpty(progressFlagId);
        bool hasItem = !string.IsNullOrEmpty(itemId);

        if (!hasFlag && !hasItem)
        {
            loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "progress_flag_id", "Flag 보상 또는 아이템 보상 중 하나는 필수입니다.");
            return false;
        }

        if (hasFlag != flagValue.HasValue)
        {
            loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "flag_value", "progress_flag_id와 flag_value는 함께 입력해야 합니다.");
            return false;
        }

        if (hasItem != itemAmount.HasValue)
        {
            loadError = CsvDefinitionReadUtility.CreateRowError(document.SourceName, row, "item_amount", "item_id와 item_amount는 함께 입력해야 합니다.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 퀘스트별 자식 Definition 목록과 ID 및 순서 집합을 조회하거나 생성합니다.
    /// </summary>
    private static void GetOrCreateChildCollections<TDefinition>(
        string questId,
        Dictionary<string, List<TDefinition>> definitionsByQuestId,
        Dictionary<string, HashSet<string>> idsByQuestId,
        Dictionary<string, HashSet<int>> ordersByQuestId,
        out List<TDefinition> definitions,
        out HashSet<string> ids,
        out HashSet<int> orders)
    {
        if (!definitionsByQuestId.TryGetValue(questId, out definitions))
        {
            definitions = new List<TDefinition>();
            definitionsByQuestId.Add(questId, definitions);
            idsByQuestId.Add(questId, new HashSet<string>(StringComparer.Ordinal));
            ordersByQuestId.Add(questId, new HashSet<int>());
        }

        ids = idsByQuestId[questId];
        orders = ordersByQuestId[questId];
    }

    /// <summary>
    /// 퀘스트 ID에 연결된 Definition 목록을 조회하며 없으면 빈 목록을 반환합니다.
    /// </summary>
    private static List<TDefinition> GetDefinitions<TDefinition>(
        IReadOnlyDictionary<string, List<TDefinition>> definitionsByQuestId,
        string questId)
    {
        if (definitionsByQuestId.TryGetValue(questId, out List<TDefinition> definitions))
        {
            return definitions;
        }

        return new List<TDefinition>();
    }

    /// <summary>퀘스트 목표를 목표 순서 오름차순으로 비교합니다.</summary>
    private static int CompareObjectives(QuestObjectiveDefinition left, QuestObjectiveDefinition right)
    {
        return left.ObjectiveOrder.CompareTo(right.ObjectiveOrder);
    }

    /// <summary>퀘스트 보상을 보상 순서 오름차순으로 비교합니다.</summary>
    private static int CompareRewards(QuestRewardDefinition left, QuestRewardDefinition right)
    {
        return left.RewardOrder.CompareTo(right.RewardOrder);
    }

    /// <summary>퀘스트 소모 아이템을 소모 순서 오름차순으로 비교합니다.</summary>
    private static int CompareConsumeItems(QuestConsumeItemDefinition left, QuestConsumeItemDefinition right)
    {
        return left.ConsumeOrder.CompareTo(right.ConsumeOrder);
    }
}

//==============================================================================
// 기능: class::QuestRowData
// 설명: Quest Definition 조립 전 Quests 행 값을 보관하는 내부 데이터
//==============================================================================
internal sealed class QuestRowData
{
    // 오류 위치를 보존할 원본 CSV 행입니다.
    private readonly CsvRow _row;

    // 퀘스트 고유 ID입니다.
    private readonly string _questId;

    // 퀘스트 제목 문장 키입니다.
    private readonly string _titleTextKey;

    // 퀘스트 설명 문장 키입니다.
    private readonly string _descriptionTextKey;

    // 퀘스트 소유 NPC ID입니다.
    private readonly string _ownerNpcId;

    /// <summary>오류 위치를 보존할 원본 CSV 행입니다.</summary>
    internal CsvRow Row => _row;

    /// <summary>퀘스트 고유 ID입니다.</summary>
    internal string QuestId => _questId;

    /// <summary>퀘스트 제목 문장 키입니다.</summary>
    internal string TitleTextKey => _titleTextKey;

    /// <summary>퀘스트 설명 문장 키입니다.</summary>
    internal string DescriptionTextKey => _descriptionTextKey;

    /// <summary>퀘스트 소유 NPC ID입니다.</summary>
    internal string OwnerNpcId => _ownerNpcId;

    /// <summary>
    /// Quests 행 데이터를 생성합니다.
    /// </summary>
    internal QuestRowData(
        CsvRow row,
        string questId,
        string titleTextKey,
        string descriptionTextKey,
        string ownerNpcId)
    {
        _row = row;
        _questId = questId;
        _titleTextKey = titleTextKey;
        _descriptionTextKey = descriptionTextKey;
        _ownerNpcId = ownerNpcId;
    }
}
