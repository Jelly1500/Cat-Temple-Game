using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static Define;

/// 게임 매니저 (Lean Version)
/// 
/// 역할:
/// - 게임 전체 흐름 조율 (초기화, 게임 루프)
/// - 다른 매니저들 간의 조율
/// - 게임 상태 관리 (일시정지, 게임오버 등)

public class GameManager : Singleton<GameManager>
{
    #region Game State

    public enum EGameState
    {
        Loading,
        Playing,
        Paused,
        GameOver
    }

    private EGameState _currentState = EGameState.Loading;
    public EGameState CurrentState => _currentState;

    public bool IsPlaying => _currentState == EGameState.Playing;

    #endregion



    #region Initialization

    /// <summary>
    /// 게임 초기화 (기존 동기식 메서드는 코루틴 실행용으로 변경)
    /// </summary>
    public void InitializeGame()
    {
        StartCoroutine(InitializeGameRoutine());
    }

    /// <summary>
    /// 비동기 초기화 시퀀스
    /// </summary>
    private IEnumerator InitializeGameRoutine()
    {
        _currentState = EGameState.Loading;

        // 1. 모든 매니저 Init 호출
        InitializeManagers();

        // 2. 비동기 SDK 매니저 대기 (최대 5초 안전장치)
        float timeout = 5.0f;
        float timer = 0f;

        while ((!AdsManager.Instance.IsInitialized || !IAPManager.Instance.IsInitialized) && timer < timeout)
        {
            Debug.Log($"[Init Check] Ads: {AdsManager.Instance.IsInitialized}, IAP: {IAPManager.Instance.IsInitialized}");
            timer += Time.unscaledDeltaTime; // Time.timeScale의 영향을 받지 않는 시간 사용
            yield return null;
        }

        // 3. 타임아웃 발생 시 로그 출력 (게임은 정상 진행)
        if (timer >= timeout)
        {
            Debug.LogWarning("[GameManager] SDK 초기화 시간 초과. 일부 기능(광고/결제)이 오프라인 상태일 수 있습니다.");
        }

        // 4. 로드 및 게임 시작 처리
        SaveManager.Instance.Load();

        _currentState = EGameState.Playing;
        SaveManager.Instance.StartAutoSave();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshAllActiveUI();
        }

        StartBGM();

        // 5. 초기화 및 로드 완료 후 이벤트 트리거
        EventManager.Instance.TriggerEvent(EEventType.DateChanged);
    }

    private void InitializeManagers()
    {
        // 1. 코어 시스템
        ResourceManager.Instance.Init();
        DataManager.Instance.Init();
        SoundManager.Instance.Init();
        FontManager.Instance.Init();
        GameDataManager.Instance.Init();

        // 2. 게임플레이 시스템
        TimeManager.Instance.Init();
        DiscipleManager.Instance.Init();
        BuildingManager.Instance.Init();
        TrainingManager.Instance.Init();

        // 3. 콘텐츠 시스템
        LetterManager.Instance.Init();
        PrayerManager.Instance.Init();
        DialogueManager.Instance.Init();
        RenownEventManager.Instance.Init();

        // [수정] 주석 해제하여 SDK 연동 매니저 초기화 시작
        AdsManager.Instance.Init();
        IAPManager.Instance.Init();

        MapManager.Instance.Init();
        ObjectManager.Instance.Init();
        InteractionManager.Instance.Init();

        TutorialManager.Instance.Init();

        // 6. UI 초기화
        UIManager.Instance.Init();
    }

    #endregion

    #region BGM Control

    /// <summary>
    /// BGM 플레이리스트 재생 시작
    /// BGMPlaylistController가 씬에 존재해야 함
    /// </summary>
    private void StartBGM()
    {
        if (BGMPlaylistController.Instance != null)
        {
            BGMPlaylistController.Instance.StartPlaylist();
        }
        else
        {
            Debug.LogWarning("[GameManager] BGMPlaylistController가 씬에 없습니다. BGM이 재생되지 않습니다.");
        }
    }

    /// <summary>
    /// BGM 플레이리스트 중지
    /// </summary>
    public void StopBGM()
    {
        if (BGMPlaylistController.Instance != null)
        {
            BGMPlaylistController.Instance.StopPlaylist();
        }
    }

    #endregion

    #region Game State Control

    public void PauseGame()
    {
        if (_currentState != EGameState.Playing) return;

        _currentState = EGameState.Paused;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (_currentState != EGameState.Paused) return;

        _currentState = EGameState.Playing;
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        _currentState = EGameState.GameOver;
        SaveManager.Instance.StopAutoSave();
    }

    #endregion


    #region Input & Exit Control

    private void Update()
    {
        // 안드로이드 뒤로 가기(Escape) 버튼 입력 감지
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleBackButton();
        }
    }

    /// <summary>
    /// 뒤로 가기 버튼 입력 시 상태에 따른 처리 요청
    /// </summary>
    private void HandleBackButton()
    {
        if (_currentState == EGameState.Playing)
        {
            PauseGame();
            // UIManager에게 팝업 표시를 요청 (객체지향적 위임)
            UIManager.Instance.ShowExitPopup();
        }
        else if (_currentState == EGameState.Paused)
        {
            // 이미 팝업이 떠있거나 일시정지 상태인 경우 팝업을 닫고 게임 재개
            UIManager.Instance.CloseExitPopup();
            ResumeGame();
        }
    }

    /// <summary>
    /// 실제 애플리케이션 종료 처리 (팝업의 '확인' 버튼에서 호출)
    /// </summary>
    public void QuitApplication()
    {
        // 종료 전 자동 저장 수행
        SaveManager.Instance.Save();

        Debug.Log("게임을 종료합니다.");
        Application.Quit();
    }

    #endregion
    // 시간 관련

    public void CalculateFutureDate(int days, out int y, out int m, out int d)
        => TimeManager.Instance.CalculateFutureDate(days, out y, out m, out d);

    public bool IsDateReached(int year, int month, int day)
        => TimeManager.Instance.IsDateReached(year, month, day);


}