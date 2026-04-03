using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_TrainingSlot : UI_UGUI
{
    enum Texts
    {
        Text_TrainingName,
        Text_TrainingCost
    }

    private TrainingDataSheet _data;
    private DiscipleData _targetDisciple;
    private System.Action<TrainingDataSheet> _onClickCallback;
    private Button _btn;
    private Image _backgroundImage;
    private bool _isSelected = false;

    [Header("Selection Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _selectedColor = new Color(0.8f, 1f, 0.8f, 1f); // 연한 초록

    // 외부에서 데이터 접근용
    public TrainingDataSheet TrainingData => _data;

    public override void Init()
    {
        if (_init) return;
        base.Init();

        Bind<TMP_Text>(typeof(Texts));

        _btn = gameObject.GetComponent<Button>();
        if (_btn == null)
            _btn = gameObject.AddComponent<Button>();

        _btn.onClick.AddListener(OnClick);

        // 배경 이미지 캐싱 (선택 표시용)
        _backgroundImage = GetComponent<Image>();
        if (_backgroundImage == null)
            _backgroundImage = gameObject.AddComponent<Image>();

        // 이벤트 구독
        EventManager.Instance.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveEvent(Define.EEventType.LanguageChanged, RefreshUI);
        }
    }

    public void Setup(DiscipleData disciple, TrainingDataSheet data, System.Action<TrainingDataSheet> onClick)
    {
        if (!_init) Init();

        _targetDisciple = disciple;
        _data = data;
        _onClickCallback = onClick;
        _isSelected = false;

        RefreshUI();
        UpdateSelectionVisual();
    }

    public override void RefreshUI()
    {
        if (!_init) return;
        if (_data == null) return;

        // 1. 훈련 이름 갱신
        string trainingName = DataManager.Instance.GetText(_data.titleKey);
        GetText((int)Texts.Text_TrainingName).text = trainingName;

        // 2. 비용 갱신 (제자별 누적 횟수 반영)
        int finalCost = _data.baseCost;

        if (TrainingManager.Instance != null && _targetDisciple != null)
        {
            finalCost = TrainingManager.Instance.CalculateCost(_targetDisciple, _data.id);
        }

        GetText((int)Texts.Text_TrainingCost).text = $"{finalCost} G";
    }

    /// <summary>
    /// 선택 상태 설정 (외부에서 호출)
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        if (_backgroundImage != null)
        {
            _backgroundImage.color = _isSelected ? _selectedColor : _normalColor;
        }
    }

    private void OnClick()
    {
        Debug.Log($"[UI_TrainingSlot] Clicked: {_data?.title}");

        if (_data == null)
        {
            Debug.LogError("[UI_TrainingSlot] _data is null on click!");
            return;
        }

        if (_onClickCallback == null)
        {
            Debug.LogError("[UI_TrainingSlot] _onClickCallback is null!");
            return;
        }

        _onClickCallback.Invoke(_data);
    }
}