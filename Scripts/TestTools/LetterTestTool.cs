using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 편지 시스템 테스트용 도구
/// - 방문객 편지 또는 제자 편지를 1일 뒤 도착하도록 예약
/// - 에디터 인스펙터 버튼 또는 런타임 코드에서 호출 가능
/// </summary>
public class LetterTestTool : MonoBehaviour
{
    [Header("테스트 설정")]
    [Tooltip("생성할 편지 타입")]
    [SerializeField] private ETestLetterType _letterType = ETestLetterType.Visitor;

    [Tooltip("보상 금액 (0이면 랜덤)")]
    [SerializeField] private int _rewardAmount = 0;

    [Tooltip("도착까지 걸리는 일수")]
    [SerializeField] private int _daysUntilArrival = 1;

    public enum ETestLetterType
    {
        Visitor,    // 방문객 편지
        Apostle     // 제자 편지
    }

    #region Letter Templates

    // 방문객 편지 템플릿 (세대별 + 번호)
    private static readonly string[] VisitorTemplates = new string[]
    {
        "LETTER_VISITOR_10_01",
        "LETTER_VISITOR_10_02",
        "LETTER_VISITOR_10_03",
        "LETTER_VISITOR_20_01",
        "LETTER_VISITOR_20_02",
        "LETTER_VISITOR_20_03",
        "LETTER_VISITOR_30_01",
        "LETTER_VISITOR_30_02",
        "LETTER_VISITOR_30_03",
        "LETTER_VISITOR_40_01",
        "LETTER_VISITOR_40_02",
        "LETTER_VISITOR_40_03",
        "LETTER_VISITOR_50_01",
        "LETTER_VISITOR_50_02",
        "LETTER_VISITOR_50_03",
        "LETTER_VISITOR_60_01",
        "LETTER_VISITOR_60_02",
        "LETTER_VISITOR_60_03",
        "LETTER_VISITOR_70_01",
        "LETTER_VISITOR_70_02",
        "LETTER_VISITOR_70_03"
    };

    // 제자 편지 템플릿
    private static readonly string[] ApostleTemplates = new string[]
    {
        "LETTER_APOSTLE_01",
        "LETTER_APOSTLE_02",
        "LETTER_APOSTLE_03",
        "LETTER_APOSTLE_04"
    };

    #endregion

    #region Public Methods

    /// <summary>
    /// 테스트 편지 예약 (인스펙터 설정 사용)
    /// </summary>
    [ContextMenu("테스트 편지 예약")]
    public void ScheduleTestLetter()
    {
        if (_letterType == ETestLetterType.Visitor)
        {
            ScheduleRandomVisitorLetter(_daysUntilArrival, _rewardAmount);
        }
        else
        {
            ScheduleRandomApostleLetter(_daysUntilArrival, _rewardAmount);
        }
    }

    /// <summary>
    /// 랜덤 방문객 편지 예약
    /// </summary>
    public static void ScheduleRandomVisitorLetter(int daysLater = 1, int reward = 0)
    {
        if (LetterManager.Instance == null || TimeManager.Instance == null)
        {
            Debug.LogError("[LetterTestTool] LetterManager 또는 TimeManager가 초기화되지 않았습니다.");
            return;
        }

        // 랜덤 템플릿 선택
        string template = VisitorTemplates[Random.Range(0, VisitorTemplates.Length)];

        // 도착 날짜 계산
        TimeManager.Instance.CalculateFutureDate(daysLater, out int y, out int m, out int d);

        // 보상 금액 결정
        int finalReward = reward > 0 ? reward : Random.Range(50, 200);

        // 편지 데이터 생성
        VisitorLetterData letter = new VisitorLetterData
        {
            id = System.Guid.NewGuid().ToString(),
            title = $"{template}_TITLE",
            greeting = $"{template}_GREETING",
            story = $"{template}_STORY",
            empathy = $"{template}_EMPATHY",
            wisdom = $"{template}_WISDOM",
            thanks = $"{template}_THANKS",
            rewardAmount = finalReward,
            arrivalYear = y,
            arrivalMonth = m,
            arrivalDay = d
        };

        // LetterManager에 직접 예약 (리플렉션 또는 내부 메서드 호출)
        ScheduleVisitorLetterInternal(letter);

        Debug.Log($"[LetterTestTool] 방문객 편지 예약 완료!\n" +
                  $"  템플릿: {template}\n" +
                  $"  도착일: {y}/{m}/{d}\n" +
                  $"  보상: {finalReward}G");
    }

