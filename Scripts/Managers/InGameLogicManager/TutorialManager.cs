using UnityEngine;
using UnityEngine.UI; // Button 사용을 위해 추가

public class TutorialManager : Singleton<TutorialManager>
{
    public bool IsTutorialActive { get; private set; }
    public bool IsInputBlocked { get; set; } = false;
    private TutorialMaskUI maskUI;

    // 현재 구독 중인 버튼을 기억하기 위한 캐싱 변수
    private Button _currentTargetButton;

    public void Init()
    {
        maskUI = FindFirstObjectByType<TutorialMaskUI>(FindObjectsInactive.Include);
    }

    public void StartTutorialStep(UnityEngine.UI.Button targetButton, RectTransform targetRect, string textId, float subtitleYPos)
    {
        IsTutorialActive = true;
        _currentTargetButton = targetButton;

        DialogueManager.Instance.CloseDialogueUI();
        Time.timeScale = 1f;

        maskUI.gameObject.SetActive(true);

        // [수정] yPos 값 전달
        maskUI.SetupMaskAndSubtitle(targetRect, textId, subtitleYPos);

        _currentTargetButton.onClick.AddListener(OnTargetButtonClicked);

        Debug.Log($"[TutorialManager] 튜토리얼 대기 시작. 타겟: {targetRect.name}");
    }

    private void OnTargetButtonClicked()
    {
        if (!IsTutorialActive) return;

        IsTutorialActive = false;

        // [추가] 마스크 비활성화 전 테두리 객체 정리
        maskUI.ClearHighlightBorder();
        maskUI.gameObject.SetActive(false);

        // 일회성 실행을 위해 연결했던 이벤트 해제
        _currentTargetButton.onClick.RemoveListener(OnTargetButtonClicked);
        _currentTargetButton = null;

        Debug.Log("[TutorialManager] 타겟 버튼 클릭됨. 튜토리얼 대기 종료.");

        DialogueManager.Instance.CurrentEvent.SetDialogueActionFinished();
    }
}