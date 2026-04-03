using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 제자 관리 전담
/// - 제자 데이터 목록 관리
/// - 제자 영입/하산
/// - 런타임 오브젝트 추적
/// 
/// [설계 원칙]
/// 1. 자체 데이터(_data)를 완전히 소유
/// 2. 제자 생성/삭제는 이 매니저를 통해서만 가능
/// 3. 오브젝트 스폰은 ObjectManager에 위임
/// </summary>
public class DiscipleManager : Singleton<DiscipleManager>, ISaveable
{
    #region Data

    private DiscipleSystemData _data = new DiscipleSystemData();

    // 런타임 오브젝트 캐시 (저장 X)
    private Dictionary<string, Disciple> _discipleObjects = new Dictionary<string, Disciple>();

    #endregion

    #region Read-Only Properties

    /// <summary>제자 데이터 목록 (읽기 전용)</summary>
    public IReadOnlyList<DiscipleData> Disciples => _data.disciples;

    /// <summary>현재 제자 수</summary>
    public int CurrentCount => _data.disciples.Count;

    /// <summary>최대 제자 수</summary>
    public int MaxCount
    {
        get
        {
            int buildingBonus = Mathf.RoundToInt(
                BuildingManager.Instance.GetTotalEffectValue(EBuildingEffectType.IncreaseMaxDiscipleCount)
            );
            return _data.maxDiscipleCount + buildingBonus;
        }
    }

    /// <summary>추가 영입 가능 여부</summary>
    public bool CanRecruit => CurrentCount < MaxCount;

    /// <summary>남은 영입 가능 수</summary>
    public int RemainingSlots => Mathf.Max(0, MaxCount - CurrentCount);

    #endregion

    #region Initialization

    public void Init()
    {
        _discipleObjects.Clear();
        SaveManager.Instance.Register(this);
    }

    #endregion

    #region Disciple Data Access

    /// <summary>
    /// ID로 제자 데이터 조회
    /// </summary>
    public DiscipleData GetDiscipleData(string id)
    {
        return _data.disciples.Find(d => d.id == id);
    }

    /// <summary>
    /// 템플릿 ID로 제자 데이터 조회 (첫 번째 매칭)
    /// </summary>
    public DiscipleData GetDiscipleByTemplateId(int templateId)
    {
        return _data.disciples.Find(d => d.templateId == templateId);
    }

    /// <summary>
    /// 조건에 맞는 제자 목록 조회
    /// </summary>
    public List<DiscipleData> GetDisciplesWhere(System.Predicate<DiscipleData> predicate)
    {
        return _data.disciples.FindAll(predicate);
    }

    #endregion

    #region Disciple Object Management

    /// <summary>
    /// 제자 오브젝트 등록 (스폰 시 호출)
    /// </summary>
    public void RegisterObject(string id, Disciple disciple)
    {
        if (string.IsNullOrEmpty(id) || disciple == null) return;
        _discipleObjects[id] = disciple;
    }

    // [호환성] 기존 ObjectManager 호출용 별칭
    public void RegisterDiscipleObject(string id, Disciple disciple) => RegisterObject(id, disciple);

    /// <summary>
    /// 제자 오브젝트 해제 (디스폰 시 호출)
    /// </summary>
    public void UnregisterObject(string id)
    {
        _discipleObjects.Remove(id);
    }

    // [호환성] 기존 ObjectManager 호출용 별칭
    public void UnregisterDiscipleObject(string id) => UnregisterObject(id);

    /// <summary>
    /// 제자 오브젝트 조회
    /// </summary>
    public Disciple GetObject(string id)
    {
        _discipleObjects.TryGetValue(id, out Disciple disciple);
        return disciple;
    }

    // [호환성] 기존 ObjectManager 호출용 별칭
    public Disciple GetDiscipleObject(string id) => GetObject(id);

    /// <summary>
    /// 모든 제자 오브젝트 조회
    /// </summary>
    public IEnumerable<Disciple> GetAllObjects()
    {
        return _discipleObjects.Values;
    }

    #endregion

    #region Disciple Creation

    /// <summary>
    /// 새 제자 생성 및 추가
    /// </summary>
    /// <returns>생성된 제자 데이터 (실패 시 null)</returns>
    public DiscipleData CreateAndAddNewDisciple(int templateId, Vector2Int? startPos = null)
    {
        // 1. 영입 가능 여부 확인
        if (!CanRecruit)
        {
            UIManager.Instance?.ShowGameToast("제자를 더 이상 받을 수 없습니다.");
            return null;
        }

        // 2. 템플릿 검증
        var template = DataManager.Instance.GetDiscipleTemplate(templateId);
        if (template == null)
        {
            Debug.LogError($"[DiscipleManager] 유효하지 않은 템플릿 ID: {templateId}");
            return null;
        }

        // 3. 위치 결정
        Vector2Int spawnPos = MapManager.Instance.GetRandomDiscipleSpawnPosition();

        // 4. 데이터 생성
        DiscipleData newData = new DiscipleData
        {
            id = System.Guid.NewGuid().ToString(),
            templateId = templateId,
            name = template.defaultName,

            trainingPatience = 0,
            trainingEmpathy = 0,
            trainingWisdom = 0,
            trainingEnlighten = 0,

            x = spawnPos.x,
            y = spawnPos.y
        };

        // 5. 리스트에 추가
        _data.disciples.Add(newData);

        // 6. 오브젝트 스폰 요청
        ObjectManager.Instance?.SpawnDisciple(newData);

        // 7. 이벤트 발생
        EventManager.Instance.TriggerEvent(EEventType.DiscipleCountChanged);

        SaveManager.Instance.Save();
        UIManager.Instance.RefreshAllActiveUI();

        return newData;
    }

