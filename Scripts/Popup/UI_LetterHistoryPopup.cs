using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_LetterHistoryPopup : UI_UGUI, IUI_Popup
{
    #region Enums for Binding
    enum GameObjects
    {
        Content
    }

    enum Buttons
    {
        Btn_Close
    }

    enum Texts
    {
        Text_Title,
        Text_BtnClose,
        Text_EmptyMessage
    }
    #endregion

    private const string SLOT_PREFAB_NAME = "UI_LetterSlot";
    private List<UI_LetterSlot> _slots = new List<UI_LetterSlot>();

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

        GetButton((int)Buttons.Btn_Close).onClick.AddListener(ClosePopupUI);

        // 언어 변경 이벤트 구독
        EventManager.Instance.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);

        // 편지 읽음 이벤트 구독 (리스트 갱신용)
        EventManager.Instance.AddEvent(Define.EEventType.LetterRead, RefreshList);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_init == false) Init();

        // 팝업이 열릴 때마다 리스트 최신화
        RefreshList();
        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (_init == false) return;

        // 1. 고정 텍스트 다국어 적용
        GetText((int)Texts.Text_Title).text = DataManager.Instance.GetText("UI_LetterHistoryPopup_Title");
        GetText((int)Texts.Text_BtnClose).text = DataManager.Instance.GetText("UI_Common_Close");

        // 안내 문구 (빈 리스트일 때 표시)
        TMP_Text emptyMsg = GetText((int)Texts.Text_EmptyMessage);
        if (emptyMsg != null)
            emptyMsg.text = DataManager.Instance.GetText("UI_LetterHistoryPopup_Text_Empty");

        // 슬롯 텍스트 갱신
        foreach (var slot in _slots)
        {
            if (slot != null)
                slot.RefreshUI();
        }
    }

    private void RefreshList()
    {
        GameObject content = Get<GameObject>((int)GameObjects.Content);

        // 1. 기존 슬롯 제거
        foreach (Transform child in content.transform)
        {
            ResourceManager.Instance.Destroy(child.gameObject);
        }
        _slots.Clear();

        // 2. 통합 편지 목록 가져오기 (읽지 않은 편지 우선, 최신순, 최대 32개)
        List<LetterDisplayInfo> displayList = LetterManager.Instance.GetAllLettersForDisplay();

        // 빈 리스트 처리
        bool isEmpty = (displayList == null || displayList.Count == 0);
        TMP_Text emptyMsg = GetText((int)Texts.Text_EmptyMessage);
        if (emptyMsg != null)
            emptyMsg.gameObject.SetActive(isEmpty);

        if (isEmpty) return;

        // 3. 슬롯 생성
        foreach (LetterDisplayInfo info in displayList)
        {
            GameObject slotGO = ResourceManager.Instance.Instantiate(SLOT_PREFAB_NAME, content.transform);
            if (slotGO == null)
            {
                Debug.LogError($"[UI_LetterHistoryPopup] 프리팹 로드 실패: {SLOT_PREFAB_NAME}");
                continue;
            }

            UI_LetterSlot slot = Utils.GetOrAddComponent<UI_LetterSlot>(slotGO);

            // 날짜 문자열 생성
            string dateStr = $"{info.letterData.arrivalYear}.{info.letterData.arrivalMonth:D2}.{info.letterData.arrivalDay:D2}";

            // 슬롯 설정 (읽지 않은 상태 전달)
            slot.SetInfo(info.letterData, dateStr, OnSlotClicked, info.isUnread);
            _slots.Add(slot);
        }

        Debug.Log($"[UI_LetterHistoryPopup] Created {_slots.Count} letter slots");
    }

    private void OnSlotClicked(LetterData data)
    {
        if (data == null) return;

        // LetterManager를 통해 편지 열기 (읽지 않은 편지 처리 포함)
        LetterManager.Instance.OpenSpecificLetter(data);
    }

    private void ClosePopupUI()
    {
        UIManager.Instance.ClosePopupUI();
    }
}