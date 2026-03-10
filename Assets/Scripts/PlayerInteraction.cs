using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public float interactionRange = 3f;
    public KeyCode interactionKey = KeyCode.E;
    public LayerMask interactableLayers = -1;
    
    [Header("UI提示")]
    public GameObject interactionPrompt;
    public TMPro.TextMeshProUGUI promptText;
    
    // 私有变量
    private RPGCharacter nearestCharacter;
    private RPGItem nearestItem;
    private bool canInteract = false;
    
    void Update()
    {
        CheckInteractables();
        
        if (canInteract && Input.GetKeyDown(interactionKey))
        {
            PerformInteraction();
        }
        
        UpdateInteractionPrompt();
    }
    
    void CheckInteractables()
    {
        // 检测角色
        Collider[] characterColliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayers);
        nearestCharacter = null;
        nearestItem = null;
        canInteract = false;
        
        float closestDistance = float.MaxValue;
        
        foreach (var collider in characterColliders)
        {
            float distance = Vector3.Distance(transform.position, collider.transform.position);
            
            if (distance < closestDistance)
            {
                // 检查是否是角色
                var character = collider.GetComponent<RPGCharacter>();
                if (character != null && character.isInteractable)
                {
                    nearestCharacter = character;
                    closestDistance = distance;
                    canInteract = true;
                }
                
                // 检查是否是物品
                var item = collider.GetComponent<RPGItem>();
                if (item != null && item.isPickable)
                {
                    nearestItem = item;
                    closestDistance = distance;
                    canInteract = true;
                }
            }
        }
    }
    
    void PerformInteraction()
    {
        if (nearestCharacter != null)
        {
            nearestCharacter.Interact();
        }
        else if (nearestItem != null)
        {
            nearestItem.Pickup();
        }
    }
    
    void UpdateInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(canInteract);
            
            if (canInteract && promptText != null)
            {
                if (nearestCharacter != null)
                {
                    promptText.text = nearestCharacter.interactionPrompt;
                }
                else if (nearestItem != null)
                {
                    promptText.text = nearestItem.pickupPrompt;
                }
            }
        }
    }
    
    // 编辑器辅助方法
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
} 