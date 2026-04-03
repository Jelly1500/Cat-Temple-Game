using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Define;

/// <summary>
/// 편지 시스템 관리 전담
/// - 편지 예약/도착 처리
/// - 편지 큐 관리
/// - 읽은 편지 히스토리
/// 
/// [설계 원칙]
/// 1. 자체 데이터(_data)를 완전히 소유
/// 2. GameManager.GameData 직접 접근 제거
/// 3. 자원 변경은 GameDataManager에 위임
/// </summary>
public class LetterManager : Singleton<LetterManager>, ISaveable
{
    #region Constants

    private const int VISITOR_LETTER_DELAY_DAYS = 1;
    private const int APOSTLE_LETTER_DELAY_DAYS = 3;
    private const int RECURRING_INTERVAL_DAYS = 3;

    private const int APOSTLE_TEMPLATE_COUNT = 4;

    #endregion

    #region Data

    private LetterSystemData _data = new LetterSystemData();

    // 런타임 전용 (저장 X) - 도착해서 읽기 대기 중인 편지들
    private Queue<LetterData> _letterQueue = new Queue<LetterData>();
    private List<string> _visitorTemplateIds = new List<string>();

    #endregion

    #region Read-Only Properties

    /// <summary>읽지 않은 편지가 있는지</summary>
    public bool HasNewLetter => _letterQueue.Count > 0;

    /// <summary>대기 중인 편지 수</summary>
    public int PendingLetterCount => _letterQueue.Count;

    /// <summary>읽은 방문객 편지 히스토리</summary>

    /// <summary>읽은 편지 ID 목록</summary>

    #endregion

    #region Initialization

    public void Init()
    {
        EventManager.Instance.AddEvent(EEventType.DateChanged, OnDateChanged);

        SaveManager.Instance.Register(this);
        LoadVisitorTemplates();
    }

    private void LoadVisitorTemplates()
    {
        _visitorTemplateIds.Clear();

        if (DataManager.Instance == null) return;

        // TextData_Content.json의 모든 키를 검사
        foreach (var key in DataManager.Instance.TextDict.Keys)
        {
            // 키 형식: LETTER_VISITOR_XX_XX_TITLE 인 것만 찾음 (TITLE을 기준으로 식별)
            if (key.StartsWith("LETTER_VISITOR_") && key.EndsWith("_TITLE"))
            {
                // "_TITLE" 뒷부분을 제거하여 Base ID 추출 (예: "LETTER_VISITOR_20_01")
                string baseId = key.Substring(0, key.Length - "_TITLE".Length);
                _visitorTemplateIds.Add(baseId);
            }
        }

    }

    #endregion

    #region Event Handlers

    private void OnDateChanged()
    {
        ProcessPendingLetters();
    }

    #endregion

    #region Letter Processing

    private void ProcessPendingLetters()
    {
        // 1. 방문객 편지 도착 체크
        ProcessIncomingVisitorLetters();

        // 2. 정기(제자) 편지 도착 체크
        ProcessRecurringLetters();
    }

    private void ProcessIncomingVisitorLetters()
    {
        if (_data.incomingLetters == null || _data.incomingLetters.Count == 0) return;

        List<VisitorLetterData> arrivedList = new List<VisitorLetterData>();

        foreach (var letter in _data.incomingLetters)
        {
            if (TimeManager.Instance.IsDateReached(letter.arrivalYear, letter.arrivalMonth, letter.arrivalDay))
            {
                arrivedList.Add(letter);
            }
        }

        foreach (var letter in arrivedList)
        {
            EnqueueLetter(letter);
            _data.incomingLetters.Remove(letter);
        }
    }

