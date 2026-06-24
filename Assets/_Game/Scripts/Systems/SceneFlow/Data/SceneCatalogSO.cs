using System.Collections.Generic;
using UnityEngine;

//==============================================================================
// 기능: class::SceneCatalogSO
// 설명: 씬 전환에 사용할 실제 씬 이름과 식별 정보를 보관하는 카탈로그 에셋
//==============================================================================
// NOTE: 실제 Unity 씬 이름을 코드에 직접 흩뿌리지 않고, 인스펙터에서 한 곳에 등록해 관리합니다.
//
//==============================================================================
[CreateAssetMenu(menuName = "_Game/Scene/SceneCatalog", fileName = "SceneCatalogSO")]
public class SceneCatalogSO : ScriptableObject
{
    //==============================================================================
    // 내부 데이터
    //==============================================================================

    /// <summary>
    /// 씬 전환에 필요한 식별자와 실제 Unity 씬 이름을 묶어 둔 엔트리입니다.
    /// </summary>
    [System.Serializable]
    public class SceneEntry
    {
        [Header("Scene")]
        [Tooltip("비스테이지 씬을 호출할 때 사용하는 안정적인 ID입니다. 예: TITLE")]
        [SerializeField]
        private string _sceneId;

        [Header("Scene")]
        [Tooltip("Unity에 등록된 실제 씬 이름입니다. 예: TitleScene")]
        [SerializeField]
        private string _sceneName;

        [Header("Scene")]
        [Tooltip("씬의 역할 분류입니다. 스테이지 씬은 STAGE로 지정합니다.")]
        [SerializeField]
        private SceneCategory _sceneCategory = SceneCategory.NONE;

        [Header("Scene")]
        [Tooltip("스테이지 씬일 때 사용하는 번호입니다. 스테이지가 아니면 -1을 사용합니다.")]
        [SerializeField]
        private int _stageNumber = -1;

        /// <summary>비스테이지 씬을 호출할 때 사용하는 안정적인 ID입니다.</summary>
        public string SceneId
        {
            get => _sceneId;
        }

        /// <summary>Unity에 등록된 실제 씬 이름입니다.</summary>
        public string SceneName
        {
            get => _sceneName;
        }

        /// <summary>씬의 역할 분류입니다.</summary>
        public SceneCategory SceneCategory
        {
            get => _sceneCategory;
        }

        /// <summary>스테이지 씬일 때 사용하는 번호입니다.</summary>
        public int StageNumber
        {
            get => _stageNumber;
        }
    }

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Scene")]
    [Tooltip("씬 전환에 사용할 씬 등록 목록입니다.")]
    [SerializeField]
    private List<SceneEntry> _entries = new List<SceneEntry>();

    //==============================================================================
    // 조회 API
    //==============================================================================

    /// <summary>
    /// 스테이지 번호와 일치하는 스테이지 씬 엔트리를 조회합니다.
    /// </summary>
    /// <param name="stageNumber">조회할 스테이지 번호입니다.</param>
    /// <param name="entry">조회된 씬 엔트리입니다.</param>
    /// <returns>일치하는 엔트리가 있으면 true를 반환합니다.</returns>
    public bool TryGetByStageNumber(int stageNumber, out SceneEntry entry)
    {
        // 출력 값을 먼저 초기화해 실패 경로에서도 안전하게 반환합니다.
        entry = null;

        // 등록 목록이 없으면 조회할 수 없으므로 실패 처리합니다.
        if (_entries == null)
        {
            return false;
        }

        // GC 오버헤드를 줄이기 위해 foreach 대신 for문으로 순회합니다.
        for (int i = 0; i < _entries.Count; i++)
        {
            SceneEntry currentEntry = _entries[i];

            // 비어 있는 엔트리는 건너뜁니다.
            if (currentEntry == null)
            {
                continue;
            }

            // 스테이지 분류와 번호가 모두 일치하는 엔트리만 유효한 스테이지로 취급합니다.
            if (currentEntry.SceneCategory == SceneCategory.STAGE && currentEntry.StageNumber == stageNumber)
            {
                entry = currentEntry;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 씬 ID와 일치하는 씬 엔트리를 조회합니다.
    /// </summary>
    /// <param name="sceneId">조회할 씬 ID입니다.</param>
    /// <param name="entry">조회된 씬 엔트리입니다.</param>
    /// <returns>일치하는 엔트리가 있으면 true를 반환합니다.</returns>
    public bool TryGetById(string sceneId, out SceneEntry entry)
    {
        // 출력 값을 먼저 초기화해 실패 경로에서도 안전하게 반환합니다.
        entry = null;

        // 비어 있는 ID나 등록 목록이 없으면 조회할 수 없으므로 실패 처리합니다.
        if (string.IsNullOrWhiteSpace(sceneId) || _entries == null)
        {
            return false;
        }

        // GC 오버헤드를 줄이기 위해 foreach 대신 for문으로 순회합니다.
        for (int i = 0; i < _entries.Count; i++)
        {
            SceneEntry currentEntry = _entries[i];

            // 비어 있는 엔트리는 건너뜁니다.
            if (currentEntry == null)
            {
                continue;
            }

            // 대소문자 차이로 인한 호출 실수를 줄이기 위해 OrdinalIgnoreCase로 비교합니다.
            if (string.Equals(currentEntry.SceneId, sceneId, System.StringComparison.OrdinalIgnoreCase))
            {
                entry = currentEntry;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Unity 씬 이름과 일치하는 씬 엔트리를 조회합니다.
    /// </summary>
    /// <param name="sceneName">Unity에 등록된 실제 씬 이름입니다.</param>
    /// <param name="entry">조회된 씬 엔트리입니다.</param>
    /// <returns>일치하는 엔트리가 있으면 true를 반환합니다.</returns>
    public bool TryGetBySceneName(string sceneName, out SceneEntry entry)
    {
        // 출력 값을 먼저 초기화해 실패 경로에서도 안전하게 반환합니다.
        entry = null;

        // 비어 있는 씬 이름이나 등록 목록이 없으면 조회할 수 없으므로 실패 처리합니다.
        if (string.IsNullOrWhiteSpace(sceneName) || _entries == null)
        {
            return false;
        }

        // GC 오버헤드를 줄이기 위해 foreach 대신 for문으로 순회합니다.
        for (int i = 0; i < _entries.Count; i++)
        {
            SceneEntry currentEntry = _entries[i];

            // 비어 있는 엔트리는 건너뜁니다.
            if (currentEntry == null)
            {
                continue;
            }

            // Unity 씬 이름이 일치하는 엔트리를 반환합니다.
            if (string.Equals(currentEntry.SceneName, sceneName, System.StringComparison.Ordinal))
            {
                entry = currentEntry;
                return true;
            }
        }

        return false;
    }
}
