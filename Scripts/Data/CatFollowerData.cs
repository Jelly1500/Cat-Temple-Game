using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CatFollowerData", menuName = "Scriptable Objects/CatFollowerData")]
public class CatFollowerData : ScriptableObject
{
    public string catName = "Null";
    public string description = "Null";

    // 주요 스탯
    public int patience = 0;    // 인내심
    public int empathy = 0;     // 공감력
    public int wisdom = 0;      // 지혜
    public int enlightenment = 0; // 깨달음

    // 훈련 기록 (리스트)
    public List<string> trainingHistory = new List<string>();

    // 훈련 로직 (예시)
    public void AddStats(int p, int e, int w)
    {
        patience += p; empathy += e; wisdom += w;
    }
}
