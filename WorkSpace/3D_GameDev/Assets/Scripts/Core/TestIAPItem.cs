using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class TestIAPItem : MonoBehaviour
{
    public void OnPurchaseSuccess(Product product)
    {
        Debug.Log("결제 성공: " + product.definition.id);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failure)
    {
        Debug.LogWarning($"결제 실패: {product.definition.id}, 이유: {failure.reason}");
    }

}
