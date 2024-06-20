using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CanvasLockOn : DynamicMonoBehaviour
{
	public DynamicMonoBehaviour target;
	public Transform a;
	
	void LateUpdate()
	{
		if (target == null) { return; }
		rectTransform.pivot = new Vector2(target.localPosition.x / width + 0.5f, target.localPosition.y / height + 0.5f);
		localPosition = Vector3.zero;
	}
}