    private void ProcessRecurringLetters()
    {
        if (_data.pendingRecurringLetters == null || _data.pendingRecurringLetters.Count == 0) return;

        List<RecurringLetterInfo> arrivedList = new List<RecurringLetterInfo>();

        foreach (var info in _data.pendingRecurringLetters)
        {
            if (TimeManager.Instance.IsDateReached(info.arrivalYear, info.arrivalMonth, info.arrivalDay))
            {
                arrivedList.Add(info);
            }
        }

        foreach (var info in arrivedList)
        {
            ApostleLetterData letter = GenerateApostleLetter(info);
            EnqueueLetter(letter);
            _data.pendingRecurringLetters.Remove(info);

            // 다음 정기 편지 즉시 재예약
            // (팝업 닫기에 의존하지 않고 여기서 처리해야 플레이어가 편지를 열지 않아도 끊기지 않음)
            ScheduleRecurringLetter(info.discipleId, info.discipleName, info.templateId, info.weeklyReward, RECURRING_INTERVAL_DAYS);

        }
    }

    private void EnqueueLetter(LetterData letter)
    {
        if (letter == null) return;

        _letterQueue.Enqueue(letter);
        EventManager.Instance.TriggerEvent(EEventType.NewLetterArrived);
    }

    #endregion

    #region Letter Opening (UI에서 호출)

    /// <summary>
    /// 특정 편지 열기 (히스토리 팝업에서 사용)
    /// </summary>
    public void OpenSpecificLetter(LetterData data)
    {
        if (data == null) return;

        // 읽지 않은 편지인 경우 큐에서 제거
        if (IsLetterUnread(data.id))
        {
            RemoveFromQueue(data.id);

            // 인지도 증가
            GameDataManager.Instance.AddRenown(1);
            EventManager.Instance.TriggerEvent(EEventType.LetterRead);
        }

        // UI 팝업 표시
        ShowLetterPopup(data);
    }

    /// <summary>
    /// 대기 중인 편지 열기 (기존 메서드 - 호환성 유지)
    /// </summary>
    public void OpenLetter()
    {
        if (_letterQueue.Count == 0) return;

        LetterData data = _letterQueue.Dequeue();
        if (data == null) return;

        // 인지도 증가
        GameDataManager.Instance.AddRenown(1);

        // UI 팝업 표시
        ShowLetterPopup(data);

        EventManager.Instance.TriggerEvent(EEventType.LetterRead);
        SaveManager.Instance.Save();
    }

    private void RemoveFromQueue(string letterId)
    {
        // 큐를 리스트로 변환하여 특정 편지 제거 후 다시 큐로
        List<LetterData> tempList = _letterQueue.ToList();
        tempList.RemoveAll(x => x.id == letterId);

        _letterQueue.Clear();
        foreach (var letter in tempList)
        {
            _letterQueue.Enqueue(letter);
        }
    }

    private void ShowLetterPopup(LetterData data)
    {
        if (data is ApostleLetterData apostleData)
        {
            var popup = UIManager.Instance.ShowPopupUI<ApostleLetterPopup>();
            popup?.SetContent(apostleData);
        }
        else if (data is VisitorLetterData visitorData)
        {
            var popup = UIManager.Instance.ShowPopupUI<VisitorLetterPopup>();
            popup?.SetContent(visitorData);
        }
    }


    #endregion

    #region Letter Closing & Reward (Popup에서 호출)

    /// <summary>
    /// 제자 편지 닫기 및 보상 처리
    /// </summary>
    public void OnApostleLetterClosed(ApostleLetterData data)
    {
        if (data == null) return;

        // 보상 지급
        if (!data.isRewardClaimed && data.rewardAmount > 0)
        {
            GameDataManager.Instance.AddGold(data.rewardAmount);
            data.isRewardClaimed = true;
        }

        // [수정] 다음 정기 편지 재예약은 ProcessRecurringLetters()에서 도착 시 자동 처리됨.
        // 팝업 닫기 시 여기서 재예약하면 ProcessRecurringLetters()의 재예약과 중복되어
        // 편지가 2통씩 오는 버그가 발생하므로 제거.

        SaveManager.Instance.Save();
    }

