using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CatInfoPopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
        Content_Panel
    }

    enum Images
    {
        Image_Portrait  // [신규] 제자 초상화 이미지
    }

    enum Texts
    {
        Text_FollowerName,
        Text_Patience,
        Text_Empathy,
        Text_Wisdom,
        Text_enlighten,
        Text_FollowerDesc,
        Text_EditNameBtn,
        Text_CloseBtn
    }

    enum Buttons
    {
        Btn_EditName,
        Btn_Close
    }

    private DiscipleData _currentData;

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

        // 컴포넌트 바인딩
        Bind<GameObject>(typeof(GameObjects));
        Bind<Image>(typeof(Images));        // [신규] 이미지 바인딩
        Bind<TMP_Text>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        // 닫기 버튼 이벤트 연결
        GetButton((int)Buttons.Btn_Close).onClick.AddListener(ClosePopupUI);
        GetButton((int)Buttons.Btn_EditName).onClick.AddListener(OnClickEditName);
    }

    public void Setup(DiscipleData data)
    {
        if (_init == false)
            Init();

        if (data == null)
        {
            Debug.LogWarning("CatInfoPopup: Data is null");
            return;
        }
        _currentData = data;

        SetPortraitImage(data);

        // 1. 이름
        string displayName = data.name;

        if (!data.isRenamed && data.Template != null && !string.IsNullOrEmpty(data.Template.nameKey))
        {
            displayName = DataManager.Instance.GetText(data.Template.nameKey);
        }
        GetText((int)Texts.Text_FollowerName).text = displayName;


        // 2. 스탯 라벨
        string labelPatience = DataManager.Instance.GetText("UI_Label_Patience");
        GetText((int)Texts.Text_Patience).text = $"{labelPatience} : {data.Patience}"; // Patience 프로퍼티가 있다면 유지, 없다면 trainingPatience 사용 확인 필요

        string labelEmpathy = DataManager.Instance.GetText("UI_Label_Empathy");
        GetText((int)Texts.Text_Empathy).text = $"{labelEmpathy} : {data.Empathy}";

        string labelWisdom = DataManager.Instance.GetText("UI_Label_Wisdom");
        GetText((int)Texts.Text_Wisdom).text = $"{labelWisdom} : {data.Wisdom}";


        // [수정] 3. 깨달음 (Enlighten -> trainingEnlighten)
        string labelEnlighten = DataManager.Instance.GetText("UI_Label_Enlighten");
        // Enlighten 프로퍼티가 없다면 trainingEnlighten 필드 사용
        GetText((int)Texts.Text_enlighten).text = $"{labelEnlighten} : {data.trainingEnlighten}";


        // [수정] 4. 설명 (PITCH_DISCIPLE_{templateId} 형식의 다국어 텍스트 키 사용)
        string descKey = $"PITCH_DISCIPLE_{data.templateId}";
        GetText((int)Texts.Text_FollowerDesc).text = DataManager.Instance.GetText(descKey);

        // 5. 닫기 버튼
        GetText((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");
    }

    private void OnClickEditName()
    {
        if (_currentData == null) return;

        CatNameEditPopup popup = UIManager.Instance.ShowPopupUI<CatNameEditPopup>();

        if (popup != null)
        {
            popup.Setup(_currentData, () =>
            {
                Setup(_currentData);
            });
        }
    }

    /// <summary>
    /// 제자 초상화 이미지를 설정합니다.
    /// </summary>
    private void SetPortraitImage(DiscipleData data)
    {
        Image portraitImage = GetImage((int)Images.Image_Portrait);
        if (portraitImage == null) return;

        // 1순위: DiscipleDataSheet에 직접 설정된 portrait 이미지
        if (data.Template != null && data.Template.portrait != null)
        {
            portraitImage.sprite = data.Template.portrait;
            portraitImage.gameObject.SetActive(true);
            return;
        }

        // 2순위: prefabName을 기반으로 리소스에서 로드 (기존 방식 호환)
        if (data.Template != null && !string.IsNullOrEmpty(data.Template.prefabName))
        {
            string path = $"Sprites/Cats/{data.Template.prefabName}";
            Sprite loadedSprite = ResourceManager.Instance.Get<Sprite>(path);

            if (loadedSprite != null)
            {
                portraitImage.sprite = loadedSprite;
                portraitImage.gameObject.SetActive(true);
                return;
            }
        }

        // 이미지를 찾지 못한 경우 비활성화
        portraitImage.gameObject.SetActive(false);
        Debug.LogWarning($"[CatInfoPopup] 제자 이미지를 찾을 수 없습니다: {data.name}");
    }

    private void ClosePopupUI()
    {
        UIManager.Instance.ClosePopupUI();
    }
}