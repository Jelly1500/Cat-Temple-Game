using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PrayInProgressPopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
        Slider_RemainingProgress,
    }
    enum Buttons
    {
        Btn_Close,
        Btn_Complete
    }
    enum Texts
    {
        Text_Title,
        Text_PrayName,
        Text_PrayDesc,
        Text_RemainAreaTitle,
        Text_RemainingTime,
        Text_WarningToast,
        Text_CloseBtn,
        Text_CompleteBtn
    }

    private Slider _progressSlider;
    private Coroutine _coToast;

    protected override void Start()
    {
        base.Start();
        Init();
    }

    public override void Init()
    {
        if (_init == true) return;
        base.Init();
        BindObjects(typeof(GameObjects));
        BindButtons(typeof(Buttons));
        BindTexts(typeof(Texts));

        _progressSlider = GetObject((int)GameObjects.Slider_RemainingProgress).GetComponent<Slider>();

        GetButton((int)Buttons.Btn_Close).onClick.AddListener(OnCloseClicked);
        GetButton((int)Buttons.Btn_Complete).onClick.AddListener(OnCompleteClicked);
        GetText((int)Texts.Text_WarningToast).gameObject.SetActive(false);

        EventManager.Instance.AddEvent(Define.EEventType.DateChanged, RefreshUI);

        RefreshUI();
    }

    protected override void OnEnable()
    {
        if (_init) RefreshUI();
    }

    public override void RefreshUI()
    {
        if (_init == false) return;
        base.RefreshUI();

        // 1. 현재 기도 중이 아니라면 팝업 닫기
        if (PrayerManager.Instance.IsPraying == false)
        {
            OnCloseClicked();
            return;
        }

        // 2. [수정] 현재 기도 데이터 시트 가져오기: GetCurrentPrayerData -> GetCurrentPrayerSheet
        PrayerDataSheet currentPrayer = PrayerManager.Instance.GetCurrentPrayerSheet();

        // 3. 정적 UI 텍스트 다국어 적용
        GetText((int)Texts.Text_Title).text = DataManager.Instance.GetText("UI_PrayInProgressPopup_Title");
        GetText((int)Texts.Text_RemainAreaTitle).text = DataManager.Instance.GetText("UI_PrayInProgressPopup_Label_Remain");
        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");
        GetText((int)Texts.Text_CompleteBtn).text = DataManager.Instance.GetText("UI_PrayInProgressPopup_Btn_Complete");

        // 4. 기도 이름 및 설명 다국어 적용
        GetText((int)Texts.Text_PrayName).text = DataManager.Instance.GetText(currentPrayer.nameKey);
        GetText((int)Texts.Text_PrayDesc).text = DataManager.Instance.GetText(currentPrayer.descKey);

        // 5. [수정] 남은 시간: PrayerManager에서 직접 가져오기
        int remaining = PrayerManager.Instance.RemainingDays;

        if (remaining > 0)
        {
            // [수정] TimeManager 사용
            TimeManager.Instance.CalculateFutureDate(remaining, out int fYear, out int fMonth, out int fDay);

            string dateString = $"{fYear}/{fMonth:D2}/{fDay:D2}";

            string fmt = DataManager.Instance.GetText("UI_PrayInProgressPopup_Format_Date");
            GetText((int)Texts.Text_RemainingTime).text = string.Format(fmt, dateString, remaining);

            GetButton((int)Buttons.Btn_Complete).interactable = false;

            if (_coToast == null)
                GetText((int)Texts.Text_WarningToast).text = DataManager.Instance.GetText("UI_PrayInProgressPopup_Status_Praying");
        }
        else
        {
            GetText((int)Texts.Text_RemainingTime).text = DataManager.Instance.GetText("UI_PrayInProgressPopup_Status_Complete");

            GetButton((int)Buttons.Btn_Complete).interactable = true;

            if (_coToast == null)
                GetText((int)Texts.Text_WarningToast).text = DataManager.Instance.GetText("UI_PrayInProgressPopup_Status_Check");
        }

        // 6. [수정] 진행도 슬라이더: GetPrayerProgress -> Progress 프로퍼티
        if (_progressSlider != null)
        {
            _progressSlider.value = PrayerManager.Instance.Progress;
        }
    }

    private void OnCloseClicked()
    {
        UIManager.Instance.ClosePopupUI();
    }

    private void OnCompleteClicked()
    {
        // [수정] PrayerManager에서 남은 일수 확인
        if (PrayerManager.Instance.RemainingDays > 0)
        {
            string msg = DataManager.Instance.GetText("UI_PrayInProgressPopup_Toast_NotYet");
            ShowToast(msg);
            return;
        }

        // [수정] 메서드명 변경: GeneratePrayerCandidates -> GenerateCandidates
        PrayerManager.Instance.GenerateCandidates();
        UIManager.Instance.SwitchPopupUI<RecruitmentResultPopup>();
    }

    private void ShowToast(string message)
    {
        if (_coToast != null)
            StopCoroutine(_coToast);

        _coToast = StartCoroutine(CoShowToast(message));
    }

    private System.Collections.IEnumerator CoShowToast(string message)
    {
        TMP_Text toast = GetText((int)Texts.Text_WarningToast);
        if (toast == null)
            yield break;

        toast.gameObject.SetActive(true);
        toast.text = message;
        toast.alpha = 1.0f;

        yield return new WaitForSecondsRealtime(2.0f);

        float duration = 0.6f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            toast.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }

        toast.alpha = 0f;
        toast.gameObject.SetActive(false);
        _coToast = null;
    }
}