    /// <summary>
    /// 방문객 편지 닫기 및 보상 처리
    /// </summary>
    public void OnVisitorLetterClosed(VisitorLetterData data)
    {
        if (data == null) return;

        // 보상 지급
        if (!data.isRewardClaimed && data.rewardAmount > 0)
        {
            GameDataManager.Instance.AddGold(data.rewardAmount);
            data.isRewardClaimed = true;
        }

        SaveManager.Instance.Save();
    }

    public void ReceiveVisitorLetterReward(VisitorLetterData letter)
    {
        if (letter == null || letter.isRewardClaimed) return;

        // 자원 지급 로직을 매니저로 가져옴
        int gold = letter.rewardAmount;
        int renown = 2; // 기본 인지도 보상

        GameDataManager.Instance.AddGold(gold);
        GameDataManager.Instance.AddRenown(renown);

        letter.isRewardClaimed = true;

        // 토스트 메시지 호출도 매니저가 담당 (혹은 UI가 담당해도 무방)
        UIManager.Instance.ShowGameToast("UI_Toast_GetGoldAndRenown", gold, renown);

        // 필요 시 저장
        SaveManager.Instance.Save();
    }

    #endregion

    #region Scheduling (예약)

    /// <summary>
    /// 후원 감사 편지 즉시 생성 (IAPManager에서 호출)
    /// 의문의 방문객이 보내는 감사 편지 컨셉
    /// </summary>
    public void ScheduleDonationThankYouLetter(int rewardAmount)
    {
        VisitorLetterData letter = new VisitorLetterData
        {
            id = System.Guid.NewGuid().ToString(),
            title = "LETTER_DONATION_TITLE",
            greeting = "LETTER_DONATION_GREETING",
            story = "LETTER_DONATION_STORY",
            empathy = "LETTER_DONATION_EMPATHY",
            wisdom = "LETTER_DONATION_WISDOM",
            thanks = "LETTER_DONATION_THANKS",
            rewardAmount = rewardAmount,
            // 즉시 도착
            arrivalYear = TimeManager.Instance.Year,
            arrivalMonth = TimeManager.Instance.Month,
            arrivalDay = TimeManager.Instance.Day
        };

        // 즉시 큐에 추가
        EnqueueLetter(letter);

        SaveManager.Instance.Save();
    }

    /// <summary>
    /// 방문객 편지 예약 (InteractionManager에서 호출)
    /// </summary>
    public void ScheduleVisitorLetter(Visitor visitor, int rewardAmount)
    {
        // [수정] 규칙에 따라 Null 체크 로직 제거

        // 템플릿이 로드되지 않았다면 다시 로드 시도
        if (_visitorTemplateIds.Count == 0) LoadVisitorTemplates();

        // 1. 편지 데이터 생성
        VisitorLetterData newLetter = new VisitorLetterData();
        newLetter.id = System.Guid.NewGuid().ToString();
        newLetter.rewardAmount = rewardAmount;

        string selectedTemplateId = "";

        if (_visitorTemplateIds.Count > 0)
        {
            // [수정] 순회 인덱스가 템플릿 총 개수보다 작을 경우 순차 선택
            if (_data.lastVisitorTemplateIndex < _visitorTemplateIds.Count)
            {
                selectedTemplateId = _visitorTemplateIds[_data.lastVisitorTemplateIndex];
                _data.lastVisitorTemplateIndex++; // 다음 순서를 위해 인덱스 증가
            }
            else
            {
                // 인덱스를 모두 돌았다면 무작위로 선택
                int randomIndex = Random.Range(0, _visitorTemplateIds.Count);
                selectedTemplateId = _visitorTemplateIds[randomIndex];
            }
        }
        else
        {
            Debug.LogWarning("[LetterManager] 방문객 편지 템플릿을 찾을 수 없습니다. 기본값 사용.");
            selectedTemplateId = "LETTER_VISITOR_20_01";
        }

        newLetter.title = $"{selectedTemplateId}_TITLE";
        newLetter.greeting = $"{selectedTemplateId}_GREETING";
        newLetter.story = $"{selectedTemplateId}_STORY";
        newLetter.empathy = $"{selectedTemplateId}_EMPATHY";
        newLetter.wisdom = $"{selectedTemplateId}_WISDOM";
        newLetter.thanks = $"{selectedTemplateId}_THANKS";

        // 4. 도착 날짜 계산
        TimeManager.Instance.CalculateFutureDate(
            VISITOR_LETTER_DELAY_DAYS,
            out newLetter.arrivalYear,
            out newLetter.arrivalMonth,
            out newLetter.arrivalDay
        );

        // 5. 예약 목록에 추가 (저장 가능한 _data.incomingLetters에 넣어야 앱 재시작 후에도 유지됨)
        //    날짜가 되면 ProcessIncomingVisitorLetters()가 _letterQueue로 옮김
        if (_data.incomingLetters == null)
            _data.incomingLetters = new List<VisitorLetterData>();

        _data.incomingLetters.Add(newLetter);

        // 인덱스 갱신 및 새로운 예약이 생겼으므로 강제 저장
        SaveManager.Instance.Save();
    }

