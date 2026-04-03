using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SpeakerSide
{
    Left,
    Right,
    None // 내레이션 등
}

[System.Serializable]
public class DialogueActionData
{
    public ActionType type;
    public AudioClip clip; // for PlaySound

    // [변경] 다국어 지원을 위해 TemplateID 사용
    public string dialogueTextTemplateId; // TextData의 TemplateID (예: "DIALOGUE_RENOWN_0_1")

    public SpeakerSide speakerSide;

    public CharacterId leftCharacterId;
    public CharacterState leftCharacterState;

    public CharacterId rightCharacterId;
    public CharacterState rightCharacterState;

    // [추가] 좌/우 감정 아이콘 (인스펙터에서 스프라이트 직접 할당)
    public Sprite leftEmotionSprite;
    public Sprite rightEmotionSprite;

    // for ChoiceDialogue - [변경] TemplateID 사용
    public string leftChoiceTextTemplateId;
    public string rightChoiceTextTemplateId;
    public DialogueEventData leftChoiceEvent;
    public DialogueEventData rightChoiceEvent;

    // [추가] 튜토리얼 및 UI 제어용 필드
    public string targetUIName; 
    public string targetObjectName;
    public int targetDataValue;

    // [추가] 훈련 해금용 필드
    public int unlockTrainingId; // 해금할 훈련 ID

    public float subtitleYPos;

    // [추가] 런타임에 현재 언어의 텍스트 가져오기
    public string GetDialogueText()
    {
        if (string.IsNullOrEmpty(dialogueTextTemplateId))
            return "";
        return DataManager.Instance.GetText(dialogueTextTemplateId);
    }

    public string GetLeftChoiceText()
    {
        if (string.IsNullOrEmpty(leftChoiceTextTemplateId))
            return "";
        return DataManager.Instance.GetText(leftChoiceTextTemplateId);
    }

    public string GetRightChoiceText()
    {
        if (string.IsNullOrEmpty(rightChoiceTextTemplateId))
            return "";
        return DataManager.Instance.GetText(rightChoiceTextTemplateId);
    }
}

public class DialogueAction
{
    public DialogueActionData Data { get; private set; }
    private bool isDialogueFinished;

    private float _actionStartTime;

    public DialogueAction(DialogueActionData data)
    {
        Data = data;
        isDialogueFinished = false;
    }

