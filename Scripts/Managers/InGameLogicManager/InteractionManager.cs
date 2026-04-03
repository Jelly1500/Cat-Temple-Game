using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Define;

/// <summary>
/// 상호작용 관리자 - 방문객 생성, 대화 관리, 제자 구역 할당을 담당합니다.
/// 
/// [설계 원칙]
/// 1. GameManager 대신 개별 매니저들을 직접 참조
/// 2. 자원 변경은 GameDataManager에 위임
/// 3. 건물 효과는 BuildingManager에서 조회
/// </summary>
public class InteractionManager : Singleton<InteractionManager>
{
    #region Visitor Spawn Configuration

    [System.Serializable]
    public struct VisitorSpawnRate
    {
        public int RenownBenchmark;
        public float[] Probabilities;
    }

    [SerializeField]
    private List<VisitorSpawnRate> _spawnRates = new List<VisitorSpawnRate>
    {
        // [수정] 0~10 구간: 2명 방문 확률 30%로 증가 (기존: 1명 100%)
        new VisitorSpawnRate { RenownBenchmark = 10, Probabilities = new float[] { 70f, 30f, 0f, 0f, 0f } },
        new VisitorSpawnRate { RenownBenchmark = 30, Probabilities = new float[] { 80f, 20f, 0f, 0f, 0f } },
        new VisitorSpawnRate { RenownBenchmark = 50, Probabilities = new float[] { 40f, 40f, 20f, 0f, 0f } },
        new VisitorSpawnRate { RenownBenchmark = 80, Probabilities = new float[] { 20f, 30f, 30f, 20f, 0f } },
        new VisitorSpawnRate { RenownBenchmark = 120, Probabilities = new float[] { 10f, 15f, 25f, 30f, 20f } }
    };

    #endregion

    #region Initialization

    public void Init()
    {
        EventManager.Instance.AddEvent(EEventType.DateChanged, OnDateChanged);
    }

    #endregion

    #region Event Handlers

    private void OnDateChanged()
    {
        // [수정] GameManager 대신 GameDataManager에서 인지도 조회
        int currentRenown = GameDataManager.Instance.Renown;

        StartCoroutine(CoDailyRoutine(currentRenown));
    }

    #endregion

    #region Daily Routine

    private IEnumerator CoDailyRoutine(int renown)
    {
        // 1. 방문객 생성
        yield return StartCoroutine(SpawnDailyVisitorsRoutine(renown));

        // 2. 대기
        yield return new WaitForSeconds(0.5f);

        // 3. 구역 할당
        AssignZonesToIdleDisciples();
    }

    #endregion

    #region Zone Assignment

