using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour 
{
	public Animator animator;
	public Rigidbody2D rb;

	[Header("Stats")]
	public int hp = 4;

	[Header("Move")]
	public float moveSpeed = 1f;
	public float chasingTime = 6f;
	public float maxChangeTime = 6f;
	public float damagedForce = 2f;
	public float freezeTime = 0.5f;
	public bool isAir = false;

	[Header("Raycast")]
	public float raycastDist = 0.2f;

	[Header("Layer")]
	public LayerMask targetLayer;
	public LayerMask groundLayer;

	[Header("SerializeField")]
	[SerializeField] private bool isEdge = false;
	[SerializeField] private bool isChasing = false;
	[SerializeField] private bool isDead = false;
	[SerializeField] private bool isFreezed = false;

	[SerializeField] private Transform player;
	[SerializeField] private Vector2 direction;
	[SerializeField] private Vector2 move = Vector2.zero;

	private Coroutine randomCoroutine;
	private Coroutine chaseCoroutine;
	private Coroutine freezeCoroutine;

	private void Update()
	{
		if (!isChasing && randomCoroutine == null)
			randomCoroutine = StartCoroutine(SetRandomDirection());
		else if (isChasing && chaseCoroutine == null)
			chaseCoroutine = StartCoroutine(SetDirectionToPlayer());

		move = isEdge ? Vector2.zero : direction;
		animator.SetFloat("Speed", Mathf.Abs(move.x));
	}

	private void FixedUpdate()
	{
		isEdge = IsOnEdge();

		if (isFreezed && isEdge)
			rb.AddForce(rb.linearVelocity * -damagedForce, ForceMode2D.Impulse); // 얍삽이 방지
		else if (!isFreezed)
		{
			Move();
			Flip();
		}
	}

	private bool IsOnEdge()
	{
		Vector2 rayDirection;

		// isFreezed == false: 움직일 수 있음 => 의도된 방향으로
		// isFreezed == true: 움직일 수 없음 => 지가 움직이는 방향으로
		if (!isFreezed)
			rayDirection = Vector2.down + direction;
		else
			rayDirection = Vector2.down + rb.linearVelocity;
		return (!Physics2D.Raycast(transform.position, rayDirection, raycastDist, groundLayer));
		// Debug.DrawRay(transform.position, rayDirection.normalized * raycastDist, Color.red);
	}

	private void OnTriggerStay2D(Collider2D hit)
	{
		if (isChasing == false)
		{
			if (hit.gameObject.layer == LayerMask.NameToLayer("Player") && hit is CapsuleCollider2D)
			{
				player = hit.transform;
				StartCoroutine(EnableChasing());
			}
		}
	}

	public void TakeDamage(int damage)
	{
		if (!isDead)
		{
			// 밀려남
			hp -= damage;
			rb.linearVelocity = Vector2.zero;
			rb.AddForce(-move * damagedForce, ForceMode2D.Impulse); // 플레이어 방향을 항상 보니까 move는

			// Freeze() 
			if (freezeCoroutine != null)
				StopCoroutine(freezeCoroutine);
			freezeCoroutine = StartCoroutine(Freeze());

			// 사망 시 
			if (hp < 1)
			{
				isDead = true;
				animator.SetTrigger("Dead");
			}
		}
	}

	private IEnumerator Freeze()
	{
		isFreezed = true;
		animator.SetTrigger("Damaged");
		yield return new WaitForSeconds(freezeTime);
		isFreezed = false;
	}


	private IEnumerator EnableChasing()
	{
		isChasing = true;
		yield return new WaitForSeconds(chasingTime);
		isChasing = false;
	}

	private IEnumerator SetRandomDirection()
	{
		float rand = Random.Range(0, 1f);
		int x = rand > 0.5f ? 1 : -1;
		int y = 0;

		direction.x = x;
		direction.y = y;

		float changeTime = maxChangeTime * Random.Range(0, 1f);
		yield return new WaitForSeconds(changeTime);
		randomCoroutine = null;
	}

	private IEnumerator SetDirectionToPlayer()
	{
		direction = player.position - transform.position;
		direction.y = 0;
		direction = direction.normalized;
		yield return new WaitForSeconds(0.5f);
		chaseCoroutine = null;
	}

	private void Move()
	{
		Vector2 velocity = rb.linearVelocity;
		velocity.x = move.x * moveSpeed;
		rb.linearVelocity = velocity;
	}

	private void Flip()
	{
		if (move.x > 0)
			transform.localScale = Vector3.one;
		else if (move.x < 0)
			transform.localScale = new Vector3(-1, 1, 1);
	}

	private void OnDeadEnd()
	{
		isFreezed = false;
		gameObject.SetActive(false);
	}
}
