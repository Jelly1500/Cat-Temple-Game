using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Unity.Mathematics; // [추가] DOTS 호환 권장 수학 라이브러리

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private BoxCollider2D _mapBounds;
    [SerializeField] private float _dragSpeed = 1f;

    [Header("Follow Settings")]
    [SerializeField] private float _followSmoothTime = 0.2f;
    [SerializeField] private float _zoomSmoothTime = 5f;
    [SerializeField] private float _zoomedOutSize = 5f;
    [SerializeField] private float _zoomedInSize = 2.5f;

    [Header("Pinch Zoom Settings")]
    [SerializeField] private float _pinchZoomSpeed = 0.01f;
    [SerializeField] private float _mouseScrollZoomSpeed = 0.5f;
    [SerializeField] private float _minZoomSize = 2f;
    [SerializeField] private float _maxZoomSize = 8f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLog = false;

    private Camera _cam;
    private Vector3 _dragOrigin;
    private bool _isDragging = false;

    private Transform _followTarget;
    private Vector3 _currentVelocity;

    private float _targetZoomSize;
    private bool _isManualZooming = false;

    private float _initialPinchDistance;
    private float _initialZoomSize;
    private bool _isPinching = false;

    private bool _ignoreDragUntilRelease = false;

    private InputAction _pressAction;
    private InputAction _positionAction;

    private float _minX, _maxX, _minY, _maxY;
    private float _camHeight, _camWidth;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _cam = GetComponent<Camera>();

        _zoomedOutSize = _cam.orthographicSize;
        _zoomedInSize = _zoomedOutSize * 0.5f;
        _targetZoomSize = _cam.orthographicSize;

        // [수정됨] math 라이브러리 사용
        _minZoomSize = math.min(_minZoomSize, _zoomedInSize);
        _maxZoomSize = math.max(_maxZoomSize, _zoomedOutSize);

        _pressAction = new InputAction(type: InputActionType.Button);
        _pressAction.AddBinding("<Mouse>/leftButton");
        _pressAction.AddBinding("<Touchscreen>/press");

        _positionAction = new InputAction(type: InputActionType.Value, expectedControlType: "Vector2");
        _positionAction.AddBinding("<Mouse>/position");
        _positionAction.AddBinding("<Touchscreen>/position");

        // [수정됨] Null 체크 지침에 따라 제거됨
        UpdateBounds();
    }

    private void OnEnable()
    {
        _pressAction.Enable();
        _positionAction.Enable();
    }

    private void OnDisable()
    {
        _pressAction.Disable();
        _positionAction.Disable();
    }

    private void LateUpdate()
    {
        HandlePinchZoom();
        HandleMouseScrollZoom();

        if (!_isPinching)
        {
            HandleInput();
        }

        if (_followTarget != null) // 타겟 추적 모드 판별을 위한 논리적 검사이므로 유지
        {
            FollowLogic();

            if (!_isManualZooming)
            {
                ZoomLogic(true);
            }
            else
            {
                ApplyManualZoom();
            }
        }
        else
        {
            if (_isManualZooming)
            {
                ApplyManualZoom();
            }
            else
            {
                ZoomLogic(false);
            }
        }

        // [수정됨] Null 체크 지침에 따라 제거됨
        ClampCameraPosition();
    }

    #region Pinch Zoom (Mobile)
    private void HandlePinchZoom()
    {
        // [수정됨] Null 체크 제거 및 New Input System 터치 처리
        var touch0 = Touchscreen.current.touches[0];
        var touch1 = Touchscreen.current.touches[1];

        // 터치가 2개 이상 눌리지 않았다면 핀치 줌 로직 종료
        if (!touch0.press.isPressed || !touch1.press.isPressed)
        {
            if (_isPinching)
            {
                _isPinching = false;
            }
            return;
        }

        // [수정됨] 모호한 참조 방지를 위해 명시적 네임스페이스 기입
        var phase0 = touch0.phase.ReadValue();
        var phase1 = touch1.phase.ReadValue();

        if (phase0 == UnityEngine.InputSystem.TouchPhase.Began || phase1 == UnityEngine.InputSystem.TouchPhase.Began)
        {
            _isPinching = true;
            _isDragging = false;
            _initialPinchDistance = math.distance(touch0.position.ReadValue(), touch1.position.ReadValue());
            _initialZoomSize = _cam.orthographicSize;

            StopFollowing();
            _isManualZooming = true;
            _targetZoomSize = _cam.orthographicSize;
        }

        if (_isPinching && (phase0 == UnityEngine.InputSystem.TouchPhase.Moved || phase1 == UnityEngine.InputSystem.TouchPhase.Moved))
        {
            float currentPinchDistance = math.distance(touch0.position.ReadValue(), touch1.position.ReadValue());

            if (_initialPinchDistance > 0)
            {
                float ratio = _initialPinchDistance / currentPinchDistance;
                float newSize = _initialZoomSize * ratio;

                _targetZoomSize = math.clamp(newSize, _minZoomSize, _maxZoomSize);
            }
        }

        if (phase0 == UnityEngine.InputSystem.TouchPhase.Ended || phase1 == UnityEngine.InputSystem.TouchPhase.Ended)
        {
            _isPinching = false;
        }
    }
    #endregion

    #region Mouse Scroll Zoom (PC)
    private void HandleMouseScrollZoom()
    {
        // [수정됨] Null 체크 제거 및 New Input System 스크롤 처리
        float scrollDelta = Mouse.current.scroll.ReadValue().y;

        if (math.abs(scrollDelta) > 0.01f)
        {
            if (IsPointerOverUI())
                return;

            StopFollowing();
            _isManualZooming = true;

            // Input System의 스크롤 값은 크기 때문에 부호(Sign)만 사용하여 속도 조절
            _targetZoomSize -= math.sign(scrollDelta) * _mouseScrollZoomSpeed;
            _targetZoomSize = math.clamp(_targetZoomSize, _minZoomSize, _maxZoomSize);
        }
    }
    #endregion

    #region Manual Zoom Apply
    private void ApplyManualZoom()
    {
        if (math.abs(_cam.orthographicSize - _targetZoomSize) < 0.01f)
        {
            _cam.orthographicSize = _targetZoomSize;
            return;
        }

        _cam.orthographicSize = math.lerp(_cam.orthographicSize, _targetZoomSize, Time.deltaTime * _zoomSmoothTime);
    }
    #endregion

    private void HandleInput()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked)
        {
            _isDragging = false;
            return;
        }
        bool isPressed = _pressAction.IsPressed();
        if (_showDebugLog)
            Debug.Log($"[CameraController] isPressed={isPressed}, isDragging={_isDragging}, ignoreFlag={_ignoreDragUntilRelease}");

        Vector2 screenPos = _positionAction.ReadValue<Vector2>();

        if (!isPressed)
        {
            _isDragging = false;
            _ignoreDragUntilRelease = false;
            return;
        }

        if (_ignoreDragUntilRelease)
        {
            if (_showDebugLog)
                Debug.Log("[CameraController] Drag ignored (ignoreDragUntilRelease)");
            return;
        }

        if (_pressAction.WasPressedThisFrame() || (isPressed && !_isDragging))
        {
            if (IsPointerOverUI())
            {
                if (_showDebugLog)
                    Debug.Log("[CameraController] Drag blocked by UI");
                _isDragging = false;
                return;
            }

            _isDragging = true;
            _dragOrigin = _cam.ScreenToWorldPoint(screenPos);
        }

        if (isPressed && _isDragging)
        {
            Vector3 currentPos = _cam.ScreenToWorldPoint(screenPos);
            Vector3 difference = _dragOrigin - currentPos;

            if (_followTarget != null && difference.magnitude > 0.01f)
            {
                StopFollowing();
            }

            if (_followTarget == null)
            {
                difference.z = 0;
                transform.position += difference * _dragSpeed;
            }
        }
    }

    private void FollowLogic()
    {
        Vector3 targetPos = new Vector3(_followTarget.position.x, _followTarget.position.y, transform.position.z);
        // SmoothDamp는 Unity.Mathematics에 완벽히 대응되는 함수가 없어 Vector3를 유지합니다.
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, _followSmoothTime);
    }

    private void ZoomLogic(bool isZoomIn)
    {
        float targetSize = isZoomIn ? _zoomedInSize : _zoomedOutSize;

        if (math.abs(_cam.orthographicSize - targetSize) < 0.01f)
        {
            _cam.orthographicSize = targetSize;
            return;
        }

        _cam.orthographicSize = math.lerp(_cam.orthographicSize, targetSize, Time.deltaTime * _zoomSmoothTime);
    }

    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
        _isDragging = false;
        _isManualZooming = false;
        _ignoreDragUntilRelease = true;
    }

    public void StopFollowing()
    {
        _followTarget = null;
    }

    public void MoveTo(Vector3 worldPosition)
    {
        StopFollowing();
        transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        ClampCameraPosition();
    }

    public void SmoothMoveTo(Vector3 worldPosition, float duration = 0.3f)
    {
        StopFollowing();
        StartCoroutine(CoSmoothMove(worldPosition, duration));
    }

    private System.Collections.IEnumerator CoSmoothMove(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;
        targetPos.z = startPos.z;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = math.smoothstep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            ClampCameraPosition();
            yield return null;
        }

        transform.position = targetPos;
        ClampCameraPosition();
    }

    // [수정됨] UI_CatMenu와 동일한 통합 판별 메서드 사용
    private bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void ClampCameraPosition()
    {
        Vector3 pos = transform.position;

        _camHeight = _cam.orthographicSize;
        _camWidth = _cam.orthographicSize * _cam.aspect;

        float clampedMinX = _minX + _camWidth;
        float clampedMaxX = _maxX - _camWidth;
        float clampedMinY = _minY + _camHeight;
        float clampedMaxY = _maxY - _camHeight;

        if (clampedMinX > clampedMaxX) pos.x = (_minX + _maxX) * 0.5f;
        else pos.x = math.clamp(pos.x, clampedMinX, clampedMaxX);

        if (clampedMinY > clampedMaxY) pos.y = (_minY + _maxY) * 0.5f;
        else pos.y = math.clamp(pos.y, clampedMinY, clampedMaxY);

        transform.position = pos;
    }

    public void UpdateBounds()
    {
        Bounds b = _mapBounds.bounds;
        _minX = b.min.x;
        _maxX = b.max.x;
        _minY = b.min.y;
        _maxY = b.max.y;
    }

    public void ResetState()
    {
        _followTarget = null;
        _isDragging = false;
        _ignoreDragUntilRelease = false;
        _isPinching = false;
        _isManualZooming = false;
        _targetZoomSize = _zoomedOutSize;
    }

}