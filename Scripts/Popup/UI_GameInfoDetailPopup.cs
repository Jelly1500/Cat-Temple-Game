using UnityEngine;
using UnityEngine.UI;

public class UI_GameInfoDetailPopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
        Background
    }

    enum Texts
    {
        Text_Title,
        Text_Description,
        Text_CloseBtn  // 닫기 버튼 텍스트 추가
    }

    enum Buttons
    {
        Btn_Close
    }


    private GameInfoDataSheet _infoData;

    protected override void Start()
    {
        Init();
        base.Start();
    }

    public override void Init()
    {
        if (_init) return;
        base.Init();

        BindObjects(typeof(GameObjects));
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // 닫기 버튼 이벤트 등록
        GetButton((int)Buttons.Btn_Close).onClick.AddListener(OnCloseClicked);

        // 배경 클릭 시 닫기
        GameObject background = GetObject((int)GameObjects.Background);
        background.GetOrAddComponent<Button>().onClick.AddListener(OnCloseClicked);

        // 언어 변경 이벤트 구독
        EventManager.Instance.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    public void SetInfo(GameInfoDataSheet data)
    {
        if (_init == false) Init();

        _infoData = data;
        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (!_init) Init();
        if (_infoData == null) return;

        base.RefreshUI();

        // 1. 제목 설정 (다국어)
        GetText((int)Texts.Text_Title).text = DataManager.Instance.GetText(_infoData.titleKey);

        // 2. 설명 설정 (다국어)
        GetText((int)Texts.Text_Description).text = DataManager.Instance.GetText(_infoData.descriptionKey);

        // 3. 닫기 버튼 (다국어)
        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");
       
    }

    private void OnCloseClicked()
    {
        UIManager.Instance.ClosePopupUI();
    }
}