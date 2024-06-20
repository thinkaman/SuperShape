using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MyVertexHelper
{
	const string genMeshName = "Generated Mesh";
	static Vector3[] verts = new Vector3[400];
	static int[] tris = new int[600];
	static Color[] colors = new Color[400];
	static Vector2[] UVs = new Vector2[400];
	static Vector3[] normals = new Vector3[400];

	static int vertsCount;
	static int trisCount;


	public static void AddSet(Vector2[] vertsList, List<Tri> triList, Color color, Vector2 UVmin, Vector2 UVmax, Vector3 normal)
	{
		if (vertsList == null) { return; }
		int x = vertsCount;
		for (int i = 0; i < vertsList.Length; i++)
		{
			verts[x + i] = vertsList[i];
			colors[x + i] = color;
			UVs[x + i] = UVmin == UVmax ? Vector2.zero : (vertsList[i] - UVmin) / (UVmax - UVmin);
			normals[x + i] = normal;
			vertsCount++;
		}
		if (triList != null)
		{
			int y = trisCount;
			for (int i = 0; i < triList.Count; i++)
			{
				tris[y + 3*i    ] = x + triList[i].a;
				tris[y + 3*i + 1] = x + triList[i].b;
				tris[y + 3*i + 2] = x + triList[i].c;
				trisCount += 3;
			}
		}
	}
	public static void AddSet(Vector2[] vertsList, List<Tri> triList)
	{
		if (vertsList == null) { return; }
		int x = vertsCount;
		for (int i = 0; i < vertsList.Length; i++)
		{
			verts[x + i] = vertsList[i];
			normals[x + i] = -Vector3.forward;
			vertsCount++;
		}
		if (triList != null)
		{
			int y = trisCount;
			for (int i = 0; i < triList.Count; i++)
			{
				tris[y + 3 * i] = x + triList[i].a;
				tris[y + 3 * i + 1] = x + triList[i].b;
				tris[y + 3 * i + 2] = x + triList[i].c;
				trisCount += 3;
			}
		}
	}

	public static void AssignToMeshFilter(MeshFilter mf, bool isPartial = false)
	{
		Mesh mesh;
		if (mf.sharedMesh != null)
		{
			mf.sharedMesh.Clear();
			mesh = mf.sharedMesh;
		}
		else
		{
			mesh = new Mesh();
			mesh.MarkDynamic();
		}
		mesh.name = genMeshName;
		mesh.SetVertices(verts, 0, vertsCount);
		mesh.SetTriangles(tris, 0, trisCount, 0);
		mesh.SetNormals(normals, 0, vertsCount);
		if (!isPartial)
		{
			mesh.SetColors(colors, 0, vertsCount);
			mesh.SetUVs(0, UVs, 0, vertsCount);
		}
		mf.sharedMesh = mesh;
	}

	public static void Clear()
	{
		vertsCount = 0;
		trisCount = 0;
	}
}

public interface ISuperShapeMesh
{
	bool IsDemandingAlwaysUpdate();
	void UpdateCounts(int layerCount, int vertexCount);
	void CalculateVerts();
	Vector2 GetBridgePivot(Vector2 sd);
	void ComputeBridgeLayer(int layerIndex, bool isRecursive);
	Vector2 GetPoint(int layerIndex, int sideIndex, float progress = 0, bool isBackwards = false);
	void AssignVert(int i, Vector2 v, Vector2 prev, Vector2 next);
	void SetDirty();
	void SetMaterial(Material m);
	void SetIsLimitingOverdraw(bool value);


	//TEMP
	Object GetObject();
	IProxyMaskMesh GetProxyMaskMesh();
}

public class SuperShapeMesh : MonoBehaviour, ISuperShapeMesh
{
	const float SQRT_2 = 1.41421356237f;
	const float INV_SQRT_2 = 0.70710678118f;

	const int DEFAULT_LAYER_COUNT = 4;
	const int DEFAULT_VERT_COUNT = 4;
	const float TAIL_PROGRESS_CUTOFF = 0.8f;

	private SuperShape _shape;
	private SuperShape  shape { get { return _shape; } }
	[System.NonSerialized]
	private bool _shapeFillBarTest = false;
	private SuperShapeFillBar _shapeFillBar;
	private MeshRenderer _meshRenderer;
	private MeshRenderer meshRenderer { get { if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>(); return _meshRenderer; } }
	private MeshFilter _meshFilter;
	private MeshFilter meshFilter { get { if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>(); return _meshFilter; } }
	private Mesh myMesh { get { return meshFilter.sharedMesh; } set { meshFilter.sharedMesh = value; } }

	public PolygonCollider2D myMainCollider;
	public PolygonCollider2D myTailCollider;

	public bool isCached = true;
	public bool isLimitingOverdrawing = true;
	public void SetIsLimitingOverdraw(bool value) { isLimitingOverdrawing = value; } 
	List<Tri> triCache = null;
	List<Tri> tailTriCache = null;

	//[HideInInspector]
	public int layerCount = DEFAULT_LAYER_COUNT;
	//[HideInInspector]
	public int vertCount = DEFAULT_VERT_COUNT;
	
	float sliceWidth; //cache
	float sliceHeight; //cache

	[HideInInspector]
	public Vector2[] verts = new Vector2[DEFAULT_LAYER_COUNT * DEFAULT_VERT_COUNT];
	[HideInInspector]
	public Vector2[] prevVerts = new Vector2[DEFAULT_LAYER_COUNT * DEFAULT_VERT_COUNT];
	[HideInInspector]
	public Vector2[] nextVerts = new Vector2[DEFAULT_LAYER_COUNT * DEFAULT_VERT_COUNT];
	public List<int> ignoreVerts = new List<int>();
	public float ignoreRatio;
	[HideInInspector]
	public int colliderLayer = 0;

	[HideInInspector] public int tailVertCount;
	[HideInInspector] public float tailFixedBaseUnitProgress; //only for fixed, used to calculate pos

	private bool isRenderingGutter = false;

	[HideInInspector] public bool isCycleMode = false;
	[HideInInspector] public float cycleTime = 0;
	[HideInInspector] public float cyclePeriod = 2.0f;
	[HideInInspector] public float cycleAmplitude = 400f;

	public ProxyMaskMesh _proxyMaskMesh;
	public IProxyMaskMesh proxyMaskMesh { get { return _proxyMaskMesh; } }

	public bool IsDemandingAlwaysUpdate() { return proxyMaskMesh != null && proxyMaskMesh.IsDemandingUpdateAlways(); }

	void Start()
	{
		if (_shape == null) _shape = GetComponent<SuperShape>();

		prevVerts = new Vector2[verts.Length];
		nextVerts = new Vector2[verts.Length];
		for (int i = 0; i < verts.Length; i++)
		{
			prevVerts[i] = verts[i];
			nextVerts[i] = verts[i];
		}
	}
	float lastCalcTime;	

	[ContextMenu("DebugVertPopulate")]
	public void DebugPopulateVerts()
	{
		vertCount = 5;
		verts = new Vector2[layerCount * vertCount];
		Vector2 b = Vector2.zero; // transform.position;
		for (int i = 0; i < layerCount; i++)
		{
			Vector2 r = Vector2.one * (0.5f + 0.12f * i) / 2;
			verts[i* vertCount + 0] = b + new Vector2(-r.x, -r.y);
			verts[i* vertCount + 1] = b + new Vector2(-r.x,  r.y);
			verts[i* vertCount + 2] = b + new Vector2( r.x,  r.y);
			verts[i* vertCount + 3] = b + new Vector2( r.x, -r.y);
			verts[i* vertCount + 4] = b + new Vector2(0, -r.y / 3);
		}

		Start();
	}

	private float GetCycleMagnitude(int layerIndex)
	{
		float t = shape.time / cyclePeriod + (layerIndex - 1.0f) / (layerCount - 1.0f);
		if (t <= 0) { return 0; }
		return cycleAmplitude * Mathf.Sin(t * Mathf.PI * 0.55f);
	}

	private Vector2 ConvertProxyVert(Vector2 vert)
	{
		return transform.TransformPoint(vert);
	}

