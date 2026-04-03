using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public interface IValidate
{
    bool Validate();
}

public interface IDataLoader<Key, Value> : IValidate
{
    Dictionary<Key, Value> MakeDict();
}

public class DataManager : Singleton<DataManager>, ISaveable
{
    private HashSet<IValidate> _loaders = new HashSet<IValidate>();

    public GameConfig GameConfig { get; private set; }
    public LocalizationConfig LocalizationConfig { get; private set; }
    public AdsConfig AdsConfig { get; private set; }
    public IAPConfig IAPConfig { get; private set; }

    public Dictionary<string, TextData> TextDict { get; private set; } = new Dictionary<string, TextData>();

    // 게임 내 데이터 시트 딕셔너리
    public Dictionary<int, DiscipleDataSheet> DiscipleTemplateDict { get; private set; } = new Dictionary<int, DiscipleDataSheet>();
    public Dictionary<int, BuildingDataSheet> BuildingSheetDict { get; private set; } = new Dictionary<int, BuildingDataSheet>();
    public Dictionary<string, LetterData> LetterDict { get; private set; } = new Dictionary<string, LetterData>();
    public Dictionary<int, PrayerDataSheet> PrayerSheetDict { get; private set; } = new Dictionary<int, PrayerDataSheet>();
    public Dictionary<int, TrainingDataSheet> TrainingSheetDict { get; private set; } = new Dictionary<int, TrainingDataSheet>();
    public Dictionary<int, VisitorDataSheet> VisitorSheetDict { get; private set; } = new Dictionary<int, VisitorDataSheet>();

    public SystemLanguage CurrentLanguage = SystemLanguage.Korean;

    private SettingsData _settingsData = new SettingsData();

    public bool IsLoaded { get; private set; } = false;

    public void Init()
    {
        LoadData();

        SaveManager.Instance.Register(this);
    }

