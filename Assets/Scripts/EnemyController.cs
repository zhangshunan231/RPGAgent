using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("敌人属性")]
    public string enemyName = "敌人";
    public int maxHealth = 50;
    public int currentHealth;
    public int attack = 10;
    public float moveSpeed = 3f;
    public float detectionRange = 8f;
    public float attackRange = 2f;
    
    [Header("AI行为")]
    public bool isHostile = true;
    public bool canMove = true;
    public Transform[] patrolPoints;
    public float waitTime = 1f;
    
    [Header("战斗")]
    public float attackCooldown = 1.5f;
    public int experienceReward = 10;
    
    private Transform player;
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float lastAttackTime = 0f;
    private Rigidbody2D rb;
    private Animator animator;
    private bool isChasing = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    void Update()
    {
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= detectionRange && isHostile)
            {
                isChasing = true;
                ChasePlayer();
            }
            else if (distanceToPlayer <= attackRange && isHostile)
            {
                AttackPlayer();
            }
            else
            {
                isChasing = false;
                if (canMove && patrolPoints.Length > 0)
                {
                    Patrol();
                }
            }
        }
    }
    
    void ChasePlayer()
    {
        if (player == null) return;
        
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
        
        // 更新朝向
        if (direction.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }
    
    void AttackPlayer()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            
            // 检测攻击范围内的玩家
            Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, LayerMask.GetMask("Player"));
            if (playerCollider != null)
            {
                var playerHealth = playerCollider.GetComponent<MainCharacterController>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attack);
                    Debug.Log($"{enemyName}攻击了玩家，造成{attack}点伤害");
                }
            }
        }
    }
    
    void Patrol()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
            return;
        }
        
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Vector2 direction = (targetPoint.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
        
        if (Vector2.Distance(transform.position, targetPoint.position) < 0.5f)
        {
            isWaiting = true;
            waitTimer = waitTime;
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"{enemyName}受到{damage}点伤害，剩余生命值：{currentHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log($"{enemyName}死亡，获得{experienceReward}点经验");
        
        // 给予玩家经验值
        var playerController = FindObjectOfType<MainCharacterController>();
        if (playerController != null)
        {
            // 这里可以调用玩家的经验值增加方法
            // playerController.GainExperience(experienceReward);
        }
        
        Destroy(gameObject);
    }
} 