using System.Collections.Generic;

//==============================================================================
// 기능: class::GameDataCsvLoader
// 설명: 전체 게임 CSV를 Definition으로 변환하고 검증된 GameDataRepository를 생성하는 진입점
//==============================================================================
public static class GameDataCsvLoader
{
    /// <summary>
    /// 전체 게임 CSV를 변환하고 모든 참조가 검증된 게임 데이터 저장소를 생성합니다.
    /// </summary>
    /// <param name="source">필수 및 선택 CSV 원본 묶음입니다.</param>
    /// <param name="repository">생성된 게임 데이터 저장소입니다.</param>
    /// <param name="loadError">로드를 중단시킨 첫 번째 오류입니다.</param>
    /// <returns>전체 CSV 변환과 저장소 생성에 성공하면 true입니다.</returns>
    public static bool TryLoad(
        GameDataCsvSource source,
        out GameDataRepository repository,
        out GameDataCsvLoadError loadError)
    {
        repository = null;
        loadError = null;

        if (source == null)
        {
            loadError = new GameDataCsvLoadError("(missing)", 0, 0, string.Empty, "게임 데이터 CSV 원본 묶음이 없습니다.");
            return false;
        }

        if (!LocalizationCsvDefinitionLoader.TryLoad(
            source.Localization,
            out List<LocalizationEntryDefinition> localizationEntries,
            out loadError)
            || !QuestCsvDefinitionLoader.TryLoad(
                source,
                out List<QuestDefinition> quests,
                out loadError)
            || !DialogueCsvDefinitionLoader.TryLoad(
                source,
                out List<DialogueActorDefinition> dialogueActors,
                out List<DialogueGraphDefinition> dialogueGraphs,
                out loadError)
            || !NpcDialogueCsvDefinitionLoader.TryLoad(
                source,
                out List<NpcDialogueBindingDefinition> npcDialogueBindings,
                out List<NpcQuestDialogueRouteDefinition> npcQuestDialogueRoutes,
                out loadError))
        {
            return false;
        }

        if (!GameDataRepository.TryCreate(
            dialogueActors,
            dialogueGraphs,
            quests,
            localizationEntries,
            npcDialogueBindings,
            npcQuestDialogueRoutes,
            out repository,
            out GameDataRepositoryBuildError buildError))
        {
            loadError = new GameDataCsvLoadError(
                "(repository)",
                0,
                0,
                buildError.DataType,
                $"{buildError.Message} id={buildError.DataId}");
            return false;
        }

        return true;
    }
}
