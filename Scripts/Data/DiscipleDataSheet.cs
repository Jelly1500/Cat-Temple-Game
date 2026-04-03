using UnityEngine;

[CreateAssetMenu(fileName = "DiscipleDataSheet", menuName = "GameData/DiscipleDataSheet")]
public class DiscipleDataSheet : ScriptableObject
{
    [Header("기본 정보")]
    public int templateId;      // 제자 종류 ID (예: 1001, 1002...)
    public string defaultName;  // 기본 이름 (예: 치즈냥이)
    public string prefabName;   // 리소스 폴더 내 프리팹 이름 (예: "Cat_Cheese")

    // [신규] 다국어 지원을 위한 텍스트 키 (예: "NAME_DISCIPLE_1001")
    public string nameKey;

    // [신규] 제자 등급 (1: 일반, 2: 희귀, 3: 영웅, 4: 전설 등)
    [Range(1, 5)]
    public int grade = 1;

    // [신규] 제자 이미지 (UI 팝업에서 표시)
    [Header("이미지")]
    public Sprite portrait;     // 제자 초상화 이미지

    [Header("기본 스탯 (Base Stats)")]
    public int basePatience;    // 기본 인내심
    public int baseEmpathy;     // 기본 공감력
    public int baseWisdom;      // 기본 지혜

    [Header("성장 잠재력 (Potential)")]
    // 훈련 시 깨달음 스탯이 1 오를 확률 (0 ~ 100%)
    [Range(0f, 100f)]
    public float enlightenGainProb;

    [Header("고용 정보")]
    public int baseHireCost;    // 기본 고용 비용
    [TextArea] public string pitchSentence; // 어필 문구 (레거시, 비다국어)

    // 다국어 지원을 위한 어필 문구 텍스트 키 (예: "PITCH_DISCIPLE_1")
    public string pitchSentenceKey;
}