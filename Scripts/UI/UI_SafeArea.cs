using UnityEngine;

public class UI_SafeArea : UI_Base
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea = new Rect(0, 0, 0, 0);

    protected override void Awake()
    {
        base.Awake();
        _rectTransform = GetComponent<RectTransform>();
    }

    protected override void Start()
    {
        // UI_Base의 Start()에서 RefreshUI()를 호출하므로
        // base.Start()를 호출하여 초기화 로직을 수행합니다.
        base.Start();
    }

    // UI_Base의 RefreshUI를 오버라이드하여 안전 영역 갱신 로직을 구현합니다.
    public override void RefreshUI()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        // 모바일 환경에서 화면 회전 등으로 Safe Area가 변경되었는지 매 프레임 체크합니다.
        // *최적화 팁: 만약 EventManager에 'ResolutionChanged' 이벤트가 있다면 
        // Update 대신 해당 이벤트를 구독하는 것이 좋습니다.
        if (_lastSafeArea != Screen.safeArea)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (_rectTransform == null) return;

        _lastSafeArea = Screen.safeArea;

        // 앵커(Anchor) 좌표로 변환
        Vector2 anchorMin = _lastSafeArea.position;
        Vector2 anchorMax = _lastSafeArea.position + _lastSafeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;

        // Safe Area 적용 시 여백이 생기지 않도록 오프셋을 0으로 초기화
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
    }

    // 최적화: Safe Area는 언어 변경(LanguageChanged)과 무관하므로
    // 불필요한 이벤트 구독을 방지하기 위해 OnEnable/OnDisable을 재정의합니다.
    protected override void OnEnable()
    {
        // base.OnEnable(); // 언어 변경 이벤트 구독 제외
    }

    protected override void OnDisable()
    {
        // base.OnDisable(); // 언어 변경 이벤트 구독 해제 제외
    }
}