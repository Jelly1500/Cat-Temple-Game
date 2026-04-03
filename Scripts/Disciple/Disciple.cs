using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 제자 고양이 클래스
/// [신규] 배회(Roaming) 상태 및 구역 기반 이동 시스템 추가
/// [신규] 터치 시 DiscipleTouched 이벤트 발생 (튜토리얼용)
/// </summary>
public class Disciple : Cat
{
    // ═══════════════════════════════════════════════════════════════
    // 배회 상태 열거형
    // ═══════════════════════════════════════════════════════════════

    public enum ERoamingState
    {
        Idle,           // 대기 (배회하지 않음)
        MovingToZone,   // 구역으로 이동 중
        Roaming,        // 구역 내 배회 중
        Busy            // 작업 중 (대화 등) - 배회 불가
    }

    // ═══════════════════════════════════════════════════════════════
    // 데이터 및 상태
    // ═══════════════════════════════════════════════════════════════

    public DiscipleData Data { get; private set; }

    public string ID => Data?.id;
    public bool IsTalking { get; set; } = false;

    // [신규] 배회 관련 변수
    private ERoamingState _roamingState = ERoamingState.Idle;
    public ERoamingState RoamingState => _roamingState;

    private int _assignedZoneId = -1;   // 현재 할당된 구역 ID
    public int AssignedZoneId => _assignedZoneId;

    private Coroutine _roamingCoroutine;
    private bool _isMoving = false;     // 이동 중 여부

    // 배회 설정
    private const float ROAMING_INTERVAL = 3f;  // 배회 간격 (초)

    // ═══════════════════════════════════════════════════════════════
    // 초기화
    // ═══════════════════════════════════════════════════════════════

    public override void Init()
    {
        base.Init();
        IsTalking = false;
        _roamingState = ERoamingState.Idle;

        if (MapManager.Instance != null)
            _CellPosition = MapManager.Instance.WorldToCell(transform.position);
    }

    public void SetInfo(DiscipleData data)
    {
        this.Data = data;

        // 데이터에 저장된 좌표로 이동 및 맵 좌표 동기화
        _CellPosition = new Vector2Int(data.x, data.y);

        if (MapManager.Instance != null)
            transform.position = MapManager.Instance.CellToWorld(_CellPosition);
    }

    public void UpdateDataPosition()
    {
        if (Data == null) return;

        Data.x = _CellPosition.x;
        Data.y = _CellPosition.y;
    }

    // ═══════════════════════════════════════════════════════════════
    // 구역 할당 및 이동
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 구역을 할당하고 해당 구역으로 이동을 시작합니다.
    /// </summary>
    /// <param name="zoneId">할당할 구역 ID</param>
    public void AssignZone(int zoneId)
    {
        if (IsTalking || _roamingState == ERoamingState.Busy)
        {
            return;
        }

        // 기존 구역 해제
        if (_assignedZoneId > 0 && MapManager.Instance != null)
        {
            MapManager.Instance.ReleaseZone(ID);
        }

        // 새 구역 점유
        if (MapManager.Instance != null && !MapManager.Instance.OccupyZone(zoneId, ID))
        {
            return;
        }

        _assignedZoneId = zoneId;

        // 구역으로 이동 시작
        StartMoveToZone();
    }

    /// <summary>
    /// 할당된 구역으로 이동을 시작합니다.
    /// </summary>
    private void StartMoveToZone()
    {
        if (_assignedZoneId <= 0) return;

        StopRoaming();

        _roamingState = ERoamingState.MovingToZone;
        _roamingCoroutine = StartCoroutine(CoMoveToZone());
    }

    /// <summary>
    /// 구역으로 이동하는 코루틴
    /// </summary>
    private IEnumerator CoMoveToZone()
    {
        // 구역 내 랜덤 위치 선택
        Vector2Int targetPos = MapManager.Instance.GetRandomCellInZone(_assignedZoneId);

        if (targetPos == Vector2Int.zero)
        {
            _roamingState = ERoamingState.Idle;
            yield break;
        }

        // 경로 계산
        List<Vector2Int> path = MapManager.Instance.FindPath(_CellPosition, targetPos);

        if (path == null || path.Count == 0)
        {
            // 경로를 찾을 수 없으면 텔레포트
            transform.position = MapManager.Instance.CellToWorld(targetPos);
            _CellPosition = targetPos;
        }
        else
        {
            // 경로 따라 이동
            yield return StartCoroutine(CoMoveAlongPath(path));
        }

        // 이동 완료 - 배회 상태로 전환
        _roamingState = ERoamingState.Roaming;

        // 배회 시작
        _roamingCoroutine = StartCoroutine(CoRoamInZone());
    }