	Vector2[] myVerts;
	Vector2[] myPrevVerts; //used as cache for border triangulation only
	static Vector2[] myTailVerts;
	Vector2[] myBridgeVerts;
	static Vector2[] myGutterVerts = new Vector2[4];
	static Vector2[] myGutter2Verts = new Vector2[4];
	static Color myGutterColor;
	static List<Vector2> myColliderVerts = new List<Vector2>();
	static List<Vector2> myProxyVerts = new List<Vector2>(); //grab the extra edges if flattened

	public void CalculateVerts()
	{
		if (lastCalcTime == Time.time && myVerts != null) { return; }
		for (int i = 0; i < layerCount; i++)
		{
			ComputeLayer(i);
		}
		for (int i = layerCount - 1; i > -1; i--)
		{
			ComputeLayerRotation(i);
		}
		if (ignoreVerts != null && ignoreVerts.Count > 0 && ignoreRatio > 0)
		{
			for (int i = 0; i < layerCount; i++)
			{
				FilterIgnoredVerts(i);
			}
		}
		lastCalcTime = Time.time;
	}
	void ComputeLayer(int index)
	{
		if (verts == null || verts.Length != layerCount * vertCount) { return; }
		if (prevVerts == null || prevVerts.Length != layerCount * vertCount) { Start(); }

		System.Array.Resize(ref myVerts, vertCount);

		for (int i = 0; i < vertCount; i++)
		{
			int j = index * vertCount + i;
			Vector2 prevVert = prevVerts[j];
			Vector2 nextVert = nextVerts[j];
			if      (prevVert.x > 0) { prevVert.x +=  sliceWidth / 2; }
			else if (prevVert.x < 0) { prevVert.x -=  sliceWidth / 2; }
			if      (prevVert.y > 0) { prevVert.y += sliceHeight / 2; }
			else if (prevVert.y < 0) { prevVert.y -= sliceHeight / 2; }
			if      (nextVert.x > 0) { nextVert.x +=  sliceWidth / 2; }
			else if (nextVert.x < 0) { nextVert.x -=  sliceWidth / 2; }
			if      (nextVert.y > 0) { nextVert.y += sliceHeight / 2; }
			else if (nextVert.y < 0) { nextVert.y -= sliceHeight / 2; }
			float progress = shape.wiggleDuration[j] == 0 ? 0 : Mathf.Clamp01(shape.wiggleProgress[j] / shape.wiggleDuration[j]);
			myVerts[i] = (prevVert * (1 - progress) + nextVert * progress + shape.musicDeltas[j] + Vector2.one * 0.5f);
			if (isCycleMode && Application.isPlaying && index > 0)
			{
				Vector2 d = myVerts[i] - myTotalVerts[i];
				myVerts[i] = myTotalVerts[i] + d.normalized * GetCycleMagnitude(index);
			}
			else if (shape.outerLayerProgress != 1 && layerCount > 1 && index == layerCount - 1)
			{
				myVerts[i] = myVerts[i] * _shape.outerLayerProgress + myTotalVerts[(index - 1) * vertCount + i] * (1 - _shape.outerLayerProgress);
			}
			myTotalVerts[index * vertCount + i] = myVerts[i];
		}
	}
	public Vector2 GetPoint(int layerIndex, int sideIndex, float progress = 0, bool isBackwards = false)
	{
		int sideIndex2 = (sideIndex + 1 >= vertCount) ? 0 : sideIndex + 1;
		Vector2 a_outer = myTotalVerts[layerIndex * vertCount + sideIndex];
		Vector2 b_outer = myTotalVerts[layerIndex * vertCount + sideIndex2];
		Vector2 a = myTotalVerts[sideIndex];
		Vector2 b = myTotalVerts[sideIndex2];

		Vector2 p;
		if (isBackwards)
		{
			p = b - (b - a).normalized * progress;
		}
		else
		{
			p = a + (b - a).normalized * progress;
		}
		return GetClosestPointOnLineSegment(p, a_outer, b_outer);

	}
	void ComputeFillBarLayer(int index, float valueA, float valueB)
	{
		//draws a component of the fill bar spanning from valueA to valueB of the 0/1 to 2/3 path of the first layer

		if (verts == null || verts.Length != layerCount * vertCount) { return; }
		if (prevVerts == null || prevVerts.Length != layerCount * vertCount) { Start(); }

		Vector2 vert0 = myTotalVerts[0];
		Vector2 vert1 = myTotalVerts[1];
		Vector2 vert2 = myTotalVerts[2];
		Vector2 vert3 = myTotalVerts[3];

		myTotalVerts[index * 4 + 0] = (1 - valueA) * vert0 + valueA * vert3;
		myTotalVerts[index * 4 + 1] = (1 - valueA) * vert1 + valueA * vert2;
		myTotalVerts[index * 4 + 2] = (1 - valueB) * vert1 + valueB * vert2;
		myTotalVerts[index * 4 + 3] = (1 - valueB) * vert0 + valueB * vert3;
	}

