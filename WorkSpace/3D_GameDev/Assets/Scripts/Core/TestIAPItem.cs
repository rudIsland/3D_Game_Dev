using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class TestIAPItem : MonoBehaviour
{
    public void OnPurchaseSuccess(Product product)
    {
        Debug.Log("���� ����: " + product.definition.id);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failure)
    {
        Debug.LogWarning($"���� ����: {product.definition.id}, ����: {failure.reason}");
    }

}
