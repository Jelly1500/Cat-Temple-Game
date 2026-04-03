using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 광고 보상 팝업 UI
/// - 1주일 골드 2배 버프 지급
/// </summary>
public class UI_AdsRewardPopup : UI_UGUI, IUI_Popup
{
    enum Buttons
    {
        Btn_Accept,
        Btn_Close
    }

    enum Texts
    {
        Text_Description,
        Text_AcceptButton,
        Text_CloseButton
    }

    public override void Init()
    {
        if (_init) return; // [수정] 중복 초기화 방지
        base.Init();

        BindButtons(typeof(Buttons));
        BindTexts(typeof(Texts));

        GetButton((int)Buttons.Btn_Accept).onClick.AddListener(OnClickAccept);
        GetButton((int)Buttons.Btn_Close).onClick.AddListener(OnClickClose);

        RefreshLocalizedTexts();
    }

    public override void RefreshUI()
    {
        if (!_init) Init(); // [수정] 바인딩되기 전에 텍스트 갱신이 호출되는 문제 해결

        base.RefreshUI();
        RefreshLocalizedTexts();
    }

    private void RefreshLocalizedTexts()
    {
        if (!DataManager.Instance.IsLoaded) return;

        GetText((int)Texts.Text_Description).text = DataManager.Instance.GetText("UI_AdsReward_Desc");
        GetText((int)Texts.Text_AcceptButton).text = DataManager.Instance.GetText("UI_AdsReward_Accept");
        GetText((int)Texts.Text_CloseButton).text = DataManager.Instance.GetText("UI_Common_Close");
    }

    private void OnClickAccept()
    {
        // 1. TimeScale 복구 및 화면 덮임 방지를 위해 팝업을 먼저 닫습니다.
        UIManager.Instance.ClosePopupUI(this);
        AdsManager.Instance.ShowRewardedAds(OnAdRewarded);
    }

    private void OnAdRewarded()
    {
        GameDataManager.Instance.ActivateGoldBuff(7);

        UIManager.Instance.ShowGameToast("UI_AdsReward_Toast");

        UIManager.Instance.ClosePopupUI(this);
    }

    private void OnClickClose()
    {
        UIManager.Instance.ClosePopupUI(this);
    }
}