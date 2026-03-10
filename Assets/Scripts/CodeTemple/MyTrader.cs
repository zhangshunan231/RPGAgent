using UnityEngine;

public class MyTrader : MonoBehaviour
{
    public int gold = 100;

    public void Trade(int amount, MyTrader other)
    {
        if (gold >= amount)
        {
            gold -= amount;
            other.ReceiveGold(amount);
            Debug.Log($"交易成功，支付 {amount} 金币");
        }
        else
        {
            Debug.Log("金币不足，无法交易");
        }
    }

    public void ReceiveGold(int amount)
    {
        gold += amount;
        Debug.Log($"收到 {amount} 金币，当前金币：{gold}");
    }
} 