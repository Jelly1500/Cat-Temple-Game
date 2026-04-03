using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UI_ExplainHighlight : UI_UGUI
{
    [Header("Highlight Settings")]
    public RectTransform highlightBorder;
    public float margin = 20f;

    [Header("Subtitle UI")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;

    [Header("Interaction")]
    public Button backgroundButton;

    private RectTransform _targetUI;
    private Action _onNextAction;

    public void SetupHighlight(RectTransform target, string textId, float yPos, Action onNext)
    {
        _targetUI = target;
        _onNextAction = onNext;

        // Canvas 렌더링 순위 상승
        Canvas canvas = GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 9999;

        // [중요] 레이캐스트(터치) 판정을 위한 GraphicRaycaster 강제 활성화 
        // 널 체크 금지 지침에 따라 에디터에 컴포넌트가 없다면 즉시 에러 발생
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        raycaster.enabled = true;

        subtitlePanel.SetActive(true);
        subtitleText.text = DataManager.Instance.GetText(textId);

        RectTransform panelRect = subtitlePanel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, yPos);

        backgroundButton.onClick.RemoveAllListeners();
        backgroundButton.onClick.AddListener(OnClickBackground);

        // 하이라이트 테두리의 피벗을 항상 정중앙(0.5, 0.5)으로 고정
        highlightBorder.pivot = new Vector2(0.5f, 0.5f);

        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        // 1. 타겟 UI의 실제 렌더링되는 월드 코너(4개 모서리) 좌표 추출
        Vector3[] corners = new Vector3[4];
        _targetUI.GetWorldCorners(corners);

        // 2. 좌측 하단(corners[0])과 우측 상단(corners[2])의 중간값을 통해 정확한 월드 중심점 계산
        Vector3 worldCenter = (corners[0] + corners[2]) / 2f;

        // 3. 중심점 및 회전값 동기화
        highlightBorder.position = worldCenter;
        highlightBorder.rotation = _targetUI.rotation;

        // 4. 스케일 왜곡 방지를 위한 상대적 로컬 스케일 계산
        Vector3 targetLossy = _targetUI.lossyScale;
        Vector3 parentLossy = highlightBorder.parent.GetComponent<RectTransform>().lossyScale;

        highlightBorder.localScale = new Vector3(
            targetLossy.x / parentLossy.x,
            targetLossy.y / parentLossy.y,
            targetLossy.z / parentLossy.z
        );

        // 5. 피벗이 중앙이므로 테두리 크기에 여백(margin)만 더해주면 사방으로 균일하게 확장됨
        highlightBorder.sizeDelta = new Vector2(_targetUI.rect.width + margin, _targetUI.rect.height + margin);
    }

    private void OnClickBackground()
    {
        _onNextAction?.Invoke();
    }

    public void ClearHighlight()
    {
        gameObject.SetActive(false);
    }
}