//==============================================================================
// 기능: interface::IQuestService
// 설명: CSV 퀘스트 정의의 조회, 상태 전환, 목표 및 보상 처리 계약
//==============================================================================
public interface IQuestService
{
    /// <summary>
    /// 퀘스트 ID로 읽기 전용 퀘스트 정의를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <param name="questDefinition">조회된 퀘스트 정의입니다.</param>
    /// <returns>퀘스트 정의를 찾으면 true입니다.</returns>
    bool TryGetQuestDefinition(string questId, out QuestDefinition questDefinition);

    /// <summary>
    /// 퀘스트 ID의 현재 런타임 상태를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <returns>현재 퀘스트 상태이며 등록되지 않은 ID는 미시작 상태입니다.</returns>
    QuestState GetQuestState(string questId);

    /// <summary>
    /// 퀘스트를 진행 중 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">수락할 퀘스트 ID입니다.</param>
    /// <returns>상태 변경에 성공하면 true입니다.</returns>
    bool TryAcceptQuest(string questId);

    /// <summary>
    /// 퀘스트 목표를 확인하고 자동 소모 아이템이 없으면 완료 상태로 갱신합니다.
    /// </summary>
    /// <param name="questId">갱신할 퀘스트 ID입니다.</param>
    /// <returns>이번 호출로 완료 상태가 되었으면 true입니다.</returns>
    bool RefreshQuestProgress(string questId);

    /// <summary>
    /// 진행 중인 모든 퀘스트의 목표 달성 여부를 갱신합니다.
    /// </summary>
    /// <returns>이번 호출로 완료 상태가 된 퀘스트 수입니다.</returns>
    int RefreshAllInProgressQuests();

    /// <summary>
    /// 목표가 달성된 퀘스트를 완료하고 필요한 아이템을 자동 소모합니다.
    /// </summary>
    /// <param name="questId">완료할 퀘스트 ID입니다.</param>
    /// <returns>완료 상태가 되었거나 이미 완료 상태면 true입니다.</returns>
    bool TryCompleteQuest(string questId);

    /// <summary>
    /// 완료된 퀘스트의 보상을 지급하고 보상 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">보상을 받을 퀘스트 ID입니다.</param>
    /// <returns>이번 호출로 보상을 지급했으면 true입니다.</returns>
    bool TryClaimReward(string questId);

    /// <summary>
    /// 모든 런타임 퀘스트 상태를 초기화합니다.
    /// </summary>
    void Reset();
}