    public void Execute()
    {
        _actionStartTime = Time.unscaledTime;
        switch (Data.type)
        {
            case ActionType.PlaySound:
                if (Data.clip != null) AudioSource.PlayClipAtPoint(Data.clip, Vector3.zero);
                isDialogueFinished = true;
                break;
            case ActionType.Dialogue:
                isDialogueFinished = false;
                DialogueManager.Instance.ShowDialogue(Data);
                break;
            case ActionType.ChoiceDialogue:
                isDialogueFinished = false;
                DialogueManager.Instance.ShowChoiceDialogue(Data);
                break;
            case ActionType.OpenUI:
                ExecuteOpenUI();
                break;
            case ActionType.CloseUI:
                UIManager.Instance.ClosePopupUIByName(Data.targetUIName);
                isDialogueFinished = true;
                break;

            case ActionType.ForceSetData:
                GameDataManager.Instance.SetGold(Data.targetDataValue);
                isDialogueFinished = true;
                break;

            case ActionType.HighlightUI:
                ExecuteHighlightUI();
                break;

            case ActionType.EndTutorial:
                isDialogueFinished = true;
                break;

            case ActionType.UnlockTraining:
                // [신규] 훈련 해금 액션
                if (Data.unlockTrainingId > 0)
                {
                    TrainingManager.Instance.UnlockTraining(Data.unlockTrainingId);
                }
                else
                {
                    Debug.LogWarning("[DialogueAction] UnlockTraining: unlockTrainingId가 설정되지 않음");
                }
                isDialogueFinished = true;
                break;

            case ActionType.IncreaseAllEnlightenment:
                // [신규] 모든 제자 깨달음 증가 액션
                // targetDataValue: 증가시킬 깨달음 수치 (인스펙터에서 설정)
                if (Data.targetDataValue > 0)
                {
                    GameDataManager.Instance.IncreaseAllDisciplesEnlightenment(Data.targetDataValue);
                }
                else
                {
                    Debug.LogWarning("[DialogueAction] IncreaseAllEnlightenment: targetDataValue가 0 이하입니다.");
                }
                isDialogueFinished = true;
                break;
            case ActionType.IncreaseAllPatience:
                // [신규] 모든 제자 인내심 증가 액션
                // 인스펙터에서 targetDataValue를 5로 설정하여 사용합니다.
                DiscipleManager.Instance.IncreaseAllPatience(Data.targetDataValue);
                isDialogueFinished = true;
                break;
            // ═══════════════════════════════════════════════════════════════
            // [신규] 제자 터치 강제 실행 액션
            // ═══════════════════════════════════════════════════════════════
            case ActionType.ForceDiscipleTouch:
                ExecuteForceDiscipleTouch();
                break;

            case ActionType.RewardGold:
                GameDataManager.Instance.AddGold(Data.targetDataValue);
                UIManager.Instance.ShowGameToast("UI_Toast_GetGold", Data.targetDataValue);
                isDialogueFinished = true;
                break;

            case ActionType.WaitTutorialStep:
                isDialogueFinished = false;

                GameObject targetBtnObj = GameObject.Find(Data.targetUIName);
                RectTransform targetRect = targetBtnObj.GetComponent<RectTransform>();
                UnityEngine.UI.Button targetButton = targetBtnObj.GetComponent<UnityEngine.UI.Button>();

                // [수정] Data.subtitleYPos 값 추가 전달
                TutorialManager.Instance.StartTutorialStep(targetButton, targetRect, Data.dialogueTextTemplateId, Data.subtitleYPos);
                break;
            case ActionType.ForceCompleteConstruction:
                // 1. 매니저에게 모든 건설 즉시 완료 지시
                BuildingManager.Instance.ForceCompleteAllConstructions();

                // 2. 현재 열려있는 모든 UI(팝업 포함) 강제 갱신
                UIManager.Instance.RefreshAllActiveUI();

                // 3. 딜레이 없이 즉시 다음 액션으로 진행
                isDialogueFinished = true;
                break;
            case ActionType.ForceCompletePrayer:
                // 1. 매니저에게 기도 즉시 완료 상태 변경 지시
                PrayerManager.Instance.ForceCompletePrayer();

                // 2. 만약 '진행 중' 팝업이 화면에 떠 있다면 즉시 닫기 
                UIManager.Instance.ClosePopupUIByName("PrayInProgressPopup");

                // 3. 메인 화면 UI 강제 갱신 (모집 RedDot 켜기 등)
                UIManager.Instance.RefreshAllActiveUI();

                isDialogueFinished = true;
                break;
            case ActionType.ExplainUIWithHighlight:
                ExecuteExplainUIWithHighlight();
                break;

            default:
                isDialogueFinished = true;
                break;
        }
    }

    private void ExecuteExplainUIWithHighlight()
    {
        isDialogueFinished = false; // 대화 텍스트처럼 사용자의 입력이 있을 때까지 대기

        DialogueManager.Instance.CloseDialogueUI();
        // 1. 매니저에게 대상 UI 스크립트 검색 요청
        UI_Base targetUI = UIManager.Instance.GetUI(Data.targetUIName);

        // 2. UI 객체 하위에서 세부 객체 검색 (널 체크 생략 지침 적용: 실패 시 에러 즉시 노출)
        GameObject targetObject = Utils.FindChildGameObject(targetUI.gameObject, Data.targetObjectName, true);
        RectTransform targetRect = targetObject.GetComponent<RectTransform>();

        // 3. UIManager를 통해 강조 표시 프리팹 생성 및 표시
        UI_ExplainHighlight highlightUI = UIManager.Instance.ShowUI<UI_ExplainHighlight>("UI_ExplainHighlight");

        // 4. 추적할 타겟과 텍스트 정보 전달
        highlightUI.SetupHighlight(targetRect, Data.dialogueTextTemplateId, Data.subtitleYPos, () =>
        {
            isDialogueFinished = true;
            highlightUI.ClearHighlight();
        });
    }

