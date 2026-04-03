using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // [추가] New Input System
using Unity.Mathematics; // [추가] DOTS 권장 수학 라이브러리

/// <summary>
/// 모바일 터치 및 PC 클릭을 처리하는 입력 핸들러
/// 씬에 하나만 존재해야 함 (게임 씬에 빈 오브젝트로 추가)
/// </summary>
public class TouchInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UI_CatMenu _catMenu;
    [SerializeField] private Camera _mainCamera;

    [Header("Settings")]
    [SerializeField] private LayerMask _discipleLayerMask = -1;

    [Header("Camera Settings")]
    [SerializeField] private bool _enableCameraFollow = true;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLog = false;

    //void Awake()
    //{
    //    #if UNITY_EDITOR
    //            UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
    //    #endif
    //}

    private void Start()
    {
        // Null 체크 금지 지침에 따라 방어적 로직 제거
        // Inspector에 할당되지 않았을 경우에만 씬에서 자동으로 찾아옵니다.
        if (!_mainCamera) _mainCamera = Camera.main;
        if (!_catMenu) _catMenu = FindFirstObjectByType<UI_CatMenu>();
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        // [핵심] New Input System 통합 감지 (PC 클릭 및 모바일 터치 모두 포괄)
        // Pointer.current에 대한 Null 체크는 지침에 따라 의도적으로 배제했습니다.
        if (Pointer.current.press.wasPressedThisFrame)
        {
            HandleTouch();
        }
    }

    private void HandleTouch()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked)
        {
            if (_showDebugLog)
                Debug.Log("[TouchInputHandler] Touch blocked by TutorialManager (Input Blocked)");
            return;
        }
        // UI 위를 터치한 경우 무시
        if (EventSystem.current.IsPointerOverGameObject())
        {
            if (_showDebugLog)
                Debug.Log("[TouchInputHandler] Touch blocked by UI");
            return;
        }

        // Pointer 위치를 스크린 좌표로 읽어 월드 좌표로 변환
        Vector2 touchScreenPos = Pointer.current.position.ReadValue();
        Vector2 worldPos = _mainCamera.ScreenToWorldPoint(touchScreenPos);

        if (_showDebugLog)
            Debug.Log($"[TouchInputHandler] Touch at world pos: {worldPos}");

        // Mathf.Infinity 대신 math.INFINITY 사용
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, math.INFINITY, _discipleLayerMask);

        // 레이캐스트 충돌 여부 확인 로직
        if (hit.collider)
        {
            if (_showDebugLog)
                Debug.Log($"[TouchInputHandler] Hit: {hit.collider.name}");

            Disciple disciple = hit.collider.GetComponent<Disciple>();
            if (!disciple)
            {
                disciple = hit.collider.GetComponentInParent<Disciple>();
            }

            // 고양이를 찾은 경우 터치 로직 실행
            if (disciple)
            {
                OnDiscipleTouched(disciple);
                return;
            }
        }

        // 빈 공간 터치 시 메뉴 닫기 (Null 방어 로직 제거됨)
        if (_catMenu.gameObject.activeSelf)
        {
            if (_showDebugLog)
                Debug.Log("[TouchInputHandler] Closing menu (empty space touch)");

            _catMenu.CloseMenu();
        }
    }

    private void OnDiscipleTouched(Disciple disciple)
    {
        if (_showDebugLog)
            Debug.Log($"[TouchInputHandler] Disciple touched: {disciple.name}");

        // [추가] 기존 Disciple.cs의 OnMouseDown()에 있던 튜토리얼 이벤트 호출 이관
        EventManager.Instance.TriggerEvent(Define.EEventType.DiscipleTouched);

        // 카메라 추적 (Null 체크 제거됨)
        if (_enableCameraFollow)
        {
            CameraController.Instance.SetFollowTarget(disciple.transform);
        }

        // UI 메뉴 열기 (Null 체크 제거됨)
        _catMenu.ShowMenu(disciple.transform);
    }

    /// <summary>
    /// 외부에서 호출 가능한 제자 선택 메서드
    /// (UI_MainGame의 리스트 슬롯 클릭 등에서 사용)
    /// </summary>
    public void SelectDisciple(Disciple disciple)
    {
        OnDiscipleTouched(disciple);
    }
}