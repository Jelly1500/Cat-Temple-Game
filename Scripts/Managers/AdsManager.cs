using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.LevelPlay;

public class AdsManager : Singleton<AdsManager>
{
    LevelPlayInterstitialAd _interstitialAd;
    LevelPlayRewardedAd _rewardedAd;
    Action _rewardedCallback;
    LevelPlayBannerAd _bannerAd;

    [SerializeField] private bool isTestMode = true;
    public bool IsInitialized { get; private set; } = false;

    #region 보상 처리
    public void ShowInterstitialAds()
    {
        _interstitialAd.LoadAd();
    }

    private void ShowInterstitialAds_AfterLoading()
    {
        if (_interstitialAd.IsAdReady())
            _interstitialAd.ShowAd();
    }

    public void ShowRewardedAds(Action rewardedCallback)
    {
        _rewardedCallback = rewardedCallback;
        _rewardedAd.LoadAd();
    }

    private void ShowRewardedAds_AfterLoading()
    {
        if (_rewardedAd.IsAdReady())
            _rewardedAd.ShowAd();
    }

    private void HandleRewards_AfterRewardedAd()
    {
        _rewardedCallback?.Invoke();
        _rewardedCallback = null;
    }
    #endregion

    public void Init()
    {
        Debug.Log("[LevelPlaySample] LevelPlay.ValidateIntegration");
        LevelPlay.ValidateIntegration();

        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

        // 테스트 모드에 따른 App Key 분기
        string appKey = isTestMode ? "8545d445" : DataManager.Instance.AdsConfig.GetAppKey();

        Debug.Log($"[LevelPlaySample] Initializing with AppKey: {appKey} (TestMode: {isTestMode})");
        LevelPlay.Init(appKey);
    }

    void EnableAds()
    {
        // Register to ImpressionDataReadyEvent
        LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;

        // Create Rewarded Video object
        string rewardedId = isTestMode ? "DefaultRewardedVideo" : DataManager.Instance.AdsConfig.GetRewardedVideoAdUnitId();
        _rewardedAd = new LevelPlayRewardedAd(rewardedId);

        // Register to Rewarded Video events
        _rewardedAd.OnAdLoaded += RewardedVideoOnLoadedEvent;
        _rewardedAd.OnAdLoadFailed += RewardedVideoOnAdLoadFailedEvent;
        _rewardedAd.OnAdDisplayed += RewardedVideoOnAdDisplayedEvent;
        _rewardedAd.OnAdDisplayFailed += RewardedVideoOnAdDisplayedFailedEvent;
        _rewardedAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
        _rewardedAd.OnAdClicked += RewardedVideoOnAdClickedEvent;
        _rewardedAd.OnAdClosed += RewardedVideoOnAdClosedEvent;
        _rewardedAd.OnAdInfoChanged += RewardedVideoOnAdInfoChangedEvent;

        // Create Interstitial object
        string interstitialId = isTestMode ? "DefaultInterstitial" : DataManager.Instance.AdsConfig.GetInterstitialAdUnitId();
        _interstitialAd = new LevelPlayInterstitialAd(interstitialId);

        // Register to Interstitial events
        _interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
        _interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
        _interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
        _interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
        _interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
        _interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
        _interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;

        // 1. 기준이 되는 BANNER 객체 참조 (필요에 따라 생성 생략 가능)
        string bannerId = isTestMode ? "DefaultBanner" : DataManager.Instance.AdsConfig.GetBannerAdUnitId();    
        LevelPlayAdSize baseBanner = LevelPlayAdSize.BANNER;

        // 2. 가로 +100px, 세로 +50px 증가된 커스텀 사이즈 생성
        int customWidth = baseBanner.Width + 100;
        int customHeight = baseBanner.Height + 15;
        LevelPlayAdSize customSize = LevelPlayAdSize.CreateCustomBannerSize(customWidth, customHeight);

        // 3. 빌더에 커스텀 사이즈 적용
        var bannerConfig = new LevelPlayBannerAd.Config.Builder()
            .SetSize(customSize)
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .Build();
        // 2. 설정값을 포함하여 배너 객체 생성 (매개변수 2개 사용)
        _bannerAd = new LevelPlayBannerAd(bannerId, bannerConfig);

        // Register to Banner events
        _bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
        _bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
        _bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
        _bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
        _bannerAd.OnAdClicked += BannerOnAdClickedEvent;
        _bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
        _bannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
        _bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;

        IsInitialized = true;

        ShowBannerAds();
    }

