using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	public LayerMask groundLayer;
	public Transform groundCheck;
	public float groundCheckDistance = 0.1f;

	public float jumpForce = 3f;
	public float moveSpeed = 2f;

	public Rigidbody2D rb;
	public Animator animator;

	private Vector2 moveInput;
	private bool jumpPressed = false;
	private bool isGrounded = false;
	private bool isAttacking = false;

	private InputSystem_Actions controls;

	private void Awake()
	{
		controls = new InputSystem_Actions();
		
		controls.Player.Run.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
		controls.Player.Run.canceled += ctx => moveInput = Vector2.zero;

		controls.Player.Jump.performed += ctx => jumpPressed = true;
		controls.Player.Jump.canceled += ctx => jumpPressed = false;

		controls.Player.Attack.performed += ctx => StartAttack();
	}

	private void Update()
	{
		if (isAttacking) return ;

		CheckGrounded();

		animator.SetFloat("Speed", Mathf.Abs(moveInput.x));
		animator.SetBool("IsFalling", !isGrounded && rb.linearVelocity.y < 0f);
		animator.SetBool("IsJumping", !isGrounded && rb.linearVelocity.y > 0f);

		if (jumpPressed && isGrounded)
		{
			Jump();
		}
	}

	private void FixedUpdate()
	{
		if (isAttacking) return ;

		Vector2 velocity = rb.linearVelocity; 
		velocity.x = moveInput.x * moveSpeed;
		rb.linearVelocity = velocity;

		if (moveInput.x > 0)
			transform.localScale = Vector3.one; // Vector3(1, 1, 1)
		else if (moveInput.x < 0)
			transform.localScale = new Vector3(-1, 1, 1);
	}

	void StartAttack()
	{
		if (isAttacking) return ;

		isAttacking = true;
		animator.SetTrigger("Attack");
	}

	void EndAttack()
	{
		isAttacking = false;
	}

	private void Jump()
	{
		rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
		jumpPressed = false;
	}

	private void CheckGrounded()
	{
		isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
	}

	private void OnEnable()
	{
		controls.Player.Enable();
	}

	private void OnDisable()
	{
		controls.Player.Disable();
	}
}
