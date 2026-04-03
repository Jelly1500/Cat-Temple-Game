using UnityEngine;

[CreateAssetMenu(fileName = "VisitorDataSheet", menuName = "Data/VisitorDataSheet")]
public class VisitorDataSheet : ScriptableObject
{
    [Header("기본 정보")]
    public int id;                  // 템플릿 ID (예: 1001)
    public string nameKey;          // 방문객 이름 (다국어 키 또는 기본 이름)
    public string prefabName;       // 로드할 프리팹 이름 (Resources 경로)

    [Header("세대 설정")]
    [Tooltip("10, 20, 30, 40, 50, 60, 70 중 하나 입력")]
    public int generation;          // 편지 매핑을 위한 핵심 키 (10단위 정수)

    [Header("등장 조건")]
    public int baseRenownReq;       // 최소 인지도 조건

}