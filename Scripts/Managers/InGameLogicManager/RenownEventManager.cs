using UnityEngine;

public class RenownEventManager : Singleton<RenownEventManager>, ISaveable
{
    [Header("Settings")]
    public int _nextTargetRenown = 0;

    private const int RENOWN_INTERVAL = 10;
    private const int MAX_RENOWN_EVENT = 100;

    public void Init()
    {
        EventManager.Instance.AddEvent(Define.EEventType.DateChanged, OnDayPassed);
        SaveManager.Instance.Register(this);
    }

    private void OnDayPassed()
    {
        int currentRenown = GameDataManager.Instance.Renown;

        if (currentRenown >= _nextTargetRenown)
        {
            TriggerRenownEvent();
        }
    }

    private void TriggerRenownEvent()
    {
        string eventName = $"DialogueEvent_Renown{_nextTargetRenown}";

        Debug.Log($"RenownEvent Triggered : {eventName}");

        // 1. 대화 이벤트 시작 시 배너 즉시 숨기기 (Null 체크 생략)
        AdsManager.Instance.HideBannerAds();

        // 2. 대화 이벤트 실행 및 종료 시 배너 다시 표시
        // (이 부분은 DialogueManager의 실제 구현에 따라 달라질 수 있습니다)
        DialogueManager.Instance.StartDialogueEvent(
            eventName,
            checkCanExecute: false,
            onCompleted: () =>
            {
                // 대화 종료 시점에 다시 배너 활성화
                AdsManager.Instance.ResumeBannerAds();
            }
        );

        // 다음 목표 설정
        if (_nextTargetRenown < MAX_RENOWN_EVENT)
        {
            SetNextTarget(_nextTargetRenown + RENOWN_INTERVAL);
        }
        else
        {
            SetNextTarget(int.MaxValue);
        }
    }

    private void SetNextTarget(int newTarget)
    {
        _nextTargetRenown = newTarget;
    }

    #region ISaveable Implementation

    public void SaveTo(GameData data)
    {
        data.renownEventTarget = _nextTargetRenown;
    }

    public void LoadFrom(GameData data)
    {
        _nextTargetRenown = data.renownEventTarget;
    }

    public void ResetToDefault()
    {
        _nextTargetRenown = 0;
    }

    #endregion
}