using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialoguePortraitData
{
    [Header("Portrait")]
    [Tooltip("대화 노드에서 선택할 초상화 ID입니다. 예: default, happy, worried")]
    [SerializeField]
    private string _portraitId;

    [Header("Portrait")]
    [Tooltip("인스펙터 드롭다운에 표시할 이름입니다. 비워두면 Portrait Id를 표시합니다.")]
    [SerializeField]
    private string _displayName;

    [Header("Portrait")]
    [Tooltip("해당 상황에서 출력할 초상화 스프라이트입니다.")]
    [SerializeField]
    private Sprite _portrait;

    public string PortraitId => _portraitId;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_displayName))
            {
                return _displayName;
            }

            return _portraitId;
        }
    }

    public Sprite Portrait => _portrait;
}

[CreateAssetMenu(fileName = "DialogueActor", menuName = "Ruin Conservator/Dialogue/Actor")]
public class DialogueActorSO : ScriptableObject
{
    [Header("Actor")]
    [Tooltip("대화 화자의 고유 ID입니다.")]
    [SerializeField]
    private string _actorId;

    [Header("Actor")]
    [Tooltip("대화창에 표시할 화자 이름입니다.")]
    [SerializeField]
    private string _displayName;

    [Header("Actor")]
    [Tooltip("노드에서 별도 초상화를 선택하지 않았을 때 사용할 기본 초상화입니다.")]
    [SerializeField]
    private Sprite _portrait;

    [Header("Portraits")]
    [Tooltip("상황별로 선택할 수 있는 초상화 목록입니다.")]
    [SerializeField]
    private List<DialoguePortraitData> _portraits = new List<DialoguePortraitData>();

    public string ActorId => _actorId;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_displayName))
            {
                return _displayName;
            }

            return _actorId;
        }
    }

    public Sprite Portrait => _portrait;

    public IReadOnlyList<DialoguePortraitData> Portraits => _portraits;

    public Sprite GetPortrait(string portraitId)
    {
        if (!string.IsNullOrWhiteSpace(portraitId) && _portraits != null)
        {
            for (int i = 0; i < _portraits.Count; i++)
            {
                DialoguePortraitData portraitData = _portraits[i];

                if (portraitData == null || portraitData.PortraitId != portraitId)
                {
                    continue;
                }

                if (portraitData.Portrait != null)
                {
                    return portraitData.Portrait;
                }

                break;
            }
        }

        return _portrait;
    }
}