    /// <summary>
    /// 제자 하산 편지 예약 (DiscipleManager에서 호출)
    /// </summary>
    public void ScheduleGraduationLetter(DiscipleData data, int reward)
    {
        ScheduleRecurringLetter(data.id, data.name, data.templateId, reward, APOSTLE_LETTER_DELAY_DAYS);
    }

    /// <summary>
    /// 정기 편지 재예약 (편지 보상 수령 시)
    /// </summary>
    public void RescheduleRecurringLetter(string id, string name, int templateId, int reward)
    {
        ScheduleRecurringLetter(id, name, templateId, reward, RECURRING_INTERVAL_DAYS);
    }

    private void ScheduleRecurringLetter(string discipleId, string name, int templateId, int reward, int daysLater)
    {
        TimeManager.Instance.CalculateFutureDate(daysLater, out int y, out int m, out int d);

        RecurringLetterInfo info = new RecurringLetterInfo
        {
            discipleId = discipleId,
            discipleName = name,
            templateId = templateId,
            weeklyReward = reward,
            arrivalYear = y,
            arrivalMonth = m,
            arrivalDay = d
        };

        if (_data.pendingRecurringLetters == null)
            _data.pendingRecurringLetters = new List<RecurringLetterInfo>();

        _data.pendingRecurringLetters.Add(info);

        SaveManager.Instance.Save();
    }

    #endregion

    #region Generators

    private ApostleLetterData GenerateApostleLetter(RecurringLetterInfo info)
    {
        int randIndex = Random.Range(1, APOSTLE_TEMPLATE_COUNT + 1);
        string idPrefix = $"LETTER_APOSTLE_{randIndex:D2}";

        ApostleLetterData letter = new ApostleLetterData
        {
            id = System.Guid.NewGuid().ToString(),
            title = $"{idPrefix}_TITLE",
            greeting = $"{idPrefix}_GREETING",
            story = $"{idPrefix}_STORY",
            sayThanks = $"{idPrefix}_THANKS",

            rewardAmount = info.weeklyReward,
            isRecurring = true,
            senderId = info.discipleId,
            senderTemplateId = info.templateId,  // templateId 보존

            arrivalYear = info.arrivalYear,
            arrivalMonth = info.arrivalMonth,
            arrivalDay = info.arrivalDay
        };

        var sheet = DataManager.Instance.GetDiscipleTemplate(info.templateId);
        letter.senderName = (sheet != null && !string.IsNullOrEmpty(sheet.nameKey))
            ? sheet.nameKey
            : info.discipleName;

        return letter;
    }


    #endregion

    #region History Access (UI용)

    /// <summary>
    /// 특정 편지가 읽지 않은 상태인지 확인
    /// </summary>
    public bool IsLetterUnread(string letterId)
    {
        foreach (var letter in _letterQueue)
        {
            if (letter.id == letterId) return true;
        }
        return false;
    }

