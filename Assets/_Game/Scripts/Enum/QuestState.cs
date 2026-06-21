using UnityEngine;

public enum QuestState
{
    QUEST_NOT_STARTED = 0,      // 미수락 (시작하지 않음)
    QUEST_IN_PROGRESS,          // 진행중
    QUEST_COMPLETED,            // 목표 달성 (완료 됨)
    QUEST_REWARDED              // 보상 수령 완료 (보상 받음)
}
