using NUnit.Framework;
using UnityEditor;
using UnityEngine;

//==============================================================================
// 기능: class::GameDataCsvRegressionTests
// 설명: 프로젝트의 실제 런타임 CSV와 필수 오류 처리를 검증하는 에디터 회귀 테스트
//==============================================================================
[TestFixture]
public sealed class GameDataCsvRegressionTests
{
    // 실제 런타임 CSV TextAsset이 저장된 프로젝트 디렉터리입니다.
    private const string CSV_DIRECTORY_PATH = "Assets/_Game/Data/Csv/";

    // 필수 CSV 누락 오류에서 사용하는 원본 이름입니다.
    private const string MISSING_SOURCE_NAME = "(missing)";

    // 잘못된 헤더 테스트에서 사용하는 정상 화자 ID 헤더입니다.
    private const string VALID_ACTOR_ID_HEADER = "actor_id";

    // 잘못된 헤더 테스트에서 정상 헤더를 대체하는 잘못된 이름입니다.
    private const string INVALID_ACTOR_ID_HEADER = "actor_code";

    // 제거된 레거시 Dialogue와 Quest ScriptableObject 타입 이름 목록입니다.
    private static readonly string[] LEGACY_TYPE_NAMES =
    {
        "DialogueActorSO",
        "DialogueGraphSO",
        "QuestDataSO"
    };

    // 제거된 레거시 Dialogue와 Quest 테스트 에셋 경로 목록입니다.
    private static readonly string[] LEGACY_ASSET_PATHS =
    {
        "Assets/_Game/Data/DialogueData/TestNPC/PlayerDialogueActor.asset",
        "Assets/_Game/Data/DialogueData/TestNPC/TestDialogueActor.asset",
        "Assets/_Game/Data/DialogueData/TestNPC/TestDialogueGraph.asset",
        "Assets/_Game/Data/QuestData/QuestData.asset"
    };

    //==============================================================================
    // 회귀 테스트
    //==============================================================================

    /// <summary>
    /// GameRoot가 사용하는 실제 CSV 12개가 유효한 게임 데이터 저장소로 변환되는지 검증합니다.
    /// </summary>
    [Test]
    public void RuntimeCsvFilesCreateRepository()
    {
        GameDataCsvSource source = CreateRuntimeSource();
        bool didLoad = GameDataCsvLoader.TryLoad(
            source,
            out GameDataRepository repository,
            out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.True, loadError?.ToString());
        Assert.That(repository, Is.Not.Null);
        Assert.That(repository.DialogueActorCount, Is.GreaterThan(0));
        Assert.That(repository.DialogueGraphCount, Is.GreaterThan(0));
        Assert.That(repository.QuestCount, Is.GreaterThan(0));
        Assert.That(repository.LocalizationEntryCount, Is.GreaterThan(0));
    }

    /// <summary>
    /// 필수 Dialogue Actors CSV가 없으면 저장소 생성을 거부하고 누락 오류를 반환하는지 검증합니다.
    /// </summary>
    [Test]
    public void MissingRequiredCsvReturnsMissingSourceError()
    {
        GameDataCsvSource runtimeSource = CreateRuntimeSource();
        GameDataCsvSource missingSource = CopySourceWithDialogueActors(runtimeSource, null);
        bool didLoad = GameDataCsvLoader.TryLoad(
            missingSource,
            out _,
            out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError, Is.Not.Null);
        Assert.That(loadError.SourceName, Is.EqualTo(MISSING_SOURCE_NAME));
    }

    /// <summary>
    /// 확정 스키마와 다른 Dialogue Actors 헤더를 첫 번째 레코드에서 거부하는지 검증합니다.
    /// </summary>
    [Test]
    public void InvalidDialogueActorHeaderReturnsHeaderError()
    {
        GameDataCsvSource runtimeSource = CreateRuntimeSource();
        string invalidText = runtimeSource.DialogueActors.Text.Replace(
            VALID_ACTOR_ID_HEADER,
            INVALID_ACTOR_ID_HEADER);
        GameDataCsvFile invalidActors = new GameDataCsvFile(
            runtimeSource.DialogueActors.SourceName,
            invalidText);
        GameDataCsvSource invalidSource = CopySourceWithDialogueActors(
            runtimeSource,
            invalidActors);
        bool didLoad = GameDataCsvLoader.TryLoad(
            invalidSource,
            out _,
            out GameDataCsvLoadError loadError);

        Assert.That(invalidText, Is.Not.EqualTo(runtimeSource.DialogueActors.Text));
        Assert.That(didLoad, Is.False);
        Assert.That(loadError, Is.Not.Null);
        Assert.That(loadError.SourceName, Is.EqualTo(runtimeSource.DialogueActors.SourceName));
        Assert.That(loadError.RecordNumber, Is.EqualTo(1));
    }

