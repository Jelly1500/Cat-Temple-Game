using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 맵 관리자 - 타일맵 기반 이동, 경로 탐색, 구역 관리를 담당합니다.
/// [신규] 10개의 구역(Zone) 타일맵을 지원합니다.
/// 
/// 타일맵 네이밍 규칙:
/// - Tilemap_Base: 사찰 내부 기본 바닥
/// - Tilemap_Entrance: 입구 영역 (방문객 스폰/퇴장)
/// - Tilemap_Zone_01 ~ Tilemap_Zone_10: 제자 배회 구역 (개발자가 Unity에서 페인팅)
/// </summary>
public class MapManager : Singleton<MapManager>
{
    // ═══════════════════════════════════════════════════════════════
    // 타일맵 참조
    // ═══════════════════════════════════════════════════════════════

    #region Tilemaps & Map Initialization
    [Header("기본 타일맵")]
    [SerializeField] private Tilemap _tilemapBase;      // 사찰 내부 (기존 바닥)
    [SerializeField] private Tilemap _tilemapEntrance;  // 입구 영역 (파란색 타일)

    [Header("구역 타일맵 (10개)")]
    [Tooltip("제자 배회 구역. Unity에서 Tilemap_Zone_01 ~ Tilemap_Zone_10으로 이름 지정")]
    [SerializeField] private List<Tilemap> _zoneTilemaps = new List<Tilemap>();

    // 내부 데이터
    private BoundsInt _totalBounds;
    private bool[,] _walkableMap;
    private List<Vector2Int> _entranceCoordinates = new List<Vector2Int>();

    // 구역별 셀 좌표 캐시
    private Dictionary<int, List<Vector2Int>> _zoneCells = new Dictionary<int, List<Vector2Int>>();

    // 구역 점유 상태 (구역ID -> 제자ID)
    private Dictionary<int, string> _zoneOccupancy = new Dictionary<int, string>();

    /// <summary>총 구역 수</summary>
    public int ZoneCount => _zoneTilemaps.Count;

    /// <summary>구역이 존재하는지 여부</summary>
    public bool HasZones => _zoneCells.Count > 0;

    public void Init()
    {
        // 인스펙터에서 할당하지 않았을 경우 자동 찾기
        if (_tilemapBase == null)
            _tilemapBase = GameObject.Find("Tilemap_Base")?.GetComponent<Tilemap>();

        if (_tilemapEntrance == null)
            _tilemapEntrance = GameObject.Find("Tilemap_Entrance")?.GetComponent<Tilemap>();

        // 구역 타일맵 자동 찾기 (인스펙터에서 할당하지 않은 경우)
        if (_zoneTilemaps.Count == 0)
        {
            AutoFindZoneTilemaps();
        }

        InitializeMap();
        InitializeZones();
    }

    /// <summary>
    /// Tilemap_Zone_01 ~ Tilemap_Zone_10 자동 탐색
    /// </summary>
    private void AutoFindZoneTilemaps()
    {
        _zoneTilemaps.Clear();

        for (int i = 1; i <= 10; i++)
        {
            string tilemapName = $"Tilemap_Zone_{i:D2}"; // Tilemap_Zone_01, 02, ...
            GameObject go = GameObject.Find(tilemapName);

            if (go != null)
            {
                Tilemap tm = go.GetComponent<Tilemap>();
                if (tm != null)
                {
                    _zoneTilemaps.Add(tm);
                }
            }
        }

    }