	#region Static Tail Data Used Between Tail Rendering Functions
	Vector2[] myTotalVerts;
	static Vector2[] myTotalTailVerts;
	Vector2[] myTotalBridgeVerts;
	static Vector4[] myBoltSegments;
	static bool isDrawingTail;
	static Vector4 tailBase;
	static Vector2 tailBaseL;
	static Vector2 tailBaseR;
	static Vector2 tailBaseBoundNormalized;
	static Vector2 trueTip;
	static Vector2 baseToTrueTip;
	static Vector2 baseToTrueTipOrth;
	static float trueLength;
	static Vector2 tailFullWiggle;
	static float tailArrowCutoffRatio;
	static float tailArrowCutoffTest;
	static int skippedSegments;
	#endregion
	public void ComputeTailData()
	{
		// FULL TAIL RENDERING PIPELINE
		// find base center
		// find base bounds/midpoint
		// find base midpoint to tip
		// find wiggled tip
		// find true length
		// each mode should supply a function that, given a true length, returns an appropriate list of bolt segments (Vector2(true length ratio, orth displacement))
		// each layer's tip point is (index + 1) / shape.tailLayerCount * (trueTailTip - firstMidpoint) + firstMidpoint
		// then add each layer's points for each segment pair (Segment Wiggle?)
		// finally, compute true closest points?

		// TODO:
		// calculate interior base at corners
		// set orth vector not to pure orth, but scale to slope of shape at base
		// figure out how outer layers will pick base points
		// wiggle?

		#region Tail Base Center
		int j, j2;
		tailBase = new Vector4(0, 0, float.MaxValue, -1); //
		Vector2 rawAbsoluteTip = shape.tailTip; //ABSOLUTE position (probably passed from some external-but-hopefully-local location), NOT relative to sizeDelta or slice values!!!
		if (shape.isTailBaseFixed && shape.tailFixedBaseIndex >= 0 && shape.tailFixedBaseIndex < vertCount)
		{
			j = shape.tailFixedBaseIndex;
			j2 = j == vertCount - 1 ? 0 : j + 1;
			Vector2 segment = myTotalVerts[j2] - myTotalVerts[j];
			tailBase = myTotalVerts[j] + segment * Mathf.Clamp01(tailFixedBaseUnitProgress / segment.magnitude);
			tailBase.z = ((Vector2)tailBase).sqrMagnitude;
			tailBase.w = j;
		}
		else
		{
			for (j = 0; j < vertCount; j++)
			{
				if (j == shape.tailBannedSideIndex) { continue; }
				j2 = j == vertCount - 1 ? 0 : j + 1;
				Vector2 segment = myTotalVerts[j2] - myTotalVerts[j];
				Vector2 a = myTotalVerts[j] + segment.normalized * (shape.tailBaseCornerAvoidance);
				Vector2 b = myTotalVerts[j] + segment.normalized * (segment.magnitude - shape.tailBaseCornerAvoidance);
				Vector4 p = GetClosestPointOnLineSegment(rawAbsoluteTip, a, b);
				//Debug.Log(j + " " + myVerts[j] + " " + myVerts[j2] + " " + p);
				if (p.z < tailBase.z) { tailBase = p; tailBase.w = j; }
			}
			j = (int)tailBase.w;
			j2 = j == vertCount - 1 ? 0 : j + 1;
			shape.tailFixedBaseIndex = j; //save for later
			tailFixedBaseUnitProgress = (myTotalVerts[j2] - myTotalVerts[j]).magnitude - ((Vector2)tailBase - myTotalVerts[j]).magnitude;
		}
		#endregion

		//TODO -- move current base towards the target base point (along edges) by amount timeDelta * speed

		#region Tail Base Bounds
		//find our tailBaseL, tailBaseR, tailBaseMidpoint, and tailBaseRealWidth
		tailBaseL = tailBase;
		tailBaseR = tailBase;
		j = (int)tailBase.w;
		j2 = j == vertCount - 1 ? 0 : j + 1;
		float localProgressL = ((Vector2)tailBase - myTotalVerts[j]).magnitude;
		float localProgressR = localProgressL;
		float progressToGoA = shape.tailBaseWidth /2;
		float progressToGoB = -progressToGoA;

		while (progressToGoA > 0)
		{
			Vector2 currentSegment = myTotalVerts[j2] - myTotalVerts[j];
			float l = currentSegment.magnitude;
			if (l - localProgressL > progressToGoA) //we have room
			{
				tailBaseL = myTotalVerts[j] + currentSegment.normalized * (localProgressL + progressToGoA);
				progressToGoA = 0;
			}
			else //we don't have room--subtract what's there and move on
			{
				progressToGoA -= l - localProgressL;
				j = j == vertCount - 1 ? 0 : j + 1;
				j2 = j == vertCount - 1 ? 0 : j + 1;
				localProgressL = 0;
				//break;
			}
		}
		j = (int)tailBase.w;
		j2 = j == vertCount - 1 ? 0 : j + 1;
		while (progressToGoB < 0)
		{
			Vector2 currentSegment = myTotalVerts[j2] - myTotalVerts[j];
			if (localProgressR > -progressToGoB) //we have room
			{
				tailBaseR = myTotalVerts[j] + currentSegment.normalized * (localProgressR + progressToGoB);
				progressToGoB = 0;
			}
			else //we don't have room--subtract what's there and move on
			{
				progressToGoB += localProgressR;
				j2 = j;
				j = j == 0 ? vertCount - 1 : j - 1;
				localProgressR = (myTotalVerts[j2] - myTotalVerts[j]).magnitude;
				//break;
			}
		}
		tailBaseBoundNormalized = (tailBaseL - tailBaseR).normalized;
		Vector2 tailBaseMidPoint = (tailBaseL + tailBaseR) / 2;
		float tailBaseRealWidth = (tailBaseL - tailBaseR).magnitude;
		#endregion

		Vector2 baseToTail = rawAbsoluteTip - tailBaseMidPoint;
		Vector2 baseToTailNormalized = baseToTail.normalized;
		float totalExtraLength = shape.tailExtraLength - (shape.tailLayerCount - 1) * shape.tailLayerThickness * (shape.isTailArrow ? SQRT_2 : 3);
		float tailLength = Mathf.Max(0, Mathf.Min(baseToTail.magnitude, shape.tailMaxLength) + totalExtraLength);
		trueTip = tailBaseMidPoint + baseToTailNormalized * tailLength;
		baseToTrueTip = trueTip - tailBaseMidPoint;
		float wiggA = shape.tailTipWiggle.x / 2 * shape.tailTipWiggleValueA * shape.tailWiggleMultiplier;
		float wiggB = shape.tailTipWiggle.y / 2 * shape.tailTipWiggleValueB * shape.tailWiggleMultiplier;
		tailFullWiggle = (wiggA * baseToTailNormalized + wiggB * new Vector2(-baseToTailNormalized.y, baseToTailNormalized.x)) * shape.tailProgressPercentage;
		float wiggleDot = Vector2.Dot(tailFullWiggle.normalized, baseToTrueTip.normalized);
		if (wiggleDot < 0 && tailFullWiggle.magnitude > baseToTrueTip.magnitude) { tailFullWiggle = tailFullWiggle.normalized * Mathf.Lerp(tailFullWiggle.magnitude, baseToTrueTip.magnitude, -wiggleDot); }

		if (trueTip.x == tailBaseMidPoint.x || trueTip.y == tailBaseMidPoint.y || IsInsidePolygon(trueTip))
		{
			if (myTotalTailVerts == null || myTotalTailVerts.Length != 0) { System.Array.Resize(ref myTotalTailVerts, 0); }
			return;
		}
		else
		{
			isDrawingTail = true;
		}

		Vector2 baseToTrueTipNormalized = baseToTrueTip.normalized;
		baseToTrueTipOrth = new Vector2(-baseToTrueTipNormalized.y, baseToTrueTipNormalized.x);
		trueLength = baseToTrueTip.magnitude;
		int segmentCount = 0;
		skippedSegments = 0;

		float x = shape.tailBoltSegmentDistance;
		tailArrowCutoffRatio = shape.tailBoltSegmentDistance / trueLength * 0.5f;
		tailArrowCutoffTest = 1 - tailArrowCutoffRatio;
		if (shape.tailMode == TailMode.BoltFixed)
		{
			segmentCount = 4;
			if (myBoltSegments == null || myBoltSegments.Length != segmentCount) { System.Array.Resize(ref myBoltSegments, segmentCount); }
			for (int i = 0; i < segmentCount; i++)
			{
				float r = (i + 1f) / (segmentCount + 1f);
				if (shape.isTailArrow) { r += tailArrowCutoffRatio * (1 - r); if (i == 0) { tailArrowCutoffTest = 1 - r; } }
				if (1f - shape.tailProgressPercentage > r) { skippedSegments++; }

				float w = tailBaseRealWidth * r;

				float d = w;
				//d += L.erp(shape.tailWiggleProgress / shape.tailWiggleDuration, Lerp.SinFullSquared) * 12 * shape.tailProgressPercentage;
				d *= i % 2 == 0 ? -1 : 1; //alternate every other 1
				d *= (1 - 2 * shape.gutterCurrentMovementPercentage); //alternate based on gutter movement
				d *= shape.isTailFlipped ? -1 : 1;

				float rot = 0;

				myBoltSegments[i] = new Vector4(r, w, d, rot);
			}
		}
		else if (shape.tailMode == TailMode.BoltTapered || shape.shape.tailMode == TailMode.BoltTaperedHalf || shape.tailMode == TailMode.BoltThick)
		{
			segmentCount = Mathf.Max(0, Mathf.FloorToInt(trueLength / x));
			if (myBoltSegments == null || myBoltSegments.Length != segmentCount) { System.Array.Resize(ref myBoltSegments, segmentCount); }
			for (int i = 0; i < segmentCount; i++)
			{
				float r = x * (i + 1) / trueLength;
				if (shape.isTailArrow) { r += tailArrowCutoffRatio * (1 - r); if (i == 0) { tailArrowCutoffTest = 1 - r; } }
				if (1f - shape.tailProgressPercentage > r) { skippedSegments++; }

				float w = tailBaseRealWidth;
				if (shape.tailMode == TailMode.BoltTapered) { w *= r; }
				if (shape.tailMode == TailMode.BoltTaperedHalf) { w *= Mathf.Sqrt(r); }

				float d = w;
				//d += L.erp(shape.tailWiggleProgress / shape.tailWiggleDuration, Lerp.SinFullSquared) * 12 * shape.tailProgressPercentage;
				d *= i % 2 == 0 ? -1 : 1; //alternate every other 1
				d *= (1 - 2 * shape.gutterCurrentMovementPercentage); //alternate based on gutter movement
				d *= shape.isTailFlipped ? -1 : 1;

				float rot = 0;

				myBoltSegments[i] = new Vector4(r, w, d, rot);
			}
			if (segmentCount > 0)
			{
				myBoltSegments[myBoltSegments.Length - 1].z *= (trueLength % x) / x; //adjust last displacement to allow smooth transitions
			}
		}
		else if (shape.tailMode == TailMode.Square)
		{
			segmentCount = 2 * Mathf.FloorToInt(trueLength / x);
			if (segmentCount < 0) { segmentCount = 0; }
			if (myBoltSegments == null || myBoltSegments.Length != segmentCount) { System.Array.Resize(ref myBoltSegments, segmentCount); }
			for (int i = 0; i < segmentCount; i++)
			{
				float r = x * (Mathf.FloorToInt(i / 2) + 1) / trueLength;
				if (shape.isTailArrow) { r += tailArrowCutoffRatio * (1 - r); if (i == 0) { tailArrowCutoffTest = 1 - r; } }
				if (1f - shape.tailProgressPercentage > r) { skippedSegments++; }

				float w = tailBaseRealWidth;
				if (shape.tailMode == TailMode.BoltTapered) { w *= r; }
				if (shape.tailMode == TailMode.BoltTaperedHalf || shape.tailMode == TailMode.Square) { w *= Mathf.Sqrt(r); }

				float d = 1.2f * w;
				//d += L.erp(shape.tailWiggleProgress / shape.tailWiggleDuration, Lerp.SinFullSquared) * 12 * (i % 2 == 0 ? -1 : 1) * shape.tailProgressPercentage;
				d *= (i + 1) % 4 > 1 ? -1 : 1; //alternate every other 2
				d *= (1 - 2 * shape.gutterCurrentMovementPercentage); //alternate based on gutter movement
				d *= shape.isTailFlipped ? -1 : 1;

				float rot = i % 4 > 1 ? -1 : 1;
				rot *= (1 - 2 * shape.gutterCurrentMovementPercentage); //alternate based on gutter movement
				rot *= shape.isTailFlipped ? -1 : 1;
				myBoltSegments[i] = new Vector4(r, w, d, rot);
			}
			if (segmentCount > 0)
			{
				myBoltSegments[myBoltSegments.Length - 1].z *= (trueLength % x) / x; //adjust last displacement to allow smooth transitions
				myBoltSegments[myBoltSegments.Length - 1].w *= (trueLength % x) / x; //adjust last displacement to allow smooth transitions
			}
		}
		else
		{
			tailArrowCutoffTest = -1;
			if (myBoltSegments == null || myBoltSegments.Length != segmentCount) { System.Array.Resize(ref myBoltSegments, segmentCount); }
		}

		tailVertCount = 3 + 2 * (segmentCount - skippedSegments) + (shape.isTailArrow && shape.tailProgressPercentage > tailArrowCutoffTest ? 4 : 0);
		if (myTailVerts == null || myTailVerts.Length != tailVertCount) { System.Array.Resize(ref myTailVerts, tailVertCount); }
		if (myTotalTailVerts == null || myTotalTailVerts.Length != tailVertCount * shape.tailLayerCount) { System.Array.Resize(ref myTotalTailVerts, tailVertCount * shape.tailLayerCount); }
	}

