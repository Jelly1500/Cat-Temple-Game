using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : Singleton<ObjectManager>
{
    #region Roots
    private Transform _playerRoot;
    public Transform PlayerRoot => Utils.GetRootTransform(ref _playerRoot, "@Players");

    private Transform _monsterRoot;
    public Transform MonsterRoot => Utils.GetRootTransform(ref _monsterRoot, "@Monsters");

    private Transform _npcRoot;
    public Transform NpcRoot => Utils.GetRootTransform(ref _npcRoot, "@Npcs");
    #endregion

    // ObjectManager가 관리하는 전체 오브젝트 (Visitor 포함)
    private HashSet<ObjectBase> _objects = new HashSet<ObjectBase>();


    public void Init()
    {
        _objects.Clear();
    }
    /// <summary>
    /// [핵심] 저장된 데이터 혹은 새로 생성된 데이터를 기반으로 제자 오브젝트를 씬에 소환
    /// </summary>
    public Disciple SpawnDisciple(DiscipleData data)
    {
        if (data == null) return null;

        // 1. 템플릿 정보 가져오기
        var template = DataManager.Instance.GetDiscipleTemplate(data.templateId);
        string prefabName = (template != null && !string.IsNullOrEmpty(template.prefabName))
                            ? template.prefabName
                            : "Disciple";

        // 2. 프리팹 생성
        GameObject go = ResourceManager.Instance.Instantiate(prefabName, PlayerRoot);
        if (go == null)
        {
            Debug.LogError($"[ObjectManager] 프리팹 '{prefabName}' 로드 실패");
            return null;
        }

        // 3. 컴포넌트 설정
        Disciple disciple = go.GetOrAddComponent<Disciple>();
        disciple.Init();

        // 4. 데이터 연결 및 위치 동기화
        disciple.SetInfo(data);

        var skeletonAnim = go.GetComponent<Spine.Unity.SkeletonAnimation>();
        skeletonAnim.GetComponent<MeshRenderer>().sortingOrder = 0;

        // 데이터에 저장된 좌표가 있다면 강제 이동 (로드 시)
        // (단, 새로 생성된 데이터라면 (0,0) 혹은 지정된 스폰 위치일 것임)
        Vector2Int gridPos = new Vector2Int(data.x, data.y);

        // 저장된 위치가 이동 불가능한 지역(맵 밖 등)이라면 랜덤 위치로 재조정
        if (!MapManager.Instance.CanMove(gridPos))
        {
            Debug.LogWarning($"[ObjectManager] 제자({data.name})의 저장된 위치({gridPos})가 유효하지 않아 재배치합니다.");
            gridPos = MapManager.Instance.GetRandomDiscipleSpawnPosition();

            // 데이터도 갱신
            data.x = gridPos.x;
            data.y = gridPos.y;
        }

        disciple.transform.position = MapManager.Instance.CellToWorld(gridPos);

        // 5. 관리 목록 등록
        _objects.Add(disciple);

        // [중요] DiscipleManager에 "이 데이터는 이 오브젝트가 담당한다"고 등록
        DiscipleManager.Instance.RegisterObject(data.id, disciple);

        return disciple;
    }

    /// <summary>
    /// [신규] DiscipleManager에 있는 모든 제자 데이터를 기반으로 오브젝트 일괄 소환
    /// (SaveManager.OnLoadComplete 에서 호출)
    /// </summary>
    public void SpawnAllDisciples()
    {
        // 1. 기존 제자 오브젝트가 있다면 정리 (중복 방지)
        // (Visitor는 놔두고 Disciple만 정리하거나, 전체 정리 후 재생성 정책 결정 필요)
        // 여기서는 안전하게 DiscipleManager에 등록된 오브젝트들을 확인해 제거 후 재생성

        // (간단히 구현: DiscipleManager의 데이터를 믿고 순회)
        var discipleList = DiscipleManager.Instance.Disciples;


        foreach (var data in discipleList)
        {
            // 이미 소환되어 있는지 체크 (안전장치)
            if (DiscipleManager.Instance.GetObject(data.id) != null)
                continue;

            SpawnDisciple(data);
        }
    }

    // [삭제됨] CreateNewDiscipleData -> DiscipleManager로 이동

    public Visitor SpawnVisitor(Vector2Int? startPos = null)
    {
        var sheet = DataManager.Instance.GetRandomVisitorSheet(GameDataManager.Instance.Renown);
        string prefabName = (sheet != null && !string.IsNullOrEmpty(sheet.prefabName)) ? sheet.prefabName : "Visitor";

        GameObject go = ResourceManager.Instance.Instantiate(prefabName, NpcRoot);
        if (go == null) go = ResourceManager.Instance.Instantiate("Visitor", NpcRoot);

        Visitor visitor = go.GetOrAddComponent<Visitor>();
        visitor.Init();
        var skeletonAnim = go.GetComponent<Spine.Unity.SkeletonAnimation>();
        skeletonAnim.GetComponent<MeshRenderer>().sortingOrder = 0;
        _objects.Add(visitor);

        visitor.Generation = (sheet != null) ? sheet.generation : 20;

        int currentRenown = GameDataManager.Instance.Renown;
        int baseStat = 10;
        visitor.StatComplaint = baseStat + currentRenown;
        visitor.StatComfort = baseStat + currentRenown;
        visitor.StatAnswer = baseStat + currentRenown;

        Vector2Int spawnPos;

        if (startPos.HasValue)
        {
            spawnPos = startPos.Value;
        }
        else
        {
            spawnPos = MapManager.Instance.GetRandomSpawnPosition();
        }

        // 혹시라도 (0,0) 등 잘못된 위치가 반환되었는지 체크 (Entrance가 하나도 없을 때를 대비)
        if (!MapManager.Instance.CanMove(spawnPos))
        {
            Debug.LogWarning("[ObjectManager] 방문객 스폰 위치가 유효하지 않아 재검색합니다.");
            spawnPos = MapManager.Instance.GetRandomWalkablePosition();
        }
        visitor.transform.position = MapManager.Instance.CellToWorld(spawnPos);

        InteractionManager.Instance.OnVisitorSpawned(visitor);

        return visitor;
    }

    public void Despawn(ObjectBase obj)
    {
        if (obj == null) return;

        _objects.Remove(obj);

        // 제자인 경우 DiscipleManager 목록에서도 해제
        if (obj is Disciple disciple && disciple.Data != null)
        {
            DiscipleManager.Instance.UnregisterObject(disciple.Data.id);
        }

        if (obj.Pooling)
        {
            obj.Init(); // 초기화
            PoolManager.Instance.Push(obj.gameObject);
        }
        else
        {
            ResourceManager.Instance.Destroy(obj.gameObject);
        }
    }
}