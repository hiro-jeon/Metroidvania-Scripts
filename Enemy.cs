using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour 
{
	public Animator animator;
	public Rigidbody2D rb;

	[Header("Stats")]
	public int hp = 1;

	[Header("Move")]
	public float moveSpeed = 1f;
	public float chasingTime = 6f;
	public float maxChangeTime = 6f;
	public float damagedForce = 3f;
	public bool isAir = false;

	[Header("Layer")]
	public LayerMask targetLayer;
	public LayerMask groundLayer;

	private bool isEdge = false;
	private bool isChasing = false;
	private bool isHurting = false;

	private Transform player;
	private Vector2 direction;
	private Vector2 move = Vector2.zero;

	private Coroutine randomCoroutine;
	private Coroutine chaseCoroutine;

	private void Update()
	{
		if (!isChasing && randomCoroutine == null)
			randomCoroutine = StartCoroutine(SetRandomDirection());
		else if (isChasing && chaseCoroutine == null)
			chaseCoroutine = StartCoroutine(SetDirectionToPlayer());
		move = !isEdge ? direction : Vector2.zero; // 모서리일 경우 속도 0

		animator.SetFloat("Speed", Mathf.Abs(move.x));
	}

	private void FixedUpdate()
	{
		isEdge = !Physics2D.Raycast(transform.position, Vector2.down + move, 0.1f, groundLayer);

		Move();
		Flip();
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
		if (!isHurting)
		{
			hp -= damage;
			// rb.linearVelocity = Vector2.zero;
			rb.AddForce(-move * damagedForce, ForceMode2D.Impulse);

			isHurting = true;
			animator.SetTrigger("Damaged");
			if (hp < 0)
				animator.SetTrigger("Dead");
		}
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

	private void OnDamageEnd()
	{
		isHurting = false;
	}

	private void OnDeadEnd()
	{
		isHurting = false;
		gameObject.SetActive(false);
	}
}