    private void AssignZonesToIdleDisciples()
    {
        if (MapManager.Instance == null || ObjectManager.Instance == null)
        {
            Debug.LogWarning("[InteractionManager] MapManager 또는 ObjectManager가 없습니다.");
            return;
        }

        if (MapManager.Instance.ZoneCount == 0)
        {
            Debug.LogWarning("[InteractionManager] 구역 타일맵이 없습니다.");
            return;
        }

        var allDisciples = ObjectManager.Instance.PlayerRoot.GetComponentsInChildren<Disciple>();
        var idleDisciples = allDisciples.Where(d => !d.IsTalking).ToList();


        ShuffleList(idleDisciples);

        foreach (var disciple in idleDisciples)
        {
            if (disciple.IsTalking) continue;

            int zoneId = MapManager.Instance.GetRandomAvailableZone(disciple.ID);

            if (zoneId > 0)
            {
                disciple.AssignZone(zoneId);
            }
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    #endregion

    #region Visitor Spawning

    private IEnumerator SpawnDailyVisitorsRoutine(int renown)
    {
        int visitorCount = GetDailyVisitorCount(renown);

        for (int i = 0; i < visitorCount; i++)
        {
            ObjectManager.Instance.SpawnVisitor(MapManager.Instance.GetRandomSpawnPosition());
            yield return new WaitForSeconds(Random.Range(2.0f, 4.0f));
        }
    }

    private int GetDailyVisitorCount(int r)
    {
        // [수정] 기존에 10 이하일 때 무조건 1명을 리턴하던 코드 삭제
        // if (r <= 10) return 1; 

        VisitorSpawnRate prev = _spawnRates[0];
        VisitorSpawnRate next = _spawnRates[_spawnRates.Count - 1];
        bool foundRange = false;

        for (int i = 0; i < _spawnRates.Count - 1; i++)
        {
            if (r >= _spawnRates[i].RenownBenchmark && r <= _spawnRates[i + 1].RenownBenchmark)
            {
                prev = _spawnRates[i];
                next = _spawnRates[i + 1];
                foundRange = true;
                break;
            }
        }

        if (!foundRange)
        {
            if (r > next.RenownBenchmark)
            {
                // 최대치 초과 시 마지막 확률 사용
                return GetRandomFromProbs(next.Probabilities);
            }
            else
            {
                // 최소치 미만(또는 범위 못 찾음) 시 첫 번째 확률 사용
                // r <= 10 인 경우 여기서 처리됨 (prev는 _spawnRates[0])
            }
        }

        float rangeSize = next.RenownBenchmark - prev.RenownBenchmark;
        float currentPos = r - prev.RenownBenchmark;
        float t = (rangeSize > 0) ? Mathf.Clamp01(currentPos / rangeSize) : 0f;

        float[] currentProbs = new float[5];
        for (int i = 0; i < 5; i++)
        {
            float p1 = (i < prev.Probabilities.Length) ? prev.Probabilities[i] : 0f;
            float p2 = (i < next.Probabilities.Length) ? next.Probabilities[i] : 0f;
            currentProbs[i] = Mathf.Lerp(p1, p2, t);
        }

        return GetRandomFromProbs(currentProbs);
    }

    private int GetRandomFromProbs(float[] probs)
    {
        float total = probs.Sum();
        float randomPoint = Random.Range(0f, total);
        float currentSum = 0f;

        for (int i = 0; i < probs.Length; i++)
        {
            currentSum += probs[i];
            if (randomPoint <= currentSum)
            {
                return i + 1;
            }
        }
        return 1;
    }

    #endregion

    #region Visitor Lifecycle

    // 현재 활성 방문객 수 (대화 진행 중 포함)
    private int _activeVisitorCount = 0;

    /// <summary>
    /// 현재 활성 방문객이 존재하는지 여부
    /// </summary>
    public bool HasActiveVisitors => _activeVisitorCount > 0;

    /// <summary>
    /// 현재 활성 방문객 수
    /// </summary>
    public int ActiveVisitorCount => _activeVisitorCount;

    public void OnVisitorSpawned(Visitor visitor)
    {
        _activeVisitorCount++;
        StartCoroutine(CoManageVisitorLifeCycle(visitor));
    }

    private IEnumerator CoManageVisitorLifeCycle(Visitor visitor)
    {
        yield return new WaitForSeconds(0.5f);

        if (MapManager.Instance != null)
        {
            visitor._CellPosition = MapManager.Instance.WorldToCell(visitor.transform.position);
        }

        // [수정] 대화 가능한 제자 찾기 (최대 2회 시도)
        Disciple targetDisciple = null;
        int searchAttempts = 0;
        int maxAttempts = 2;

        while (targetDisciple == null && searchAttempts < maxAttempts)
        {
            targetDisciple = FindAvailableDisciple();
            searchAttempts++;

            if (targetDisciple == null)
            {
                // 마지막 시도가 아닐 때만 1초 대기 후 재시도
                if (searchAttempts < maxAttempts)
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }

        // [추가] 2회 탐색 모두 실패 시 즉시 소멸 처리
        if (targetDisciple == null)
        {
            UIManager.Instance.ShowGameToast("UI_Toast_VisitorGone");
            ObjectManager.Instance.Despawn(visitor);
            _activeVisitorCount--;
            yield break;
        }

        // [수정] ReserveForTalk()로 제자 이동을 즉시 멈추고 위치를 타일 중앙에 고정.
        targetDisciple.ReserveForTalk();

        // ReserveForTalk() 이후 스냅된 위치로 확정된 셀 좌표를 가져옴
        Vector2Int discipleCellPos = targetDisciple._CellPosition;

        // 방문객 위치에서 가장 가깝고 이동 가능한 제자 옆 타일을 찾음
        Vector2Int destPos = GetBestInteractionPosition(discipleCellPos, visitor._CellPosition);

        if (destPos == Vector2Int.zero)
        {
            Debug.LogWarning($"[InteractionManager] {targetDisciple.name} 주변에 접근 가능한 타일이 없습니다. 제자 위치로 강제 설정합니다.");
            destPos = discipleCellPos;
        }

        // 경로 탐색
        List<Vector2Int> path = MapManager.Instance.FindPath(visitor._CellPosition, destPos);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[InteractionManager] 경로 없음: {visitor._CellPosition} -> {destPos}. 강제 이동합니다.");
            visitor.transform.position = MapManager.Instance.CellToWorld(destPos);
            visitor._CellPosition = destPos;
        }

        if (path != null && path.Count > 0)
        {
            yield return StartCoroutine(visitor.CoMoveAlongPath(path));
        }

        // 대화 시작 (방문객이 도착한 후)
        yield return StartCoroutine(CoProcessConversation(visitor, targetDisciple));

        // 대화 종료
        targetDisciple.EndTalk();

        // 퇴장 처리
        Vector2Int exitPos = MapManager.Instance.GetRandomExitPosition();
        visitor._CellPosition = MapManager.Instance.WorldToCell(visitor.transform.position);
        List<Vector2Int> exitPath = MapManager.Instance.FindPath(visitor._CellPosition, exitPos);

        if (exitPath != null && exitPath.Count > 0)
        {
            yield return StartCoroutine(visitor.CoMoveAlongPath(exitPath));
        }

        ObjectManager.Instance.Despawn(visitor);
        _activeVisitorCount--;
    }

    private Vector2Int GetBestInteractionPosition(Vector2Int targetPos, Vector2Int startPos)
    {
        // 상하좌우 2칸 이격된 오프셋
        Vector2Int[] offsets =
        {
            new Vector2Int(0, 2),   // 상
            new Vector2Int(0, -2),  // 하
            new Vector2Int(-2, 0),  // 좌
            new Vector2Int(2, 0)    // 우
        };

        Vector2Int bestPos = Vector2Int.zero;
        float minDistance = float.MaxValue;
        bool found = false;

        foreach (var offset in offsets)
        {
            Vector2Int neighbor = targetPos + offset;

            // 1. 해당 타일이 이동 가능한지 확인
            if (MapManager.Instance.CanMove(neighbor))
            {
                // 2. 해당 위치까지 실제 경로가 존재하는지 확인
                List<Vector2Int> testPath = MapManager.Instance.FindPath(startPos, neighbor);
                if (testPath == null || testPath.Count == 0) continue;

                // 3. 방문객 시작 위치로부터의 거리 계산 (유클리드 거리)
                float dist = Vector2Int.Distance(neighbor, startPos);

                // 4. 가장 가까운 곳 선택
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestPos = neighbor;
                    found = true;
                }
            }
        }

        // 유효한 위치를 찾았다면 반환, 아니면 (0,0) 반환
        return found ? bestPos : Vector2Int.zero;
    }

    private Disciple FindAvailableDisciple()
    {
        var allDisciples = ObjectManager.Instance.PlayerRoot.GetComponentsInChildren<Disciple>();

        // 대화 중이 아닌 제자들만 필터링
        var candidates = allDisciples.Where(d => !d.IsTalking).ToList();

        if (candidates.Count > 0)
        {
            // [수정 로직]
            // 인내(Patience) + 공감(Empathy) + 지혜(Wisdom) 합계 기준으로 내림차순 정렬
            // 가장 높은 점수를 가진 첫 번째 제자를 반환
            var bestDisciple = candidates
                .OrderByDescending(d => d.Data.Patience + d.Data.Empathy + d.Data.Wisdom)
                .FirstOrDefault();

            return bestDisciple;
        }
        return null;
    }

    #endregion

    #region Conversation

    private IEnumerator CoProcessConversation(Visitor visitor, Disciple disciple)
    {
        // [수정] BeginTalk 호출은 여기서만. CoManageVisitorLifeCycle에서의 중복 호출 제거됨.
        // IsTalking은 이미 true이므로 BeginTalk 내부의 StopRoaming + 상태 세팅만 실행됨.
        disciple.BeginTalk(visitor._CellPosition);
        visitor.LookAt(disciple._CellPosition);
        visitor.State = Cat.ECatState.Talk;

        int successCount = 0;
        int totalRolls = 0;
        float rewardScore = 0f;

        // 3단계 대화
        var phases = new[]
        {
            new { vIcon = "Icon_Say", dIcon = "Icon_Listen", dStat = disciple.Data.Patience, vStat = visitor.StatComplaint, buff = EBuildingEffectType.IncreasePatience },
            new { vIcon = "Icon_Touched", dIcon = "Icon_Empathy", dStat = disciple.Data.Empathy, vStat = visitor.StatComfort, buff = EBuildingEffectType.IncreaseEmpathy },
            new { vIcon = "Icon_HeadShaking", dIcon = "Icon_Wisdom", dStat = disciple.Data.Wisdom, vStat = visitor.StatAnswer, buff = EBuildingEffectType.IncreaseWisdom }
        };

        foreach (var phase in phases)
        {
            for (int i = 0; i < 2; i++)
            {
                totalRolls++;
                bool isSuccess = RollDice(phase.dStat, phase.vStat, phase.buff);

                if (isSuccess)
                {
                    successCount++;
                    visitor.ShowChatIcon(phase.vIcon + "_Success");
                    disciple.ShowChatIcon(phase.dIcon + "_Success");
                    rewardScore += (phase.vStat * 0.5f);
                }
                else
                {
                    visitor.ShowChatIcon(phase.vIcon + "_Fail");
                    disciple.ShowChatIcon(phase.dIcon + "_Fail");
                }
                yield return new WaitForSeconds(1.5f);
            }
        }

        // 결과 정산
        int finalReward = Mathf.FloorToInt(rewardScore * 10f);
        int actualGoldGained = GameDataManager.Instance.AddGold(finalReward);

        int renownReward = 0;
        if (successCount >= 4)
        {
            LetterManager.Instance?.ScheduleVisitorLetter(visitor, actualGoldGained);
            visitor.ShowChatIcon("Icon_Happy");
        }
        else
        {
            visitor.ShowChatIcon("Icon_Normal");
        }

        if (renownReward > 0)
        {
            UIManager.Instance.ShowGameToast("UI_Toast_GetGoldAndLetter", actualGoldGained);
        }
        else
        {
            UIManager.Instance.ShowGameToast("UI_Toast_GetGold", actualGoldGained);
        }

        yield return new WaitForSeconds(1.0f);
        visitor.State = Cat.ECatState.Idle;

        disciple.ReleaseChatBubble();
        visitor.ReleaseChatBubble();
    }

    #endregion

    #region Dice Roll

    public bool RollDice(int discipleStat, int visitorStat, EBuildingEffectType buffType)
    {
        // [수정] BuildingManager에서 직접 효과 조회
        float buildingBonus = BuildingManager.Instance?.GetTotalEffectValue(buffType) ?? 0f;
        float finalStat = discipleStat + buildingBonus;
        float threshold = 50f - (finalStat - visitorStat);
        return Random.Range(0, 100) >= threshold;
    }

    #endregion
}