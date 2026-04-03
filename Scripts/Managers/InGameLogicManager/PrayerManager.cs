using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 기도 시스템 관리 전담
/// - 기도 상태 관리 (시작/진행/완료)
/// - 기도 후보자 생성 및 영입
/// 
/// [설계 원칙]
/// 1. 자체 데이터(_data)를 완전히 소유
/// 2. 외부에는 읽기 전용 프로퍼티와 명령형 메서드만 노출
/// 3. 자원 차감은 GameDataManager에 요청
/// </summary>
public class PrayerManager : Singleton<PrayerManager>, ISaveable
{
    #region Data

    private PrayerSystemData _data = new PrayerSystemData();

    // 런타임 전용 (저장 X) - 기도 완료 시 생성되는 후보자 리스트
    private List<DiscipleDataSheet> _currentCandidates = new List<DiscipleDataSheet>();

    #endregion

    #region Read-Only Properties (외부 조회용)

    /// <summary>현재 기도 중인지 여부</summary>
    public bool IsPraying => _data.isPraying;

    /// <summary>기도 결과를 수령할 수 있는 상태인지</summary>
    public bool IsResultReady => _data.isPraying && _data.remainingDays <= 0;

    /// <summary>현재 기도의 남은 일수</summary>
    public int RemainingDays => _data.remainingDays;

    /// <summary>현재 기도의 총 기간</summary>
    public int TotalDays => _data.totalDays;

    /// <summary>기도 종료 예정 날짜 문자열</summary>
    public string EndDateString => _data.endDateString;

    /// <summary>현재 후보자 리스트 (읽기 전용)</summary>
    public IReadOnlyList<DiscipleDataSheet> CurrentCandidates => _currentCandidates;

    /// <summary>기도 진행률 (0.0 ~ 1.0)</summary>
    public float Progress
    {
        get
        {
            if (!_data.isPraying || _data.totalDays <= 0) return 0f;
            return Mathf.Clamp01(1f - ((float)_data.remainingDays / _data.totalDays));
        }
    }

    #endregion

    #region Initialization

    public void Init()
    {
        // 날짜 변경 이벤트 구독
        EventManager.Instance.AddEvent(EEventType.DateChanged, OnDateChanged);
        SaveManager.Instance.Register(this);
    }

    #endregion

    #region Event Handlers

    private void OnDateChanged()
    {
        if (!_data.isPraying) return;

        _data.remainingDays--;

        if (_data.remainingDays <= 0)
        {
            _data.remainingDays = 0;
            // 기도 완료 이벤트 발생 (UI 갱신용)
            EventManager.Instance.TriggerEvent(EEventType.PrayerCompleted);
        }

        // 기도 상태 변경 이벤트
        EventManager.Instance.TriggerEvent(EEventType.PrayerUpdated);
    }

    #endregion

    #region Public Commands (행동 요청)

    /// <summary>
    /// 기도 시작 시도
    /// </summary>
    /// <returns>성공 여부와 실패 사유</returns>
    public PrayerResult TryStartPrayer(int prayerId)
    {
        // 1. 데이터 검증
        var prayerSheet = DataManager.Instance.GetPrayerSheet(prayerId);
        if (prayerSheet == null)
        {
            return PrayerResult.Fail(EPrayerFailReason.InvalidPrayer);
        }

        // 2. 이미 기도 중인지 확인
        if (_data.isPraying)
        {
            return PrayerResult.Fail(EPrayerFailReason.AlreadyPraying);
        }

        // 3. 비용 확인 및 차감 (GameDataManager에 위임)
        if (!GameDataManager.Instance.TrySpendGold(prayerSheet.cost))
        {
            return PrayerResult.Fail(EPrayerFailReason.NotEnoughGold);
        }

        // 4. 기도 상태 설정
        _data.isPraying = true;
        _data.currentPrayerId = prayerId;
        _data.totalDays = prayerSheet.durationDays;
        _data.remainingDays = prayerSheet.durationDays;

        // 5. 종료 날짜 계산
        TimeManager.Instance.CalculateFutureDate(
            prayerSheet.durationDays,
            out int endYear,
            out int endMonth,
            out int endDay
        );
        _data.endDateString = $"{endYear}년 {endMonth}월 {endDay}일";

        // 6. 저장 및 이벤트
        SaveManager.Instance.Save();
        EventManager.Instance.TriggerEvent(EEventType.PrayerStarted);

        return PrayerResult.Success();
    }

    /// <summary>
    /// 기도 포기
    /// </summary>
    public void AbandonPrayer()
    {
        if (!_data.isPraying) return;

        ResetPrayerState();
        EventManager.Instance.TriggerEvent(EEventType.PrayerAbandoned);
    }

