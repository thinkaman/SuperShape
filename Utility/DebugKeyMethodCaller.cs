using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DebugKeyMethodCallerEntry
{
	public KeyCode key;
	public string methodName;
	public bool isResetOnPlay;
}

public class DebugKeyMethodCaller : DynamicMonoBehaviour
{
	public MonoBehaviour targetComponent;
	public List<DebugKeyMethodCallerEntry> entryList;
	public List<Vector3> testVectors;
	public List<Lerp> testLerps;
	public List<float> testDurations;
	public List<float> testDelays;

	void Update ()
	{
		if (!Application.isEditor) { return; }
		foreach (DebugKeyMethodCallerEntry entry in entryList)
		{
			if (Input.GetKeyDown(entry.key))
			{
				if (entry.isResetOnPlay) { transformData.LoadLocalState0(); }
				targetComponent.Invoke(entry.methodName, 0);
			}
		}
	}
}
