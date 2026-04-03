using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_PrayerSlot : UI_UGUI
{
    enum Texts
    {
        Text_Name,
        Text_Cost
    }

    private PrayerDataSheet _data;
    // [추가] 외부에서 현재 슬롯의 데이터를 확인할 수 있도록 프로퍼티 추가
    public PrayerDataSheet Data => _data;

    private System.Action<PrayerDataSheet> _onClickCallback;
    private Button _btn;
    private Image _bgImage; // [추가] 배경 색상 변경을 위한 Image 컴포넌트

    public override void Init()
    {
        if (_init) return;
        base.Init();

        Bind<TMP_Text>(typeof(Texts));

        _btn = gameObject.GetComponent<Button>();
        _btn.onClick.AddListener(OnClick);

        // [추가] 배경 이미지 컴포넌트 캐싱
        _bgImage = gameObject.GetComponent<Image>();

        // [핵심] 슬롯 스스로 언어 변경 이벤트를 구독
        EventManager.Instance.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    private void OnDestroy()
    {
        // [필수] 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveEvent(Define.EEventType.LanguageChanged, RefreshUI);
        }
    }

    public void SetInfo(PrayerDataSheet data, System.Action<PrayerDataSheet> onClick)
    {
        if (!_init) Init();

        _data = data;
        _onClickCallback = onClick;

        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (!_init) return;
        if (_data == null) return;

        // [갱신] 현재 설정된 언어로 이름 갱신
        GetText((int)Texts.Text_Name).text = DataManager.Instance.GetText(_data.nameKey);
        GetText((int)Texts.Text_Cost).text = $"{_data.cost} G";
    }

    // [신규] 선택 상태 시각적 강조 기능
    public void SetSelected(bool isSelected)
    {
        if (_bgImage != null)
        {
            // 선택 시 연한 초록색, 비선택 시 원래 색상(흰색)
            _bgImage.color = isSelected ? new Color(0.8f, 1.0f, 0.8f) : Color.white;
        }
    }

    private void OnClick()
    {
        _onClickCallback?.Invoke(_data);
    }
}