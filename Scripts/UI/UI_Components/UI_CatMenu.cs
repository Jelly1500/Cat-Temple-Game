using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // [추가] New Input System 네임스페이스

public class UI_CatMenu : UI_UGUI
{
    enum Buttons
    {
        Btn_Info,
        Btn_Train,
        Btn_Leave,
        Btn_CloseBackground
    }

    private DiscipleData _currentCatData;
    private Transform _targetCatTransform;
    private Camera _mainCamera;

    private bool _openedThisFrame = false;

    // 팝업으로 인해 임시로 숨겨진 상태인지 여부
    // 이 상태에서는 gameObject는 비활성이지만 _targetCatTransform은 유지됨
    private bool _hiddenForPopup = false;

    [SerializeField] private Vector3 _worldOffset = new Vector3(0, 0, 0);

    [Header("Camera Settings")]
    [SerializeField] private bool _stopCameraFollowOnClose = true;

    [SerializeField] private RectTransform _menuButtonsParent;

    protected override void Awake()
    {
        base.Awake();
        _mainCamera = Camera.main;
    }

    protected override void Start()
    {
        base.Start();
        Init();
    }

    public override void Init()
    {
        if (_init) return;
        base.Init();

        BindButtons(typeof(Buttons));

        GetButton((int)Buttons.Btn_Info).onClick.AddListener(OnInfoClicked);
        GetButton((int)Buttons.Btn_Train).onClick.AddListener(OnTrainClicked);
        GetButton((int)Buttons.Btn_Leave).onClick.AddListener(OnLeaveClicked);
        GetButton((int)Buttons.Btn_CloseBackground).onClick.AddListener(OnCloseBackgroundClicked);

        var bgButton = GetButton((int)Buttons.Btn_CloseBackground);
        if (bgButton != null)
        {
            var bgRt = bgButton.GetComponent<RectTransform>();
            if (bgRt != null)
            {
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.anchoredPosition = Vector2.zero;
                bgRt.sizeDelta = Vector2.zero;

                bgButton.transform.SetAsFirstSibling();
            }
            var img = bgButton.GetComponent<Image>();
            if (img == null) img = bgButton.gameObject.AddComponent<Image>();

            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
        }

        if (_menuButtonsParent != null)
        {
            _menuButtonsParent.SetAsLastSibling();
        }
        else
        {
            GetButton((int)Buttons.Btn_Info)?.transform.SetAsLastSibling();
            GetButton((int)Buttons.Btn_Train)?.transform.SetAsLastSibling();
            GetButton((int)Buttons.Btn_Leave)?.transform.SetAsLastSibling();
        }

        gameObject.SetActive(false);
    }

    private void OnCloseBackgroundClicked()
    {
        if (TutorialManager.Instance.IsInputBlocked)
        {
            return;
        }

        CloseMenu();
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (_openedThisFrame)
        {
            _openedThisFrame = false;
            return;
        }

        if (TutorialManager.Instance.IsInputBlocked)
        {
            return;
        }

        if (Pointer.current.press.wasPressedThisFrame)
        {
            if (!IsPointerOverUI())
            {
                CloseMenu();
            }
        }
    }

    private void LateUpdate()
    {
        if (_targetCatTransform == null || gameObject.activeSelf == false)
        {
            if (gameObject.activeSelf) CloseMenu();
            return;
        }

        if (_mainCamera != null)
        {
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(_targetCatTransform.position + _worldOffset);
            transform.position = screenPos;
        }
    }

    // [수정됨] 마우스 및 모바일 터치 ID를 통합하여 UI 클릭 여부 판별
    private bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void ShowMenu(Transform targetCat)
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        _targetCatTransform = targetCat;

        var disciple = targetCat.GetComponent<Disciple>();
        if (disciple != null)
        {
            _currentCatData = disciple.Data;
        }
        else
        {
            _currentCatData = null;
            Debug.LogWarning("UI_CatMenu: Target has no Disciple component.");
        }

        _openedThisFrame = true;
        gameObject.SetActive(true);
        LateUpdate();
    }

    public void CloseMenu()
    {
        if (_stopCameraFollowOnClose && CameraController.Instance != null)
        {
            CameraController.Instance.StopFollowing();
        }

        _targetCatTransform = null;
        _currentCatData = null;
        _hiddenForPopup = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 팝업이 열릴 때 UIManager가 호출.
    /// _targetCatTransform은 유지한 채 gameObject만 비활성화.
    /// </summary>
    public void HideForPopup()
    {
        if (!gameObject.activeSelf) return;
        _hiddenForPopup = true;
        //gameObject.SetActive(false);
    }

    /// <summary>
    /// 모든 팝업이 닫힐 때 UIManager가 호출.
    /// 추적 대상이 살아있으면 메뉴를 복구하고, 사라졌으면 완전히 닫음.
    /// </summary>
    public void RestoreIfValid()
    {
        if (!_hiddenForPopup) return;
        _hiddenForPopup = false;

        if (_targetCatTransform == null)
        {
            // 팝업이 열려있는 사이 고양이가 사라진 경우
            CloseMenu();
            return;
        }

        _openedThisFrame = true; // 복구 직후 프레임의 오입력 방지
        gameObject.SetActive(true);
    }

    private void OnInfoClicked()
    {
        if (_currentCatData == null) return;
        var popup = UIManager.Instance.ShowPopupUI<CatInfoPopup>();
        popup.Setup(_currentCatData);
    }

    private void OnTrainClicked()
    {
        if (_currentCatData == null) return;
        var popup = UIManager.Instance.ShowPopupUI<CatTrainingPopup>();
        popup.Setup(_currentCatData);
    }

    private void OnLeaveClicked()
    {
        if (_currentCatData == null) return;
        var popup = UIManager.Instance.ShowPopupUI<CatDeparturePopup>();
        popup.Setup(_currentCatData);
    }
}