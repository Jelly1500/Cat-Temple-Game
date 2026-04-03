using UnityEngine;
using TMPro;

public class ShopUI : UI_UGUI, IUI_Popup
{
    #region Enums for Binding
    enum Buttons
    {
        HammerBoostButton,
        CatExpansionButton,
        DonationButton,
        CloseButton
    }

    enum Texts
    {
        // 공통
        TitleText,
        CloseButtonText,

        // 망치 강화
        NameText_Hammer,
        DescText_Hammer,
        PriceText_Hammer,
        HammerBoostButtonText,

        // 제자 확장
        NameText_Cat,
        DescText_Cat,
        PriceText_Cat,
        CatExpansionButtonText,

        // 개발자 후원
        NameText_Donation,
        DescText_Donation,
        PriceText_Donation,
        DonationButtonText,
    }

    enum GameObjects
    {
        
    }
    #endregion

    protected override void Start()
    {
        // Start 시점에 초기화 진행
        Init();
        base.Start();
    }

    public override void Init()
    {
        if (_init) return;
        base.Init();

        // 1. 바인딩
        BindButtons(typeof(Buttons));
        BindTexts(typeof(Texts));
        BindObjects(typeof(GameObjects));

        // 2. 이벤트 리스너 연결
        GetButton((int)Buttons.HammerBoostButton).onClick.AddListener(OnHammerBoostClicked);
        GetButton((int)Buttons.CatExpansionButton).onClick.AddListener(OnCatExpansionClicked);
        GetButton((int)Buttons.DonationButton).onClick.AddListener(OnDonationClicked);
        GetButton((int)Buttons.CloseButton).onClick.AddListener(OnCloseClicked);

        // [핵심 수정] 3. 초기화 직후 UI 상태 갱신 (오버레이 및 버튼 상태 동기화)
        RefreshUI();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 팝업이 다시 열릴 때도 상태 갱신
        if (_init) RefreshUI();
    }

    public override void RefreshUI()
    {
        if (!_init) return;

        // IAP 매니저 미초기화 시 예외 처리
        if (!IAPManager.Instance.IsInitialized)
        {
            SetAllButtonsInteractable(false);
            
            return;
        }
        RefreshLocalizedTexts();

        // 각 아이템별 구매 상태 확인 및 오버레이 갱신
        RefreshHammerBoost();
        RefreshCatExpansion();
        RefreshDonation();
    }


    private void SetAllButtonsInteractable(bool interactable)
    {
        GetButton((int)Buttons.HammerBoostButton).interactable = interactable;
        GetButton((int)Buttons.CatExpansionButton).interactable = interactable;
        GetButton((int)Buttons.DonationButton).interactable = interactable;
    }

    #region Localized Texts
    private void RefreshLocalizedTexts()
    {
        if (DataManager.Instance == null || !DataManager.Instance.IsLoaded) return;

        // 타이틀 및 닫기 버튼
        GetText((int)Texts.TitleText).text = DataManager.Instance.GetText("UI_Shop_Title");
        GetText((int)Texts.CloseButtonText).text = DataManager.Instance.GetText("UI_Common_Close");

        // 망치 강화 텍스트
        GetText((int)Texts.NameText_Hammer).text = DataManager.Instance.GetText("UI_Shop_HammerBoost_Name");
        GetText((int)Texts.DescText_Hammer).text = DataManager.Instance.GetText("UI_Shop_HammerBoost_Desc");
        GetText((int)Texts.PriceText_Hammer).text = DataManager.Instance.GetText("UI_Shop_HammerBoost_Price");

        // 제자 확장 텍스트
        GetText((int)Texts.NameText_Cat).text = DataManager.Instance.GetText("UI_Shop_CatExpansion_Name");
        GetText((int)Texts.DescText_Cat).text = DataManager.Instance.GetText("UI_Shop_CatExpansion_Desc");
        GetText((int)Texts.PriceText_Cat).text = DataManager.Instance.GetText("UI_Shop_CatExpansion_Price");

        // 후원 텍스트
        GetText((int)Texts.NameText_Donation).text = DataManager.Instance.GetText("UI_Shop_Donation_Name");
        GetText((int)Texts.DescText_Donation).text = DataManager.Instance.GetText("UI_Shop_Donation_Desc");
        GetText((int)Texts.PriceText_Donation).text = DataManager.Instance.GetText("UI_Shop_Donation_Price");
    }
    #endregion