	public void ComputeTailLayer(int index)
	{
		//compute and add the vertex pair for each segment
		int pairIndex = 1;
		Vector2 firstMidpoint = tailBase;
		Vector2 myTipTarget = trueTip;
		Vector2 myOutermostTip = trueTip;
		float r0 = 0;
		float r1 = 1f;
		int firstFullPairIndex = -1;
		float segmentProgress = 1f - shape.tailProgressPercentage;
		float invDot = 1.0f / Vector2.Dot(tailBaseBoundNormalized, baseToTrueTipOrth);
		if (shape.shape.tailMode == TailMode.Basic)
		{
			myOutermostTip = firstMidpoint + (myTipTarget - firstMidpoint + tailFullWiggle) * (1 - segmentProgress);
			if (shape.isTailArrow)
			{
				float size = shape.tailBaseWidth / 4 + index * shape.tailLayerThickness;

				Vector2 c1 = (myTipTarget - firstMidpoint).normalized;
				Vector2 c2 = new Vector2(-c1.y, c1.x);
				//Debug.Log(trueTip.x + " " + trueTip.y + " " + tailBase.x + " " + tailBase.y + " " + c1.x + " " + c1.y + " " + c2.x + " " + c2.y);
				float x = Mathf.Min(shape.tailBaseWidth, shape.tailBoltSegmentDistance) * INV_SQRT_2;
				float myThickness = index * (shape.tailLayerThickness) * INV_SQRT_2;
				Vector2 a1 = myOutermostTip - c1 * (x + myThickness) - c2 * (x * INV_SQRT_2 + myThickness * 2);
				Vector2 a2 = myOutermostTip - c1 * (x + myThickness) + c2 * (x * INV_SQRT_2 + myThickness * 2);
				myTotalTailVerts[index * tailVertCount + 1] = a1;
				myTotalTailVerts[index * tailVertCount + tailVertCount - 1] = a2;
				a1 = myOutermostTip - c1 * (x + myThickness) - c2 * size * INV_SQRT_2;
				a2 = myOutermostTip - c1 * (x + myThickness) + c2 * size * INV_SQRT_2;
				myTotalTailVerts[index * tailVertCount + 2] = a1;
				myTotalTailVerts[index * tailVertCount + tailVertCount - 2] = a2;
				pairIndex += 2;
				firstMidpoint = (a1 + a2) / 2;
			}
		}
		else if (shape.shape.tailMode != TailMode.NONE)
		{
			myOutermostTip = firstMidpoint; //in case we don't accept any segments
			for (int i = 0; i < myBoltSegments.Length; i++)
			{
				//Debug.Log(index + " " + i + " " + myBoltSegments[i]);
				float r = myBoltSegments[i].x;
				float w = myBoltSegments[i].y;
				float d = myBoltSegments[i].z;
				float rot = myBoltSegments[i].w;
				//Debug.Log(index + "/" + i + " r: " + r + " w: " + w + " d: " + d + " rot: " + rot);

				float dist = Mathf.Sqrt(4*d*d + shape.tailBoltSegmentDistance * shape.tailBoltSegmentDistance); //estimated distance to prev/next segments (not fully accurate)
				float layerExtension = Mathf.Abs(2*d)/dist * shape.tailLayerThickness * 2;
				float size = w / 2 + index * layerExtension; //orthoganal half-width of segment
				if (rot != 0) { size *= INV_SQRT_2; }

				if (firstFullPairIndex == -1 && 1f - shape.tailProgressPercentage < r) //our progress ratio has "passed" this point--accept it
				{
					firstFullPairIndex = i;
					r1 = r;
					segmentProgress = (1f - shape.tailProgressPercentage - r0) / (r1 - r0);
					size *= 1 - segmentProgress; //reverse progress lookahead adjustment to width
				}

				Vector2 mySegmentVector = r * invDot * tailBaseBoundNormalized + (1 - r) * baseToTrueTipOrth;
				Vector2 wiggle = tailFullWiggle * (1f - r) * shape.tailProgressPercentage;
				Vector2 p1 = trueTip - baseToTrueTip * r + wiggle + mySegmentVector * (d - size) + baseToTrueTip.normalized * rot * shape.tailLayerThickness * INV_SQRT_2 * (index + 1);
				Vector2 p2 = trueTip - baseToTrueTip * r + wiggle + mySegmentVector * (d + size) - baseToTrueTip.normalized * rot * shape.tailLayerThickness * INV_SQRT_2 * (index + 1);
				//Debug.Log(trueTip - baseToTrueTip * (1 - r)  + " " + p1 + " " + p2);

				if (firstFullPairIndex == -1) //still no full pair yet!
				{
					myTipTarget = (p1 + p2) / 2;
					r0 = r;
				}
				else
				{
					if (firstFullPairIndex == i)
					{
						firstMidpoint = (p1 + p2) / 2;
						myOutermostTip = firstMidpoint + (myTipTarget - firstMidpoint) * (1 - segmentProgress) + tailFullWiggle * shape.tailProgressPercentage;
						if (shape.isTailArrow && shape.tailProgressPercentage > tailArrowCutoffTest)
						{
							Vector2 c1 = (myTipTarget - firstMidpoint).normalized;
							Vector2 c2 = new Vector2(-c1.y, c1.x);
							float x = Mathf.Min(shape.tailBaseWidth, shape.tailBoltSegmentDistance) * INV_SQRT_2;
							float myThickness = index * (shape.tailLayerThickness) * INV_SQRT_2;
							Vector2 a1 = myOutermostTip - c1 * (x + myThickness) - c2 * (x * INV_SQRT_2 + myThickness * 2);
							Vector2 a2 = myOutermostTip - c1 * (x + myThickness) + c2 * (x * INV_SQRT_2 + myThickness * 2);
							myTotalTailVerts[index * tailVertCount + 1] = a1;
							myTotalTailVerts[index * tailVertCount + tailVertCount - 1] = a2;
							a1 = myOutermostTip - c1 * (x + myThickness) - c2 * size * INV_SQRT_2;
							a2 = myOutermostTip - c1 * (x + myThickness) + c2 * size * INV_SQRT_2;
							myTotalTailVerts[index * tailVertCount + 2] = a1;
							myTotalTailVerts[index * tailVertCount + tailVertCount - 2] = a2;
							pairIndex += 2;
							firstMidpoint = (a1 + a2) / 2;
						}
					}
					myTotalTailVerts[index * tailVertCount + pairIndex] = p1;
					myTotalTailVerts[index * tailVertCount + tailVertCount - pairIndex] = p2;
					pairIndex++;
				}
			}
			if (firstFullPairIndex == -1) //accepted none, skipped all!
			{
				r1 = 1;
				segmentProgress = (1f - shape.tailProgressPercentage - r0) / (r1 - r0);
				myOutermostTip = firstMidpoint + (myTipTarget - firstMidpoint) * (1 - segmentProgress) + tailFullWiggle;// * shape.tailProgressPercentage;
				if (shape.isTailArrow && shape.tailProgressPercentage > tailArrowCutoffTest)
				{
					float size = shape.tailBaseWidth / 2 + index * shape.tailLayerThickness;

					Vector2 c1 = (myTipTarget - firstMidpoint).normalized;
					Vector2 c2 = new Vector2(-c1.y, c1.x);
					float x = Mathf.Min(shape.tailBaseWidth, shape.tailBoltSegmentDistance) * INV_SQRT_2;
					float myThickness = index * (shape.tailLayerThickness) * INV_SQRT_2;
					Vector2 a1 = myOutermostTip - c1 * (x + myThickness) - c2 * (x * INV_SQRT_2 + myThickness * 2);
					Vector2 a2 = myOutermostTip - c1 * (x + myThickness) + c2 * (x * INV_SQRT_2 + myThickness * 2);
					myTotalTailVerts[index * tailVertCount + 1] = a1;
					myTotalTailVerts[index * tailVertCount + tailVertCount - 1] = a2;
					a1 = myOutermostTip - c1 * (x + myThickness) - c2 * size * INV_SQRT_2;
					a2 = myOutermostTip - c1 * (x + myThickness) + c2 * size * INV_SQRT_2;
					myTotalTailVerts[index * tailVertCount + 2] = a1;
					myTotalTailVerts[index * tailVertCount + tailVertCount - 2] = a2;
					pairIndex += 2;
					firstMidpoint = (a1 + a2) / 2;
				}
			}
		}

		//now tip vertex
		Vector2 v = (myOutermostTip - firstMidpoint);
		float ratio = shape.isTailArrow ? SQRT_2 : Mathf.Min(v.magnitude /  (shape.tailBaseWidth / 2),3);
		if (shape.tailMode != TailMode.Basic) { ratio *= shape.tailProgressPercentage > tailArrowCutoffTest ? (1-segmentProgress) : 0; }

		myTailVerts[0] = myOutermostTip + v.normalized * index * shape.tailLayerThickness * ratio;
		myTotalTailVerts[index * tailVertCount + 0] = myTailVerts[0];

		//finally add the base pair
		if (index == 0)
		{
			myTailVerts[pairIndex] = tailBaseL - baseToTrueTip.normalized * 1f;
			myTailVerts[tailVertCount - pairIndex] = tailBaseR - baseToTrueTip.normalized * 1f;
		}
		else
		{
			myTailVerts[pairIndex] = myTotalTailVerts[pairIndex] + tailBaseBoundNormalized * index * shape.tailLayerThickness;
			myTailVerts[tailVertCount - pairIndex] = myTotalTailVerts[tailVertCount - pairIndex] - tailBaseBoundNormalized * index * shape.tailLayerThickness;
		}
		myTotalTailVerts[index * tailVertCount + pairIndex] = myTailVerts[pairIndex];
		myTotalTailVerts[index * tailVertCount + tailVertCount - pairIndex] = myTailVerts[tailVertCount - pairIndex];
		//Debug.Log(myBoltSegments.Length + " " + tailVertCount + " " + pairIndex + " " + (index * tailVertCount + pairIndex) + " " + (index * tailVertCount + tailVertCount - pairIndex));
	}
	private void ScaleTailArrowVerts()
	{
		for (int index = 0; index < shape.tailLayerCount; index++)
		{
			float p = Mathf.Clamp01((shape.tailProgressPercentage - tailArrowCutoffTest) / (1f- tailArrowCutoffTest));
			myTotalTailVerts[index * tailVertCount + 1]                 = Vector2.Lerp(myTotalTailVerts[(shape.tailLayerCount - 1) * tailVertCount], myTotalTailVerts[index * tailVertCount + 1], p);
			myTotalTailVerts[index * tailVertCount + tailVertCount - 1] = Vector2.Lerp(myTotalTailVerts[(shape.tailLayerCount - 1) * tailVertCount], myTotalTailVerts[index * tailVertCount + tailVertCount - 1], p);
			myTotalTailVerts[index * tailVertCount + 2]                 = Vector2.Lerp(myTotalTailVerts[(shape.tailLayerCount - 1) * tailVertCount], myTotalTailVerts[index * tailVertCount + 2], p);
			myTotalTailVerts[index * tailVertCount + tailVertCount - 2] = Vector2.Lerp(myTotalTailVerts[(shape.tailLayerCount - 1) * tailVertCount], myTotalTailVerts[index * tailVertCount + tailVertCount - 2], p);
			myTotalTailVerts[index * tailVertCount]                     = Vector2.Lerp(myTotalTailVerts[(shape.tailLayerCount - 1) * tailVertCount], myTotalTailVerts[index * tailVertCount], p);
		}
	}

