using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_LetterSlot : UI_UGUI
{
    enum GameObjects
    {
        RedDot  // 읽지 않은 편지 표시용 붉은 점
    }

    enum Texts
    {
        Text_LetterName,
        Text_WriteTime,
        Text_SenderName
    }

    private LetterData _letterData;
    private string _receivedDate;
    private System.Action<LetterData> _onClickCallback;
    private bool _isUnread;

    public override void Init()
    {
        if (_init) return;
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<TMP_Text>(typeof(Texts));
        gameObject.GetOrAddComponent<Button>().onClick.AddListener(OnClicked);
    }

    /// <summary>
    /// 슬롯 정보 설정
    /// </summary>
    /// <param name="data">편지 데이터</param>
    /// <param name="receivedDate">수신 날짜 문자열</param>
    /// <param name="onClickCallback">클릭 콜백</param>
    /// <param name="isUnread">읽지 않은 편지 여부</param>
    public void SetInfo(LetterData data, string receivedDate, System.Action<LetterData> onClickCallback, bool isUnread = false)
    {
        if (_init == false) Init();

        _letterData = data;
        _receivedDate = receivedDate;
        _onClickCallback = onClickCallback;
        _isUnread = isUnread;

        RefreshUI();
    }

    public override void RefreshUI()
    {
        if (_init == false) return;
        if (_letterData == null) return;

        // 1. 편지 제목 설정
        GetText((int)Texts.Text_LetterName).text = DataManager.Instance.GetText(_letterData.title);

        // 2. 보낸 사람 이름 설정 (타입에 따른 분기 처리)
        if (_letterData is ApostleLetterData apostle)
        {
            string senderKey = !string.IsNullOrEmpty(apostle.senderName) ? apostle.senderName : "UI_Letter_Sender_Apostle";
            GetText((int)Texts.Text_SenderName).text = DataManager.Instance.GetText(senderKey);
        }
        else if (_letterData is VisitorLetterData visitor)
        {
            GetText((int)Texts.Text_SenderName).text = DataManager.Instance.GetText("UI_Letter_Sender_Visitor");
        }
        else
        {
            GetText((int)Texts.Text_SenderName).text = "";
        }

        // 3. 수신 날짜
        GetText((int)Texts.Text_WriteTime).text = _receivedDate;

        // 4. 읽지 않은 편지 표시 (붉은 점)
        GameObject redDot = Get<GameObject>((int)GameObjects.RedDot);
        if (redDot != null)
        {
            redDot.SetActive(_isUnread);
        }
    }

    void OnClicked()
    {
        _onClickCallback?.Invoke(_letterData);
    }
}