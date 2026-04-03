using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TempleBuildingSlot : UI_UGUI
{
    enum Texts
    {
        Text_Name,
        Text_Level,
        Text_Cost
    }

    private BuildingData _data;
    private Action<BuildingData> _onClickCallback;
    private Image _backgroundImage;
    private bool _isSelected = false;

    [Header("Selection Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _selectedColor = new Color(0.8f, 1f, 0.8f, 1f); // 연한 초록

    public BuildingData Data => _data;

    public override void Init()
    {
        if (_init) return;
        base.Init();

        Bind<TMP_Text>(typeof(Texts));

        Button btn = gameObject.GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();
        btn.onClick.AddListener(OnClickSlot);

        // 배경 이미지 캐싱 (선택 표시용)
        _backgroundImage = GetComponent<Image>();
        if (_backgroundImage == null)
            _backgroundImage = gameObject.AddComponent<Image>();
    }

    public BuildingData GetData()
    {
        return _data;
    }

    public void SetSlot(BuildingData data, Action<BuildingData> callback)
    {
        if (_init == false) Init();

        _data = data;
        _onClickCallback = callback;
        _isSelected = false;

        RefreshSlotUI();
        UpdateSelectionVisual();
    }

    public void RefreshSlotUI()
    {
        if (_init == false) return;
        if (_data == null) return;

        // 1. 이름 설정
        BuildingDataSheet sheet = _data.Sheet;
        string displayName = sheet.buildingName;

        if (sheet != null && !string.IsNullOrEmpty(sheet.nameKey))
        {
            displayName = DataManager.Instance.GetText(sheet.nameKey);
        }

        GetText((int)Texts.Text_Name).text = displayName;
        GetText((int)Texts.Text_Level).text = $"Lv.{_data.currentLevel}";

        // 2. 상태별 텍스트 분기 (건설중 / 만렙 / 일반)
        TMP_Text costText = GetText((int)Texts.Text_Cost);

        if (_data.IsConstructing)
        {
            costText.text = DataManager.Instance.GetText("UI_TempleBuildingSlot_Text_UnderConstruction") ?? "건설 중";
            costText.color = Color.gray;
        }
        else if (_data.IsMaxLevel)
        {
            costText.text = DataManager.Instance.GetText("UI_Common_MaxLevel") ?? "MAX";
            costText.color = Color.red;
        }
        else
        {
            costText.text = $"{_data.NextLevelCost} G";
            costText.color = Color.black;
        }

        UpdateSelectionVisual();
    }

    /// <summary>
    /// 선택 상태 설정 (외부에서 호출)
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        if (_backgroundImage != null)
        {
            _backgroundImage.color = _isSelected ? _selectedColor : _normalColor;
        }
    }

    private void OnClickSlot()
    {
        _onClickCallback?.Invoke(_data);
    }
}