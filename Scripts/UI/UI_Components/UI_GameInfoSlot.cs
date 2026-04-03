using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_GameInfoSlot : UI_UGUI
{
    enum Texts
    {
        Text_InfoName
    }


    private GameInfoDataSheet _infoData;
    private System.Action<GameInfoDataSheet> _onClickCallback;

    public override void Init()
    {
        if (_init) return;
        base.Init();

        Bind<TMP_Text>(typeof(Texts));

        gameObject.GetOrAddComponent<Button>().onClick.AddListener(OnClicked);
    }

    public void SetInfo(GameInfoDataSheet data, System.Action<GameInfoDataSheet> onClickCallback)
    {
        if (_init == false) Init();

        _infoData = data;
        _onClickCallback = onClickCallback;

        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (_init == false) return;

        // 1. 제목 설정 (다국어)
        GetText((int)Texts.Text_InfoName).text = DataManager.Instance.GetText(_infoData.titleKey);

    }

    void OnClicked()
    {
        _onClickCallback?.Invoke(_infoData);
    }
}