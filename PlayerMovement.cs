using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	public LayerMask groundLayer;
	public Transform groundCheck;
	public float groundCheckDistance = 0.1f;

	public float crouchSize = 0.5f;

	public float damageForce = 5f;
	public float dashForce = 3f;
	public float jumpForce = 3f;
	public float moveSpeed = 2f;
	public float climbSpeed = 2f;

	// Crouch
	public CapsuleCollider2D standingCollider;
	public CapsuleCollider2D crouchingCollider;

	public Rigidbody2D rb;
	public Animator animator;

	private Vector2 moveInput;
	private bool attackPressed = false;
	private bool jumpPressed = false;
	private bool dashPressed = false;
	private bool crouchPressed = false;

	private bool isGrounded = false;
	private bool isCrouching = false;
	private bool isRunning = false;

	private bool isAttacking = false;
	private bool isDashing = false;

	private bool isHurting = false;
	private bool isDead = false;

	private bool onLadder = false;
	private bool onWall = false;

	private InputSystem_Actions controls;
	
	private void Awake()
	{
		standingCollider.enabled = true;
		crouchingCollider.enabled = false;

		controls = new InputSystem_Actions();
		
		controls.Player.Run.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
		controls.Player.Run.canceled += ctx => moveInput = Vector2.zero;

		controls.Player.Jump.performed += ctx => jumpPressed = true;
		controls.Player.Jump.canceled += ctx => jumpPressed = false;

		controls.Player.Attack.performed += ctx => attackPressed = true;
		controls.Player.Attack.canceled += ctx => attackPressed = false;

		controls.Player.Dash.performed += ctx => dashPressed = true;
		controls.Player.Dash.canceled += ctx => dashPressed = false;

		controls.Player.Crouch.performed += ctx => crouchPressed = true;
		controls.Player.Crouch.canceled += ctx => crouchPressed = false;
	}

	private void FixedUpdate()
	{
		if (isHurting || isDead) return ;
		else if (isAttacking || isCrouching) return ;
		else if (isDashing) return ;

		Run();
		PlayerDirection();
	}

	private void Update()
	{
		if (onLadder)
		{
			rb.linearVelocity = new Vector2(0, moveInput.y * climbSpeed);
			rb.gravityScale = 0;

			if (Mathf.Abs(moveInput.y) > 0)
			{
				animator.Play("Climb");
				animator.speed = 1f;
			}
			else
			{
				animator.Play("Climb");
				animator.speed = 0f;

			}
			return ; 
		}
		else
		{
			rb.gravityScale = 1;
			animator.speed = 1f;
		}

		if (isHurting || isDead) return ;
		else if (isAttacking) return ;
		// 대시 중에는 
		else if (isDashing)
		{
			// Dash Attack
			if (attackPressed)
			{
				EndDash();
				animator.SetTrigger("Attack");
				StartAttack();
			}
			// Slide
			else if (crouchPressed)
			{
				SetCrouchCollider(true);
				animator.SetTrigger("Slide");
			}
			// 나머지 불가
			else return ;
		}
		else if (isCrouching)
		{
			// 앉기 해제
			if (!crouchPressed)
			{
				isCrouching = false;
				animator.SetBool("IsCrouching", false);
				SetCrouchCollider(false);
			}
			else return ;
		}
		CheckGrounded();

		animator.SetFloat("Speed", Mathf.Abs(moveInput.x));
		animator.SetBool("IsFalling", !isGrounded && rb.linearVelocity.y < 0f);
		animator.SetBool("IsJumping", !isGrounded && rb.linearVelocity.y > 0f);

		// 앉기
		if (crouchPressed && !isCrouching)
		{
			isCrouching = true;
			animator.SetBool("IsCrouching", true);
			SetCrouchCollider(true);
		}
		// Trigger
		if (isGrounded)
		{
			if (jumpPressed)
			{
				Jump();
			}
			else if (attackPressed && !isDashing)
			{
				animator.SetTrigger("Attack");
				StartAttack();
			}
			else if (dashPressed && isRunning && !isDashing)
			{
				animator.SetTrigger("Dash");
				StartDash();
			}
		}
	}

	private void Jump()
	{
		rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
		jumpPressed = false;
	}

	private void OnTriggerEnter2D(Collider2D hit)
	{
		float rand = Random.Range(0f, 1f);

		if (hit.gameObject.layer == LayerMask.NameToLayer("Obstacle") && !isHurting)
		{
			Vector2 direction = transform.position - hit.transform.position;
			rb.linearVelocity = Vector2.zero;
			rb.AddForce(direction.normalized * damageForce, ForceMode2D.Impulse);

			isHurting = true; // player.TakeDamage();
			if (!isAttacking && !isDashing)
			{
				animator.SetTrigger("Hurt");
			}

			if (rand > 0.8f)
			{
				isDead = true; 
				animator.SetTrigger("Death"); // player.Death();
			}
		}
	}

	private void OnTriggerStay2D(Collider2D ladder)
	{
		
	}

	private void EndDeath()
	{
		gameObject.SetActive(false);
	}

	private void EndHurt()
	{
		isHurting = false;
	}


	void StartAttack()
	{
		isAttacking = true;
	}

	void EndAttack()
	{
		isDashing = false;
		isAttacking = false;
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

	private void StartDash()
	{
		isDashing = true;

		// 대시 동작
		if (moveInput.x > 0)
			rb.AddForce(Vector2.right * dashForce, ForceMode2D.Impulse);
		else
			rb.AddForce(Vector2.left * dashForce, ForceMode2D.Impulse);
	}

	private void EndDash()
	{
		isDashing = false;
	}
	private void EndSlide()
	{
		isDashing = false;
		if (crouchPressed && isGrounded)
		{
			isCrouching = true;
			SetCrouchCollider(true);
			animator.SetBool("IsCrouching", true);
		}
		else
		{
			isCrouching = false;
			animator.SetBool("IsCrouching", false);
			SetCrouchCollider(false);
		}
	}

	private void SetCrouchCollider(bool isCrouching)
	{
		standingCollider.enabled = !isCrouching;
		crouchingCollider.enabled = isCrouching;
	}

	private void Run()
	{
		if (moveInput.x != 0)
			isRunning = true;
		else
			isRunning = false;

		Vector2 velocity = rb.linearVelocity; 
		velocity.x = moveInput.x * moveSpeed;
		rb.linearVelocity = velocity;
	}

	private void PlayerDirection()
	{
		if (moveInput.x > 0)
			transform.localScale = Vector3.one;
		else if (moveInput.x < 0)
			transform.localScale = new Vector3(-1, 1, 1);
	}
}
