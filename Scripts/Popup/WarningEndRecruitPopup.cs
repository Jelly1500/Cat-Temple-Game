using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기도 결과 나가기 경고 팝업
/// - 포기하기: 기도 상태 완전 초기화, 다음에 기도 선택 팝업부터 시작
/// - 보류하기: 기도 상태 유지, 다음에 기도 결과 팝업 다시 볼 수 있음
/// </summary>
public class WarningEndRecruitPopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
    }
    enum Buttons
    {
        Btn_ContinueBtn, // "보류하기" (팝업만 닫고 나중에 다시 볼 수 있음)
        Btn_Close        // "포기하기" (기도 상태 완전 초기화)
    }

    enum Texts
    {
        Text_WarningMessage,
        Text_CloseBtn,     // 포기 버튼 텍스트
        Text_ContinueBtn   // 보류 버튼 텍스트
    }

    protected override void Start()
    {
        base.Start();
        Init();
    }

    public override void Init()
    {
        if (_init)
            return;
        base.Init();
        BindObjects(typeof(GameObjects));
        BindButtons(typeof(Buttons));
        BindTexts(typeof(Texts));

        GetButton((int)Buttons.Btn_ContinueBtn).onClick.AddListener(OnHoldClicked);
        GetButton((int)Buttons.Btn_Close).onClick.AddListener(OnGiveUpClicked);

        RefreshUI();
    }

    protected override void OnEnable()
    {
        if (_init)
        {
            RefreshUI();
        }
    }

    public override void RefreshUI()
    {
        if (_init == false) return;
        base.RefreshUI();

        // 경고 메시지
        GetText((int)Texts.Text_WarningMessage).text = DataManager.Instance.GetText("UI_WarningEndRecruitPopup_Message");

        // 버튼 텍스트
        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_WarningEndRecruitPopup_Btn_GiveUp");
        GetText((int)Texts.Text_ContinueBtn).text = DataManager.Instance.GetText("UI_WarningEndRecruitPopup_Btn_Hold");
    }

    /// <summary>
    /// 보류하기 - 기도 상태 유지, 팝업만 닫음
    /// 나중에 Btn_Recruitment 누르면 기도 결과 팝업 다시 볼 수 있음
    /// </summary>
    private void OnHoldClicked()
    {
        // 기도 상태는 그대로 유지 (IsResultReady == true 상태 유지)
        // 모든 팝업만 닫음
        UIManager.Instance.CloseAllPopupUI();
    }

    /// <summary>
    /// 포기하기 - 기도 상태 완전 초기화
    /// 다음에 Btn_Recruitment 누르면 기도 선택 팝업부터 시작
    /// </summary>
    private void OnGiveUpClicked()
    {
        // [수정] 튜토리얼 진행 중일 때는 타겟 버튼 외의 조작을 무시함
        if (TutorialManager.Instance.IsTutorialActive)
        {
            Debug.Log("[WarningEndRecruitPopup] 튜토리얼 진행 중이므로 닫기 버튼이 동작하지 않습니다.");
            return;
        }

        // 기도 상태 완전 초기화
        PrayerManager.Instance.CompletePrayer();

        // 모든 팝업 닫음
        UIManager.Instance.CloseAllPopupUI();
    }
}