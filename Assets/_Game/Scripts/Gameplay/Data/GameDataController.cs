using UnityEngine;

//==============================================================================
// 기능: class::GameDataController
// 설명: Unity TextAsset CSV를 검증된 게임 데이터 저장소로 초기화하는 컨트롤러
//==============================================================================
[DisallowMultipleComponent]
public sealed class GameDataController : MonoBehaviour, ILocalizationService
{
    //==============================================================================
    // 직렬화 필드 - Dialogue
    //==============================================================================

    [Header("Dialogue CSV")]
    [Tooltip("대화 화자 데이터를 정의하는 dialogue_actors.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _dialogueActorsCsv;

    [Tooltip("대화 초상화 데이터를 정의하는 dialogue_portraits.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _dialoguePortraitsCsv;

    [Tooltip("대화 그래프 데이터를 정의하는 dialogue_graphs.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _dialogueGraphsCsv;

    [Tooltip("대화 노드 데이터를 정의하는 dialogue_nodes.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _dialogueNodesCsv;

    [Tooltip("대화 선택지 데이터를 정의하는 dialogue_choices.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _dialogueChoicesCsv;

    //==============================================================================
    // 직렬화 필드 - Quest
    //==============================================================================

    [Header("Quest CSV")]
    [Tooltip("퀘스트 기본 데이터를 정의하는 quests.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _questsCsv;

    [Tooltip("퀘스트 목표 데이터를 정의하는 quest_objectives.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _questObjectivesCsv;

    [Tooltip("퀘스트 보상 데이터를 정의하는 quest_rewards.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _questRewardsCsv;

    [Tooltip("퀘스트 완료 시 소모할 아이템을 정의하는 quest_consume_items.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _questConsumeItemsCsv;

    //==============================================================================
    // 직렬화 필드 - Localization 및 NPC
    //==============================================================================

    [Header("Localization CSV")]
    [Tooltip("화면 표시 문장을 정의하는 localization.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _localizationCsv;

    [Tooltip("Localization CSV에서 화면에 표시할 언어입니다.")]
    [SerializeField]
    private GameLanguage _language = GameLanguage.KOREAN;

    [Header("Optional NPC CSV")]
    [Tooltip("NPC 기본 대화 연결을 정의하는 선택적 npc_dialogue_bindings.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _npcDialogueBindingsCsv;

    [Tooltip("NPC 퀘스트 상태별 대화 경로를 정의하는 선택적 npc_quest_dialogue_routes.csv TextAsset입니다.")]
    [SerializeField]
    private TextAsset _npcQuestDialogueRoutesCsv;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 초기화가 끝난 읽기 전용 게임 데이터 저장소입니다.
    private GameDataRepository _repository;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>게임 데이터 저장소 초기화가 완료됐는지 여부입니다.</summary>
    public bool IsLoaded => _repository != null;

    /// <summary>초기화가 끝난 읽기 전용 게임 데이터 저장소 계약입니다.</summary>
    public IGameDataRepository Repository => _repository;

    /// <summary>현재 화면 표시 언어입니다.</summary>
    public GameLanguage Language => _language;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// 인스펙터에 연결된 CSV TextAsset을 검증하고 게임 데이터 저장소를 초기화합니다.
    /// </summary>
    /// <param name="loadError">초기화를 중단시킨 첫 번째 CSV 오류입니다.</param>
    /// <returns>전체 CSV를 읽고 저장소를 생성했으면 true입니다.</returns>
    public bool TryInit(out GameDataCsvLoadError loadError)
    {
        _repository = null;
        loadError = null;

        if (!TryCreateCsvSource(out GameDataCsvSource source, out loadError))
        {
            return false;
        }

        if (!GameDataCsvLoader.TryLoad(source, out GameDataRepository repository, out loadError))
        {
            return false;
        }

        _repository = repository;
        return true;
    }

    //==============================================================================
    // 공개 API - Localization
    //==============================================================================

    /// <summary>
    /// Localization Key를 현재 언어의 화면 표시 문자열로 변환합니다.
    /// </summary>
    /// <param name="textKey">변환할 Localization Key입니다.</param>
    /// <returns>현재 언어 문자열이며 찾지 못하면 입력 Key입니다.</returns>
    public string GetText(string textKey)
    {
        if (string.IsNullOrWhiteSpace(textKey)
            || _repository == null
            || !_repository.TryGetLocalizationEntry(textKey, out LocalizationEntryDefinition entry))
        {
            return textKey ?? string.Empty;
        }

        string localizedText = GetPreferredText(entry);
        return string.IsNullOrEmpty(localizedText)
            ? textKey
            : localizedText.Replace("\\n", "\n");
    }

    /// <summary>
    /// 화면 표시 언어를 변경합니다.
    /// </summary>
    /// <param name="language">변경할 언어입니다.</param>
    /// <returns>지원하는 언어로 변경했으면 true입니다.</returns>
    public bool TrySetLanguage(GameLanguage language)
    {
        if (language != GameLanguage.KOREAN
            && language != GameLanguage.ENGLISH
            && language != GameLanguage.JAPANESE)
        {
            return false;
        }

        _language = language;
        return true;
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 필수 TextAsset 연결을 검사하고 순수 C# CSV 원본 묶음을 생성합니다.
    /// </summary>
    /// <param name="source">생성된 순수 C# CSV 원본 묶음입니다.</param>
    /// <param name="loadError">필수 TextAsset 검증을 중단시킨 오류입니다.</param>
    /// <returns>모든 필수 TextAsset이 연결됐으면 true입니다.</returns>
    private bool TryCreateCsvSource(
        out GameDataCsvSource source,
        out GameDataCsvLoadError loadError)
    {
        source = null;
        loadError = null;

        if (!TryValidateRequiredAsset(_dialogueActorsCsv, "dialogue_actors.csv", out loadError)
            || !TryValidateRequiredAsset(_dialoguePortraitsCsv, "dialogue_portraits.csv", out loadError)
            || !TryValidateRequiredAsset(_dialogueGraphsCsv, "dialogue_graphs.csv", out loadError)
            || !TryValidateRequiredAsset(_dialogueNodesCsv, "dialogue_nodes.csv", out loadError)
            || !TryValidateRequiredAsset(_dialogueChoicesCsv, "dialogue_choices.csv", out loadError)
            || !TryValidateRequiredAsset(_questsCsv, "quests.csv", out loadError)
            || !TryValidateRequiredAsset(_questObjectivesCsv, "quest_objectives.csv", out loadError)
            || !TryValidateRequiredAsset(_questRewardsCsv, "quest_rewards.csv", out loadError)
            || !TryValidateRequiredAsset(_questConsumeItemsCsv, "quest_consume_items.csv", out loadError)
            || !TryValidateRequiredAsset(_localizationCsv, "localization.csv", out loadError))
        {
            return false;
        }

        source = new GameDataCsvSource(
            CreateCsvFile(_dialogueActorsCsv),
            CreateCsvFile(_dialoguePortraitsCsv),
            CreateCsvFile(_dialogueGraphsCsv),
            CreateCsvFile(_dialogueNodesCsv),
            CreateCsvFile(_dialogueChoicesCsv),
            CreateCsvFile(_questsCsv),
            CreateCsvFile(_questObjectivesCsv),
            CreateCsvFile(_questRewardsCsv),
            CreateCsvFile(_questConsumeItemsCsv),
            CreateCsvFile(_localizationCsv),
            CreateOptionalCsvFile(_npcDialogueBindingsCsv),
            CreateOptionalCsvFile(_npcQuestDialogueRoutesCsv));
        return true;
    }

    /// <summary>
    /// 필수 CSV TextAsset이 연결되고 내용이 있는지 확인합니다.
    /// </summary>
    /// <param name="textAsset">검증할 필수 CSV TextAsset입니다.</param>
    /// <param name="expectedFileName">오류에 표시할 예상 CSV 파일 이름입니다.</param>
    /// <param name="loadError">TextAsset 연결 또는 내용 오류입니다.</param>
    /// <returns>TextAsset이 연결되고 내용이 비어 있지 않으면 true입니다.</returns>
    private static bool TryValidateRequiredAsset(
        TextAsset textAsset,
        string expectedFileName,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (textAsset == null)
        {
            loadError = new GameDataCsvLoadError(
                expectedFileName,
                0,
                0,
                string.Empty,
                "필수 CSV TextAsset이 인스펙터에 연결되지 않았습니다.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(textAsset.text))
        {
            return true;
        }

        loadError = new GameDataCsvLoadError(
            expectedFileName,
            0,
            0,
            string.Empty,
            "필수 CSV TextAsset 내용이 비어 있습니다.");
        return false;
    }

    /// <summary>
    /// Unity TextAsset을 순수 C# CSV 원본 데이터로 변환합니다.
    /// </summary>
    /// <param name="textAsset">변환할 CSV TextAsset입니다.</param>
    /// <returns>TextAsset 이름과 문자열이 포함된 CSV 원본 데이터입니다.</returns>
    private static GameDataCsvFile CreateCsvFile(TextAsset textAsset)
    {
        return new GameDataCsvFile(textAsset.name, textAsset.text);
    }

    /// <summary>
    /// 선택 Unity TextAsset이 있으면 순수 C# CSV 원본 데이터로 변환합니다.
    /// </summary>
    /// <param name="textAsset">변환할 선택 CSV TextAsset입니다.</param>
    /// <returns>CSV 원본 데이터이며 TextAsset이 없으면 null입니다.</returns>
    private static GameDataCsvFile CreateOptionalCsvFile(TextAsset textAsset)
    {
        return textAsset != null ? CreateCsvFile(textAsset) : null;
    }

    /// <summary>
    /// 현재 언어 문자열을 우선하고 비어 있으면 사용 가능한 다른 언어로 대체합니다.
    /// </summary>
    /// <param name="entry">문장을 선택할 로컬라이징 정의입니다.</param>
    /// <returns>우선순위에 따라 선택된 화면 표시 문자열입니다.</returns>
    private string GetPreferredText(LocalizationEntryDefinition entry)
    {
        string preferredText = _language switch
        {
            GameLanguage.ENGLISH => entry.EnglishText,
            GameLanguage.JAPANESE => entry.JapaneseText,
            _ => entry.KoreanText
        };

        if (!string.IsNullOrEmpty(preferredText))
        {
            return preferredText;
        }

        if (!string.IsNullOrEmpty(entry.KoreanText))
        {
            return entry.KoreanText;
        }

        if (!string.IsNullOrEmpty(entry.EnglishText))
        {
            return entry.EnglishText;
        }

        return entry.JapaneseText;
    }
}
