using UnityEngine;
using Spine.Unity;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SkeletonAnimation))]
public class Cat : ObjectBase
{
    public enum EAnimation
    {
        // [주의] 이 이름들이 스파인 데이터의 애니메이션 이름과 정확히 일치해야 합니다!
        b_wait,
        b_walk,
        f_wait,
        f_walk
    }

    public enum ECatState
    {
        Idle,
        Move,
        Talk,
    }

    private UI_ChatBubble _currentChatBubble;
    protected SkeletonAnimation _skeletonAnimation;
    protected ECatState _state;
    private bool _isFacingForward = true;
    private bool _isFlipped = false;

    [Header("Chat Icon Location")]
    [SerializeField] protected Transform _headTransform;

    public Vector2Int _CellPosition { get; set; }

    // Awake에서 컴포넌트 연결
    protected override void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (_skeletonAnimation == null)
            Debug.LogError($"[Cat Error] {gameObject.name}에 SkeletonAnimation 컴포넌트가 없습니다!");
    }

    protected virtual void Start()
    {
        State = ECatState.Idle;
    }

    // 프롭티: 상태/방향 변경 시 애니메이션 갱신
    public ECatState State
    {
        get { return _state; }
        set
        {
            _state = value;
            UpdateAnimation();
        }
    }

    public bool IsFacingForward
    {
        get { return _isFacingForward; }
        set { _isFacingForward = value; UpdateAnimation(); }
    }

    public bool IsFlipped
    {
        get { return _isFlipped; }
        set { _isFlipped = value; _skeletonAnimation.skeleton.ScaleX = _isFlipped ? -1 : 1; }
    }

    // Isometric sorting for spine renderer
    public virtual void Update()
    {
        if (_skeletonAnimation != null)
        {
            Renderer renderer = _skeletonAnimation.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }
    }

    public void LookAt(Vector2Int targetPos)
    {
        // 1. 제자리인 경우 처리하지 않음
        if (targetPos == _CellPosition) return;

        // 안전장치: 맵 매니저가 없으면 계산 불가
        if (MapManager.Instance == null) return;

        // 2. 그리드 좌표 -> 월드 좌표 변환
        // (그리드 설정이 복잡해도 MapManager가 실제 화면 위치를 알려줍니다)
        Vector3 currentWorldPos = MapManager.Instance.CellToWorld(_CellPosition);
        Vector3 targetWorldPos = MapManager.Instance.CellToWorld(targetPos);

        // 3. 방향 벡터 계산 (목표 지점 - 현재 지점)
        Vector3 direction = (targetWorldPos - currentWorldPos).normalized;

        // 4. MoveTo 함수 벤치마킹 로직 적용
        // ------------------------------------------------------------------
        // Y축 판정: y가 0 이하(화면 아래쪽으로 이동)이면 Front(True)
        // (아이소메트릭에서 '아래'로 내려오는 것은 카메라 앞으로 오는 것이므로 Front)
        IsFacingForward = direction.y <= 0;

        // X축 판정: x가 0 미만(화면 왼쪽으로 이동)이면 Flip(True)
        // (리소스가 오른쪽을 보고 있다고 가정할 때, 왼쪽 이동 시 반전 필요)
        IsFlipped = direction.x < 0;
        // ------------------------------------------------------------------

        UpdateAnimation();
    }

    // Spine 애니메이션 재생(유효성 및 중복 재생 방지 포함)
    public void PlayAnimation(EAnimation animation)
    {
        if (_skeletonAnimation == null) return;

        string animName = animation.ToString();

        if (_skeletonAnimation.Skeleton == null || _skeletonAnimation.Skeleton.Data == null)
        {
            Debug.LogError("[Cat Error] Skeleton 데이터가 없습니다.");
            return;
        }

        if (_skeletonAnimation.Skeleton.Data.FindAnimation(animName) == null)
        {
            Debug.LogError($"[Cat Error] 스파인 데이터에 '{animName}' 애니메이션이 없습니다! (Enum 이름 확인 필요)");
            return;
        }

        var currentTrack = _skeletonAnimation.AnimationState.GetCurrent(0);
        if (currentTrack != null && currentTrack.Animation != null && currentTrack.Animation.Name == animName)
            return;

        _skeletonAnimation.AnimationState.SetAnimation(0, animName, true);
    }

    protected void UpdateAnimation()
    {
        EAnimation animation;

        switch (_state)
        {
            case ECatState.Idle:
                animation = _isFacingForward ? EAnimation.f_wait : EAnimation.b_wait;
                break;
            case ECatState.Move:
                animation = _isFacingForward ? EAnimation.f_walk : EAnimation.b_walk;
                break;
            case ECatState.Talk:
                animation = _isFacingForward ? EAnimation.f_wait : EAnimation.b_wait;
                break;
            default:
                animation = EAnimation.f_wait;
                break;
        }

        PlayAnimation(animation);
    }

    protected virtual void OnMouseDown()
    {

    }

    // ═══════════════════════════════════════════════════════════════
    // 말풍선 시스템 (재사용 패턴)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 말풍선 아이콘 표시 (재사용 패턴 적용)
    /// - 기존 버블이 있으면 재사용
    /// - 없으면 새로 생성
    /// </summary>
    public void ShowChatIcon(string spriteName)
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning("[Cat] ResourceManager.Instance is null. Cannot show chat icon.");
            return;
        }

        // 스프라이트 로드
        Sprite iconSprite = ResourceManager.Instance.Get<Sprite>(spriteName);
        if (iconSprite == null)
        {
            Debug.LogWarning($"[Cat] 이미지를 찾을 수 없습니다: {spriteName}");
            return;
        }

        // 버블이 없거나 파괴되었으면 새로 생성
        if (_currentChatBubble == null || _currentChatBubble.gameObject == null)
        {
            CreateChatBubble();
        }

        // 아이콘 표시 (재사용)
        _currentChatBubble.Show(iconSprite);
    }

    /// <summary>
    /// 말풍선 생성 (최초 1회)
    /// </summary>
    private void CreateChatBubble()
    {
        GameObject go = ResourceManager.Instance.Instantiate("UI_ChatBubble");
        if (go == null)
        {
            Debug.LogError("[Cat] 'UI_ChatBubble' 프리팹을 찾을 수 없습니다.");
            return;
        }

        _currentChatBubble = go.GetOrAddComponent<UI_ChatBubble>();

        // 머리 위치를 따라다니도록 Transform 전달
        Transform followTarget = (_headTransform != null) ? _headTransform : transform;
        _currentChatBubble.Init(followTarget);
    }

    /// <summary>
    /// 말풍선 해제 (대화 완전 종료 시 호출)
    /// </summary>
    public void ReleaseChatBubble()
    {
        if (_currentChatBubble != null && _currentChatBubble.gameObject != null)
        {
            _currentChatBubble.Release();
            _currentChatBubble = null;
        }
    }

    /// <summary>
    /// 말풍선 즉시 숨기기 (파괴하지 않음)
    /// </summary>
    public void HideChatBubble()
    {
        if (_currentChatBubble != null && _currentChatBubble.gameObject != null)
        {
            _currentChatBubble.Hide();
        }
    }
}