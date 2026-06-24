using System.Collections.Generic;
using UnityEngine;

//==============================================================================
// 기능: class::ProgressController
// 설명: 게임 진행 상태의 런타임 저장소 (전역 플래그 + 씬 오브젝트 상태)
//==============================================================================
// NOTE:
//  - 싱글톤이 아닙니다. 외부 접근은 GameRoot.Instance.Progress(IProgressService) 경유.
//  - 디스크 I/O는 Phase 5에서 추가합니다.
//==============================================================================
[DisallowMultipleComponent]
public class ProgressController : MonoBehaviour, IProgressService
{
    //==============================================================================
    // 내부 타입
    //==============================================================================

    /// <summary>스테이지 번호 + 오브젝트 ID를 묶은 딕셔너리 키입니다.</summary>
    private struct ObjectStateKey
    {
        // 스테이지 번호입니다.
        public int StageNumber;

        // 씬 내 오브젝트 고유 ID입니다.
        public string ObjectId;

        /// <summary>키를 생성합니다.</summary>
        /// <param name="stageNumber">스테이지 번호입니다.</param>
        /// <param name="objectId">오브젝트 ID입니다.</param>
        public ObjectStateKey(int stageNumber, string objectId)
        {
            StageNumber = stageNumber;
            ObjectId = objectId;
        }
    }

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 전역 플래그 저장소입니다.
    private Dictionary<string, bool> _globalFlags = new Dictionary<string, bool>();

    // 씬 오브젝트 상태(JSON blob) 저장소입니다.
    private Dictionary<ObjectStateKey, string> _objectStates = new Dictionary<ObjectStateKey, string>();

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다.
    /// </summary>
    public void Init()
    {
        // 현재는 빈 저장소로 시작합니다. New Game 시 Reset()을 호출합니다.
    }

    //==============================================================================
    // 공개 API (IProgressService)
    //==============================================================================

    /// <summary>
    /// 전역 플래그 값을 저장합니다.
    /// </summary>
    /// <param name="flagId">플래그 고유 ID입니다.</param>
    /// <param name="value">저장할 bool 값입니다.</param>
    public void SetFlag(string flagId, bool value)
    {
        // 비어 있는 ID는 저장하지 않습니다.
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return;
        }

        _globalFlags[flagId] = value;
    }

    /// <summary>
    /// 전역 플래그 값을 조회합니다.
    /// </summary>
    /// <param name="flagId">플래그 고유 ID입니다.</param>
    /// <returns>저장된 값이 있으면 해당 값, 없으면 false입니다.</returns>
    public bool GetFlag(string flagId)
    {
        // 비어 있는 ID는 조회할 수 없으므로 false를 반환합니다.
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return false;
        }

        return _globalFlags.TryGetValue(flagId, out bool value) && value;
    }

    /// <summary>
    /// 특정 스테이지의 오브젝트 상태를 저장합니다.
    /// </summary>
    /// <param name="stageNumber">스테이지 번호입니다.</param>
    /// <param name="objectId">씬 내 오브젝트 고유 ID입니다.</param>
    /// <param name="stateJson">직렬화된 상태 문자열입니다.</param>
    public void SetObjectState(int stageNumber, string objectId, string stateJson)
    {
        // 유효하지 않은 키나 상태는 저장하지 않습니다.
        if (stageNumber < 0 || string.IsNullOrWhiteSpace(objectId) || string.IsNullOrWhiteSpace(stateJson))
        {
            return;
        }

        ObjectStateKey key = new ObjectStateKey(stageNumber, objectId);
        _objectStates[key] = stateJson;
    }

    /// <summary>
    /// 특정 스테이지의 오브젝트 상태를 조회합니다.
    /// </summary>
    /// <param name="stageNumber">스테이지 번호입니다.</param>
    /// <param name="objectId">씬 내 오브젝트 고유 ID입니다.</param>
    /// <param name="stateJson">조회된 상태 문자열입니다.</param>
    /// <returns>저장된 상태가 있으면 true, 없으면 false입니다.</returns>
    public bool TryGetObjectState(int stageNumber, string objectId, out string stateJson)
    {
        stateJson = null;

        // 유효하지 않은 키는 조회할 수 없습니다.
        if (stageNumber < 0 || string.IsNullOrWhiteSpace(objectId))
        {
            return false;
        }

        ObjectStateKey key = new ObjectStateKey(stageNumber, objectId);
        return _objectStates.TryGetValue(key, out stateJson);
    }

    /// <summary>
    /// 진행 상태를 초기화합니다. (New Game)
    /// </summary>
    public void Reset()
    {
        _globalFlags.Clear();
        _objectStates.Clear();
    }
}
