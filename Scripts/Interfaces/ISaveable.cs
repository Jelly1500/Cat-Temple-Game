public interface ISaveable
{
    /// <summary>
    /// 현재 상태를 GameData에 저장
    /// </summary>
    void SaveTo(GameData data);

    /// <summary>
    /// GameData로부터 상태 복원
    /// </summary>
    void LoadFrom(GameData data);

    /// <summary>
    /// 초기 상태로 리셋
    /// </summary>
    void ResetToDefault();
}