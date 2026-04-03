using UnityEngine;
using UnityEngine.UI;

public class UI_ToggleMenu : UI_UGUI
{
    // 1. 바인딩할 오브젝트 이름 정의 (enum 이름은 Hierarchy의 이름과 일치해야 함)
    enum Buttons
    {
        Btn_Toggle
    }

    enum GameObjects
    {
        ScrollView_List
    }

    protected override void Start()
    {
        base.Start();
        Init();
    }

    // UI_Base에 정의된 protected _init 필드를 사용하도록 override
    public override void Init()
    {
        base.Init();

        // 2. UI_UGUI의 바인딩 메서드 활용
        BindButtons(typeof(Buttons));
        BindObjects(typeof(GameObjects));

        // 3. 버튼 이벤트 연결
        GetButton((int)Buttons.Btn_Toggle).onClick.AddListener(OnToggleClicked);

        // 4. 초기 상태 설정 (목록 닫기) - null 안전 처리
        var listObj = GetObject((int)GameObjects.ScrollView_List);
        if (listObj != null)
            listObj.SetActive(false);
    }

    // 토글 로직
    private void OnToggleClicked()
    {
        GameObject list = GetObject((int)GameObjects.ScrollView_List);
        if (list == null) return;

        // 현재 활성화 상태의 반대값으로 설정 (켜져있으면 끄고, 꺼져있으면 켬)
        bool isActive = list.activeSelf;
        list.SetActive(!isActive);

        // (선택 사항) 버튼의 텍스트나 이미지를 상태에 따라 변경하려면 여기에 추가 로직 작성
    }
}