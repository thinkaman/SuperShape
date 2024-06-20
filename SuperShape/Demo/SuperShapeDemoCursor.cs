using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperShapeDemoCursor : DynamicMonoBehaviour
{
	const float rotateSpeed = 360; //degrees per second

	public SuperShape targetShape;
	public int targetLayerIndex;
	public int targetVertIndex;

	public void SetTarget(SuperShape targetShape, int targetLayerIndex, int targetVertIndex)
	{
		this.targetShape = targetShape;
		this.targetLayerIndex = targetLayerIndex;
		this.targetVertIndex = targetVertIndex;
	}

	private void Update()
	{
		localRotation = Quaternion.Euler(new Vector3(0, 0, TheGameTime.time * rotateSpeed));

		if (targetShape != null)
		{
			transform.position = targetShape.GetWorldPosOfVert(targetLayerIndex, targetVertIndex);
		}
	}
}
