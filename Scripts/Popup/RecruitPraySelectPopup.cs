using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecruitPraySelectPopup : UI_UGUI, IUI_Popup
{
    #region Enums
    enum GameObjects
    {
        Content_PrayList
    }
    enum Buttons
    {
        Btn_Close,
        Btn_Pray
    }
    enum Texts
    {
        Text_Title,
        Text_PrayName,
        Text_PrayDesc,
        Text_PrayCost,
        Text_PrayTime,
        Text_WarningToast,
        Text_CloseBtn,
        Text_PrayBtn
    }
    #endregion

    private const string SLOT_PREFAB_NAME = "UI_PrayerSlot";
    private Coroutine _coToast;
    private PrayerDataSheet _currentSelectedPrayer;

    private bool _isListGenerated = false;

    // [추가] 생성된 슬롯들을 관리하기 위한 리스트
    private List<UI_PrayerSlot> _createdSlots = new List<UI_PrayerSlot>();

    protected override void Start()
    {
        base.Start();
        Init();
    }

    public override void Init()
    {
        if (_init) return;
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.Btn_Close).onClick.AddListener(OnCloseClicked);
        GetButton((int)Buttons.Btn_Pray).onClick.AddListener(OnPrayClicked);
        GetText((int)Texts.Text_WarningToast).gameObject.SetActive(false);

        EventManager.Instance.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);

        InitializeData();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveEvent(Define.EEventType.LanguageChanged, RefreshUI);
        }
    }

    private void InitializeData()
    {
        if (_isListGenerated) return;

        List<PrayerDataSheet> prayerList = PrayerManager.Instance.GetAllPrayerSheets();
        GenerateList(prayerList);

        // 첫 번째 항목 자동 선택
        if (prayerList.Count > 0)
        {
            OnPrayerSlotClicked(prayerList[0]);
        }

        _isListGenerated = true;
    }

    public override void RefreshUI()
    {
        if (_init == false) return;

        // 1. 고정 텍스트 갱신 (타이틀, 버튼 등)
        GetText((int)Texts.Text_Title).text = DataManager.Instance.GetText("UI_RecruitPraySelectPopup_Title");
        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");
        GetText((int)Texts.Text_PrayBtn).text = DataManager.Instance.GetText("UI_RecruitPraySelectPopup_Btn_Pray");

        if (_currentSelectedPrayer != null)
        {
            UpdateDetailInfo(_currentSelectedPrayer);
        }

        // [추가] UI 새로고침 시 슬롯 선택 상태 동기화
        UpdateSlotSelection();
    }

    private void GenerateList(List<PrayerDataSheet> list)
    {
        GameObject content = GetObject((int)GameObjects.Content_PrayList);

        foreach (Transform child in content.transform)
        {
            ResourceManager.Instance.Destroy(child.gameObject);
        }

        // [추가] 슬롯 리스트 초기화
        _createdSlots.Clear();

        foreach (var prayer in list)
        {
            GameObject slotGO = ResourceManager.Instance.Instantiate(SLOT_PREFAB_NAME, content.transform);

            if (slotGO == null)
            {
                Debug.LogError($"[RecruitPraySelectPopup] 프리팹 '{SLOT_PREFAB_NAME}'을 찾을 수 없습니다.");
                continue;
            }

            UI_PrayerSlot slot = slotGO.GetOrAddComponent<UI_PrayerSlot>();
            slot.SetInfo(prayer, OnPrayerSlotClicked);

            // [추가] 생성된 슬롯을 리스트에 보관
            _createdSlots.Add(slot);
        }
    }

    private void OnPrayerSlotClicked(PrayerDataSheet data)
    {
        _currentSelectedPrayer = data;
        UpdateDetailInfo(data);

        // [추가] 항목 클릭 시 선택 상태 시각적 갱신
        UpdateSlotSelection();
    }

    // [신규] 모든 슬롯의 선택 상태를 확인하고 색상을 변경하는 메서드
    private void UpdateSlotSelection()
    {
        foreach (var slot in _createdSlots)
        {
            if (slot != null && slot.Data != null)
            {
                // 현재 클릭/선택된 기도 데이터 ID와 슬롯의 데이터 ID를 비교
                bool isSelected = (_currentSelectedPrayer != null) && (slot.Data.id == _currentSelectedPrayer.id);
                slot.SetSelected(isSelected);
            }
        }
    }

    private void UpdateDetailInfo(PrayerDataSheet data)
    {
        GetText((int)Texts.Text_PrayName).text = DataManager.Instance.GetText(data.nameKey);
        GetText((int)Texts.Text_PrayDesc).text = DataManager.Instance.GetText(data.descKey);

        string costFmt = DataManager.Instance.GetText("UI_RecruitPraySelectPopup_Format_Cost");
        GetText((int)Texts.Text_PrayCost).text = string.Format(costFmt, data.cost);

        string timeFmt = DataManager.Instance.GetText("UI_RecruitPraySelectPopup_Format_Duration");
        GetText((int)Texts.Text_PrayTime).text = string.Format(timeFmt, data.durationDays);

        GetText((int)Texts.Text_WarningToast).text = "";
    }

    private void OnCloseClicked()
    {
        UIManager.Instance.ClosePopupUI();
    }

    private void OnPrayClicked()
    {
        if (_currentSelectedPrayer == null) return;

        if (!GameDataManager.Instance.CanAffordGold(_currentSelectedPrayer.cost))
        {
            string msg = DataManager.Instance.GetText("UI_RecruitPraySelectPopup_Toast_NoGold");
            ShowWarning(msg);
            return;
        }

        var result = PrayerManager.Instance.TryStartPrayer(_currentSelectedPrayer.id);

        if (result.IsSuccess)
        {
            UIManager.Instance.ClosePopupUI();
            UIManager.Instance.ShowPopupUI<PrayInProgressPopup>();
        }
        else
        {
            switch (result.FailReason)
            {
                case EPrayerFailReason.AlreadyPraying:
                    ShowWarning(DataManager.Instance.GetText("UI_RecruitPraySelectPopup_Toast_AlreadyPraying"));
                    break;
                case EPrayerFailReason.NotEnoughGold:
                    ShowWarning(DataManager.Instance.GetText("UI_RecruitPraySelectPopup_Toast_NoGold"));
                    break;
                default:
                    ShowWarning(DataManager.Instance.GetText("UI_RecruitPraySelectPopup_Toast_ErrorUnknown"));
                    break;
            }
        }
    }

    private void ShowWarning(string message)
    {
        if (_coToast != null) StopCoroutine(_coToast);
        _coToast = StartCoroutine(CoShowToast(message));
    }

    private IEnumerator CoShowToast(string message)
    {
        TMP_Text toastText = GetText((int)Texts.Text_WarningToast);
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3.0f);
        toastText.gameObject.SetActive(false);
        _coToast = null;
    }
}