    public void LoadData()
    {
        if (IsLoaded) return;
        IsLoaded = false;

        // 1. 설정 파일 로드
        AdsConfig = LoadScriptableObject<AdsConfig>("AdsConfig");
        Debug.Log($"[DataManager] AdsConfig 로드: {(AdsConfig != null ? "성공" : "실패")}");
        IAPConfig = LoadScriptableObject<IAPConfig>("IAPConfig");

        // 2. JSON 데이터 로드
        TextDict = new Dictionary<string, TextData>();

        // (1) ui 데이터
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_Common");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_MainGame");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_TempleUpgradeSelectPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_LetterHistoryPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_GameSettingPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_CatDeparturePopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_CatInfoPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_CatTrainingPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_RecruitPraySelectPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_PrayInProgressPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_RecruitmentResultPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_WarningEndRecruitPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_CatDepartureWarningPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_ConstructionCancelWarningPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_Shop");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_GameInfo");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_GameInfoListPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_CatNameEditPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_ExitPopup");
        LoadAndMergeText("PreLoad/Data/JsonData/TextData_UI_AdsRewardPopup");

        // (4) 그 외 데이터 (서식, 제자 이름, 건물 이름 등)
        LoadAndMergeText("PreLoad/Data/LetterData/TextData_Content");
        LoadAndMergeText("PreLoad/Data/DiscipleData/TextData_Disciple");
        LoadAndMergeText("PreLoad/Data/BuildingData/TextData_Building");
        LoadAndMergeText("PreLoad/Data/PrayerData/TextData_Prayer");
        LoadAndMergeText("PreLoad/Data/TrainingData/TextData_Training");
        LoadAndMergeText("PreLoad/Data/RenownEventData/TextData_DialogueEvent");

        // 3. ScriptableObject 데이터 시트 일괄 로드
        DiscipleTemplateDict = LoadSheetFiles<DiscipleDataSheet, int>("PreLoad/Data/DiscipleData", sheet => sheet.templateId);
        BuildingSheetDict = LoadSheetFiles<BuildingDataSheet, int>("PreLoad/Data/BuildingData", sheet => sheet.buildingId);
        PrayerSheetDict = LoadSheetFiles<PrayerDataSheet, int>("PreLoad/Data/PrayerData", sheet => sheet.id);
        TrainingSheetDict = LoadSheetFiles<TrainingDataSheet, int>("PreLoad/Data/TrainingData", sheet => sheet.id);
        VisitorSheetDict = LoadSheetFiles<VisitorDataSheet, int>("PreLoad/Data/VisitorData", sheet => sheet.id);

        Validate();
        IsLoaded = true;
    }
    public VisitorDataSheet GetRandomVisitorSheet(int currentRenown)
    {
        List<VisitorDataSheet> candidates = new List<VisitorDataSheet>();

        foreach (var sheet in VisitorSheetDict.Values)
        {
            // 인지도 조건이 맞으면 후보군에 추가
            if (currentRenown >= sheet.baseRenownReq)
            {
                candidates.Add(sheet);
            }
        }

        // 조건에 맞는게 없으면 null 리턴
        if (candidates.Count == 0) return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
    private void LoadAndMergeText(string path)
    {
        var loader = LoadJson<TextDataLoader, string, TextData>(path);
        if (loader != null)
        {
            MergeDictionary(TextDict, loader.MakeDict());
        }
    }
    private void MergeDictionary<K, V>(Dictionary<K, V> target, Dictionary<K, V> source)
    {
        foreach (var kvp in source)
        {
            if (target.ContainsKey(kvp.Key))
            {
                Debug.LogWarning($"[DataManager] 중복된 키 발견: {kvp.Key}. 덮어씁니다.");
                target[kvp.Key] = kvp.Value;
            }
            else
            {
                target.Add(kvp.Key, kvp.Value);
            }
        }
    }

    public BuildingDataSheet GetBuildingSheet(int id)
    {
        if (BuildingSheetDict.TryGetValue(id, out var sheet))
            return sheet;
        return null;
    }

    // 일관성 있는 SO 로더 함수
    // T: 시트 타입, K: 키 타입 (int or string)
    // path: Resources 경로
    // keySelector: 객체에서 키를 추출하는 람다 함수 (예: x => x.id)
    private Dictionary<K, T> LoadSheetFiles<T, K>(string path, System.Func<T, K> keySelector) where T : ScriptableObject
    {
        Dictionary<K, T> dict = new Dictionary<K, T>();

        // 해당 경로의 모든 SO 로드
        T[] sheets = Resources.LoadAll<T>(path);

        foreach (T sheet in sheets)
        {
            K key = keySelector(sheet);
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, sheet);
            }

        }

        return dict;
    }

    #region Helper Functions
    // 헬퍼 함수
    public string GetText(string id)
    {
        if (TextDict.TryGetValue(id, out TextData data))
        {
            return data.GetText(CurrentLanguage);
        }

        Debug.LogWarning($"[DataManager] 텍스트 ID를 찾을 수 없습니다: {id}");
        return id; // 키가 없으면 키 자체를 반환하여 디버깅 용이하게 함
    }
    public TrainingDataSheet GetTrainingSheet(int id)
    {
        if (TrainingSheetDict.TryGetValue(id, out var sheet))
            return sheet;
        return null;
    }
    public List<TrainingDataSheet> GetAllTrainingSheets()
    {
        return new List<TrainingDataSheet>(TrainingSheetDict.Values);
    }
    public DiscipleDataSheet GetDiscipleTemplate(int id)
    {
        if (DiscipleTemplateDict.TryGetValue(id, out var sheet))
            return sheet;
        return null;
    }

    public PrayerDataSheet GetPrayerSheet(int id)
    {
        if (PrayerSheetDict.TryGetValue(id, out var sheet))
            return sheet;
        return null;
    }

    public List<PrayerDataSheet> GetAllPrayerSheets()
    {
        return new List<PrayerDataSheet>(PrayerSheetDict.Values);
    }

    public List<DiscipleDataSheet> GetDisciplesByGradeRange(int minGrade, int maxGrade)
    {
        List<DiscipleDataSheet> list = new List<DiscipleDataSheet>();

        foreach (var sheet in DiscipleTemplateDict.Values)
        {
            if (sheet.grade >= minGrade && sheet.grade <= maxGrade)
            {
                list.Add(sheet);
            }
        }
        return list;
    }

    #endregion

    private T LoadScriptableObject<T>(string path) where T : ScriptableObject
    {
        T asset = ResourceManager.Instance.Get<T>(path);

        return asset;
    }

    private Loader LoadJson<Loader, Key, Value>(string path) where Loader : IDataLoader<Key, Value>
    {
        TextAsset textAsset = Resources.Load<TextAsset>(path);

        // 2. 파일이 없을 경우에 대한 방어 코드
        if (textAsset == null)
        {
            Debug.LogError($"[DataManager] 파일을 찾을 수 없습니다: {path}");
            return default(Loader);
        }

        // 3. JSON 역직렬화 수행
        Loader loader = JsonConvert.DeserializeObject<Loader>(textAsset.text);

        // 4. 유효성 검사 목록에 추가
        _loaders.Add(loader);

        return loader;
    }

    private bool Validate()
    {
        bool success = true;

        foreach (IValidate loader in _loaders)
        {
            if (loader.Validate() == false)
                success = false;
        }

        _loaders.Clear();

        return success;
    }

    public List<BuildingDataSheet> GetAllBuildingSheets()
    {
        if (BuildingSheetDict == null) return new List<BuildingDataSheet>();
        return new List<BuildingDataSheet>(BuildingSheetDict.Values);
    }

    #region ISaveable Implementation (설정 저장/로드 로직)

    public void SaveTo(GameData data)
    {
        // 1. 현재 언어를 인덱스 혹은 정수형으로 변환 (SystemLanguage는 Enum)
        _settingsData.languageIndex = (int)CurrentLanguage;

        // 3. 데이터 할당
        data.settings = _settingsData;
    }

    public void LoadFrom(GameData data)
    {
        // 데이터가 없으면 기본값 사용 (최초 실행 시)
        if (data.settings == null)
        {
            _settingsData = new SettingsData();

            // [핵심 로직] 기기 언어 확인하여 초기값 설정
            SystemLanguage deviceLang = Application.systemLanguage;

            if (deviceLang == SystemLanguage.Korean)
            {
                _settingsData.languageIndex = (int)SystemLanguage.Korean;
            }
            else if (deviceLang == SystemLanguage.Japanese)
            {
                _settingsData.languageIndex = (int)SystemLanguage.Japanese;
            }
            else
            {
                // 그 외 국가는 영어로 설정
                _settingsData.languageIndex = (int)SystemLanguage.English;
            }

            _settingsData.masterVolume = 1.0f;
        }
        else
        {
            _settingsData = data.settings;
        }

        // 1. 언어 설정 적용
        CurrentLanguage = (SystemLanguage)_settingsData.languageIndex;

        // 2. 사운드 설정 적용
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMasterVolume(_settingsData.masterVolume);
        }

        // [추가] 로드 직후 폰트 갱신 (FontManager가 존재한다고 가정)
        if (FontManager.Instance != null)
        {
            FontManager.Instance.RefreshFont(CurrentLanguage);
        }
    }

    public void ResetToDefault()
    {
        // 초기화 시에도 기기 언어 기반으로 재설정하려면 동일 로직 적용
        SystemLanguage deviceLang = Application.systemLanguage;
        CurrentLanguage = (deviceLang == SystemLanguage.Japanese) ? SystemLanguage.Japanese :
                          (deviceLang == SystemLanguage.Korean) ? SystemLanguage.Korean : SystemLanguage.English;

        _settingsData = new SettingsData
        {
            languageIndex = (int)CurrentLanguage,
            masterVolume = 1.0f
        };

        // 폰트 갱신
        if (FontManager.Instance != null)
        {
            FontManager.Instance.RefreshFont(CurrentLanguage);
        }
    }

    #endregion

    public void SetVolumeData(float volume)
    {
        _settingsData.masterVolume = volume;
    }

    
}