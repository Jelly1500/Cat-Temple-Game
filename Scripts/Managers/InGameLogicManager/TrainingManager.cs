using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 훈련 시스템 관리 전담
/// - 훈련 비용 계산
/// - 훈련 실행
/// - 훈련 해금 관리
/// 
/// [설계 원칙]
/// 1. 훈련 데이터는 별도 저장 필요 없음 (제자 데이터에 포함)
/// 2. 자원 차감은 GameDataManager에 위임
/// 3. 건물 효과는 BuildingManager에서 조회
/// 4. 해금 상태는 저장/로드 필요
/// </summary>
public class TrainingManager : Singleton<TrainingManager>, ISaveable
{
    #region Data

    // 해금된 훈련 ID 목록
    private HashSet<int> _unlockedTrainingIds = new HashSet<int>();

    #endregion

    #region Initialization

    public void Init()
    {
        SaveManager.Instance.Register(this);

        // 기본 해금 훈련 초기화
        InitializeDefaultUnlocks();
    }

    /// <summary>
    /// unlockedByDefault가 true인 훈련들을 기본 해금
    /// </summary>
    private void InitializeDefaultUnlocks()
    {
        var allSheets = DataManager.Instance?.GetAllTrainingSheets();
        if (allSheets == null) return;

        foreach (var sheet in allSheets)
        {
            if (sheet.unlockedByDefault && !_unlockedTrainingIds.Contains(sheet.id))
            {
                _unlockedTrainingIds.Add(sheet.id);
            }
        }
    }

    #endregion

    #region Training Unlock System

    /// <summary>
    /// 훈련 해금 여부 확인
    /// </summary>
    public bool IsTrainingUnlocked(int trainingId)
    {
        return _unlockedTrainingIds.Contains(trainingId);
    }

    /// <summary>
    /// 훈련 해금 (인지도 이벤트에서 호출)
    /// </summary>
    public void UnlockTraining(int trainingId)
    {
        if (_unlockedTrainingIds.Contains(trainingId))
        {
            return;
        }

        var sheet = GetTrainingSheet(trainingId);
        if (sheet == null)
        {
            return;
        }

        _unlockedTrainingIds.Add(trainingId);

        // 해금 알림 (토스트 메시지)
        string trainingName = DataManager.Instance.GetText(sheet.titleKey);
        if (string.IsNullOrEmpty(trainingName) || trainingName == sheet.titleKey)
        {
            trainingName = sheet.title;
        }

        string unlockMsg = DataManager.Instance.GetText("UI_Toast_TrainingUnlocked");
        if (string.IsNullOrEmpty(unlockMsg) || unlockMsg == "UI_Toast_TrainingUnlocked")
        {
            unlockMsg = "새로운 훈련법 해금: {0}";
        }

        UIManager.Instance.ShowGameToast(string.Format(unlockMsg, trainingName));


        // 저장
        SaveManager.Instance.Save();
    }

    #endregion

    #region Training Data Access

    /// <summary>
    /// 해금된 훈련 목록만 반환 (UI 표시용)
    /// </summary>
    public List<TrainingDataSheet> GetUnlockedTrainingSheets()
    {
        var allSheets = GetAllTrainingSheets();
        var unlockedSheets = new List<TrainingDataSheet>();

        foreach (var sheet in allSheets)
        {
            if (IsTrainingUnlocked(sheet.id))
            {
                unlockedSheets.Add(sheet);
            }
        }

        return unlockedSheets;
    }

    /// <summary>
    /// 전체 훈련 목록 (내부용)
    /// </summary>
    public List<TrainingDataSheet> GetAllTrainingSheets()
    {
        if (DataManager.Instance == null)
            return new List<TrainingDataSheet>();

        var list = DataManager.Instance.GetAllTrainingSheets();
        list.Sort((a, b) => a.id.CompareTo(b.id));
        return list;
    }

    /// <summary>
    /// 특정 훈련 데이터 조회
    /// </summary>
    public TrainingDataSheet GetTrainingSheet(int trainingId)
    {
        return DataManager.Instance?.GetTrainingSheet(trainingId);
    }

    #endregion

    #region Cost Calculation

    /// <summary>
    /// 훈련 실제 비용 계산 (건물 할인 + 횟수 증가 반영)
    /// </summary>
    public int CalculateCost(DiscipleData disciple, int trainingId)
    {
        var sheet = GetTrainingSheet(trainingId);
        if (sheet == null) return 0;

        return CalculateCostInternal(disciple, trainingId, sheet.baseCost, sheet.costIncreasePerCount);
    }

    /// <summary>
    /// 훈련 실제 비용 계산 (기본 비용 직접 전달)
    /// </summary>
    public int CalculateCost(DiscipleData disciple, int trainingId, int baseCost)
    {
        var sheet = GetTrainingSheet(trainingId);
        int costIncrease = sheet?.costIncreasePerCount ?? 30; // 기본값 30

        return CalculateCostInternal(disciple, trainingId, baseCost, costIncrease);
    }