    private IEnumerator CoRoamInZone()
    {
        while (_roamingState == ERoamingState.Roaming && !IsTalking)
        {
            // 배회 간격 대기
            yield return new WaitForSeconds(ROAMING_INTERVAL);

            // 대화 중이면 중단
            if (IsTalking || _roamingState != ERoamingState.Roaming)
            {
                break;
            }

            // 구역 내 랜덤 위치로 이동
            Vector2Int nextPos = MapManager.Instance.GetRandomCellInZone(_assignedZoneId, _CellPosition);

            if (nextPos != Vector2Int.zero && nextPos != _CellPosition)
            {
                List<Vector2Int> path = MapManager.Instance.FindPath(_CellPosition, nextPos);

                if (path != null && path.Count > 0)
                {
                    yield return StartCoroutine(CoMoveAlongPath(path));
                }
            }
        }

        // 배회 종료
        if (_roamingState == ERoamingState.Roaming)
        {
            _roamingState = ERoamingState.Idle;
        }
    }

    /// <summary>
    /// 배회를 중지합니다.
    /// </summary>
    public void StopRoaming()
    {
        if (_roamingCoroutine != null)
        {
            StopCoroutine(_roamingCoroutine);
            _roamingCoroutine = null;
        }

        _isMoving = false;

        if (_roamingState == ERoamingState.Roaming || _roamingState == ERoamingState.MovingToZone)
        {
            _roamingState = ERoamingState.Idle;
        }
    }

    /// <summary>
    /// 경로를 따라 이동하는 코루틴
    /// </summary>
    public IEnumerator CoMoveAlongPath(List<Vector2Int> path)
    {
        _isMoving = true;

        foreach (Vector2Int nextCell in path)
        {
            // 중단 조건 체크
            if (IsTalking || _roamingState == ERoamingState.Busy)
            {
                _isMoving = false;
                State = ECatState.Idle;
                yield break;
            }

            LookAt(nextCell);
            State = ECatState.Move;
            UpdateAnimation();

            Vector3 startPos = transform.position;
            Vector3 endPos = MapManager.Instance.CellToWorld(nextCell);
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (IsTalking || _roamingState == ERoamingState.Busy)
                {
                    transform.position = endPos;
                    _CellPosition = nextCell;
                    _isMoving = false;
                    State = ECatState.Idle;
                    yield break;
                }

                transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos;
            _CellPosition = nextCell;
        }

        _isMoving = false;
        State = ECatState.Idle;
    }

    public Vector2Int GetValidInteractionPos()
    {
        Vector2Int[] directions = { Vector2Int.down, Vector2Int.right, Vector2Int.left, Vector2Int.up };

        foreach (var dir in directions)
        {
            Vector2Int checkPos = _CellPosition + dir;
            if (MapManager.Instance.CanMove(checkPos))
            {
                return checkPos;
            }
        }
        return _CellPosition;
    }

    /// <summary>
    /// 대화 예약 (InteractionManager가 제자를 찾은 직후 호출)
    /// IsTalking=true + StopRoaming()으로 이동을 즉시 멈춰 위치를 고정한다.
    /// 방문객 이동 중 제자가 계속 배회하여 목적지가 어긋나는 버그 방지.
    /// 실제 대화 애니메이션/방향 전환은 방문객 도착 후 BeginTalk()에서 수행.
    /// </summary>
    public void ReserveForTalk()
    {
        IsTalking = true;
        StopRoaming();                          // 배회 코루틴 즉시 중단
        _roamingState = ERoamingState.Busy;     // 추가 배회 할당 차단

        // 현재 위치를 정확히 타일 중앙으로 스냅
        // (Lerp 도중 StopRoaming되면 타일 중간에 멈출 수 있으므로)
        if (MapManager.Instance != null)
        {
            _CellPosition = MapManager.Instance.WorldToCell(transform.position);
            transform.position = MapManager.Instance.CellToWorld(_CellPosition);
        }

        State = ECatState.Idle;
    }

