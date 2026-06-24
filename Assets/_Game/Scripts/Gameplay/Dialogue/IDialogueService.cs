//==============================================================================
// 기능: interface::IDialogueService
// 설명: 대화 시스템에 대한 외부 공개 계약
//==============================================================================
public interface IDialogueService
{
    /// <summary>현재 대화창이 열려 있는지 여부입니다.</summary>
    bool IsDialogueOpen { get; }

    /// <summary>
    /// 지정한 대화 그래프를 시작합니다.
    /// </summary>
    /// <param name="dialogueGraph">실행할 대화 그래프입니다.</param>
    /// <param name="actorOverride">NPC별로 덮어쓸 화자 데이터입니다.</param>
    /// <returns>대화를 시작했으면 true입니다.</returns>
    bool TryStartDialogue(DialogueGraphSO dialogueGraph, DialogueActorSO actorOverride = null);

    /// <summary>
    /// 선택지가 없는 현재 노드를 다음 노드로 진행합니다.
    /// </summary>
    void Advance();

    /// <summary>
    /// 현재 노드의 선택지를 선택합니다.
    /// </summary>
    /// <param name="choiceIndex">선택지 인덱스입니다.</param>
    void SelectChoice(int choiceIndex);

    /// <summary>
    /// 현재 대화를 종료합니다.
    /// </summary>
    void EndDialogue();
}
