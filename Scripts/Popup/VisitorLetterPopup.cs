using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VisitorLetterPopup : UI_UGUI, IUI_Popup
{
    enum Texts
    {
        Text_Title,
        Text_Greeting,
        Text_Story,
        Text_Empathy,
        Text_Wisdom,
        Text_Thanks,
        Text_Reward,
        Text_CloseBtn
    }

    enum Buttons
    {
        Btn_Close
    }

    enum ScrollRects
    {
        ScrollView_Letter 
    }

    private VisitorLetterData _currentData;

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

        Bind<UnityEngine.UI.ScrollRect>(typeof(ScrollRects));

        GetButton((int)Buttons.Btn_Close).onClick.AddListener(OnCloseClicked);
    }

    public void SetContent(VisitorLetterData data)
    {
        if (_init == false) Init();
        if (data == null) return;

        _currentData = data;
        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (_init == false) Init();
        if (_currentData == null) return;

        // 1. 제목
        GetText((int)Texts.Text_Title).text = DataManager.Instance.GetText(_currentData.title);

        // 2. 내용들
        GetText((int)Texts.Text_Greeting).text = DataManager.Instance.GetText(_currentData.greeting);
        GetText((int)Texts.Text_Story).text = DataManager.Instance.GetText(_currentData.story);

        // 3. 스탯 관련 문구
        GetText((int)Texts.Text_Empathy).text = DataManager.Instance.GetText(_currentData.empathy);
        GetText((int)Texts.Text_Wisdom).text = DataManager.Instance.GetText(_currentData.wisdom);
        GetText((int)Texts.Text_Thanks).text = DataManager.Instance.GetText(_currentData.thanks);

        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");

        // 텍스트 변경에 따른 Content 영역의 크기를 즉시 강제 재계산합니다.
        Canvas.ForceUpdateCanvases();

        // 스크롤의 수직 위치를 최상단(1f)으로 고정합니다. (최하단은 0f)
        Get<UnityEngine.UI.ScrollRect>((int)ScrollRects.ScrollView_Letter).verticalNormalizedPosition = 1f;

        // 4. 보상 표시
        if (_currentData.rewardAmount > 0)
        {
            if (_currentData.isRewardClaimed)
            {
                GetText((int)Texts.Text_Reward).text = "<color=#808080>Completed</color>";
            }
            else
            {
                GetText((int)Texts.Text_Reward).text = string.Format("{0:#,###}G", _currentData.rewardAmount);
            }
        }
        else
        {
            GetText((int)Texts.Text_Reward).text = "";
        }
    }

    void OnCloseClicked()
    {
        if (_currentData != null)
        {
            // [수정] GameManager → LetterManager
            LetterManager.Instance.ReceiveVisitorLetterReward(_currentData);
        }
        
        UIManager.Instance.ClosePopupUI();
    }
}