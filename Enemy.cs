using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour 
{
	public Animator animator;
	public Rigidbody2D rb;

	public float moveSpeed = 1f;
	public float chasingTime = 6f;
	public float maxChangeTime = 6f;
	public float damagedForce = 3f;
	public bool isAir = false;

	public LayerMask targetLayer;
	public LayerMask groundLayer;

	private bool isChasing = false;

	private Transform player;
	private Vector2 direction;
	private Vector2 move = Vector2.zero;

	private bool isEdge = false;

	private Coroutine randomCoroutine;
	private Coroutine chaseCoroutine;

	private void Update()
	{
		if (!isChasing && randomCoroutine == null)
			randomCoroutine = StartCoroutine(SetRandomDirection());
		else if (isChasing && chaseCoroutine == null)
			chaseCoroutine = StartCoroutine(SetDirectionToPlayer());

		// 끝일 경우 속도 0
		move = !isEdge ? direction : Vector2.zero;

		animator.SetFloat("Speed", Mathf.Abs(move.x));
	}

	private void FixedUpdate()
	{
		isEdge = !Physics2D.Raycast(transform.position, Vector2.down + direction, 0.1f, groundLayer);

		Move(move);

		if (move.x > 0)
			transform.localScale = Vector3.one;
		else if (move.x < 0)
			transform.localScale = new Vector3(-1, 1, 1);
	}

	private void OnTriggerStay2D(Collider2D hit)
	{
		if (isChasing == false)
		{
			if (hit.gameObject.layer == LayerMask.NameToLayer("Player") && hit is CapsuleCollider2D)
			{
				Debug.Log("플레이어 확인");
				player = hit.transform;
				StartCoroutine(ChasePlayer());
			}
		}
	}

	private void OnDamaged()
	{
		rb.AddForce(-direction * damagedForce, ForceMode2D.Impulse);
		animator.SetTrigger("Hurt");
	}

	private IEnumerator ChasePlayer()
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

	private void Move(Vector2 direction)
	{
		Vector2 velocity = rb.linearVelocity;
		velocity.x = direction.x * moveSpeed;
		rb.linearVelocity = velocity;
	}
}
