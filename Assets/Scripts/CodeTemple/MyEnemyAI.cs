using UnityEngine;

public class MyEnemyAI : MonoBehaviour
{
    public int health = 100;
    public int attackPower = 20;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"敌人受到 {damage} 点伤害，剩余生命：{health}");
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("敌人已死亡");
        Destroy(gameObject);
    }
} 