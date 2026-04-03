using UnityEngine;

[CreateAssetMenu(fileName = "AdsConfig", menuName = "Config/AdsConfig")]
public class AdsConfig : ScriptableObject
{
    [Header("Android Settings")]
    [SerializeField]
    private string android_app_id;
    [SerializeField]
    private string android_interstitial_id;
    [SerializeField]
    private string android_rewarded_id;
    [SerializeField]
    private string android_banner_id; // [추가] 배너 전용 ID

    [Header("iOS Settings")]
    [SerializeField]
    private string ios_app_id;
    [SerializeField]
    private string ios_interstitial_id;
    [SerializeField]
    private string ios_rewarded_id;
    [SerializeField]
    private string ios_banner_id; // [추가] 배너 전용 ID

    #region IDs
    public string GetAppKey()
    {
#if UNITY_ANDROID
        return android_app_id;
#elif UNITY_IPHONE
        return ios_app_id;
#else
        return "unexpected_platform";
#endif
    }

    public string GetInterstitialAdUnitId()
    {
#if UNITY_ANDROID
        return android_interstitial_id;
#elif UNITY_IPHONE
		return ios_interstitial_id;
#else
        return "unexpected_platform";
#endif
    }

    public string GetRewardedVideoAdUnitId()
    {
#if UNITY_ANDROID
        return android_rewarded_id;
#elif UNITY_IPHONE
		return ios_rewarded_id;
#else
        return "unexpected_platform";
#endif
    }

    // [수정] 배너 아이디 반환 로직 정상화
    public string GetBannerAdUnitId()
    {
#if UNITY_ANDROID
        return android_banner_id;
#elif UNITY_IPHONE
        return ios_banner_id;
#else
        return "editor_test_id";
#endif
    }
    #endregion
}