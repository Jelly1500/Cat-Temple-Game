using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

[System.Serializable]
public class ProductData
{
    public string productId;
    public ProductType productType;
    // TODO
}
public static class ProductIDs
{
    public const string HAMMER_BOOST = "com.yourgame.hammer_boost";
    public const string CAT_EXPANSION = "com.yourgame.cat_expansion";
    public const string DONATION = "com.yourgame.donation";
}

[CreateAssetMenu(fileName = "IAPConfig", menuName = "Config/IAPConfig")]
public class IAPConfig : ScriptableObject
{
    [Header("Product Definitions")]
    [SerializeField]
    private List<ProductData> products = new List<ProductData>
    {
        new ProductData { productId = "com.yourgame.hammer_boost", productType = ProductType.NonConsumable },
        new ProductData { productId = "com.yourgame.cat_expansion", productType = ProductType.NonConsumable },
        new ProductData { productId = "com.yourgame.donation", productType = ProductType.NonConsumable }
    };

    public List<ProductDefinition> GetProductDefinitions()
    {
        var productDefinitions = new List<ProductDefinition>();
        foreach (var product in products)
        {
            productDefinitions.Add(new ProductDefinition(product.productId, product.productType));
        }
        return productDefinitions;
    }
}

