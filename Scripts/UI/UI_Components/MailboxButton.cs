using UnityEngine;
using UnityEngine.UI;

public class MailboxButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject _redDotObject; // 에디터에서 빨간 점(Image) 오브젝트를 연결하세요.
    [SerializeField] private Button _mailboxBtn;       // 에디터에서 버튼 컴포넌트를 연결하세요.

    private void Awake()
    {
        // [수정] Start() → Awake()
        // Start()는 오브젝트 활성화 후 첫 프레임에 실행되므로,
        // 게임 초기화 중 이미 NewLetterArrived가 발화된 뒤에 구독이 등록되어
        // 이벤트를 놓치는 버그가 발생했다.
        // Awake()는 씬 로드 직후 즉시 실행되므로 모든 이벤트 발화보다 먼저 구독이 등록된다.

        if (_mailboxBtn == null)
            _mailboxBtn = GetComponent<Button>();

        _mailboxBtn.onClick.AddListener(OnMailboxClicked);

        // [이벤트 구독] 편지 도착 / 읽음 시 빨간 점 갱신
        EventManager.Instance.AddEvent(Define.EEventType.NewLetterArrived, RefreshRedDot);
        EventManager.Instance.AddEvent(Define.EEventType.LetterRead, RefreshRedDot);
    }

    private void Start()
    {
        // Awake()에서 구독 완료 후, 초기 상태를 Start()에서 확인.
        // (Awake() 시점에는 LetterManager가 아직 초기화되지 않았을 수 있으므로
        //  HasNewLetter 조회는 Start()에서 수행한다.)
        RefreshRedDot();
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveEvent(Define.EEventType.NewLetterArrived, RefreshRedDot);
            EventManager.Instance.RemoveEvent(Define.EEventType.LetterRead, RefreshRedDot);
        }
    }

    private void RefreshRedDot()
    {
        if (_redDotObject == null) return;

        bool hasLetter = LetterManager.Instance.HasNewLetter;
        _redDotObject.SetActive(hasLetter);
    }

    private void OnMailboxClicked()
    {
        if (LetterManager.Instance.HasNewLetter)
        {
            LetterManager.Instance.OpenLetter();
        }
    }
}