	public Vector2 GetBridgePivot(Vector2 sd) //sizeDelta
	{
		return new Vector2(0.178f, 1.095f); //temp
		//Vector2 p = GetPoint(0, 1, 0f);
		//Debug.Log(sd + " " + p);
		//return new Vector2(p.x / sd.x + 0.5f, 1 + 20 / sd.y);
	}

	public void ComputeBridgeLayer(int layerIndex, bool isRecursive = false)
	{
		if (myTotalBridgeVerts == null) { return; }
		//if (lastCalcTime == Time.time) { return; }
		float lw = shape.bridgeLayerOuterWidth * layerIndex;
		float totalWidth = (shape.bridgeLayerInnerWidth / 2 + lw) * shape.bridgeProgressC / Mathf.Max(0.1f, transform.lossyScale.x);
		RectTransform rt = shape.bridgeTargetRectPosDriver == null ? shape.bridgeTarget.rectTransform : shape.bridgeTargetRectPosDriver;
		Vector3 targetUnscaledPos = shape.bridgeTarget.position - (Vector3)(rt.sizeDelta * (rt.pivot - Vector2.one / 2)) * (1-rt.lossyScale.x);
		Vector2 posDelta = targetUnscaledPos - shape.position;
		if (!isRecursive && shape.bridgeTarget.bridgeTarget == shape)
		{
			shape.bridgeTarget._mesh.ComputeBridgeLayer(layerIndex, true);
		}

		Vector2 a = GetPoint(layerIndex, shape.bridgeMySideIndex, shape.bridgeProgressA - totalWidth, shape.isBridgeOffsetsInverted);
		Vector2 b = GetPoint(layerIndex, shape.bridgeMySideIndex, shape.bridgeProgressA + totalWidth, shape.isBridgeOffsetsInverted);
		Vector2 c = shape.bridgeTarget._mesh.GetPoint(layerIndex, shape.bridgeTargetSideIndex, shape.bridgeProgressB + totalWidth, !shape.isBridgeOffsetsInverted) + posDelta;
		Vector2 d = shape.bridgeTarget._mesh.GetPoint(layerIndex, shape.bridgeTargetSideIndex, shape.bridgeProgressB - totalWidth, !shape.isBridgeOffsetsInverted) + posDelta;

		float theta = 0;
		float angledLayerCenterOffset = 0;
		if (layerIndex > 0)
		{
			Vector2 delta = myTotalBridgeVerts[3] - myTotalBridgeVerts[0];
			theta = Mathf.Atan2(delta.y, delta.x);
			totalWidth = (shape.bridgeLayerInnerWidth / 2 + shape.bridgeLayerOuterWidth / Mathf.Max(Mathf.Abs(Mathf.Sin(theta)), 0.001f) * layerIndex) * shape.bridgeProgressC;
			angledLayerCenterOffset = shape.bridgeLayerOuterWidth * layerIndex / Mathf.Abs(Mathf.Tan(theta));

			if (theta*Mathf.Rad2Deg > 90 )
			{
				a = GetPoint(layerIndex, shape.bridgeMySideIndex, shape.bridgeProgressA - totalWidth - angledLayerCenterOffset, shape.isBridgeOffsetsInverted);
				c = shape.bridgeTarget._mesh.GetPoint(layerIndex, shape.bridgeTargetSideIndex, shape.bridgeProgressB + totalWidth + angledLayerCenterOffset, !shape.isBridgeOffsetsInverted) + posDelta;
			}
			else
			{
				b = GetPoint(layerIndex, shape.bridgeMySideIndex, shape.bridgeProgressA + totalWidth + angledLayerCenterOffset, shape.isBridgeOffsetsInverted);
				d = shape.bridgeTarget._mesh.GetPoint(layerIndex, shape.bridgeTargetSideIndex, shape.bridgeProgressB - totalWidth - angledLayerCenterOffset, !shape.isBridgeOffsetsInverted) + posDelta;
			}

		}

		float length = shape.bridgeProgressD;
		float r = shape.bridgeLayerOuterWidth / (a - d).magnitude;
		float limit = r / 4;
		if (length > 1 - limit)
		{
			float p = (length - limit) / (1 - limit);
			length += r * (layerCount - layerIndex) * -length * (1 - p);
			length *= 1 + shape.bridgeLayerOuterWidth / (d-a).magnitude / 4 * p;
		}
		else if (length > 0)
		{
			length += r * (layerCount - layerIndex) * -length;
		}
		else if (length == 0)
		{
			//pass
		}
		else
		{
			length = -3 * r * length  * (layerCount - layerIndex - 1);
		}
		//Debug.Log(gameObject.name + " " + layerIndex + " " + a + " " + b + " " + (c - posDelta) + " " + (d - posDelta) + " " + length);

		myTotalBridgeVerts[layerIndex * 4 + 0] = a;
		myTotalBridgeVerts[layerIndex * 4 + 1] = b;
		myTotalBridgeVerts[layerIndex * 4 + 2] = (1 - length) * b + length * c;
		myTotalBridgeVerts[layerIndex * 4 + 3] = (1 - length) * a + length * d;
	}

