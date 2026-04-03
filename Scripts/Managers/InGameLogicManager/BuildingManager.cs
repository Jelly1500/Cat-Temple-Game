using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 건물/시설 관리 전담
/// - 건물 목록 관리
/// - 건설/업그레이드
/// - 건물 효과 계산
/// 
/// [설계 원칙]
/// 1. 자체 데이터(_data)를 완전히 소유
/// 2. 건설 요청은 Try* 패턴으로 처리
/// 3. 자원 차감은 GameDataManager에 위임
/// 
/// [망치 관리 책임]
/// BuildingManager는 "몇 개의 망치가 건설에 투입되어 있는지"를 소유합니다.
/// - HammersInUse: 현재 진행 중인 건설에 투입된 망치 수 (현재 동시 건설 1개 = 0 또는 1)
/// - 건설 시작 시: HammersInUse 증가 (currentConstruction 생성)
/// - 건설 완료/취소 시: HammersInUse 감소 (currentConstruction = null)
/// 
/// GameDataManager는 이 값을 참조하여 AvailableHammers를 계산합니다.
/// </summary>
public class BuildingManager : Singleton<BuildingManager>, ISaveable
{
    #region Data

    private BuildingSystemData _data = new BuildingSystemData();

    #endregion

    #region Read-Only Properties

    /// <summary>보유 건물 목록 (읽기 전용)</summary>
    public IReadOnlyList<BuildingData> Facilities => _data.facilities;

    /// <summary>현재 건설 중인지 여부</summary>
    /// <summary>건설 진행 중인 건물이 하나라도 있는지</summary>
    public bool IsConstructing => _data.activeConstructions.Count > 0;

    /// <summary>현재 건설에 투입된 망치 수 = 진행 중인 건설 수</summary>
    public int HammersInUse => _data.activeConstructions.Count;

    /// <summary>특정 건물이 건설 중인지 확인</summary>
    public bool IsBuildingUnderConstruction(int buildingId)
    {
        return _data.activeConstructions.Exists(c => c.buildingId == buildingId);
    }
    public ConstructionInfo GetConstructionInfo(int buildingId)
    {
        var data = _data.activeConstructions.Find(c => c.buildingId == buildingId);
        return data != null ? new ConstructionInfo(data) : null;
    }

    #endregion

    #region Initialization

    public void Init()
    {
        SaveManager.Instance.Register(this);
        EventManager.Instance.AddEvent(EEventType.DateChanged, OnDateChanged);
    }

    #endregion

    #region Event Handlers

    private void OnDateChanged()
    {
        CheckConstructionCompletion();
    }

    #endregion

    #region Building Data Access

    /// <summary>
    /// 건물 데이터 조회
    /// </summary>
    public BuildingData GetBuildingData(int buildingId)
    {
        return _data.facilities.Find(b => b.buildingId == buildingId);
    }

    /// <summary>
    /// 건물 레벨 조회 (없으면 0)
    /// </summary>
    public int GetBuildingLevel(int buildingId)
    {
        var building = GetBuildingData(buildingId);
        return building?.currentLevel ?? 0;
    }

    /// <summary>
    /// 건물 보유 여부
    /// </summary>
    public bool HasBuilding(int buildingId)
    {
        return GetBuildingData(buildingId) != null;
    }

    #endregion

    #region Construction Commands

