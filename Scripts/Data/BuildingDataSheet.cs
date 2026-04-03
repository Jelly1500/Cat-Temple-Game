using System;
using System.Collections.Generic;
using UnityEngine;
using static Define; // EBuildingEffectType 사용을 위함

[Serializable]
public struct LevelInfo
{
    public int level;           // 레벨 (1, 2, 3...)

    [Header("레벨별 설정")]
    public float effectValue;   // [신규] 해당 레벨 도달 시 적용될 효과 수치 (누적 아님, 최종값)
    public int cost;            // 다음 레벨로 가기 위한 비용 (현재 레벨이 1이면 2로 갈 때의 비용)
    public int days;            // 건설 소요 시간

    [Header("설명 및 텍스트")]
    public string descriptionKey; // 다국어 키
    [TextArea]
    public string description;    // 기본 설명
}

[CreateAssetMenu(fileName = "BuildingDataSheet", menuName = "GameData/BuildingDataSheet")]
public class BuildingDataSheet : ScriptableObject
{
    [Header("기본 정보")]
    public int buildingId;
    public string buildingName;
    public string nameKey;

    [Header("효과 설정")]
    public EBuildingEffectType effectType;

    // [삭제됨] public float valuePerLevel; -> 이제 LevelInfo.effectValue를 사용합니다.

    [Header("이미지")]
    // ResourceManager로 로드할 Sprite 리소스 이름 (예: "Building_MainHall")
    public string spriteName;

    [Header("레벨별 데이터 리스트")]
    // 인스펙터에서 레벨 1, 2, 3 순서대로 데이터를 입력합니다.
    public List<LevelInfo> levelDataList = new List<LevelInfo>();

    #region Helper Properties
    public bool IsConversationEffect =>
        effectType == EBuildingEffectType.IncreasePatience ||
        effectType == EBuildingEffectType.IncreaseEmpathy ||
        effectType == EBuildingEffectType.IncreaseWisdom;

    public bool IsTrainingEffect =>
        effectType == EBuildingEffectType.DiscountTrainingCost;
    #endregion

    #region Helper Methods

    /// <summary>
    /// 특정 레벨의 정보를 가져옵니다.
    /// </summary>
    public LevelInfo GetLevelInfo(int level)
    {
        // 리스트 순회 (레벨 데이터가 많지 않으므로 Find 사용 무방)
        // 만약 레벨 0(미건설)을 요청하면 기본값을 반환
        if (level <= 0) return default;

        int index = levelDataList.FindIndex(x => x.level == level);

        if (index >= 0)
        {
            return levelDataList[index];
        }

        // [예외처리] 해당 레벨 데이터가 없으면, 가장 마지막(최고 레벨) 데이터를 반환하거나 0 반환
        // 여기서는 안전하게 마지막 데이터를 반환하도록 처리 (Max Level 초과 조회 시)
        if (levelDataList.Count > 0 && level > levelDataList[levelDataList.Count - 1].level)
        {
            return levelDataList[levelDataList.Count - 1];
        }

        return default;
    }

    /// <summary>
    /// 다음 레벨이 존재하는지 확인 (Max Level 체크용)
    /// </summary>
    public bool HasNextLevel(int currentLevel)
    {
        return levelDataList.Exists(x => x.level == currentLevel + 1);
    }

    public string GetDescription(int level)
    {
        if (level <= 0) return "";

        var info = GetLevelInfo(level);
        // 레벨 정보가 없으면 빈 문자열
        if (info.level == 0) return "";

        // 1. 다국어 키 확인
        if (!string.IsNullOrEmpty(info.descriptionKey) && DataManager.Instance != null)
        {
            string localizedText = DataManager.Instance.GetText(info.descriptionKey);
            if (localizedText != info.descriptionKey)
                return localizedText;
        }

        // 2. 기본 설명 반환
        return info.description;
    }
    #endregion
}