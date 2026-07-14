//==============================================================================
// 기능: interface::IDialogueService
// 설명: CSV 대화 그래프의 시작, 진행, 선택 및 종료 계약
//==============================================================================
public interface IDialogueService
{
    /// <summary>현재 대화창이 열려 있는지 여부입니다.</summary>
    bool IsDialogueOpen { get; }

    /// <summary>
    /// 대화 그래프 ID와 선택적 화자 ID로 대화를 시작합니다.
    /// </summary>
    /// <param name="graphId">실행할 CSV 대화 그래프 ID입니다.</param>
    /// <param name="actorOverrideId">그래프 기본 화자를 덮어쓸 선택적 화자 ID입니다.</param>
    /// <returns>대화를 시작했으면 true입니다.</returns>
    bool TryStartDialogue(string graphId, string actorOverrideId = null);

    /// <summary>
    /// NPC ID의 기본 연결과 퀘스트 상태별 경로를 해석해 대화를 시작합니다.
    /// </summary>
    /// <param name="npcId">상호작용한 NPC ID입니다.</param>
    /// <returns>NPC 대화를 시작했으면 true입니다.</returns>
    bool TryStartNpcDialogue(string npcId);

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
