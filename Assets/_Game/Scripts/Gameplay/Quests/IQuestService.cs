//==============================================================================
// 기능: interface::IQuestService
// 설명: 퀘스트 상태 관리 시스템에 대한 외부 공개 계약
//==============================================================================
public interface IQuestService
{
    /// <summary>
    /// 퀘스트 ID로 퀘스트 데이터를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <param name="questData">조회된 퀘스트 데이터입니다.</param>
    /// <returns>퀘스트 데이터를 찾으면 true입니다.</returns>
    bool TryGetQuestData(string questId, out QuestDataSO questData);

    /// <summary>
    /// 퀘스트 상태를 조회합니다.
    /// </summary>
    /// <param name="questData">조회할 퀘스트 데이터입니다.</param>
    /// <returns>현재 퀘스트 상태입니다.</returns>
    QuestState GetQuestState(QuestDataSO questData);

    /// <summary>
    /// 퀘스트 상태를 조회합니다.
    /// </summary>
    /// <param name="questId">조회할 퀘스트 ID입니다.</param>
    /// <returns>현재 퀘스트 상태입니다.</returns>
    QuestState GetQuestState(string questId);

    /// <summary>
    /// 퀘스트를 수락 상태로 변경합니다.
    /// </summary>
    /// <param name="questData">수락할 퀘스트 데이터입니다.</param>
    /// <returns>상태 변경에 성공하면 true입니다.</returns>
    bool TryAcceptQuest(QuestDataSO questData);

    /// <summary>
    /// 퀘스트를 수락 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">수락할 퀘스트 ID입니다.</param>
    /// <returns>상태 변경에 성공하면 true입니다.</returns>
    bool TryAcceptQuest(string questId);

    /// <summary>
    /// 퀘스트 목표 달성 여부를 확인하고 완료 상태로 갱신합니다.
    /// </summary>
    /// <param name="questData">갱신할 퀘스트 데이터입니다.</param>
    /// <returns>이번 호출로 완료 상태가 되었으면 true입니다.</returns>
    bool RefreshQuestProgress(QuestDataSO questData);

    /// <summary>
    /// 퀘스트 목표 달성 여부를 확인하고 완료 상태로 갱신합니다.
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
    /// 완료 가능한 퀘스트를 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questData">완료할 퀘스트 데이터입니다.</param>
    /// <returns>완료 상태가 되었거나 이미 완료 상태면 true입니다.</returns>
    bool TryCompleteQuest(QuestDataSO questData);

    /// <summary>
    /// 완료 가능한 퀘스트를 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">완료할 퀘스트 ID입니다.</param>
    /// <returns>완료 상태가 되었거나 이미 완료 상태면 true입니다.</returns>
    bool TryCompleteQuest(string questId);

    /// <summary>
    /// 완료된 퀘스트 보상을 지급하고 보상 수령 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questData">보상을 받을 퀘스트 데이터입니다.</param>
    /// <returns>이번 호출로 보상을 지급했으면 true입니다.</returns>
    bool TryClaimReward(QuestDataSO questData);

    /// <summary>
    /// 완료된 퀘스트 보상을 지급하고 보상 수령 완료 상태로 변경합니다.
    /// </summary>
    /// <param name="questId">보상을 받을 퀘스트 ID입니다.</param>
    /// <returns>이번 호출로 보상을 지급했으면 true입니다.</returns>
    bool TryClaimReward(string questId);

    /// <summary>
    /// 런타임 퀘스트 상태를 초기화합니다.
    /// </summary>
    void Reset();
}