	public void ComputeGutter()
	{
		isRenderingGutter = false;
		if (shape.isGutter && shape.layerCount >= 2 && shape.gutterSide < shape.vertexCount)
		{
			Vector2 d = shape.gutterTotalMovement.normalized;
			Vector2 dt = d * shape.gutterLineThickness;
			int g1 = shape.gutterSide;
			int g0 = g1 + 1;
			if (g0 == vertCount) { g0 = 0; }
			int g2 = g1 - 1;
			if (g2 < 0) { g2 = vertCount - 1; }

			int x1 = g1;
			int x0 = g0;
			int x2 = g2;

			if (shape.wiggleProfiles[0].rotateOption != WiggleRotateOption.NoRotate)
			{
				x1 -= shape.wiggleRotationStep[0];
				while (x1 < 0) { x1 += vertCount; }
				x0 -= shape.wiggleRotationStep[0];
				while (x0 < 0) { x0 += vertCount; }
				x2 -= shape.wiggleRotationStep[0];
				while (x2 < 0) { x2 += vertCount; }
			}

			myGutterVerts[0] = myTotalVerts[x0] + dt;
			myGutterVerts[1] = myTotalVerts[x1] + dt;

			if (shape.isGutter3D)
			{
				myGutter2Verts[0] = myTotalVerts[x1] + dt;
				myGutter2Verts[1] = myTotalVerts[x2] + dt;

				//adjust myGutterVerts[1] and myGutter2Verts[0] inward
				myGutterVerts[1] -= (myGutterVerts[1] - myGutterVerts[0]).normalized* shape.gutterLineThickness / 2;
				myGutter2Verts[0] -= (myGutter2Verts[0] - myGutter2Verts[1]).normalized * shape.gutterLineThickness / 2;
			}

			myGutterVerts[2] = SuperShape.LineSegmentsIntersection(myGutterVerts[1], myGutterVerts[1] + d * 1000, myTotalVerts[vertCount + g1], myTotalVerts[vertCount + g0]) - dt;
			myGutterVerts[3] = SuperShape.LineSegmentsIntersection(myGutterVerts[0], myGutterVerts[0] + d * 1000, myTotalVerts[vertCount + g1], myTotalVerts[vertCount + g0]) - dt;

			if (shape.isGutter3D)
			{
				int z = shape.isGutter3DSameSide ? g0 : g2;
				myGutter2Verts[2] = SuperShape.LineSegmentsIntersection(myGutter2Verts[1], myGutter2Verts[1] + d * 1000, myTotalVerts[vertCount + g1], myTotalVerts[vertCount + z]) - dt;
				myGutter2Verts[3] = SuperShape.LineSegmentsIntersection(myGutter2Verts[0], myGutter2Verts[0] + d * 1000, myTotalVerts[vertCount + g1], myTotalVerts[vertCount + z]) - dt;
			}
			isRenderingGutter = true;
		}
	}

	public void ComputeLayerRotation(int index)
	{
		if (index >= shape.wiggleProfiles.Length) { Debug.Log(gameObject.name + " " + index); return; }

		WiggleRotateOption wro = shape.wiggleProfiles[index].rotateOption;
		if (wro == WiggleRotateOption.NoRotate) { return; }
		bool isClockwise = wro == WiggleRotateOption.Clockwise || wro == WiggleRotateOption.ClockwiseOuter;
		bool isOuter = (index != layerCount - 1) && (wro == WiggleRotateOption.ClockwiseOuter || wro == WiggleRotateOption.CClockwiseOuter);
		int stepProgress = shape.wiggleRotationStep[index];
		float progress = L.erp(shape.wiggleProgress[index] / shape.wiggleDuration[index], shape.wiggleProfiles[index].lerp);
		for (int j = 0; j < vertCount; j++)
		{
			int j1 = j + (stepProgress) * (isClockwise ? 1 : -1);
			int j2 = j + (stepProgress + 1) * (isClockwise ? 1 : -1);
			while (j1 >= vertCount) j1 -= vertCount; while (j1 < 0) j1 += vertCount;
			while (j2 >= vertCount) j2 -= vertCount; while (j2 < 0) j2 += vertCount;
			int k1 = (index + (isOuter ? 1 : 0)) * vertCount + j1;
			int k2 = (index + (isOuter ? 1 : 0)) * vertCount + j2;
			myVerts[j] = myTotalVerts[k1] * (1 - progress) + myTotalVerts[k2] * progress;
		}
		for (int j = 0; j < vertCount; j++)
		{
			myTotalVerts[index * vertCount + j] = myVerts[j];
		}
	}

