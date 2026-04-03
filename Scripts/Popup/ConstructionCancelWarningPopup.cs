using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 공사 취소 확인 팝업
/// 취소 시 비용 50% 환불, 망치 즉시 반환
/// </summary>
public class ConstructionCancelWarningPopup : UI_UGUI, IUI_Popup
{
    enum Texts
    {
        Text_BuildingName,
        Text_WarningDesc,
        Text_RefundInfo,
        Text_CancelBtn,
        Text_ConfirmBtn
    }

    enum Buttons
    {
        Btn_Cancel,  // 팝업 닫기 (취소 안 함)
        Btn_Confirm  // 공사 취소 확정
    }

    private BuildingData _buildingData;

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
        // OnEnable에서는 RefreshUI를 호출하지 않음 - Setup에서 처리
    }

    public void Setup(BuildingData data)
    {
        Init();

        _buildingData = data;

        if (_buildingData != null)
        {
            RefreshUI();
        }
    }

    public override void RefreshUI()
    {
        if (_init == false) return;
        if (_buildingData == null) return;


        // [수정] 2. 건물 이름 가져오기 (BuildingData -> Template -> buildingName)
        string buildingName = "";
        BuildingDataSheet sheet = _buildingData.Sheet; // BuildingData의 Template 프로퍼티 사용

        if (sheet != null)
        {
            buildingName = sheet.buildingName; // 기본 이름
            if (!string.IsNullOrEmpty(sheet.nameKey))
            {
                // 다국어 키가 있으면 다국어 이름으로 덮어쓰기
                string localizedName = DataManager.Instance.GetText(sheet.nameKey);
                if (!string.IsNullOrEmpty(localizedName))
                    buildingName = localizedName;
            }
        }

        var buildingNameText = GetText((int)Texts.Text_BuildingName);
        if (buildingNameText != null)
            buildingNameText.text = buildingName;

        // 3. 경고 설명
        var warningDescText = GetText((int)Texts.Text_WarningDesc);
        if (warningDescText != null)
            warningDescText.text = DataManager.Instance.GetText("UI_ConstructionCancelWarning_Desc");

        // [수정] 4. 환불 정보 계산 (현재 건설 중인 정보에서 비용 조회)
        int originalCost = 0;
        var constructionInfo = BuildingManager.Instance.GetConstructionInfo(_buildingData.buildingId);
        if (constructionInfo != null && sheet != null)
        {
            originalCost = sheet.GetLevelInfo(constructionInfo.TargetLevel).cost;
        }

        int refundAmount = Mathf.FloorToInt(originalCost * 0.5f);

        var refundInfoText = GetText((int)Texts.Text_RefundInfo);
        if (refundInfoText != null)
        {
            string refundFormat = DataManager.Instance.GetText("UI_ConstructionCancelWarning_RefundInfo");
            if (string.IsNullOrEmpty(refundFormat))
                refundFormat = "환불 금액: {0}G (원래 비용의 50%)\n망치 포인트: 즉시 반환";

            refundInfoText.text = string.Format(refundFormat, refundAmount, originalCost);
        }

        // 5. 버튼 텍스트
        var cancelBtnText = GetText((int)Texts.Text_CancelBtn);
        if (cancelBtnText != null)
            cancelBtnText.text = DataManager.Instance.GetText("UI_Common_Close");

        var confirmBtnText = GetText((int)Texts.Text_ConfirmBtn);
        if (confirmBtnText != null)
            confirmBtnText.text = DataManager.Instance.GetText("UI_ConstructionCancelWarning_Btn_Confirm");
    }

    /// <summary>
    /// 팝업 닫기 (공사 취소 안 함)
    /// </summary>
    private void OnCancelClicked()
    {
        UIManager.Instance.ClosePopupUI();
    }

    /// <summary>
    /// 공사 취소 확정
    /// </summary>
    private void OnConfirmClicked()
    {
        if (_buildingData == null) return;

        BuildingManager.Instance.CancelConstruction(_buildingData.buildingId);
        UIManager.Instance.RefreshPopupUI<TempleUpgradeSelectPopup>();
        UIManager.Instance.ClosePopupUI();
    }

    public void Close()
    {
        UIManager.Instance.ClosePopupUI();
    }
}