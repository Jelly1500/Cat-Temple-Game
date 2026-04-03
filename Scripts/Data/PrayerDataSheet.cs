using UnityEngine;

[CreateAssetMenu(fileName = "PrayerDataSheet", menuName = "GameData/PrayerDataSheet")]
public class PrayerDataSheet : ScriptableObject
{
    [Header("기도 기본 정보")]
    public int id;              // 기도 고유 ID (예: 0, 1, 2...)
    public string prayerName;   // 기도 이름
    [TextArea]
    public string description;  // 설명

    [Tooltip("다국어 텍스트 ID (예: NAME_PRAY_1)")]
    public string nameKey;

    [Tooltip("다국어 설명 ID (예: DESC_PRAY_1)")]
    [TextArea]
    public string descKey;

    [Header("비용 및 시간")]
    public int cost;            // 필요 골드
    public int durationDays;    // 소요 시간(일)

    [Header("모집 설정 (Gacha Config)")]
    public int candidateCount;  // 모집 후 등장할 후보 수 (예: 3명이 찾아옴)

    [Range(1, 5)]
    public int minGrade;        // 등장 가능한 최소 등급
    [Range(1, 5)]
    public int maxGrade;        // 등장 가능한 최대 등급
}