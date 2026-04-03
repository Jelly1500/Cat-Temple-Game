using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_GameSettingPopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
        Slider_SoundSize // [슬라이더 오브젝트 이름과 일치해야 함]
    }

    enum Texts
    {
        Text_SettingTitle,
        Text_Language,
        Text_CurrentLanguage,
        Text_Volume,    // "사운드 크기" 라벨
        Text_SoundSize, // "50", "100" 등 숫자 표시 텍스트
        Text_CloseBtn,
        Text_SaveBtn
    }

    enum Buttons
    {
        Btn_LanguagePrev,
        Btn_LanguageNext,
        Btn_Close,
        Btn_Save
    }

    private List<SystemLanguage> _supportedLanguages = new List<SystemLanguage>
    {
        SystemLanguage.Korean,
        SystemLanguage.English,
    };

    private int _currentIndex = 0;

    protected override void Start()
    {
        base.Start();
        Init();
    }

    public override void Init()
    {
        if (_init) return;
        base.Init();

        // 1. 컴포넌트 바인딩
        Bind<TextMeshProUGUI>(typeof(Texts));
        BindButtons(typeof(Buttons));
        Bind<Slider>(typeof(GameObjects));

        // 2. 버튼 이벤트 연결
        GetButton((int)Buttons.Btn_LanguagePrev).onClick.AddListener(OnPrevLanguageClicked);
        GetButton((int)Buttons.Btn_LanguageNext).onClick.AddListener(OnNextLanguageClicked);
        GetButton((int)Buttons.Btn_Close).onClick.AddListener(ClosePopupUI);
        GetButton((int)Buttons.Btn_Save).onClick.AddListener(OnSaveClicked);

        // 3. 슬라이더 이벤트 연결
        Slider soundSlider = Get<Slider>((int)GameObjects.Slider_SoundSize);
        if (soundSlider != null)
        {
            soundSlider.onValueChanged.AddListener(OnSoundSliderChanged);
        }

        SyncData();
        RefreshUI();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_init)
        {
            SyncData();
            RefreshUI();
        }
    }

    public override void RefreshUI()
    {
        if (_init == false) return;
        base.RefreshUI();

        // 텍스트 다국어 적용
        Get<TextMeshProUGUI>((int)Texts.Text_SettingTitle).text = DataManager.Instance.GetText("UI_GameSettingPopup_Title");
        Get<TextMeshProUGUI>((int)Texts.Text_Language).text = DataManager.Instance.GetText("UI_GameSettingPopup_Label_Language");
        Get<TextMeshProUGUI>((int)Texts.Text_Volume).text = DataManager.Instance.GetText("UI_GameSettingPopup_Label_Sound");
        Get<TextMeshProUGUI>((int)Texts.Text_SaveBtn).text = DataManager.Instance.GetText("UI_GameSettingPopup_Btn_Save");
        Get<TextMeshProUGUI>((int)Texts.Text_CloseBtn).text = DataManager.Instance.GetText("UI_Common_Close");

        // 언어 표시 갱신
        TextMeshProUGUI languageText = Get<TextMeshProUGUI>((int)Texts.Text_CurrentLanguage);
        if (languageText != null)
        {
            SystemLanguage selectedLang = _supportedLanguages[_currentIndex];
            languageText.text = GetLanguageDisplayName(selectedLang);
        }

        // 사운드 슬라이더 및 수치 텍스트 동기화
        float currentVol = SoundManager.Instance.GetMasterVolume(); // 0.0 ~ 1.0

        // 이벤트 중복 발생 방지를 위해 리스너 없이 값 설정 가능하나, 여기선 단순히 값 할당
        Get<Slider>((int)GameObjects.Slider_SoundSize).value = currentVol;
        UpdateSoundText(currentVol);
    }

    private void OnSoundSliderChanged(float value)
    {
        // 1. 사운드 매니저에 볼륨 반영 (즉시 듣기 위함)
        SoundManager.Instance.SetMasterVolume(value);

        // [삭제됨] GameManager.Instance.GameData 접근 코드 삭제
        // 이유: GameManager는 GameData를 직접 노출하지 않으며, 
        // 볼륨 데이터는 SoundManager 내부(PlayerPrefs 등)에서 관리하는 것이 일반적입니다.

        // 2. 텍스트 갱신 (0~100)
        UpdateSoundText(value);
    }

    private void UpdateSoundText(float value)
    {
        // 0.0 ~ 1.0 값을 0 ~ 100 정수로 변환하여 표시
        int displayVol = (int)(value * 100);
        Get<TextMeshProUGUI>((int)Texts.Text_SoundSize).text = displayVol.ToString();
    }

    private void SyncData()
    {
        if (DataManager.Instance == null) return;
        SystemLanguage current = DataManager.Instance.CurrentLanguage;
        _currentIndex = _supportedLanguages.IndexOf(current);
        if (_currentIndex == -1) _currentIndex = 0;
    }

    private string GetLanguageDisplayName(SystemLanguage lang)
    {
        switch (lang)
        {
            case SystemLanguage.Korean: return "Korean";
            case SystemLanguage.English: return "English";
            case SystemLanguage.Japanese: return "Japanese";
            default: return lang.ToString();
        }
    }

    private void OnPrevLanguageClicked()
    {
        _currentIndex--;
        if (_currentIndex < 0) _currentIndex = _supportedLanguages.Count - 1;
        RefreshUI();
    }

    private void OnNextLanguageClicked()
    {
        _currentIndex++;
        if (_currentIndex >= _supportedLanguages.Count) _currentIndex = 0;
        RefreshUI();
    }

    private void OnSaveClicked()
    {
        SystemLanguage selectedLang = _supportedLanguages[_currentIndex];

        // 언어가 실제로 변경되었는지 확인
        bool isLanguageChanged = (DataManager.Instance.CurrentLanguage != selectedLang);

        DataManager.Instance.CurrentLanguage = selectedLang;

        float currentVol = Get<Slider>((int)GameObjects.Slider_SoundSize).value;
        DataManager.Instance.SetVolumeData(currentVol);

        // 언어 설정 적용 (텍스트 내용 변경)
        UIManager.Instance.RefreshAllActiveUI();
        EventManager.Instance.TriggerEvent(Define.EEventType.LanguageChanged);

        // [추가] 폰트 에셋 교체 요청
        if (isLanguageChanged && FontManager.Instance != null)
        {
            FontManager.Instance.RefreshFont(selectedLang);
        }

        // 게임 데이터 저장
        SaveManager.Instance.Save();

        ClosePopupUI();
    }

    private void ClosePopupUI()
    {
        UIManager.Instance.ClosePopupUI();
    }
}