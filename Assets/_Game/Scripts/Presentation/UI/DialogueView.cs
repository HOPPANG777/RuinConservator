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

    [Header("Actor")]
    [Tooltip("NPC 초상화를 출력할 Image입니다.")]
    [SerializeField]
    private Image _portraitImage;

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
        SetVisible(true);
        ApplyActor(actor);

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
    /// 화자 이름과 초상화를 출력합니다.
    /// </summary>
    /// <param name="actor">출력할 화자 데이터입니다.</param>
    private void ApplyActor(DialogueActorSO actor)
    {
        if (_speakerNameText != null)
        {
            _speakerNameText.text = actor != null ? actor.DisplayName : string.Empty;
        }

        if (_portraitImage == null)
        {
            return;
        }

        Sprite portrait = actor != null ? actor.Portrait : null;
        _portraitImage.sprite = portrait;

        if (_hidePortraitWhenEmpty)
        {
            _portraitImage.enabled = portrait != null;
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