    /// <summary>
    /// 대화 시작 (방문객 도착 후 InteractionManager가 호출)
    /// ReserveForTalk() 이후에 호출되어 방향 전환 및 Talk 상태 적용.
    /// </summary>
    public void BeginTalk(Vector2Int targetLookPos)
    {
        // StopRoaming + Busy는 ReserveForTalk()에서 이미 처리됨.
        // IsTalking=true인 상태로 재진입하는 경우도 안전하게 처리.
        StopRoaming();
        _roamingState = ERoamingState.Busy;

        IsTalking = true;
        LookAt(targetLookPos);
        State = ECatState.Talk;
    }

    /// <summary>
    /// 대화 종료
    /// </summary>
    public void EndTalk()
    {
        IsTalking = false;
        State = ECatState.Idle;
        _roamingState = ERoamingState.Idle;

        // 대화 종료 후 1초 대기 → 1회 배회 → 이후 기존 로직
        if (_assignedZoneId > 0)
        {
            _roamingCoroutine = StartCoroutine(CoPostTalkRoaming());
        }
    }

    /// <summary>
    /// 대화 종료 후 1초 대기 → 1회 배회 → 이후 일반 배회 루프
    /// </summary>
    private IEnumerator CoPostTalkRoaming()
    {
        // 1초 대기
        yield return new WaitForSeconds(1f);

        // 대기 중 다시 대화에 들어갔으면 중단
        if (IsTalking || _roamingState == ERoamingState.Busy)
        {
            yield break;
        }

        // 1회 배회 실행
        _roamingState = ERoamingState.Roaming;

        Vector2Int nextPos = MapManager.Instance.GetRandomCellInZone(_assignedZoneId, _CellPosition);

        if (nextPos != Vector2Int.zero && nextPos != _CellPosition)
        {
            List<Vector2Int> path = MapManager.Instance.FindPath(_CellPosition, nextPos);

            if (path != null && path.Count > 0)
            {
                yield return StartCoroutine(CoMoveAlongPath(path));
            }
        }

        // 1회 배회 완료 후, 대화 중이 아니면 일반 배회 루프 시작
        if (!IsTalking && _roamingState == ERoamingState.Roaming)
        {
            _roamingCoroutine = StartCoroutine(CoRoamInZone());
        }
    }

    protected override void OnMouseDown()
    {
        base.OnMouseDown();

        // 1. UI(버튼 등)를 누르고 있는 상태라면 고양이 선택 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 2. 대화 중이면 메뉴 열지 않음
        if (IsTalking) return;

        // ═══════════════════════════════════════════════════════════════
        // [신규] 제자 터치 이벤트 발생 (튜토리얼용)
        // ═══════════════════════════════════════════════════════════════
        EventManager.Instance.TriggerEvent(Define.EEventType.DiscipleTouched);

        // 3. [수정됨] 팝업 생성이 아니라, 이미 존재하는 MainGame UI를 찾아 요청합니다.
        // UIManager가 현재 씬의 UI(UI_MainGame)를 알고 있습니다.
        UI_MainGame mainGameUI = UIManager.Instance.SceneUI as UI_MainGame;

        if (mainGameUI != null)
        {
            // UI_MainGame에 이미 구현되어 있는 ShowCatMenu 함수 호출
            mainGameUI.ShowCatMenu(this.transform);

            // [선택 사항] 카메라가 고양이를 따라가게 하려면
            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetFollowTarget(this.transform);
            }
        }
        else
        {
            Debug.LogWarning("[Disciple] UI_MainGame을 찾을 수 없습니다. 씬 구성을 확인하세요.");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 정리
    // ═══════════════════════════════════════════════════════════════

    private void OnDestroy()
    {
        StopRoaming();

        // 구역 점유 해제
        if (_assignedZoneId > 0 && MapManager.Instance != null)
        {
            MapManager.Instance.ReleaseZone(ID);
        }
    }
}