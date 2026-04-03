using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public interface IUI_Popup
{

}

public interface IUI_Scene
{

}

public interface ITutorialHighlightable
{
    /// <summary>
    /// 내부 인덱스를 1 증가시키고, 해당 순서의 강조 이미지를 활성화합니다.
    /// 리스트 범위를 초과하면 자동으로 -1(모두 끄기) 상태로 전환됩니다.
    /// </summary>
    void ShowNextTutorialHighlight();

    /// <summary>
    /// 튜토리얼 강조 상태를 초기화(-1)하고 모든 강조를 끕니다.
    /// </summary>
    void ResetTutorialHighlight();
}

public class UI_Base : MonoBehaviour
{
    // 초기화 여부를 체크하는 플래그 (중복 실행 방지)
    protected bool _init = false;

    public virtual void Init()
    {
        if (_init)
            return;

        _init = true;
    }

    protected virtual void Awake()
    {

    }

    protected virtual void Start()
    {
        RefreshUI();
    }

    protected virtual void OnEnable()
    {
        EventManager.Instance.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    protected virtual void OnDisable()
    {
        EventManager.Instance.RemoveEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    public virtual void RefreshUI()
    {

    }
}
