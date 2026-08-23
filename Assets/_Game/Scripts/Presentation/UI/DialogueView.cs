using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// 기능: class::DialogueView
// 설명: CSV 대화 Definition과 해석된 Sprite를 Canvas UI에 출력하는 뷰
//==============================================================================
public sealed class DialogueView : MonoBehaviour
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Root")]
    [Tooltip("대화창 전체 표시와 입력 차단을 제어할 CanvasGroup입니다.")]
    [SerializeField]
    private CanvasGroup _rootCanvasGroup;

    [Header("Background")]
    [Tooltip("대화창 배경 Sprite를 출력할 Image입니다.")]
    [SerializeField]
    private Image _dialogueBackgroundImage;

    [Header("Actor")]
    [Tooltip("왼쪽 초상화 필드가 비어 있을 때 대신 사용할 기존 단일 Image입니다.")]
    [SerializeField]
    private Image _portraitImage;

    [Tooltip("왼쪽 화자 초상화를 출력할 Image입니다.")]
    [SerializeField]
    private Image _leftPortraitImage;

    [Tooltip("오른쪽 화자 초상화를 출력할 Image입니다.")]
    [SerializeField]
    private Image _rightPortraitImage;

    [Tooltip("현재 말하는 화자 초상화에 적용할 색상입니다.")]
    [SerializeField]
    private Color _activePortraitColor = Color.white;

    [Tooltip("현재 말하지 않는 화자 초상화에 적용할 색상입니다.")]
    [SerializeField]
    private Color _inactivePortraitColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);

    [Tooltip("초상화 Sprite가 없을 때 해당 Image를 숨길지 여부입니다.")]
    [SerializeField]
    private bool _hidePortraitWhenEmpty = true;

    [Header("Text")]
    [Tooltip("현재 화자 이름을 출력할 TMP 텍스트입니다.")]
    [SerializeField]
    private TMP_Text _speakerNameText;

    [Tooltip("현재 대화 본문을 출력할 TMP 텍스트입니다.")]
    [SerializeField]
    private TMP_Text _bodyText;

    [Header("Choices")]
    [Tooltip("선택지 버튼을 배치할 부모 Transform입니다.")]
    [SerializeField]
    private Transform _choiceRoot;

    [Tooltip("선택지 하나를 출력할 재사용 Button 프리팹입니다.")]
    [SerializeField]
    private Button _choiceButtonPrefab;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 생성한 선택지 버튼을 재사용하기 위한 풀입니다.
    private readonly List<Button> _choiceButtons = new List<Button>();

    // 문장 Key를 현재 언어 문자열로 변환할 서비스입니다.
    private ILocalizationService _localizationService;

    // UI에 기본으로 지정된 대화창 배경 Sprite입니다.
    private Sprite _defaultDialogueBackgroundSprite;

    // 기본 배경 Sprite를 한 번 저장했는지 여부입니다.
    private bool _hasCachedDialogueBackground;

    // 왼쪽 초상화 Image이며 신규 필드가 비어 있으면 기존 단일 Image를 사용합니다.
    private Image LeftPortraitImage => _leftPortraitImage != null ? _leftPortraitImage : _portraitImage;

    // 오른쪽 초상화 Image입니다.
    private Image RightPortraitImage => _rightPortraitImage;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// UI에 지정된 기본 배경 Sprite를 저장합니다.
    /// </summary>
    private void Awake()
    {
        CacheDefaultDialogueBackground();
    }

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// 문장 Key를 변환할 로컬라이징 서비스를 주입합니다.
    /// </summary>
    /// <param name="localizationService">현재 언어 문자열을 제공할 서비스입니다.</param>
    public void Init(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        Hide();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 현재 노드의 문장, 선택지, 좌우 초상화와 배경을 대화 UI에 출력합니다.
    /// </summary>
    /// <param name="speakerNameKey">현재 화자 이름 Localization Key입니다.</param>
    /// <param name="node">출력할 CSV 대화 노드 Definition입니다.</param>
    /// <param name="onChoiceSelected">선택지 선택 콜백입니다.</param>
    /// <param name="leftPortrait">왼쪽에 표시할 초상화 Sprite입니다.</param>
    /// <param name="rightPortrait">오른쪽에 표시할 초상화 Sprite입니다.</param>
    /// <param name="isLeftActorSpeaking">현재 말하는 화자가 왼쪽 화자인지 여부입니다.</param>
    /// <param name="dialogueBackground">이번 노드에 표시할 배경 Sprite입니다.</param>
    public void ShowNode(
        string speakerNameKey,
        DialogueNodeDefinition node,
        Action<int> onChoiceSelected,
        Sprite leftPortrait,
        Sprite rightPortrait,
        bool isLeftActorSpeaking,
        Sprite dialogueBackground)
    {
        if (node == null)
        {
            return;
        }

        SetVisible(true);
        ApplyDialogueBackground(dialogueBackground);
        ApplyActor(speakerNameKey, leftPortrait, rightPortrait, isLeftActorSpeaking);

        if (_bodyText != null)
        {
            _bodyText.text = GetLocalizedText(node.BodyTextKey);
        }

        RebuildChoices(node.Choices, onChoiceSelected);
    }

    /// <summary>
    /// 대화 UI와 모든 선택지 버튼을 숨깁니다.
    /// </summary>
    public void Hide()
    {
        ClearChoices();
        SetVisible(false);
    }

    //==============================================================================
    // 내부 로직 - 표시
    //==============================================================================

    /// <summary>
    /// 대화창 전체 표시 상태를 변경합니다.
    /// </summary>
    /// <param name="isVisible">표시 여부입니다.</param>
    private void SetVisible(bool isVisible)
    {
        if (_rootCanvasGroup != null)
        {
            _rootCanvasGroup.gameObject.SetActive(true);
            _rootCanvasGroup.alpha = isVisible ? 1.0f : 0.0f;
            _rootCanvasGroup.interactable = isVisible;
            _rootCanvasGroup.blocksRaycasts = isVisible;
            return;
        }

        gameObject.SetActive(isVisible);
    }

    /// <summary>
    /// 대화창 배경 Sprite 또는 UI 기본 Sprite를 출력합니다.
    /// </summary>
    /// <param name="dialogueBackground">출력할 선택적 배경 Sprite입니다.</param>
    private void ApplyDialogueBackground(Sprite dialogueBackground)
    {
        if (_dialogueBackgroundImage == null)
        {
            return;
        }

        CacheDefaultDialogueBackground();
        _dialogueBackgroundImage.sprite = dialogueBackground != null
            ? dialogueBackground
            : _defaultDialogueBackgroundSprite;
        _dialogueBackgroundImage.enabled = _dialogueBackgroundImage.sprite != null
            || _dialogueBackgroundImage.color.a > 0.0f;
    }

    /// <summary>
    /// UI에 지정된 기본 대화창 배경 Sprite를 한 번 저장합니다.
    /// </summary>
    private void CacheDefaultDialogueBackground()
    {
        if (_hasCachedDialogueBackground || _dialogueBackgroundImage == null)
        {
            return;
        }

        _defaultDialogueBackgroundSprite = _dialogueBackgroundImage.sprite;
        _hasCachedDialogueBackground = true;
    }

    /// <summary>
    /// 현재 화자 이름과 좌우 초상화의 활성 색상을 출력합니다.
    /// </summary>
    /// <param name="speakerNameKey">현재 화자 이름 Localization Key입니다.</param>
    /// <param name="leftPortrait">왼쪽 초상화 Sprite입니다.</param>
    /// <param name="rightPortrait">오른쪽 초상화 Sprite입니다.</param>
    /// <param name="isLeftActorSpeaking">왼쪽 화자가 말하고 있는지 여부입니다.</param>
    private void ApplyActor(
        string speakerNameKey,
        Sprite leftPortrait,
        Sprite rightPortrait,
        bool isLeftActorSpeaking)
    {
        if (_speakerNameText != null)
        {
            _speakerNameText.text = GetLocalizedText(speakerNameKey);
        }

        ApplyPortrait(LeftPortraitImage, leftPortrait, isLeftActorSpeaking);
        ApplyPortrait(RightPortraitImage, rightPortrait, !isLeftActorSpeaking);
    }

    /// <summary>
    /// 초상화 Image 하나의 Sprite, 색상과 표시 상태를 갱신합니다.
    /// </summary>
    /// <param name="portraitImage">출력 대상 Image입니다.</param>
    /// <param name="portrait">출력할 초상화 Sprite입니다.</param>
    /// <param name="isSpeaking">이 위치의 화자가 현재 말하고 있는지 여부입니다.</param>
    private void ApplyPortrait(Image portraitImage, Sprite portrait, bool isSpeaking)
    {
        if (portraitImage == null)
        {
            return;
        }

        portraitImage.sprite = portrait;
        portraitImage.color = isSpeaking ? _activePortraitColor : _inactivePortraitColor;

        if (_hidePortraitWhenEmpty)
        {
            portraitImage.enabled = portrait != null;
        }
    }

    //==============================================================================
    // 내부 로직 - 선택지
    //==============================================================================

    /// <summary>
    /// 현재 노드의 선택지 버튼을 재사용해 갱신합니다.
    /// </summary>
    /// <param name="choices">표시할 CSV 선택지 Definition 목록입니다.</param>
    /// <param name="onChoiceSelected">선택지 선택 콜백입니다.</param>
    private void RebuildChoices(
        IReadOnlyList<DialogueChoiceDefinition> choices,
        Action<int> onChoiceSelected)
    {
        ClearChoices();

        if (choices == null || choices.Count == 0 || _choiceRoot == null || _choiceButtonPrefab == null)
        {
            return;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            Button button = GetOrCreateChoiceButton(i);
            DialogueChoiceDefinition choice = choices[i];
            int choiceIndex = i;

            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { onChoiceSelected?.Invoke(choiceIndex); });

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = choice != null
                    ? GetLocalizedText(choice.ChoiceTextKey)
                    : string.Empty;
            }
        }
    }

    /// <summary>
    /// 선택지 버튼을 풀에서 재사용하거나 부족하면 새로 생성합니다.
    /// </summary>
    /// <param name="index">필요한 버튼 인덱스입니다.</param>
    /// <returns>사용할 선택지 버튼입니다.</returns>
    private Button GetOrCreateChoiceButton(int index)
    {
        while (_choiceButtons.Count <= index)
        {
            Button createdButton = Instantiate(_choiceButtonPrefab, _choiceRoot);
            _choiceButtons.Add(createdButton);
        }

        return _choiceButtons[index];
    }

    /// <summary>
    /// 모든 선택지 버튼을 숨기고 등록된 콜백을 제거합니다.
    /// </summary>
    private void ClearChoices()
    {
        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            Button button = _choiceButtons[i];

            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Localization Key를 현재 언어 문자열로 변환합니다.
    /// </summary>
    /// <param name="textKey">변환할 Localization Key입니다.</param>
    /// <returns>변환된 문자열이며 서비스가 없으면 입력 Key입니다.</returns>
    private string GetLocalizedText(string textKey)
    {
        return _localizationService != null
            ? _localizationService.GetText(textKey)
            : textKey ?? string.Empty;
    }
}
