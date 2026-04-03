using Newtonsoft.Json;
using System.Collections.Generic;
using static Define;

// ============================================================
// GameData.cs - 전체 게임 저장 데이터 컨테이너
// ============================================================

/// <summary>
/// 게임 전체 저장 데이터
/// 각 매니저의 데이터를 참조로 보유
/// SaveManager가 이 객체를 JSON으로 직렬화/역직렬화
/// </summary>
[System.Serializable]
public class GameData
{
    // === 시간 ===
    public TimeData time = new TimeData();

    // === 자원 ===
    public ResourceData resources = new ResourceData();

    // === 제자 ===
    public DiscipleSystemData discipleSystem = new DiscipleSystemData();

    // === 건물 ===
    public BuildingSystemData buildingSystem = new BuildingSystemData();

    // === 기도 ===
    public PrayerSystemData prayerSystem = new PrayerSystemData();

    // === 편지 (별도 구현 필요) ===
    public LetterSystemData letterSystem = new LetterSystemData();

    // === IAP (별도 구현 필요) ===
    public IAPData iap = new IAPData();

    // 설정 데이터 필드 추가
    public SettingsData settings;

    // === 인지도 이벤트 ===
    public int renownEventTarget = 0;

    public List<int> unlockedTrainingIds;
}

// ============================================================
// 각 시스템별 데이터 클래스들
// (이미 각 매니저 파일에 정의되어 있으나, 여기서 한눈에 볼 수 있도록 정리)
// ============================================================

// TimeData, ResourceData, DiscipleSystemData, BuildingSystemData, PrayerSystemData
// 는 각 매니저 파일 하단에 정의되어 있음

/// <summary>
/// 편지 시스템 저장 데이터 (별도 LetterManager에서 구현)
/// </summary>

[System.Serializable]
public class ScheduledLetter
{
    public string discipleId;
    public int weeklyReward;
    public int nextDeliveryDay;
}

/// <summary>
/// IAP 데이터 (별도 IAPManager에서 구현)
/// </summary>
[System.Serializable]
public class IAPData
{
    public bool removeAds = false;
    public bool HasPurchasedHammerBoost = false;
    public bool HasPurchasedCatExpansion = false;
    public bool HasPurchasedDonation = false;
}

// ============================================================
// DiscipleData (제자 개별 데이터) - 기존 구조 유지
// ============================================================

[System.Serializable]
public class DiscipleData
{
    public string id;
    public int templateId;
    public string name;

    public bool isRenamed = false;

    // 위치
    public int x;
    public int y;

    // 훈련 스탯
    public int trainingPatience;
    public int trainingEmpathy;
    public int trainingWisdom;
    public int trainingEnlighten;

    // 훈련 횟수 (훈련ID -> 횟수)
    public Dictionary<int, int> trainingCounts = new Dictionary<int, int>();

    // === Computed Properties (저장 X) ===

    /// <summary>템플릿 데이터 참조</summary>
    [JsonIgnore]
    public DiscipleDataSheet Template => DataManager.Instance?.GetDiscipleTemplate(templateId);

    /// <summary>총 인내 (기본 + 훈련)</summary>
    public int Patience => (Template?.basePatience ?? 0) + trainingPatience;

    /// <summary>총 공감 (기본 + 훈련)</summary>
    public int Empathy => (Template?.baseEmpathy ?? 0) + trainingEmpathy;

    /// <summary>총 지혜 (기본 + 훈련)</summary>
    public int Wisdom => (Template?.baseWisdom ?? 0) + trainingWisdom;

    public int Enlighten => trainingEnlighten;
}

// ============================================================
// BuildingData (건물 개별 데이터) - 기존 구조 유지
// ============================================================

[System.Serializable]
public class BuildingData
{
    public int buildingId;   // 건물 ID
    public int currentLevel; // 현재 레벨

    // 원본 데이터 시트 연결 (DataManager에서 조회)
    [JsonIgnore]
    public BuildingDataSheet Sheet => DataManager.Instance?.GetBuildingSheet(buildingId);

    // 건물 이름 (시트에서 조회)
    [JsonIgnore]
    public string Name => Sheet != null ? Sheet.buildingName : "";

    // 건물 효과 타입 (시트에서 조회)
    [JsonIgnore]
    public EBuildingEffectType ActiveEffectType => Sheet != null ? Sheet.effectType : EBuildingEffectType.None;

    // 현재 레벨의 효과 수치
    [JsonIgnore]
    public float CurrentEffectValue
    {
        get
        {
            if (Sheet == null || currentLevel <= 0) return 0f;
            var info = Sheet.GetLevelInfo(currentLevel);
            return info.effectValue;
        }
    }

    // 다음 레벨 업그레이드 비용
    [JsonIgnore]
    public int NextLevelCost
    {
        get
        {
            if (IsMaxLevel || Sheet == null) return 0;
            // 다음 레벨(currentLevel + 1)로 가기 위한 비용 조회
            var nextInfo = Sheet.GetLevelInfo(currentLevel + 1);
            return nextInfo.cost;
        }
    }

    // 다음 레벨 업그레이드 소요 일수
    [JsonIgnore]
    public int NextLevelDuration
    {
        get
        {
            if (IsMaxLevel || Sheet == null) return 0;
            var nextInfo = Sheet.GetLevelInfo(currentLevel + 1);
            return nextInfo.days;
        }
    }

    // 최대 레벨 도달 여부
    [JsonIgnore]
    public bool IsMaxLevel
    {
        get
        {
            if (Sheet == null) return true;
            return !Sheet.HasNextLevel(currentLevel);
        }
    }

    // 현재 건설/업그레이드 중인지 확인
    [JsonIgnore]
    public bool IsConstructing
    {
        get
        {
            return BuildingManager.Instance != null
                && BuildingManager.Instance.IsBuildingUnderConstruction(buildingId);
        }
    }

    // ==========================================
    // [3] 생성자
    // ==========================================
    public BuildingData()
    {
        // [중요] 생성자는 비워둡니다.
        // 기존에 여기서 리스트를 new List<>() 하거나 데이터를 채우던 코드는 모두 삭제해야 합니다.
    }
}

/// <summary>
/// 게임 설정 데이터 (언어, 사운드 등)
/// </summary>
[System.Serializable]
public class SettingsData
{
    public int languageIndex; // 0: Korean, 1: English ...
    public float masterVolume; // 0.0 ~ 1.0
}