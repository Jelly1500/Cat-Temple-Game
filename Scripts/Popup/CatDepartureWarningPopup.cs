using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 깨달음 없이 하산 시 표시되는 경고 팝업
/// 분원 설립 없이 소멸됨을 경고
/// </summary>
public class CatDepartureWarningPopup : UI_UGUI, IUI_Popup
{
    enum Texts
    {
        Text_Warning,
        Text_CancelBtn,
        Text_ConfirmBtn
    }

    enum Buttons
    {
        Btn_Cancel,
        Btn_Confirm
    }

    private DiscipleData _discipleData;

    protected override void Start()
    {
        base.Start();
        Init();
    }

    public override void Init()
    {
        if (_init) return;
        base.Init();

        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        GetButton((int)Buttons.Btn_Cancel)?.onClick.AddListener(OnCancelClicked);
        GetButton((int)Buttons.Btn_Confirm)?.onClick.AddListener(OnConfirmClicked);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_init)
        {
            RefreshUI();
        }
    }

    public void Setup(DiscipleData data)
    {
        if (_init == false) Init();

        _discipleData = data;
        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (_init == false) return;

        // 경고 문구: 깨달음 없이 하산하면 분원 설립 없이 영구히 사라짐
        GetText((int)Texts.Text_Warning).text = DataManager.Instance.GetText("UI_CatDepartureWarning_Desc");

        // 버튼 텍스트
        GetText((int)Texts.Text_CancelBtn).text = DataManager.Instance.GetText("UI_CatDepartureWarning_Cancel");
        GetText((int)Texts.Text_ConfirmBtn).text = DataManager.Instance.GetText("UI_CatDepartureWarning_Confirm");
    }

    private void OnCancelClicked()
    {
        // 경고 팝업만 닫고 이전 팝업으로 돌아감
        UIManager.Instance.ClosePopupUI();
    }

    private void OnConfirmClicked()
    {
        if (_discipleData == null) return;

        // [수정] ObjectManager -> DiscipleManager로 변경
        // FindDiscipleObject 대신 GetObject 사용 (ID 기반 조회)
        Disciple discipleObj = DiscipleManager.Instance.GetObject(_discipleData.id);

        if (discipleObj != null && discipleObj.IsTalking)
        {
            // 대화 중이면 경고 팝업만 닫고 이전 팝업으로 복귀
            UIManager.Instance.ClosePopupUI();
            return;
        }

        // [수정] GameManager -> DiscipleManager로 변경
        // DismissDisciple 대신 ProcessDeparture 사용
        DiscipleManager.Instance.ProcessDeparture(_discipleData.id);

        // 모든 팝업 닫기
        UIManager.Instance.CloseAllPopupUI();
    }

    public void Close()
    {
        UIManager.Instance.ClosePopupUI();
    }
}