    /// <summary>
    /// 랜덤 제자 편지 예약
    /// </summary>
    public static void ScheduleRandomApostleLetter(int daysLater = 1, int reward = 0)
    {
        if (LetterManager.Instance == null || TimeManager.Instance == null)
        {
            Debug.LogError("[LetterTestTool] LetterManager 또는 TimeManager가 초기화되지 않았습니다.");
            return;
        }

        // 랜덤 템플릿 선택
        string template = ApostleTemplates[Random.Range(0, ApostleTemplates.Length)];

        // 도착 날짜 계산
        TimeManager.Instance.CalculateFutureDate(daysLater, out int y, out int m, out int d);

        // 보상 금액 결정
        int finalReward = reward > 0 ? reward : Random.Range(100, 300);

        // 편지 데이터 생성
        ApostleLetterData letter = new ApostleLetterData
        {
            id = System.Guid.NewGuid().ToString(),
            title = $"{template}_TITLE",
            greeting = $"{template}_GREETING",
            story = $"{template}_STORY",
            sayThanks = $"{template}_THANKS",
            senderName = "테스트 제자",  // 테스트용 이름
            senderId = "test_disciple_" + System.Guid.NewGuid().ToString().Substring(0, 8),
            isRecurring = false,  // 테스트 편지는 정기 편지가 아님
            rewardAmount = finalReward,
            arrivalYear = y,
            arrivalMonth = m,
            arrivalDay = d
        };

        // LetterManager에 직접 예약
        ScheduleApostleLetterInternal(letter);

        Debug.Log($"[LetterTestTool] 제자 편지 예약 완료!\n" +
                  $"  템플릿: {template}\n" +
                  $"  도착일: {y}/{m}/{d}\n" +
                  $"  보상: {finalReward}G");
    }

    /// <summary>
    /// 즉시 도착하는 테스트 편지 생성 (큐에 바로 추가)
    /// </summary>
    public static void CreateImmediateLetter(ETestLetterType type = ETestLetterType.Visitor, int reward = 100)
    {
        if (LetterManager.Instance == null || TimeManager.Instance == null)
        {
            Debug.LogError("[LetterTestTool] LetterManager 또는 TimeManager가 초기화되지 않았습니다.");
            return;
        }

        int y = TimeManager.Instance.Year;
        int m = TimeManager.Instance.Month;
        int d = TimeManager.Instance.Day;

        if (type == ETestLetterType.Visitor)
        {
            string template = VisitorTemplates[Random.Range(0, VisitorTemplates.Length)];

            VisitorLetterData letter = new VisitorLetterData
            {
                id = System.Guid.NewGuid().ToString(),
                title = $"{template}_TITLE",
                greeting = $"{template}_GREETING",
                story = $"{template}_STORY",
                empathy = $"{template}_EMPATHY",
                wisdom = $"{template}_WISDOM",
                thanks = $"{template}_THANKS",
                rewardAmount = reward,
                arrivalYear = y,
                arrivalMonth = m,
                arrivalDay = d
            };

            // 즉시 도착 처리를 위해 오늘 날짜로 예약
            ScheduleVisitorLetterInternal(letter);
            Debug.Log($"[LetterTestTool] 방문객 편지 즉시 예약 (오늘 도착): {template}");
        }
        else
        {
            string template = ApostleTemplates[Random.Range(0, ApostleTemplates.Length)];

            ApostleLetterData letter = new ApostleLetterData
            {
                id = System.Guid.NewGuid().ToString(),
                title = $"{template}_TITLE",
                greeting = $"{template}_GREETING",
                story = $"{template}_STORY",
                sayThanks = $"{template}_THANKS",
                senderName = "테스트 제자",
                senderId = "test_" + System.Guid.NewGuid().ToString().Substring(0, 8),
                isRecurring = false,
                rewardAmount = reward,
                arrivalYear = y,
                arrivalMonth = m,
                arrivalDay = d
            };

            ScheduleApostleLetterInternal(letter);
            Debug.Log($"[LetterTestTool] 제자 편지 즉시 예약 (오늘 도착): {template}");
        }
    }

