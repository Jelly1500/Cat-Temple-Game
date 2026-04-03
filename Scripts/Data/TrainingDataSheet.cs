using UnityEngine;

[CreateAssetMenu(fileName = "TrainingDataSheet", menuName = "GameData/TrainingDataSheet")]
public class TrainingDataSheet : ScriptableObject
{
    [Header("기본 정보")]
    public int id;              // 훈련 ID (예: 2001, 2002...)
    public string title;        // 훈련 이름 (예: 명상, 독서, 봉사)
    [TextArea]
    public string description;  // 훈련 설명
    [Tooltip("훈련 이름 ID (예: TITLE_TRAIN_1)")]
    public string titleKey;

    [Tooltip("훈련 설명 ID (예: DESC_TRAIN_1)")]
    public string descKey;
    public Sprite icon;         // 훈련 아이콘

    [Header("비용")]
    public int baseCost;        // 기본 훈련 비용 (골드)

    [Tooltip("훈련 횟수당 비용 증가량 (기본값: 30)")]
    public int costIncreasePerCount = 30;

    [Header("능력치 상승량")]
    public int gainPatience;    // 인내심 상승량
    public int gainEmpathy;     // 공감력 상승량
    public int gainWisdom;      // 지혜 상승량

    [Header("특수 효과")]
    [Range(0f, 100f)]
    public float enlightenBonusProb; // 깨달음 스탯이 오를 확률 보정치 (%)

    [Header("해금 조건")]
    [Tooltip("게임 시작 시 해금되어 있는지 여부")]
    public bool unlockedByDefault = true;

    [Tooltip("해금에 필요한 인지도 (0 = 조건 없음, unlockedByDefault가 false일 때만 사용)")]
    public int requiredRenownToUnlock = 0;
}