	public void FilterIgnoredVerts(int index)
	{
		for (int j = 0; j < vertCount; j++)
		{
			if (!ignoreVerts.Contains(j)) { continue; }
			int j0 = j == 0 ? vertCount - 1 : j - 1;
			while (ignoreVerts.Contains(j0))
			{
				if (j0 == j) { return; } //give up
				j0 = j0 == 0 ? vertCount - 1 : j0 - 1;
			}
			int j2 = j == vertCount - 1 ? 0 : j + 1;
			while (ignoreVerts.Contains(j2))
			{
				if (j2 == j) { return; } //give up
				j2 = j2 == vertCount - 1 ? 0 : j2 + 1;
			}
			float ratio = Mathf.Clamp01(ignoreRatio);
			myTotalVerts[index * vertCount + j] = myTotalVerts[index * vertCount + j] * (1 - ratio) +
			                                      (myTotalVerts[index * vertCount + j0] + myTotalVerts[index * vertCount + j2]) / 2 * ratio;
		}
	}

	public void ComputeColliderVerts()
	{
		myColliderVerts.Clear();
		for (int j = vertCount * colliderLayer; j < vertCount * (colliderLayer + 1); j++)
		{
			myColliderVerts.Add(myTotalVerts[j]);
		}
		myMainCollider.points = myColliderVerts.ToArray();

		if (myTailCollider != null)
		{
			if (myTotalTailVerts == null || myTotalTailVerts.Length == 0 || shape.tailMinLayer > 0)
			{
				if (myTailCollider.points.Length != 0)
				{
					myTailCollider.points = new Vector2[1] { Vector2.zero }; //empty array doesn't work
				}
			}
			else
			{
				myColliderVerts.Clear();
				for (int j = tailVertCount * colliderLayer; j < tailVertCount * (colliderLayer + 1); j++)
				{
					myColliderVerts.Add(myTotalTailVerts[j]);
				}
				myTailCollider.points = myColliderVerts.ToArray();
			}
		}

	}

	public void ComputeProxyVerts()
	{
		myProxyVerts.Clear();
		for (int j = 0; j < vertCount; j++)
		{
			myProxyVerts.Add(ConvertProxyVert(myTotalVerts[j]));
			if (j == shape.trueFlattenSide)
			{
				int j2 = j == vertCount - 1 ? 0 : j + 1;
				myProxyVerts.Add(ConvertProxyVert(myTotalVerts[(layerCount - 1) * vertCount + j])); //outermost
				myProxyVerts.Add(ConvertProxyVert(myTotalVerts[(layerCount - 1) * vertCount + j2]));
			}
		}
	}


	public void DrawLayer(int index, bool isUVsZero, bool isAssigningToMask = false)
	{
		Vector2 gutterMovement = shape.gutterTotalMovement * shape.gutterCurrentMovementPercentage;
		for (int j = 0; j < vertCount; j++)
		{
			myVerts[j] = myTotalVerts[index * vertCount + j];
			if (isRenderingGutter && (index == 0))// || (Vector2.Dot(myVerts[j], shape.gutterTotalMovement) < 0 && !shape.isGutter3D)))
			{
				myVerts[j] += gutterMovement;
			}
		}
		if (isDrawingTail && index < shape.tailLayerCount)
		{
			for (int j = 0; j < tailVertCount; j++)
			{
				myTailVerts[j] = myTotalTailVerts[index * tailVertCount + j];
				if (isRenderingGutter && (index == 0))// || (Vector2.Dot(myTailVerts[j], shape.gutterTotalMovement) < 0 && !shape.isGutter3D)))
				{
					//myTailVerts[j] += gutterMovement;
				}
			}
		}
		if (shape.bridgeTarget != null && shape.bridgeTarget._mesh != null)
		{
			for (int j = 0; j < 4; j++)
			{
				myBridgeVerts[j] = myTotalBridgeVerts[index * vertCount + j];
			}
		}
		if (index == 0 && isRenderingGutter)
		{
			myGutterVerts[0] += gutterMovement;
			myGutterVerts[1] += gutterMovement;
			if (shape.isGutter3D)
			{
				myGutter2Verts[0] += gutterMovement;
				myGutter2Verts[1] += gutterMovement;
			}
		}

		Color myColor = index < shape.layerColors.Length ? _shape.layerColors[index] : Color.white;
		if (index == 0) { myGutterColor = myColor; }
		Vector2 UVmin = new Vector2(-1, 1);
		Vector2 UVmax = new Vector2(-1, 1);
		InnerTextureMode innerTextureMode = shape.innerTextureMode;
		InnerTextureSubMode innerTextureSubMode = shape.innerTextureSubMode;
		if (isUVsZero)
		{
			//pass
		}
		else if (innerTextureMode == InnerTextureMode.ScreenSpace)
		{
			UVmax = Vector2.one;
			UVmin = Vector2.zero;
		}
		else if (innerTextureMode == InnerTextureMode.Fixed)
		{
			UVmax = shape.fixedUVRange;
			UVmin = -UVmax;
		}
		else if (innerTextureMode == InnerTextureMode.SliceOnly)
		{
			UVmax = new Vector2(sliceWidth, sliceHeight) / 2;
			UVmin = -UVmax;
		}
		else
		{
			UVmin = new Vector2(float.MaxValue, float.MaxValue);
			UVmax = new Vector2(float.MinValue, float.MinValue);
			for (int i = 0; i < myVerts.Length; i++)
			{
				Vector2 v = verts[i];
				if      (shape.innerTextureSubMode == InnerTextureSubMode.Full) { v = myVerts[i]; }
				else if (shape.innerTextureSubMode == InnerTextureSubMode.OmitWiggle) //but keep slice -- add to base here
				{
					if (v.x < 0) { v.x -= sliceWidth  / 2; }
					if (v.y < 0) { v.y -= sliceHeight / 2; }
					if (v.x > 0) { v.x += sliceWidth  / 2; }
					if (v.y > 0) { v.y += sliceHeight / 2; }
				}

				if      (v.x < UVmin.x) { UVmin.x = v.x; }
				else if (v.x > UVmax.x) { UVmax.x = v.x; }
				if      (v.y < UVmin.y) { UVmin.y = v.y; }
				else if (v.y > UVmax.y) { UVmax.y = v.y; }
			}
			if (innerTextureMode == InnerTextureMode.MinMaxVertsFixedRatioMin)
			{
				float range = Mathf.Min(UVmax.x - UVmin.x, UVmax.y - UVmin.y);
				UVmax = UVmin + new Vector2(range, range);
			}
			else if (innerTextureMode == InnerTextureMode.MinMaxVertsFixedRatioMax)
			{
				float range = Mathf.Max(UVmax.x - UVmin.x, UVmax.y - UVmin.y);
				UVmax = UVmin + new Vector2(range, range);
			}
			else if (innerTextureMode == InnerTextureMode.MinMaxVertsFixedRatioX)
			{
				float range = UVmax.x - UVmin.x;
				UVmax = UVmin + new Vector2(range, range);
			}
			else if (innerTextureMode == InnerTextureMode.MinMaxVertsFixedRatioY)
			{
				float range = UVmax.y - UVmin.y;
				UVmax = UVmin + new Vector2(range, range);
			}
		}

		if (!isCached || triCache == null || triCache.Count != vertCount - 2) { triCache = null; tailTriCache = null; }
		if (triCache == null) {triCache = new List<Tri>(TriangulationUtility.TriangulateFull(myVerts, myColor, UVmin, UVmax, triCache)); }
		if (isDrawingTail && shape.tailLayerCount > index && index >= shape.tailMinLayer) { tailTriCache = new List<Tri>(TriangulationUtility.TriangulateFull(myTailVerts, myColor, UVmin, UVmax, tailTriCache)); }
		
		MyVertexHelper.AddSet(myVerts, triCache, myColor, UVmin, UVmax, -Vector3.forward);
		MyVertexHelper.AddSet(myTailVerts, tailTriCache, myColor, UVmin, UVmax, -Vector3.forward);
		if (shape.bridgeTarget != null)
		{
			MyVertexHelper.AddSet(myBridgeVerts, gutterTris, myColor, UVmin, UVmax, -Vector3.forward);
		}

		if (isAssigningToMask)
		{
			//Vector3 childPos = Vector3.zero;
			if (proxyMaskMesh != null)
			{
				proxyMaskMesh.AssignVerts(myProxyVerts);
			}
		}
		
	}

