using UnityEngine;

public class CameraManager : MonoBehaviour
{
	public float maxY = 4f;
	public float minY = -3f;
	public Transform player;
	public Vector3 offset;
	public float smoothSpeed = 5f;

	private float maxCameraY;
	private float minCameraY;

	void Start()
	{
		float camHeight = Camera.main.orthographicSize * 2f;
		float halfHeight = camHeight / 2f;

		maxCameraY = maxY - halfHeight;
		minCameraY = minY + halfHeight;
	}

	void Update()
	{
		if (player == null)
		{
			player = GameObject.FindWithTag("Player")?.transform;
		}
	}

	void LateUpdate()
	{
		if (player == null) return ;

		Vector3 targetPosition = player.position + offset;
		Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
		float clampedY = Mathf.Clamp(smoothedPosition.y, minCameraY, maxCameraY);

		transform.position = new Vector3(smoothedPosition.x, clampedY, transform.position.z);
	}
}