    private void InitializeMap()
    {
        _entranceCoordinates.Clear();

        if (_tilemapBase == null)
        {
            return;
        }

        // 전체 영역 계산 (기본 + 입구 + 모든 구역)
        _totalBounds = _tilemapBase.cellBounds;

        if (_tilemapEntrance != null)
        {
            ExpandBounds(ref _totalBounds, _tilemapEntrance.cellBounds);
        }

        foreach (var zoneTilemap in _zoneTilemaps)
        {
            if (zoneTilemap != null)
            {
                ExpandBounds(ref _totalBounds, zoneTilemap.cellBounds);
            }
        }

        int width = _totalBounds.size.x;
        int height = _totalBounds.size.y;
        _walkableMap = new bool[width, height];

        for (int x = _totalBounds.xMin; x < _totalBounds.xMax; x++)
        {
            for (int y = _totalBounds.yMin; y < _totalBounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                // 이동 가능 여부 판정: 기본/입구/구역 타일맵 중 하나라도 타일이 있으면 이동 가능
                bool isWalkable = _tilemapBase.HasTile(pos);

                if (!isWalkable && _tilemapEntrance != null)
                    isWalkable = _tilemapEntrance.HasTile(pos);

                if (!isWalkable)
                {
                    foreach (var zoneTilemap in _zoneTilemaps)
                    {
                        if (zoneTilemap != null && zoneTilemap.HasTile(pos))
                        {
                            isWalkable = true;
                            break;
                        }
                    }
                }

                int arrayX = x - _totalBounds.xMin;
                int arrayY = y - _totalBounds.yMin;
                _walkableMap[arrayX, arrayY] = isWalkable;

                // 입구 좌표 수집
                if (_tilemapEntrance != null && _tilemapEntrance.HasTile(pos))
                {
                    _entranceCoordinates.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    /// <summary>
    /// 구역별 셀 좌표를 캐싱합니다.
    /// </summary>
    private void InitializeZones()
    {
        _zoneCells.Clear();
        _zoneOccupancy.Clear();

        for (int zoneIndex = 0; zoneIndex < _zoneTilemaps.Count; zoneIndex++)
        {
            Tilemap zoneTilemap = _zoneTilemaps[zoneIndex];
            if (zoneTilemap == null) continue;

            int zoneId = zoneIndex + 1; // 1-based ID
            List<Vector2Int> cells = new List<Vector2Int>();

            BoundsInt bounds = zoneTilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if (zoneTilemap.HasTile(pos))
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            _zoneCells[zoneId] = cells;
            _zoneOccupancy[zoneId] = null; // 초기에는 비어있음

        }
    }

    private void ExpandBounds(ref BoundsInt target, BoundsInt source)
    {
        target.xMin = Mathf.Min(target.xMin, source.xMin);
        target.xMax = Mathf.Max(target.xMax, source.xMax);
        target.yMin = Mathf.Min(target.yMin, source.yMin);
        target.yMax = Mathf.Max(target.yMax, source.yMax);
    }
    #endregion

    // ═══════════════════════════════════════════════════════════════
    // 구역 관리 시스템
    // ═══════════════════════════════════════════════════════════════

    #region Zone Management

    /// <summary>
    /// 사용 가능한 (점유되지 않은) 구역 ID 목록을 반환합니다.
    /// </summary>
    public List<int> GetAvailableZones()
    {
        List<int> available = new List<int>();

        foreach (var kvp in _zoneOccupancy)
        {
            if (string.IsNullOrEmpty(kvp.Value))
            {
                available.Add(kvp.Key);
            }
        }

        return available;
    }

    /// <summary>
    /// 특정 제자가 점유 중인 구역을 제외한 사용 가능한 구역을 반환합니다.
    /// </summary>
    public List<int> GetAvailableZonesExcept(string discipleId)
    {
        List<int> available = new List<int>();

        foreach (var kvp in _zoneOccupancy)
        {
            // 비어있거나 해당 제자가 점유 중인 구역
            if (string.IsNullOrEmpty(kvp.Value) || kvp.Value == discipleId)
            {
                available.Add(kvp.Key);
            }
        }

        return available;
    }

    /// <summary>
    /// 구역을 점유합니다.
    /// </summary>
    /// <param name="zoneId">구역 ID (1-based)</param>
    /// <param name="discipleId">제자 ID</param>
    /// <returns>성공 여부</returns>
    public bool OccupyZone(int zoneId, string discipleId)
    {
        if (!_zoneOccupancy.ContainsKey(zoneId))
        {
            return false;
        }

        // 이미 다른 제자가 점유 중인지 확인
        if (!string.IsNullOrEmpty(_zoneOccupancy[zoneId]) && _zoneOccupancy[zoneId] != discipleId)
        {
            return false;
        }

        // 기존에 점유하던 구역 해제
        ReleaseZone(discipleId);

        // 새 구역 점유
        _zoneOccupancy[zoneId] = discipleId;
        return true;
    }

    /// <summary>
    /// 제자가 점유한 구역을 해제합니다.
    /// </summary>
    public void ReleaseZone(string discipleId)
    {
        foreach (var zoneId in _zoneOccupancy.Keys.ToList())
        {
            if (_zoneOccupancy[zoneId] == discipleId)
            {
                _zoneOccupancy[zoneId] = null;
            }
        }
    }

    /// <summary>
    /// 모든 구역 점유 상태를 초기화합니다.
    /// </summary>
    public void ClearAllZoneOccupancy()
    {
        foreach (var zoneId in _zoneOccupancy.Keys.ToList())
        {
            _zoneOccupancy[zoneId] = null;
        }
    }

    /// <summary>
    /// 특정 구역의 랜덤 셀 좌표를 반환합니다.
    /// </summary>
    /// <param name="zoneId">구역 ID (1-based)</param>
    /// <param name="excludePos">제외할 위치 (현재 위치)</param>
    public Vector2Int GetRandomCellInZone(int zoneId, Vector2Int? excludePos = null)
    {
        if (!_zoneCells.ContainsKey(zoneId) || _zoneCells[zoneId].Count == 0)
        {
            return Vector2Int.zero;
        }

        List<Vector2Int> cells = _zoneCells[zoneId];

        // 제외할 위치가 있으면 필터링
        if (excludePos.HasValue && cells.Count > 1)
        {
            List<Vector2Int> filtered = cells.Where(c => c != excludePos.Value).ToList();
            if (filtered.Count > 0)
            {
                return filtered[UnityEngine.Random.Range(0, filtered.Count)];
            }
        }

        return cells[UnityEngine.Random.Range(0, cells.Count)];
    }

    /// <summary>
    /// 특정 구역의 모든 셀 좌표를 반환합니다.
    /// </summary>
    public List<Vector2Int> GetZoneCells(int zoneId)
    {
        if (_zoneCells.TryGetValue(zoneId, out var cells))
        {
            return new List<Vector2Int>(cells);
        }
        return new List<Vector2Int>();
    }

    /// <summary>
    /// 랜덤으로 사용 가능한 구역 ID를 반환합니다.
    /// </summary>
    /// <param name="excludeDiscipleId">제외할 제자 ID (본인이 이미 점유 중인 구역은 포함)</param>
    public int GetRandomAvailableZone(string excludeDiscipleId = null)
    {
        List<int> available;

        if (!string.IsNullOrEmpty(excludeDiscipleId))
        {
            available = GetAvailableZonesExcept(excludeDiscipleId);
        }
        else
        {
            available = GetAvailableZones();
        }

        if (available.Count == 0)
        {
            // 모든 구역이 점유됨 - 아무 구역이나 반환
            if (_zoneCells.Count > 0)
            {
                return _zoneCells.Keys.First();
            }
            return -1;
        }

        return available[UnityEngine.Random.Range(0, available.Count)];
    }

    /// <summary>
    /// 특정 제자가 현재 점유 중인 구역 ID를 반환합니다.
    /// </summary>
    public int GetOccupiedZone(string discipleId)
    {
        foreach (var kvp in _zoneOccupancy)
        {
            if (kvp.Value == discipleId)
            {
                return kvp.Key;
            }
        }
        return -1; // 점유 중인 구역 없음
    }

    /// <summary>
    /// 특정 위치가 속한 구역 ID를 반환합니다.
    /// </summary>
    public int GetZoneAtPosition(Vector2Int pos)
    {
        foreach (var kvp in _zoneCells)
        {
            if (kvp.Value.Contains(pos))
            {
                return kvp.Key;
            }
        }
        return -1; // 어떤 구역에도 속하지 않음
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // 제자 스폰 위치 (구역 기반)
    // ═══════════════════════════════════════════════════════════════

    #region Disciple Spawn Position

    /// <summary>
    /// 제자 스폰용 랜덤 위치를 반환합니다.
    /// 구역이 있으면 랜덤 구역의 랜덤 셀을, 없으면 기존 GetRandomWalkablePosition()을 사용합니다.
    /// </summary>
    /// <returns>스폰 위치</returns>
    public Vector2Int GetRandomDiscipleSpawnPosition(string discipleId = "")
    {
        // 구역이 있으면 구역 기반으로 스폰
        if (HasZones && _zoneCells.Count > 0)
        {
            // 특정 제자가 점유 중인 구역을 포함하여 사용 가능한 구역 목록 가져오기
            List<int> availableZones;
            if (discipleId != "")
            {
                availableZones = GetAvailableZonesExcept(discipleId);
            }
            else
            {
                availableZones = GetAvailableZones();
            }

            // 사용 가능한 구역이 있을 경우 그 중 랜덤 선택
            if (availableZones.Count > 0)
            {
                int randomZoneId = availableZones[UnityEngine.Random.Range(0, availableZones.Count)];
                Vector2Int pos = GetRandomCellInZone(randomZoneId);

                if (pos != Vector2Int.zero)
                {
                    return pos;
                }
            }
            else
            {
                // 모든 구역이 점유된 경우의 예비 처리 (전체 구역 중 랜덤 선택)
                List<int> allZoneIds = _zoneCells.Keys.ToList();
                int fallbackZoneId = allZoneIds[UnityEngine.Random.Range(0, allZoneIds.Count)];
                Vector2Int pos = GetRandomCellInZone(fallbackZoneId);

                if (pos != Vector2Int.zero)
                {
                    return pos;
                }
            }
        }

        // 구역이 없거나 실패 시 기존 방식 사용
        return GetRandomWalkablePosition();
    }

    /// <summary>
    /// 지정된 구역에서 제자 스폰 위치를 반환합니다.
    /// </summary>
    /// <param name="zoneId">구역 ID (1-based). -1이면 랜덤 구역</param>
    /// <returns>스폰 위치</returns>
    public Vector2Int GetDiscipleSpawnPositionInZone(int zoneId = -1)
    {
        // 구역이 없으면 기존 방식
        if (!HasZones || _zoneCells.Count == 0)
        {
            return GetRandomWalkablePosition();
        }

        // 구역 ID가 -1이면 랜덤 선택
        if (zoneId <= 0)
        {
            List<int> allZoneIds = _zoneCells.Keys.ToList();
            zoneId = allZoneIds[UnityEngine.Random.Range(0, allZoneIds.Count)];
        }

        // 해당 구역에서 랜덤 셀 반환
        if (_zoneCells.ContainsKey(zoneId) && _zoneCells[zoneId].Count > 0)
        {
            Vector2Int pos = GetRandomCellInZone(zoneId);
            return pos;
        }

        // 실패 시 기존 방식
        return GetRandomWalkablePosition();
    }

    /// <summary>
    /// 모든 구역의 셀을 합쳐서 랜덤 위치를 반환합니다.
    /// </summary>
    public Vector2Int GetRandomPositionFromAllZones()
    {
        if (!HasZones || _zoneCells.Count == 0)
        {
            return GetRandomWalkablePosition();
        }

        // 모든 구역의 셀을 합침
        List<Vector2Int> allCells = new List<Vector2Int>();
        foreach (var cells in _zoneCells.Values)
        {
            allCells.AddRange(cells);
        }

        if (allCells.Count > 0)
        {
            return allCells[UnityEngine.Random.Range(0, allCells.Count)];
        }

        return GetRandomWalkablePosition();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // 입구 / 스폰 헬퍼
    // ═══════════════════════════════════════════════════════════════

    #region Entrance / Spawn Helpers
    public Vector2Int GetRandomSpawnPosition()
    {
        if (_entranceCoordinates.Count == 0) return Vector2Int.zero;
        return _entranceCoordinates[UnityEngine.Random.Range(0, _entranceCoordinates.Count)];
    }

    public Vector2Int GetRandomExitPosition()
    {
        if (_entranceCoordinates.Count == 0) return Vector2Int.zero;
        return _entranceCoordinates[UnityEngine.Random.Range(0, _entranceCoordinates.Count)];
    }
    #endregion

    // ═══════════════════════════════════════════════════════════════
    // 이동 / 걷기 가능 여부
    // ═══════════════════════════════════════════════════════════════

    #region Movement / Walkability
    public bool CanMove(Vector2Int pos)
    {
        int arrayX = pos.x - _totalBounds.xMin;
        int arrayY = pos.y - _totalBounds.yMin;

        if (arrayX < 0 || arrayX >= _walkableMap.GetLength(0) ||
            arrayY < 0 || arrayY >= _walkableMap.GetLength(1))
            return false;

        return _walkableMap[arrayX, arrayY];
    }

    private bool IsWalkable(int x, int y)
    {
        if (_walkableMap == null) return false;
        int arrayX = x - _totalBounds.xMin;
        int arrayY = y - _totalBounds.yMin;

        if (arrayX < 0 || arrayX >= _walkableMap.GetLength(0) ||
            arrayY < 0 || arrayY >= _walkableMap.GetLength(1))
            return false;

        return _walkableMap[arrayX, arrayY];
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        if (_tilemapBase == null) return Vector3.zero;
        return _tilemapBase.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
    }

    public Vector2Int WorldToCell(Vector3 pos)
    {
        if (_tilemapBase == null) return Vector2Int.zero;
        Vector3Int c = _tilemapBase.WorldToCell(pos);
        return new Vector2Int(c.x, c.y);
    }

    public List<Vector2Int> GetWalkableCells()
    {
        List<Vector2Int> walkableCells = new List<Vector2Int>();
        BoundsInt bounds = _totalBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var cellPos = new Vector2Int(x, y);
                if (CanMove(cellPos))
                {
                    walkableCells.Add(cellPos);
                }
            }
        }
        return walkableCells;
    }

    public Vector2Int GetRandomWalkablePosition()
    {
        if (_walkableMap == null) return Vector2Int.zero;

        List<Vector2Int> candidates = new List<Vector2Int>();
        BoundsInt bounds = _totalBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                if (IsWalkable(x, y))
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    candidates.Add(pos);
                }
            }
        }

        if (candidates.Count > 0)
        {
            int rnd = UnityEngine.Random.Range(0, candidates.Count);
            return candidates[rnd];
        }

        return Vector2Int.zero;
    }
    #endregion

    // ═══════════════════════════════════════════════════════════════
    // 경로 탐색 (A*)
    // ═══════════════════════════════════════════════════════════════

    #region Pathfinding (A*)
    private struct Node : IComparable<Node>
    {
        public Vector2Int position;
        public int f;
        public Node(Vector2Int pos, int f) { this.position = pos; this.f = f; }
        public int CompareTo(Node other) => f.CompareTo(other.f);
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        if (!IsWalkable(start.x, start.y) || !IsWalkable(goal.x, goal.y))
        {
            if (!IsWalkable(start.x, start.y))
                Debug.LogError($"[MapManager] 시작 위치({start})가 맵 바깥이거나 이동 불가 지역입니다.");
            if (!IsWalkable(goal.x, goal.y))
                Debug.LogError($"[MapManager] 목표 위치({goal})가 맵 바깥이거나 이동 불가 지역입니다.");
            return path;
        }

        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> G = new Dictionary<Vector2Int, int>();
        Dictionary<Vector2Int, int> F = new Dictionary<Vector2Int, int>();

        Rookiss.PriorityQueue<Node> openSet = new Rookiss.PriorityQueue<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        openSet.Enqueue(new Node(start, Heuristic(start, goal)));
        G[start] = 0;
        F[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.Dequeue();
            Vector2Int current = currentNode.position;

            if (current == goal)
            {
                while (parent.ContainsKey(current))
                {
                    path.Insert(0, current);
                    current = parent[current];
                }
                return path;
            }

            closedSet.Add(current);

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;
                if (!CanMove(neighbor) || closedSet.Contains(neighbor)) continue;

                int tentativeGScore = G[current] + 1;
                if (!G.ContainsKey(neighbor) || tentativeGScore < G[neighbor])
                {
                    parent[neighbor] = current;
                    G[neighbor] = tentativeGScore;
                    F[neighbor] = G[neighbor] + Heuristic(neighbor, goal);
                    openSet.Enqueue(new Node(neighbor, F[neighbor]));
                }
            }
        }

        // 목표 도달 실패 시 가장 가까운 지점까지의 경로 반환
        if (path.Count == 0)
        {
            Vector2Int closest = start;
            int minDist = Heuristic(start, goal);
            foreach (var pos in parent.Keys)
            {
                int dist = Heuristic(pos, goal);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = pos;
                }
            }
            while (parent.ContainsKey(closest))
            {
                path.Insert(0, closest);
                closest = parent[closest];
            }
        }

        return path;
    }

    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
    }
    #endregion
}