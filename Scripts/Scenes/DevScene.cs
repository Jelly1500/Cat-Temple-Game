using System.Collections;
using UnityEngine;

public class DevScene : BaseScene
{
    protected override void Awake()
    {
        base.Awake();

        Init();
    }

    private void Init()
    {
        
        GameManager.Instance.InitializeGame();
    }

    public void Clear()
    {
        Debug.Log("[DevScene] Clear Called");
        // 씬이 변경될 때 정리해야 할 내용 (매니저 정리 등)
        UIManager.Instance.Clear();
        SoundManager.Instance.Clear();
    }
}