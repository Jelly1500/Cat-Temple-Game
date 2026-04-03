using UnityEngine;

public static class Define
{
    public enum EScene
    {
        Unknown,
        LoadingScene,
        DevScene,
    }

    public enum EEventType
    {
        None,
        LanguageChanged,
        // === 시간 관련 ===
        DateChanged,        // 날짜 변경

        // === 자원 관련 ===
        GoldChanged,        // 골드 변경
        RenownChanged,      // 인지도 변경
        HammerChanged,      // 망치 포인트 변경

        // === 기도 관련 ===
        PrayerStarted,      // 기도 시작
        PrayerUpdated,      // 기도 진행 상태 변경
        PrayerCompleted,    // 기도 완료 (결과 수령 가능)
        PrayerAbandoned,    // 기도 포기

        // === 제자 관련 ===
        DiscipleRecruited,      // 제자 영입
        DiscipleDeparted,       // 제자 하산
        DiscipleCountChanged,   // 제자 수 변경
        DiscipleCapacityChanged,// 최대 제자 수 변경

        // === 건설 관련 ===
        ConstructionStarted,    // 건설 시작
        ConstructionCompleted,  // 건설 완료
        ConstructionCancelled,  // 건설 취소

        // === 훈련 관련 ===
        TrainingCompleted,  // 훈련 완료
        // 편지 관련 이벤트 추가
        NewLetterArrived,
        LetterRead,
        DiscipleTouched,
    }

    public enum ESound
    {
        Bgm,
        Effect,

        MaxCount
    }

    public enum ELanguage
    {
        KOR,
        ENG,
    }

	public enum EAnimation
	{
		b_wait,
		b_walk,
		f_wait,
		f_walk
	}

	public enum ECatState
	{
		Idle,
		Move,
		Work
	}

    public enum EBuildingEffectType
    {
        None = 0,

        // --- 대화 (Conversation) 관련 효과 ---
        IncreasePatience, // 인내심 증가
        IncreaseEmpathy,  // 공감력 증가
        IncreaseWisdom,   // 지혜 증가

        // --- 훈련 (Training) 관련 효과 ---
        DiscountTrainingCost,       // 훈련 비용 할인 (%)
        IncreaseMaxDiscipleCount,
        IncreaseMaxHammer
    }
}
