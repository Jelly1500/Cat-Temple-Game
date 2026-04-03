using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class TutorialMaskUI : MonoBehaviour, ICanvasRaycastFilter
{
    public RectTransform targetUI;

    [Header("Fixed Subtitle UI")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;

    // 애니메이션 코루틴과 원본 크기 추적용 변수
    private Coroutine _pulseCoroutine;
    private Vector3 _originalScale;

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        // 타겟 영역 내부면 레이캐스트 통과(클릭 허용), 외부면 차단
        return !RectTransformUtility.RectangleContainsScreenPoint(targetUI, sp, eventCamera);
    }

    public void SetupMaskAndSubtitle(RectTransform target, string textId, float yPos)
    {
        // 기존 진행 중인 애니메이션이 있다면 중지하고 크기 복구
        ClearHighlightBorder();

        targetUI = target;

        // [핵심] 애니메이션을 시작하기 전 타겟 UI의 원래 스케일 저장
        _originalScale = targetUI.localScale;

        // 1. 타겟 UI 스케일 애니메이션 시작
        _pulseCoroutine = StartCoroutine(CoPulseAnimation());

        // 2. 자막 UI 세팅
        if (!string.IsNullOrEmpty(textId))
        {
            subtitlePanel.SetActive(true);
            subtitleText.text = DataManager.Instance.GetText(textId);

            RectTransform panelRect = subtitlePanel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, yPos);
        }
        else
        {
            subtitlePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 타겟 UI의 크기를 부드럽게 1.0배 ~ 1.2배로 반복 변경하는 코루틴
    /// </summary>
    private IEnumerator CoPulseAnimation()
    {
        float speed = 5f; // 애니메이션 속도
        float maxScaleMultiplier = 1.2f; // 최대 확대 비율

        while (true)
        {
            // Mathf.Sin을 이용하여 0 ~ 1 사이의 부드러운 왕복 값(t) 생성
            // Time.unscaledTime을 사용하여 Time.timeScale = 0 상태에서도 애니메이션 동작 보장
            float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) / 2f;
            float currentMultiplier = Mathf.Lerp(1.0f, maxScaleMultiplier, t);

            // Null 체크 금지 지침에 따라 방어 로직 없이 즉시 스케일 적용
            targetUI.localScale = _originalScale * currentMultiplier;

            yield return null;
        }
    }

    /// <summary>
    /// 다음 스텝으로 넘어가거나 마스크가 해제될 때 애니메이션을 멈추고 크기를 복구합니다.
    /// (메서드명은 TutorialManager 호환을 위해 유지)
    /// </summary>
    public void ClearHighlightBorder()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;

            // 코루틴이 실행 중이었다면 targetUI가 할당된 상태이므로, 방어 로직 없이 원래 스케일로 복구
            targetUI.localScale = _originalScale;
        }
    }
}