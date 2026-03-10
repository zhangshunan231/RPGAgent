using UnityEngine;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    [Header("NPC属性")]
    public string npcName = "NPC";
    public bool hasDialogue = true;
    public bool canMove = true;
    public float moveSpeed = 2f;
    public float patrolRadius = 5f;
    
    [Header("对话系统")]
    public List<string> dialogueLines = new List<string>();
    public bool isInteractable = true;
    
    [Header("AI行为")]
    public Transform[] patrolPoints;
    public float waitTime = 2f;
    
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private Rigidbody2D rb;
    private Animator animator;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        if (dialogueLines.Count == 0)
        {
            dialogueLines.Add("你好，旅行者！");
            dialogueLines.Add("有什么我可以帮助你的吗？");
        }
    }
    
    void Update()
    {
        if (canMove && patrolPoints.Length > 0)
        {
            Patrol();
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
    
    public void Interact()
    {
        if (hasDialogue && isInteractable)
        {
            ShowDialogue();
        }
    }
    
    void ShowDialogue()
    {
        string randomDialogue = dialogueLines[Random.Range(0, dialogueLines.Count)];
        Debug.Log($"{npcName}: {randomDialogue}");
        
        // 这里可以集成UI对话系统
        // DialogueManager.Instance.ShowDialogue(npcName, randomDialogue);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isInteractable)
        {
            // 显示交互提示
            Debug.Log($"按E与{npcName}对话");
        }
    }
} 