    public List<LetterData> GetUnreadLetters()
    {
        return _letterQueue.ToList();
    }

    /// <summary>
    /// 통합 편지 목록 반환 (읽지 않은 편지 + 읽은 편지)
    /// 정렬: 읽지 않은 편지 우선, 그 다음 최신순
    /// 최대 개수 제한 적용
    /// </summary>
    public List<LetterDisplayInfo> GetAllLettersForDisplay()
    {
        List<LetterDisplayInfo> displayList = new List<LetterDisplayInfo>();

        // 읽지 않은 편지만 추가 (최신순 정렬)
        List<LetterData> unreadLetters = _letterQueue.ToList();
        unreadLetters = unreadLetters
            .OrderByDescending(x => x.arrivalYear)
            .ThenByDescending(x => x.arrivalMonth)
            .ThenByDescending(x => x.arrivalDay)
            .ToList();

        foreach (var letter in unreadLetters)
        {
            displayList.Add(new LetterDisplayInfo
            {
                letterData = letter,
                isUnread = true
            });
        }

        // [삭제] 읽은 편지 병합 로직 제거

        return displayList;
    }

    #endregion

    #region ISaveable Implementation

    public void SaveTo(GameData data)
    {
        data.letterSystem = _data;
    }

    public void LoadFrom(GameData data)
    {
        _data = data.letterSystem ?? new LetterSystemData();
        _letterQueue.Clear();

        // [핵심] _letterQueue는 저장되지 않으므로 앱 재시작 시 비어있다.
        // 이미 도착 날짜가 지난 incomingLetters를 즉시 큐로 복원한다.
        // 복원하지 않으면 하루가 지나 DateChanged가 발화되기 전까지
        // HasNewLetter = false가 되어 빨간 점이 켜지지 않는다.
        if (_data.incomingLetters != null)
        {
            List<VisitorLetterData> toRestore = new List<VisitorLetterData>();

            foreach (var letter in _data.incomingLetters)
            {
                if (TimeManager.Instance.IsDateReached(letter.arrivalYear, letter.arrivalMonth, letter.arrivalDay))
                {
                    toRestore.Add(letter);
                }
            }

            foreach (var letter in toRestore)
            {
                _letterQueue.Enqueue(letter);
                _data.incomingLetters.Remove(letter);
            }
        }
    }

    public void ResetToDefault()
    {
        _data = new LetterSystemData();
        _letterQueue.Clear();
    }

    #endregion
}

#region Data Classes

/// <summary>
/// UI 표시용 편지 정보
/// </summary>
public class LetterDisplayInfo
{
    public LetterData letterData;
    public bool isUnread;
}

/// <summary>
/// 편지 시스템 저장 데이터
/// </summary>
[System.Serializable]
public class LetterSystemData
{
    public List<VisitorLetterData> incomingLetters = new List<VisitorLetterData>();
    public List<RecurringLetterInfo> pendingRecurringLetters = new List<RecurringLetterInfo>();
    public int lastVisitorTemplateIndex = 0;
}

[System.Serializable]
public class RecurringLetterInfo
{
    public string discipleId;
    public string discipleName;
    public int templateId;
    public int weeklyReward;
    public int arrivalYear;
    public int arrivalMonth;
    public int arrivalDay;
}

[System.Serializable]
public class LetterData
{
    public string id;
    public string title;
    public int rewardAmount;
    public bool isRewardClaimed;
    public int arrivalYear;
    public int arrivalMonth;
    public int arrivalDay;
}

[System.Serializable]
public class ApostleLetterData : LetterData
{
    public string greeting;
    public string story;
    public string sayThanks;
    public string senderName;
    public string senderId;
    public int senderTemplateId;  // 재예약 시 올바른 제자 템플릿 식별에 사용
    public bool isRecurring;
}

[System.Serializable]
public class VisitorLetterData : LetterData
{
    public string greeting;
    public string story;
    public string empathy;
    public string wisdom;
    public string thanks;
}


#endregion