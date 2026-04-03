using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 종료 확인 팝업
/// </summary>
public class UI_ExitPopup : UI_UGUI, IUI_Popup
{
    enum Texts
    {
        Text_Desc,
        Text_CancelBtn,
        Text_ConfirmBtn
    }

    enum Buttons
    {
        Btn_Cancel,  // 팝업 닫기 (게임 재개)
        Btn_Confirm  // 게임 종료
    }

    protected override void Start()
    {
        base.Start();
        Init();
    }

    public override void Init()
    {
        if (_init) return;
        base.Init();

        // 1. UI 요소 바인딩
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // 2. 버튼 이벤트 연결
        GetButton((int)Buttons.Btn_Cancel).onClick.AddListener(OnCancelClicked);
        GetButton((int)Buttons.Btn_Confirm).onClick.AddListener(OnConfirmClicked);

        // [수정] 초기화 및 바인딩 완료 직후 텍스트 즉시 갱신
        RefreshUI();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (_init == false) return;

        // 3. 다국어 텍스트 적용
        GetText((int)Texts.Text_Desc).text = DataManager.Instance.GetText("UI_ExitPopup_Desc");
        GetText((int)Texts.Text_CancelBtn).text = DataManager.Instance.GetText("UI_Common_Cancel");
        GetText((int)Texts.Text_ConfirmBtn).text = DataManager.Instance.GetText("UI_ExitPopup_Confirm");
    }

    private void OnCancelClicked()
    {
        GameManager.Instance.ResumeGame();
        UIManager.Instance.CloseExitPopup();
    }

    private void OnConfirmClicked()
    {
        GameManager.Instance.QuitApplication();
    }
}