using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // [추가] 코루틴 사용을 위해 추가

public class CatNameEditPopup : UI_UGUI, IUI_Popup
{
    enum GameObjects
    {
        InputField_Name, // 플레이어가 이름을 입력할 InputField
    }

    enum Buttons
    {
        Btn_Confirm,
        Btn_Cancel
    }

    enum Texts
    {
        Text_Title,
        Text_Cancel,
        Text_Confirm,
        Text_Placeholder,
        Text_WarningToast // [추가] 팝업 종속 토스트 텍스트
    }

    private DiscipleData _targetData;
    private System.Action _onNameChanged; // 이름 변경 후 호출될 콜백

    // 토스트 메시지 스팸 출력을 방지하기 위한 쿨타임 타이머
    private float _lastToastTime = 0f;
    private Coroutine _coToast; // [추가] 토스트 코루틴 관리 변수

    protected override void Start()
    {
        base.Start();
        Init();
        SetLocalizedText();
    }

    public override void Init()
    {
        if (_init) return;
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.Btn_Confirm).onClick.AddListener(OnClickedConfirm);
        GetButton((int)Buttons.Btn_Cancel).onClick.AddListener(ClosePopupUI);

        // InputField 텍스트 변경 이벤트 리스너 등록
        TMP_InputField inputField = Get<GameObject>((int)GameObjects.InputField_Name).GetComponent<TMP_InputField>();
        inputField.onValueChanged.AddListener(OnNameValueChanged);

        
    }

    private void SetLocalizedText()
    {
        GetText((int)Texts.Text_Title).text = DataManager.Instance.GetText("UI_EditName_Title");
        GetText((int)Texts.Text_Placeholder).text = DataManager.Instance.GetText("UI_EditName_Placeholder");
        GetText((int)Texts.Text_Confirm).text = DataManager.Instance.GetText("UI_EditName_Confirm");
        GetText((int)Texts.Text_Cancel).text = DataManager.Instance.GetText("UI_EditName_Cancel");
    }

    public void Setup(DiscipleData data, System.Action onNameChanged = null)
    {
        if (_init == false) Init();
        GetText((int)Texts.Text_WarningToast).gameObject.SetActive(false);
        if (_coToast != null)
        {
            StopCoroutine(_coToast);
            _coToast = null;
        }
        _targetData = data;
        _onNameChanged = onNameChanged;

        // 현재 이름 표시 (기본 이름 or 이미 수정된 이름)
        TMP_InputField inputField = Get<GameObject>((int)GameObjects.InputField_Name).GetComponent<TMP_InputField>();

        // 이름을 표시하는 로직 (CatInfoPopup과 동일한 우선순위 적용하여 현재 보이는 이름 가져오기)
        string currentDisplayName = data.name;
        if (!data.isRenamed && data.Template != null && !string.IsNullOrEmpty(data.Template.nameKey))
        {
            currentDisplayName = DataManager.Instance.GetText(data.Template.nameKey);
        }

        inputField.text = currentDisplayName;
    }

    // 실시간 글자 수 체크 및 토스트 메시지 출력
    private void OnNameValueChanged(string newName)
    {
        // 5글자 초과 여부 확인
        if (newName.Length > 5)
        {
            TMP_InputField inputField = Get<GameObject>((int)GameObjects.InputField_Name).GetComponent<TMP_InputField>();

            // 문자열을 5글자로 강제 절삭
            inputField.text = newName.Substring(0, 5);

            // 입력 커서를 맨 뒤(5번째)로 고정
            inputField.caretPosition = 5;

            // 팝업 중 Time.timeScale이 0일 수 있으므로 unscaledTime 사용
            if (Time.unscaledTime - _lastToastTime >= 3f)
            {
                _lastToastTime = Time.unscaledTime;

                // [수정] 팝업 내부 토스트 출력
                string warningMsg = DataManager.Instance.GetText("UI_Toast_NameLimit");
                ShowToast(warningMsg);
            }
        }
    }

    private void OnClickedConfirm()
    {
        if (_targetData == null) return;

        TMP_InputField inputField = Get<GameObject>((int)GameObjects.InputField_Name).GetComponent<TMP_InputField>();
        string newName = inputField.text;

        // 이름 유효성 검사 (빈 값 등)
        if (string.IsNullOrWhiteSpace(newName))
        {
            // [수정] 팝업 내부 토스트 출력
            string emptyMsg = DataManager.Instance.GetText("UI_Toast_NameEmpty");
            ShowToast(emptyMsg);
            return;
        }

        // 데이터 업데이트
        _targetData.name = newName;
        _targetData.isRenamed = true; // [중요] 이름을 수정했음을 표시하여 TextData 우선순위를 무시하게 함

        // 저장
        SaveManager.Instance.Save();

        // 콜백 호출 (CatInfoPopup 갱신용)
        _onNameChanged?.Invoke();

        if (UIManager.Instance.SceneUI is UI_MainGame mainGame)
        {
            mainGame.RefreshFollowerList();
        }

        ClosePopupUI();
    }

    // ═══════════════════════════════════════════════════════════════
    // [신규] 팝업 종속 토스트 출력 로직
    // ═══════════════════════════════════════════════════════════════
    private void ShowToast(string message)
    {
        if (_coToast != null) StopCoroutine(_coToast);
        _coToast = StartCoroutine(CoShowToast(message));
    }

    private IEnumerator CoShowToast(string message)
    {
        TMP_Text toastText = GetText((int)Texts.Text_WarningToast);

        toastText.text = message;
        toastText.gameObject.SetActive(true);

        // Time.timeScale = 0 상태에서도 3초를 정상적으로 세기 위해 Realtime 사용
        yield return new WaitForSecondsRealtime(3.0f);

        toastText.gameObject.SetActive(false);
    }

    private void ClosePopupUI()
    {
        UIManager.Instance.ClosePopupUI();
    }
}