    /// <summary>
    /// OpenUI 액션 실행 - 팝업 UI 열기 + 팝업별 데이터 주입
    /// </summary>
    private void ExecuteOpenUI()
    {
        if (string.IsNullOrEmpty(Data.targetUIName))
        {
            Debug.LogError("[DialogueAction] OpenUI: targetUIName이 설정되지 않음");
            isDialogueFinished = true;
            return;
        }

        // 팝업 열기 시도
        UI_Base popup = UIManager.Instance.ShowPopupUIByName(Data.targetUIName);

        if (popup == null)
        {
            Debug.LogError($"[DialogueAction] OpenUI 실패: {Data.targetUIName} - 프리팹을 찾을 수 없거나 UI_Base 컴포넌트가 없습니다.");
            isDialogueFinished = true;
            return;
        }

        // 팝업별 데이터 주입
        SetupPopupWithData(popup);

        isDialogueFinished = true;
    }

    /// <summary>
    /// 팝업 이름에 따라 적절한 데이터를 주입합니다.
    /// DiscipleManager에 실제 제자가 있으면 첫 번째 제자를, 없으면 더미 데이터를 사용합니다.
    /// </summary>
    private void SetupPopupWithData(UI_Base popup)
    {
        DiscipleData discipleData = GetDiscipleDataForTutorial();

        switch (Data.targetUIName)
        {
            case "CatInfoPopup":
                if (popup is CatInfoPopup catInfoPopup)
                    catInfoPopup.Setup(discipleData);
                break;

            case "CatDeparturePopup":
                if (popup is CatDeparturePopup catDeparturePopup)
                    catDeparturePopup.Setup(discipleData);
                break;

            case "CatTrainingPopup":
                if (popup is CatTrainingPopup catTrainingPopup)
                    catTrainingPopup.Setup(discipleData);
                break;

            default:
                // Setup이 필요 없는 팝업은 열기만 함
                break;
        }
    }

    /// <summary>
    /// 튜토리얼용 제자 데이터를 반환합니다.
    /// targetDataValue가 양수이면 해당 templateId의 제자를 우선 탐색하고,
    /// 없으면 첫 번째 제자, 그것도 없으면 더미 데이터를 반환합니다.
    /// </summary>
    private DiscipleData GetDiscipleDataForTutorial()
    {
        var disciples = DiscipleManager.Instance.Disciples;

        // targetDataValue로 templateId를 지정한 경우 해당 제자 우선 탐색
        if (Data.targetDataValue > 0 && disciples != null)
        {
            DiscipleData targeted = DiscipleManager.Instance.GetDiscipleByTemplateId(Data.targetDataValue);
            if (targeted != null)
                return targeted;
        }

        // 제자가 한 명이라도 있으면 첫 번째 반환
        if (disciples != null && disciples.Count > 0)
            return disciples[0];

        // 실제 제자가 없는 경우: UI 표시 전용 더미 데이터 생성 (저장되지 않음)
        return CreateDummyDiscipleData();
    }

    /// <summary>
    /// 튜토리얼 표시용 더미 DiscipleData를 생성합니다.
    /// 실제 게임 데이터에는 저장되지 않으며 UI 표시 목적으로만 사용됩니다.
    /// </summary>
    private DiscipleData CreateDummyDiscipleData()
    {
        DiscipleDataSheet template = DataManager.Instance.GetDiscipleTemplate(1001);

        return new DiscipleData
        {
            id = "dummy_tutorial",
            templateId = template != null ? template.templateId : 1001,
            name = template != null ? template.defaultName : "치즈냥이",

            trainingPatience = 3,
            trainingEmpathy = 3,
            trainingWisdom = 3,
            trainingEnlighten = 1,

            x = 0,
            y = 0
        };
    }

