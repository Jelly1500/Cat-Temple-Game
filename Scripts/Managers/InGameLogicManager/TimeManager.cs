using UnityEngine;
using static Define;

/// <summary>
/// 게임 내 시간 관리 전담
/// - 날짜/시간 진행
/// - 날짜 계산 유틸리티
/// 
/// [설계 원칙]
/// 1. 자체 데이터(_data)를 완전히 소유
/// 2. 시간은 TimeManager만 변경 가능 (AdvanceTime, PassDay)
/// 3. 다른 매니저는 이벤트 구독으로 날짜 변경에 반응
/// </summary>
public class TimeManager : Singleton<TimeManager>, ISaveable
{
    #region Constants

    public const int DAYS_PER_MONTH = 30;
    public const int MONTHS_PER_YEAR = 12;
    public const int HOURS_PER_DAY = 24;
    public const int MINUTES_PER_HOUR = 60;

    #endregion

    #region Data

    private TimeData _data = new TimeData();

    #endregion

    #region Read-Only Properties

    public int Year => _data.year;
    public int Month => _data.month;
    public int Day => _data.day;
    public int Hour => _data.hour;
    public int Minute => _data.minute;

    /// <summary>현재 날짜를 총 일수로 환산</summary>
    public int TotalDays => (_data.year * MONTHS_PER_YEAR * DAYS_PER_MONTH)
                          + (_data.month * DAYS_PER_MONTH)
                          + _data.day;

    /// <summary>
    /// 하루 진행률 (0.0 ~ 1.0)
    /// UI에서 시계 프로그래스 바 등에 사용
    /// </summary>
    public float DayProgress => Mathf.Clamp01(_dayTimer / _secondsPerDay);

    /// <summary>
    /// 하루가 지나는 데 걸리는 현실 시간(초)
    /// </summary>
    public float SecondsPerDay => _secondsPerDay;

    #endregion

    [Header("Time Settings")]
    [Tooltip("하루가 지나가는 데 걸리는 현실 시간(초)")]
    [SerializeField] private float _secondsPerDay = 25.0f; // 25초
    private float _dayTimer = 0f;

    #region Unity Lifecycle

    // [핵심 수정] Update 루프 추가
    private void Update()
    {
        // 게임 중이 아니면 시간 정지
        if (!GameManager.Instance.IsPlaying) return;

        _dayTimer += Time.deltaTime;

        if (_dayTimer >= _secondsPerDay)
        {
            _dayTimer = 0f; // 타이머 초기화 (오차 누적 방지를 위해 -= _secondsPerDay를 써도 됨)

            // 하루 넘기기
            PassDay();
        }
    }

    #endregion

    #region Initialization

    public void Init()
    {
        // 현재는 특별한 초기화 없음
        SaveManager.Instance.Register(this);
    }

    #endregion

    #region Time Advancement

    /// <summary>
    /// 하루 경과 처리
    /// </summary>
    public void PassDay()
    {
        _data.day++;

        // 일 -> 월 오버플로우 처리
        if (_data.day > DAYS_PER_MONTH)
        {
            _data.day = 1;
            _data.month++;

            // 월 -> 년 오버플로우 처리
            if (_data.month > MONTHS_PER_YEAR)
            {
                _data.month = 1;
                _data.year++;
            }
        }

        // 날짜 변경 이벤트 (다른 매니저들이 구독)
        EventManager.Instance.TriggerEvent(EEventType.DateChanged);
    }

    #endregion

    #region Formatting

    /// <summary>
    /// 현재 날짜 문자열 (YYYY/MM/DD)
    /// </summary>
    public string GetDateString()
    {
        return $"{_data.year:D4}/{_data.month:D2}/{_data.day:D2}";
    }

    /// <summary>
    /// 현재 날짜 문자열 (한글)
    /// </summary>
    public string GetDateStringKorean()
    {
        return $"{_data.year}년 {_data.month}월 {_data.day}일";
    }

    /// <summary>
    /// 현재 시간 문자열 (HH:MM)
    /// </summary>
    public string GetTimeString()
    {
        return $"{_data.hour:D2}:{_data.minute:D2}";
    }

    #endregion

    #region Date Utilities

    /// <summary>
    /// N일 후 날짜 계산
    /// </summary>
    public void CalculateFutureDate(int daysLater, out int year, out int month, out int day)
    {
        year = _data.year;
        month = _data.month;
        day = _data.day + daysLater;

        while (day > DAYS_PER_MONTH)
        {
            day -= DAYS_PER_MONTH;
            month++;

            if (month > MONTHS_PER_YEAR)
            {
                month = 1;
                year++;
            }
        }
    }

    /// <summary>
    /// N일 후 날짜를 문자열로 반환
    /// </summary>
    public string GetFutureDateString(int daysLater)
    {
        CalculateFutureDate(daysLater, out int year, out int month, out int day);
        return $"{year}년 {month}월 {day}일";
    }

    /// <summary>
    /// 특정 날짜에 도달했는지 확인
    /// </summary>
    public bool IsDateReached(int targetYear, int targetMonth, int targetDay)
    {
        // 년도 비교
        if (_data.year > targetYear) return true;
        if (_data.year < targetYear) return false;

        // 월 비교
        if (_data.month > targetMonth) return true;
        if (_data.month < targetMonth) return false;

        // 일 비교
        return _data.day >= targetDay;
    }

    /// <summary>
    /// 두 날짜 사이의 일수 계산
    /// </summary>
    public int GetDaysBetween(int fromYear, int fromMonth, int fromDay,
                              int toYear, int toMonth, int toDay)
    {
        int fromTotal = (fromYear * MONTHS_PER_YEAR * DAYS_PER_MONTH)
                      + (fromMonth * DAYS_PER_MONTH)
                      + fromDay;

        int toTotal = (toYear * MONTHS_PER_YEAR * DAYS_PER_MONTH)
                    + (toMonth * DAYS_PER_MONTH)
                    + toDay;

        return toTotal - fromTotal;
    }

    /// <summary>
    /// 현재 날짜부터 특정 날짜까지의 남은 일수
    /// </summary>
    public int GetRemainingDays(int targetYear, int targetMonth, int targetDay)
    {
        return GetDaysBetween(_data.year, _data.month, _data.day,
                              targetYear, targetMonth, targetDay);
    }

    #endregion

    #region ISaveable Implementation

    public void SaveTo(GameData data)
    {
        data.time = _data;
    }

    public void LoadFrom(GameData data)
    {
        _data = data.time ?? new TimeData();
    }

    public void ResetToDefault()
    {
        _data = new TimeData();
    }

    #endregion
}

#region Data Classes

/// <summary>
/// 시간 저장 데이터
/// </summary>
[System.Serializable]
public class TimeData
{
    public int year = 2026;
    public int month = 1;
    public int day = 1;
    public int hour = 8;
    public int minute = 0;
}

#endregion