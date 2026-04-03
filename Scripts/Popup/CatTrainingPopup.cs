using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CatTrainingPopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
        Content_TrainingList
    }

    enum Texts
    {
        Text_Label_Patience,
        Text_Label_Empathy,
        Text_Label_Wisdom,
        Text_Before,
        Text_After,

        Text_PatienceBefore,
        Text_PatienceAfter,
        Text_EmpathyBefore,
        Text_EmpathyAfter,
        Text_WisdomBefore,
        Text_WisdomAfter,

        Text_WarningToast,
        Text_CloseBtn,
        Text_TrainingBtn
    }

    enum Buttons
    {
        Btn_Close,
        Btn_Training
    }

    private const string SLOT_PREFAB_NAME = "UI_TrainingSlot";
    private DiscipleData _currentDisciple;
    private TrainingDataSheet _selectedTraining;
    private List<UI_TrainingSlot> _slots = new List<UI_TrainingSlot>();
    private Coroutine _toastCoroutine;

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
        Bind<TMP_Text>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.Btn_Close).onClick.AddListener(ClosePopupUI);
        GetButton((int)Buttons.Btn_Training).onClick.AddListener(OnTrainClicked);

        GetText((int)Texts.Text_WarningToast).gameObject.SetActive(false);

        EventManager.Instance.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    public void Setup(DiscipleData disciple)
    {
        if (_init == false) Init();

        _currentDisciple = disciple;
        _selectedTraining = null; // 선택 초기화

        // [핵심] 슬롯 리스트 초기화 후 새로 생성
        _slots.Clear();
        RefreshList(); // 먼저 리스트 생성

        // 첫 번째 슬롯 자동 선택
        SelectFirstSlot();

        RefreshUI();   // 그 다음 UI 갱신
    }

    public override void RefreshUI()
    {
        if (_init == false) return;
        if (_currentDisciple == null) return;

        // 1. 정적 텍스트 다국어 적용
        GetText((int)Texts.Text_Label_Patience).text = DataManager.Instance.GetText("UI_Label_Patience");
        GetText((int)Texts.Text_Label_Empathy).text = DataManager.Instance.GetText("UI_Label_Empathy");
        GetText((int)Texts.Text_Label_Wisdom).text = DataManager.Instance.GetText("UI_Label_Wisdom");

        GetText((int)Texts.Text_Before).text = DataManager.Instance.GetText("UI_Label_Before");
        GetText((int)Texts.Text_After).text = DataManager.Instance.GetText("UI_Label_After");

        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");
        GetText((int)Texts.Text_TrainingBtn).text = DataManager.Instance.GetText("UI_CatTrainingPopup_Btn_Train");

        // 2. 수치 업데이트
        UpdateStatUI(_selectedTraining);

        // 3. 슬롯 텍스트 갱신
        foreach (var slot in _slots)
        {
            if (slot != null)
                slot.RefreshUI();
        }
    }

    private void RefreshList()
    {
        Transform contentParent = Get<GameObject>((int)GameObjects.Content_TrainingList).transform;

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        _slots.Clear();

        // [수정] 해금된 훈련만 가져오기
        List<TrainingDataSheet> trainings = TrainingManager.Instance.GetUnlockedTrainingSheets();

        foreach (var trainingData in trainings)
        {
            GameObject go = ResourceManager.Instance.Instantiate(SLOT_PREFAB_NAME, contentParent);
            if (go == null) continue;

            go.transform.localScale = Vector3.one;

            UI_TrainingSlot slot = go.GetComponent<UI_TrainingSlot>();
            if (slot != null)
            {
                slot.Setup(_currentDisciple, trainingData, OnTrainingSelected);
                _slots.Add(slot);
            }
        }

        Debug.Log($"[CatTrainingPopup] Created {_slots.Count} training slots (unlocked only)");
    }

    /// <summary>
    /// 첫 번째 슬롯 자동 선택
    /// </summary>
    private void SelectFirstSlot()
    {
        if (_slots.Count > 0)
        {
            var firstSlot = _slots[0];
            _selectedTraining = firstSlot.TrainingData;

            // 선택 상태 업데이트
            foreach (var slot in _slots)
            {
                slot.SetSelected(slot == firstSlot);
            }
        }
    }

    /// <summary>
    /// 훈련 슬롯 클릭 시 호출되는 콜백
    /// </summary>
    private void OnTrainingSelected(TrainingDataSheet sheet)
    {
        if (sheet == null)
        {
            Debug.LogWarning("[CatTrainingPopup] OnTrainingSelected: sheet is null");
            return;
        }

        _selectedTraining = sheet;
        Debug.Log($"[CatTrainingPopup] Training selected: {sheet.title} (ID: {sheet.id})");

        // 슬롯 선택 상태 업데이트 (선택된 슬롯 하이라이트)
        foreach (var slot in _slots)
        {
            if (slot != null)
                slot.SetSelected(slot.TrainingData == sheet);
        }

        UpdateStatUI(sheet);
    }

    private void OnTrainClicked()
    {
        if (_selectedTraining == null)
        {
            string msg = DataManager.Instance.GetText("UI_CatTrainingPopup_Toast_Select");
            ShowWarningToast(msg);
            return;
        }

        // TryExecuteTraining은 bool이 아닌 TrainingResult 객체를 반환합니다.
        TrainingResult result = TrainingManager.Instance.TryExecuteTraining(_currentDisciple, _selectedTraining.id);

        if (result.IsSuccess)
        {
            // 훈련 성공 시 UI 갱신
            UpdateStatUI(_selectedTraining);

            foreach (var slot in _slots)
            {
                if (slot != null)
                    slot.RefreshUI();
            }

            GetText((int)Texts.Text_WarningToast).gameObject.SetActive(false);

            // 깨달음 획득 시 추가 연출 가능 (result.GotEnlightenment 확인)
            if (result.GotEnlightenment)
            {
                string enlightenMsg = DataManager.Instance.GetText("UI_CatTrainingPopup_Toast_Enlighten");
                if (string.IsNullOrEmpty(enlightenMsg) || enlightenMsg == "UI_CatTrainingPopup_Toast_Enlighten")
                {
                    enlightenMsg = "깨달음을 얻었습니다!";
                }
                ShowWarningToast(enlightenMsg); // 이건 경고가 아닌 축하 메시지로 사용
            }

            Debug.Log("[CatTrainingPopup] Training successful!");
        }
        else
        {
            // 실패 원인에 따라 메시지 분기
            string msg;
            switch (result.FailReason)
            {
                case ETrainingFailReason.NotEnoughGold:
                    msg = DataManager.Instance.GetText("UI_CatTrainingPopup_Toast_NoGold");
                    break;
                case ETrainingFailReason.NotUnlocked:
                    msg = DataManager.Instance.GetText("UI_CatTrainingPopup_Toast_Locked");
                    if (string.IsNullOrEmpty(msg) || msg == "UI_CatTrainingPopup_Toast_Locked")
                    {
                        msg = "아직 해금되지 않은 훈련입니다.";
                    }
                    break;
                default:
                    msg = DataManager.Instance.GetText("UI_Error_Unknown");
                    break;
            }
            ShowWarningToast(msg);
        }
    }

    private void ShowWarningToast(string message)
    {
        TMP_Text toastText = GetText((int)Texts.Text_WarningToast);
        if (toastText == null) return;

        toastText.text = message;
        toastText.gameObject.SetActive(true);

        if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
        _toastCoroutine = StartCoroutine(CoCloseToast(3.0f));
    }

    private IEnumerator CoCloseToast(float duration)
    {
        // [핵심 수정] Time.timeScale = 0에서도 작동하도록 WaitForSecondsRealtime 사용
        yield return new WaitForSecondsRealtime(duration);

        var toast = GetText((int)Texts.Text_WarningToast);
        if (toast != null)
            toast.gameObject.SetActive(false);

        _toastCoroutine = null;
    }

    private void UpdateStatUI(TrainingDataSheet sheet)
    {
        if (_currentDisciple == null) return;

        // Before 값 (현재 스탯)
        GetText((int)Texts.Text_PatienceBefore).text = _currentDisciple.Patience.ToString();
        GetText((int)Texts.Text_EmpathyBefore).text = _currentDisciple.Empathy.ToString();
        GetText((int)Texts.Text_WisdomBefore).text = _currentDisciple.Wisdom.ToString();

        // After 값 (훈련 적용 시 예상 스탯)
        if (sheet != null)
        {
            GetText((int)Texts.Text_PatienceAfter).text = FormatStatString(_currentDisciple.Patience, sheet.gainPatience);
            GetText((int)Texts.Text_EmpathyAfter).text = FormatStatString(_currentDisciple.Empathy, sheet.gainEmpathy);
            GetText((int)Texts.Text_WisdomAfter).text = FormatStatString(_currentDisciple.Wisdom, sheet.gainWisdom);
        }
        else
        {
            GetText((int)Texts.Text_PatienceAfter).text = _currentDisciple.Patience.ToString();
            GetText((int)Texts.Text_EmpathyAfter).text = _currentDisciple.Empathy.ToString();
            GetText((int)Texts.Text_WisdomAfter).text = _currentDisciple.Wisdom.ToString();
        }
    }

    private string FormatStatString(int current, int increase)
    {
        if (increase > 0)
            return $"{current} <color=#4CAF50>+{increase}</color>";
        else
            return current.ToString();
    }

    private void ClosePopupUI()
    {
        _selectedTraining = null;
        UIManager.Instance.ClosePopupUI();
    }
}