    /// <summary>
    /// HighlightUI 액션 실행 - UI 강조 표시
    /// </summary>
    private void ExecuteHighlightUI()
    {
        if (string.IsNullOrEmpty(Data.targetUIName))
        {
            Debug.LogWarning("[DialogueAction] HighlightUI: targetUIName이 설정되지 않음");
            isDialogueFinished = true;
            return;
        }

        // 1. UIManager에서 UI 찾기 (SceneUI 또는 열린 Popup)
        UI_Base targetUI = UIManager.Instance.GetUI(Data.targetUIName);

        // 2. UIManager에서 못 찾았으면 FindFirstObjectByType으로 씬 전체 검색
        if (targetUI == null)
        {
            // 타입 이름으로 직접 찾기 시도
            var allUIBases = Object.FindObjectsByType<UI_Base>(FindObjectsSortMode.None);
            foreach (var ui in allUIBases)
            {
                if (ui.GetType().Name == Data.targetUIName && ui.gameObject.activeInHierarchy)
                {
                    targetUI = ui;
                    break;
                }
            }
        }

        if (targetUI != null)
        {
            if (targetUI is ITutorialHighlightable highlightableUI)
            {
                highlightableUI.ShowNextTutorialHighlight();
            }
            else
            {
                Debug.LogWarning($"[DialogueAction] {Data.targetUIName}은(는) ITutorialHighlightable 미구현.");
            }
        }
        else
        {
            Debug.LogWarning($"[DialogueAction] HighlightUI - UI 찾기 실패: {Data.targetUIName}");
        }

        isDialogueFinished = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // [신규] 제자 터치 강제 실행 액션
    // ═══════════════════════════════════════════════════════════════

    // 카메라 줌인 대기 시간 (초)
    private const float FORCE_DISCIPLE_TOUCH_DELAY = 1.5f;

    /// <summary>
    /// 제자 터치 강제 실행 액션
    /// - DiscipleManager에서 제자 목록을 가져옴
    /// - 첫 번째 제자를 자동 선택하여 UI_CatMenu 표시
    /// - 1.5초 대기 후 다음 액션으로 진행 (카메라 줌인 대기)
    /// </summary>
    private void ExecuteForceDiscipleTouch()
    {
        isDialogueFinished = false; // 딜레이 완료 전까지 대기

        // 1. DiscipleManager에서 제자 목록 가져오기
        var disciples = DiscipleManager.Instance.Disciples;

        if (disciples == null || disciples.Count == 0)
        {
            Debug.LogWarning("[DialogueAction] ForceDiscipleTouch: 제자가 없습니다.");
            isDialogueFinished = true;
            return;
        }

        // 2. 첫 번째 제자 데이터 가져오기
        DiscipleData firstDiscipleData = disciples[0];

        // 3. 해당 제자의 런타임 오브젝트 가져오기
        Disciple discipleObj = DiscipleManager.Instance.GetObject(firstDiscipleData.id);

        if (discipleObj == null)
        {
            Debug.LogWarning($"[DialogueAction] ForceDiscipleTouch: 제자 오브젝트를 찾을 수 없습니다. (ID: {firstDiscipleData.id})");
            isDialogueFinished = true;
            return;
        }

        // 5. 대화 UI 닫기 및 Time.timeScale 복원
        DialogueManager.Instance.CloseDialogueUI();
        Time.timeScale = 1f;

        

        UI_MainGame mainGameUI = UIManager.Instance.SceneUI as UI_MainGame;

        if (mainGameUI != null)
        {
            // UI_CatMenu 표시
            mainGameUI.ShowCatMenu(discipleObj.transform);

            // 카메라 포커스
            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetFollowTarget(discipleObj.transform);
            }
        }
        else
        {
            Debug.LogWarning("[DialogueAction] ForceDiscipleTouch: UI_MainGame을 찾을 수 없습니다.");
        }

        TutorialManager.Instance.IsInputBlocked = true;

        UIManager.Instance.SetSceneUIInteractable(false);

        // 7. 1.5초 딜레이 후 액션 완료 (카메라 줌인 대기)
        DialogueManager.Instance.StartCoroutine(CoDelayedFinish(FORCE_DISCIPLE_TOUCH_DELAY));
    }

    /// <summary>
    /// 지정된 시간 후에 isDialogueFinished를 true로 설정하는 코루틴
    /// </summary>
    private System.Collections.IEnumerator CoDelayedFinish(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.IsInputBlocked = false;
        }

