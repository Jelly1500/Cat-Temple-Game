using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

[CreateAssetMenu(menuName = "VN/Dialogue Event")]
public class DialogueEventData : ScriptableObject
{
    public string EventName;
    public ExecutionType executionType = ExecutionType.Repeated;
    public bool initiallyUnlocked = false;
    public List<DialogueEventData> nextEvents = new List<DialogueEventData>();
    public List<DialogueConditionData> conditions;
    public List<DialogueActionData> actions;
}

public class DialogueEvent : MonoBehaviour
{
    public DialogueEventData Data { get; private set; }
    private int currentActionIndex;
    private List<DialogueAction> runtimeActions;
    private List<DialogueCondition> runtimeConditions;
    private Coroutine currentCoroutine;
    private bool hasBeenExecuted = false;
    public bool IsUnlocked { get; private set; }

    public void Initialize(DialogueEventData data)
    {
        Data = data;
        currentActionIndex = 0;
        IsUnlocked = data.initiallyUnlocked;
        runtimeActions = new List<DialogueAction>();
        if (data.actions != null)
        {
            foreach (var actionData in data.actions)
            {
                runtimeActions.Add(new DialogueAction(actionData));
            }
        }
        runtimeConditions = new List<DialogueCondition>();
        if (data.conditions != null)
        {
            foreach (var conditionData in data.conditions)
            {
                var condition = new DialogueCondition();
                condition.Initialize(conditionData);
                runtimeConditions.Add(condition);
            }
        }
    }

    public bool CanExecute()
    {
        return IsUnlocked && (Data.executionType == ExecutionType.Repeated || !hasBeenExecuted) && (runtimeConditions == null || runtimeConditions.All(c => c.Check()));
    }

    public void Unlock()
    {
        IsUnlocked = true;
    }

    public bool IsFinished()
    {
        return currentActionIndex >= runtimeActions.Count || runtimeActions.All(a => a.IsFinished());
    }

    public void Execute(System.Action onCompleted = null)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        currentActionIndex = 0;
        currentCoroutine = StartCoroutine(CoExecuteAllActions(onCompleted));
    }

    private IEnumerator CoExecuteAllActions(System.Action onCompleted)
    {
        while (currentActionIndex < runtimeActions.Count)
        {
            runtimeActions[currentActionIndex].Execute();

            while (!runtimeActions[currentActionIndex].IsFinished())
            {
                yield return new WaitForSecondsRealtime(0.016f);
            }

            currentActionIndex++;
            yield return new WaitForSecondsRealtime(0.05f);
        }

        currentCoroutine = null;
        hasBeenExecuted = true;

        if (Data.nextEvents != null)
        {
            foreach (var nextEvent in Data.nextEvents)
            {
                if (nextEvent != null)
                {
                    DialogueManager.Instance.UnlockEvent(nextEvent);
                }
            }
        }

        DialogueManager.Instance.CloseDialogueUI();

        // [핵심 추가] 모든 액션 종료 및 UI 닫기 후 콜백 실행
        onCompleted?.Invoke();
    }

    public void Reset()
    {
        // Stop any running coroutine
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        currentActionIndex = 0;
        // Reset execution flag only for Repeated type
        if (Data.executionType == ExecutionType.Repeated)
        {
            hasBeenExecuted = false;
        }
        // Recreate runtime actions to reset state
        runtimeActions.Clear();
        if (Data.actions != null)
        {
            foreach (var actionData in Data.actions)
            {
                runtimeActions.Add(new DialogueAction(actionData));
            }
        }
        // Reset conditions
        runtimeConditions.Clear();
        if (Data.conditions != null)
        {
            foreach (var conditionData in Data.conditions)
            {
                var condition = new DialogueCondition();
                condition.Initialize(conditionData);
                runtimeConditions.Add(condition);
            }
        }
    }

    public void SetDialogueActionFinished()
    {
        if (currentActionIndex < runtimeActions.Count)
        {
            runtimeActions[currentActionIndex].SetDialogueFinished();
        }

        foreach (var cond in runtimeConditions)
        {
            if (cond != null && cond.Data != null && cond.Data.type == ConditionType.CoolTime)
            {
                cond.RecordExecutionTime();
            }
        }
    }
}

public enum ExecutionType
{
    Once,
    Repeated
}