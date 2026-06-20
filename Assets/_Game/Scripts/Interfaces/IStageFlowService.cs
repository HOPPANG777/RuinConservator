using Cysharp.Threading.Tasks;

//==============================================================================
// 기능: interface::IStageFlowService
// 설명: 씬/스테이지 전환 흐름에 대한 외부 공개 계약(Contract)
//==============================================================================
// NOTE: 외부(GameRoot, 타 클래스)는 StageFlowController 구현체를 직접 참조하지 않고
//       반드시 본 인터페이스(GameRoot.Instance.StageFlow)를 통해서만 접근합니다.
//==============================================================================

/// <summary>
/// 스테이지(씬) 전환 흐름을 제어하는 서비스의 공개 계약입니다.
/// </summary>
public interface IStageFlowService
{
    /// <summary>현재 스테이지 전환(페이드/로드)이 진행 중인지 여부입니다.</summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// 지정한 스테이지 번호로 전환합니다. (페이드 인 → Capture → 이전 씬 언로드 → 새 씬 Additive 로드 → Restore → 페이드 아웃)
    /// </summary>
    /// <param name="stageNumber">씬 카탈로그에 등록된 스테이지 번호입니다.</param>
    /// <param name="data">씬 간 보존/전달할 데이터입니다. (스폰 지점, 퍼즐 진행 등)</param>
    /// <returns>전환 파이프라인 완료를 await 할 수 있는 UniTask입니다.</returns>
    UniTask ChangeStageAsync(int stageNumber, SceneTransferData data);

    /// <summary>
    /// 등록된 씬 ID를 기준으로 일반 씬(타이틀/로비/엔딩 등)으로 전환합니다.
    /// </summary>
    /// <param name="sceneId">씬 카탈로그에 등록된 안정적인 씬 ID입니다. (예: "TITLE")</param>
    /// <param name="data">씬 간 보존/전달할 데이터입니다. 필요 없으면 null을 전달합니다.</param>
    /// <returns>전환 파이프라인 완료를 await 할 수 있는 UniTask입니다.</returns>
    UniTask ChangeSceneAsync(string sceneId, SceneTransferData data);
}
