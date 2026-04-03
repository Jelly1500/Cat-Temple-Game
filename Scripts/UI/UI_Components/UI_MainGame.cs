using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_MainGame : UI_UGUI, IUI_Scene
{
    enum GameObjects
    {
        SafeArea_Panel,
        HeaderPanel,
        Dynamic_UI_Group,
        UI_CatMenu,
        ScrollView_List,
        Content_FollowerList,
        RedDot_Building,
        RedDot_Recruitment,
        ClockGauge,
        ClockHands,
        
    }

    enum Texts
    {
        Text_PlayerGold,
        Text_Renown,
        Text_GameTime,
        Text_FollowerAccount,
        Text_GameToastMessage,
        Text_GameSetting,
        Text_BuildingButton,
        Text_RecruitmentButton,
        Text_DocumentButton,
        Text_ShopButton,
    }

    enum Buttons
    {
        Btn_FollowerListToggle,
        Btn_GameSetting,
        Btn_AdsReward, // [추가] 광고 보상 버튼
        Btn_Building,
        Btn_Recruitment,
        Btn_Document,
        Btn_Shop
    }

    private UI_CatMenu _catMenu;
    private GameObject _scrollViewList;
    private Image _clockGaugeImage;
    private RectTransform _clockHandsRect;

    private const string FOLLOWER_SLOT_PREFAB = "UI_FollowerNameSlot";
    private Coroutine _toastCoroutine;
    [SerializeField] private List<GameObject> _tutorialHighlights = new List<GameObject>();
    private int _currentHighlightIndex = -1;

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
        BindButtons(typeof(Buttons));
        BindTexts(typeof(Texts));

        GameObject catMenuGO = GetObject((int)GameObjects.UI_CatMenu);
        _catMenu = catMenuGO.GetComponent<UI_CatMenu>();
        _catMenu.Init();

        _scrollViewList = GetObject((int)GameObjects.ScrollView_List);
        _scrollViewList.SetActive(false);

        GetText((int)Texts.Text_GameToastMessage).gameObject.SetActive(false);

        _clockGaugeImage = GetObject((int)GameObjects.ClockGauge).GetComponent<Image>();
        _clockGaugeImage.type = Image.Type.Filled;
        _clockGaugeImage.fillMethod = Image.FillMethod.Radial360;
        _clockGaugeImage.fillOrigin = (int)Image.Origin360.Top;
        _clockGaugeImage.fillClockwise = true;
        _clockGaugeImage.fillAmount = 0f;

        _clockHandsRect = GetObject((int)GameObjects.ClockHands).GetComponent<RectTransform>();

        GetButton((int)Buttons.Btn_FollowerListToggle).onClick.AddListener(OnFollowerListToggleClicked);
        GetButton((int)Buttons.Btn_Building).onClick.AddListener(OnBuildingClicked);
        GetButton((int)Buttons.Btn_Recruitment).onClick.AddListener(OnRecruitmentClicked);
        GetButton((int)Buttons.Btn_Document).onClick.AddListener(OnDocumentClicked);
        GetButton((int)Buttons.Btn_GameSetting).onClick.AddListener(OnGameSettingClicked);
        GetButton((int)Buttons.Btn_Shop).onClick.AddListener(OnShopClicked);
        GetButton((int)Buttons.Btn_AdsReward).onClick.AddListener(OnAdsRewardClicked); // [추가] 리스너 등록

        GetObject((int)GameObjects.RedDot_Building).SetActive(false);
        GetObject((int)GameObjects.RedDot_Recruitment).SetActive(false);

        EventManager.Instance.AddEvent(Define.EEventType.DateChanged, RefreshTimeUI);
        EventManager.Instance.AddEvent(Define.EEventType.GoldChanged, RefreshResourceUI);
        EventManager.Instance.AddEvent(Define.EEventType.RenownChanged, RefreshResourceUI);
        EventManager.Instance.AddEvent(Define.EEventType.DiscipleRecruited, RefreshDiscipleCountUI);
        EventManager.Instance.AddEvent(Define.EEventType.DiscipleDeparted, RefreshDiscipleCountUI);
        EventManager.Instance.AddEvent(Define.EEventType.DiscipleCountChanged, RefreshDiscipleCountUI);
        EventManager.Instance.AddEvent(Define.EEventType.ConstructionCompleted, OnConstructionCompleted);
        EventManager.Instance.AddEvent(Define.EEventType.PrayerCompleted, UpdateRecruitmentRedDot);
        EventManager.Instance.AddEvent(Define.EEventType.PrayerStarted, UpdateRecruitmentRedDot);
    }

    private void Update()
    {
        UpdateClockUI();
    }

    public override void RefreshUI()
    {
        if (!_init)
        {
            Init();
        }
        base.RefreshUI();
        RefreshTimeUI();
        RefreshResourceUI();
        RefreshFollowerList();
        RefreshLocalizedTexts();
        RefreshDiscipleCountUI();
        RefreshRedDots();
    }

    private void RefreshLocalizedTexts()
    {
        if (!DataManager.Instance.IsLoaded) return;

        GetText((int)Texts.Text_BuildingButton).text = DataManager.Instance.GetText("UI_MainGame_Btn_Building");
        GetText((int)Texts.Text_RecruitmentButton).text = DataManager.Instance.GetText("UI_MainGame_Btn_Recruitment");
        GetText((int)Texts.Text_DocumentButton).text = DataManager.Instance.GetText("UI_MainGame_Btn_Document");
        GetText((int)Texts.Text_GameSetting).text = DataManager.Instance.GetText("UI_MainGame_Btn_Setting");
        GetText((int)Texts.Text_ShopButton).text = DataManager.Instance.GetText("UI_MainGame_Btn_Shop");
    }

    private void UpdateClockUI()
    {
        float progress = TimeManager.Instance.DayProgress;
        _clockGaugeImage.fillAmount = progress;
        float angle = -360f * progress;
        _clockHandsRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    void RefreshTimeUI()
    {
        GetText((int)Texts.Text_GameTime).text = TimeManager.Instance.GetDateString();
        RefreshAdButtonUI(); // [추가] 날짜가 변경될 때 버프 만료 여부를 체크하여 버튼 활성/비활성화
    }

    void RefreshResourceUI()
    {
        GetText((int)Texts.Text_PlayerGold).text = $"{GameDataManager.Instance.Gold}G";
        GetText((int)Texts.Text_Renown).text = $"{GameDataManager.Instance.Renown}";
    }

    private void RefreshDiscipleCountUI()
    {
        GetText((int)Texts.Text_FollowerAccount).text =
            $"{DiscipleManager.Instance.CurrentCount}/{DiscipleManager.Instance.MaxCount}";
    }

    // [추가] 광고 버튼 표시 갱신 로직
    private void RefreshAdButtonUI()
    {
        // 버프가 활성화되어 있다면 버튼을 비활성화(숨김), 아니라면 활성화(표시)
        bool isBuffActive = GameDataManager.Instance.IsGoldBuffActive;
        GetButton((int)Buttons.Btn_AdsReward).gameObject.SetActive(!isBuffActive);
    }

    // [추가] 광고 보상 버튼 클릭 이벤트
    private void OnAdsRewardClicked()
    {
        UIManager.Instance.ShowPopupUI<UI_AdsRewardPopup>();
    }

    private void OnFollowerListToggleClicked()
    {
        bool newState = !_scrollViewList.activeSelf;
        _scrollViewList.SetActive(newState);
        if (newState) RefreshFollowerList();
    }

    public void ShowToastMessage(string message)
    {
        TMP_Text toastText = GetText((int)Texts.Text_GameToastMessage);
        toastText.text = message;
        toastText.gameObject.SetActive(true);

        if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
        _toastCoroutine = StartCoroutine(CoHideToastAfterDelay(toastText, 5.0f));
    }

    private System.Collections.IEnumerator CoHideToastAfterDelay(TMP_Text target, float delay)
    {
        yield return new WaitForSeconds(delay);
        target.gameObject.SetActive(false);
        _toastCoroutine = null;
    }

    private void OnGameSettingClicked()
    {
        UIManager.Instance.ShowPopupUI<UI_GameSettingPopup>();
    }

    private void OnShopClicked()
    {
        UIManager.Instance.ShowPopupUI<ShopUI>();
    }

    public void RefreshFollowerList()
    {
        Transform parent = GetObject((int)GameObjects.Content_FollowerList).transform;

        foreach (Transform child in parent)
        {
            ResourceManager.Instance.Destroy(child.gameObject);
        }

        var disciples = DiscipleManager.Instance.Disciples;

        foreach (var disciple in disciples)
        {
            GameObject go = ResourceManager.Instance.Instantiate(FOLLOWER_SLOT_PREFAB, parent);
            UI_FollowerNameSlot slot = go.GetComponent<UI_FollowerNameSlot>();

            if (slot != null)
            {
                slot.SetInfo(disciple, OnClickDiscipleSlot);
            }
        }
    }

    private void OnClickDiscipleSlot(string discipleId)
    {
        Disciple disciple = DiscipleManager.Instance.GetObject(discipleId);
        _scrollViewList.SetActive(false);
        ShowCatMenu(disciple.transform);
        CameraController.Instance.SetFollowTarget(disciple.transform);
    }

    private void OnBuildingClicked()
    {
        GetObject((int)GameObjects.RedDot_Building).SetActive(false);
        UIManager.Instance.ShowPopupUI<TempleUpgradeSelectPopup>();
    }

    private void OnRecruitmentClicked()
    {
        if (!PrayerManager.Instance.IsPraying)
        {
            UIManager.Instance.ShowPopupUI<RecruitPraySelectPopup>();
        }
        else if (!PrayerManager.Instance.IsResultReady)
        {
            UIManager.Instance.ShowPopupUI<PrayInProgressPopup>();
        }
        else
        {
            GetObject((int)GameObjects.RedDot_Recruitment).SetActive(false);
            UIManager.Instance.ShowPopupUI<RecruitmentResultPopup>();
        }
    }

    private void OnDocumentClicked()
    {
        UIManager.Instance.ShowPopupUI<UI_GameInfoListPopup>();
    }

    public void ShowCatMenu(Transform targetCat)
    {
        _catMenu.Init();
        _catMenu.ShowMenu(targetCat);
    }

    private void RefreshRedDots()
    {
        UpdateRecruitmentRedDot();
    }

    private void UpdateRecruitmentRedDot()
    {
        bool isReady = PrayerManager.Instance.IsResultReady;

        if (isReady)
        {
            var popup = FindFirstObjectByType<RecruitmentResultPopup>(); // [cite: 2025-11-16]
            if (popup != null && popup.gameObject.activeInHierarchy)
            {
                isReady = false;
            }
        }

        GetObject((int)GameObjects.RedDot_Recruitment).SetActive(isReady);
    }

    private void OnConstructionCompleted()
    {
        GetObject((int)GameObjects.RedDot_Building).SetActive(true);
        UIManager.Instance.ShowGameToast("건설이 완료되었습니다.");
        RefreshDiscipleCountUI();
    }

    public void ShowNextTutorialHighlight()
    {
        if (_tutorialHighlights == null || _tutorialHighlights.Count == 0) return;

        _currentHighlightIndex++;

        if (_currentHighlightIndex >= _tutorialHighlights.Count)
        {
            _currentHighlightIndex = -1;
        }

    }

    
    
}