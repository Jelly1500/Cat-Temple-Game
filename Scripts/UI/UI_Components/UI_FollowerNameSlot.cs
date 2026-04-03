using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UI_FollowerNameSlot : UI_UGUI
{
    enum Texts
    {
        Text_FollowerName
    }

    private string _discipleID;
    private Action<string> _onClickCallback;

    public override void Init()
    {
        base.Init();
        Bind<TMP_Text>(typeof(Texts));
        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    // [수정] 다국어 로직 적용
    public void SetInfo(DiscipleData data, Action<string> onClickCallback)
    {
        if (_init == false) Init();

        _discipleID = data.id;
        _onClickCallback = onClickCallback;

        // --- 이름 결정 로직 시작 ---
        string displayName = data.name; // 기본값 (안전장치)

        // 1. 플레이어가 이름을 변경한 적이 있다면 -> 저장된 data.name을 그대로 사용
        if (data.isRenamed)
        {
            displayName = data.name;
        }
        // 2. 이름을 변경한 적이 없다면 -> 다국어 키를 통해 번역된 이름 가져오기
        else
        {
            if (data.Template != null && !string.IsNullOrEmpty(data.Template.nameKey))
            {
                displayName = DataManager.Instance.GetText(data.Template.nameKey);
            }
        }
        // --- 이름 결정 로직 끝 ---

        // 텍스트 반영
        GetText((int)Texts.Text_FollowerName).text = displayName;
    }

    private void OnClicked()
    {
        _onClickCallback?.Invoke(_discipleID);
    }
}