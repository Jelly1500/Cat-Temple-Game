using UnityEngine;
using static Define;

/// <summary>
/// 게임 자원 관리 전담
/// - Gold (골드)
/// - Renown (인지도)
/// - Hammer (망치 포인트)
/// 
/// [설계 원칙]
/// 1. 자체 데이터(_data)를 완전히 소유
/// 2. 자원 변경은 이 매니저를 통해서만 가능
/// 3. Try* 패턴으로 실패 가능한 작업 처리
/// </summary>
public class GameDataManager : Singleton<GameDataManager>, ISaveable
{
    #region Data

    private ResourceData _data = new ResourceData();

    #endregion

    #region Read-Only Properties

    public int Gold => _data.gold;
    public int Renown => _data.renown;


    #endregion

    #region Initialization

    public void Init()
    {
        // 날짜 변경 시 망치 회복 (필요시)
        // EventManager.Instance.AddEvent(EEventType.DateChanged, OnDateChanged);
        SaveManager.Instance.Register(this);
    }

    #endregion

    #region Gold Operations

    /// <summary>
    /// 골드 추가
    /// </summary>
    public int AddGold(int amount) // [수정] void에서 int로 반환형 변경
    {
        if (amount <= 0) return 0;

        // 골드 버프가 활성화되어 있다면 획득량 2배 처리
        int actualAmount = CalculateExpectedGold(amount);

        _data.gold += actualAmount;
        EventManager.Instance.TriggerEvent(EEventType.GoldChanged);

        return actualAmount; // [추가] 실제 획득한 골드 반환
    }

    public int CalculateExpectedGold(int baseAmount)
    {
        if (baseAmount <= 0) return 0;
        return IsGoldBuffActive ? baseAmount * 2 : baseAmount;
    }

    /// <summary>
    /// 골드 사용 가능 여부 확인
    /// </summary>
    public bool CanAffordGold(int amount)
    {
        return _data.gold >= amount;
    }

    /// <summary>
    /// 골드 차감 시도
    /// </summary>
    /// <returns>성공 여부</returns>
    public bool TrySpendGold(int amount)
    {
        if (!CanAffordGold(amount)) return false;

        _data.gold -= amount;
        EventManager.Instance.TriggerEvent(EEventType.GoldChanged);
        return true;
    }

    /// <summary>
    /// 골드 강제 설정 (치트/테스트용)
    /// </summary>
    public void SetGold(int amount)
    {
        _data.gold = Mathf.Max(0, amount);
        EventManager.Instance.TriggerEvent(EEventType.GoldChanged);
    }

    #endregion

    #region Renown Operations

    /// <summary>
    /// 인지도 추가
    /// </summary>
    public void AddRenown(int amount)
    {
        if (amount <= 0) return;

        _data.renown += amount;
        EventManager.Instance.TriggerEvent(EEventType.RenownChanged);
    }

    #endregion

    #region Hammer Properties (Computed)

    /// <summary>
    /// 망치 총 상한 (건물 효과로 결정)
    /// 작업실 건물의 IncreaseMaxHammer 효과값 합산
    /// </summary>
    public int MaxHammerPoints
    {
        get
        {
            // 1. 건물 효과에 의한 기본 망치 수 계산
            int baseMax = Mathf.RoundToInt(
                BuildingManager.Instance.GetTotalEffectValue(EBuildingEffectType.IncreaseMaxHammer)
            );

            // 2. IAP 부스트 구매 여부 확인 후 보너스 합산 (예: +1)
            int iapBonus = IAPManager.Instance.IsPurchased(ProductIDs.HAMMER_BOOST) ? 1 : 0;

            return baseMax + iapBonus;
        }
    }

    /// <summary>
    /// 현재 건설에 투입된 망치 수 (BuildingManager에게 위임)
    /// </summary>
    public int HammersInUse => BuildingManager.Instance.HammersInUse;

    /// <summary>
    /// 현재 사용 가능한 망치 수 (총량 - 투입량)
    /// UI 표시 및 건설 가능 여부 판단에 사용
    /// </summary>
    public int AvailableHammers => Mathf.Max(0, MaxHammerPoints - HammersInUse);

    /// <summary>
    /// 망치가 최대치인지 여부 (투입된 것이 없는 상태)
    /// </summary>
    public bool IsHammerFull => HammersInUse == 0;

    #endregion

    #region Hammer Operations

    /// <summary>
    /// 망치 사용 가능 여부 확인
    /// 사용 가능한 망치가 요청량 이상인지 체크
    /// </summary>
    public bool CanUseHammer(int amount = 1)
    {
        return AvailableHammers >= amount;
    }

    /// <summary>
    /// 망치 상태 변경 알림
    /// 건설 시작/완료/취소 시 BuildingManager가 호출하여 UI 갱신을 트리거
    /// </summary>
    public void NotifyHammerChanged()
    {
        EventManager.Instance.TriggerEvent(EEventType.HammerChanged);
    }

    #endregion


    #region Ad Reward Buff

    /// <summary>
    /// 광고 보상(골드 2배) 활성화 여부 반환
    /// </summary>
    public bool IsGoldBuffActive
    {
        get
        {
            if (!_data.isGoldBuffActive) return false;

            // 기간이 만료되었는지 확인 (시간이 지났다면 버프 해제)
            if (TimeManager.Instance.IsDateReached(_data.buffEndYear, _data.buffEndMonth, _data.buffEndDay))
            {
                _data.isGoldBuffActive = false;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 지정된 일(Days) 수만큼 골드 버프 활성화
    /// </summary>
    public void ActivateGoldBuff(int durationDays)
    {
        TimeManager.Instance.CalculateFutureDate(durationDays, out _data.buffEndYear, out _data.buffEndMonth, out _data.buffEndDay);
        _data.isGoldBuffActive = true;
    }

    #endregion

    #region ISaveable Implementation

    public void SaveTo(GameData data)
    {
        data.resources = _data;
    }

    public void LoadFrom(GameData data)
    {
        _data = data.resources ?? new ResourceData();
    }

    public void ResetToDefault()
    {
        _data = new ResourceData
        {
            gold = 1000,
            renown = 0
        };
    }

    #endregion

    public void IncreaseAllDisciplesEnlightenment(int amount)
    {
        // 1. 데이터 업데이트 (DiscipleManager.Disciples 프로퍼티 활용)
        var discipleDataList = DiscipleManager.Instance.Disciples;

        foreach (var data in discipleDataList)
        {
            // 제자 데이터의 스탯 증가 (DiscipleData에 해당 필드가 있다고 가정)
            data.trainingEnlighten += amount;
        }

        UIManager.Instance.ShowGameToast($"모든 제자의 깨달음이 {amount}만큼 상승했습니다!");
    }
}



#region Data Classes

/// <summary>
/// 자원 저장 데이터
/// </summary>
[System.Serializable]
public class ResourceData
{
    public int gold;
    public int renown;
    public bool isGoldBuffActive;
    public int buffEndYear;
    public int buffEndMonth;
    public int buffEndDay;
}

#endregion