    #region Refresh Item Status (오버레이 로직)

    private void RefreshHammerBoost()
    {
        // [체크] IAP 구매 내역 확인
        bool isPurchased = IAPManager.Instance.IsPurchased(ProductIDs.HAMMER_BOOST);

        // 버튼 상태 및 텍스트 설정
        GetButton((int)Buttons.HammerBoostButton).interactable = !isPurchased;
        var buttonText = GetText((int)Texts.HammerBoostButtonText);
        if (buttonText != null)
        {
            buttonText.text = isPurchased
                ? DataManager.Instance.GetText("UI_Shop_SoldOut")
                : DataManager.Instance.GetText("UI_Shop_Buy");
        }

    }

    private void RefreshCatExpansion()
    {
        // [체크]
        bool isPurchased = IAPManager.Instance.IsPurchased(ProductIDs.CAT_EXPANSION);

        // 버튼 상태 및 텍스트
        GetButton((int)Buttons.CatExpansionButton).interactable = !isPurchased;
        var buttonText = GetText((int)Texts.CatExpansionButtonText);
        if (buttonText != null)
        {
            buttonText.text = isPurchased
                ? DataManager.Instance.GetText("UI_Shop_SoldOut")
                : DataManager.Instance.GetText("UI_Shop_Buy");
        }

    }

    private void RefreshDonation()
    {
        // [체크]
        bool isPurchased = IAPManager.Instance.IsPurchased(ProductIDs.DONATION);

        // 버튼 상태 및 텍스트
        GetButton((int)Buttons.DonationButton).interactable = !isPurchased;
        var buttonText = GetText((int)Texts.DonationButtonText);
        if (buttonText != null)
        {
            buttonText.text = isPurchased
                ? DataManager.Instance.GetText("UI_Shop_SoldOut")
                : DataManager.Instance.GetText("UI_Shop_Buy");
        }

    }

    #endregion

    #region Button Handlers

    private void OnHammerBoostClicked()
    {
        if (IAPManager.Instance.IsPurchased(ProductIDs.HAMMER_BOOST))
        {
            UIManager.Instance.ShowGameToast(DataManager.Instance.GetText("UI_Shop_AlreadyPurchased"));
            return;
        }

        IAPManager.Instance.Purchase(ProductIDs.HAMMER_BOOST,
            onSuccess: () =>
            {
                RefreshUI();
                UIManager.Instance.ShowGameToast(DataManager.Instance.GetText("UI_Shop_HammerBoost_Success"));
            },
            onFailed: (reason) =>
            {
                Debug.LogWarning($"[ShopUI] Hammer boost purchase failed: {reason}");
            }
        );
    }

    private void OnCatExpansionClicked()
    {
        if (IAPManager.Instance.IsPurchased(ProductIDs.CAT_EXPANSION))
        {
            UIManager.Instance.ShowGameToast(DataManager.Instance.GetText("UI_Shop_AlreadyPurchased"));
            return;
        }

        IAPManager.Instance.Purchase(ProductIDs.CAT_EXPANSION,
            onSuccess: () =>
            {
                RefreshUI();
                UIManager.Instance.ShowGameToast(DataManager.Instance.GetText("UI_Shop_CatExpansion_Success"));
            },
            onFailed: (reason) =>
            {
                Debug.LogWarning($"[ShopUI] Cat expansion purchase failed: {reason}");
            }
        );
    }

    private void OnDonationClicked()
    {
        if (IAPManager.Instance.IsPurchased(ProductIDs.DONATION))
        {
            UIManager.Instance.ShowGameToast(DataManager.Instance.GetText("UI_Shop_PurchaseFailed"));
            return;
        }

        IAPManager.Instance.Purchase(ProductIDs.DONATION,
            onSuccess: () =>
            {
                RefreshUI();
                UIManager.Instance.ShowGameToast(DataManager.Instance.GetText("UI_Shop_Donation_Success"));
            },
            onFailed: (reason) =>
            {
                Debug.LogWarning($"[ShopUI] Donation purchase failed: {reason}");
            }
        );
    }

    private void OnCloseClicked()
    {
        UIManager.Instance.ClosePopupUI();
    }

    #endregion
}