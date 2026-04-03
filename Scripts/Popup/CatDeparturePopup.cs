using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class CatDeparturePopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
        Content_Panel,
        Image_Portrait  // 제자 초상화 이미지
    }

    enum Texts
    {
        Text_FollowerName,
        Text_Patience,
        Text_Empathy,
        Text_Wisdom,
        Text_Enlightenment,
        Text_DispartureDecs, // 설명 문구 (포맷팅 대상)
        Text_CloseBtn,       // 닫기 버튼 텍스트
        Text_LeaveBtn,       // 하산 버튼 텍스트
        Text_WarningToast    // 경고 토스트
    }

    enum Buttons
    {
        Btn_Close,
        Btn_Leave
    }

    // 데이터를 RefreshUI에서 사용하기 위해 멤버 변수로 저장
    private DiscipleData _discipleData;
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

        BindObjects(typeof(GameObjects));
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        GetButton((int)Buttons.Btn_Close)?.onClick.AddListener(Close);

        // 버튼 리스너 연결 (여기서 한 번만 연결해도 됩니다)
        GetButton((int)Buttons.Btn_Leave)?.onClick.AddListener(OnDepartClicked);

        // 토스트 초기화
        var toast = GetText((int)Texts.Text_WarningToast);
        if (toast != null) toast.gameObject.SetActive(false);
    }

    // [신규] 언어 변경 시 자동 갱신
    protected override void OnEnable()
    {
        base.OnEnable();
        if (_init && _discipleData != null)
        {
            RefreshUI();
        }
    }

    public void Setup(DiscipleData data)
    {
        if (_init == false) Init();

        _discipleData = data;
        if (data == null) return;

        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (_init == false) return;
        if (_discipleData == null) return;
        string displayName = _discipleData.name; // 기본값 (저장된 이름)

        // 1. 데이터 시트(Template)에 접근하여 nameKey 확인
        if (_discipleData.Template != null && !string.IsNullOrEmpty(_discipleData.Template.nameKey))
        {
            // 2. 키 값이 존재하면 DataManager를 통해 번역된 이름 가져오기
            displayName = DataManager.Instance.GetText(_discipleData.Template.nameKey);
        }

        GetText((int)Texts.Text_FollowerName).text = displayName;

        // [수정] 1. 스탯 라벨 + 수치 표시 (예: "인내 : 10")
        string labelPatience = DataManager.Instance.GetText("UI_Label_Patience");
        GetText((int)Texts.Text_Patience).text = $"{labelPatience} : {_discipleData.Patience}";

        string labelEmpathy = DataManager.Instance.GetText("UI_Label_Empathy");
        GetText((int)Texts.Text_Empathy).text = $"{labelEmpathy} : {_discipleData.Empathy}";

        string labelWisdom = DataManager.Instance.GetText("UI_Label_Wisdom");
        GetText((int)Texts.Text_Wisdom).text = $"{labelWisdom} : {_discipleData.Wisdom}";

        string labelEnlighten = DataManager.Instance.GetText("UI_Label_Enlighten");
        GetText((int)Texts.Text_Enlightenment).text = $"{labelEnlighten} : {_discipleData.Enlighten}";

        // 안전하게 GameObject에서 Image 컴포넌트를 얻어 처리
        GameObject portraitGO = GetObject((int)GameObjects.Image_Portrait);
        Image portraitImage = null;
        if (portraitGO != null)
            portraitImage = portraitGO.GetComponent<Image>();

        if (portraitImage == null)
        {
            Debug.LogWarning("CatDeparturePopup: Image_Portrait GameObject에 Image 컴포넌트가 없습니다.");
        }
        else
        {
            if (_discipleData.Template != null && _discipleData.Template.portrait != null)
            {
                portraitImage.sprite = _discipleData.Template.portrait;
                portraitImage.enabled = true;
                portraitImage.preserveAspect = true;
            }
            else
            {
                // 이미지가 없을 경우 안전하게 숨기거나 기본 스프라이트를 사용
                portraitImage.sprite = null;
                portraitImage.enabled = false;
            }
        }

        // 2. 버튼 텍스트 다국어 적용
        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");
        GetText((int)Texts.Text_LeaveBtn).text = DataManager.Instance.GetText("UI_CatDeparturePopup_Btn_Leave");

        // 3. 설명 문구 다국어 포맷팅
        long product = DiscipleManager.Instance.CalculateWeeklyReward(_discipleData);
        string productStr = product > 0 ? string.Format("{0:#,###}", product) : "0";

        string descFormat = DataManager.Instance.GetText("UI_CatDeparturePopup_Desc");

        GetText((int)Texts.Text_DispartureDecs).text = string.Format(descFormat,
            productStr
        );
    }

    private void OnDepartClicked()
    {
        if (_discipleData == null) return;

        Disciple discipleObj = DiscipleManager.Instance.GetObject(_discipleData.id);
        if (discipleObj != null && discipleObj.IsTalking)
        {
            string msg = DataManager.Instance.GetText("UI_CatDeparturePopup_Toast_Talking");
            ShowWarningToast(msg);
            return;
        }


        // [수정] 깨달음이 0 이하인 경우 → 경고 팝업 표시
        if (_discipleData.Enlighten < 1)
        {
            // 경고 팝업 열기 (분원 없이 소멸됨을 알림)
            var warningPopup = UIManager.Instance.ShowPopupUI<CatDepartureWarningPopup>();
            warningPopup.Setup(_discipleData);
            return;
        }

        // 깨달음이 1 이상인 경우 → 정상 하산 (분원 설립, 편지/보상)
        DiscipleManager.Instance.ProcessDeparture(_discipleData.id);

        // 팝업 닫기
        Close();
    }

    private void ShowWarningToast(string msg)
    {
        var toast = GetText((int)Texts.Text_WarningToast);
        if (toast == null) return;

        toast.text = msg;
        toast.gameObject.SetActive(true);

        if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
        _toastCoroutine = StartCoroutine(CoCloseToast(3f));
    }

    private IEnumerator CoCloseToast(float time)
    {
        yield return new WaitForSecondsRealtime(time);
        GetText((int)Texts.Text_WarningToast)?.gameObject.SetActive(false);
    }

    public void Close()
    {
        UIManager.Instance.ClosePopupUI();
    }
}