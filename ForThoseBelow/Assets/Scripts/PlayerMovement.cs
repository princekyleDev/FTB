
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool canMove = true;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastFacingDirection = Vector2.down;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (!canMove)
            return;

        moveInput = context.ReadValue<Vector2>();

        if (moveInput != Vector2.zero)
        {
            lastFacingDirection = moveInput.normalized;
        }

        animator.SetBool("isWalking", moveInput != Vector2.zero);
        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);

        if (context.canceled)
        {
            animator.SetFloat("LastInputX", lastFacingDirection.x);
            animator.SetFloat("LastInputY", lastFacingDirection.y);
        }
    }

    public void DisableMovement()
    {
        canMove = false;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        animator.SetBool("isWalking", false);
        animator.SetFloat("LastInputX", lastFacingDirection.x);
        animator.SetFloat("LastInputY", lastFacingDirection.y);
    }

    public void EnableMovement()
    {
        canMove = true;

        animator.SetBool("isWalking", false);
        animator.SetFloat("LastInputX", lastFacingDirection.x);
        animator.SetFloat("LastInputY", lastFacingDirection.y);
    }
}