    #endregion

    #region Disciple Removal

    /// <summary>
    /// 제자 제거 (데이터만)
    /// </summary>
    public bool RemoveDisciple(string id)
    {
        var disciple = _data.disciples.Find(d => d.id == id);
        if (disciple == null) return false;

        _data.disciples.Remove(disciple);
        UnregisterObject(id);

        EventManager.Instance.TriggerEvent(EEventType.DiscipleCountChanged);
        return true;
    }

    /// <summary>
    /// 제자 하산 처리 (깨달음 레벨에 따른 결과)
    /// </summary>
    public DiscipleDepartureResult ProcessDeparture(string id)
    {
        var data = GetDiscipleData(id);
        if (data == null) return null;

        // 결과 객체 생성
        var result = new DiscipleDepartureResult
        {
            discipleId = id,
            discipleName = data.name,
            templateId = data.templateId,
            enlightenLevel = data.trainingEnlighten,
            isSuccessful = data.trainingEnlighten >= 1
        };

        // 성공적 하산 시 정기 편지 예약
        if (result.isSuccessful)
        {
            result.weeklyReward = CalculateWeeklyReward(data);
            LetterManager.Instance?.ScheduleGraduationLetter(data, result.weeklyReward);
        }

        // 오브젝트 제거
        var obj = GetObject(id);
        if (obj != null)
        {
            ObjectManager.Instance?.Despawn(obj);
        }

        // 데이터 제거
        RemoveDisciple(id);

        EventManager.Instance.TriggerEvent(EEventType.DiscipleDeparted);
        return result;
    }

    public int CalculateWeeklyReward(DiscipleData data)
    {
        int statBonus = (data.Patience + data.Empathy + data.Wisdom);
        return statBonus * data.trainingEnlighten * 3;
    }

    #endregion

    #region Disciple Stat Management

    /// <summary>
    /// 보유 중인 모든 제자의 인내심(Patience) 수치를 증가시킵니다.
    /// </summary>
    public void IncreaseAllPatience(int amount)
    {
        // 값 검증만 수행 (널 체크는 에러 즉시 파악을 위해 생략)
        if (amount <= 0) return;

        foreach (var disciple in _data.disciples)
        {
            disciple.trainingPatience += amount;
        }

        // 스탯 변경 후 UI 갱신을 위해 이벤트 발생 및 저장
        EventManager.Instance.TriggerEvent(EEventType.TrainingCompleted);
        SaveManager.Instance.Save();
    }

    #endregion

    #region Max Count Management

    /// <summary>
    /// 최대 제자 수 증가 (IAP 등)
    /// </summary>
    public void IncreaseMaxCount(int amount)
    {
        _data.maxDiscipleCount += amount;
        EventManager.Instance.TriggerEvent(EEventType.DiscipleCapacityChanged);
    }

    /// <summary>
    /// 최대 제자 수 설정
    /// </summary>
    public void SetMaxCount(int count)
    {
        _data.maxDiscipleCount = Mathf.Max(1, count);
        EventManager.Instance.TriggerEvent(EEventType.DiscipleCapacityChanged);
    }

    #endregion

    #region Position Sync

    /// <summary>
    /// 모든 제자 오브젝트의 위치를 데이터에 동기화
    /// </summary>
    public void SyncAllPositions()
    {

        foreach (var disciple in _data.disciples)
        {
            if (_discipleObjects.TryGetValue(disciple.id, out Disciple obj))
            {
                // [수정] 월드 좌표(Transform)를 타일맵의 셀 좌표(Grid Cell)로 변환하여 저장
                Vector2Int cellPos = MapManager.Instance.WorldToCell(obj.transform.position);

                disciple.x = cellPos.x;
                disciple.y = cellPos.y;
            }
        }
    }

    #endregion

    #region ISaveable Implementation

    public void SaveTo(GameData data)
    {
        SyncAllPositions();
        data.discipleSystem = _data;
    }

    public void LoadFrom(GameData data)
    {
        _data = data.discipleSystem ?? new DiscipleSystemData();
        _discipleObjects.Clear();
    }

    public void ResetToDefault()
    {
        _data = new DiscipleSystemData();
        _discipleObjects.Clear();
    }

    #endregion
}

#region Result Types

/// <summary>
/// 제자 하산 결과
/// </summary>
public class DiscipleDepartureResult
{
    public string discipleId;
    public string discipleName;
    public int templateId;
    public int enlightenLevel;
    public bool isSuccessful;
    public int weeklyReward;
}

#endregion

#region Data Classes

/// <summary>
/// 제자 시스템 저장 데이터
/// </summary>
[System.Serializable]
public class DiscipleSystemData
{
    public List<DiscipleData> disciples = new List<DiscipleData>();
    public int maxDiscipleCount = 2;
}

#endregion