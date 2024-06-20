using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Mask))]
public class ProxyMaskCanvasMesh : MaskableGraphic, IProxyMaskMesh
{
	public RectTransform fatherRectTransform;
	SuperShape _fatherShape;
	SuperShape fatherShape { get { if (_fatherShape == null) _fatherShape = fatherRectTransform.GetComponent<SuperShape>(); return _fatherShape; } }

	private bool _isMatchingParentPosition; //update these in Update for easy inspector support
	private bool _isMatchingParentRotation;
	private bool _isMatchingParentScale;
	public bool isMatchingParentPosition;
	public bool isMatchingParentRotation;
	public bool isMatchingParentScale;
	public bool isProtruding = true;
	public Vector2 direction = new Vector2(0, 600.0f);
	public float spread = 0;
	private Vector2 effectiveDirection
	{
		get
		{
			if (isMatchingParentRotation) return direction * transform.localScale;
			return RotateVectorByAngle(direction * transform.localScale, -transform.rotation.eulerAngles.z);
		}
	}
	private static Vector2 RotateVectorByAngle(Vector2 direction, float theta)
	{
		float sin = Mathf.Sin(theta * Mathf.Deg2Rad);
		float cos = Mathf.Cos(theta * Mathf.Deg2Rad);
		return new Vector2(cos * direction.x - sin * direction.y, sin * direction.x + cos * direction.y);
	}

	public List<Vector2> baseVertsLeft;
	public Vector2 centroidLeft;
	public List<Vector2> baseVertsRight;
	public Vector2 centroid;

	public void AssignVerts(List<Vector2> baseVerts)
	{
		baseVertsLeft.Clear();
		centroidLeft = Vector2.zero;
		foreach (Vector2 vert in baseVerts)
		{
			Vector2 localVert = transform.InverseTransformPoint(vert);
			centroidLeft += localVert;
			baseVertsLeft.Add(localVert);
		}
		centroidLeft /= baseVerts.Count;
	}

	private static List<Vector2> myVerts = new List<Vector2>();
	private void GetProtrudingVerts(List<Vector2> candidates)
	{
		Vector2 ed = effectiveDirection;
		Vector2 orth = new Vector2(ed.y, -ed.x);
		//Debug.Log("ed: " + ed + " orth: " + orth);
		float highestDot = float.MinValue;
		int highestIndex = -1;
		float lowestDot = float.MaxValue;
		int lowestIndex = -1;

		for (int i = 0; i < candidates.Count; i++)
		{
			float dot = Vector2.Dot(candidates[i] - centroid, orth);
			if (dot > highestDot)
			{
				highestDot = dot;
				highestIndex = i;
			}
			if (dot < lowestDot)
			{
				lowestDot = dot;
				lowestIndex = i;
			}
		}
		//Debug.Log(lowestIndex + " " + highestIndex);

		bool isLooped = false;
		myVerts.Add(candidates[highestIndex] + ed + orth * spread);
		for (int i = highestIndex; !isLooped || i < highestIndex; i++)
		{
			if (i >= candidates.Count) { i = 0; isLooped = true; }
			myVerts.Add(candidates[i]);
			if (i == lowestIndex) { break; }
		}
		myVerts.Add(candidates[lowestIndex] + ed - orth * spread);
	}

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		if (baseVertsLeft == null || baseVertsLeft.Count == 0) { return; }

		// Clear vertex helper to reset vertices, indices etc.
		vh.Clear();
		myVerts.Clear();

		if (!isProtruding)
		{
			for (int i = 0; i < baseVertsLeft.Count; i++)
			{
				myVerts.Add(baseVertsLeft[i]);
			}
		}
		else
		{
			GetProtrudingVerts(baseVertsLeft);
		}

		string s = "";
		for (int i = 0; i < myVerts.Count; i++)
		{
			s += myVerts[i] + " ";
		}
		//Debug.Log(s + ":    (" + myVerts.Count + ")");
		
		TriangulationUtility.TriangulateFull(myVerts.ToArray(), color, Vector2.zero, Vector2.zero, null, vh); //UVs don't matter
	}

	public void MatchParent(RectTransform parent)
	{
		if (isMatchingParentPosition) { transform.localPosition = parent.localPosition; }
		if (isMatchingParentRotation) { transform.localRotation = parent.localRotation; rectTransform.sizeDelta = parent.sizeDelta;  rectTransform.pivot = parent.pivot; }
		if (isMatchingParentScale) { transform.localScale = parent.localScale; }
	}

	[ContextMenu("SetPositionMatchingFalse")] public void SetPositionMatchingFalse() { SetPositionMatching(false); }
	[ContextMenu("SetPositionMatchingTrue")] public void SetPositionMatchingTrue() { SetPositionMatching(true); }
	public void SetPositionMatching(bool isMatching)
	{
		isMatchingParentPosition = isMatching;
		if (isMatching)
		{
			
		}
	}

	
	public void LateUpdate()
	{
		if (fatherRectTransform == null) { return; }

		if (_isMatchingParentScale != isMatchingParentScale)
		{
			_isMatchingParentScale = isMatchingParentScale;
			if (_isMatchingParentScale)
			{
				foreach (Transform child in transform)
				{
					child.localScale = new Vector3(transform.localScale.x / fatherRectTransform.localScale.x * child.localScale.x,
					                               transform.localScale.y / fatherRectTransform.localScale.y * child.localScale.y,
					                               transform.localScale.z / fatherRectTransform.localScale.z * child.localScale.z);
				}
			}
			fatherShape.SetMeshDirty();
		}
		if (_isMatchingParentRotation != isMatchingParentRotation)
		{
			_isMatchingParentRotation = isMatchingParentRotation;
			if (_isMatchingParentRotation)
			{
				foreach (Transform child in transform)
				{
					Vector3 euler = transform.localRotation.eulerAngles;
					euler.z -= fatherRectTransform.localRotation.eulerAngles.z - transform.localRotation.eulerAngles.z;
				}
			}
			fatherShape.SetMeshDirty();
		}
		if (_isMatchingParentPosition != isMatchingParentPosition)
		{
			_isMatchingParentPosition = isMatchingParentPosition;
			if (_isMatchingParentPosition)
			{
				foreach (Transform child in transform)
				{
					child.localPosition -= new Vector3((fatherRectTransform.localPosition.x - transform.localPosition.x) * transform.localScale.x,
					                                   (fatherRectTransform.localPosition.y - transform.localPosition.y) * transform.localScale.y,
					                                   (fatherRectTransform.localPosition.z - transform.localPosition.z) * transform.localScale.z);
				}
			}
			fatherShape.SetMeshDirty();
		}
		if (isMatchingParentPosition) { transform.localPosition = fatherRectTransform.localPosition; }
		if (isMatchingParentRotation) { transform.localRotation = fatherRectTransform.localRotation; rectTransform.sizeDelta = fatherRectTransform.sizeDelta; rectTransform.pivot = fatherRectTransform.pivot; }
		if (isMatchingParentScale) { transform.localScale = fatherRectTransform.localScale; }
	}
	public bool IsDemandingUpdateAlways()
	{
		return isMatchingParentPosition || isMatchingParentRotation || isMatchingParentScale;
	}
	public GameObject GetGameObject()
	{
		return gameObject;
	}
	public void SetIsMatchingParentPosition(bool b)
	{
		isMatchingParentPosition = b;
	}
	public void SetIsMatchingParentRotation(bool b)
	{
		isMatchingParentRotation = b;
	}
}
