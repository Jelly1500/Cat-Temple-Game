using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecruitmentResultPopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
        Image_Candidate
    }
    enum Buttons
    {
        Btn_Prev,
        Btn_Next,
        Btn_Recruit,
        Btn_Close
    }
    enum Texts
    {
        Text_Title,
        Text_FollowerAccount,
        Text_CandidateNumber,
        Text_DiscipleName,
        Text_StatusInfo,
        Text_Patience,
        Text_Empathy,
        Text_Wisdom,
        Text_CandidateDesc,
        Text_RecruitCost,
        Text_WarningToast,
        Text_Name,

        Text_BtnRecruit,
        Text_BtnClose
    }

    private PrayerDataSheet _completedPrayerData;
    private List<DiscipleDataSheet> _candidates;
    private int _currentIndex = 0;
    private Coroutine _coToast;

    protected override void Start()
    {
        base.Start();
        Init();
        GetText((int)Texts.Text_WarningToast).gameObject.SetActive(false);
    }

    public override void Init()
    {
        if (_init) return;
        base.Init();
        BindObjects(typeof(GameObjects));
        BindButtons(typeof(Buttons));
        BindTexts(typeof(Texts));

        GetButton((int)Buttons.Btn_Recruit).onClick.AddListener(OnRecruitClicked);
        GetButton((int)Buttons.Btn_Close).onClick.AddListener(OnCloseClicked);

        GetButton((int)Buttons.Btn_Prev).onClick.AddListener(OnPrevClicked);
        GetButton((int)Buttons.Btn_Next).onClick.AddListener(OnNextClicked);



        LoadResultData();
        RefreshUI();
    }

    protected override void OnEnable()
    {
        if (_init)
        {
            LoadResultData();
            RefreshUI();
        }
    }

    public override void RefreshUI()
    {
        if (_init == false) return;
        base.RefreshUI();

        // 1. 기본 다국어 텍스트
        GetText((int)Texts.Text_Title).text = DataManager.Instance.GetText("UI_RecruitmentResultPopup_Title");
        GetText((int)Texts.Text_StatusInfo).text = DataManager.Instance.GetText("UI_RecruitmentResultPopup_StatusInfo");
        GetText((int)Texts.Text_BtnRecruit).text = DataManager.Instance.GetText("UI_RecruitmentResultPopup_Btn_Recruit");
        GetText((int)Texts.Text_BtnClose).text = DataManager.Instance.GetText("UI_Common_Close");

        // 2. [수정] 제자 보유 현황 - DiscipleManager에서 가져오기
        int currentCount = DiscipleManager.Instance.CurrentCount;
        int maxCount = DiscipleManager.Instance.MaxCount;
        GetText((int)Texts.Text_FollowerAccount).text = $"{currentCount} / {maxCount}";
    }

    private void LoadResultData()
    {
        // [수정] 메서드명 변경: GetCurrentPrayerData -> GetCurrentPrayerSheet
        _completedPrayerData = PrayerManager.Instance.GetCurrentPrayerSheet();

        // [수정] 메서드명 변경: CurrentPrayerCandidates -> CurrentCandidates
        if (PrayerManager.Instance.CurrentCandidates.Count == 0)
        {
            // [수정] 메서드명 변경: GeneratePrayerCandidates -> GenerateCandidates
            PrayerManager.Instance.GenerateCandidates();
        }

        // IReadOnlyList를 List로 변환
        _candidates = new List<DiscipleDataSheet>(PrayerManager.Instance.CurrentCandidates);

        if (_completedPrayerData == null || _candidates == null || _candidates.Count == 0)
        {
            return;
        }

        _currentIndex = 0;
        RefreshCandidateUI();
    }

    private void RefreshCandidateUI()
    {
        if (_candidates == null || _candidates.Count == 0) return;

        if (_currentIndex >= _candidates.Count) _currentIndex = _candidates.Count - 1;
        if (_currentIndex < 0) _currentIndex = 0;

        DiscipleDataSheet data = _candidates[_currentIndex];

        // 이름
        string displayName = data.defaultName;
        if (!string.IsNullOrEmpty(data.nameKey))
        {
            displayName = DataManager.Instance.GetText(data.nameKey);
        }
        GetText((int)Texts.Text_DiscipleName).text = displayName;

        // 후보 번호 (1/3)
        GetText((int)Texts.Text_CandidateNumber).text = $"{_currentIndex + 1} / {_candidates.Count}";

        // 스탯
        GetText((int)Texts.Text_Patience).text = $"{DataManager.Instance.GetText("UI_Label_Patience")}:{data.basePatience}";
        GetText((int)Texts.Text_Empathy).text = $"{DataManager.Instance.GetText("UI_Label_Empathy")}:{data.baseEmpathy}";
        GetText((int)Texts.Text_Wisdom).text = $"{DataManager.Instance.GetText("UI_Label_Wisdom")}:{data.baseWisdom}";

        // 설명
        // 설명 - pitchSentenceKey 우선, 없으면 pitchSentence 레거시 폴백
        string pitch;
        if (!string.IsNullOrEmpty(data.pitchSentenceKey))
            pitch = DataManager.Instance.GetText(data.pitchSentenceKey);
        else
            pitch = data.pitchSentence;
        GetText((int)Texts.Text_CandidateDesc).text = $"\"{pitch}\"";

        // 영입 비용
        GetText((int)Texts.Text_RecruitCost).text = $"{DataManager.Instance.GetText("UI_Label_RecruitCost")}:{data.baseHireCost} G";

        // 이미지 설정
        SetCandidateImage(data);

        // 버튼 활성화 상태
        GetButton((int)Buttons.Btn_Prev).interactable = (_currentIndex > 0);
        GetButton((int)Buttons.Btn_Next).interactable = (_currentIndex < _candidates.Count - 1);
    }

    private void SetCandidateImage(DiscipleDataSheet data)
    {
        Image candidateImage = GetObject((int)GameObjects.Image_Candidate).GetComponent<Image>();
        if (candidateImage == null) return;

        // 1순위: portrait 이미지
        if (data.portrait != null)
        {
            candidateImage.sprite = data.portrait;
            return;
        }

        // 2순위: prefabName 기반 리소스 로드
        if (!string.IsNullOrEmpty(data.prefabName))
        {
            string path = $"Sprites/Cats/{data.prefabName}";
            Sprite loadedSprite = ResourceManager.Instance.Get<Sprite>(path);

            if (loadedSprite != null)
            {
                candidateImage.sprite = loadedSprite;
                return;
            }
        }

        Debug.LogWarning($"[RecruitmentResultPopup] 이미지를 찾을 수 없습니다: {data.defaultName}");
    }

    private void OnPrevClicked()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            RefreshCandidateUI();
        }
    }

    private void OnNextClicked()
    {
        if (_currentIndex < _candidates.Count - 1)
        {
            _currentIndex++;
            RefreshCandidateUI();
        }
    }

    private void OnRecruitClicked()
    {
        if (_candidates == null || _candidates.Count == 0) return;

        // [수정] 한도 체크 - DiscipleManager 사용
        if (!DiscipleManager.Instance.CanRecruit)
        {
            string msg = DataManager.Instance.GetText("UI_RecruitmentResultPopup_Toast_DiscipleFull");
            ShowToast(msg);
            return;
        }

        if (_currentIndex >= _candidates.Count) _currentIndex = 0;

        DiscipleDataSheet target = _candidates[_currentIndex];

        // [수정] 골드 체크 - GameDataManager 사용
        if (!GameDataManager.Instance.CanAffordGold(target.baseHireCost))
        {
            ShowToast(DataManager.Instance.GetText("UI_RecruitmentResultPopup_Toast_NoGold"));
            return;
        }

        // [수정] 골드 차감 - GameDataManager 사용
        GameDataManager.Instance.TrySpendGold(target.baseHireCost);

        // [수정] 영입 - TryRecruitCandidate 사용
        bool recruited = PrayerManager.Instance.TryRecruitCandidate(target);

        if (!recruited)
        {
            // 영입 실패 시 골드 환불
            GameDataManager.Instance.AddGold(target.baseHireCost);
            ShowToast("영입에 실패했습니다.");
            return;
        }

        // 로컬 리스트에서도 제거
        _candidates.Remove(target);

        // 영입 성공 시 보유 숫자 갱신
        RefreshUI();

        if (_candidates.Count == 0)
        {
            Debug.Log("모든 후보 영입 완료 또는 기도 종료.");
            PrayerManager.Instance.CompletePrayer();
            UIManager.Instance.ClosePopupUI();
        }
        else
        {
            if (_currentIndex >= _candidates.Count)
            {
                _currentIndex = _candidates.Count - 1;
            }
            RefreshCandidateUI();

            ShowToast(DataManager.Instance.GetText("UI_RecruitmentResultPopup_Toast_Success"));
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
            yield return new WaitForSeconds(3.0f);
            toastText.gameObject.SetActive(false);
        }
        _coToast = null;
    }

    private void OnCloseClicked()
    {
        UIManager.Instance.ShowPopupUI<WarningEndRecruitPopup>();
    }
}