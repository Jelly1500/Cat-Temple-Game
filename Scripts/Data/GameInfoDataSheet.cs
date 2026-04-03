using UnityEngine;

[CreateAssetMenu(fileName = "GameInfo_", menuName = "Data/GameInfoDataSheet")]
public class GameInfoDataSheet : ScriptableObject
{
    [Header("기본 정보")]
    public int id;
    public int sortOrder;  // 정렬 순서

    [Header("다국어 키")]
    public string titleKey;        // 제목 TextData 키
    public string descriptionKey;  // 설명 TextData 키

    [Header("아이콘 (선택)")]
    public Sprite icon;
}