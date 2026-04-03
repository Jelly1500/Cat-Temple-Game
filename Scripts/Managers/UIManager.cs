using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UIManager : Singleton<UIManager>
{
    Transform _root;
    Transform Root
    {
        get
        {
            return Utils.GetRootTransform(ref _root, "@UI_Root");
        }
    }

    #region Scene UI
    private UI_Base _sceneUI;
    public UI_Base SceneUI
    {
        get
        {
            foreach (UI_Base ui in FindObjectsByType<UI_Base>(FindObjectsSortMode.None))
            {
                if (ui is IUI_Scene)
                {
                    _sceneUI = ui;
                    break;
                }
            }

            return _sceneUI;
        }
    }

    public void Init()
    {
        // 초기화 작업이 필요한 경우 여기에 추가
    }

    public T ShowSceneUI<T>(string name = null) where T : UI_Base, IUI_Scene
    {
        if (_sceneUI != null)
            return _sceneUI as T;

        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        T sceneUI = FindFirstObjectByType<T>();
        if (sceneUI == null)
        {
            GameObject go = ResourceManager.Instance.Instantiate(name);
            sceneUI = Utils.GetOrAddComponent<T>(go);
        }

        sceneUI.transform.SetParent(Root);
        _sceneUI = sceneUI;

        return sceneUI;
    }
    #endregion

    #region Popup UI
    Transform _popupRoot;
    Transform PopupRoot
    {
        get
        {
            return Utils.GetRootTransform(ref _popupRoot, "@PopupRoot", Root);
        }
    }

    private int _popupOrder = 100;
    private Stack<UI_Base> _popupStack = new Stack<UI_Base>();
    private Dictionary<string, UI_Base> _popups = new Dictionary<string, UI_Base>();

    public T ShowPopupUI<T>(string name = null) where T : UI_Base, IUI_Popup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        // 내부 공통 로직 호출 후 캐스팅하여 반환
        return ShowPopupUIByName(name) as T;
    }

    public UI_Base ShowPopupUIByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("[UIManager] 팝업 이름이 유효하지 않습니다.");
            return null;
        }

        if (_popups.TryGetValue(name, out UI_Base popup) == false)
        {
            GameObject go = ResourceManager.Instance.Instantiate(name);
            if (go == null)
            {
                Debug.LogError($"[UIManager] 팝업 프리팹을 찾을 수 없습니다: {name}");
                return null;
            }

            // [수정] UI_Base를 상속받은 실제 컴포넌트를 찾음
            popup = go.GetComponent<UI_Base>();

            if (popup == null)
            {
                Debug.LogError($"[UIManager] {name} 프리팹에 UI_Base 컴포넌트가 없습니다.");
                ResourceManager.Instance.Destroy(go);
                return null;
            }

            _popups[name] = popup;
            EnsureCanvasGroup(popup.gameObject);
        }

        if (_popupStack.Contains(popup))
        {
            return popup;
        }

        _popupStack.Push(popup);

        popup.transform.SetParent(PopupRoot);
        popup.gameObject.SetActive(true);
        _popupOrder++;

        if (popup is UI_Toolkit toolkitUI)
        {
            toolkitUI.GetComponent<UIDocument>().sortingOrder = _popupOrder;
            toolkitUI.GetComponent<UIDocument>().rootVisualElement.visible = true;
        }
        else
        {
            popup.GetComponent<Canvas>().sortingOrder = _popupOrder;
            EnableCanvasGroup(popup.gameObject, true);
        }

        Time.timeScale = 0f;

        // 팝업이 열릴 때 UI_CatMenu를 숨겨 Update/LateUpdate의 오작동을 원천 차단
        SetCatMenuVisible(false);

        return popup;
    }

    public T GetLastPopupUI<T>() where T : UI_Base
    {
        if (_popupStack.Count == 0)
            return null;

        return _popupStack.Peek() as T;
    }

    public void ClosePopupUI()
    {
        if (_popupStack.Count == 0)
            return;

        UI_Base popup = _popupStack.Pop();

        ClosePopupInternal(popup);
    }

    public void ClosePopupUI(UI_Base targetPopup)
    {
        if (targetPopup == null || _popupStack.Count == 0)
            return;

        // 스택에서 해당 팝업 제거
        Stack<UI_Base> tempStack = new Stack<UI_Base>();
        bool found = false;

        while (_popupStack.Count > 0)
        {
            UI_Base popup = _popupStack.Pop();
            if (popup == targetPopup)
            {
                found = true;
                break;
            }
            tempStack.Push(popup);
        }

        // 임시 스택에 있던 팝업들 다시 원래 스택으로
        while (tempStack.Count > 0)
        {
            _popupStack.Push(tempStack.Pop());
        }

        if (found)
        {
            ClosePopupInternal(targetPopup);
        }
    }

    public void ClosePopupUIByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;

        // 1. 딕셔너리에서 해당 이름의 팝업 인스턴스 검색
        if (_popups.TryGetValue(name, out UI_Base popup))
        {
            // 2. 이미 구현된 인스턴스 기반 닫기 함수 호출
            // (이 함수가 스택 재정렬 및 비활성화를 모두 처리함)
            ClosePopupUI(popup);
        }
        else
        {
            Debug.LogWarning($"[UIManager] 닫으려는 팝업을 찾을 수 없습니다: {name}");
        }
    }

    public UI_Base GetUI(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // 1. 현재 Scene UI인지 확인
        // (SceneUI 프로퍼티는 캐싱된 값이 없으면 찾아서 반환하므로 안전함)
        if (SceneUI != null && SceneUI.GetType().Name == name)
        {
            return SceneUI;
        }

        // 2. 팝업 딕셔너리에서 검색
        // _popups는 이미 생성된 팝업들을 관리하는 Dictionary입니다.
        if (_popups.TryGetValue(name, out UI_Base popup))
        {
            return popup;
        }

        // 찾지 못한 경우
        return null;
    }

    private void ClosePopupInternal(UI_Base popup)
    {
        if (popup is UI_Toolkit toolkitUI)
        {
            toolkitUI.GetComponent<UIDocument>().rootVisualElement.visible = false;
        }
        else
        {
            EnableCanvasGroup(popup.gameObject, false);
            popup.gameObject.SetActive(false);
        }

        _popupOrder--;

        if (_popupStack.Count == 0)
        {
            Time.timeScale = 1f;
            CleanupOrphanedPopups();

            // 모든 팝업이 닫혔을 때 UI_CatMenu를 다시 표시
            SetCatMenuVisible(true);
        }
    }

    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();

        // 모든 팝업이 닫혔으니 시간 정상화
        Time.timeScale = 1f;

        // [신규] 확실하게 모든 팝업 비활성화
        ForceDisableAllPopups();
    }

    public void SwitchPopupUI<T>(string name = null) where T : UI_Base, IUI_Popup
    {
        // 1. 현재 최상단 팝업(여기서는 Warning) 닫기
        ClosePopupUI();

        // 2. 원하는 팝업(여기서는 RecruitmentResult) 열기
        ShowPopupUI<T>(name);
    }
    #endregion
    #region [신규] Raycast 블로킹 관리 헬퍼 메서드

    private void EnsureCanvasGroup(GameObject go)
    {
        if (go.GetComponent<CanvasGroup>() == null)
        {
            go.AddComponent<CanvasGroup>();
        }
    }

    private void EnableCanvasGroup(GameObject go, bool enable)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = enable;
            cg.blocksRaycasts = enable;

            if (!enable)
            {
                cg.alpha = 0f; // 확실하게 투명 처리
            }
            else
            {
                cg.alpha = 1f;
            }
        }
    }

    /// <summary>
    /// 팝업 열림/닫힘 시 UI_CatMenu의 활성 상태를 제어.
    /// UI_CatMenu는 팝업이 하나라도 열려있는 동안 비활성화되어야
    /// Update/LateUpdate의 CloseMenu 오작동을 막을 수 있다.
    /// </summary>
    private void SetCatMenuVisible(bool visible)
    {
        UI_CatMenu catMenu = FindFirstObjectByType<UI_CatMenu>();
        if (catMenu == null) return;

        if (visible)
        {
            // 팝업이 모두 닫혔을 때: CatMenu가 추적 중인 대상이 있을 때만 다시 켬
            catMenu.RestoreIfValid();
        }
        else
        {
            // 팝업이 열릴 때: CatMenu를 즉시 숨김 (SetActive 대신 플래그로 처리)
            catMenu.HideForPopup();
        }
    }

    private void CleanupOrphanedPopups()
    {
        HashSet<UI_Base> stackedPopups = new HashSet<UI_Base>(_popupStack);

        foreach (var kvp in _popups)
        {
            UI_Base popup = kvp.Value;
            if (popup == null) continue;

            // 스택에 없는데 활성화되어 있으면 비활성화
            if (!stackedPopups.Contains(popup) && popup.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[UIManager] 고아 팝업 발견 및 정리: {kvp.Key}");
                EnableCanvasGroup(popup.gameObject, false);
                popup.gameObject.SetActive(false);
            }
        }
    }

    private void ForceDisableAllPopups()
    {
        foreach (var kvp in _popups)
        {
            if (kvp.Value != null)
            {
                EnableCanvasGroup(kvp.Value.gameObject, false);
                kvp.Value.gameObject.SetActive(false);
            }
        }
    }

    public void DebugLogPopupState()
    {
        Debug.Log($"===== UIManager 팝업 상태 =====");
        Debug.Log($"팝업 스택 크기: {_popupStack.Count}");
        Debug.Log($"등록된 팝업 수: {_popups.Count}");

        foreach (var kvp in _popups)
        {
            bool isActive = kvp.Value != null && kvp.Value.gameObject.activeInHierarchy;
            bool inStack = _popupStack.Contains(kvp.Value);

            string status = isActive ? "ACTIVE" : "inactive";
            string stackStatus = inStack ? "IN_STACK" : "NOT_IN_STACK";

            if (isActive && !inStack)
            {
                Debug.LogWarning($"  ⚠️ {kvp.Key}: {status}, {stackStatus} [문제!]");
            }
            else
            {
                Debug.Log($"  - {kvp.Key}: {status}, {stackStatus}");
            }
        }
        Debug.Log($"================================");
    }

    #endregion

    public void RefreshPopupUI<T>() where T : UI_Base, IUI_Popup
    {
        foreach (var popup in _popups)
        {
            if (popup is T targetPopup)
            {
                targetPopup.RefreshUI();
                break;
            }
        }
    }

    public void RefreshAllActiveUI()
    {
        // [수정] _sceneUI 대신 SceneUI 프로퍼티를 사용하여, 연결이 안 되어 있다면 찾아서라도 갱신
        if (SceneUI != null)
            SceneUI.RefreshUI();

        // 2. 스택에 있는 팝업들 중 활성화된 것 갱신
        foreach (var popup in _popupStack)
        {
            if (popup != null && popup.gameObject.activeSelf)
                popup.RefreshUI();
        }

    }

    public T ShowUI<T>(string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = ResourceManager.Instance.Instantiate(name);
        go.transform.SetParent(PopupRoot, false);
        go.transform.SetAsLastSibling();

        return go.GetOrAddComponent<T>();
    }

    #region [신규] Scene UI 터치 제어 (튜토리얼용)

    /// <summary>
    /// 메인 게임 UI(SceneUI)의 모든 버튼 및 터치 상호작용을 활성화/비활성화합니다.
    /// </summary>
    public void SetSceneUIInteractable(bool isInteractable)
    {
        // Null 체크 로직 추가 금지 지침에 따라 SceneUI에 직접 접근하여 에러 노출 유도
        CanvasGroup cg = SceneUI.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = SceneUI.gameObject.AddComponent<CanvasGroup>();
        }

        cg.interactable = isInteractable;
        cg.blocksRaycasts = isInteractable;
    }

    #endregion

    public void Clear()
    {
        CloseAllPopupUI();
        _popups.Clear();

        Root.DestroyChildren();

        _sceneUI = null;
    }

    public void ShowGameToast(string textKey, params object[] args)
    {
        // 1. 현재 씬 UI가 UI_MainGame인지 확인
        if (SceneUI is UI_MainGame mainGameUI)
        {
            // 2. 텍스트 데이터 로드
            string format = DataManager.Instance.GetText(textKey);
            if (string.IsNullOrEmpty(format)) return;

            // 3. 문자열 포맷팅 (args가 없으면 그냥 출력)
            string message = (args != null && args.Length > 0)
                             ? string.Format(format, args)
                             : format;

            // 4. UI 표시 요청
            mainGameUI.ShowToastMessage(message);
        }
    }

    #region Exit Popup Control

    /// <summary>
    /// 게임 종료 확인 팝업 출력
    /// </summary>
    public void ShowExitPopup()
    {
        // 팝업 프리팹 이름이 "UI_ExitPopup"이라고 가정
        ShowPopupUI<UI_ExitPopup>("UI_ExitPopup");
    }

    /// <summary>
    /// 게임 종료 확인 팝업 닫기
    /// </summary>
    public void CloseExitPopup()
    {
        ClosePopupUIByName("UI_ExitPopup");
    }

    #endregion
}