    /// <summary>
    /// CSV 전환이 끝난 Dialogue와 Quest 레거시 ScriptableObject 타입이 런타임 어셈블리에 없는지 검증합니다.
    /// </summary>
    [Test]
    public void LegacyDialogueAndQuestTypesAreRemoved()
    {
        System.Reflection.Assembly runtimeAssembly = typeof(GameDataController).Assembly;

        for (int i = 0; i < LEGACY_TYPE_NAMES.Length; i++)
        {
            System.Type legacyType = runtimeAssembly.GetType(LEGACY_TYPE_NAMES[i]);

            Assert.That(
                legacyType,
                Is.Null,
                $"제거된 레거시 타입이 다시 추가되었습니다. type={LEGACY_TYPE_NAMES[i]}");
        }
    }

    /// <summary>
    /// 제거된 Dialogue와 Quest 레거시 테스트 에셋이 프로젝트에 다시 포함되지 않았는지 검증합니다.
    /// </summary>
    [Test]
    public void LegacyDialogueAndQuestAssetsAreRemoved()
    {
        for (int i = 0; i < LEGACY_ASSET_PATHS.Length; i++)
        {
            Object legacyAsset = AssetDatabase.LoadAssetAtPath<Object>(LEGACY_ASSET_PATHS[i]);

            Assert.That(
                legacyAsset,
                Is.Null,
                $"제거된 레거시 에셋이 다시 추가되었습니다. path={LEGACY_ASSET_PATHS[i]}");
        }
    }

    //==============================================================================
    // 테스트 데이터 구성
    //==============================================================================

    /// <summary>
    /// 프로젝트의 실제 런타임 CSV TextAsset 12개를 전체 게임 데이터 원본 묶음으로 생성합니다.
    /// </summary>
    /// <returns>실제 런타임 CSV 원본 묶음입니다.</returns>
    private static GameDataCsvSource CreateRuntimeSource()
    {
        return new GameDataCsvSource(
            LoadRequiredCsv("dialogue_actors.csv"),
            LoadRequiredCsv("dialogue_portraits.csv"),
            LoadRequiredCsv("dialogue_graphs.csv"),
            LoadRequiredCsv("dialogue_nodes.csv"),
            LoadRequiredCsv("dialogue_choices.csv"),
            LoadRequiredCsv("quests.csv"),
            LoadRequiredCsv("quest_objectives.csv"),
            LoadRequiredCsv("quest_rewards.csv"),
            LoadRequiredCsv("quest_consume_items.csv"),
            LoadRequiredCsv("localization.csv"),
            LoadRequiredCsv("npc_dialogue_bindings.csv"),
            LoadRequiredCsv("npc_quest_dialogue_routes.csv"));
    }

    /// <summary>
    /// 런타임 CSV 원본 묶음을 복사하면서 Dialogue Actors 원본만 교체합니다.
    /// </summary>
    /// <param name="source">복사할 런타임 CSV 원본 묶음입니다.</param>
    /// <param name="dialogueActors">교체할 Dialogue Actors 원본입니다.</param>
    /// <returns>Dialogue Actors 원본이 교체된 CSV 원본 묶음입니다.</returns>
    private static GameDataCsvSource CopySourceWithDialogueActors(
        GameDataCsvSource source,
        GameDataCsvFile dialogueActors)
    {
        return new GameDataCsvSource(
            dialogueActors,
            source.DialoguePortraits,
            source.DialogueGraphs,
            source.DialogueNodes,
            source.DialogueChoices,
            source.Quests,
            source.QuestObjectives,
            source.QuestRewards,
            source.QuestConsumeItems,
            source.Localization,
            source.NpcDialogueBindings,
            source.NpcQuestDialogueRoutes);
    }

    /// <summary>
    /// 프로젝트 경로에서 필수 CSV TextAsset을 읽어 순수 C# CSV 원본으로 변환합니다.
    /// </summary>
    /// <param name="fileName">읽을 CSV 파일 이름입니다.</param>
    /// <returns>파일 이름과 전체 문자열이 포함된 CSV 원본입니다.</returns>
    private static GameDataCsvFile LoadRequiredCsv(string fileName)
    {
        string assetPath = $"{CSV_DIRECTORY_PATH}{fileName}";
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

        Assert.That(textAsset, Is.Not.Null, $"필수 CSV TextAsset을 찾을 수 없습니다. path={assetPath}");
        return new GameDataCsvFile(textAsset.name, textAsset.text);
    }
}