        UIManager.Instance.SetSceneUIInteractable(true);

        isDialogueFinished = true;
    }

    public void SetDialogueFinished()
    {
        if (Time.unscaledTime - _actionStartTime < 0.1f)
        {
            return;
        }

        if (Data.type == ActionType.Dialogue ||
            Data.type == ActionType.ChoiceDialogue ||
            Data.type == ActionType.WaitTutorialStep)
            isDialogueFinished = true;
        else if (Data.type == ActionType.ExplainUIWithHighlight)
        {
            isDialogueFinished = true;

            // 씬에 활성화된 강조 UI를 찾아 비활성화 (널 체크 생략 지침 적용)
            UI_ExplainHighlight highlightUI = Object.FindFirstObjectByType<UI_ExplainHighlight>();
            highlightUI.ClearHighlight();
        }
    }

    public bool IsFinished()
    {
        switch (Data.type)
        {
            case ActionType.Dialogue:
            case ActionType.ChoiceDialogue:
            case ActionType.ForceDiscipleTouch:
            case ActionType.WaitTutorialStep:
            case ActionType.ExplainUIWithHighlight:
                return isDialogueFinished;
            default:
                return true;
        }
    }
}

public enum ActionType
{
    PlaySound,
    Dialogue,
    ChoiceDialogue,

    Emoticon,
    SpawnNpc,
    MoveNpc,

    // --- Tutorial Actions ---
    OpenUI,         // 특정 UI 팝업 열기
    CloseUI,        // 특정 UI 팝업 닫기
    HighlightUI,    // (선택) 특정 UI 강조 표시
    ForceSetData,   // 튜토리얼용 특정 데이터(골드, 제자 등) 강제 세팅
    EndTutorial,    // 튜토리얼 완료 처리

    // --- Unlock Actions ---
    UnlockTraining,  // 훈련 해금

    // --- Disciple Actions ---
    IncreaseAllEnlightenment,  // 보유 중인 모든 제자의 깨달음 수치를 N 증가
    IncreaseAllPatience,

    // --- [신규] 튜토리얼 강제 실행 액션 ---
    ForceDiscipleTouch,
    RewardGold,
    WaitTutorialStep,
    ForceCompleteConstruction,
    ForceCompletePrayer,
    ExplainUIWithHighlight,
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DialogueActionData))]
public class DialogueActionDataDrawer : PropertyDrawer
{
    private float LineHeight => EditorGUIUtility.singleLineHeight;
    private float Spacing => EditorGUIUtility.standardVerticalSpacing;
    private float SingleRowHeight => LineHeight + Spacing;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // BeginProperty에 GUIContent.none을 전달하여 기본 라벨 그리기 방지
        EditorGUI.BeginProperty(position, GUIContent.none, property);

        // 들여쓰기 레벨 저장 후 리셋 (중첩 문제 방지)
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        float yPos = position.y;

        // 1. Action Type
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        Rect typeRect = new Rect(position.x, yPos, position.width, LineHeight);
        EditorGUI.PropertyField(typeRect, typeProp);
        yPos += SingleRowHeight;

        ActionType type = (ActionType)typeProp.enumValueIndex;