    private int CalculateCostInternal(DiscipleData disciple, int trainingId, int baseCost, int costIncreasePerCount)
    {
        int trainCount = (disciple != null) ? GetTrainingCount(disciple, trainingId) : 0;
        int rawCost = baseCost + (trainCount * costIncreasePerCount);

        float totalDiscountPercent = 0f;

        if (BuildingManager.Instance != null)
        {
            foreach (var building in BuildingManager.Instance.Facilities)
            {
                if (building.Sheet != null &&
                    building.Sheet.effectType == Define.EBuildingEffectType.DiscountTrainingCost)
                {
                    totalDiscountPercent += building.CurrentEffectValue;
                }
            }
        }

        float multiplier = 1.0f - (totalDiscountPercent / 100.0f);
        multiplier = Mathf.Clamp(multiplier, 0.1f, 1.0f);

        int finalCost = Mathf.FloorToInt(rawCost * multiplier);

        return finalCost;
    }

    /// <summary>
    /// 특정 제자의 특정 훈련 횟수 조회
    /// </summary>
    public int GetTrainingCount(DiscipleData disciple, int trainingId)
    {
        if (disciple?.trainingCounts == null) return 0;

        disciple.trainingCounts.TryGetValue(trainingId, out int count);
        return count;
    }

    #endregion

    #region Training Execution

    /// <summary>
    /// 훈련 실행 시도
    /// </summary>
    public TrainingResult TryExecuteTraining(DiscipleData disciple, int trainingId)
    {
        // 1. 데이터 검증
        if (disciple == null)
        {
            return TrainingResult.Fail(ETrainingFailReason.InvalidDisciple);
        }

        var sheet = GetTrainingSheet(trainingId);
        if (sheet == null)
        {
            return TrainingResult.Fail(ETrainingFailReason.InvalidTraining);
        }

        // 1.5. 해금 여부 확인
        if (!IsTrainingUnlocked(trainingId))
        {
            return TrainingResult.Fail(ETrainingFailReason.NotUnlocked);
        }

        // 2. 비용 계산 및 차감
        int realCost = CalculateCost(disciple, trainingId, sheet.baseCost);

        if (!GameDataManager.Instance.TrySpendGold(realCost))
        {
            return TrainingResult.Fail(ETrainingFailReason.NotEnoughGold);
        }

        // 3. 능력치 적용
        disciple.trainingPatience += sheet.gainPatience;
        disciple.trainingEmpathy += sheet.gainEmpathy;
        disciple.trainingWisdom += sheet.gainWisdom;

        // 4. 깨달음 확률 계산
        bool gotEnlightenment = false;
        float enlightenChance = disciple.Template.enlightenGainProb + sheet.enlightenBonusProb;

        if (Random.Range(0f, 100f) < enlightenChance)
        {
            disciple.trainingEnlighten += 1;
            gotEnlightenment = true;
        }

        // 5. 훈련 횟수 증가
        IncrementTrainingCount(disciple, trainingId);

        // 6. 저장 및 이벤트
        SaveManager.Instance.Save();
        EventManager.Instance.TriggerEvent(EEventType.TrainingCompleted);

        return TrainingResult.Success(gotEnlightenment);
    }

    private void IncrementTrainingCount(DiscipleData disciple, int trainingId)
    {
        if (disciple.trainingCounts == null)
        {
            disciple.trainingCounts = new Dictionary<int, int>();
        }

        if (disciple.trainingCounts.ContainsKey(trainingId))
        {
            disciple.trainingCounts[trainingId]++;
        }
        else
        {
            disciple.trainingCounts[trainingId] = 1;
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 훈련 가능 여부 확인 (비용만 체크)
    /// </summary>
    public bool CanAffordTraining(DiscipleData disciple, int trainingId)
    {
        int cost = CalculateCost(disciple, trainingId);
        return GameDataManager.Instance.CanAffordGold(cost);
    }

    /// <summary>
    /// 훈련 가능한 목록 필터링
    /// </summary>
    public List<TrainingDataSheet> GetAffordableTrainings(DiscipleData disciple)
    {
        var unlockedTrainings = GetUnlockedTrainingSheets();
        return unlockedTrainings.FindAll(t => CanAffordTraining(disciple, t.id));
    }

    #endregion

    #region ISaveable Implementation

    public void SaveTo(GameData data)
    {
        data.unlockedTrainingIds = new List<int>(_unlockedTrainingIds);
    }

    public void LoadFrom(GameData data)
    {
        _unlockedTrainingIds.Clear();

        if (data.unlockedTrainingIds != null)
        {
            foreach (int id in data.unlockedTrainingIds)
            {
                _unlockedTrainingIds.Add(id);
            }
        }

        // 기본 해금 훈련 보장
        InitializeDefaultUnlocks();
    }

    public void ResetToDefault()
    {
        _unlockedTrainingIds.Clear();
        InitializeDefaultUnlocks();
    }

    #endregion
}

#region Result Types

/// <summary>
/// 훈련 결과
/// </summary>
public class TrainingResult
{
    public bool IsSuccess { get; private set; }
    public ETrainingFailReason FailReason { get; private set; }
    public bool GotEnlightenment { get; private set; }

    private TrainingResult() { }

    public static TrainingResult Success(bool gotEnlightenment = false)
    {
        return new TrainingResult
        {
            IsSuccess = true,
            GotEnlightenment = gotEnlightenment
        };
    }

    public static TrainingResult Fail(ETrainingFailReason reason)
    {
        return new TrainingResult
        {
            IsSuccess = false,
            FailReason = reason
        };
    }
}

public enum ETrainingFailReason
{
    None,
    InvalidDisciple,
    InvalidTraining,
    NotEnoughGold,
    NotUnlocked
}

#endregion