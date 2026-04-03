using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class TextData
{
    public string TemplateID; // 예: "LETTER_01_GREETING"
    public string KOR;        // "스승님, {0}입니다."
    public string ENG;        // "Master, this is {0}."
    public string JPN;          // 일본어 텍스트 추가 가능

    public string GetText(SystemLanguage lang)
    {
        switch (lang)
        {
            case SystemLanguage.English:
                return string.IsNullOrEmpty(ENG) ? KOR : ENG;

            case SystemLanguage.Japanese:
                if (!string.IsNullOrEmpty(JPN)) return JPN;
                if (!string.IsNullOrEmpty(ENG)) return ENG;
                return KOR;

            default: 
                return KOR;
        }
    }
}

[Serializable]
public class TextDataLoader : IDataLoader<string, TextData>
{
    public List<TextData> texts = new List<TextData>();

    public Dictionary<string, TextData> MakeDict()
    {
        Dictionary<string, TextData> dict = new Dictionary<string, TextData>();
        foreach (var text in texts)
            dict.Add(text.TemplateID, text);

        return dict;
    }

    public bool Validate()
    {
        return true;
    }
}