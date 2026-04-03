using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 카메라 드래그를 막고 있는 UI 요소를 실시간으로 찾아주는 디버그 도구
/// 게임 실행 중 아무 오브젝트에 붙이면 동작합니다.
/// </summary>
public class UIBlockingDebugger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _enableDebug = true;
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    [SerializeField] private bool _showOnScreenLog = true;

    [Header("Runtime Info (Read Only)")]
    [SerializeField] private string _currentBlockingUI = "None";
    [SerializeField] private int _activeRaycastTargetCount = 0;

    private List<RaycastResult> _raycastResults = new List<RaycastResult>();
    private PointerEventData _pointerEventData;

    // 화면 표시용
    private string _onScreenMessage = "";
    private float _messageTimer = 0f;

    private void Start()
    {
        _pointerEventData = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _enableDebug = !_enableDebug;
            Debug.Log($"[UIBlockingDebugger] Debug mode: {(_enableDebug ? "ON" : "OFF")}");
        }

        if (!_enableDebug) return;

        // 매 프레임 체크
        CheckBlockingUI();

        // 클릭/터치 시 상세 로그
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began))
        {
            LogDetailedBlockingInfo();
        }

        // 메시지 타이머
        if (_messageTimer > 0)
            _messageTimer -= Time.unscaledDeltaTime;
    }

    private void CheckBlockingUI()
    {
        if (EventSystem.current == null) return;

        Vector2 screenPos = Input.mousePosition;
        if (Input.touchCount > 0)
            screenPos = Input.GetTouch(0).position;

        _pointerEventData.position = screenPos;
        _raycastResults.Clear();

        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

        _activeRaycastTargetCount = _raycastResults.Count;

        if (_raycastResults.Count > 0)
        {
            // 가장 위에 있는 UI 요소
            var topResult = _raycastResults[0];
            _currentBlockingUI = GetFullPath(topResult.gameObject);
        }
        else
        {
            _currentBlockingUI = "None";
        }
    }

    private void LogDetailedBlockingInfo()
    {
        if (EventSystem.current == null)
        {
            Debug.Log("[UIBlockingDebugger] EventSystem이 없습니다.");
            return;
        }

        Vector2 screenPos = Input.mousePosition;
        if (Input.touchCount > 0)
            screenPos = Input.GetTouch(0).position;

        _pointerEventData.position = screenPos;
        _raycastResults.Clear();

        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

        Debug.Log($"========== [UIBlockingDebugger] 클릭 위치 분석 ==========");
        Debug.Log($"화면 좌표: {screenPos}");
        Debug.Log($"감지된 UI 요소 수: {_raycastResults.Count}");

        if (_raycastResults.Count == 0)
        {
            Debug.Log("→ UI가 없습니다. 카메라 드래그가 작동해야 합니다.");
            _onScreenMessage = "UI 없음 - 드래그 가능";
        }
        else
        {
            Debug.Log("→ [경고] 다음 UI들이 입력을 가로채고 있습니다:");
            _onScreenMessage = $"블로킹 UI {_raycastResults.Count}개 발견!";

            for (int i = 0; i < _raycastResults.Count; i++)
            {
                var result = _raycastResults[i];
                GameObject go = result.gameObject;

                // 상세 정보 수집
                string path = GetFullPath(go);
                bool isActive = go.activeInHierarchy;

                // Graphic 컴포넌트 정보
                Graphic graphic = go.GetComponent<Graphic>();
                string graphicInfo = "No Graphic";
                if (graphic != null)
                {
                    graphicInfo = $"raycastTarget={graphic.raycastTarget}, color.a={graphic.color.a:F2}";
                }

                // CanvasGroup 정보
                CanvasGroup canvasGroup = go.GetComponentInParent<CanvasGroup>();
                string canvasGroupInfo = "No CanvasGroup";
                if (canvasGroup != null)
                {
                    canvasGroupInfo = $"blocksRaycasts={canvasGroup.blocksRaycasts}, alpha={canvasGroup.alpha:F2}";
                }

                Debug.Log($"  [{i}] {path}");
                Debug.Log($"      Active: {isActive}, {graphicInfo}");
                Debug.Log($"      CanvasGroup: {canvasGroupInfo}");

                // 문제가 될 수 있는 UI 강조
                if (graphic != null && graphic.raycastTarget && graphic.color.a < 0.01f)
                {
                    Debug.LogWarning($"  ⚠️ [{i}] 투명하지만 raycastTarget이 켜져 있음! → {path}");
                }

                if (canvasGroup != null && canvasGroup.blocksRaycasts && canvasGroup.alpha < 0.01f)
                {
                    Debug.LogWarning($"  ⚠️ [{i}] CanvasGroup이 투명하지만 blocksRaycasts가 켜져 있음! → {path}");
                }
            }
        }

        Debug.Log("==========================================================");
        _messageTimer = 3f;
    }

    /// <summary>
    /// 모든 활성화된 raycastTarget UI를 검사하여 문제가 될 수 있는 것들을 찾습니다.
    /// </summary>
    [ContextMenu("Scan All Suspicious UI")]
    public void ScanAllSuspiciousUI()
    {
        Debug.Log("========== [UIBlockingDebugger] 전체 UI 스캔 ==========");

        // 모든 Graphic 컴포넌트 검사
        Graphic[] allGraphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        int suspiciousCount = 0;

        foreach (var graphic in allGraphics)
        {
            if (!graphic.raycastTarget) continue;
            if (!graphic.gameObject.activeInHierarchy) continue;

            bool isSuspicious = false;
            string reason = "";

            // 투명한 Image/RawImage
            if (graphic.color.a < 0.01f)
            {
                isSuspicious = true;
                reason = "투명(alpha=0)이지만 raycastTarget이 켜져 있음";
            }

            // 크기가 화면 전체를 덮는 경우
            RectTransform rt = graphic.rectTransform;
            if (rt.rect.width > Screen.width * 0.8f && rt.rect.height > Screen.height * 0.8f)
            {
                if (graphic.color.a < 0.1f)
                {
                    isSuspicious = true;
                    reason = "화면 전체를 덮는 투명 UI";
                }
            }

            if (isSuspicious)
            {
                suspiciousCount++;
                Debug.LogWarning($"⚠️ 의심 UI: {GetFullPath(graphic.gameObject)}");
                Debug.LogWarning($"   이유: {reason}");
                Debug.LogWarning($"   Alpha: {graphic.color.a:F2}, Size: {rt.rect.size}");
            }
        }

        // 모든 CanvasGroup 검사
        CanvasGroup[] allCanvasGroups = FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None);
        foreach (var cg in allCanvasGroups)
        {
            if (!cg.blocksRaycasts) continue;
            if (!cg.gameObject.activeInHierarchy) continue;

            if (cg.alpha < 0.01f)
            {
                suspiciousCount++;
                Debug.LogWarning($"⚠️ 의심 CanvasGroup: {GetFullPath(cg.gameObject)}");
                Debug.LogWarning($"   Alpha=0이지만 blocksRaycasts가 켜져 있음");
            }
        }

        Debug.Log($"스캔 완료: 의심스러운 UI {suspiciousCount}개 발견");
        Debug.Log("==========================================================");
    }

    /// <summary>
    /// 팝업 스택 상태를 확인합니다.
    /// </summary>
    [ContextMenu("Check Popup Stack")]
    public void CheckPopupStack()
    {
        Debug.Log("========== [UIBlockingDebugger] 팝업 스택 확인 ==========");

        if (UIManager.Instance == null)
        {
            Debug.Log("UIManager.Instance가 없습니다.");
            return;
        }

        // UIManager의 _popups Dictionary 확인 (리플렉션 사용)
        var popupsField = typeof(UIManager).GetField("_popups",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (popupsField != null)
        {
            var popups = popupsField.GetValue(UIManager.Instance) as Dictionary<string, UI_Base>;
            if (popups != null)
            {
                Debug.Log($"등록된 팝업 수: {popups.Count}");
                foreach (var kvp in popups)
                {
                    bool isActive = kvp.Value != null && kvp.Value.gameObject.activeInHierarchy;
                    Debug.Log($"  - {kvp.Key}: {(isActive ? "ACTIVE ⚠️" : "inactive")}");

                    // 활성화된 팝업의 CanvasGroup 확인
                    if (isActive && kvp.Value != null)
                    {
                        var cg = kvp.Value.GetComponent<CanvasGroup>();
                        if (cg != null)
                        {
                            Debug.Log($"    CanvasGroup: alpha={cg.alpha:F2}, blocksRaycasts={cg.blocksRaycasts}");
                        }
                    }
                }
            }
        }

        // 스택 확인
        var stackField = typeof(UIManager).GetField("_popupStack",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (stackField != null)
        {
            var stack = stackField.GetValue(UIManager.Instance) as Stack<UI_Base>;
            if (stack != null)
            {
                Debug.Log($"팝업 스택 크기: {stack.Count}");
            }
        }

        Debug.Log("==========================================================");
    }

    private string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private void OnGUI()
    {
        if (!_enableDebug || !_showOnScreenLog) return;

        // 화면 좌상단에 현재 상태 표시
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        string status = _activeRaycastTargetCount > 0
            ? $"<color=red>UI 블로킹: {_activeRaycastTargetCount}개</color>"
            : "<color=green>드래그 가능</color>";

        GUI.Box(new Rect(10, 10, 400, 80),
            $"[UI Debugger] F1로 토글\n{status}\n현재: {_currentBlockingUI}",
            style);

        // 클릭 시 메시지 표시
        if (_messageTimer > 0)
        {
            GUI.Box(new Rect(10, 100, 400, 30), _onScreenMessage, style);
        }
    }
}