    #endregion

    #region Internal Scheduling

    private static void ScheduleVisitorLetterInternal(VisitorLetterData letter)
    {
        // LetterManager의 내부 데이터에 접근하여 편지 추가
        // 리플렉션을 사용하거나, LetterManager에 테스트용 public 메서드 추가 필요

        // 방법 1: LetterManager에 테스트용 메서드가 있다면 호출
        // LetterManager.Instance.AddTestVisitorLetter(letter);

        // 방법 2: 리플렉션으로 private 필드 접근
        var dataField = typeof(LetterManager).GetField("_data",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (dataField != null)
        {
            var data = dataField.GetValue(LetterManager.Instance) as LetterSystemData;
            if (data != null)
            {
                if (data.incomingLetters == null)
                    data.incomingLetters = new List<VisitorLetterData>();

                data.incomingLetters.Add(letter);
                SaveManager.Instance?.Save();
            }
        }
    }

    private static void ScheduleApostleLetterInternal(ApostleLetterData letter)
    {
        // 제자 편지는 RecurringLetterInfo를 통해 예약됨
        // 테스트를 위해 직접 큐에 추가하거나 리플렉션 사용

        var dataField = typeof(LetterManager).GetField("_data",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (dataField != null)
        {
            var data = dataField.GetValue(LetterManager.Instance) as LetterSystemData;
            if (data != null)
            {
                // 제자 편지는 RecurringLetterInfo로 예약
                RecurringLetterInfo info = new RecurringLetterInfo
                {
                    discipleId = letter.senderId,
                    discipleName = letter.senderName,
                    templateId = 0,  // 테스트용
                    weeklyReward = letter.rewardAmount,
                    arrivalYear = letter.arrivalYear,
                    arrivalMonth = letter.arrivalMonth,
                    arrivalDay = letter.arrivalDay
                };

                if (data.pendingRecurringLetters == null)
                    data.pendingRecurringLetters = new List<RecurringLetterInfo>();

                data.pendingRecurringLetters.Add(info);
                SaveManager.Instance?.Save();
            }
        }
    }

    #endregion
}

#if UNITY_EDITOR
/// <summary>
/// 에디터 확장 - 테스트 도구 버튼
/// </summary>
[CustomEditor(typeof(LetterTestTool))]
public class LetterTestToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LetterTestTool tool = (LetterTestTool)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("테스트 기능", EditorStyles.boldLabel);

        // 플레이 모드에서만 버튼 활성화
        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("📬 테스트 편지 예약 (설정값 사용)", GUILayout.Height(30)))
        {
            tool.ScheduleTestLetter();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("방문객 편지 (1일 후)", GUILayout.Height(25)))
        {
            LetterTestTool.ScheduleRandomVisitorLetter(1, 0);
        }
        if (GUILayout.Button("제자 편지 (1일 후)", GUILayout.Height(25)))
        {
            LetterTestTool.ScheduleRandomApostleLetter(1, 0);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("방문객 편지 (즉시)", GUILayout.Height(25)))
        {
            LetterTestTool.CreateImmediateLetter(LetterTestTool.ETestLetterType.Visitor, 100);
        }
        if (GUILayout.Button("제자 편지 (즉시)", GUILayout.Height(25)))
        {
            LetterTestTool.CreateImmediateLetter(LetterTestTool.ETestLetterType.Apostle, 150);
        }
        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 테스트 가능합니다.", MessageType.Info);
        }
    }
}
#endif