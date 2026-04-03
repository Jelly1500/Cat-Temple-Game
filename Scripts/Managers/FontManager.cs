using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FontManager : Singleton<FontManager>
{
    [Header("Font Assets")]
    [Tooltip("한국어/영어용 폰트 (온글잎 박다현체)")]
    public TMP_FontAsset fontKorEng;


    public void Init()
    {
        LoadFonts();
        // 싱글톤 인스턴스 접근을 통해 객체 생성을 보장하고 로그를 출력
        // 실제 폰트 적용은 DataManager의 Load()가 완료되어 언어 설정이 불러와진 직후에 수행됩니다.
        Debug.Log("[FontManager] Initialized");
    }

    private void LoadFonts()
    {
        // 1. 한국어/영어 폰트 로드 ('Fonts/' 하위 폴더 우선 검색, 실패 시 Root 검색)
        if (fontKorEng == null)
        {
            fontKorEng = Resources.Load<TMP_FontAsset>("Fonts/온글잎 박다현체 SDF");
            if (fontKorEng == null)
                fontKorEng = Resources.Load<TMP_FontAsset>("온글잎 박다현체 SDF");
        }

        

        // 3. 로드 실패 여부 확인 (Null 체크 규칙 준수)
        if (fontKorEng == null)
        {
            Debug.LogError("[FontManager] '온글잎 박다현체 SDF' 폰트를 Resources 폴더에서 찾을 수 없습니다.");
        }

    }

    /// <summary>
    /// 현재 언어 설정에 맞춰 씬 내의 모든 TMP 텍스트 폰트를 교체합니다.
    /// </summary>
    public void RefreshFont(SystemLanguage language)
    {
        // 1. 교체할 타겟 폰트 결정
        TMP_FontAsset targetFont = fontKorEng;

        if (targetFont == null)
        {
            Debug.LogError("[FontManager] 교체할 폰트 에셋이 연결되지 않았습니다!");
            return;
        }

        // -------------------------------------------------------------------------
        // 2. [씬(Scene) 업데이트] 현재 활성화된 씬 내의 모든 텍스트 교체
        // -------------------------------------------------------------------------
        TMP_Text[] sceneTexts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        int sceneCount = 0;
        foreach (TMP_Text text in sceneTexts)
        {
            if (text.font == targetFont) continue;
            text.font = targetFont;
            sceneCount++;
        }

        // -------------------------------------------------------------------------
        // 3. [프리팹(Prefab) 업데이트] ResourceManager에 로드된 모든 프리팹 원본 교체
        // -------------------------------------------------------------------------
        int prefabCount = 0;
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ApplyToAllPrefabs((GameObject prefab) =>
            {
                // 프리팹 내의 비활성화된 자식 오브젝트까지 모두 포함하여 검색
                TMP_Text[] prefabTexts = prefab.GetComponentsInChildren<TMP_Text>(true);

                foreach (var text in prefabTexts)
                {
                    // 폰트가 다를 경우에만 교체
                    if (text.font != targetFont)
                    {
                        text.font = targetFont;
                        // 참고: 런타임에 프리팹 에셋을 수정하면, 이후 Instantiate되는 객체들에 반영됩니다.
                        // 에디터 종료 시 이 변경사항은 저장되지 않고 초기화되므로 안전합니다.
                    }
                }

                if (prefabTexts.Length > 0) prefabCount++;
            });
        }

        Debug.Log($"[FontManager] 언어({language}) 적용 완료: 씬 객체({sceneCount}개), 캐시된 프리팹({prefabCount}개) 업데이트됨.");
    }
}