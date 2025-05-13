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
            Debug.Log("�� ���� �õ�: " + testProductId);
            storeController.InitiatePurchase(product);
        }
        else
        {
            Debug.LogWarning("��ǰ ���� �Ǵ� ���� �Ұ�.");
        }
    }


    //�ʱ�ȭ
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        storeExtensionProvider = extensions;
    }
    //�ʱ�ȭ ����
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("�ʱ�ȭ ����: " + error);
    }

    //�ʱ�ȭ ����2
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"�ʱ�ȭ ����: {error}, �޽���: {message}");
    }

    //���� �õ�
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        Debug.Log("���� ����: " + purchaseEvent.purchasedProduct.definition.id);

        //���⼭ ���� �� ������ ���� ó��
        return PurchaseProcessingResult.Complete;
    }

    //���� ����
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($" ���� ����: {product.definition.id}, ����: {failureReason}");
    }

    //���� ���� 2
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogWarning($" ���� ����: {product.definition.id}, ����: {failureDescription}");
    }

}
