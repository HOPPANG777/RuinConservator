using System.Collections.Generic;
using UnityEngine;

//==============================================================================
// 기능: class::DialogueVisualAssetEntry
// 설명: CSV 에셋 ID와 Unity Sprite 참조를 연결하는 직렬화 항목
//==============================================================================
[System.Serializable]
public sealed class DialogueVisualAssetEntry
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Tooltip("CSV의 sprite_id 또는 background_id와 일치하는 에셋 ID입니다.")]
    [SerializeField]
    private string _assetId;

    [Tooltip("에셋 ID에 연결할 Unity Sprite입니다.")]
    [SerializeField]
    private Sprite _sprite;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>CSV에서 참조하는 에셋 ID입니다.</summary>
    public string AssetId => _assetId;

    /// <summary>에셋 ID에 연결된 Unity Sprite입니다.</summary>
    public Sprite Sprite => _sprite;
}

//==============================================================================
// 기능: class::DialogueVisualCatalogSO
// 설명: CSV 대화 에셋 ID를 Unity Sprite로 변환하는 단일 비주얼 카탈로그
//==============================================================================
[CreateAssetMenu(fileName = "DialogueVisualCatalog", menuName = "Ruin Conservator/Dialogue/Visual Catalog")]
public sealed class DialogueVisualCatalogSO : ScriptableObject
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Portrait Sprites")]
    [Tooltip("dialogue_portraits.csv의 sprite_id와 초상화 Sprite 연결 목록입니다.")]
    [SerializeField]
    private List<DialogueVisualAssetEntry> _portraitSprites = new List<DialogueVisualAssetEntry>();

    [Header("Background Sprites")]
    [Tooltip("dialogue_graphs.csv와 dialogue_nodes.csv의 background_id 연결 목록입니다.")]
    [SerializeField]
    private List<DialogueVisualAssetEntry> _backgroundSprites = new List<DialogueVisualAssetEntry>();

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 초상화 Sprite를 에셋 ID로 즉시 조회하기 위한 캐시입니다.
    private readonly Dictionary<string, Sprite> _portraitSpriteById = new Dictionary<string, Sprite>();

    // 배경 Sprite를 에셋 ID로 즉시 조회하기 위한 캐시입니다.
    private readonly Dictionary<string, Sprite> _backgroundSpriteById = new Dictionary<string, Sprite>();

    // 직렬화 목록을 Dictionary 캐시로 변환했는지 여부입니다.
    private bool _isCacheBuilt;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 에셋이 활성화될 때 다음 조회에서 캐시를 다시 구성하도록 표시합니다.
    /// </summary>
    private void OnEnable()
    {
        _isCacheBuilt = false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 인스펙터 값이 변경되면 다음 조회에서 캐시를 다시 구성하도록 표시합니다.
    /// </summary>
    private void OnValidate()
    {
        _isCacheBuilt = false;
    }
#endif

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// CSV 초상화 Sprite ID로 Unity Sprite를 조회합니다.
    /// </summary>
    /// <param name="spriteId">dialogue_portraits.csv의 sprite_id입니다.</param>
    /// <param name="sprite">조회된 초상화 Sprite입니다.</param>
    /// <returns>유효한 초상화 Sprite를 찾으면 true입니다.</returns>
    public bool TryGetPortraitSprite(string spriteId, out Sprite sprite)
    {
        EnsureCache();
        sprite = null;

        return !string.IsNullOrWhiteSpace(spriteId)
            && _portraitSpriteById.TryGetValue(spriteId, out sprite);
    }

    /// <summary>
    /// CSV 대화 배경 ID로 Unity Sprite를 조회합니다.
    /// </summary>
    /// <param name="backgroundId">대화 그래프 또는 노드의 background_id입니다.</param>
    /// <param name="sprite">조회된 배경 Sprite입니다.</param>
    /// <returns>유효한 배경 Sprite를 찾으면 true입니다.</returns>
    public bool TryGetBackgroundSprite(string backgroundId, out Sprite sprite)
    {
        EnsureCache();
        sprite = null;

        return !string.IsNullOrWhiteSpace(backgroundId)
            && _backgroundSpriteById.TryGetValue(backgroundId, out sprite);
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 직렬화 목록을 최초 조회 시 한 번만 Dictionary 캐시로 변환합니다.
    /// </summary>
    private void EnsureCache()
    {
        if (_isCacheBuilt)
        {
            return;
        }

        _portraitSpriteById.Clear();
        _backgroundSpriteById.Clear();
        AddEntries(_portraitSprites, _portraitSpriteById, "portrait");
        AddEntries(_backgroundSprites, _backgroundSpriteById, "background");
        _isCacheBuilt = true;
    }

    /// <summary>
    /// 유효한 비주얼 항목을 ID 조회 캐시에 등록합니다.
    /// </summary>
    /// <param name="entries">등록할 직렬화 항목 목록입니다.</param>
    /// <param name="spriteById">항목을 등록할 대상 Dictionary입니다.</param>
    /// <param name="category">중복 로그에 표시할 에셋 분류입니다.</param>
    private static void AddEntries(
        IReadOnlyList<DialogueVisualAssetEntry> entries,
        Dictionary<string, Sprite> spriteById,
        string category)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            DialogueVisualAssetEntry entry = entries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.AssetId) || entry.Sprite == null)
            {
                continue;
            }

            if (!spriteById.TryAdd(entry.AssetId, entry.Sprite))
            {
                Debug.LogWarning(
                    $"[DialogueVisualCatalogSO] 중복 비주얼 에셋 ID를 건너뜁니다. category={category}, assetId={entry.AssetId}");
            }
        }
    }
}
