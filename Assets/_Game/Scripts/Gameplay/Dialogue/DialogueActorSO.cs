using UnityEngine;

//==============================================================================
// 기능: class::DialogueActorSO
// 설명: 대화창에 표시할 화자 이름과 초상화 데이터
//==============================================================================
[CreateAssetMenu(fileName = "DialogueActor", menuName = "Ruin Conservator/Dialogue/Actor")]
public class DialogueActorSO : ScriptableObject
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Actor")]
    [Tooltip("대화 화자의 고유 ID입니다.")]
    [SerializeField]
    private string _actorId;

    [Header("Actor")]
    [Tooltip("대화창에 표시할 화자 이름입니다.")]
    [SerializeField]
    private string _displayName;

    [Header("Actor")]
    [Tooltip("대화창에 표시할 초상화 스프라이트입니다.")]
    [SerializeField]
    private Sprite _portrait;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================

    /// <summary>대화 화자의 고유 ID입니다.</summary>
    public string ActorId => _actorId;

    /// <summary>대화창에 표시할 이름입니다.</summary>
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

    /// <summary>대화창에 표시할 초상화입니다.</summary>
    public Sprite Portrait => _portrait;
}
