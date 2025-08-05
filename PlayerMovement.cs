using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

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

	private bool isOnLadder = false;
	// private bool isOnWall = false;

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
		else if (isOnLadder) return ;

		Run();
		PlayerDirection();
	}

	private void Update()
	{
		if (isOnLadder)
		{
			if (isHurting || isDead || jumpPressed)
			{
				isOnLadder = false;
				animator.speed = 1f; 
				rb.gravityScale = 1;
				animator.SetBool("IsOnLadder", false);

				if (jumpPressed)
				{
					Vector2 direction = new Vector2(moveInput.x, 0.5f).normalized;
					rb.AddForce(direction * jumpForce, ForceMode2D.Impulse);
					jumpPressed = false;
					return ;
				}
			}
			rb.linearVelocity = new Vector2(0, moveInput.y * climbSpeed);
			rb.gravityScale = 0;

			// 가만히 있을 때/ 움직일 때 애니메이션
			if (Mathf.Abs(moveInput.y) > 0)
				animator.speed = 1f;
			else
				animator.speed = 0f;
			return ; 
		}

		// 피격 중에는 아무것도 못함 ... 애니메이션 종료 시 Flag Off
		if (isHurting || isDead) return ;

		// 공격 중에도 아무것도 못함 ... 애니메이션 종료 시 Flag Off
		else if (isAttacking) return ;

		// Hurt는 Update가 아닌 TriggerEnter2D 에서 정의
		else if (isDashing)
		{
			// 대시 중에는 DashAttack/Slide/Hurt 를 할 수 있다
			if (!(attackPressed || crouchPressed)) return ;

			// Dash Attack
			else if (attackPressed)
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
		}

		// 앉는 중에는 Hurt/앉기 해제를 할 수 있다
		// Hurt는 Update가 아닌 TriggerEnter2D 에서 정의
		else if (isCrouching)
		{
			if (crouchPressed) return ;
			// 앉기 버튼 뗌
			else if (!crouchPressed)
			{
				isCrouching = false;
				animator.SetBool("IsCrouching", false);
				SetCrouchCollider(false);
			}
		}

		CheckGrounded();

		// 
		animator.SetFloat("Speed", Mathf.Abs(moveInput.x));

		// 
		animator.SetBool("IsFalling", !isGrounded && rb.linearVelocity.y < 0f);
		animator.SetBool("IsJumping", !isGrounded && rb.linearVelocity.y > 0f);

		// 앉기 버튼 눌림
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
		// Hurt
		if (hit.gameObject.layer == LayerMask.NameToLayer("Obstacle") && !isHurting)
		{
			isHurting = true; // player.TakeDamage();

			// 밀려나가는 효과
			Vector2 direction = transform.position - hit.transform.position;
			rb.linearVelocity = Vector2.zero;
			rb.AddForce(direction.normalized * damageForce, ForceMode2D.Impulse);

			if (!isAttacking && !isDashing)
			{
				// 애니메이션 사용할 건지
				animator.SetTrigger("Hurt");
			}

			// 만약 죽으면
			float rand = Random.Range(0f, 1f); // 디버그용 변수값

			if (rand > 0.8f)
			{
				isDead = true; 
				animator.SetTrigger("Death"); // player.Death();
			}
		}
	}

	private void OnTriggerStay2D(Collider2D ladder)
	{
		// 사다리 진입 조건: ladder에 돌입 && 방향키 위 버튼 입력
		if (!isOnLadder && ladder.gameObject.layer == LayerMask.NameToLayer("Ladder"))
		{
			if (moveInput.y > 0)
			{
				Tilemap tilemap = ladder.GetComponent<Tilemap>();
				
				var dist = standingCollider.Distance(ladder);

				// 사다리 위치에 고정시키기 위한 것들
				if (!dist.isOverlapped)
					return ;
				Vector2 contact = (dist.pointA + dist.pointB) * 0.5f; 
				Vector3Int tilePos = tilemap.WorldToCell(contact); // Position => Grid
				if (tilemap.HasTile(tilePos) == false)
					return ;

				Vector3 ladderPos = tilemap.GetCellCenterWorld(tilePos);

				isOnLadder = true;
				rb.linearVelocity = Vector2.zero; 
				transform.position = new Vector3(ladderPos.x, transform.position.y, 0); 
				animator.SetBool("IsOnLadder", true); // 애니메이션
			}
		}
	}

	private void OnTriggerExit2D(Collider2D ladder)
	{
		// 사다리 탈출 조건 1
		if (isOnLadder)
		{
			isOnLadder = false;

			// 아래 두개는 사다리 탈출 시 반드시
			animator.speed = 1f; 
			rb.gravityScale = 1;

			animator.SetBool("IsOnLadder", false);
			// animator.SetTrigger("UpToFall"); // => uptofall => [idle/run/jump/fall]
		}
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
