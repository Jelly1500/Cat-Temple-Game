using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visitor : Cat
{
    // 스탯 데이터는 유지
    public int StatComplaint;
    public int StatComfort;
    public int StatAnswer;
    public int Generation = 20;

    // AI 관련 변수(Coroutine, RetryCount 등) 모두 삭제

    public override void Init()
    {
        base.Init();
        _state = ECatState.Idle;
    }

    // [핵심] InteractionManager가 호출하는 이동 명령
    // 경로 계산은 매니저가 해서 path를 넘겨줌
    public IEnumerator CoMoveAlongPath(List<Vector2Int> path)
    {
        _state = ECatState.Move;
        UpdateAnimation();

        foreach (Vector2Int nextCell in path)
        {
            LookAt(nextCell); // 방향 전환

            // 물리적 이동 (Lerp)
            Vector3 startPos = transform.position;
            Vector3 endPos = MapManager.Instance.CellToWorld(nextCell);
            float duration = 0.3f; // 이동 속도
            float elapsed = 0f;

            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos;
            _CellPosition = nextCell; // 논리 좌표 갱신
        }

        _state = ECatState.Idle;
        UpdateAnimation();
    }

    // InteractionManager에 의해 관리되므로 자율 행동(StartCoroutine) 없음
}