    /// <summary>
    /// 건설 시작 시도
    /// 
    /// [망치 흐름]
    /// 1. GameDataManager.CanUseHammer()로 사용 가능한 망치가 있는지 확인
    /// 2. currentConstruction을 생성 → HammersInUse가 0→1로 증가
    /// 3. GameDataManager.NotifyHammerChanged()로 UI 갱신
    /// 
    /// 망치를 "차감"하는 것이 아니라, 건설 슬롯을 "점유"하는 개념입니다.
    /// </summary>
    public ConstructionResult TryStartConstruction(int buildingId, int targetLevel, int cost, int durationDays)
    {
        // 1. 이미 건설 중인지 확인
        if (IsBuildingUnderConstruction(buildingId))
            return ConstructionResult.Fail(EConstructionFailReason.AlreadyConstructing);

        // 2. 사용 가능한 망치가 있는지 확인
        if (!GameDataManager.Instance.CanUseHammer())
            return ConstructionResult.Fail(EConstructionFailReason.NotEnoughHammer);

        // 3. 골드 차감 시도
        if (!GameDataManager.Instance.TrySpendGold(cost))
        {
            return ConstructionResult.Fail(EConstructionFailReason.NotEnoughGold);
        }

        // 4. 건설 데이터 생성 → 이 순간 HammersInUse가 0→1로 변경됨
        TimeManager.Instance.CalculateFutureDate(durationDays, out int year, out int month, out int day);

        _data.activeConstructions.Add(new ConstructionData 
        { 
            buildingId = buildingId,
            targetLevel = targetLevel,
            endYear = year,
            endMonth = month,
            endDay = day
        });

        // 5. 저장 및 이벤트
        SaveManager.Instance.Save();
        GameDataManager.Instance.NotifyHammerChanged();
        EventManager.Instance.TriggerEvent(EEventType.ConstructionStarted);

        return ConstructionResult.Success();
    }

    /// <summary>
    /// 건설 취소
    /// 
    /// [망치 흐름]
    /// currentConstruction = null → HammersInUse가 1→0으로 감소
    /// 별도의 "망치 반환" 호출 불필요 (슬롯 해제 = 자동 반환)
    /// </summary>
    public void CancelConstruction(int buildingId, float refundRate = 0.5f)
    {
        var construction = _data.activeConstructions.Find(c => c.buildingId == buildingId);
        if (construction == null) return;

        // 골드 환불
        var sheet = DataManager.Instance.GetBuildingSheet(construction.buildingId);
        if (sheet != null)
        {
            int constructionCost = sheet.GetLevelInfo(construction.targetLevel).cost;
            int refundAmount = Mathf.FloorToInt(constructionCost * refundRate);
            GameDataManager.Instance.AddGold(refundAmount);
        }

        _data.activeConstructions.Remove(construction);

        SaveManager.Instance.Save();
        GameDataManager.Instance.NotifyHammerChanged();
        EventManager.Instance.TriggerEvent(EEventType.ConstructionCancelled);
    }

    #endregion

    #region Construction Completion

    private void CheckConstructionCompletion()
    {
        for (int i = _data.activeConstructions.Count - 1; i >= 0; i--)
        {
            var construction = _data.activeConstructions[i];
            if (TimeManager.Instance.IsDateReached(
                construction.endYear, construction.endMonth, construction.endDay))
            {
                CompleteConstruction(construction);
            }
        }
    }

    private void CompleteConstruction(ConstructionData construction)
    {
        var building = GetBuildingData(construction.buildingId);

        if (building == null)
        {
            building = new BuildingData
            {
                buildingId = construction.buildingId,
                currentLevel = 0
            };
            _data.facilities.Add(building);
        }

        building.currentLevel = construction.targetLevel;
        _data.activeConstructions.Remove(construction);

        SaveManager.Instance.Save();

        // [수정됨] 기본 망치 갱신 알림
        GameDataManager.Instance.NotifyHammerChanged();

        // [신규] 효과 타입에 따른 즉시 갱신 이벤트 트리거
        // 최대 레벨 달성 등 상태 변화 시, 수동적인 스탯(최대치 등)이 즉시 반영되도록 합니다.
        if (building.ActiveEffectType == EBuildingEffectType.IncreaseMaxDiscipleCount)
        {
            EventManager.Instance.TriggerEvent(EEventType.DiscipleCapacityChanged);
        }
        else if (building.ActiveEffectType == EBuildingEffectType.IncreaseMaxHammer)
        {
            // 망치 최대치가 늘어났으므로 다시 한번 갱신 (UI 즉시 반영용)
            GameDataManager.Instance.NotifyHammerChanged();
        }

        EventManager.Instance.TriggerEvent(EEventType.ConstructionCompleted);
    }

