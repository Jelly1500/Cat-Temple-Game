#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

public class FontReplacer : EditorWindow
{
    private TMP_FontAsset _targetOldFont; // 바꿀 대상 (비워두면 모든 폰트 대상)
    private TMP_FontAsset _newFont;       // 새로 적용할 폰트

    [MenuItem("Tools/Font Replacer (All Prefabs & Scene)")]
    public static void ShowWindow()
    {
        GetWindow<FontReplacer>("Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Font Replacer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _targetOldFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Old Font (Optional)", _targetOldFont, typeof(TMP_FontAsset), false);
        GUILayout.Label("※ 비워두면 모든 텍스트의 폰트를 교체합니다.", EditorStyles.miniLabel);

        EditorGUILayout.Space();

        _newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New Font", _newFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Replace All (Scene + Project Prefabs)"))
        {
            if (_newFont == null)
            {
                EditorUtility.DisplayDialog("Error", "새로운 폰트(New Font)를 지정해주세요.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Confirm", "프로젝트 내의 모든 프리팹과 현재 씬의 폰트를 변경하시겠습니까?\n(이 작업은 되돌릴 수 없으니 백업을 권장합니다.)", "Yes", "No"))
            {
                ReplaceFonts();
            }
        }
    }

    private void ReplaceFonts()
    {
        int count = 0;

        // 1. 현재 씬(Scene)에 있는 오브젝트 변경
        TMP_Text[] sceneTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text text in sceneTexts)
        {
            // 에셋(프리팹 원본)이 아닌 씬 객체만, 그리고 에디터 UI가 아닌 것만
            if (text.gameObject.scene.rootCount != 0 && !EditorUtility.IsPersistent(text.transform.root.gameObject))
            {
                if (ShouldReplace(text.font))
                {
                    Undo.RecordObject(text, "Font Change");
                    text.font = _newFont;
                    EditorUtility.SetDirty(text);
                    count++;
                }
            }
        }

        // 2. 프로젝트 내의 모든 프리팹(Prefab) 변경
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                // 프리팹 내용을 수정하기 위해 로드하지 않고 바로 수정 (간단한 방식)
                // 주의: 복잡한 프리팹 구조에서는 PrefabUtility.SavePrefabAsset을 사용하는 것이 정석이나, 
                // 폰트 교체는 단순 데이터 변경이므로 아래 방식으로 처리

                bool changed = false;
                TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);

                foreach (TMP_Text t in texts)
                {
                    if (ShouldReplace(t.font))
                    {
                        t.font = _newFont;
                        changed = true;
                        count++;
                    }
                }

                if (changed)
                {
                    EditorUtility.SetDirty(prefab);
                }
            }
        }

        // 변경 사항 저장
        AssetDatabase.SaveAssets();

        Debug.Log($"[Font Replacer] 총 {count}개의 폰트가 교체되었습니다.");
        EditorUtility.DisplayDialog("Success", $"총 {count}개의 컴포넌트가 교체되었습니다.", "OK");
    }

    private bool ShouldReplace(TMP_FontAsset currentFont)
    {
        // Old Font가 지정되지 않았으면 무조건 교체
        if (_targetOldFont == null) return true;

        // 현재 폰트가 없거나, 지정된 Old Font와 같으면 교체
        return currentFont == _targetOldFont;
    }
}
#endif