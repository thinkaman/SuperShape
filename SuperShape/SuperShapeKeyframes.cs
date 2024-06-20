using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SuperShapeKeyframe
{
	public int layerCount;
	public Vector2[] baseVerts;
	public Vector2 slice;
	public Vector2 quadSkew;
	public Vector2[] wiggleZones;
	public Color[] colors;
	public float alpha;

	public SuperShapeKeyframe()
	{
		layerCount = 1;
		baseVerts = new Vector2[3];
		slice = Vector2.zero;
		quadSkew = Vector2.zero;
		wiggleZones = new Vector2[3];
		colors = new Color[1];
		alpha = 1;
	}

	public SuperShapeKeyframe(int layerCount,
	                          Vector2[] baseVerts,
	                          Vector2 slice,
	                          Vector2 quadSkew,
	                          Vector2[] wiggleZones,
	                          Color[] colors,
	                          float alpha)
	{
		this.layerCount = layerCount;
		this.baseVerts = baseVerts;
		this.slice = slice;
		this.quadSkew = quadSkew;
		this.wiggleZones = wiggleZones;
		this.colors = colors;
		this.alpha = alpha;
	}
}
public class SuperShapeKeyframes : MonoBehaviour
{
	public List<SuperShapeKeyframe> keyframes = new List<SuperShapeKeyframe>();
	public bool isAutoLoadFirstKeyframe;

	void Awake()
	{
		if (keyframes.Count > 0 && isAutoLoadFirstKeyframe)
		{
			LoadLocalState0();
		}
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

	public void SaveLocalState(int index)
	{
		SuperShape shape = GetComponent<SuperShape>();
		int layerCount = shape.layerCount;
		Vector2[] myBaseVerts = new Vector2[shape.baseVerts.Length];
		shape.baseVerts.CopyTo(myBaseVerts, 0);
		Vector2[] myWiggleZones = new Vector2[shape.wiggleZones.Length];
		shape.wiggleZones.CopyTo(myWiggleZones, 0);
		Color[] myColors = new Color[shape.layerColors.Length];
		shape.layerColors.CopyTo(myColors, 0);
		SuperShapeKeyframe entry = new SuperShapeKeyframe(layerCount, myBaseVerts, shape.slice, shape.quadSkew,
		                                                  myWiggleZones, myColors, shape.alpha);
		if (keyframes.Count > index)
		{
			keyframes[index] = entry;
		}
		else
		{
			for (int i = keyframes.Count; i < index - 1; i++)
			{
				keyframes.Add(new SuperShapeKeyframe());
			}
			keyframes.Add(entry);
		}
	}

	public void LoadLocalState(int index)
	{
		if (index >= keyframes.Count) { return; }
		SuperShapeKeyframe kf = keyframes[index];
		if (kf.layerCount <= 0) { Debug.LogWarning("Invalid SuperShape Keyframe: " + index); return; }

		SuperShape shape = GetComponent<SuperShape>();
		shape.CancelAllCoroutines();
		shape.layerCount = kf.layerCount;
		shape.vertexCount = kf.baseVerts.Length / kf.layerCount;
		shape.ResizeData();

		kf.baseVerts.CopyTo(shape.baseVerts, 0);
		kf.baseVerts.CopyTo(shape.prevVerts, 0);
		kf.baseVerts.CopyTo(shape.nextVerts, 0);
		shape.slice = kf.slice;
		shape.quadSkew = kf.quadSkew;
		kf.wiggleZones.CopyTo(shape.wiggleZones, 0);
		kf.colors.CopyTo(shape.layerColors, 0);
		shape.alpha = kf.alpha;
		shape.UpdateMesh();
	}
}