    public void ForceCompleteAllConstructions()
    {
        // 리스트의 요소가 삭제되므로 역순으로 순회하여 즉시 완료 처리
        for (int i = _data.activeConstructions.Count - 1; i >= 0; i--)
        {
            CompleteConstruction(_data.activeConstructions[i]);
        }
    }

    #endregion

    #region Effect Calculation

    /// <summary>
    /// 특정 효과 타입의 총합 계산
    /// </summary>
    public float GetTotalEffectValue(EBuildingEffectType effectType)
    {
        float total = 0f;

        foreach (var building in _data.facilities)
        {
            if (building.ActiveEffectType == effectType)
            {
                total += building.CurrentEffectValue;
            }
        }

        return total;
    }

    /// <summary>
    /// 특정 효과를 가진 건물 목록 조회
    /// </summary>
    public List<BuildingData> GetBuildingsWithEffect(EBuildingEffectType effectType)
    {
        return _data.facilities.FindAll(b => b.ActiveEffectType == effectType);
    }

    #endregion

    #region ISaveable Implementation

    public void SaveTo(GameData data)
    {
        data.buildingSystem = _data;
    }

    public void LoadFrom(GameData data)
    {
        _data = data.buildingSystem ?? new BuildingSystemData();
    }

    public void ResetToDefault()
    {
        _data = new BuildingSystemData();

        var allSheets = DataManager.Instance.GetAllBuildingSheets();

        foreach (var sheet in allSheets)
        {
            int initLevel = 0;

            // 작업실(IncreaseMaxHammer)은 초기 1레벨 (기본 망치 1개 제공)
            if (sheet.effectType == EBuildingEffectType.IncreaseMaxHammer)
                initLevel = 1;

            BuildingData newBuilding = new BuildingData
            {
                buildingId = sheet.buildingId,
                currentLevel = initLevel
            };

            _data.facilities.Add(newBuilding);
        }

    }

    #endregion
}

#region Result Types

/// <summary>
/// 건설 시작 결과
/// </summary>
public class ConstructionResult
{
    public bool IsSuccess { get; private set; }
    public EConstructionFailReason FailReason { get; private set; }

    private ConstructionResult() { }

    public static ConstructionResult Success()
    {
        return new ConstructionResult { IsSuccess = true };
    }

    public static ConstructionResult Fail(EConstructionFailReason reason)
    {
        return new ConstructionResult { IsSuccess = false, FailReason = reason };
    }
}

public enum EConstructionFailReason
{
    None,
    AlreadyConstructing,
    NotEnoughGold,
    NotEnoughHammer
}

/// <summary>
/// 현재 건설 정보 (읽기 전용 래퍼)
/// </summary>
public class ConstructionInfo
{
    public int BuildingId { get; }
    public int TargetLevel { get; }
    public int EndYear { get; }
    public int EndMonth { get; }
    public int EndDay { get; }

    public string EndDateString => $"{EndYear}년 {EndMonth}월 {EndDay}일";

    public ConstructionInfo(ConstructionData data)
    {
        BuildingId = data.buildingId;
        TargetLevel = data.targetLevel;
        EndYear = data.endYear;
        EndMonth = data.endMonth;
        EndDay = data.endDay;
    }
}

#endregion

#region Data Classes

/// <summary>
/// 건물 시스템 저장 데이터
/// </summary>
[System.Serializable]
public class BuildingSystemData
{
    public List<BuildingData> facilities = new List<BuildingData>();
    public List<ConstructionData> activeConstructions = new List<ConstructionData>();
}

/// <summary>
/// 건설 중인 건물 데이터
/// </summary>
[System.Serializable]
public class ConstructionData
{
    public int buildingId;
    public int targetLevel;
    public int endYear;
    public int endMonth;
    public int endDay;
}

#endregion