	public void DrawLayerBorder(int index)
	{
		for (int j = 0; j < vertCount; j++)
		{
			myVerts[j] = myTotalVerts[index * vertCount + j];
		}
		if (isDrawingTail && index < shape.tailLayerCount)
		{
			for (int j = 0; j < tailVertCount; j++)
			{
				myTailVerts[j] = myTotalTailVerts[index * tailVertCount + j];
			}
		}

		Color myColor = index < shape.layerColors.Length ? _shape.layerColors[index] : Color.white;

		MyVertexHelper.AddSet(myPrevVerts, null, myColor, Vector2.zero, Vector2.zero, -Vector3.forward);
		MyVertexHelper.AddSet(myVerts, TriangulationUtility.TriangulateBorderList(myVerts.Length), myColor, Vector2.zero, Vector2.zero, -Vector3.forward);

		for (int j = 0; j < vertCount; j++)
		{
			myPrevVerts[j] = myVerts[j];
		}
	}

	static readonly List<Tri> gutterTris = new List<Tri>() { new Tri(0,2,1), new Tri(2,3,0) };
	public void DrawGutter()
	{
		MyVertexHelper.AddSet(myGutterVerts, gutterTris, myGutterColor, Vector2.zero, Vector2.zero, -Vector3.forward);
		if (shape.isGutter3D)
		{
			MyVertexHelper.AddSet(myGutter2Verts, gutterTris, myGutterColor, Vector2.zero, Vector2.zero, -Vector3.forward);
		}
	}

	private void ComputeAndDrawFillBar()
	{
		//handle everything in a single function the replaces the normal pipeline, since this is simplier but so different
		System.Array.Resize(ref myTotalVerts, 16);

		ComputeLayer(0);
		float a = Mathf.Min(_shapeFillBar.baseComplete, _shapeFillBar.tweenComplete);
		float b = Mathf.Max(_shapeFillBar.baseComplete, _shapeFillBar.tweenComplete);
		ComputeFillBarLayer(2, b, 1);
		ComputeFillBarLayer(1, a, b);
		ComputeFillBarLayer(0, 0, a);
		ComputeLayer(3);

		ComputeGutter();
		if (myMainCollider != null)
		{
			ComputeColliderVerts();
		}
		if (proxyMaskMesh != null)
		{
			ComputeProxyVerts();
		}

		if (myMesh == null) { myMesh = new Mesh(); }
		MyVertexHelper.Clear();

		if (a > 0)
		{
			DrawLayer(0, false, true);
		}
		if (b - a != 0)
		{
			for (int j = 0; j < vertCount; j++) { myVerts[j] = myTotalVerts[1 * vertCount + j]; }
			MyVertexHelper.AddSet(myVerts, gutterTris, _shape.layerColors[1], Vector2.zero, Vector2.zero, -Vector3.forward);
		}
		if (1 > b)
		{
			for (int j = 0; j < vertCount; j++) { myVerts[j] = myTotalVerts[2 * vertCount + j]; }
			MyVertexHelper.AddSet(myVerts, gutterTris, _shape.layerColors[2], Vector2.zero, Vector2.zero, -Vector3.forward);
		}
		System.Array.Resize(ref myPrevVerts, vertCount);
		myPrevVerts[0] = myTotalVerts[0];
		myPrevVerts[1] = myTotalVerts[1];
		myPrevVerts[2] = myTotalVerts[10];
		myPrevVerts[3] = myTotalVerts[11];
		DrawLayerBorder(3);

		MyVertexHelper.AssignToMeshFilter(meshFilter);
		if (_proxyMaskMesh != null)
		{
			_proxyMaskMesh.PopulateMesh();
		}
	}

	private Vector3 GetClosestPointOnLineSegment(Vector2 target, Vector2 a, Vector2 b) //pos.z is sqrMagnitude
	{
		Vector2 line = b - a;
		float l2 = line.sqrMagnitude;
		if (l2 == 0) { return new Vector3(a.x, a.y, Vector2.Distance(target, a)); }
		Vector2 v = target - a;
		float t = Mathf.Clamp01(Vector2.Dot(target - a, line) / l2);
		Vector2 proj = a + t * line;
		return new Vector3(proj.x, proj.y, (proj - target).sqrMagnitude);
	}

	public void UpdateCounts(int layerCount, int vertCount)
	{
		if (this.layerCount == layerCount && this.vertCount == vertCount) { return; }
		this.layerCount = layerCount;
		this.vertCount = vertCount;
		triCache = null;
		tailTriCache = null;

		int x = layerCount * vertCount;
		verts = new Vector2[x];
		prevVerts = new Vector2[x];
		nextVerts = new Vector2[x];
	}
	public void AssignVert(int i, Vector2 v, Vector2 prev, Vector2 next)
	{
		verts[i] = v;
		prevVerts[i] = prev;
		nextVerts[i] = next;
	}
	public void SetDirty()
	{
		if (_shape == null) _shape = GetComponent<SuperShape>();

		sliceWidth = shape.slice.x;
		sliceHeight = shape.slice.y;

		if (!_shapeFillBarTest) { _shapeFillBar = GetComponent<SuperShapeFillBar>(); _shapeFillBarTest = true; }
		if (_shapeFillBar != null) { ComputeAndDrawFillBar(); return; }

		System.Array.Resize(ref myTotalVerts, vertCount * layerCount);

		CalculateVerts();
		isDrawingTail = false;
		for (int i = 0; i < shape.tailLayerCount; i++)
		{
			if (shape.tailMode == TailMode.NONE) { break; }
			if (i == 0) { ComputeTailData(); }
			if (!isDrawingTail) { break; }
			ComputeTailLayer(i);
		}
		if (isDrawingTail && shape.tailLayerCount > 0 && shape.isTailArrow && shape.tailMode != TailMode.Basic && shape.tailProgressPercentage > tailArrowCutoffTest && shape.tailProgressPercentage < 1)
		{
			ScaleTailArrowVerts();
		}
		if (shape.bridgeTarget != null && shape.bridgeTarget._mesh != null)
		{
			System.Array.Resize(ref myBridgeVerts, 4); //never changes size
			System.Array.Resize(ref myTotalBridgeVerts, 4 * layerCount);

			shape.bridgeTarget._mesh.CalculateVerts();
			for (int i = 0; i < layerCount; i++)
			{
				ComputeBridgeLayer(i);
			}
		}
		ComputeGutter();
		if (myMainCollider != null)
		{
			ComputeColliderVerts();
		}

		if (proxyMaskMesh != null)
		{
			//myProxy.fatherTransform = rectTransform;
			//myProxy.MatchParent(rectTransform);
			ComputeProxyVerts();
		}

		if (myMesh == null) { myMesh = new Mesh(); }
		MyVertexHelper.Clear();

		if (isLimitingOverdrawing)
		{
			DrawLayer( 0, false, true);

			System.Array.Resize(ref myPrevVerts, vertCount);
			for (int j = 0; j < vertCount; j++)
			{
				myPrevVerts[j] = myVerts[j];
			}

			for (int i = 1; i < layerCount; i++)
			{
				DrawLayerBorder(i);
			}
		}
		else
		{
			for (int i = layerCount - 1; i > -1; i--)
			{
				DrawLayer(i, i > 0, i == 0);
			}
		}
		if (isRenderingGutter)
		{
			DrawGutter();
		}
		MyVertexHelper.AssignToMeshFilter(meshFilter);
		if (_proxyMaskMesh != null)
		{
			_proxyMaskMesh.PopulateMesh();
		}

	}
	public void SetMaterial(Material m)
	{
		meshRenderer.sharedMaterial = m;
	}
	public Object GetObject()
	{
		return this;
	}
	public IProxyMaskMesh GetProxyMaskMesh()
	{
		return proxyMaskMesh;
	}

	public bool IsInsidePolygon(Vector2 p)
	{
		if (myVerts == null) { return false; }
		int j = myVerts.Length - 1;
		bool isInside = false;
		for (int i = 0; i < myVerts.Length; j = i++)
		{
			Vector2 pi = myTotalVerts[i];
			Vector2 pj = myTotalVerts[j];
			if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
			     (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
				isInside = !isInside;
		}
		return isInside;
	}
}
