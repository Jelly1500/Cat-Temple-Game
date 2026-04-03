#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class GameInfoDataCreator : MonoBehaviour
{
    [MenuItem("Tools/Create GameInfo Data Assets")]
    public static void CreateAllGameInfoAssets()
    {
        // 저장 경로 확인/생성
        string path = "Assets/Resources/PreLoad/Data/GameInfoData";
        if (!AssetDatabase.IsValidFolder(path))
        {
            // 폴더 구조 순차적으로 생성
            CreateFolderIfNotExists("Assets", "Resources");
            CreateFolderIfNotExists("Assets/Resources", "PreLoad");
            CreateFolderIfNotExists("Assets/Resources/PreLoad", "Data");
            CreateFolderIfNotExists("Assets/Resources/PreLoad/Data", "GameInfoData");
        }

        // 게임 정보 데이터 정의
        var infoList = new (int id, int sortOrder, string titleKey, string descKey)[]
        {
            (1, 1, "UI_GameInfo_Title_Recruit", "UI_GameInfo_Desc_Recruit"),
            (2, 2, "UI_GameInfo_Title_Building", "UI_GameInfo_Desc_Building"),
            (3, 3, "UI_GameInfo_Title_Letter", "UI_GameInfo_Desc_Letter"),
            (4, 4, "UI_GameInfo_Title_Training", "UI_GameInfo_Desc_Training"),
            (5, 5, "UI_GameInfo_Title_Conversation", "UI_GameInfo_Desc_Conversation"),
            (6, 6, "UI_GameInfo_Title_ConversationResult", "UI_GameInfo_Desc_ConversationResult"),
            (7, 7, "UI_GameInfo_Title_Departure", "UI_GameInfo_Desc_Departure"),
            (8, 8, "UI_GameInfo_Title_Renown", "UI_GameInfo_Desc_Renown"),
            (9, 9, "UI_GameInfo_Title_Enlightenment", "UI_GameInfo_Desc_Enlightenment"),
        };

        foreach (var info in infoList)
        {
            GameInfoDataSheet asset = ScriptableObject.CreateInstance<GameInfoDataSheet>();
            asset.id = info.id;
            asset.sortOrder = info.sortOrder;
            asset.titleKey = info.titleKey;
            asset.descriptionKey = info.descKey;

            string assetPath = $"{path}/GameInfo_{info.id:D2}_{info.titleKey.Replace("UI_GameInfo_Title_", "")}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GameInfoDataCreator] {infoList.Length}개의 GameInfo 에셋이 생성되었습니다.");
    }

    private static void CreateFolderIfNotExists(string parent, string newFolderName)
    {
        string fullPath = $"{parent}/{newFolderName}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, newFolderName);
        }
    }
}
#endif