using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

/// <summary>
/// 저장/로드 전담 매니저
/// - ISaveable을 구현한 모든 매니저의 데이터를 수집하여 저장
/// - 로드 시 각 매니저에 데이터 배포
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    private const string SAVE_FILE_NAME = "GameData.json";
    private const string BACKUP_FILE_NAME = "GameData.bak.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    public static string TempSavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME + ".tmp");
    public static string BackupPath => Path.Combine(Application.persistentDataPath, BACKUP_FILE_NAME);

    private const float AUTO_SAVE_INTERVAL = 60f;
    private Coroutine _coAutoSave;

    // ISaveable을 구현한 매니저 목록
    private List<ISaveable> _saveables = new List<ISaveable>();

    private JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public void Init()
    {
        // 1. 저장 가능한 매니저들 등록
        _saveables.Clear();

    }

    #region Registration

    /// <summary>
    /// ISaveable 매니저 등록
    /// </summary>
    public void Register(ISaveable saveable)
    {
        if (!_saveables.Contains(saveable))
        {
            _saveables.Add(saveable);
        }
    }

    /// <summary>
    /// ISaveable 매니저 해제
    /// </summary>
    public void Unregister(ISaveable saveable)
    {
        _saveables.Remove(saveable);
    }

    /// <summary>
    /// 모든 매니저 자동 등록 (초기화 시 호출)
    /// </summary>
    public void RegisterAllManagers()
    {
        _saveables.Clear();

        // 1순위: 설정 복원 (언어/폰트가 다른 매니저 LoadFrom보다 먼저 적용되어야 함)
        Register(DataManager.Instance);

        // 2순위: 게임 기반 수치 (골드, 인지도 등 — 다른 매니저들이 참조 가능)
        Register(GameDataManager.Instance);

        // 3순위: 게임플레이 매니저 (상호 의존 없음, 알파벳/기능 순)
        Register(TimeManager.Instance);
        Register(DiscipleManager.Instance);
        Register(BuildingManager.Instance);
        Register(TrainingManager.Instance);  // ← 누락 복구
        Register(LetterManager.Instance);
        Register(PrayerManager.Instance);
        Register(IAPManager.Instance);
        Register(RenownEventManager.Instance);
    }

    #endregion

    #region AutoSave

    public void StartAutoSave()
    {
        StopAutoSave();
        _coAutoSave = StartCoroutine(CoAutoSave());
    }

    public void StopAutoSave()
    {
        if (_coAutoSave != null)
        {
            StopCoroutine(_coAutoSave);
            _coAutoSave = null;
        }
    }

    private IEnumerator CoAutoSave()
    {
        WaitForSeconds wait = new WaitForSeconds(AUTO_SAVE_INTERVAL);

        while (true)
        {
            yield return wait;
            Save();
        }
    }

    #endregion

    #region Save / Load / Reset

    /// <summary>
    /// 저장 - 모든 매니저에서 데이터 수집 후 파일에 저장
    /// </summary>
    public void Save()
    {
        // 성능 측정 시작
        Stopwatch sw = new Stopwatch();
        sw.Start();

        // 1. 새 GameData 컨테이너 생성
        GameData gameData = new GameData();

        // 2. 모든 매니저에서 데이터 수집
        foreach (var saveable in _saveables)
        {
            saveable.SaveTo(gameData);
        }

        // 3. JSON 직렬화 (여기가 1차 병목)
        string json = JsonConvert.SerializeObject(gameData, _jsonSettings);

        // 4. Atomic Save (여기가 2차 병목 - 파일 쓰기)
        try
        {
            File.WriteAllText(TempSavePath, json);

            if (File.Exists(SavePath))
            {
                File.Copy(SavePath, BackupPath, true);
            }

            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(TempSavePath, SavePath);

            sw.Stop(); // 측정 종료
                       // 100ms(0.1초) 이상 걸리면 경고 로그 출력
            if (sw.ElapsedMilliseconds > 100)
            {
                UnityEngine.Debug.LogWarning($"[SaveManager] 저장 시간이 너무 깁니다! 소요 시간: {sw.ElapsedMilliseconds}ms ({sw.ElapsedMilliseconds / 1000f}초)");
            }
            else
            {
                UnityEngine.Debug.Log($"[SaveManager] 저장 완료 ({sw.ElapsedMilliseconds}ms)");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 로드 - 파일에서 읽어 각 매니저에 데이터 배포
    /// </summary>
    public void Load()
    {
        // Debug.Log("[SaveManager] Load 함수 호출됨");
        if (!File.Exists(SavePath))
        {
            // Debug.Log("[SaveManager] 저장 파일 없음. 새 게임 시작.");
            Reset();
            return;
        }

        try
        {
            // 1. 파일 읽기
            string json = File.ReadAllText(SavePath);

            // 2. 역직렬화
            GameData loadedData = JsonConvert.DeserializeObject<GameData>(json, _jsonSettings);

            if (loadedData != null)
            {
                // [변경점] GameManager.Instance.GameData = loadedData; 삭제됨!
                // 대신 아래 loop에서 GameDataManager.LoadFrom(loadedData)가 호출되며 주입됨.

                foreach (var saveable in _saveables)
                {
                    saveable.LoadFrom(loadedData);
                }

                OnLoadComplete();
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[SaveManager] 로드 실패: {e.Message}. 백업 파일 시도...");
            TryLoadBackup();
        }
    }

    /// <summary>
    /// 백업 파일 로드 시도
    /// </summary>
    private void TryLoadBackup()
    {
        if (!File.Exists(BackupPath))
        {
            // Debug.LogWarning("[SaveManager] 백업 파일도 없음. 새 게임 시작.");
            Reset();
            return;
        }

        try
        {
            string json = File.ReadAllText(BackupPath);
            GameData loadedData = JsonConvert.DeserializeObject<GameData>(json, _jsonSettings);

            if (loadedData != null)
            {
                foreach (var saveable in _saveables)
                {
                    saveable.LoadFrom(loadedData);
                }
                // Debug.Log("[SaveManager] 백업 파일 로드 완료");
                OnLoadComplete();
            }
        }
        catch (System.Exception e)
        {
            // Debug.LogError($"[SaveManager] 백업 로드도 실패: {e.Message}");
            Reset();
        }
    }

    /// <summary>
    /// 리셋 - 모든 매니저를 초기 상태로
    /// </summary>
    public void Reset()
    {
        foreach (var saveable in _saveables)
        {
            saveable.ResetToDefault();
        }

        CreateStartingDisciple();

        // 초기 상태 저장
        Save();

        // 게임 상태 동기화
        OnLoadComplete();

        // Debug.Log("[SaveManager] 게임 리셋 완료");
    }

    /// <summary>
    /// 저장 파일 삭제
    /// </summary>
    public void Delete()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            // Debug.Log("[SaveManager] 저장 파일 삭제됨");
        }

        if (File.Exists(BackupPath))
        {
            File.Delete(BackupPath);
        }
    }

    /// <summary>
    /// 로드 완료 후 게임 상태 동기화
    /// </summary>
    private void OnLoadComplete()
    {
        // 씬의 오브젝트 생성/동기화
        // 예: 제자 오브젝트 스폰, UI 갱신 등

        // ObjectManager가 있다면 제자 스폰
        if (ObjectManager.Instance != null)
        {
            ObjectManager.Instance.SpawnAllDisciples();
        }

    }

    public void CreateStartingDisciple()
    {
        // 1등급 제자 목록 가져오기
        var grade1Disciples = DataManager.Instance.GetDisciplesByGradeRange(1, 1);

        if (grade1Disciples.Count == 0)
        {
            // Debug.LogWarning("[SaveManager] 1등급 제자 템플릿이 없습니다.");
            return;
        }

        // 랜덤하게 1명 선택
        var template = grade1Disciples[UnityEngine.Random.Range(0, grade1Disciples.Count)];

        // DiscipleManager를 통해 제자 생성
        DiscipleManager.Instance.CreateAndAddNewDisciple(template.templateId);

        // Debug.Log($"[SaveManager] 초기 제자 생성: {template.nameKey}");
    }

    #endregion

    #region Application Lifecycle

    private void OnApplicationQuit()
    {
        // Debug.Log("[SaveManager] 앱 종료. 저장 중...");
        Save();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Debug.Log("[SaveManager] 앱 일시정지. 저장 중...");
            Save();
        }
    }

    #endregion
}