        if (type == ActionType.PlaySound)
        {
            Rect clipRect = new Rect(position.x, yPos, position.width, LineHeight);
            EditorGUI.PropertyField(clipRect, property.FindPropertyRelative("clip"));
        }
        else if (type == ActionType.Dialogue || type == ActionType.ChoiceDialogue)
        {
            // 2. Speaker Side
            Rect speakerRect = new Rect(position.x, yPos, position.width, LineHeight);
            EditorGUI.PropertyField(speakerRect, property.FindPropertyRelative("speakerSide"));
            yPos += SingleRowHeight + 5f;

            // 3. 좌/우 캐릭터 설정 (2열 레이아웃)
            float gap = 10f;
            float columnWidth = (position.width - gap) / 2f;

            // -- Header Labels --
            Rect leftHeaderRect = new Rect(position.x, yPos, columnWidth, LineHeight);
            Rect rightHeaderRect = new Rect(position.x + columnWidth + gap, yPos, columnWidth, LineHeight);
            EditorGUI.LabelField(leftHeaderRect, "Left Character", EditorStyles.boldLabel);
            EditorGUI.LabelField(rightHeaderRect, "Right Character", EditorStyles.boldLabel);
            yPos += SingleRowHeight;

            // -- Character ID --
            Rect leftIdRect = new Rect(position.x, yPos, columnWidth, LineHeight);
            Rect rightIdRect = new Rect(position.x + columnWidth + gap, yPos, columnWidth, LineHeight);
            EditorGUI.PropertyField(leftIdRect, property.FindPropertyRelative("leftCharacterId"), GUIContent.none);
            EditorGUI.PropertyField(rightIdRect, property.FindPropertyRelative("rightCharacterId"), GUIContent.none);
            yPos += SingleRowHeight;

            // -- Character State --
            Rect leftStateRect = new Rect(position.x, yPos, columnWidth, LineHeight);
            Rect rightStateRect = new Rect(position.x + columnWidth + gap, yPos, columnWidth, LineHeight);
            EditorGUI.PropertyField(leftStateRect, property.FindPropertyRelative("leftCharacterState"), GUIContent.none);
            EditorGUI.PropertyField(rightStateRect, property.FindPropertyRelative("rightCharacterState"), GUIContent.none);
            yPos += SingleRowHeight;

            // -- Emotion Sprite --
            Rect leftEmotionRect = new Rect(position.x, yPos, columnWidth, LineHeight);
            Rect rightEmotionRect = new Rect(position.x + columnWidth + gap, yPos, columnWidth, LineHeight);
            EditorGUI.PropertyField(leftEmotionRect, property.FindPropertyRelative("leftEmotionSprite"), new GUIContent("Emotion"));
            EditorGUI.PropertyField(rightEmotionRect, property.FindPropertyRelative("rightEmotionSprite"), new GUIContent("Emotion"));
            yPos += SingleRowHeight + 5f;

            // 4. Dialogue Text Template ID (변경)
            SerializedProperty textProp = property.FindPropertyRelative("dialogueTextTemplateId");
            Rect textRect = new Rect(position.x, yPos, position.width, LineHeight);
            EditorGUI.PropertyField(textRect, textProp, new GUIContent("Dialogue Text Template ID"));
            yPos += LineHeight + Spacing;

            // 5. Choice Dialogue 추가 필드
            if (type == ActionType.ChoiceDialogue)
            {
                yPos += 5f;
                Rect choiceHeaderRect = new Rect(position.x, yPos, position.width, LineHeight);
                EditorGUI.LabelField(choiceHeaderRect, "Choice Settings", EditorStyles.boldLabel);
                yPos += SingleRowHeight;

                // Left Choice Template ID
                Rect leftTextRect = new Rect(position.x, yPos, position.width, LineHeight);
                EditorGUI.PropertyField(leftTextRect, property.FindPropertyRelative("leftChoiceTextTemplateId"), new GUIContent("Left Choice Template ID"));
                yPos += SingleRowHeight;

                SerializedProperty leftEventProp = property.FindPropertyRelative("leftChoiceEvent");
                float leftEventHeight = EditorGUI.GetPropertyHeight(leftEventProp);
                Rect leftEventRect = new Rect(position.x, yPos, position.width, leftEventHeight);
                EditorGUI.PropertyField(leftEventRect, leftEventProp);
                yPos += leftEventHeight + Spacing + 5f;

                // Right Choice Template ID
                Rect rightTextRect = new Rect(position.x, yPos, position.width, LineHeight);
                EditorGUI.PropertyField(rightTextRect, property.FindPropertyRelative("rightChoiceTextTemplateId"), new GUIContent("Right Choice Template ID"));
                yPos += SingleRowHeight;

                SerializedProperty rightEventProp = property.FindPropertyRelative("rightChoiceEvent");
                float rightEventHeight = EditorGUI.GetPropertyHeight(rightEventProp);
                Rect rightEventRect = new Rect(position.x, yPos, position.width, rightEventHeight);
                EditorGUI.PropertyField(rightEventRect, rightEventProp);
            }
        }
        else if (type == ActionType.CloseUI)
        {
            Rect uiNameRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty uiNameProp = property.FindPropertyRelative("targetUIName");
            EditorGUI.PropertyField(uiNameRect, uiNameProp, new GUIContent("Target UI Name"));
        }
        else if (type == ActionType.OpenUI)
        {
            // UI 이름
            Rect uiNameRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty uiNameProp = property.FindPropertyRelative("targetUIName");
            EditorGUI.PropertyField(uiNameRect, uiNameProp, new GUIContent("Target UI Name"));
            yPos += SingleRowHeight;

            // 제자 팝업에 주입할 templateId (0이면 첫 번째 제자 또는 더미 사용)
            Rect valRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty valProp = property.FindPropertyRelative("targetDataValue");
            EditorGUI.PropertyField(valRect, valProp, new GUIContent("Disciple TemplateId (0 = auto)"));
        }
        else if (type == ActionType.ForceSetData)
        {
            Rect valRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty valProp = property.FindPropertyRelative("targetDataValue");
            EditorGUI.PropertyField(valRect, valProp, new GUIContent("Add Value (Gold/Etc)"));
        }
        else if (type == ActionType.HighlightUI)
        {
            // UI 이름 입력만 남김
            Rect uiNameRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty uiNameProp = property.FindPropertyRelative("targetUIName");
            EditorGUI.PropertyField(uiNameRect, uiNameProp, new GUIContent("Target UI Name"));
        }
        else if (type == ActionType.UnlockTraining)
        {
            // [신규] 훈련 해금 액션 필드
            Rect trainingIdRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty trainingIdProp = property.FindPropertyRelative("unlockTrainingId");
            EditorGUI.PropertyField(trainingIdRect, trainingIdProp, new GUIContent("Training ID to Unlock"));
        }
        else if (type == ActionType.IncreaseAllEnlightenment)
        {
            // [신규] 모든 제자 깨달음 증가 액션 필드
            Rect valueRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty valueProp = property.FindPropertyRelative("targetDataValue");
            EditorGUI.PropertyField(valueRect, valueProp, new GUIContent("Enlighten Amount (N)"));
        }
        else if (type == ActionType.IncreaseAllPatience)
        {
            // [신규] 모든 제자 인내심 증가 액션 필드
            Rect valueRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty valueProp = property.FindPropertyRelative("targetDataValue");
            EditorGUI.PropertyField(valueRect, valueProp, new GUIContent("Patience Amount (N)"));
        }
        else if (type == ActionType.ForceDiscipleTouch)
        {
            // [신규] ForceDiscipleTouch - 추가 필드 없음 (안내 라벨만 표시)
            Rect infoRect = new Rect(position.x, yPos, position.width, LineHeight * 2);
            EditorGUI.HelpBox(infoRect, "첫 번째 제자를 자동 선택하여 UI_CatMenu를 표시합니다.\n대화 UI가 닫히고 게임이 재개됩니다.", MessageType.Info);
        }
        else if (type == ActionType.RewardGold)
        {
            Rect goldRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty goldProp = property.FindPropertyRelative("targetDataValue");
            EditorGUI.PropertyField(goldRect, goldProp, new GUIContent("Gold Amount"));
        }
        else if (type == ActionType.WaitTutorialStep)
        {
            // 1. 타겟 UI 오브젝트 이름
            Rect uiNameRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty uiNameProp = property.FindPropertyRelative("targetUIName");
            EditorGUI.PropertyField(uiNameRect, uiNameProp, new GUIContent("Target UI Name"));
            yPos += SingleRowHeight;

            // 2. 가이드 텍스트 템플릿 ID
            Rect guideTextRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty guideTextProp = property.FindPropertyRelative("dialogueTextTemplateId");
            EditorGUI.PropertyField(guideTextRect, guideTextProp, new GUIContent("Guide Text ID"));
            yPos += SingleRowHeight;

            Rect yPosRect = new Rect(position.x, yPos, position.width, LineHeight);
            SerializedProperty subtitleYProp = property.FindPropertyRelative("subtitleYPos");
            EditorGUI.PropertyField(yPosRect, subtitleYProp, new GUIContent("Subtitle Y Position"));
        }
        else if (type == ActionType.ExplainUIWithHighlight) // [추가] 인스펙터 렌더링 로직
        {
            // 1. 타겟 UI 스크립트/프리팹 이름
            Rect uiNameRect = new Rect(position.x, yPos, position.width, LineHeight);
            EditorGUI.PropertyField(uiNameRect, property.FindPropertyRelative("targetUIName"), new GUIContent("Target UI Script Name"));
            yPos += SingleRowHeight;

            // 2. 강조할 세부 객체 이름
            Rect objNameRect = new Rect(position.x, yPos, position.width, LineHeight);
            EditorGUI.PropertyField(objNameRect, property.FindPropertyRelative("targetObjectName"), new GUIContent("Target Object Name"));
            yPos += SingleRowHeight;

            // 3. 가이드 텍스트 템플릿 ID
            Rect guideTextRect = new Rect(position.x, yPos, position.width, LineHeight);
            EditorGUI.PropertyField(guideTextRect, property.FindPropertyRelative("dialogueTextTemplateId"), new GUIContent("Guide Text ID"));
            yPos += SingleRowHeight;

            // 4. 자막 Y 위치
            Rect yPosRect = new Rect(position.x, yPos, position.width, LineHeight);
            EditorGUI.PropertyField(yPosRect, property.FindPropertyRelative("subtitleYPos"), new GUIContent("Subtitle Y Position"));
        }

