using UnityEngine;
using System.Collections.Generic;

public class MainCharacterController : MonoBehaviour
{
    [Header("主角属性")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public int maxHealth = 100;
    public int currentHealth;
    
    [Header("战斗属性")]
    public int attack = 15;
    public int defense = 8;
    public float attackRange = 2f;
    
    [Header("技能系统")]
    public List<Skill> skills = new List<Skill>();
    
    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }
    
    void Update()
    {
        HandleMovement();
        HandleCombat();
    }
    
    void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }
    
    void HandleCombat()
    {
        if (Input.GetMouseButtonDown(0)) // 左键攻击
        {
            Attack();
        }
    }
    
    void Attack()
    {
        // 检测攻击范围内的敌人
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                var enemyHealth = enemy.GetComponent<EnemyController>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(attack);
                }
            }
        }
    }
    
    public void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - defense);
        currentHealth -= actualDamage;
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log("主角死亡");
        // 处理死亡逻辑
    }
}

[System.Serializable]
public class Skill
{
    public string skillName;
    public int damage;
    public float cooldown;
    public float range;
} 