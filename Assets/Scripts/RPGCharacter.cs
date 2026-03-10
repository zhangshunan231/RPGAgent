using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueData
{
    public string dialogueId;
    public string[] dialogueLines;
    public bool isCompleted;
}

[System.Serializable]
public class CombatStats
{
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int attack = 10;
    public int defense = 5;
    public float attackSpeed = 1.0f;
    public float criticalChance = 0.1f;
}

public class RPGCharacter : MonoBehaviour
{
    [Header("角色信息")]
    public string characterName;
    public string characterType; // NPC, Enemy, Player
    public string description;
    
    [Header("对话系统")]
    public List<DialogueData> dialogues = new List<DialogueData>();
    public float interactionRange = 2f;
    public bool canTalk = true;
    
    [Header("战斗系统")]
    public CombatStats combatStats = new CombatStats();
    public bool canFight = false;
    public string[] attackAnimations;
    
    [Header("AI行为")]
    public bool hasAI = false;
    public float patrolRadius = 5f;
    public float idleTime = 3f;
    public Transform[] patrolPoints;
    
    [Header("交互")]
    public bool isInteractable = true;
    public string interactionPrompt = "按E交互";
    
    // 私有变量
    private Transform player;
    private bool isInRange = false;
    private int currentDialogueIndex = 0;
    private bool isInCombat = false;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        InitializeCharacter();
    }
    
    void Update()
    {
        if (player != null)
        {
            CheckPlayerDistance();
        }
        
        if (hasAI)
        {
            UpdateAI();
        }
    }
    
    void InitializeCharacter()
    {
        // 根据角色类型设置默认行为
        switch (characterType.ToLower())
        {
            case "npc":
                canTalk = true;
                canFight = false;
                hasAI = true;
                break;
            case "enemy":
                canTalk = false;
                canFight = true;
                hasAI = true;
                break;
            case "player":
                canTalk = false;
                canFight = true;
                hasAI = false;
                break;
        }
    }
    
    void CheckPlayerDistance()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = isInRange;
        isInRange = distance <= interactionRange;
        
        if (isInRange && !wasInRange)
        {
            OnPlayerEnterRange();
        }
        else if (!isInRange && wasInRange)
        {
            OnPlayerExitRange();
        }
    }
    
    void OnPlayerEnterRange()
    {
        if (isInteractable)
        {
            // 显示交互提示
            Debug.Log($"靠近 {characterName}: {interactionPrompt}");
        }
    }
    
    void OnPlayerExitRange()
    {
        // 隐藏交互提示
        Debug.Log($"离开 {characterName}");
    }
    
    public void Interact()
    {
        if (!isInRange || !isInteractable) return;
        
        if (canTalk && dialogues.Count > 0)
        {
            StartDialogue();
        }
        else if (canFight && characterType.ToLower() == "enemy")
        {
            StartCombat();
        }
    }
    
    public void StartDialogue()
    {
        if (dialogues.Count == 0) return;
        
        var currentDialogue = dialogues[currentDialogueIndex];
        Debug.Log($"[{characterName}] {string.Join(" ", currentDialogue.dialogueLines)}");
        
        // 这里可以集成到UI系统
        // DialogueManager.Instance.StartDialogue(currentDialogue);
    }
    
    public void StartCombat()
    {
        if (!canFight) return;
        
        isInCombat = true;
        Debug.Log($"{characterName} 进入战斗状态!");
        
        // 这里可以集成到战斗系统
        // CombatManager.Instance.StartCombat(this);
    }
    
    public void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - combatStats.defense);
        combatStats.currentHealth -= actualDamage;
        
        Debug.Log($"{characterName} 受到 {actualDamage} 点伤害，剩余血量: {combatStats.currentHealth}");
        
        if (combatStats.currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        combatStats.currentHealth = Mathf.Min(combatStats.maxHealth, combatStats.currentHealth + amount);
        Debug.Log($"{characterName} 恢复 {amount} 点血量，当前血量: {combatStats.currentHealth}");
    }
    
    void Die()
    {
        Debug.Log($"{characterName} 死亡");
        // 这里可以添加死亡动画、掉落物品等
        Destroy(gameObject);
    }
    
    void UpdateAI()
    {
        // 简单的AI行为
        if (isInCombat)
        {
            // 战斗AI
            UpdateCombatAI();
        }
        else
        {
            // 巡逻AI
            UpdatePatrolAI();
        }
    }
    
    void UpdateCombatAI()
    {
        if (player == null) return;
        
        // 简单的追击逻辑
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * Time.deltaTime * 2f;
        
        // 面向玩家
        transform.LookAt(player);
    }
    
    void UpdatePatrolAI()
    {
        // 简单的巡逻逻辑
        if (patrolPoints.Length > 0)
        {
            // 这里可以实现巡逻点移动
        }
    }
    
    // 编辑器辅助方法
    void OnDrawGizmosSelected()
    {
        // 显示交互范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 显示巡逻范围
        if (hasAI)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
        }
    }
} 