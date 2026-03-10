using UnityEngine;

public class MyCharacterController : MonoBehaviour
{
    public float moveSpeed = 5f;

    [SerializeField] private Animator animator;

    private static readonly int StateParam = Animator.StringToHash("State");
    private int currentState = -1;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(h, v);
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 move = new Vector3(input.x, input.y, 0f);
        transform.Translate(move * moveSpeed * Time.deltaTime);

        UpdateAnimationState(input);
    }

    private void UpdateAnimationState(Vector2 input)
    {
        if (animator == null)
        {
            return;
        }

        int state = 0;
        if (input.sqrMagnitude > 0.0001f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                state = input.x > 0f ? 4 : 3;
            }
            else
            {
                state = input.y > 0f ? 1 : 2;
            }
        }

        if (state != currentState)
        {
            currentState = state;
            animator.SetInteger(StateParam, state);
        }
    }
}
