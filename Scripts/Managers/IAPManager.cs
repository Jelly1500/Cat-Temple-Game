using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : Singleton<IAPManager>, ISaveable
{
    private StoreController _storeController;
    private IAPData _data = new IAPData();

    private bool _isInitialized = false;
    public bool IsInitialized => _isInitialized;

    // 후원 보상 금액
    private const int DONATION_REWARD_GOLD = 10000;

    // 구매 완료 콜백 저장용
    private Action _onPurchaseSuccess;
    private Action<string> _onPurchaseFailed;

    public async void Init()
    {
        SaveManager.Instance.Register(this);
        await InitializePurchasing();
    }

    private async Task InitializePurchasing()
    {
        if (_isInitialized) return;

        try
        {
            _storeController = UnityIAPServices.StoreController();

            // 기존 구매 진행 관련 콜백
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;

            // [추가] 초기화 및 상품 조회 관련 필수 콜백
            _storeController.OnStoreDisconnected += (error) =>
            {
                Debug.LogWarning("[IAP] Store Disconnected.");
                _isInitialized = false;
            };

            _storeController.OnProductsFetched += (products) =>
            {
                Debug.Log($"[IAP] Successfully fetched {products.Count()} products.");
            };

            _storeController.OnProductsFetchFailed += (reason) =>
            {
                Debug.LogError($"[IAP] Failed to fetch products: {reason}");
            };

            await _storeController.Connect();

            var products = new List<ProductDefinition>
            {
                new ProductDefinition(ProductIDs.HAMMER_BOOST, ProductType.NonConsumable),
                new ProductDefinition(ProductIDs.CAT_EXPANSION, ProductType.NonConsumable),
                new ProductDefinition(ProductIDs.DONATION, ProductType.NonConsumable) // 1회성
            };

            _storeController.FetchProducts(products);
            _isInitialized = true;

        }
        catch (Exception e)
        {
            Debug.LogError($"[IAP] Initialization Failed: {e.Message}");
        }
    }

    #region Purchase State Check

    public bool CanPurchase(string productId)
    {
        if (!_isInitialized) return false;

        return productId switch
        {
            ProductIDs.HAMMER_BOOST => !_data.HasPurchasedHammerBoost,
            ProductIDs.CAT_EXPANSION => !_data.HasPurchasedCatExpansion,
            ProductIDs.DONATION => !_data.HasPurchasedDonation,
            _ => false
        };
    }

    public bool IsPurchased(string productId)
    {
        return productId switch
        {
            ProductIDs.HAMMER_BOOST => _data.HasPurchasedHammerBoost,
            ProductIDs.CAT_EXPANSION => _data.HasPurchasedCatExpansion,
            ProductIDs.DONATION => _data.HasPurchasedDonation,
            _ => false
        };
    }

    #endregion

    #region Purchase Methods

    public void Purchase(string productId)
    {
        Purchase(productId, null, null);
    }

    public void Purchase(string productId, Action onSuccess)
    {
        Purchase(productId, onSuccess, null);
    }

    public void Purchase(string productId, Action onSuccess, Action<string> onFailed)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[IAP] Not Initialized");
            onFailed?.Invoke("IAP not initialized");
            return;
        }

        if (!CanPurchase(productId))
        {
            Debug.LogWarning($"[IAP] Cannot purchase: {productId}");
            onFailed?.Invoke("Already purchased or not available");
            return;
        }

        _onPurchaseSuccess = onSuccess;
        _onPurchaseFailed = onFailed;

        Debug.Log($"[IAP] Purchasing: {productId}");
        _storeController.PurchaseProduct(productId);
    }

    #endregion

    #region Event Handlers (IAP v5)

    private void OnPurchasePending(PendingOrder order)
    {
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;

        string productId = product.definition.id;
        Debug.Log($"[IAP] Purchase Pending: {productId}");

        ProcessOrder(productId);
        _storeController.ConfirmPurchase(order);

        _onPurchaseSuccess?.Invoke();
        ClearCallbacks();
    }

    private void OnPurchaseFailed(FailedOrder failedOrder)
    {
        var product = failedOrder.CartOrdered.Items().FirstOrDefault()?.Product;
        string productId = product != null ? product.definition.id : "Unknown";
        var reason = failedOrder.FailureReason;

        Debug.LogError($"[IAP] Purchase Failed: {productId}, Reason: {reason}");

        _onPurchaseFailed?.Invoke(reason.ToString());
        ClearCallbacks();

        UIManager.Instance.ShowGameToast(DataManager.Instance.GetText("UI_IAP_PurchaseFailed"));
    }

    private void OnPurchasesFetched(Orders orders)
    {
        foreach (var order in orders.ConfirmedOrders)
        {
            var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product == null) continue;

            string id = product.definition.id;

            if (id == ProductIDs.HAMMER_BOOST && !_data.HasPurchasedHammerBoost)
            {
                ApplyHammerBoost();
            }
            else if (id == ProductIDs.CAT_EXPANSION && !_data.HasPurchasedCatExpansion)
            {
                ApplyCatExpansion();
            }
            else if (id == ProductIDs.DONATION && !_data.HasPurchasedDonation)
            {
                ApplyDonationReward();
            }
        }
    }

    private void ClearCallbacks()
    {
        _onPurchaseSuccess = null;
        _onPurchaseFailed = null;
    }

    #endregion

    #region Reward Logic

    private void ProcessOrder(string productId)
    {
        switch (productId)
        {
            case ProductIDs.HAMMER_BOOST:
                ApplyHammerBoost();
                break;
            case ProductIDs.CAT_EXPANSION:
                ApplyCatExpansion();
                break;
            case ProductIDs.DONATION:
                ApplyDonationReward();
                break;
        }
        SaveManager.Instance.Save();
    }

    private void ApplyHammerBoost()
    {
        if (_data.HasPurchasedHammerBoost) return;
        _data.HasPurchasedHammerBoost = true;
        GameDataManager.Instance.NotifyHammerChanged();
        Debug.Log("[IAP] Hammer Boost applied");
    }

    private void ApplyCatExpansion()
    {
        if (_data.HasPurchasedCatExpansion) return;
        _data.HasPurchasedCatExpansion = true;

        DiscipleManager.Instance.IncreaseMaxCount(1);
        var newDisciple = DiscipleManager.Instance.CreateAndAddNewDisciple(3);

        Debug.Log($"[IAP] Cat Expansion applied - 3등급 제자 영입 시도 완료 (ID: {newDisciple.id}, Name: {newDisciple.name})");
    }

    private void ApplyDonationReward()
    {
        if (_data.HasPurchasedDonation) return;
        _data.HasPurchasedDonation = true;

        LetterManager.Instance.ScheduleDonationThankYouLetter(DONATION_REWARD_GOLD);

        Debug.Log("[IAP] Donation reward applied - Thank you letter scheduled");
    }

    #endregion

    #region ISaveable Implementation

    public void SaveTo(GameData data)
    {
        data.iap = _data;
    }

    public void LoadFrom(GameData data)
    {
        _data = data.iap ?? new IAPData();
    }

    public void ResetToDefault()
    {
        _data = new IAPData();
    }

    #endregion
}