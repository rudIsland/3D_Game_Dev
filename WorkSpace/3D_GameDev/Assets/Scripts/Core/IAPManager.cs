using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    private IStoreController storeController;
    private IExtensionProvider storeExtensionProvider;

    private const string testProductId = "test1";

    void Start()
    {
        InitPurchasing();
    }

    private void InitPurchasing()
    {
        if (storeController != null) return;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(testProductId, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }
    public void BuyTestItem()
    {
        if (storeController == null)
        {
            Debug.LogWarning("Store not initialized.");
            return;
        }

        Product product = storeController.products.WithID(testProductId);
        if (product != null && product.availableToPurchase)
        {
            Debug.Log("▶ 결제 시도: " + testProductId);
            storeController.InitiatePurchase(product);
        }
        else
        {
            Debug.LogWarning("상품 없음 또는 구매 불가.");
        }
    }


    //초기화
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        storeExtensionProvider = extensions;
    }
    //초기화 실패
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("초기화 실패: " + error);
    }

    //초기화 실패2
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"초기화 실패: {error}, 메시지: {message}");
    }

    //구매 시도
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        Debug.Log("결제 성공: " + purchaseEvent.purchasedProduct.definition.id);

        //여기서 결제 후 아이템 지급 처리
        return PurchaseProcessingResult.Complete;
    }

    //구매 실패
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($" 결제 실패: {product.definition.id}, 사유: {failureReason}");
    }

    //구매 실패 2
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogWarning($" 결제 실패: {product.definition.id}, 사유: {failureDescription}");
    }

}