    #region 배너 처리
    public void ShowBannerAds()
    {
        if (!IsInitialized) return;
        _bannerAd.LoadAd();
    }

    public void ResumeBannerAds()
    {
        if (!IsInitialized) return;
        _bannerAd.ShowAd();
    }

    private void ShowBannerAds_AfterLoading()
    {
        if (!IsInitialized) return;
        _bannerAd.ShowAd();
    }

    public void HideBannerAds()
    {
        if (!IsInitialized) return;
        _bannerAd.HideAd();
    }

    public void DestroyBannerAds()
    {
        if (!IsInitialized) return;
        _bannerAd.DestroyAd();
    }
    #endregion

    // 4. #region 로그 내부에 배너 관련 이벤트 핸들러 추가
    #region 배너 로그
    void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
        ShowBannerAds_AfterLoading();
    }

    void BannerOnAdLoadFailedEvent(LevelPlayAdError error)
    {
    }

    void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void BannerOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
    }

    void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo)
    {
    }

    void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo)
    {
    }
    #endregion

    #region 로그
    void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
    {
        Debug.Log($"[LevelPlaySample] Received SdkInitializationCompletedEvent with Config: {config}");
        EnableAds();
    }

    void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.Log($"[LevelPlaySample] Received SdkInitializationFailedEvent with Error: {error}");
    }

    void RewardedVideoOnLoadedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnLoadedEvent With AdInfo: {adInfo}");
        ShowRewardedAds_AfterLoading();
    }

    void RewardedVideoOnAdLoadFailedEvent(LevelPlayAdError error)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdLoadFailedEvent With Error: {error}");
    }

    void RewardedVideoOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdDisplayedEvent With AdInfo: {adInfo}");
    }

    void RewardedVideoOnAdDisplayedFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdDisplayedFailedEvent With AdInfo: {adInfo} and Error: {error}");
    }

    void RewardedVideoOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdRewardedEvent With AdInfo: {adInfo} and Reward: {reward}");
        HandleRewards_AfterRewardedAd();
    }

    void RewardedVideoOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdClickedEvent With AdInfo: {adInfo}");
    }

    void RewardedVideoOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdClosedEvent With AdInfo: {adInfo}");
    }

    void RewardedVideoOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received RewardedVideoOnAdInfoChangedEvent With AdInfo {adInfo}");
    }

    void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdLoadedEvent With AdInfo: {adInfo}");
        ShowInterstitialAds_AfterLoading();
    }

    void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdLoadFailedEvent With Error: {error}");
    }

    void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdDisplayedEvent With AdInfo: {adInfo}");
    }

    void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdDisplayFailedEvent With AdInfo: {adInfo} and Error: {error}");
    }

    void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdClickedEvent With AdInfo: {adInfo}");
    }

    void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdClosedEvent With AdInfo: {adInfo}");
    }

    void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[LevelPlaySample] Received InterstitialOnAdInfoChangedEvent With AdInfo: {adInfo}");
    }

    void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData)
    {
        Debug.Log($"[LevelPlaySample] Received ImpressionDataReadyEvent ToString(): {impressionData}");
        Debug.Log($"[LevelPlaySample] Received ImpressionDataReadyEvent allData: {impressionData.AllData}");
    }
    #endregion
    public void ShowBannerAdAt(RectTransform targetRect)
    {
        if (_bannerAd == null) return;

        // 1. UI 요소의 월드 좌표를 스크린 좌표로 변환
        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        // UI 위치 기준 (중단 아래쪽) 계산 (Screen Space)
        float screenHeight = Screen.height;
        float bannerY = corners[0].y; // 아래쪽 경계선 좌표

        // 2. LevelPlay 배너 위치 설정 (참고: API에 따라 Position 설정 방식이 다를 수 있음)
        // 일반적으로 하단 고정이 많지만, 특정 위치를 원할 경우 Layout 옵션을 활용합니다.
        _bannerAd.LoadAd();
    }
}