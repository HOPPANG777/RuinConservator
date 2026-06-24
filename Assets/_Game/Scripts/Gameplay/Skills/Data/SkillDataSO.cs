using UnityEngine;

//==============================================================================
// 기능: class::SkillDataSO
// 설명: 스킬의 파라미터가 들어있는 ScriptableObjects가 상속받을 공통 부모 클래스.
//==============================================================================
// TODO: 
//
//==============================================================================
[CreateAssetMenu(menuName = "_Game/Skill/SkillData", fileName = "SkillDataSO")]
public class SkillDataSO : ScriptableObject
{
    [Header("Skill")]
    [Tooltip("스킬의 카테고리")]
    [SerializeField] private SkillType _skillType;

    [Header("Skill")]
    [Tooltip("스킬 사용시 결정될 상태")]
    [SerializeField] private SkillActionState _skillActionState;




}
