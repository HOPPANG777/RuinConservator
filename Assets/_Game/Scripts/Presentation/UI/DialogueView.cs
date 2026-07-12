using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// 기능: class::DialogueView
// 설명: 대화 데이터를 Canvas 기반 UI에 출력하는 뷰
//==============================================================================
public class DialogueView : MonoBehaviour
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Root")]
    [Tooltip("대화창 전체 표시/입력 차단을 제어할 CanvasGroup입니다.")]
    [SerializeField]
    private CanvasGroup _rootCanvasGroup;

    [Header("Background")]
    [Tooltip("대화창 배경 이미지를 출력할 Image입니다.")]
    [SerializeField]
    private Image _dialogueBackgroundImage;

    [Header("Actor")]
    [Tooltip("기존 단일 초상화 Image입니다. Left Portrait Image가 비어 있으면 왼쪽 초상화로 사용합니다.")]
    [SerializeField]
    private Image _portraitImage;

    [Header("Actor")]
    [Tooltip("왼쪽 화자 초상화를 출력할 Image입니다.")]
    [SerializeField]
    private Image _leftPortraitImage;

    [Header("Actor")]
    [Tooltip("오른쪽 화자 초상화를 출력할 Image입니다.")]
    [SerializeField]
    private Image _rightPortraitImage;

    [Header("Actor")]
    [Tooltip("현재 말하는 화자 초상화에 적용할 색상입니다.")]
    [SerializeField]
    private Color _activePortraitColor = Color.white;

    [Header("Actor")]
    [Tooltip("현재 말하지 않는 화자 초상화에 적용할 색상입니다.")]
    [SerializeField]
    private Color _inactivePortraitColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);

    [Header("Actor")]
    [Tooltip("초상화가 없을 때 Image를 숨길지 여부입니다.")]
    [SerializeField]
    private bool _hidePortraitWhenEmpty = true;

    [Header("Text")]
    [Tooltip("화자 이름을 출력할 TMP 텍스트입니다.")]
    [SerializeField]
    private TMP_Text _speakerNameText;

    [Header("Text")]
    [Tooltip("대화 본문을 출력할 TMP 텍스트입니다.")]
    [SerializeField]
    private TMP_Text _bodyText;

    [Header("Choices")]
    [Tooltip("선택지 버튼들이 배치될 부모 Transform입니다.")]
    [SerializeField]
    private Transform _choiceRoot;

    [Header("Choices")]
    [Tooltip("선택지 하나를 출력할 Button 프리팹입니다.")]
    [SerializeField]
    private Button _choiceButtonPrefab;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 재사용할 선택지 버튼 목록입니다.
    private readonly List<Button> _choiceButtons = new List<Button>();

    // UI에 기본으로 지정된 대화창 배경 이미지입니다.
    private Sprite _defaultDialogueBackgroundSprite;

    // 기본 배경 이미지를 한 번만 저장하기 위한 플래그입니다.
    private bool _hasCachedDialogueBackground;

    // 왼쪽 초상화 Image입니다. 신규 필드가 비어 있으면 기존 단일 초상화를 재사용합니다.
    private Image LeftPortraitImage => _leftPortraitImage != null ? _leftPortraitImage : _portraitImage;

    // 오른쪽 초상화 Image입니다.
    private Image RightPortraitImage => _rightPortraitImage;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// UI에 지정된 기본 배경 이미지를 저장합니다.
    /// </summary>
    private void Awake()
    {
        CacheDefaultDialogueBackground();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 노드 내용을 대화 UI에 출력합니다.
    /// </summary>
    /// <param name="actor">출력할 화자 데이터입니다.</param>
    /// <param name="node">출력할 대화 노드입니다.</param>
    /// <param name="onChoiceSelected">선택지 선택 콜백입니다.</param>
    public void ShowNode(DialogueActorSO actor, DialogueNodeData node, Action<int> onChoiceSelected)
    {
        ShowNode(actor, node, onChoiceSelected, actor, null, null);
    }

    /// <summary>
    /// 노드 내용과 좌우 화자 초상화, 대화창 배경을 대화 UI에 출력합니다.
    /// </summary>
    /// <param name="actor">현재 말하는 화자 데이터입니다.</param>
    /// <param name="node">출력할 대화 노드입니다.</param>
    /// <param name="onChoiceSelected">선택지 선택 콜백입니다.</param>
    /// <param name="leftActor">왼쪽에 표시할 화자입니다.</param>
    /// <param name="rightActor">오른쪽에 표시할 화자입니다.</param>
    /// <param name="dialogueBackground">이번 노드에 표시할 대화창 배경 이미지입니다.</param>
    public void ShowNode(
        DialogueActorSO actor,
        DialogueNodeData node,
        Action<int> onChoiceSelected,
        DialogueActorSO leftActor,
        DialogueActorSO rightActor,
        Sprite dialogueBackground)
    {
        SetVisible(true);
        ApplyDialogueBackground(dialogueBackground);
        ApplyActor(actor, node != null ? node.PortraitId : string.Empty, leftActor, rightActor);

        if (_bodyText != null)
        {
            _bodyText.text = node != null ? node.BodyText : string.Empty;
        }

        RebuildChoices(node, onChoiceSelected);
    }

    /// <summary>
    /// 대화 UI를 숨깁니다.
    /// </summary>
    public void Hide()
    {
        ClearChoices();
        SetVisible(false);
    }

    //==============================================================================
    // 내부 로직
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
    /// 대화창 배경 이미지를 출력합니다.
    /// </summary>
    /// <param name="dialogueBackground">출력할 배경 이미지입니다.</param>
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
    /// UI에 지정된 기본 대화창 배경 이미지를 저장합니다.
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
    /// 화자 이름과 좌우 초상화를 출력합니다.
    /// </summary>
    /// <param name="actor">현재 말하는 화자 데이터입니다.</param>
    /// <param name="portraitId">현재 말하는 화자에게 적용할 초상화 ID입니다.</param>
    /// <param name="leftActor">왼쪽에 표시할 화자입니다.</param>
    /// <param name="rightActor">오른쪽에 표시할 화자입니다.</param>
    private void ApplyActor(
        DialogueActorSO actor,
        string portraitId,
        DialogueActorSO leftActor,
        DialogueActorSO rightActor)
    {
        if (_speakerNameText != null)
        {
            _speakerNameText.text = actor != null ? actor.DisplayName : string.Empty;
        }

        if (leftActor == null && rightActor == null)
        {
            leftActor = actor;
        }

        ApplyPortrait(LeftPortraitImage, leftActor, actor, portraitId);
        ApplyPortrait(RightPortraitImage, rightActor, actor, portraitId);
    }

    /// <summary>
    /// 초상화 Image 하나를 갱신합니다.
    /// </summary>
    /// <param name="portraitImage">출력 대상 Image입니다.</param>
    /// <param name="portraitActor">이 위치에 표시할 화자입니다.</param>
    /// <param name="speakingActor">현재 말하는 화자입니다.</param>
    /// <param name="speakingPortraitId">현재 말하는 화자에게 적용할 초상화 ID입니다.</param>
    private void ApplyPortrait(
        Image portraitImage,
        DialogueActorSO portraitActor,
        DialogueActorSO speakingActor,
        string speakingPortraitId)
    {
        if (portraitImage == null)
        {
            return;
        }

        bool isSpeaking = portraitActor != null && portraitActor == speakingActor;
        Sprite portrait = portraitActor != null
            ? portraitActor.GetPortrait(isSpeaking ? speakingPortraitId : string.Empty)
            : null;

        portraitImage.sprite = portrait;
        portraitImage.color = isSpeaking ? _activePortraitColor : _inactivePortraitColor;

        if (_hidePortraitWhenEmpty)
        {
            portraitImage.enabled = portrait != null;
        }
    }

    /// <summary>
    /// 현재 노드의 선택지 버튼들을 갱신합니다.
    /// </summary>
    /// <param name="node">선택지를 표시할 노드입니다.</param>
    /// <param name="onChoiceSelected">선택지 선택 콜백입니다.</param>
    private void RebuildChoices(DialogueNodeData node, Action<int> onChoiceSelected)
    {
        ClearChoices();

        if (node == null || node.HasChoices == false || _choiceRoot == null || _choiceButtonPrefab == null)
        {
            return;
        }

        for (int i = 0; i < node.Choices.Count; i++)
        {
            Button button = GetOrCreateChoiceButton(i);
            DialogueChoiceData choice = node.Choices[i];
            int choiceIndex = i;

            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { onChoiceSelected?.Invoke(choiceIndex); });

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = choice != null ? choice.ChoiceText : string.Empty;
            }
        }
    }

    /// <summary>
    /// 선택지 버튼을 재사용하거나 새로 생성합니다.
    /// </summary>
    /// <param name="index">필요한 버튼 인덱스입니다.</param>
    /// <returns>사용할 버튼입니다.</returns>
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
    /// 모든 선택지 버튼을 숨기고 콜백을 제거합니다.
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
}
