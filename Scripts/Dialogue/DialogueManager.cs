using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class DialogueManager : Singleton<DialogueManager>
{
    public UI_RenownEventDialogue dialogueUI;
    [SerializeField]
    private List<DialogueEventData> events = new List<DialogueEventData>();
    [SerializeField]
    private List<CharacterData> characterDataList = new List<CharacterData>();

    public DialogueEvent CurrentEvent { get; private set; }
    private List<DialogueEvent> eventInstances = new List<DialogueEvent>();

    // Resources에서 로드된 이벤트 데이터 캐시
    private Dictionary<string, DialogueEventData> eventDataCache = new Dictionary<string, DialogueEventData>();
    private Dictionary<string, DialogueEvent> dynamicEventInstances = new Dictionary<string, DialogueEvent>();

    private bool _isInitialized = false;

    public void Init()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        // Pre-create DialogueEvent instances from serialized list
        eventInstances = new List<DialogueEvent>();
        if (events != null)
        {
            foreach (var eventData in events)
            {
                if (eventData == null) continue;

                GameObject eventGO = new GameObject($"DialogueEvent_{eventData.EventName}");
                eventGO.transform.SetParent(transform);
                DialogueEvent dialogueEvent = eventGO.AddComponent<DialogueEvent>();
                dialogueEvent.Initialize(eventData);
                eventInstances.Add(dialogueEvent);
            }
        }

        // Load all DialogueEventData from Resources
        LoadAllDialogueEventData();

        // Load all CharacterData from Resources
        LoadAllCharacterData();

    }

    private void LoadAllDialogueEventData()
    {
        // PreLoad 폴더 하위의 모든 DialogueEventData 로드
        DialogueEventData[] allEvents = Resources.LoadAll<DialogueEventData>("PreLoad");

        foreach (var eventData in allEvents)
        {
            if (eventData == null) continue;

            if (!eventDataCache.ContainsKey(eventData.EventName))
            {
                eventDataCache[eventData.EventName] = eventData;
            }
        }

    }

    private void LoadAllCharacterData()
    {
        if (characterDataList == null)
        {
            characterDataList = new List<CharacterData>();
        }

        // PreLoad 폴더 하위의 모든 CharacterData 로드
        CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("PreLoad");

        // 기존 리스트에 없는 캐릭터만 추가
        foreach (var character in allCharacters)
        {
            if (character == null) continue;

            if (!characterDataList.Any(c => c.characterId == character.characterId))
            {
                characterDataList.Add(character);
            }
        }

    }

    private DialogueEvent GetOrCreateEventInstance(string eventName)
    {
        // 1. 기존 serialized 리스트에서 검색
        var existingEvent = eventInstances.FirstOrDefault(e => e.Data.EventName == eventName);
        if (existingEvent != null)
            return existingEvent;

        // 2. 동적으로 생성된 인스턴스에서 검색
        if (dynamicEventInstances.TryGetValue(eventName, out var cachedEvent))
            return cachedEvent;

        // 3. Resources 캐시에서 데이터를 찾아 새로 생성
        if (eventDataCache.TryGetValue(eventName, out var eventData))
        {
            GameObject eventGO = new GameObject($"DialogueEvent_{eventName}");
            eventGO.transform.SetParent(transform);
            DialogueEvent dialogueEvent = eventGO.AddComponent<DialogueEvent>();
            dialogueEvent.Initialize(eventData);

            dynamicEventInstances[eventName] = dialogueEvent;
            return dialogueEvent;
        }

        return null;
    }

    public void StartDialogueEvent(DialogueEventData dialogueEventData, bool checkCanExecute = true, System.Action onCompleted = null)
    {
        if (dialogueEventData == null)
        {
            onCompleted?.Invoke(); // 안전장치
            return;
        }

        var dialogueEvent = GetOrCreateEventInstance(dialogueEventData.EventName);
        if (dialogueEvent == null)
        {
            onCompleted?.Invoke();
            return;
        }

        StartDialogueEvent(dialogueEvent, checkCanExecute, onCompleted);
    }

    public void StartDialogueEvent(string eventName, bool checkCanExecute = true, System.Action onCompleted = null)
    {
        var dialogueEvent = GetOrCreateEventInstance(eventName);
        StartDialogueEvent(dialogueEvent, checkCanExecute, onCompleted);
    }

    public void StartDialogueEvent(DialogueEvent dialogueEvent, bool checkCanExecute = true, System.Action onCompleted = null)
    {
        if (dialogueEvent == null)
        {
            Debug.LogWarning("[DialogueManager] DialogueEvent is null");
            onCompleted?.Invoke();
            return;
        }

        if (checkCanExecute && !dialogueEvent.CanExecute())
        {
            Debug.LogWarning($"[DialogueManager] DialogueEvent '{dialogueEvent.Data.EventName}' cannot execute - conditions not met");
            onCompleted?.Invoke();
            return;
        }

        CurrentEvent = dialogueEvent;
        // [수정] 콜백을 Execute로 전달
        CurrentEvent.Execute(onCompleted);
    }

    public void ShowDialogue(DialogueActionData data)
    {
        var popup = UIManager.Instance.ShowPopupUI<UI_RenownEventDialogue>("UI_RenownEventDialogue");
        dialogueUI = popup;

        var (leftName, leftSprite) = GetCharacterInfo(data.leftCharacterId, data.leftCharacterState);
        var (rightName, rightSprite) = GetCharacterInfo(data.rightCharacterId, data.rightCharacterState);

        string dialogueText = data.GetDialogueText();

        dialogueUI.SetDialogue(
            dialogueText,
            leftName, leftSprite,
            rightName, rightSprite,
            data.speakerSide,
            data.leftEmotionSprite, data.rightEmotionSprite,
            OnDialogueFinished
        );
    }

    public void ShowChoiceDialogue(DialogueActionData data)
    {
        var popup = UIManager.Instance.ShowPopupUI<UI_RenownEventDialogue>("UI_RenownEventDialogue");
        dialogueUI = popup;

        var (leftName, leftSprite) = GetCharacterInfo(data.leftCharacterId, data.leftCharacterState);
        var (rightName, rightSprite) = GetCharacterInfo(data.rightCharacterId, data.rightCharacterState);

        string dialogueText = data.GetDialogueText();
        string leftChoiceText = data.GetLeftChoiceText();
        string rightChoiceText = data.GetRightChoiceText();

        dialogueUI.SetChoiceDialogue(
            dialogueText,
            leftName, leftSprite,
            rightName, rightSprite,
            data.speakerSide,
            data.leftEmotionSprite, data.rightEmotionSprite,
            leftChoiceText, rightChoiceText,
            () => OnChoiceSelected(data.leftChoiceEvent),
            () => OnChoiceSelected(data.rightChoiceEvent)
        );
    }

    private (string name, Sprite sprite) GetCharacterInfo(CharacterId id, CharacterState state)
    {
        if (id == CharacterId.None) return (null, null);

        var data = characterDataList.FirstOrDefault(d => d.characterId == id);
        if (data == null) return (null, null);

        var expression = data.expressions?.FirstOrDefault(e => e.characterState == state);
        return (data.characterName, expression?.sprite);
    }

    private void OnChoiceSelected(DialogueEventData choiceEvent)
    {
        if (choiceEvent != null)
        {
            UnlockEvent(choiceEvent);
        }

        CurrentEvent?.SetDialogueActionFinished();
    }

    public void OnDialogueFinished()
    {
        // Notify current dialogue event that dialogue action is finished
        CurrentEvent?.SetDialogueActionFinished();
    }

    public void CloseDialogueUI()
    {
        if (dialogueUI != null)
        {
            UIManager.Instance.ClosePopupUI(dialogueUI);
            dialogueUI = null;
        }
    }

    public bool IsCurrentEventFinished()
    {
        return CurrentEvent == null || CurrentEvent.IsFinished();
    }

    public List<DialogueEvent> GetAvailableEvents()
    {
        return eventInstances.Where(e => e.CanExecute()).ToList();
    }

    public void UnlockEvent(DialogueEventData eventData)
    {
        var dialogueEvent = GetOrCreateEventInstance(eventData.EventName);
        if (dialogueEvent != null)
        {
            dialogueEvent.Unlock();
        }
    }
}