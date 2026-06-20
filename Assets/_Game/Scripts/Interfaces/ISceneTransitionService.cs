using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

//==============================================================================
// 기능: interface::ISceneTransitionService
// 설명: 씬 전환 시 시각적 연출(페이드, 로딩 UI)에 대한 외부 공개 계약
//==============================================================================
// NOTE: StageFlowController 등 외부에서는 SceneTransitionPresenter 구현체를 직접 참조하지 않고
//       본 인터페이스를 통해서만 연출 기능에 접근합니다.
//==============================================================================

/// <summary>
/// 씬 전환 시 화면 페이드 및 로딩 연출을 담당하는 서비스의 공개 계약입니다.
/// </summary>
public interface ISceneTransitionService
{
    /// <summary>
    /// 페이드 캔버스와 로딩 UI를 초기 숨김/투명 상태로 정렬합니다.
    /// </summary>
    void Init();

    /// <summary>
    /// 화면을 암전시킵니다. (CanvasGroup alpha 0 → 1, 로딩 UI는 비활성 상태 유지)
    /// </summary>
    /// <param name="token">취소 토큰입니다.</param>
    /// <returns>페이드 인 완료를 await 할 수 있는 UniTask입니다.</returns>
    UniTask FadeInAsync(CancellationToken token);

    /// <summary>
    /// 비동기 씬 작업(로드/언로드)을 모니터링하며, 경과 시간에 따라 로딩 UI를 단계적으로 노출합니다.
    /// </summary>
    /// <param name="asyncOp">모니터링할 Unity 비동기 작업입니다.</param>
    /// <param name="token">취소 토큰입니다.</param>
    /// <returns>작업 완료 및 로딩 UI 정리를 await 할 수 있는 UniTask입니다.</returns>
    UniTask MonitorTransitionAsync(AsyncOperation asyncOp, CancellationToken token);

    /// <summary>
    /// 화면을 복귀시킵니다. (CanvasGroup alpha 1 → 0)
    /// </summary>
    /// <param name="token">취소 토큰입니다.</param>
    /// <returns>페이드 아웃 완료를 await 할 수 있는 UniTask입니다.</returns>
    UniTask FadeOutAsync(CancellationToken token);
}
