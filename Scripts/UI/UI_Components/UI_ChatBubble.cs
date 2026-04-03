using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 대화 말풍선 UI
/// [리팩토링] 생성/파괴 대신 재사용 패턴 적용
/// - 한 번 생성된 버블을 계속 재사용
/// - Show() 호출 시 애니메이션만 리셋하여 재생
/// - 대화 완전 종료 시에만 Release()로 반환
/// </summary>
public class UI_ChatBubble : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private CanvasGroup _canvasGroup; // 페이드용 (없으면 자동 추가)

    [Header("Animation Settings")]
    [SerializeField] private float _floatDuration = 2.0f;   // 상승 시간
    [SerializeField] private float _floatDistance = 1.5f;   // 상승 거리
    [SerializeField] private float _popDuration = 0.15f;    // 등장 애니메이션 시간
    [SerializeField] private float _fadeDuration = 0.3f;    // 페이드아웃 시간
    [SerializeField] private Vector3 _offset = new Vector3(0, 2.5f, 0);

    private Camera _mainCamera;
    private Transform _followTarget;        // 따라다닐 대상 (Cat의 머리)
    private Vector3 _baseOffset;            // 기본 오프셋
    private Vector3 _animationOffset;       // 애니메이션에 의한 추가 오프셋
    private Vector3 _originalScale;

    private Coroutine _animationCoroutine;
    private bool _isInitialized = false;

    // ═══════════════════════════════════════════════════════════════
    // 초기화 및 설정
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 최초 1회 초기화 (생성 시)
    /// </summary>
    public void Init(Transform followTarget)
    {
        _mainCamera = Camera.main;
        _followTarget = followTarget;
        _baseOffset = _offset;
        _animationOffset = Vector3.zero;
        _originalScale = transform.localScale;

        // CanvasGroup 확보 (페이드 효과용)
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 초기 상태: 숨김
        _canvasGroup.alpha = 0f;
        if (_iconImage != null)
            _iconImage.gameObject.SetActive(false);

        _isInitialized = true;
    }

    /// <summary>
    /// [기존 호환] 위치 기반 초기화 (followTarget 없이 사용 시)
    /// </summary>
    public void Init(Vector3 ownerPosition)
    {
        _mainCamera = Camera.main;
        _followTarget = null;
        _baseOffset = _offset;
        _animationOffset = Vector3.zero;
        _originalScale = transform.localScale;

        transform.position = ownerPosition + _offset;

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        _canvasGroup.alpha = 0f;
        if (_iconImage != null)
            _iconImage.gameObject.SetActive(false);

        _isInitialized = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // 아이콘 표시 (재사용)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 아이콘 표시 및 애니메이션 시작 (재사용 가능)
    /// 이미 애니메이션 중이면 즉시 리셋 후 새로 시작
    /// </summary>
    public void Show(Sprite sprite)
    {
        if (_iconImage == null || sprite == null) return;

        // 진행 중인 애니메이션 중단
        StopCurrentAnimation();

        // 상태 리셋
        ResetState();

        // 아이콘 설정
        _iconImage.sprite = sprite;
        _iconImage.preserveAspect = true;
        _iconImage.gameObject.SetActive(true);

        // 애니메이션 시작
        _animationCoroutine = StartCoroutine(CoFloatAnimation());
    }

    /// <summary>
    /// [기존 호환] SetIconAndStart 메서드
    /// </summary>
    public void SetIconAndStart(Sprite sprite)
    {
        Show(sprite);
    }

    /// <summary>
    /// 즉시 숨기기 (애니메이션 없이)
    /// </summary>
    public void Hide()
    {
        StopCurrentAnimation();
        _canvasGroup.alpha = 0f;
        _animationOffset = Vector3.zero;
        if (_iconImage != null)
            _iconImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 버블 해제 (대화 완전 종료 시 호출)
    /// 오브젝트를 파괴하거나 풀에 반환
    /// </summary>
    public void Release()
    {
        StopCurrentAnimation();

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.Destroy(gameObject);
        else
            Destroy(gameObject);
    }

    // ═══════════════════════════════════════════════════════════════
    // 내부 로직
    // ═══════════════════════════════════════════════════════════════

    private void StopCurrentAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
    }

    private void ResetState()
    {
        _animationOffset = Vector3.zero;
        _canvasGroup.alpha = 1f;
        transform.localScale = _originalScale;

        // 위치 즉시 갱신
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (_followTarget != null)
        {
            transform.position = _followTarget.position + _baseOffset + _animationOffset;
        }
    }

    private void LateUpdate()
    {
        // 빌보드
        if (_mainCamera != null)
        {
            transform.rotation = _mainCamera.transform.rotation;
        }

        // 타겟 추적 (애니메이션 오프셋 포함)
        if (_followTarget != null && _isInitialized)
        {
            transform.position = _followTarget.position + _baseOffset + _animationOffset;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 애니메이션 코루틴
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator CoFloatAnimation()
    {
        // Phase 1: Pop 등장
        transform.localScale = Vector3.zero;
        float timer = 0f;

        while (timer < _popDuration)
        {
            timer += Time.deltaTime;
            float t = timer / _popDuration;
            // EaseOutBack 효과
            float overshoot = 1.2f;
            float curve = 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, _originalScale, curve);
            yield return null;
        }
        transform.localScale = _originalScale;

        // Phase 2: Float 상승
        float floatTimer = 0f;
        float fadeStartTime = _floatDuration - _fadeDuration;

        while (floatTimer < _floatDuration)
        {
            floatTimer += Time.deltaTime;
            float progress = floatTimer / _floatDuration;

            // 상승 (EaseOut)
            float easeProgress = 1f - Mathf.Pow(1f - progress, 2f);
            _animationOffset = Vector3.up * (easeProgress * _floatDistance);

            // 페이드아웃 (마지막 구간)
            if (floatTimer > fadeStartTime)
            {
                float fadeProgress = (floatTimer - fadeStartTime) / _fadeDuration;
                _canvasGroup.alpha = 1f - fadeProgress;
            }

            yield return null;
        }

        // 애니메이션 완료 - 숨김 상태로 대기 (파괴하지 않음)
        _canvasGroup.alpha = 0f;
        _animationOffset = Vector3.zero;
        _animationCoroutine = null;
    }
}