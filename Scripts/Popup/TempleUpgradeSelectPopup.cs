using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TempleUpgradeSelectPopup : UI_UGUI, IUI_Popup
{
    #region Enums & Bindings
    enum GameObjects
    {
        State_BeforeUpgrade,
        State_InProgress,
        Content_BuildingList,
        Content_UpgradeInfo
    }

    enum Texts
    {
        Text_Title,                 // 팝업 제목 (건물 업그레이드)

        Label_CurrentInfo,          // "현재 효과"
        Label_UpgradeInfo,          // "다음 단계 효과"

        Text_CurrentHammerPoint,
        Text_CurrentInfo,           // 실제 효과 설명 (DB에서 가져옴)
        Text_UpgradeInfo,           // 실제 다음 효과 설명
        Text_Cost,                  // 실제 비용 수치
        Text_UpgradeTime,           // 실제 시간 수치
        Text_RemainingTime,

        // 버튼 텍스트
        Text_CloseBtn,              // "닫기"
        Text_UpgradeBtn,            // "업그레이드"

        Text_WarningToast
    }

    enum Buttons
    {
        Btn_Close,
        Btn_Upgrade
    }

    enum Sliders
    {
        Slider_ProgressBar
    }

    enum Images
    {
        Image_Building
    }
    #endregion

    private const string SLOT_PREFAB_NAME = "TempleBuildingSlot";

    private BuildingData _currentSelectedBuilding;
    private Coroutine _coToast;

    private List<TempleBuildingSlot> _createdSlots = new List<TempleBuildingSlot>();

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
        Bind<Slider>(typeof(Sliders));
        Bind<Image>(typeof(Images));

        GetButton((int)Buttons.Btn_Close).onClick.AddListener(ClosePopupUI);
        GetButton((int)Buttons.Btn_Upgrade).onClick.AddListener(TryStartUpgrade);

        GetText((int)Texts.Text_WarningToast).gameObject.SetActive(false);

        // [간소화] RefreshUI()가 모든 초기화를 담당
        RefreshUI();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_init)
        {
            // [간소화] RefreshUI()가 모든 갱신을 담당
            RefreshUI();
            StartCoroutine(CoRebuildLayout());
        }
    }

    private IEnumerator CoRebuildLayout()
    {
        yield return null;
        RectTransform content = Get<GameObject>((int)GameObjects.Content_UpgradeInfo).GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    public void Setup(BuildingData data)
    {
        if (_init == false) Init();
        _currentSelectedBuilding = data;

        // [간소화] RefreshUI()가 UpdateSlotSelection() 포함하여 모든 갱신 처리
        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (_init == false) return;

        // ============================================
        // [통합] RefreshUI()는 모든 UI 초기화를 담당
        // ============================================

        // 1. 리스트 갱신 (슬롯 생성/삭제)
        RefreshList();

        // 2. 첫 번째 건물 선택 (현재 선택된 건물이 없거나 리스트가 재구성된 경우)
        //    _currentSelectedBuilding이 null이면 첫 슬롯 자동 선택
        if (_currentSelectedBuilding == null && _createdSlots.Count > 0)
        {
            _currentSelectedBuilding = _createdSlots[0].GetData();
        }

        // 3. 슬롯 선택 상태 갱신
        UpdateSlotSelection();

        // 4. 건물 이미지 갱신
        RefreshBuildingImage();

        // 5. 다국어 텍스트 갱신
        RefreshLocalizedTexts();

        // 6. 망치 포인트 표시
        int available = GameDataManager.Instance.AvailableHammers;
        int max = GameDataManager.Instance.MaxHammerPoints;
        GetText((int)Texts.Text_CurrentHammerPoint).text = $"{available}/{max}";

        // 7. 현재 선택된 건물이 없으면 여기서 종료 (방어 코드)
        if (_currentSelectedBuilding == null)
        {
            Debug.LogWarning("[TempleUpgradeSelectPopup] RefreshUI: 선택된 건물이 없습니다.");
            return;
        }

        // 8. 건설 상태 확인
        bool isConstructing = BuildingManager.Instance.IsBuildingUnderConstruction(_currentSelectedBuilding.buildingId);
        var construction = BuildingManager.Instance.GetConstructionInfo(_currentSelectedBuilding.buildingId);

        // 9. 패널 전환 (업그레이드 전 / 건설 중)
        Get<GameObject>((int)GameObjects.State_BeforeUpgrade).SetActive(!isConstructing);
        Get<GameObject>((int)GameObjects.State_InProgress).SetActive(isConstructing);

        // 10. 시트 조회
        BuildingDataSheet sheet = _currentSelectedBuilding.Sheet;
        if (sheet == null)
        {
            Debug.LogError($"[TempleUpgradeSelectPopup] BuildingSheet를 찾을 수 없습니다: {_currentSelectedBuilding.buildingId}");
            return;
        }

        // 11. 업그레이드 버튼 상태 갱신
        Button upgradeBtn = GetButton((int)Buttons.Btn_Upgrade);
        TMP_Text upgradeBtnText = GetText((int)Texts.Text_UpgradeBtn);

        if (isConstructing)
        {
            RefreshInProgressUI(sheet, construction);
            upgradeBtn.interactable = true;
            upgradeBtnText.text = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Btn_CancelConstruction");
        }
        else
        {
            RefreshBeforeUpgradeUI(sheet);

            if (_currentSelectedBuilding.IsMaxLevel)
            {
                upgradeBtn.interactable = false;
                upgradeBtnText.text = DataManager.Instance.GetText("UI_Common_MaxLevel");
            }
            else
            {
                upgradeBtn.interactable = true;
                upgradeBtnText.text = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Btn_UpgradeStart");
            }
        }

        // 12. 모든 슬롯 UI 갱신
        foreach (var slot in _createdSlots)
        {
            if (slot != null)
                slot.RefreshSlotUI();
        }

        // 13. [추가] RefreshList()의 첫 번째 건물 기반으로 상세 정보 채우기
        //     이미 위 로직(8~11단계)에서 _currentSelectedBuilding 기반으로 처리됨.
        //     RefreshList() → 첫 슬롯 선택 → RefreshBeforeUpgradeUI/RefreshInProgressUI 흐름으로
        //     Text_CurrentInfo, Text_UpgradeInfo, Text_Cost, Text_UpgradeTime이 자동 채워짐.
    }

    private void RefreshLocalizedTexts()
    {
        if (DataManager.Instance == null || !DataManager.Instance.IsLoaded) return;

        // 제목
        GetText((int)Texts.Text_Title).text = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Label_Title");

        GetText((int)Texts.Label_CurrentInfo).text = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Label_CurrentInfo");
        GetText((int)Texts.Label_UpgradeInfo).text = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Label_UpgradeInfo");

        // 2. 버튼 텍스트 설정
        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");
        GetText((int)Texts.Text_UpgradeBtn).text = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Btn_Upgrade");

        GetText((int)Texts.Text_CurrentInfo).text = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Label_CurrentInfo");
        GetText((int)Texts.Text_UpgradeInfo).text = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Label_UpgradeInfo");

        if (_currentSelectedBuilding == null) return;

        // 1. BuildingDataSheet 가져오기
        int buildingId = _currentSelectedBuilding.buildingId;
        if (DataManager.Instance.BuildingSheetDict.TryGetValue(buildingId, out BuildingDataSheet sheet) == false)
        {
            Debug.LogError($"[TempleUpgradePopup] ID {buildingId}의 시트 데이터를 찾을 수 없습니다.");
            return;
        }

        // [수정] 현재 레벨 정보 및 텍스트 키 가져오기 (LV0 처리 로직 추가) -----------------------
        int currentLevel = BuildingManager.Instance.GetBuildingLevel(buildingId);
        string currentDescKey;

        if (currentLevel == 0)
        {
            // 0레벨(미건설) 상태: 
            // 시트의 levelDataList에는 1레벨부터 정보가 있으므로, 0레벨은 규칙에 따라 키를 직접 생성합니다.
            // 규칙: DESC_BUILDING_{ID}_LV0
            currentDescKey = $"DESC_BUILDING_{buildingId}_LV0";
        }
        else
        {
            // 1레벨 이상:
            // 시트에 정의된 정확한 키를 가져옵니다.
            LevelInfo currentInfo = sheet.GetLevelInfo(currentLevel);
            currentDescKey = currentInfo.descriptionKey;
        }

        // DataManager에 유효한 키(LV0 포함)를 전달하여 텍스트 출력
        GetText((int)Texts.Text_CurrentInfo).text = DataManager.Instance.GetText(currentDescKey);
        // -------------------------------------------------------------------------------------


        // [다음 단계 정보 (업그레이드)]
        int nextLevel = currentLevel + 1;
        int maxLevel = sheet.levelDataList.Count;

        if (nextLevel <= maxLevel)
        {
            LevelInfo nextInfo = sheet.GetLevelInfo(nextLevel);

            // 다음 레벨 설명
            GetText((int)Texts.Text_UpgradeInfo).text = DataManager.Instance.GetText(nextInfo.descriptionKey);

            // 건설 비용
            GetText((int)Texts.Text_Cost).text = $"{nextInfo.cost} G";

            // 건설 시간
            string dayUnit = DataManager.Instance.GetText("UI_Common_Day");
            GetText((int)Texts.Text_UpgradeTime).text = $"{nextInfo.days}{dayUnit}";
        }
        else
        {
            // 최대 레벨 도달 시
            GetText((int)Texts.Text_UpgradeInfo).text = DataManager.Instance.GetText("UI_Common_MaxLevel");
            GetText((int)Texts.Text_Cost).text = "-";
            GetText((int)Texts.Text_UpgradeTime).text = "-";
        }
    }

    private void RefreshList()
    {
        GameObject content = Get<GameObject>((int)GameObjects.Content_BuildingList);

        // 1. 기존 슬롯 제거
        foreach (Transform child in content.transform)
            Destroy(child.gameObject);
        _createdSlots.Clear();

        // 2. 데이터 시트 정렬 및 슬롯 생성
        var allSheets = DataManager.Instance.GetAllBuildingSheets();
        allSheets.Sort((a, b) => a.buildingId.CompareTo(b.buildingId));

        foreach (var sheet in allSheets)
        {
            BuildingData data = BuildingManager.Instance.GetBuildingData(sheet.buildingId);

            if (data == null)
            {
                data = new BuildingData
                {
                    buildingId = sheet.buildingId,
                    currentLevel = 0
                };
            }

            GameObject go = ResourceManager.Instance.Instantiate(SLOT_PREFAB_NAME, content.transform);
            TempleBuildingSlot slot = go.GetOrAddComponent<TempleBuildingSlot>();
            slot.SetSlot(data, OnSlotClicked);
            _createdSlots.Add(slot);
        }
    }

    /// <summary>
    /// 현재 선택된 건물에 맞게 슬롯 선택 상태 업데이트
    /// </summary>
    private void UpdateSlotSelection()
    {
        foreach (var slot in _createdSlots)
        {
            if (slot != null && slot.Data != null)
            {
                bool isSelected = (_currentSelectedBuilding != null)
                    && (slot.Data.buildingId == _currentSelectedBuilding.buildingId);
                slot.SetSelected(isSelected);
            }
        }
    }

    private void OnSlotClicked(BuildingData data)
    {
        _currentSelectedBuilding = data;

        // [간소화] RefreshUI()가 UpdateSlotSelection() + RefreshBuildingImage() 포함하여 모든 갱신 처리
        RefreshUI();
    }

    private void RefreshBuildingImage()
    {
        BuildingDataSheet sheet = _currentSelectedBuilding.Sheet;
        if (sheet == null || string.IsNullOrEmpty(sheet.spriteName)) return;

        Sprite sprite = ResourceManager.Instance.Get<Sprite>(sheet.spriteName);
        if (sprite == null)
        {
            Debug.LogWarning($"[TempleUpgradeSelectPopup] Sprite를 찾을 수 없습니다: {sheet.spriteName}");
            return;
        }

        Get<Image>((int)Images.Image_Building).sprite = sprite;
    }

    private void RefreshBeforeUpgradeUI(BuildingDataSheet sheet)
    {
        int currentLevel = _currentSelectedBuilding.currentLevel;
        bool isMax = _currentSelectedBuilding.IsMaxLevel; //

        // 1. 현재 레벨 정보 표시 (항상 표시)
        string currentDesc = GetBuildingLevelDescription(sheet, currentLevel);
        if (string.IsNullOrEmpty(currentDesc))
        {
            currentDesc = DataManager.Instance.GetText("UI_Common_None");
        }

        string formatKey = "UI_TempleUpgradeSelectPopup_Format_CurrentInfo";
        string formatText = DataManager.Instance.GetText(formatKey);
        if (formatText == formatKey) formatText = "Lv.{0}\n{1}";

        GetText((int)Texts.Text_CurrentInfo).text = string.Format(formatText, currentLevel, currentDesc);

        // 2. 다음 레벨 정보 표시 (최대 레벨 여부에 따라 분기)
        if (isMax)
        {
            // [수정됨] 최대 레벨 도달 시 업그레이드 정보 숨김 혹은 안내 문구 표시
            GetText((int)Texts.Text_UpgradeInfo).text = DataManager.Instance.GetText("UI_Desc_MaxLevelReached"); // "최고 레벨에 도달하여\n더 이상 업그레이드할 수 없습니다."

            // 비용과 시간 텍스트 숨김 (또는 빈 문자열)
            GetText((int)Texts.Text_Cost).text = "-";
            GetText((int)Texts.Text_UpgradeTime).text = "-";
        }
        else
        {
            int nextLevel = currentLevel + 1;
            string nextDesc = GetBuildingLevelDescription(sheet, nextLevel);

            if (string.IsNullOrEmpty(nextDesc))
            {
                // 데이터 시트상 다음 레벨 설명이 비어있을 때
                nextDesc = DataManager.Instance.GetText("UI_Common_MaxLevel");
            }
            else
            {
                string formatUpgrade = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Format_UpgradeInfo");
                nextDesc = string.Format(formatUpgrade, nextLevel, nextDesc);
            }

            GetText((int)Texts.Text_UpgradeInfo).text = nextDesc;

            // 비용 및 시간 표시
            var nextLevelInfo = sheet.GetLevelInfo(nextLevel);
            GetText((int)Texts.Text_Cost).text = $"{nextLevelInfo.cost} G";

            string dayText = DataManager.Instance.GetText("UI_Common_Day");
            GetText((int)Texts.Text_UpgradeTime).text = $"{nextLevelInfo.days} {dayText}";
        }
    }

    private string GetBuildingLevelDescription(BuildingDataSheet sheet, int level)
    {
        if (level <= 0) return "";

        var levelInfo = sheet.GetLevelInfo(level);
        if (levelInfo.level == 0) return "";

        if (!string.IsNullOrEmpty(levelInfo.descriptionKey))
        {
            string localizedText = DataManager.Instance.GetText(levelInfo.descriptionKey);
            if (localizedText != levelInfo.descriptionKey)
                return localizedText;
        }

        return levelInfo.description;
    }

    private void RefreshInProgressUI(BuildingDataSheet sheet, ConstructionInfo construction)
    {
        int remainingDays = TimeManager.Instance.GetRemainingDays(
            construction.EndYear, construction.EndMonth, construction.EndDay);

        string remainingFormat = DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Text_Remaining");
        GetText((int)Texts.Text_RemainingTime).text = string.Format(remainingFormat, remainingDays);

        var targetLevelInfo = sheet.GetLevelInfo(construction.TargetLevel);
        int totalDays = targetLevelInfo.days;

        float progress = 0f;
        if (totalDays > 0)
        {
            progress = 1.0f - ((float)remainingDays / totalDays);
            progress = Mathf.Clamp01(progress);
        }

        Get<Slider>((int)Sliders.Slider_ProgressBar).value = progress;
    }

    private void TryStartUpgrade()
    {
        if (_currentSelectedBuilding == null) return;

        // 현재 선택한 건물이 건설 중이면 → 공사 취소 경고 팝업
        if (BuildingManager.Instance.IsBuildingUnderConstruction(_currentSelectedBuilding.buildingId))
        {
            var popup = UIManager.Instance.ShowPopupUI<ConstructionCancelWarningPopup>();
            popup.Setup(_currentSelectedBuilding);
            return;
        }

        BuildingDataSheet sheet = _currentSelectedBuilding.Sheet;
        if (sheet == null) return;

        int nextLevel = _currentSelectedBuilding.currentLevel + 1;
        var nextLevelInfo = sheet.GetLevelInfo(nextLevel);

        if (GameDataManager.Instance.Gold < nextLevelInfo.cost)
        {
            ShowToast(DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Toast_NotEnoughGold"));
            return;
        }

        // [변경] AvailableHammers로 체크
        if (GameDataManager.Instance.AvailableHammers <= 0)
        {
            ShowToast(DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Toast_NoHammer"));
            return;
        }

        var result = BuildingManager.Instance.TryStartConstruction(
            _currentSelectedBuilding.buildingId,
            nextLevel,
            nextLevelInfo.cost,
            nextLevelInfo.days
        );

        if (result.IsSuccess)
        {
            // [간소화] RefreshUI()가 RefreshList() + UpdateSlotSelection() 포함하여 모든 갱신 처리
            RefreshUI();
            ShowToast(DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Toast_StartUpgrade"));
        }
        else
        {
            string msg = result.FailReason switch
            {
                EConstructionFailReason.AlreadyConstructing => DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Toast_AlreadyBuilding"),
                EConstructionFailReason.NotEnoughGold => DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Toast_NotEnoughGold"),
                EConstructionFailReason.NotEnoughHammer => DataManager.Instance.GetText("UI_TempleUpgradeSelectPopup_Toast_NoHammer"),
                _ => DataManager.Instance.GetText("UI_Error_Unknown")
            };
            ShowToast(msg);
        }
    }

    private void ShowToast(string message)
    {
        if (_coToast != null) StopCoroutine(_coToast);
        _coToast = StartCoroutine(CoShowToast(message));
    }

    private IEnumerator CoShowToast(string message)
    {
        TMP_Text toastText = GetText((int)Texts.Text_WarningToast);
        if (toastText != null)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(4.0f);
            toastText.gameObject.SetActive(false);
        }
    }

    private void ClosePopupUI()
    {
        UIManager.Instance.ClosePopupUI();
    }
}