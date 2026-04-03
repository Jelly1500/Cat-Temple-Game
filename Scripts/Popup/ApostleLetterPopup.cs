using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ApostleLetterPopup : UI_UGUI, IUI_Popup
{
    enum Texts
    {
        Text_Title,
        Text_Greeting,
        Text_Story,
        Text_SayThanks,
        Text_Reward,
        Text_CloseBtn
    }

    enum Buttons
    {
        Btn_Close
    }

    enum ScrollRects
    {
        ScrollView_Letter // 인스펙터의 스크롤 뷰 오브젝트 이름과 완벽히 일치해야 합니다.
    }

    private ApostleLetterData _currentData;

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

    public void SetContent(ApostleLetterData data)
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

        // 1. 제자 이름 번역
        string localizedSenderName = DataManager.Instance.GetText(_currentData.senderName);

        // 2. 제목 번역 (이름 포맷팅 포함)
        string titleFormat = DataManager.Instance.GetText(_currentData.title);
        try
        {
            GetText((int)Texts.Text_Title).text = string.Format(titleFormat, localizedSenderName);
        }
        catch
        {
            GetText((int)Texts.Text_Title).text = titleFormat;
        }

        // 3. 인사말 번역
        string greetingFormat = DataManager.Instance.GetText(_currentData.greeting);
        try
        {
            GetText((int)Texts.Text_Greeting).text = string.Format(greetingFormat, localizedSenderName);
        }
        catch
        {
            GetText((int)Texts.Text_Greeting).text = greetingFormat;
        }

        // 4. 본문 및 맺음말
        GetText((int)Texts.Text_Story).text = DataManager.Instance.GetText(_currentData.story);
        GetText((int)Texts.Text_SayThanks).text = DataManager.Instance.GetText(_currentData.sayThanks);
        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");

        // 텍스트 변경에 따른 Content 영역의 크기를 즉시 강제 재계산합니다.
        Canvas.ForceUpdateCanvases();

        // 스크롤의 수직 위치를 최상단(1f)으로 고정합니다. (최하단은 0f)
        Get<UnityEngine.UI.ScrollRect>((int)ScrollRects.ScrollView_Letter).verticalNormalizedPosition = 1f;

        // 5. 보상 표시
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
            LetterManager.Instance.OnApostleLetterClosed(_currentData);
        }
        UIManager.Instance.ShowGameToast("UI_Toast_GetGold", _currentData.rewardAmount);
        UIManager.Instance.ClosePopupUI();
    }
}