        // 들여쓰기 레벨 복원
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        ActionType type = (ActionType)typeProp.enumValueIndex;

        // 기본 높이 (Type 필드)
        float height = SingleRowHeight;

        if (type == ActionType.PlaySound)
        {
            height += SingleRowHeight; // clip 필드
        }
        else if (type == ActionType.Dialogue || type == ActionType.ChoiceDialogue)
        {
            // SpeakerSide + gap
            height += SingleRowHeight + 5f;
            // Header + CharacterId + CharacterState + EmotionSprite + gap
            height += SingleRowHeight * 4 + 5f;
            // DialogueTextTemplateId
            height += LineHeight + Spacing;

            if (type == ActionType.ChoiceDialogue)
            {
                // gap + Choice Header
                height += 5f + SingleRowHeight;
                // leftChoiceTextTemplateId
                height += SingleRowHeight;
                // leftChoiceEvent (가변 높이)
                SerializedProperty leftEventProp = property.FindPropertyRelative("leftChoiceEvent");
                height += EditorGUI.GetPropertyHeight(leftEventProp) + Spacing + 5f;
                // rightChoiceTextTemplateId
                height += SingleRowHeight;
                // rightChoiceEvent (가변 높이)
                SerializedProperty rightEventProp = property.FindPropertyRelative("rightChoiceEvent");
                height += EditorGUI.GetPropertyHeight(rightEventProp) + Spacing;
            }
        }
        else if (type == ActionType.OpenUI)
        {
            height += SingleRowHeight * 2; 
        }
        else if (type == ActionType.CloseUI)
        {
            height += SingleRowHeight;
        }
        else if (type == ActionType.ForceSetData)
        {
            height += SingleRowHeight;
        }
        else if (type == ActionType.HighlightUI)
        {
            height += SingleRowHeight;
        }
        else if (type == ActionType.UnlockTraining)
        {
            height += SingleRowHeight;
        }
        else if (type == ActionType.IncreaseAllEnlightenment)
        {
            height += SingleRowHeight * 2; 
        }
        else if (type == ActionType.IncreaseAllPatience)
        {
            height += SingleRowHeight;
        }
        else if (type == ActionType.ForceDiscipleTouch)
        {
            height += LineHeight * 2 + 10f; 
        }
        else if (type == ActionType.RewardGold)
        {
            height += SingleRowHeight;
        }
        else if (type == ActionType.WaitTutorialStep)
        {
            height += SingleRowHeight * 3;
        }
        else if (type == ActionType.ExplainUIWithHighlight) 
        {
            height += SingleRowHeight * 4;
        }

        return height;
    }
}
#endif