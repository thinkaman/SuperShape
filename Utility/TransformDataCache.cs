using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class TransformDataCacheEntry
{
	public Vector3 localPosition;
	public Vector3 localRotationEuler;
	public Vector3 localScale;
	public float alpha;

	public TransformDataCacheEntry(Vector3 localPosition,
	                               Vector3 localRotationEuler,
	                               Vector3 localScale,
	                               float alpha = 1.0f)
	{
		this.localPosition = localPosition;
		this.localRotationEuler = localRotationEuler;
		this.localScale = localScale;
		this.alpha = alpha;
	}
}

public class TransformDataCache : MonoBehaviour
{
	public List<TransformDataCacheEntry> data;

	void Awake()
	{
		LoadLocalState0();
	}

	[ContextMenu("Save Local Position 0")]
	public void SaveLocalState0() { SaveLocalState(0); }
	[ContextMenu("Save Local Position 1")]
	public void SaveLocalState1() { SaveLocalState(1); }
	[ContextMenu("Save Local Position 2")]
	public void SaveLocalState2() { SaveLocalState(2); }
	[ContextMenu("Save Local Position 3")]
	public void SaveLocalState3() { SaveLocalState(3); }
	[ContextMenu("Save Local Position 4")]
	public void SaveLocalState4() { SaveLocalState(4); }

	[ContextMenu("Load Local Position 0")]
	public void LoadLocalState0() { LoadLocalState(0); }
	[ContextMenu("Load Local Position 1")]
	public void LoadLocalState1() { LoadLocalState(1); }
	[ContextMenu("Load Local Position 2")]
	public void LoadLocalState2() { LoadLocalState(2); }
	[ContextMenu("Load Local Position 3")]
	public void LoadLocalState3() { LoadLocalState(3); }
	[ContextMenu("Load Local Position 4")]
	public void LoadLocalState4() { LoadLocalState(4); }

	[ContextMenu("Debug Print Positions")]
	public void DebugPrintPositions()
	{
		DynamicMonoBehaviour dmb = GetComponent<DynamicMonoBehaviour>();
		Debug.Log("Local Position: " + transform.localPosition.ToString());
		Debug.Log("Anchored Position: " + dmb.anchoredPosition.ToString());
		Debug.Log("Normalized Position: " + dmb.normalizedPosition.ToString());
		Debug.Log("Absolute Position: " + transform.position.ToString());
	}

	public void SaveLocalState(int index)
	{
		DynamicMonoBehaviour dmb = GetComponent<DynamicMonoBehaviour>();
		TransformDataCacheEntry entry = new TransformDataCacheEntry(dmb.anchoredPosition,
		                                                            transform.localRotation.eulerAngles,
		                                                            transform.localScale);
		if (data.Count > index)
		{
			data[index] = entry;
		}
		else
		{
			for (int i = data.Count; i < index - 1; i++)
			{
				data.Add(new TransformDataCacheEntry(Vector3.zero, Vector3.zero, Vector3.one));
			}
			data.Add(entry);
		}
	}

	public void LoadLocalState(int index)
	{
		if (index >= data.Count) { return; }
		DynamicMonoBehaviour dmb = GetComponent<DynamicMonoBehaviour>();
		dmb.CancelAllCoroutines();
		dmb.effectiveAnchoredPosition = data[index].localPosition;
		if (data[index].localPosition.z != 0) { dmb.SetZ(data[index].localPosition.z); }
		transform.localRotation = Quaternion.Euler(data[index].localRotationEuler);
		transform.localScale = data[index].localScale;
		dmb.alpha = data[index].alpha;
	}
}
