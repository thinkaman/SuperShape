using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleRotationScript : MonoBehaviour
{
	public float speed = 1.0f;

	private Vector2 initialScale;
	private float initialZ;

	public static bool isGlobalEnabled = true;

	private void Awake()
	{
		isGlobalEnabled = true;
		initialScale = transform.localScale;
		initialZ = transform.localRotation.eulerAngles.z * Mathf.Deg2Rad;
	}

	private void Update()
	{
		if (!isGlobalEnabled) return;
		transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x,
		                                           transform.localRotation.eulerAngles.y,
		                                           transform.localRotation.eulerAngles.z + TheGameTime.deltaTime * 360 * speed);
		float deltaZ = transform.localRotation.eulerAngles.z * Mathf.Deg2Rad- initialZ;
		float t = Mathf.Abs(Mathf.Cos(deltaZ));
		transform.localScale = new Vector3(initialScale.x * t + initialScale.y * (1-t), initialScale.y * t + initialScale.x * (1 - t), 1);
	}
}