    /// <summary>
    /// 기도 완료 후 후보자 생성
    /// </summary>
    public void GenerateCandidates()
    {
        _currentCandidates.Clear();

        if (!IsResultReady) return;

        var prayerSheet = GetCurrentPrayerSheet();
        if (prayerSheet == null) return;

        // 등급 범위에 해당하는 제자 풀 가져오기
        var pool = DataManager.Instance.GetDisciplesByGradeRange(
            prayerSheet.minGrade,
            prayerSheet.maxGrade
        );

        if (pool.Count == 0)
        {
            Debug.LogWarning("[PrayerManager] 해당 등급의 제자가 없습니다.");
            return;
        }

        // 후보자 수만큼 랜덤 선택
        for (int i = 0; i < prayerSheet.candidateCount; i++)
        {
            int randIdx = Random.Range(0, pool.Count);
            _currentCandidates.Add(pool[randIdx]);
        }

    }

    /// <summary>
    /// 특정 후보자 영입
    /// </summary>
    public bool TryRecruitCandidate(DiscipleDataSheet candidateSheet)
    {
        if (candidateSheet == null) return false;
        if (!_currentCandidates.Contains(candidateSheet)) return false;

        // DiscipleManager에 영입 위임
        var newDisciple = DiscipleManager.Instance.CreateAndAddNewDisciple(candidateSheet.templateId);

        if (newDisciple == null) return false;

        // 후보자 목록에서 제거
        _currentCandidates.Remove(candidateSheet);

        EventManager.Instance.TriggerEvent(EEventType.DiscipleRecruited);
        return true;
    }

    public void CompletePrayer()
    {
        ResetPrayerState();
    }

    public void ForceCompletePrayer()
    {
        // 남은 일수를 0으로 조작하여 IsResultReady를 true로 만듦
        _data.remainingDays = 0;

        SaveManager.Instance.Save();

        // UI_MainGame의 RedDot 갱신 등을 위해 이벤트 트리거
        EventManager.Instance.TriggerEvent(EEventType.PrayerCompleted);
        EventManager.Instance.TriggerEvent(EEventType.PrayerUpdated);
    }

    #endregion

    #region Data Getters

    /// <summary>
    /// 현재 진행 중인 기도의 데이터 시트
    /// </summary>
    public PrayerDataSheet GetCurrentPrayerSheet()
    {
        if (!_data.isPraying || _data.currentPrayerId == -1) return null;
        return DataManager.Instance.GetPrayerSheet(_data.currentPrayerId);
    }

    /// <summary>
    /// 전체 기도 목록 (UI 표시용)
    /// </summary>
    public List<PrayerDataSheet> GetAllPrayerSheets()
    {
        if (DataManager.Instance == null) return new List<PrayerDataSheet>();

        var list = DataManager.Instance.GetAllPrayerSheets();
        list.Sort((a, b) => a.id.CompareTo(b.id));
        return list;
    }

    #endregion

    #region Private Methods

    private void ResetPrayerState()
    {
        _data.isPraying = false;
        _data.currentPrayerId = -1;
        _data.totalDays = 0;
        _data.remainingDays = 0;
        _data.endDateString = "";

        _currentCandidates.Clear();

        SaveManager.Instance.Save();
    }

    #endregion

    #region ISaveable Implementation

    public void SaveTo(GameData data)
    {
        data.prayerSystem = _data;
    }

    public void LoadFrom(GameData data)
    {
        _data = data.prayerSystem ?? new PrayerSystemData();
        _currentCandidates.Clear();
    }

    public void ResetToDefault()
    {
        _data = new PrayerSystemData();
        _currentCandidates.Clear();
    }

    #endregion
}

#region Result Types

/// <summary>
/// 기도 시작 결과
/// </summary>
public class PrayerResult
{
    public bool IsSuccess { get; private set; }
    public EPrayerFailReason FailReason { get; private set; }

    private PrayerResult() { }

    public static PrayerResult Success()
    {
        return new PrayerResult { IsSuccess = true };
    }

    public static PrayerResult Fail(EPrayerFailReason reason)
    {
        return new PrayerResult { IsSuccess = false, FailReason = reason };
    }
}

public enum EPrayerFailReason
{
    None,
    InvalidPrayer,
    AlreadyPraying,
    NotEnoughGold
}

#endregion

#region Data Classes

/// <summary>
/// 기도 시스템 저장 데이터
/// </summary>
[System.Serializable]
public class PrayerSystemData
{
    public bool isPraying = false;
    public int currentPrayerId = -1;
    public int totalDays = 0;
    public int remainingDays = 0;
    public string endDateString = "";
}

#endregion