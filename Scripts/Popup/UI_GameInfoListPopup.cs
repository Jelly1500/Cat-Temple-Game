using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UI_GameInfoListPopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
        Background,
        Content_InfoList
    }

    enum Texts
    {
        Text_Title,
        Text_CloseBtn  // 닫기 버튼 텍스트 추가
    }

    enum Buttons
    {
        Btn_Close
    }

    private const string INFO_SLOT_PREFAB = "UI_GameInfoSlot";
    private const string INFO_DATA_PATH = "PreLoad/Data/GameInfoData";

    private List<GameInfoDataSheet> _infoSheets = new List<GameInfoDataSheet>();
    private List<UI_GameInfoSlot> _slots = new List<UI_GameInfoSlot>();

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
        background.GetOrAddComponent<UnityEngine.UI.Button>().onClick.AddListener(OnCloseClicked);

        // GameInfo 데이터 로드
        LoadInfoData();

        // 언어 변경 이벤트 구독
        EventManager.Instance.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    private void LoadInfoData()
    {
        // Resources에서 모든 GameInfoDataSheet 로드
        GameInfoDataSheet[] sheets = Resources.LoadAll<GameInfoDataSheet>(INFO_DATA_PATH);

        // sortOrder 기준으로 정렬
        _infoSheets = sheets.OrderBy(s => s.sortOrder).ToList();
    }

    public override void RefreshUI()
    {
        if (!_init) Init();
        base.RefreshUI();

        RefreshLocalizedTexts();
        RefreshInfoList();
    }

    private void RefreshLocalizedTexts()
    {
        // 팝업 제목
        GetText((int)Texts.Text_Title).text = DataManager.Instance.GetText("UI_GameInfoListPopup_Title");

        // 닫기 버튼
        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");
    }

    private void RefreshInfoList()
    {
        GameObject content = GetObject((int)GameObjects.Content_InfoList);

        // 1. 기존 목록 초기화
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }
        _slots.Clear();

        // 2. 데이터가 없으면 리턴
        if (_infoSheets.Count == 0)
        {
            Debug.LogWarning($"[UI_GameInfoListPopup] '{INFO_DATA_PATH}' 경로에 GameInfoDataSheet가 없습니다.");
            return;
        }

        // 3. 슬롯 생성
        foreach (var infoData in _infoSheets)
        {
            GameObject go = ResourceManager.Instance.Instantiate(INFO_SLOT_PREFAB, content.transform);

            go.transform.localScale = Vector3.one;

            UI_GameInfoSlot slot = go.GetOrAddComponent<UI_GameInfoSlot>();
            slot.SetInfo(infoData, OnInfoSlotClicked);
            _slots.Add(slot);
        }

        Debug.Log($"[UI_GameInfoListPopup] Created {_slots.Count} info slots");
    }

    private void OnInfoSlotClicked(GameInfoDataSheet data)
    {
        // 상세 정보 팝업 표시
        UI_GameInfoDetailPopup detailPopup = UIManager.Instance.ShowPopupUI<UI_GameInfoDetailPopup>();
        detailPopup.SetInfo(data);
    }

    private void OnCloseClicked()
    {
        UIManager.Instance.ClosePopupUI();
    }
}