using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum DMBMotionTargetTexture
{
	Texture1 = 1,
	Texture2 = 2,
}

public enum WigglePattern
{
	NONE = 0,
	INHERIT = 1,
	Basic,

}

public enum WiggleRotateOption
{
	NoRotate = 0,
	Clockwise,
	CClockwise,
	ClockwiseOuter,
	CClockwiseOuter,
}

public enum WiggleMusicOption
{
	NONE = 0,
	Basic,
}

public enum TailMode
{
	NONE = 0,
	Basic,
	BoltTapered,
	BoltTaperedHalf,
	BoltThick,
	Square,
	BoltFixed,
	Bridge,
}

public enum SuperShapeTextureCount
{
	Zero = 0,
	One = 1,
	Two = 2,
}

public enum InnerTextureMode
{
	Fixed = 0,
	MinMaxVerts = 1,
	MinMaxVertsFixedRatioMin = 2,
	MinMaxVertsFixedRatioMax = 3,
	MinMaxVertsFixedRatioX = 4,
	MinMaxVertsFixedRatioY = 5,
	SliceOnly = 10,
	ScreenSpace = -1 ,
}

public enum InnerTextureSubMode
{
	Full,
	OmitWiggle,
	OmitSlice,
}

public enum SuperShapeMaskMode
{
	NONE,
	NothingButMask,
	EverythingButMask,
	//NothingButCutoutMask,
	//EverythingButCutoutMask,
}

public enum SuperShapePreset
{
	NONE = 0,
	Triangle,
	Square,
	Rectangle,
	Parallelogram,
	Trapazoid,
	Pentagon,
	Hexagon,
	Chevron,
	Octagon,
	C,
	C7,
	Sigma,
	X,
}

public enum LayerPropagationMode
{
	NONE = 0,
	OutwardDirect,
	OutwardRadial,
	OutwardPivot,
	InwardDirect,
	InwardRadial,
	InwardPivot,
	AllDirect,
	AllRadial,
	AlternatingInverse, //inverse on layer modes is a direct inverse
	NextOnlyInverse,
}

public enum VertexPropagationMode
{
	NONE = 0,
	NONE_RecalcNeighbors,
	NextOnly,
	NextOnly_RecalcNeighbors,
	Direct,
	Radial,
	AlternatingInverse, //inverse on vertex modes is a radial inverse
	MirroredXDirect,
	MirroredYDirect,
	MirroredXScaled,
	MirroredYScaled,
}

[System.Serializable]
public class WiggleProfile
{
	public WigglePattern pattern = WigglePattern.INHERIT;
	public float speed = 1.0f;
	public Lerp lerp = Lerp.Linear;
	public WiggleRotateOption rotateOption = WiggleRotateOption.NoRotate;
	public bool isStoppingWiggleRotation = false;
	public WiggleMusicOption musicOption;
	public int musicBand;
	public float volumeRatio = 1.0f;
	public float minVolume = 0;
}

//[ExecuteAlways]
public class SuperShape : DynamicMonoBehaviour, IMaterialModifier
{
	const float DEFUALT_SUPERSHAPE_ANIM_DURATION = 0.5f;
	const float DEFUALT_SUPERSHAPE_MORPH_DURATION = 0.25f;

	public const int MIN_VERTEX_COUNT = 3;
	public const int DEFAULT_VERTEX_COUNT = 4;
	public const int MAX_VERTEX_COUNT = 15;
	public const int MIN_LAYER_COUNT = 1;
	public const int DEFAULT_LAYER_COUNT = 4;
	public const int MAX_LAYER_COUNT = 10;
	public const float DEFAULT_LAYER_WIDTH = 50;

	private float GUTTER_DOWN_SPEED = 10.00f; //percentage per second
	private float GUTTER_UP_SPEED = 10.00f; //percentage per second

	const float EDITOR_ADD_POINT_BOUNDARY_SNAP_DISTANCE = 15f;

	static Material commonMaterialNoFade;
	static Material commonMaterialFade;

	Material material;
	bool isMeshDirty;
	bool isMaterialDirty;
	bool isKeywordsDirty;

	// used for detecting when the object scale has changed so we can re-compute shader values
	Vector3 computedScale;
	Rect computedRect;
	public ISuperShapeMesh _mesh;
	public ISuperShapeMesh shapeMesh { get { return _mesh; } }

	[SerializeField] private int _vertexCount = DEFAULT_VERTEX_COUNT; //per layer
	[SerializeField] private int _layerCount = DEFAULT_LAYER_COUNT;
	public int vertexCount = DEFAULT_VERTEX_COUNT; //always use the private versions internally
	public int layerCount = DEFAULT_LAYER_COUNT;

	//variables used to store incoming resize data
	private bool isAddingVert;
	private bool isDeletingVert;
	private bool isDeleteRecalcMode; //do we recalc neighbors after a delete?
	private Vector2 vertToAdd;
	private int vertToAddIndex; //also used for Delete
	private int vertToAddLayer;

	[SerializeField] public Vector2[] baseVerts = new Vector2[DEFAULT_VERTEX_COUNT * DEFAULT_LAYER_COUNT];
	[SerializeField] public Vector2[] prevVerts = new Vector2[DEFAULT_VERTEX_COUNT * DEFAULT_LAYER_COUNT];
	[SerializeField] public Vector2[] nextVerts = new Vector2[DEFAULT_VERTEX_COUNT * DEFAULT_LAYER_COUNT];
	public Vector2[] storedVerts = new Vector2[DEFAULT_VERTEX_COUNT * DEFAULT_LAYER_COUNT];
	public Vector2[] musicDeltas = new Vector2[DEFAULT_VERTEX_COUNT * DEFAULT_LAYER_COUNT];
	public float[] wiggleProgress = new float[DEFAULT_VERTEX_COUNT * DEFAULT_LAYER_COUNT];
	public float[] wiggleDuration = new float[DEFAULT_VERTEX_COUNT * DEFAULT_LAYER_COUNT];
	public Vector2[] wiggleZones = new Vector2[DEFAULT_VERTEX_COUNT * DEFAULT_LAYER_COUNT];
	public float outerLayerProgress = 1; //when less than 1, outer-most layer is scaled inward to lower layer--for animations
	public int flattenSide = -1; //between this vert and the next, all layers should be flatted together
	public int trueFlattenSide { get { return layerCount > 1 ? flattenSide : -1; } }
	public List<int> ignoreVerts = new List<int>();
	public float ignoreRatio;
	[SerializeField] private Vector2 _slice = Vector2.zero;
	public Vector2 slice { get { return _slice; } set { _slice = value; isMeshDirty = true; } }
	public float sliceWidth  { get { return _slice.x; } set { _slice.x = value; isMeshDirty = true; } }
	public float sliceHeight { get { return _slice.y; } set { _slice.y = value; isMeshDirty = true; } }
	public bool isRectTransformedLockedToSlice = false;

	[SerializeField] private Vector2 _quadSkew = Vector2.zero;
	public Vector2 quadSkew { get { return _quadSkew; } set { _quadSkew = value; isMeshDirty = true; } }
	public Vector2 GetSkew(int i)
	{
		if (vertexCount != 4) return Vector2.zero;
		int x = i % 4;
		float l = (i / 4) * 0.41f + 1;
		if (x == 0) return new Vector2(Mathf.Min(-_quadSkew.x * l, 0), Mathf.Min(-_quadSkew.y * l, 0));
		if (x == 1) return new Vector2(Mathf.Min( _quadSkew.x * l, 0), Mathf.Max(-_quadSkew.y * l, 0));
		if (x == 2) return new Vector2(Mathf.Max( _quadSkew.x * l, 0), Mathf.Max( _quadSkew.y * l, 0));
		if (x == 3) return new Vector2(Mathf.Max(-_quadSkew.x * l, 0), Mathf.Min( _quadSkew.y * l, 0));
		return Vector2.zero;
	}

	public Vector2 editorResizeRectOffset = Vector2.zero;
	public int editorSetPivotLayer = 0;
	public int editorSetPivotVertex = 0;
	public SuperShapePreset editorSelectedPreset = SuperShapePreset.NONE;
	public bool editorPresetReset = false;

	Vector2[] storedDeleteVertexRecalcCoordinates;

	[SerializeField] private BlendModes.BlendMode _blendMode;
	public BlendModes.BlendMode blendMode { get { return _blendMode; } set { if (_blendMode == value) return; _blendMode = value; isMaterialDirty = true; } }
	[SerializeField] private SuperShapeMaskMode _maskMode;
	public SuperShapeMaskMode maskMode { get { return _maskMode; } set { if (_maskMode == value) return; _maskMode = value; isMaterialDirty = true; } }
	[SerializeField] private bool _isUnifiedGrabEnabled;
	public bool isUnifiedGrabEnabled { get { return _isUnifiedGrabEnabled; } set { if (_isUnifiedGrabEnabled == value) return; _isUnifiedGrabEnabled = value; isMaterialDirty = true; } }

	[SerializeField] private SuperShapeTextureCount _textureCount;
	public SuperShapeTextureCount textureCount { get { return _textureCount; } set { _textureCount = value; isMeshDirty = true; isMaterialDirty = true; } }
	[SerializeField] private InnerTextureMode _innerTextureMode = InnerTextureMode.Fixed;
	public InnerTextureMode innerTextureMode { get { return _innerTextureMode; } set { _innerTextureMode = value; isMeshDirty = true; isMaterialDirty = true; } }
	[SerializeField] private InnerTextureSubMode _innerTextureSubMode = InnerTextureSubMode.Full;
	public InnerTextureSubMode innerTextureSubMode { get { return _innerTextureSubMode; } set { _innerTextureSubMode = value; isMeshDirty = true; isMaterialDirty = true; } }
	[SerializeField] private Vector2 _fixedUVRange = new Vector2(500, 500);
	public Vector2 fixedUVRange { get { return _fixedUVRange; } set { _fixedUVRange = value; isMeshDirty = true; } }

	[SerializeField] private Texture2D _fillTexture1;
	public Texture2D fillTexture1 { get { return _fillTexture1; } set { if (_fillTexture1 == value) return; _fillTexture1 = value; isMaterialDirty = true; } }
	[SerializeField] private Vector2 _fillTexture1Offset = Vector2.zero;
	public Vector2 fillTexture1Offset { get { return _fillTexture1Offset; } set { if (_fillTexture1Offset == value) return; _fillTexture1Offset = value; isMaterialDirty = true; } }
	[SerializeField] private Vector2 _fillTexture1Scale = Vector2.one;
	public Vector2 fillTexture1Scale { get { return _fillTexture1Scale; } set { if (_fillTexture1Scale == value) return; _fillTexture1Scale = value; isMaterialDirty = true; } }
	[SerializeField] private Color _fillTexture1Tint = Color.white;
	public Color fillTexture1Tint { get { return _fillTexture1Tint; } set { if (_fillTexture1Tint == value) return; _fillTexture1Tint = value; isMaterialDirty = true; } }
	[SerializeField] [Range(0, 1)] private float _fillTexture1Alpha = 1;
	public float fillTexture1Alpha { get { return _fillTexture1Alpha; } set { if (_fillTexture1Alpha == value) return; _fillTexture1Alpha = value; isMaterialDirty = true; } }

	[SerializeField] private Texture2D _fillTexture2;
	public Texture2D fillTexture2 { get { return _fillTexture2; } set { if (_fillTexture2 == value) return; _fillTexture2 = value; isMaterialDirty = true; } }
	[SerializeField] private Vector2 _fillTexture2Offset = Vector2.zero;
	public Vector2 fillTexture2Offset { get { return _fillTexture2Offset; } set { if (_fillTexture2Offset == value) return; _fillTexture2Offset = value; isMaterialDirty = true; } }
	[SerializeField] private Vector2 _fillTexture2Scale = Vector2.one;
	public Vector2 fillTexture2Scale { get { return _fillTexture2Scale; } set { if (_fillTexture2Scale == value) return; _fillTexture2Scale = value; isMaterialDirty = true; } }
	[SerializeField] private Color _fillTexture2Tint = Color.white;
	public Color fillTexture2Tint { get { return _fillTexture2Tint; } set { if (_fillTexture2Tint == value) return; _fillTexture2Tint = value; isMaterialDirty = true; } }
	[SerializeField] [Range(0, 1)] private float _fillTexture2Alpha = 1;
	public float fillTexture2Alpha { get { return _fillTexture2Alpha; } set { if (_fillTexture2Alpha == value) return; _fillTexture2Alpha = value; isMaterialDirty = true; } }

	[SerializeField]
	private Color[] _layerColors = new Color[4] { Color.white, Color.white, Color.white, Color.white };
	public Color[] layerColors { get { return _layerColors; } set { _layerColors = value; isMeshDirty = true; } }

	public WiggleProfile[] wiggleProfiles = new WiggleProfile[DEFAULT_LAYER_COUNT];
	public int[] wiggleRotationStep = new int[DEFAULT_LAYER_COUNT] { 0, 0, 0, 0 }; //, 0, 0, 0, 0, 0 };
	public bool isWigglePaused;
	public bool isWigglingInEditor;

	[SerializeField] public Transform tailTipTransform;
	[SerializeField] public Vector2 tailTip;
	public bool isTailTipFixed;
	public bool isTailBaseFixed; //if not, calculate the closest point on all segments - cornerAvoidance from both ends
	public int tailFixedBaseIndex; //only for fixed, used to calcualte pos
	[SerializeField] float tailFixedBaseUnitProgress; //only for fixed, used to calculate pos
	public int tailBannedSideIndex = -1;
	[Min(0)] public float tailBaseWidth = 100;
	public float tailBaseCornerAvoidance; //only for non-fixed
	public float tailMaxLength = 1000;
	public float tailExtraLength = -50;
	[Min(0)] public int tailMinLayer = 0;
	public int tailMaxLayer = DEFAULT_LAYER_COUNT;
	public int tailLayerCount { get { return tailMode == TailMode.NONE ? 0 : Mathf.Min(layerCount, tailMaxLayer); } }
	public TailMode tailMode = TailMode.NONE;
	public float tailModeParam = 0;
	public Vector2 tailTipWiggle;
	public Vector2 tailBaseWiggleL;
	public Vector2 tailBaseWiggleR;
	public float tailTipWiggleValueA; //in-line
	public float tailTipWiggleValueB; //orth\
	public float tailWiggleMultiplier = 1f;
	public float tailWiggleProgress;
	public float tailWiggleDuration;
	[Min(1)] public float tailLayerThickness = 25f;
	[Min(5f)] public float tailBoltSegmentDistance = 150f;
	[Range(0,1f)] public float tailProgressPercentage = 1.0f;
	public bool isTailArrow = false;
	public float tailArrowSize = 50f;
	public bool isTailFlipped = false;

	[SerializeField] public SuperShape bridgeTarget;
	[SerializeField] public RectTransform bridgeTargetRectPosDriver;
	[SerializeField] public int bridgeMySideIndex;
	[SerializeField] public int bridgeTargetSideIndex;
	[SerializeField] public float bridgeLayerInnerWidth;
	[SerializeField] public float bridgeLayerOuterWidth;
	[SerializeField, Range(0, 1000)] public float bridgeProgressA = 0.5f; //location of bridge on my side [0,1]
	[SerializeField, Range(0, 1000)] public float bridgeProgressB = 0.5f; //location of bridge on target side [0,1]-- remember, will usually be counter-cyclical
	[SerializeField, Range(0, 2)] public float bridgeProgressC = 1;   //width of bridge [0,1]
	[SerializeField, Range(-1.00f, 1.00f)] public float bridgeProgressD = 1;   //length of bridge [0,1]
	[SerializeField] public bool isBridgeOffsetsInverted = false;

	public bool isGutter = false;
	public bool isGutter3D = false;
	public bool isGutter3DSameSide = false;
	[Min(1)] public float gutterLineThickness = 20f;
	[Min(0)] public int gutterSide = 3;
	public Vector2 gutterTotalMovement = new Vector2(0,-70f);
	public Transform gutterContainer;
	private float gutterInnerMovementPercentage = 0f; //apply a lerp to this as we see fit
	public float gutterCurrentMovementPercentage = 0f;
	[NonSerialized] public bool isClickedGutterDescending = false;

	public SuperShape sliceHost; //what (inactive) sister SuperShape object is waiting to host the second half when we split?
	[SerializeField] int savedSliceValues = 0; //check this to see if the following values are valid, assigned data
	[SerializeField] int[] savedSliceIndexes = new int[2];
	[SerializeField] float[] savedSliceRatio = new float[2];

	[SerializeField] private bool _isRespectingTintGroup = false;
	public bool isRespectingTintGroup { get { return _isRespectingTintGroup; } set { _isRespectingTintGroup = value; isMaterialDirty = true; } }

	public readonly static Dictionary<SuperShapePreset, Vector2[]> presetData = new Dictionary<SuperShapePreset, Vector2[]>()
	{
		{ SuperShapePreset.Triangle,      new Vector2[3] { new Vector2(-200.00f, -115.47f), new Vector2(   0.00f,  230.94f), new Vector2( 200.00f, -115.47f) } },
		{ SuperShapePreset.Square,        new Vector2[4] { new Vector2(-150.00f, -150.00f), new Vector2(-150.00f,  150.00f), new Vector2( 150.00f,  150.00f), new Vector2( 150.00f, -150.00f) } },
		{ SuperShapePreset.Rectangle,     new Vector2[4] { new Vector2(-200.00f, -100.00f), new Vector2(-200.00f,  100.00f), new Vector2( 200.00f,  100.00f), new Vector2( 200.00f, -100.00f) } },
		{ SuperShapePreset.Parallelogram, new Vector2[4] { new Vector2(-250.00f, -100.00f), new Vector2(-150.00f,  100.00f), new Vector2( 250.00f,  100.00f), new Vector2( 150.00f, -100.00f) } },
		{ SuperShapePreset.Trapazoid,     new Vector2[4] { new Vector2(-250.00f, -100.00f), new Vector2(-150.00f,  100.00f), new Vector2( 150.00f,  100.00f), new Vector2( 250.00f, -100.00f) } },
		{ SuperShapePreset.Pentagon,      new Vector2[5] { new Vector2(-146.95f, -202.25f), new Vector2(-237.76f,   77.25f), new Vector2(   0.00f,  250.00f), new Vector2( 237.76f,   77.25f), new Vector2( 146.95f, -202.25f) } },
		{ SuperShapePreset.Hexagon,       new Vector2[6] { new Vector2(-200.00f,    0.00f), new Vector2(-100.00f,  173.21f), new Vector2( 100.00f,  173.21f),
		                                                   new Vector2( 200.00f,    0.00f), new Vector2( 100.00f, -173.21f), new Vector2(-100.00f, -173.21f)} },
		{ SuperShapePreset.Chevron,       new Vector2[6] { new Vector2(-200.00f, -150.00f), new Vector2(-200.00f,   50.00f), new Vector2(   0.00f,  150.00f),
		                                                  new Vector2( 200.00f,   50.00f), new Vector2( 200.00f, -150.00f), new Vector2(   0.00f,  -50.00f)} },
		{ SuperShapePreset.Octagon,       new Vector2[8] { new Vector2(-300.00f, -100.00f), new Vector2(-300.00f,  100.00f), new Vector2(-100.00f,  300.00f), new Vector2( 100.00f,  300.00f),
		                                                   new Vector2( 300.00f,  100.00f), new Vector2( 300.00f, -100.00f), new Vector2( 100.00f, -300.00f), new Vector2(-100.00f, -300.00f)} },
		{ SuperShapePreset.C,   new Vector2[8] { new Vector2(-400.00f, -400.00f), new Vector2(-400.00f,  400.00f), new Vector2( 400.00f,  400.00f), new Vector2( 400.00f,  350.00f),
		                                         new Vector2(-350.00f,  350.00f), new Vector2(-350.00f, -350.00f), new Vector2( 400.00f, -350.00f), new Vector2( 400.00f, -400.00f)} },
		{ SuperShapePreset.C7,   new Vector2[7] { new Vector2(-400.00f, -400.00f), new Vector2(-400.00f,  400.00f), new Vector2( 400.00f,  400.00f), new Vector2( 400.00f,  350.00f),
		                                          new Vector2(-200.00f,    0.00f), new Vector2( 400.00f, -350.00f), new Vector2( 400.00f, -400.00f)} },
		{ SuperShapePreset.Sigma,   new Vector2[8] { new Vector2(-400.00f, -400.00f), new Vector2(-150.00f,    0.00f), new Vector2(-400.00f,  400.00f), new Vector2( 400.00f,  400.00f), new Vector2( 400.00f,  350.00f),
		                                             new Vector2(150.00f,    0.00f), new Vector2( 400.00f, -350.00f), new Vector2( 400.00f, -400.00f)} },
		{ SuperShapePreset.X,   new Vector2[8] { new Vector2(-400.00f, -400.00f), new Vector2(-250.00f,    0.00f), new Vector2(-400.00f,  400.00f), new Vector2(   0.00f,  250.00f),
		                                         new Vector2( 400.00f,  400.00f), new Vector2( 250.00f,    0.00f), new Vector2( 400.00f, -400.00f), new Vector2(   0.00f, -250.00f)} },
	};

	public void ResizeDataCheck() { if (layerCount != _layerCount || vertexCount != _vertexCount) { ResizeData(); } }
	public void ResizeData()
	{
		if (vertexCount < MIN_VERTEX_COUNT) { vertexCount = MIN_VERTEX_COUNT; }
		else if (vertexCount > MAX_VERTEX_COUNT) { vertexCount = MAX_VERTEX_COUNT; }
		if (layerCount < MIN_LAYER_COUNT) { layerCount = MIN_LAYER_COUNT; }
		else if (layerCount > MAX_LAYER_COUNT) { layerCount = MAX_LAYER_COUNT; }
		if (vertexCount == _vertexCount && layerCount == _layerCount) { return; }

		//if the initial state is borked, we shouldn't even try to copy data
		if (baseVerts.Length != _vertexCount * _layerCount)
		{
			baseVerts = new Vector2[vertexCount * layerCount];
			prevVerts = new Vector2[vertexCount * layerCount];
			nextVerts = new Vector2[vertexCount * layerCount];
			wiggleZones = new Vector2[vertexCount * layerCount];
			_vertexCount = vertexCount;
			_layerCount = layerCount;
		}

		Vector2[] tempBaseVerts = new Vector2[baseVerts.Length];
		Vector2[] tempPrevVerts = new Vector2[baseVerts.Length];
		Vector2[] tempNextVerts = new Vector2[baseVerts.Length];

		//first, add new default values for vertexes if needed
		if (vertexCount != _vertexCount)
		{
			Vector2[] newBaseVerts = new Vector2[vertexCount * _layerCount];
			Vector2[] newPrevVerts = new Vector2[vertexCount * _layerCount];
			Vector2[] newNextVerts = new Vector2[vertexCount * _layerCount];
			Vector2[] newWiggleZones = new Vector2[vertexCount * _layerCount];

			if (vertexCount == _vertexCount + 1)
			{
				float newVertWeightRatio = 0.5f; //new verts will be between previous verts n and n+1; how far between them is this amount 
				int j1 = _vertexCount - 1;
				int j2 = 0;
				if (isAddingVert)
				{
					j1 = vertToAddIndex;
					if (j1 < _vertexCount - 1) { j2 = j1 + 1; } //else leave at 0
					Vector2 p1 = baseVerts[vertToAddLayer * _vertexCount + j1];
					Vector2 p2 = baseVerts[vertToAddLayer * _vertexCount + j2];
					Vector2 lineDelta = p2 - p1;
					Vector2 proj = Vector2.Dot(lineDelta, vertToAdd - p1) / lineDelta.sqrMagnitude * lineDelta;
					newVertWeightRatio = proj.magnitude / lineDelta.magnitude;
				}
				for (int i = 0; i < _layerCount; i++)
				{
					for (int j = 0; j < vertexCount; j++)
					{
						if (j <= j1)
						{
							newBaseVerts[i * vertexCount + j] = baseVerts[i * _vertexCount + j];
							newPrevVerts[i * vertexCount + j] = prevVerts[i * _vertexCount + j];
							newNextVerts[i * vertexCount + j] = nextVerts[i * _vertexCount + j];
							newWiggleZones[i * vertexCount + j] = wiggleZones[i * _vertexCount + j];
						}
						else if (j == j1 + 1)
						{
							Vector2 newBaseVert = baseVerts[i * _vertexCount + j1] * (1 - newVertWeightRatio) + baseVerts[i * _vertexCount + j2] * newVertWeightRatio;
							Vector2 newPrevVert = prevVerts[i * _vertexCount + j1] * (1 - newVertWeightRatio) + prevVerts[i * _vertexCount + j2] * newVertWeightRatio;
							Vector2 newNextVert = nextVerts[i * _vertexCount + j1] * (1 - newVertWeightRatio) + nextVerts[i * _vertexCount + j2] * newVertWeightRatio;
							if (isAddingVert)
							{
								if (vertToAdd.x == 0 && sliceWidth  > 0) { newBaseVert.x = 0; newPrevVert.x = 0; newNextVert.x = 0; }
								if (vertToAdd.y == 0 && sliceHeight > 0) { newBaseVert.y = 0; newPrevVert.y = 0; newNextVert.y = 0; }
							}
							newBaseVerts[i * vertexCount + j] = newBaseVert;
							newPrevVerts[i * vertexCount + j] = newPrevVert;
							newNextVerts[i * vertexCount + j] = newNextVert;
							newWiggleZones[i * vertexCount + j] = wiggleZones[i * _vertexCount + j1]; //old wiggle zone is fine, no better alternative
						}
						else
						{
							newBaseVerts[i * vertexCount + j] = baseVerts[i * _vertexCount + j - 1];
							newPrevVerts[i * vertexCount + j] = prevVerts[i * _vertexCount + j - 1];
							newNextVerts[i * vertexCount + j] = nextVerts[i * _vertexCount + j - 1];
							newWiggleZones[i * vertexCount + j] = wiggleZones[i * _vertexCount + j - 1];
						}
					}
				}
				isAddingVert = false;
			}
			else if (vertexCount == _vertexCount - 1)
			{
				int j1 = vertexCount - 1;
				if (isDeletingVert)
				{
					j1 = vertToAddIndex;
				}
				for (int i = 0; i < _layerCount; i++)
				{
					for (int j = 0; j < vertexCount; j++)
					{
						if (j < j1)
						{
							newBaseVerts[i * vertexCount + j] = baseVerts[i * _vertexCount + j];
							newPrevVerts[i * vertexCount + j] = prevVerts[i * _vertexCount + j];
							newNextVerts[i * vertexCount + j] = nextVerts[i * _vertexCount + j];
							newWiggleZones[i * vertexCount + j] = wiggleZones[i * _vertexCount + j];
						}
						else
						{
							newBaseVerts[i * vertexCount + j] = baseVerts[i * _vertexCount + j + 1];
							newPrevVerts[i * vertexCount + j] = prevVerts[i * _vertexCount + j + 1];
							newNextVerts[i * vertexCount + j] = nextVerts[i * _vertexCount + j + 1];
							newWiggleZones[i * vertexCount + j] = wiggleZones[i * _vertexCount + j + 1];
						}
					}
				}
			}
			else if (vertexCount < _vertexCount) //removing more than one--let's just assume they already filtered their data and copy it
			{
				for (int i = 0; i < _layerCount; i++)
				{
					for (int j = 0; j < vertexCount; j++)
					{
						newBaseVerts[i * vertexCount + j] = baseVerts[i * _vertexCount + j];
						newPrevVerts[i * vertexCount + j] = prevVerts[i * _vertexCount + j];
						newNextVerts[i * vertexCount + j] = nextVerts[i * _vertexCount + j];
						newWiggleZones[i * vertexCount + j] = wiggleZones[i * _vertexCount + j];
					}
				}
			}
			else //adding more than one
			{

			}
			tempBaseVerts = newBaseVerts;
			tempPrevVerts = newPrevVerts;
			tempNextVerts = newNextVerts;
			wiggleZones = newWiggleZones;
		}
		else
		{
			baseVerts.CopyTo(tempBaseVerts, 0);
			prevVerts.CopyTo(tempPrevVerts, 0);
			nextVerts.CopyTo(tempNextVerts, 0);
		}

		//second, add or remove layers as needed--we only support added or removing outer-most layers
		if (layerCount != _layerCount)
		{
			Vector2[] newBaseVerts = new Vector2[vertexCount * layerCount];
			Vector2[] newPrevVerts = new Vector2[vertexCount * layerCount];
			Vector2[] newNextVerts = new Vector2[vertexCount * layerCount];
			Vector2[] newWiggleZones = new Vector2[vertexCount * layerCount];

			if (_layerCount == 0 || layerCount == 0) //increase from nothing -- assume some debug thing will overwrite this and don't care about what we assign
			{
				baseVerts.CopyTo(newBaseVerts, 0);
				prevVerts.CopyTo(newPrevVerts, 0);
				nextVerts.CopyTo(newNextVerts, 0);
				wiggleZones.CopyTo(newWiggleZones, 0);
			}
			else if (_layerCount == 1) //increase from one -- add some debug vector from center
			{
				//copy base layer data first
				for (int j = 0; j < vertexCount; j++)
				{
					newBaseVerts[j] = tempBaseVerts[j];
					newPrevVerts[j] = tempPrevVerts[j];
					newNextVerts[j] = tempNextVerts[j];
					newWiggleZones[j] = wiggleZones[j];
				}
				//make layer 1
				for (int j = 0; j < vertexCount; j++)
				{
					int j0 = j == 0 ? vertexCount - 1 : j - 1;
					int j2 = j == vertexCount - 1 ? 0 : j + 1;
					Vector2 p0 = tempBaseVerts[j0];
					Vector2 p = tempBaseVerts[j];
					Vector2 p2 = tempBaseVerts[j2];
					Vector2 s01 = p - p0;
					Vector2 s12 = p2 - p;
					Vector2 s01orth = new Vector2(-s01.y, s01.x).normalized * DEFAULT_LAYER_WIDTH;
					Vector2 s12orth = new Vector2(-s12.y, s12.x).normalized * DEFAULT_LAYER_WIDTH;
					Vector2 x = LineSegmentsIntersection(p0 + s01orth, p + s01orth, p + s12orth, p2 + s12orth);
					newBaseVerts[vertexCount + j] = x;
					newPrevVerts[vertexCount + j] = x;
					newNextVerts[vertexCount + j] = x;
					newWiggleZones[vertexCount + j] = wiggleZones[j];
				}
				_layerCount++;

				while (_layerCount < layerCount) //make new layers
				{
					for (int j = 0; j < vertexCount; j++)
					{
						newBaseVerts[_layerCount * vertexCount + j] = tempBaseVerts[j] * (_layerCount + 2) / 2;
						newPrevVerts[_layerCount * vertexCount + j] = tempPrevVerts[j] * (_layerCount + 2) / 2;
						newNextVerts[_layerCount * vertexCount + j] = tempNextVerts[j] * (_layerCount + 2) / 2;
						newWiggleZones[_layerCount * vertexCount + j] = wiggleZones[j];
					}
					_layerCount++;
				}
			}
			else if (_layerCount < layerCount) //increase
			{
				for (int i = 0; i < _layerCount; i++) //copy data first
				{
					for (int j = 0; j < vertexCount; j++)
					{
						newBaseVerts[i * vertexCount + j] = tempBaseVerts[i * vertexCount + j];
						newPrevVerts[i * vertexCount + j] = tempPrevVerts[i * vertexCount + j];
						newNextVerts[i * vertexCount + j] = tempNextVerts[i * vertexCount + j];
						newWiggleZones[i * vertexCount + j] = wiggleZones[i * vertexCount + j];
					}
				}
				int originalLayerCount = _layerCount;
				while (_layerCount < layerCount)
				{
					for (int j = 0; j < vertexCount; j++) //make new layers
					{
						newBaseVerts[_layerCount * vertexCount + j] = 2 * tempBaseVerts[(originalLayerCount - 1) * vertexCount + j] - tempBaseVerts[(originalLayerCount - 2) * vertexCount + j];
						newPrevVerts[_layerCount * vertexCount + j] = 2 * tempPrevVerts[(originalLayerCount - 1) * vertexCount + j] - tempPrevVerts[(originalLayerCount - 2) * vertexCount + j];
						newNextVerts[_layerCount * vertexCount + j] = 2 * tempNextVerts[(originalLayerCount - 1) * vertexCount + j] - tempNextVerts[(originalLayerCount - 2) * vertexCount + j];
						newWiggleZones[_layerCount * vertexCount + j] = wiggleZones[(originalLayerCount - 1) * vertexCount + j];
					}
					_layerCount++;
				}
			}
			else //decrease -- simple manual copy of data in range
			{
				for (int i = 0; i < layerCount; i++)
				{
					for (int j = 0; j < vertexCount; j++)
					{
						newBaseVerts[i * vertexCount + j] = tempBaseVerts[i * vertexCount + j];
						newPrevVerts[i * vertexCount + j] = tempPrevVerts[i * vertexCount + j];
						newNextVerts[i * vertexCount + j] = tempNextVerts[i * vertexCount + j];
						newWiggleZones[i * vertexCount + j] = wiggleZones[i * vertexCount + j];
					}
				}
			}
			tempBaseVerts = newBaseVerts;
			tempPrevVerts = newPrevVerts;
			tempNextVerts = newNextVerts;
			wiggleZones = newWiggleZones;
		}

		if (isDeletingVert) //finally, handle recalc data for deletes at end
		{
			isDeletingVert = false;
			if (isDeleteRecalcMode)
			{
				AddRecalcDelta(tempBaseVerts, vertToAddIndex, 0, true);
			}
			else
			{
				/*
				storedDeleteVertexRecalcCoordinates = new Vector2[tempBaseVerts.Length];
				tempBaseVerts.CopyTo(storedDeleteVertexRecalcCoordinates, 0);
				AddRecalcDelta(storedDeleteVertexRecalcCoordinates, vertToAddIndex, 0, true);
				*/
			}
		}

		_layerCount = layerCount;
		_vertexCount = vertexCount;
		baseVerts = tempBaseVerts;
		prevVerts = tempPrevVerts;
		nextVerts = tempNextVerts;

		//finally, resize the umimportant stuff that will be written over by other fuctions as long as they are properly sized = layerCount * vertexCount
		int fullSize = _vertexCount * _layerCount;
		System.Array.Resize(ref musicDeltas, fullSize);
		System.Array.Resize(ref wiggleProgress, fullSize);
		System.Array.Resize(ref wiggleDuration, fullSize);
		System.Array.Resize(ref wiggleZones, fullSize);

		//resize the following arrays by layerCount
		System.Array.Resize(ref wiggleProfiles, layerCount);
		for (int i = 0; i < layerCount; i++)
		{
			if (wiggleProfiles[i] == null) { wiggleProfiles[i] = new WiggleProfile(); }
		}
		System.Array.Resize(ref wiggleRotationStep, layerCount);

		Color[] newColors = new Color[layerCount];
		for (int i = 0; i < layerCount; i++)
		{
			newColors[i] = i < layerColors.Length ? layerColors[i] : defaultLayerColors[i];
		}
		layerColors = newColors;
		isMeshDirty = true;
	}

	private readonly static Color[] defaultLayerColors = new Color[8] { Color.white, Color.black, Color.white, Color.blue, Color.green, Color.red, Color.yellow, Color.cyan };

	public void AddVertexAuto()
	{
		vertexCount++;
		ResizeData();
	}
	public void AddVertex(Vector2 newPoint, int fullVertexIndex, bool isPreAdjusted = false)
	{
		if (isPreAdjusted)
		{
			vertToAdd = newPoint;
		}
		else
		{
			vertToAdd = new Vector2((newPoint.x - position.x) / (transform.lossyScale.x), (newPoint.y - position.y) / (transform.lossyScale.y));
		}
		if (vertToAdd.x != 0 && Mathf.Abs(vertToAdd.x) <= sliceWidth / 2)
		{
			if (Mathf.Abs(vertToAdd.x) < EDITOR_ADD_POINT_BOUNDARY_SNAP_DISTANCE)
			{
				vertToAdd.x = 0;
			}
			else
			{
				Debug.LogWarning("Cannot place point inside x-range of inner 9-slice boundary. (Except at 0)");
				return;
			}
		}
		else if (vertToAdd.x > 0) { vertToAdd.x -= sliceWidth / 2; }
		else if (vertToAdd.x < 0) { vertToAdd.x += sliceWidth / 2; }
		if (vertToAdd.y != 0 && Mathf.Abs(vertToAdd.y) <= sliceHeight / 2)
		{
			if (Mathf.Abs(vertToAdd.y) < EDITOR_ADD_POINT_BOUNDARY_SNAP_DISTANCE)
			{
				vertToAdd.y = 0;
			}
			else
			{
				Debug.LogWarning("Cannot place point inside y-range of inner 9-slice boundary. (Except at 0)");
				return;
			}
		}
		else if (vertToAdd.y > 0) { vertToAdd.y -= sliceHeight / 2; }
		else if (vertToAdd.y < 0) { vertToAdd.y += sliceHeight / 2; }
		isAddingVert = true;
		isDeletingVert = false;
		vertToAddIndex = fullVertexIndex % vertexCount;
		vertToAddLayer = fullVertexIndex / vertexCount;
		vertexCount++;
		ResizeData();
		UpdateMesh();
	}
	public void RemoveVertex(int fullVertexIndex, bool isRecalc = false)
	{
		isAddingVert = false;
		isDeletingVert = true;
		isDeleteRecalcMode = isRecalc;
		vertToAddIndex = fullVertexIndex % vertexCount;
		vertToAddLayer = fullVertexIndex / vertexCount;
		vertexCount--;
		ResizeData();
		UpdateMesh();
	}
	public void ConvertFakeOctagonToSquare()
	{
		for (int i = 0; i < layerCount; i++ )
		{
			for (int j = 1; j < 4; j++)
			{
				baseVerts[i * vertexCount + j] = baseVerts[i * vertexCount + 2 * j];
				prevVerts[i * vertexCount + j] = prevVerts[i * vertexCount + 2 * j];
				nextVerts[i * vertexCount + j] = nextVerts[i * vertexCount + 2 * j];
				wiggleZones[i * vertexCount + j] = wiggleZones[i * vertexCount + 2 * j];
			}
		}
		vertexCount = 4;
		ResizeData();
	}

	public Vector2[] GetVertsFromTranslationTo(int vertexIndex, int layerIndex, Vector2 targetPos, LayerPropagationMode layerMode, VertexPropagationMode vertexMode)
	{
		if (vertexIndex >= vertexCount) { Debug.LogError("Invalid vertex index: " + vertexIndex); return null; }
		if (layerIndex  >= layerCount ) { Debug.LogError("Invalid layer index: "  + layerIndex);  return null; }
		Vector2 delta = targetPos - baseVerts[layerIndex * vertexCount + vertexIndex];
		return GetVertsFromTranslation(vertexIndex, layerIndex, delta, layerMode, vertexMode);
	}
	public Vector2[] GetVertsFromTranslation(int fullVertexIndex, Vector2 delta, LayerPropagationMode layerMode, VertexPropagationMode vertexMode)
	{
		int vertexIndex = fullVertexIndex % vertexCount;
		int layerIndex = fullVertexIndex / vertexCount;
		return GetVertsFromTranslation(vertexIndex, layerIndex, delta, layerMode, vertexMode);
	}
	public Vector2[] GetVertsFromTranslation(int vertexIndex, int layerIndex, Vector2 delta, LayerPropagationMode layerMode, VertexPropagationMode vertexMode)
	{
		if (vertexIndex >= vertexCount) { Debug.LogError("Invalid vertex index: " + vertexIndex); return null; }
		if (layerIndex  >= layerCount ) { Debug.LogError("Invalid layer index: "  + layerIndex);  return null; }
		if (layerMode == LayerPropagationMode.OutwardPivot && layerIndex == layerCount - 1) { layerMode = LayerPropagationMode.NONE; }
		if (layerMode == LayerPropagationMode.InwardPivot  && layerIndex == 0)              { layerMode = LayerPropagationMode.NONE; }

		int j2 = vertexIndex == vertexCount - 1 ? 0 : vertexIndex + 1;

		Vector2[] myVerts = storedVerts != null && storedVerts.Length > 0 ? storedVerts : baseVerts;
		Vector2[] result = new Vector2[myVerts.Length];
		Vector2 vert = myVerts[layerIndex * vertexCount + vertexIndex];
		Vector2 target = vert + delta;
		Vector2 pivot = Vector2.zero;
		if      (layerMode == LayerPropagationMode.InwardPivot)  { pivot = myVerts[vertexIndex]; }
		else if (layerMode == LayerPropagationMode.OutwardPivot) { pivot = myVerts[(layerCount - 1) * vertexCount + vertexIndex]; }
		Vector2 vertToPivot = vert - pivot;
		Vector2 targetToPivot = target - pivot;
		float magDelta = targetToPivot.magnitude / vertToPivot.magnitude;
		float oldAngle = Mathf.Atan2(vertToPivot.y, vertToPivot.x);
		float newAngle = Mathf.Atan2(targetToPivot.y, targetToPivot.x);
		float angleDelta = newAngle - oldAngle;
		float sin = Mathf.Sin(angleDelta);
		float cos = Mathf.Cos(angleDelta);

		int minLayer = 0;
		if (layerMode == LayerPropagationMode.NONE || layerMode == LayerPropagationMode.NextOnlyInverse ||
		    layerMode == LayerPropagationMode.OutwardDirect || layerMode == LayerPropagationMode.OutwardRadial ||
		    layerMode == LayerPropagationMode.OutwardPivot) { minLayer = layerIndex; }
		int maxLayer = layerCount - 1;
		if (layerMode == LayerPropagationMode.NONE ||
		    layerMode == LayerPropagationMode.InwardDirect || layerMode == LayerPropagationMode.InwardRadial ||
		    layerMode == LayerPropagationMode.InwardPivot) { maxLayer = layerIndex; }
		if (layerMode == LayerPropagationMode.NextOnlyInverse && layerIndex + 1 < layerCount) { maxLayer = layerIndex + 1; }
		bool isLayerRadial = (layerMode == LayerPropagationMode.OutwardRadial || layerMode == LayerPropagationMode.InwardRadial ||
		                      layerMode == LayerPropagationMode.AllRadial ||
		                      layerMode == LayerPropagationMode.OutwardPivot || layerMode == LayerPropagationMode.InwardPivot);
		bool isLayersInverting = layerMode == LayerPropagationMode.AlternatingInverse || layerMode == LayerPropagationMode.NextOnlyInverse;

		int minVertex = 0;
		int maxVertex = vertexCount - 1;
		int specialVertex = -1;
		if (vertexMode == VertexPropagationMode.NONE || vertexMode == VertexPropagationMode.NONE_RecalcNeighbors) { minVertex = vertexIndex; maxVertex = vertexIndex; }
		if (vertexMode == VertexPropagationMode.NextOnly || vertexMode == VertexPropagationMode.NextOnly_RecalcNeighbors) { minVertex = vertexIndex; maxVertex = vertexIndex; specialVertex = j2; }
		bool isVertexRadial = vertexMode == VertexPropagationMode.Radial || vertexMode == VertexPropagationMode.AlternatingInverse;
		bool isVertexInverting = vertexMode == VertexPropagationMode.AlternatingInverse;
		bool isVertexMirrorScaled = vertexMode == VertexPropagationMode.MirroredXScaled || vertexMode == VertexPropagationMode.MirroredYScaled;
		
		for (int i = 0; i < layerCount; i++)
		{
			bool isLayerInverse = isLayersInverting && (i - layerIndex) % 2 == 1; //odd layer relative to base layerIndex
			for (int j = 0; j < vertexCount; j++)
			{
				Vector2 oldVert = myVerts[i * vertexCount + j];
				if (i < minLayer || i > maxLayer || (j != specialVertex && (j < minVertex || j > maxVertex)))
				{
					result[i * vertexCount + j] = oldVert;
					continue;
				}
				Vector2 oldBaseVert = myVerts[layerIndex * vertexCount + j]; //the vertex on the index layer
				Vector2 myPivot = pivot;
				if      (layerMode == LayerPropagationMode.InwardPivot)  { myPivot = myVerts[j]; }
				else if (layerMode == LayerPropagationMode.OutwardPivot) { myPivot = myVerts[(layerCount - 1) * vertexCount + j]; }
				Vector2 oldBaseVertToPivot = oldBaseVert - myPivot;
				Vector2 oldVertToPivot = oldVert - myPivot;
				float baseFullAngle = Mathf.Atan2(oldBaseVert.y, oldBaseVert.x);
				float baseFullPivotAngle = Mathf.Atan2(oldBaseVertToPivot.y, oldBaseVertToPivot.x);
				float layerFullPivotAngle = Mathf.Atan2(oldVertToPivot.y, oldVertToPivot.x);
				Vector2 myDelta = delta;

				if (isVertexRadial)
				{
					float vertexAngleDelta = baseFullAngle - oldAngle;
					float vSin = Mathf.Sin(vertexAngleDelta);
					float vCos = Mathf.Cos(vertexAngleDelta);
					myDelta = new Vector2(vCos * delta.x - vSin * delta.y, vCos * delta.y + vSin * delta.x);
					if (vert.magnitude != 0)
					{
						myDelta *= oldBaseVert.magnitude / vert.magnitude;
					}
				}
				if (isLayerRadial)
				{
					float layerAngleDelta = layerFullPivotAngle - (isVertexRadial ? baseFullAngle : baseFullPivotAngle); //don't know why this works, just run with it
					float lSin = Mathf.Sin(layerAngleDelta);
					float lCos = Mathf.Cos(layerAngleDelta);
					myDelta = new Vector2(lCos * myDelta.x - lSin * myDelta.y, lCos * myDelta.y + lSin * myDelta.x);
					if (oldBaseVertToPivot.magnitude != 0)
					{
						myDelta *= oldVertToPivot.magnitude / oldBaseVertToPivot.magnitude;
					}
				}
				if (isLayerInverse) { myDelta *= -1; }
				if (isVertexInverting && (j - vertexIndex) % 2 == 1) { myDelta *= -1; }
				if (vertexMode == VertexPropagationMode.MirroredXDirect || vertexMode == VertexPropagationMode.MirroredXScaled)
				{
					if (vert.y * oldVert.y == 0) { myDelta.y = 0; }
					else if (vert.y * oldVert.y < 0) { myDelta.y *= -1; }
	}
				if (vertexMode == VertexPropagationMode.MirroredYDirect || vertexMode == VertexPropagationMode.MirroredYScaled)
				{
					if (vert.x * oldVert.x == 0) { myDelta.x = 0; }
					else if (vert.x * oldVert.x < 0) { myDelta.x *= -1; }
				}
				if (vertexMode == VertexPropagationMode.MirroredXScaled) { myDelta.y *= oldVert.y / vert.y; }
				if (vertexMode == VertexPropagationMode.MirroredYScaled) { myDelta.x *= oldVert.x / vert.x; }

				result[i * vertexCount + j] = oldVert + myDelta;
				if (target.x == 0 && j == vertexIndex && sliceHeight > 0) { result[i * vertexCount + j].x = 0; }
				if (target.y == 0 && j == vertexIndex && sliceWidth  > 0) { result[i * vertexCount + j].y = 0; }
			}
		}
		if (vertexMode == VertexPropagationMode.NONE_RecalcNeighbors)
		{
			AddRecalcDelta(result, vertexIndex, layerIndex, false, 0);
		}
		else if (vertexMode == VertexPropagationMode.NextOnly_RecalcNeighbors)
		{
			AddRecalcDelta(result, vertexIndex, layerIndex, false, 1);
		}
		return result;
	}

	public void SetVertsFromTranslationToWorldPos(int fullVertexIndex, Vector3 worldPos,
	                                              LayerPropagationMode layerMode, VertexPropagationMode vertexMode)
	{
		if (storedVerts == null || storedVerts.Length == 0)
		{
			storedVerts = new Vector2[baseVerts.Length];
			baseVerts.CopyTo(storedVerts, 0);
		}
		int vertexIndex = fullVertexIndex % vertexCount;
		int layerIndex = fullVertexIndex / vertexCount; if (vertexIndex >= vertexCount) { Debug.LogError("Invalid vertex index: " + vertexIndex); return; }
		if (layerIndex >= layerCount) { Debug.LogError("Invalid layer index: " + layerIndex); return; }
		Vector2 targetPos = GetLocalVertFromWorld(worldPos);
		Vector2 delta = targetPos - storedVerts[fullVertexIndex];
		Vector2[] newBaseVerts = GetVertsFromTranslation(vertexIndex, layerIndex, delta, layerMode, vertexMode);
		for (int i = 0; i < baseVerts.Length; i++)
		{
			baseVerts[i] = newBaseVerts[i];
			prevVerts[i] = newBaseVerts[i];
			nextVerts[i] = newBaseVerts[i];
		}
		UpdateMesh();
	}

	public void AddRecalcDelta(Vector2[] newBaseVerts, int vertexCenterIndex, int exemptLayerIndex, bool isDelete, int extraOffset = 0)
	{
		// when a single, specific vertex is moved or removed, we may want to adjust the neighboring verts *on other layers*
		//                                                                   to maintain similar angles relative to the new sides
		// this method applies these adjustments to the supplied newBaseVerts array

		for (int x = 0; x < _vertexCount; x++) //do by-vertex first in this case
		{
			int exemptA = (vertexCenterIndex + 1 + extraOffset) % _vertexCount;
			int exemptB = vertexCenterIndex == 0 ? _vertexCount - 1 : vertexCenterIndex - 1;
			if (x != exemptA && x != exemptB) { continue; }
			Vector2 referencePoint = baseVerts[exemptLayerIndex * _vertexCount + x];
			if ((referencePoint.x == 0 && sliceWidth != 0) || (referencePoint.y == 0 && sliceHeight != 0)) { continue; }

			int j = x;
			int j0 = j == 0 ? _vertexCount - 1 : j - 1;
			int j2 = j == _vertexCount - 1 ? 0 : j + 1;
			Vector2 side01 = baseVerts[exemptLayerIndex * _vertexCount + j0] - baseVerts[exemptLayerIndex * _vertexCount + j];
			Vector2 side21 = baseVerts[exemptLayerIndex * _vertexCount + j2] - baseVerts[exemptLayerIndex * _vertexCount + j];
			float oldAngle01 = Mathf.Atan2(side01.y, side01.x);
			float oldAngle21 = Mathf.Atan2(side21.y, side21.x);
			float previousInternalAngle = oldAngle21 - oldAngle01;
			if (previousInternalAngle < 0) { previousInternalAngle += 2 * (float)Math.PI; }
			float previousExternalAngle = previousInternalAngle > 0 ? Mathf.PI * 2 - previousInternalAngle : -Mathf.PI * 2 - previousInternalAngle;

			if (isDelete && j > vertexCenterIndex) { j--; j0--; j2--; }
			if (isDelete && j == 0) { j0--; }
			if ( j < 0) {  j += vertexCount; }
			if (j0 < 0) { j0 += vertexCount; }
			if (j2 < 0) { j2 += vertexCount; }

			//Debug.Log(j0 + " " + j + " " + j2);
			side01 = newBaseVerts[exemptLayerIndex * vertexCount + j0] - newBaseVerts[exemptLayerIndex * vertexCount + j];
			Vector2 referenceVector = side01.normalized; // THIS is what will ultimately be rotated to find the final point
			side21 = newBaseVerts[exemptLayerIndex * vertexCount + j2] - newBaseVerts[exemptLayerIndex * vertexCount + j];
			float newInternalAngle = Mathf.Atan2(side21.y, side21.x) - Mathf.Atan2(side01.y, side01.x);
			if (newInternalAngle < 0) { newInternalAngle += 2 * (float)Math.PI; }
			float newExternalAngle = newInternalAngle > 0 ? Mathf.PI * 2 - newInternalAngle : -Mathf.PI * 2 - newInternalAngle;

			//Debug.Log("Point " + j0 + " " + j + " " + j2 + " oldAngle01: " + oldAngle01 + " oldAngle21: " + oldAngle21 + " internalAngle: " + previousInternalAngle + " > " + newInternalAngle);

			for (int i = 0; i < layerCount; i++)
			{
				if (i == exemptLayerIndex) { continue; }

				float previousMagnitude = (baseVerts[i * _vertexCount + x] - baseVerts[exemptLayerIndex * _vertexCount + x]).magnitude;

				Vector2 sideX1 = baseVerts[i * _vertexCount + x] - baseVerts[exemptLayerIndex * _vertexCount + x];
				float oldAngleX1 = Mathf.Atan2(sideX1.y, sideX1.x);
				float previousAngleA = oldAngleX1 - oldAngle21;
				if (previousAngleA < -Math.PI) { previousAngleA += 2 * (float)Math.PI; }
				if (previousAngleA >  Math.PI) { previousAngleA -= 2 * (float)Math.PI; }
				bool isInterior = Mathf.Sign(previousAngleA) != Mathf.Sign(previousExternalAngle);
				float angleRatio = isInterior ? previousAngleA / previousInternalAngle : previousAngleA / previousExternalAngle;
				float newAngleA = isInterior ? angleRatio * newInternalAngle : angleRatio * newExternalAngle;
				float sin = Mathf.Sin(newInternalAngle + newAngleA);
				float cos = Mathf.Cos(newInternalAngle + newAngleA);
				Vector2 finalAngle = new Vector2(cos * referenceVector.x - sin * referenceVector.y, cos * referenceVector.y + sin * referenceVector.x);
				//Debug.Log(newInternalAngle + newAngleA + " sin: " + sin + " cos: " + cos + " " + (cos * referenceVector.x - sin * referenceVector.y) + " " + finalAngle.y);
				Vector2 newVertDelta = finalAngle * previousMagnitude;
				//Debug.Log(j + " " + i + " ref: " + referenceVector + " oldAngleX1: " + oldAngleX1 + " angleRatio: " + angleRatio + " AngelA: " + previousAngleA + " > " + newAngleA + "      " + newVertDelta.x + " " + newVertDelta.y);
				newBaseVerts[i * vertexCount + j] = referencePoint + newVertDelta;
			}
		}
	}

	[ContextMenu("DebugSlice03")]public void DebugSlice03() { Slice(0, 3); }
	[ContextMenu("DebugSliceAdd03")] public void DebugSliceAdd03() { SliceAdd2((baseVerts[0] + baseVerts[1]) * 0.5f, 0, (baseVerts[3] + baseVerts[4]) * 0.5f, 3); }
	public void SliceAddSaved()
	{
		if (savedSliceValues != 2) { return; }
		//TODO
	}
	public void SliceAdd2(Vector2 pointA, int indexA, Vector2 pointB, int indexB)
	{
		if (indexA < indexB) { indexB++; }
		AddVertex(pointA, indexA, true);
		AddVertex(pointB, indexB, true);
		Slice(indexA + 1, indexB + 1);
	}
	public void Slice(int indexA, int indexB)
	{
		//check to see if slice is valid
		if (sliceHost == null) { Debug.Log("No slice host on this SuperShape!"); return; }
		if (indexA - indexB == 0) { Debug.Log("Cannot slice same point!"); return; }
		if (indexA - indexB == 1) { Debug.Log("Cannot slice adjacent points!"); return; }
		if (indexA - indexB == -1) { Debug.Log("Cannot slice adjacent points!"); return; }

		//decide who-gets-what
		List<int> myIndexes = new List<int>();
		List<int> hostIndexes = new List<int>();
		if (_mesh == null) { _mesh = GetComponent<ISuperShapeMesh>(); }
		//int oldTailIndex = _mesh.tailFixedBaseIndex; //reach into the mesh and check where the tail is currently at;
		//float oldTailUnitProgress = _mesh.tailFixedBaseUnitProgress;
		//int myNewTailIndex = -1;
		//float myNewTailUnitProgress = 0;
		//int hostNewTailIndex = -1;
		//float hostNewTailUnitProgress = 0;
		bool isMine = true;
		for (int i = 0; i < vertexCount; i++)
		{
			if (i == indexA || i == indexB)
			{
				myIndexes.Add(i);
				hostIndexes.Add(i);
				if (isMine) { flattenSide = i; }
				isMine = !isMine;
			}
			else
			{
				if (isMine) { myIndexes.Add(i); }
				else { hostIndexes.Add(i); }
			}
		}
		sliceHost.flattenSide = hostIndexes.Count - 1;

		sliceHost.vertexCount = hostIndexes.Count;
		sliceHost._vertexCount = sliceHost.vertexCount;
		sliceHost.layerCount = layerCount;
		sliceHost._layerCount = layerCount;

		//first wipe all host data in prep for transplant
		sliceHost.baseVerts = new Vector2[sliceHost.vertexCount * layerCount];
		sliceHost.prevVerts = new Vector2[sliceHost.vertexCount * layerCount];
		sliceHost.nextVerts = new Vector2[sliceHost.vertexCount * layerCount];
		sliceHost.musicDeltas = new Vector2[sliceHost.vertexCount * layerCount];
		sliceHost.wiggleProgress = new float[sliceHost.vertexCount * layerCount];
		sliceHost.wiggleDuration = new float[sliceHost.vertexCount * layerCount];
		sliceHost.wiggleZones = new Vector2[sliceHost.vertexCount * layerCount];
		sliceHost._layerColors = new Color[layerCount];
		sliceHost.wiggleProfiles = new WiggleProfile[layerCount];
		sliceHost.wiggleRotationStep = new int[layerCount];

		//obviously, copy host data first
		for (int i = 0; i < layerCount; i++)
		{
			for (int j = 0; j < hostIndexes.Count; j++)
			{
				int k = i * hostIndexes.Count + j;
				int x = i * vertexCount + hostIndexes[j];
				sliceHost.baseVerts[k] = baseVerts[x];
				sliceHost.prevVerts[k] = prevVerts[x];
				sliceHost.nextVerts[k] = nextVerts[x];
				sliceHost.musicDeltas[k] = musicDeltas[x];
				sliceHost.wiggleProgress[k] = wiggleProgress[x];
				sliceHost.wiggleDuration[k] = wiggleDuration[x];
				sliceHost.wiggleZones[k] = wiggleZones[x];
			}
			sliceHost._layerColors[i] = _layerColors[i];
			sliceHost.wiggleProfiles[i] = wiggleProfiles[i];
			sliceHost.wiggleRotationStep[i] = wiggleRotationStep[i];
		}

		//destructively copying our own data downwards is fine, as long as it's "downwards"
		for (int i = 0; i < layerCount; i++)
		{
			for (int j = 0; j < myIndexes.Count; j++)
			{
				int k = i * myIndexes.Count + j;
				int x = i * vertexCount + myIndexes[j];
				baseVerts[k] = baseVerts[x];
				prevVerts[k] = prevVerts[x];
				nextVerts[k] = nextVerts[x];
				musicDeltas[k] = musicDeltas[x];
				wiggleProgress[k] = wiggleProgress[x];
				wiggleDuration[k] = wiggleDuration[x];
				wiggleZones[k] = wiggleZones[x];
			}
		}

		//now just transfer common stuff
		sliceHost._blendMode = _blendMode;
		sliceHost._fillTexture1 = _fillTexture1;
		sliceHost.isWigglePaused = isWigglePaused;
		sliceHost.tailMode = TailMode.NONE;
		sliceHost.innerTextureMode = innerTextureMode;
		sliceHost.sliceHost = null;
		savedSliceValues = 0;
		sliceHost.savedSliceValues = 0;

		//finally, once index-dependent transfers are done, make the new counts official
		//we are careful/exhaustive in this method, so go ahead and override the internal variables
		vertexCount = myIndexes.Count;
		_vertexCount = vertexCount;

		//and resize my data
		int fullSize = _vertexCount * _layerCount;
		System.Array.Resize(ref baseVerts, fullSize);
		System.Array.Resize(ref prevVerts, fullSize);
		System.Array.Resize(ref nextVerts, fullSize);
		System.Array.Resize(ref musicDeltas, fullSize);
		System.Array.Resize(ref wiggleProgress, fullSize);
		System.Array.Resize(ref wiggleDuration, fullSize);
		System.Array.Resize(ref wiggleZones, fullSize);

		SetMeshDirty();
		sliceHost.SetMeshDirty();
		sliceHost._mesh = sliceHost.GetComponent<ISuperShapeMesh>();
		//sliceHost._mesh.proxyMaskMesh = null;

		//synchronize and enable
		sliceHost.localPosition = localPosition;
		sliceHost.localRotation = localRotation;
		sliceHost.localScale = localScale;
		sliceHost.rectTransform.anchorMax = rectTransform.anchorMax;
		sliceHost.rectTransform.anchorMin = rectTransform.anchorMin;
		sliceHost.rectTransform.pivot = rectTransform.pivot;
		sliceHost.slice = slice;
		sliceHost.SetActive(true);

		//any existing motions applied to the donor will have to be manually applied to the host at time of split
	}
	
	public void AssignBaseVertexPivot(int vertexIndex) { AssignVertexPivot(vertexIndex); }
	public void AssignOuterVertexPivot(int vertexIndex) { AssignVertexPivot(vertexIndex, layerCount - 1); }

	public void AssignVertexPivot(int baseVertexIndex, int layerIndex = 0)
	{
		Vector2 vert = Vector2.zero;
		if (baseVertexIndex == -99 && layerIndex == -99) 
		{
			//hack to select center for reset
		}
		else
		{
			if (layerIndex < 0 || layerIndex >= _layerCount) { Debug.LogWarning("Invalid layerIndex for pivot"); return; }
			if (baseVertexIndex < 0 || baseVertexIndex >= _vertexCount) { Debug.LogWarning("Invalid vertexIndex for pivot"); return; }
			vert = baseVerts[layerIndex * vertexCount + baseVertexIndex];
		}
		if      (vert.x > 0) { vert += new Vector2(sliceWidth / 2, 0); }
		else if (vert.x < 0) { vert -= new Vector2(sliceWidth / 2, 0); }
		if      (vert.y > 0) { vert += new Vector2(0, sliceHeight / 2); }
		else if (vert.y < 0) { vert -= new Vector2(0, sliceHeight / 2); }
		Vector2 newPivot = vert / sizeDelta + Vector2.one * 0.5f;
		Vector2 shift = (Vector3)(sizeDelta * (newPivot - rectTransform.pivot));
		float theta = localRotation.eulerAngles.z * Mathf.Deg2Rad;
		float sin = Mathf.Sin(theta);
		float cos = Mathf.Cos(theta);
		localPosition += new Vector3(cos * shift.x - sin * shift.y, sin * shift.x + cos * shift.y, 0);
		rectTransform.pivot = newPivot; 
	}

	void ResizeRect0() { ResizeSliceRect(Vector2.zero, true); }
	void ResizeRectMax() { ResizeSliceRect(Vector2.zero); }
	public void ResizeSliceRect(Vector2 offset, bool isZero = false)
	{
		Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
		for (int i = 0; i < baseVerts.Length; i++)
		{
			float x = Mathf.Abs(baseVerts[i].x) + sliceWidth / 2;
			float y = Mathf.Abs(baseVerts[i].y) + sliceHeight / 2;
			if (x != 0 && x < min.x) { min.x = x; }
			if (y != 0 && y < min.y) { min.y = y; }
		}
		Vector2 newSlice;
		if (isZero) //new slice values is offset or min, whichever is smaller
		{
			newSlice = new Vector2(Mathf.Min(offset.x, min.x), Mathf.Min(offset.y, min.y)) * 2;
		}
		else //new slice values is min - offset or 0f, whichever is greater
		{
			if (offset.x <= 0) { offset.x = 0.00002f; }
			if (offset.y <= 0) { offset.y = 0.00002f; }
			newSlice = new Vector2(Mathf.Max(min.x - offset.x, 0), Mathf.Max(min.y - offset.y, 0)) * 2;
		}
		Vector2 delta = (newSlice - new Vector2(sliceWidth,sliceHeight)) / 2;
		for (int i = 0; i < baseVerts.Length; i++)
		{
			if      (baseVerts[i].x > 0) { baseVerts[i].x -= delta.x; }
			else if (baseVerts[i].x < 0) { baseVerts[i].x += delta.x; }
			if      (baseVerts[i].y > 0) { baseVerts[i].y -= delta.y; }
			else if (baseVerts[i].y < 0) { baseVerts[i].y += delta.y; }
			prevVerts[i] = baseVerts[i];
			nextVerts[i] = baseVerts[i];
		}
		slice = newSlice;
		UpdateMesh();
	}

	public void ResizeRectToSliceRect()
	{
		rectTransform.sizeDelta = slice;
	}
	public void ResizeRectToInnerLayer()
	{
		Vector2 min = Vector2.zero;
		Vector2 max = Vector2.zero;
		for (int i = 0; i < vertexCount; i++)
		{
			Vector2 p = baseVerts[i];
			if (p.x - sliceWidth  / 2 < min.x) { min.x = p.x - sliceWidth  / 2; }
			if (p.y - sliceHeight / 2 < min.y) { min.y = p.y - sliceHeight / 2; }
			if (p.x + sliceWidth  / 2 > max.x) { max.x = p.x + sliceWidth  / 2; }
			if (p.y + sliceHeight / 2 > max.y) { max.y = p.y + sliceHeight / 2; }
		}
		rectTransform.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
	}
	public void ResizeRectToOuterLayer()
	{
		Vector2 min = Vector2.zero;
		Vector2 max = Vector2.zero;
		for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
		{
			for (int i = 0; i < vertexCount; i++)
			{
				Vector2 p = baseVerts[layerIndex * layerCount + i];
				if (p.x - sliceWidth  / 2 < min.x) { min.x = p.x - sliceWidth  / 2; }
				if (p.y - sliceHeight / 2 < min.y) { min.y = p.y - sliceHeight / 2; }
				if (p.x + sliceWidth  / 2 > max.x) { max.x = p.x + sliceWidth  / 2; }
				if (p.y + sliceHeight / 2 > max.y) { max.y = p.y + sliceHeight / 2; }
			}
		}
		rectTransform.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
	}

	private void Reset()
	{
		ResizeDataCheck();

		baseVerts.CopyTo(prevVerts, 0);
		baseVerts.CopyTo(nextVerts, 0);

		SetMaterialProperties();
		UpdateMesh();
	}

	private void Awake()
	{
		_mesh = GetComponent<ISuperShapeMesh>();

		ResizeDataCheck();

		baseVerts.CopyTo(prevVerts, 0);
		baseVerts.CopyTo(nextVerts, 0);
		storedVerts = null;

		gutterInnerMovementPercentage = 0;
		gutterCurrentMovementPercentage = 0;

		SetMaterialProperties();
		UpdateMesh();
	}

	private void OnEnable()
	{
		SetMaterialProperties();
	}

	private void OnValidate()
	{
		//Debug.Log(name);
		//TODO - filter dirty mesh and dirty material properties, only change either as needed!
		return;
		//SetMaterialProperties();
	}

	private void OnDidApplyAnimationProperties()
	{
		isMaterialDirty = true;
	}

	public void OnClickShake()
	{
		Move(new Vector3(-gutterTotalMovement.y, gutterTotalMovement.x, 0) * 0.5f, 0.3f, Lerp.Shake2);
	}

	private void OnDrawGizmos()
	{
		#if UNITY_EDITOR
		// Ensure continuous Update calls.
		if (isWigglingInEditor && !Application.isPlaying)
		{
			//UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
			//UnityEditor.SceneView.RepaintAll();
		}
		#endif
	}

	private void Update()
	{
		//if (!enabled || !gameObject.activeInHierarchy) { return; }
		if (isMaterialDirty) { SetMaterialProperties(); }
		ResizeDataCheck();
		if (bridgeTarget != null)
		{
			SetMeshDirty();
		}
		if (isGutter)
		{
			bool isToggle = superShapeButton != null && superShapeButton.isToggled;
			float target = (isClickedGutterDescending || !interactable) ? 1.0f :
			                isToggle ? 0.8f :
			                0;
			if (gutterInnerMovementPercentage < target)
			{
				gutterInnerMovementPercentage += GUTTER_DOWN_SPEED * deltaTime;
				if (gutterInnerMovementPercentage > target) { gutterInnerMovementPercentage = target; }
				gutterCurrentMovementPercentage = L.erp(gutterInnerMovementPercentage, Lerp.Linear);
				isMeshDirty = true;
				if (gutterContainer != null)
				{
					gutterContainer.localPosition = gutterInnerMovementPercentage * gutterTotalMovement;
				}
			}
			else if (gutterInnerMovementPercentage > target)
			{
				float upSpeed = GUTTER_UP_SPEED * (superShapeButton != null && isToggle ? 0.2f: 1);
				gutterInnerMovementPercentage -= GUTTER_UP_SPEED * deltaTime;
				if (gutterInnerMovementPercentage < target) { gutterInnerMovementPercentage = target; }
				if (isToggle)
				{
					gutterCurrentMovementPercentage = L.erp(gutterInnerMovementPercentage, Lerp.Linear);
				}
				else
				{
					gutterCurrentMovementPercentage = 1.0f - L.erp(1.0f - gutterInnerMovementPercentage, Lerp.Parabola01Overshoot); isMeshDirty = true;
				}
				if (gutterContainer != null)
				{
					gutterContainer.localPosition = gutterInnerMovementPercentage * gutterTotalMovement;
				}
			}
			else
			{
				if (isClickedGutterDescending && !Input.GetMouseButton(0)) { isClickedGutterDescending = false; }
			}
		}
		if (isSpinning) { MySpinFunction(2.0f, 200, 80, 10); }
		if (!isWigglePaused && (Application.isPlaying || isWigglingInEditor))
		{
			for (int i = 0; i < layerCount; i++)
			{
				if (wiggleProfiles[i] == null) { wiggleProfiles[i] = new WiggleProfile(); }
				WiggleProfile wp = wiggleProfiles[i];

				//calculate music deltas
				if ((!Application.isEditor || Application.isPlaying) && wp.musicOption != WiggleMusicOption.NONE)
				{
					isMeshDirty = true;
					switch (wp.musicOption)
					{
						case WiggleMusicOption.Basic:
							float z = TheMusicPlayer.GetSpectumValue(wp.musicBand, wp.minVolume) - 0.5f;
							for (int j = 0; j < _vertexCount; j++)
							{
								int k = i * _vertexCount + j;
								musicDeltas[k] = wiggleZones[k] * z * wp.volumeRatio;
							}
							break;
						default:
							break;
					}
				}

				WigglePattern pattern = wp.pattern;
				int i2 = i;
				while (pattern == WigglePattern.INHERIT)
				{
					i2--;
					pattern = i2 < 0 ? WigglePattern.Basic : wiggleProfiles[i2].pattern;
				}
				if ((pattern == WigglePattern.NONE && wp.rotateOption == WiggleRotateOption.NoRotate) || wp.speed <= 0) { continue; }

				isMeshDirty = true;
				for (int j = 0; j < _vertexCount; j++)
				{
					int k = i * _vertexCount + j;
					wiggleProgress[k] += deltaTime;
					if (wiggleProgress[k] > wiggleDuration[k])
					{
						prevVerts[k] = nextVerts[k];
						wiggleProgress[k] = Mathf.Max(wiggleProgress[k] - wiggleDuration[k], 0.05f);

						if (j == 0)
						{
							wiggleRotationStep[i]++;
							if (wiggleRotationStep[i] >= _vertexCount) { wiggleRotationStep[i] -= _vertexCount; }
							if (wp.isStoppingWiggleRotation)
							{
								wp.isStoppingWiggleRotation = false;
								wiggleProgress[k] = 0;
								wp.rotateOption = WiggleRotateOption.NoRotate;
							}
						}

						int kr = k;
						/*
						if (wp.rotateOption == WiggleRotateOption.Clockwise)
						{
							kr += wiggleRotationStep[i];
							while (kr >= _vertexCount * (i+1)) { kr -= _vertexCount; }
							Debug.Log(k + " " + (_vertexCount * (i + 1)) + " " + kr);
						}
						else if (wp.rotateOption == WiggleRotateOption.CClockwise)
						{
							kr -= wiggleRotationStep[i];
							while (kr < _vertexCount * i) { kr += _vertexCount; }
						}*/

						switch (pattern)
						{
							case WigglePattern.Basic:
								nextVerts[k] = baseVerts[kr] + new Vector2(wiggleZones[kr].x * Rand(-0.5f, 0.5f), wiggleZones[kr].y * Rand(-0.5f, 0.5f));
								wiggleDuration[k] = 1.0f / wp.speed;
								break;
							case WigglePattern.NONE:
								nextVerts[k] = baseVerts[kr];
								wiggleDuration[k] = 1.0f / wp.speed;
								break;
							default:
								break;
						}
					}
				}
				if (tailMode != TailMode.NONE)
				{
					tailWiggleProgress += deltaTime;
					tailWiggleDuration = 3.0f; //temp
					if (tailWiggleProgress > tailWiggleDuration) { tailWiggleProgress -= tailWiggleDuration; }
					tailTipWiggleValueA = L.erp(tailWiggleProgress / tailWiggleDuration, Lerp.SinFull);
					tailTipWiggleValueB = L.erp(tailWiggleProgress / tailWiggleDuration, Lerp.CosFull);
				}
			}
		}
		if (_mesh == null) { _mesh = GetComponent<SuperShapeMesh>(); if (_mesh == null) { _mesh = GetComponent<SuperShapeCanvasMesh>(); } }
		if (isMeshDirty || shapeMesh.IsDemandingAlwaysUpdate()) { UpdateMesh(); }
	}

	void OnDestroy()
	{
		if (material != null)
		{
			//DestroyImmediate(material);
			material = null;
		}
	}

	public Vector2 TranslateWorldDeltaToLocal(Vector2 delta)
	{
		return transform.InverseTransformVector(delta);
	}

	public Vector3 GetWorldPosOfVert(int layerIndex, int vertexIndex)
	{
		int k = layerIndex * vertexCount + vertexIndex;
		Vector2 vert = prevVerts[k] * (1 - wiggleProgress[k]/wiggleDuration[k]) + nextVerts[k] * wiggleProgress[k] / wiggleDuration[k] + musicDeltas[k];
		return GetWorldVertFromLocal(vert);
	}

	public Vector3 GetWorldVertFromLocal(Vector2 vert2D)
	{
		Vector2 vert = vert2D;
		if (vert.x != 0) { vert.x += Mathf.Sign(vert.x) * sliceWidth / 2; }
		if (vert.y != 0) { vert.y += Mathf.Sign(vert.y) * sliceHeight / 2; }
		//vert -= (rectTransform.pivot - Vector2.one * 0.5f) * slice;
		return transform.TransformPoint(vert);
	}

	public Vector3[] GetWorldVertices()
	{
		Vector3[] verts3D = new Vector3[baseVerts.Length];
		for (int i = 0; i < baseVerts.Length; i++)
		{
			verts3D[i] = GetWorldVertFromLocal(storedVerts != null && storedVerts.Length > 0 ? storedVerts[i] : baseVerts[i]);
		}
		return verts3D;
	}

	public Vector2 GetLocalVertFromWorld(Vector3 vert3D)
	{
		Vector2 vert = (Vector2)transform.InverseTransformPoint(vert3D);// + (rectTransform.pivot - Vector2.one * 0.5f) * slice;
		//floor and snap problematic data
		if (sliceWidth > 0)
		{
			if (Mathf.Abs(vert.x) > sliceWidth / 2) { vert.x -= Mathf.Sign(vert.x) * sliceWidth / 2; }
			else if (Mathf.Abs(vert.x) < EDITOR_ADD_POINT_BOUNDARY_SNAP_DISTANCE) { vert.x = 0; }
			else { vert.x = Mathf.Sign(vert.x) * 0.000001f; }
		}
		if (sliceHeight > 0)
		{
			if (Mathf.Abs(vert.y) > sliceHeight / 2) { vert.y -= Mathf.Sign(vert.y) * sliceHeight / 2; }
			else if (Mathf.Abs(vert.y) < EDITOR_ADD_POINT_BOUNDARY_SNAP_DISTANCE) { vert.y = 0; }
			else { vert.y = Mathf.Sign(vert.y) * 0.000001f; }
		}
		return vert;
	}

	public void SetWorldVertices(Vector3[] verts3D)
	{
		for (int i = 0; i < verts3D.Length; i++)
		{
			baseVerts[i] = GetLocalVertFromWorld(verts3D[i]);
			prevVerts[i] = baseVerts[i];
			nextVerts[i] = baseVerts[i];
		}
		UpdateMesh();
	}

	public Rect[] GetWorldWiggleZones()
	{
		Rect[] result = new Rect[wiggleZones.Length];
		Vector3[] worldVerts = GetWorldVertices();
		for (int i = 0; i < wiggleZones.Length; i++)
		{
			Vector2 worldZone = new Vector2(wiggleZones[i].x * transform.lossyScale.x, wiggleZones[i].y * transform.lossyScale.y);
			result[i] = new Rect((Vector2)worldVerts[i] - worldZone / 2, worldZone);
		}
		return result;
	}

	public void SetWorldWiggleZones(Rect[] newZones)
	{
		Vector2 scale = new Vector2(transform.lossyScale.x, transform.lossyScale.y);
		for (int i = 0; i < newZones.Length; i++)
		{
			wiggleZones[i] = new Vector2(newZones[i].width / scale.x, newZones[i].height / scale.y);
		}
	}

	public Rect GetWorldTailWiggleZone()
	{
		Vector3 tailTipWorldPos = transform.TransformPoint(tailTip);
		Vector2 worldZone =  new Vector2(tailTipWiggle.x * transform.lossyScale.x, tailTipWiggle.y * transform.lossyScale.y);
		return new Rect((Vector2)tailTipWorldPos - worldZone / 2, worldZone);
	}

	public void SetWorldTailWiggleZone(Rect newZone)
	{
		Vector2 scale = new Vector2(transform.lossyScale.x, transform.lossyScale.y);
		tailTipWiggle = new Vector2(newZone.width / scale.x, newZone.height / scale.y);
	}

	public Vector3[] GetWorldBounds()
	{
		Vector3[] bounds = new Vector3[5] { new Vector3(-sliceWidth/2 * transform.lossyScale.x, -sliceHeight/2 * transform.lossyScale.y, 0),
		                                    new Vector3(-sliceWidth/2 * transform.lossyScale.x,  sliceHeight/2 * transform.lossyScale.y, 0),
		                                    new Vector3( sliceWidth/2 * transform.lossyScale.x,  sliceHeight/2 * transform.lossyScale.y, 0),
		                                    new Vector3( sliceWidth/2 * transform.lossyScale.x, -sliceHeight/2 * transform.lossyScale.y, 0),
		                                    new Vector3(-sliceWidth/2 * transform.lossyScale.x, -sliceHeight/2 * transform.lossyScale.y, 0)};
		float theta = rotation.eulerAngles.z * Mathf.Deg2Rad;
		float sin = Mathf.Sin(theta);
		float cos = Mathf.Cos(theta);
		for (int i = 0; i < bounds.Length; i++)
		{
			Vector3 p = bounds[i];
			bounds[i] = new Vector3(p.x * cos - p.y * sin, p.y * cos + p.x * sin, 0) + position;
		}
		return bounds;
	}

	public void SetMaterialProperties()
	{
		//if (!enabled) { return; }
		bool isMaskEnabled = maskMode != SuperShapeMaskMode.NONE;
		string shaderName = "Hidden/BlendModes/SuperShape/Basic";
		/*		if (blendMode != BlendModes.BlendMode.NONE)
				{
					shaderName = string.Format("Hidden/BlendModes/SuperShape/{0}{1}", isUnifiedGrabEnabled ? "UnifiedGrab" : "Grab", isMaskEnabled ? "Masked" : "");
				}*/
		bool isUsingCommonMaterial = false; // shaderName == "Hidden/BlendModes/SuperShape/Basic" && _textureCount == SuperShapeTextureCount.Zero; // and not masked?
		if (isUsingCommonMaterial)
		{
			if (isRespectingTintGroup)
			{
				if (commonMaterialFade == null)
				{
					commonMaterialFade = new Material(Shader.Find(shaderName));
					commonMaterialFade.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
					commonMaterialFade.EnableKeyword("FADE");
					commonMaterialFade.DisableKeyword("TEXTURE_COORDINATES_UV");
					commonMaterialFade.DisableKeyword("TEXTURE_COORDINATES_DOUBLE_UV");
					commonMaterialFade.DisableKeyword("TEXTURE_COORDINATES_SCREENSPACE");
				}
				material = commonMaterialFade;
				material.SetTexture("_MainTex", null);
			}
			else
			{
				if (commonMaterialNoFade == null)
				{
					commonMaterialNoFade = new Material(Shader.Find(shaderName));
					commonMaterialNoFade.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
					commonMaterialNoFade.DisableKeyword("FADE");
					commonMaterialNoFade.DisableKeyword("TEXTURE_COORDINATES_UV");
					commonMaterialNoFade.DisableKeyword("TEXTURE_COORDINATES_DOUBLE_UV");
					commonMaterialNoFade.DisableKeyword("TEXTURE_COORDINATES_SCREENSPACE");
				}
				material = commonMaterialNoFade;
				material.SetTexture("_MainTex", null);
			}
		}
		else
		{
			material = new Material(Shader.Find(shaderName));
			material.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;

			material.SetTexture("_MainTex", (int)_textureCount >= 1 ? fillTexture1 : null);
			material.SetTexture("_MainTex2", (int)_textureCount >= 2 ? fillTexture2 : null);
			if (fillTexture1 != null && (int)_textureCount >= 1)
			{
				material.SetTextureOffset("_MainTex", fillTexture1Offset);
				material.SetTextureScale("_MainTex", fillTexture1Scale);
				material.SetFloat("_MainTex_Alpha", fillTexture1Alpha);

				if (innerTextureMode == InnerTextureMode.ScreenSpace)
				{
					material.DisableKeyword("TEXTURE_COORDINATES_UV");
					material.DisableKeyword("TEXTURE_COORDINATES_DOUBLE_UV");
					material.EnableKeyword("TEXTURE_COORDINATES_SCREENSPACE");
				}
				else if (fillTexture2 != null && (int)_textureCount >= 2)
				{
					material.SetTextureOffset("_MainTex2", fillTexture2Offset);
					material.SetTextureScale("_MainTex2", fillTexture2Scale);
					material.SetFloat("_MainTex2_Alpha", fillTexture2Alpha);

					//Debug.Log(gameObject.name);
					material.DisableKeyword("TEXTURE_COORDINATES_UV");
					material.DisableKeyword("TEXTURE_COORDINATES_SCREENSPACE");
					material.EnableKeyword("TEXTURE_COORDINATES_DOUBLE_UV");
				}
				else
				{
					material.DisableKeyword("TEXTURE_COORDINATES_DOUBLE_UV");
					material.DisableKeyword("TEXTURE_COORDINATES_SCREENSPACE");
					material.EnableKeyword("TEXTURE_COORDINATES_UV");
				}
			}
			else
			{
				material.DisableKeyword("TEXTURE_COORDINATES_UV");
				material.DisableKeyword("TEXTURE_COORDINATES_DOUBLE_UV");
				material.DisableKeyword("TEXTURE_COORDINATES_SCREENSPACE");
			}

			if (isMaskEnabled)
			{
				int blendStencilComp = (maskMode == SuperShapeMaskMode.NothingButMask) ? 3 : 6;
				int normalStencilComp = (maskMode == SuperShapeMaskMode.NothingButMask) ? 6 : 3;
				//if (maskMode == SuperShapeMaskMode.EverythingButCutoutMask || maskMode == SuperShapeMaskMode.NothingButCutoutMask) { normalStencilComp = 1; }
				material.SetFloat("_BLENDMODES_BlendStencilComp", blendStencilComp);
				material.SetFloat("_BLENDMODES_NormalStencilComp", normalStencilComp);
				material.SetFloat("_BLENDMODES_StencilId", 1);
			}

			if (isRespectingTintGroup || blendMode != BlendModes.BlendMode.NONE)
			{
				material.EnableKeyword("FADE");
			}
			else
			{
				material.DisableKeyword("FADE");
			}
			BlendModes.ShaderUtilities.SelectBlendModeKeyword(material, blendMode);

		}
		if (sortingGroup != null)
		{
			material.SetInt("_GroupBGIndex", SortingLayer.GetLayerValueFromID(sortingGroup.sortingLayerID) > SortingLayer.GetLayerValueFromID(0) ? 1 : 0);
		}

		if (_mesh == null) { _mesh = GetComponent<SuperShapeMesh>(); }
		if (_mesh == null) { _mesh = GetComponent<SuperShapeCanvasMesh>(); }
		_mesh.SetMaterial(material);
		if (isMaterialDirty) { isMaterialDirty = false; }
	}

	[ContextMenu("TestRemove0")] void TestRemove0() { RemoveVertex(0); }
	[ContextMenu("TestRemove1")] void TestRemove1() { RemoveVertex(1); }
	[ContextMenu("TestRemove2")] void TestRemove2() { RemoveVertex(2); }
	[ContextMenu("TestRemove3")] void TestRemove3() { RemoveVertex(3); }
	[ContextMenu("TestRemove4")] void TestRemove4() { RemoveVertex(4); }
	[ContextMenu("TestRemove5")] void TestRemove5() { RemoveVertex(5); }
	[ContextMenu("TestRemove9")] void TestRemove9() { RemoveVertex(9); }

	[ContextMenu("AddIgnoreListToVertsTest")] void AdoptNewIgnoreListTest() { AdoptNewIgnoreList(new List<int>() { 0 }); }
	public void AdoptNewIgnoreList(List<int> ignoreVertList)
	{
		int myVertCount = vertexCount + ignoreVertList.Count;
		int fullSize = myVertCount * layerCount;
		Array.Resize(ref baseVerts, fullSize);
		Array.Resize(ref prevVerts, fullSize);
		Array.Resize(ref nextVerts, fullSize);
		Array.Resize(ref musicDeltas, fullSize);
		Array.Resize(ref wiggleProgress, fullSize);
		Array.Resize(ref wiggleDuration, fullSize);
		Array.Resize(ref wiggleZones, fullSize);
		for (int i = layerCount - 1; i > -1; i--)
		{
			int z = ignoreVertList.Count;
			for (int j = myVertCount - 1; j > -1; j--)
			{
				if (ignoreVertList.Contains(j))
				{
					z--;
					baseVerts[i * myVertCount + j] = Vector2.one;
				}
				else
				{
					baseVerts[i * myVertCount + j] = baseVerts[i * vertexCount + j - z];
					prevVerts[i * myVertCount + j] = prevVerts[i * vertexCount + j - z];
					nextVerts[i * myVertCount + j] = nextVerts[i * vertexCount + j - z];
					musicDeltas[i * myVertCount + j] = musicDeltas[i * vertexCount + j - z];
					wiggleProgress[i * myVertCount + j] = wiggleProgress[i * vertexCount + j - z];
					wiggleDuration[i * myVertCount + j] = wiggleDuration[i * vertexCount + j - z];
					wiggleZones[i * myVertCount + j] = wiggleZones[i * vertexCount + j - z];
				}
			}
		}
		for (int j = 0; j < myVertCount; j++)
		{
			if (!ignoreVertList.Contains(j)) { continue; }
			int j0 = j == 0 ? myVertCount - 1 : j - 1;
			int j0Dist = 1;
			while (ignoreVertList.Contains(j0))
			{
				j0 = j0 == 0 ? myVertCount - 1 : j0 - 1;
				j0Dist++;
			}
			int j2 = j == myVertCount - 1 ? 0 : j + 1;
			int j2Dist = 1;
			while (ignoreVertList.Contains(j2))
			{
				j2 = j2 == myVertCount - 1 ? 0 : j2 + 1;
				j2Dist++;
			}
			for (int i = 0; i < layerCount; i++)
			{
				baseVerts[i * myVertCount + j] = (baseVerts[i * myVertCount + j0] * j2Dist + baseVerts[i * myVertCount + j2] * j0Dist) / (j0Dist + j2Dist);
				prevVerts[i * myVertCount + j] = (prevVerts[i * myVertCount + j0] * j2Dist + prevVerts[i * myVertCount + j2] * j0Dist) / (j0Dist + j2Dist);
				nextVerts[i * myVertCount + j] = (nextVerts[i * myVertCount + j0] * j2Dist + nextVerts[i * myVertCount + j2] * j0Dist) / (j0Dist + j2Dist);
				musicDeltas[i * myVertCount + j] = (musicDeltas[i * myVertCount + j0] * j2Dist + musicDeltas[i * myVertCount + j2] * j0Dist) / (j0Dist + j2Dist);
				wiggleProgress[i * myVertCount + j] = (wiggleProgress[i * myVertCount + j0] * j2Dist + wiggleProgress[i * myVertCount + j2] * j0Dist) / (j0Dist + j2Dist);
				wiggleDuration[i * myVertCount + j] = (wiggleDuration[i * myVertCount + j0] * j2Dist + wiggleDuration[i * myVertCount + j2] * j0Dist) / (j0Dist + j2Dist);
				wiggleZones[i * myVertCount + j] = (wiggleZones[i * myVertCount + j0] * j2Dist + wiggleZones[i * myVertCount + j2] * j0Dist) / (j0Dist + j2Dist);
			}
		}
		
		vertexCount = myVertCount;
		_vertexCount = myVertCount;
	}

	public Vector2[] AddIgnoreListToVerts(Vector2[] verts, List<int> ignoreVertList)
	{
		if (ignoreVertList == null || ignoreVertList.Count <= 0) { return verts; }
		int myVertCount = verts.Length + ignoreVertList.Count;
		Vector2[] results = new Vector2[myVertCount];
		int z = 0;
		for (int j = 0; j < myVertCount; j++)
		{
			if (ignoreVertList.Contains(j)) { z++; }
			else { results[j] = verts[j - z]; }
		}

		for (int j = 0; j < myVertCount; j++)
		{
			if (!ignoreVertList.Contains(j)) { continue; }
			int j0 = j == 0 ? myVertCount - 1 : j - 1;
			int j0Dist = 1;
			while (ignoreVertList.Contains(j0))
			{
				j0 = j0 == 0 ? myVertCount - 1 : j0 - 1;
				j0Dist++;
			}
			int j2 = j == myVertCount - 1 ? 0 : j + 1;
			int j2Dist = 1;
			while (ignoreVertList.Contains(j2))
			{
				j2 = j2 == myVertCount - 1 ? 0 : j2 + 1;
				j2Dist++;
			}
			int k0 = 0;
			for (int i = 0; i < j0; i++) { if (!ignoreVertList.Contains(i)) { k0++; } }
			int k2 = 0;
			for (int i = 0; i < j2; i++) { if (!ignoreVertList.Contains(i)) { k2++; } }
			results[j] = (verts[k0] * j2Dist + verts[k2] * j0Dist) / (j0Dist + j2Dist);
		}
		return results;
	}

	public Vector2[] GetLayeredPresetVerts(SuperShapePreset preset, List<int> ignoreVertList = null)
	{
		Vector2[] data = presetData[preset];
		int myVertCount = data.Length;
		if (ignoreVertList != null) { myVertCount += ignoreVertList.Count; }
		Vector2[] myData = new Vector2[myVertCount * layerCount];

		//use ignore list and preset data to generate layer 0
		if (ignoreVertList != null)
		{
			AddIgnoreListToVerts(data, ignoreVertList).CopyTo(myData, 0);
		}
		else
		{
			data.CopyTo(myData, 0);
		}
		vertexCount = myVertCount;

		if (layerCount == 1) { return myData; }

		//make layer 1
		for (int j = 0; j < myVertCount; j++)
		{
			int j0 = j == 0 ? myVertCount - 1 : j - 1;
			int j2 = j == myVertCount - 1 ? 0 : j + 1;
			Vector2 p0 = myData[j0];
			Vector2 p = myData[j];
			Vector2 p2 = myData[j2];
			Vector2 s01 = p - p0;
			Vector2 s12 = p2 - p;
			Vector2 s01orth = new Vector2(-s01.y, s01.x).normalized * DEFAULT_LAYER_WIDTH;
			Vector2 s12orth = new Vector2(-s12.y, s12.x).normalized * DEFAULT_LAYER_WIDTH;
			if (s01orth == s12orth)
			{
				myData[vertexCount + j] = p + s01orth;
			}
			else
			{
				Vector2 x = LineSegmentsIntersection(p0 + s01orth, p + s01orth, p + s12orth, p2 + s12orth);
				myData[vertexCount + j] = x;
			}
		}

		//make other layers
		for (int i = 2; i < layerCount; i++)
		{
			for (int j = 0; j < vertexCount; j++)
			{
				myData[i * vertexCount + j] = 2 * myData[(i - 1) * vertexCount + j] - myData[(i - 2) * vertexCount + j];
			}
		}

		return myData;
	}

	public void SetupPreset(SuperShapePreset preset, int newLayerCount = 2)
	{
		Vector2[] data = presetData[preset];
		vertexCount = data.Length;
		layerCount = 0;
		ResizeData();
		data.CopyTo(baseVerts, 0);
		data.CopyTo(prevVerts, 0);
		data.CopyTo(nextVerts, 0);
		for (int i = 2; i <= newLayerCount; i++)
		{
			SetLayerCount(i);
		}
		InitWiggleZones();
		SetMeshDirty();
	}
	
	[ContextMenu("MorphTestA")] public void MorphTestA() { MorphToPreset(SuperShapePreset.Square); }
	[ContextMenu("MorphTestB")] public void MorphTestB() { MorphToPreset(SuperShapePreset.Pentagon); }
	public void MorphToPreset(SuperShapePreset preset,
	                          float duration = DEFUALT_SUPERSHAPE_MORPH_DURATION,
	                          Lerp lerpCurve = Lerp.Linear,
	                          float delay = 0,
	                          Action<object[]> callback = null,
	                          params object[] args)
	{
		ignoreVerts.Sort();
		int dataLength = presetData[preset].Length;
		if (dataLength < vertexCount)
		{
			//we are too big and will need to add verts to the target (which we will then need to clean up when done)
			int addCount = vertexCount - dataLength;
			while (ignoreVerts.Count > addCount) { ignoreVerts.RemoveAt(ignoreVerts.Count - 1); } // okay, but that's too many
			while (ignoreVerts.Count < addCount) { ignoreVerts.Add(ignoreVerts.Count); } // generate more
			Vector2[] myTargetVerts = GetLayeredPresetVerts(preset, ignoreVerts);
			ignoreRatio = 0;
			ChangeIgnoreRatioTo(1, duration + 0.05f, Lerp.Cubic, delay, 1, new Call(CleanupIgnoredVerts).back);
			MoveAllVertsTo(myTargetVerts, duration, lerpCurve, delay, 1, callback, args);
		}
		else if (dataLength > vertexCount)
		{
			//we are too small and need to add verts to us
			int addCount = dataLength - vertexCount;
			while (ignoreVerts.Count > addCount) { ignoreVerts.RemoveAt(ignoreVerts.Count - 1); } // okay, but that's too many
			while (ignoreVerts.Count < addCount) { ignoreVerts.Add(ignoreVerts.Count); } // generate more
			AdoptNewIgnoreList(ignoreVerts);
			Vector2[] myTargetVerts = GetLayeredPresetVerts(preset, null);
			ignoreRatio = 1;
			ChangeIgnoreRatioTo(0, duration, Lerp.Cubic, delay, 1);
			MoveAllVertsTo(myTargetVerts, duration, lerpCurve, delay, 1, callback, args);
		}
		else
		{
			Vector2[] myTargetVerts = GetLayeredPresetVerts(preset, null);
			MoveAllVertsTo(myTargetVerts, duration, lerpCurve, delay, 1, callback, args);
		}
	}

	public void CleanupIgnoredVerts()
	{
		for (int i = ignoreVerts.Count - 1; i > -1; i--)
		{
			RemoveVertex(ignoreVerts[i]);
			ignoreVerts.RemoveAt(i);
		}
	}

	[ContextMenu("PresetTriangle")] void PresetTriangle() { SetupPreset(SuperShapePreset.Triangle); }
	[ContextMenu("PresetSquare")] void PresetSquare() { SetupPreset(SuperShapePreset.Square); }
	[ContextMenu("PresetRectangle")] void PresetRectangle() { SetupPreset(SuperShapePreset.Rectangle); }
	[ContextMenu("PresetParallelogram")] void PresetParallelogram() { SetupPreset(SuperShapePreset.Parallelogram); }
	[ContextMenu("PresetTrapazoid")] void PresetTrapazoid() { SetupPreset(SuperShapePreset.Trapazoid); }
	[ContextMenu("PresetPentagon")] void PresetPentagon() { SetupPreset(SuperShapePreset.Pentagon); }
	[ContextMenu("PresetHexagon")] void PresetHexagon() { SetupPreset(SuperShapePreset.Hexagon); }
	[ContextMenu("PresetChevron")] void PresetChevron() { SetupPreset(SuperShapePreset.Chevron); }
	[ContextMenu("PresetOctagon")] void PresetOctagon() { SetupPreset(SuperShapePreset.Octagon); }

	public void SetLayerCount(int newLayerCount)
	{
		layerCount = newLayerCount;
		ResizeData();
		baseVerts.CopyTo(prevVerts, 0);
		baseVerts.CopyTo(nextVerts, 0);
		UpdateMesh();
	}
	[ContextMenu("DebugReset")] void ResetTestPentagon()
	{
		PresetPentagon(); SetLayerCount(1); SetLayerCount(2); SetLayerCount(3);
		InitWiggleZones(); flattenSide = -1; if (sliceHost != null) sliceHost.SetActive(false); UpdateMesh();
	}

	[ContextMenu("Init Wiggle Zones")] void InitWiggleZones() { SetWiggle(1); }

	public void SetWiggle(float amount)
	{
		Vector2 centroid = Vector2.zero;
		foreach (Vector2 vert in baseVerts)
		{
			centroid += vert;
		}
		centroid /= baseVerts.Length;

		for (int i = 0; i < wiggleZones.Length; i++)
		{
			wiggleZones[i] = (baseVerts[i] - centroid) * -0.1f * amount;
		}
		ResetWiggle();
	}

	[ContextMenu("ResetWiggle")]
	public void ResetWiggle()
	{
		for (int i = 0; i < baseVerts.Length; i++)
		{
			prevVerts[i] = baseVerts[i];
			nextVerts[i] = baseVerts[i];
			musicDeltas[i] = Vector2.zero;
			wiggleProgress[i] = 0;
		}
		for (int i = 0; i < wiggleRotationStep.Length; i++)
		{
			wiggleRotationStep[i] = 0;
		}
		tailWiggleProgress = 0;
		UpdateMesh();
	}
	public void SetMeshDirty() { isMeshDirty = true; }
	public void SetMaterialDirty() { isMaterialDirty = true; }

	[ContextMenu("UpdateMesh")]
	public void UpdateMesh()
	{
		ResizeDataCheck();
		if (_mesh == null) { _mesh = GetComponent<SuperShapeMesh>(); }
		if (_mesh == null) { _mesh = GetComponent<SuperShapeCanvasMesh>(); }
		_mesh.UpdateCounts(layerCount, vertexCount);
		if (storedVerts != null && storedVerts.Length != baseVerts.Length) { storedVerts = null; }

		int x = baseVerts.Length;
		if (vertexCount == 4 && (quadSkew.x != 0 || quadSkew.y != 0))
		{
			for (int i = 0; i < x; i++)
			{
				Vector2 skew = GetSkew(i);
				_mesh.AssignVert(i, baseVerts[i] + skew, prevVerts[i] + skew, nextVerts[i] + skew);
			}
		}
		else
		{
			for (int i = 0; i < x; i++)
			{
				_mesh.AssignVert(i, baseVerts[i], prevVerts[i], nextVerts[i]);
			}
		}
		if (flattenSide > -1 && flattenSide < vertexCount && wiggleProfiles[0].rotateOption == WiggleRotateOption.NoRotate) //rotate not supported
		{
			int j0 = flattenSide - 1; if (j0 < 0) { j0 += vertexCount; }
			int j1 = flattenSide;
			int j2 = flattenSide + 1; if (j2 >= vertexCount) { j2 -= vertexCount; }
			int j3 = flattenSide + 2; if (j3 >= vertexCount) { j3 -= vertexCount; }
			for (int i = 1; i < layerCount; i++)
			{
				if (wiggleProfiles[0].rotateOption != WiggleRotateOption.NoRotate) { continue; } //rotate not supported

				_mesh.AssignVert(i * vertexCount + j1, LineSegmentsIntersection(baseVerts[j1], baseVerts[j2], baseVerts[i * vertexCount + j0], baseVerts[i * vertexCount + j1]),
				                                       LineSegmentsIntersection(prevVerts[j1], prevVerts[j2], prevVerts[i * vertexCount + j0], prevVerts[i * vertexCount + j1]),
				                                       LineSegmentsIntersection(nextVerts[j1], nextVerts[j2], nextVerts[i * vertexCount + j0], nextVerts[i * vertexCount + j1]));

				_mesh.AssignVert(i * vertexCount + j2, LineSegmentsIntersection(baseVerts[j1], baseVerts[j2], baseVerts[i * vertexCount + j2], baseVerts[i * vertexCount + j3]),
				                                       LineSegmentsIntersection(prevVerts[j1], prevVerts[j2], prevVerts[i * vertexCount + j2], prevVerts[i * vertexCount + j3]),
				                                       LineSegmentsIntersection(nextVerts[j1], nextVerts[j2], nextVerts[i * vertexCount + j2], nextVerts[i * vertexCount + j3]));
			}
		}

		if (trueFlattenSide > -1)
		{
			isTailBaseFixed = true;
			//no new data overwritten--sliced shape tail is left fixed at previous LOCAL coordinates
		}
		else
		{
			if (tailTipTransform != null && !isTailTipFixed) { tailTip = transform.InverseTransformPoint(tailTipTransform.transform.position); }
		}

		_mesh.SetDirty();
		isMeshDirty = false;
	}

	[ContextMenu("ReassignMesh")]
	public void ReassignMesh()
	{
		_mesh = GetComponent<SuperShapeMesh>();
	}

	public static Vector2 LineSegmentsIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector3 p4)
	{
		var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);
		if (d == 0.0f) { return Vector2.zero; }
		var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
		var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;
		return new Vector2(p1.x + u * (p2.x - p1.x), p1.y + u * (p2.y - p1.y));
	}
	
	Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
	{
		if (!enabled || material == null)
		{
			return baseMaterial;
		}
		/*
		if (baseMaterial != null && UIShapeIsMasked(this, true))
		{
			material.SetFloat("_Stencil", baseMaterial.GetFloat("_Stencil"));
			material.SetFloat("_StencilOp", baseMaterial.GetFloat("_StencilOp"));
			material.SetFloat("_StencilComp", baseMaterial.GetFloat("_StencilComp"));
			material.SetFloat("_StencilReadMask", baseMaterial.GetFloat("_StencilReadMask"));
			material.SetFloat("_StencilWriteMask", baseMaterial.GetFloat("_StencilWriteMask"));
			material.SetFloat("_ColorMask", baseMaterial.GetFloat("_ColorMask"));
		}
		else
		{*/
			material.SetFloat("_Stencil", 0);
			material.SetFloat("_StencilOp", 0);
			material.SetFloat("_StencilComp", 8);
			material.SetFloat("_StencilReadMask", 255);
			material.SetFloat("_StencilWriteMask", 255);
			material.SetFloat("_ColorMask", 15);
		//}
		return material;
	}
	
	void CheckForShaderReset()
	{
		if (material == null) { return; }
		if (!material.HasProperty("_XScale"))
		{
			UpdateMesh();
			//ApplyShaderKeywordsToMaterial();
			//ApplyShaderPropertiesToMaterial();
		}
	}

	void OnWillRenderObject()
	{
		#if UNITY_EDITOR
			//CheckForShaderReset();
		#endif
	}

	public void OnMouseDown()
	{
		if (isGutter && button != null && button.IsInteractable())
		{
			isClickedGutterDescending = true;
			//this is only for Canvas UI.Button cases; we handle this in SuperShapeButton directly for most
		}
	}

	private bool isSpinning;

	[ContextMenu("Spin")]
	public void Spin() { isSpinning = true; }
	private void MySpinFunction(float period, float rx, float ry, float layerWidth)
	{
		float timePercent = (Time.time % period) / period;
		for (int i = 0; i < 1; i++)
		{
			for (int j = 0; j < vertexCount; j++)
			{
				float rad = ((float)j / (float)vertexCount + timePercent) * 2 * Mathf.PI;
				Vector2 v = new Vector2(Mathf.Cos(rad)*rx, Mathf.Sin(rad)*ry);
				baseVerts[i * layerCount + j] = v + v.normalized * i * layerWidth;
			}
		}
	}

	#region Coroutines
	//DMB coroutines

	private int moveVertsToMutexCounter = 0;
	public void MoveVertex(int layerIndex, int vertexIndex,
	                       LayerPropagationMode layerMode, VertexPropagationMode vertexMode,
	                       Vector2 delta,
	                       float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                       Lerp lerpCurve = Lerp.Linear,
	                       float delay = 0,
	                       int playCount = 1,
	                       Action<object[]> callback = null,
	                       params object[] args)
	{
		SetActive(true);
		Vector2[] endPoss = GetVertsFromTranslation(vertexIndex, layerIndex, delta, layerMode, vertexMode);
		for (int i = 0; i < baseVerts.Length; i++)
		{
			endPoss[i] -= baseVerts[i];
		}
		StartCoroutine(MoveVertsCoroutine(endPoss, duration, lerpCurve, delay, playCount, moveVertsToMutexCounter, callback, args));
	}
	
	public void MoveVertexTo(int layerIndex, int vertexIndex,
	                         LayerPropagationMode layerMode, VertexPropagationMode vertexMode,
	                         Vector2 endPos,
	                         float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                         Lerp lerpCurve = Lerp.Linear,
	                         float delay = 0,
	                         int playCount = 1,
	                         Action<object[]> callback = null,
	                         params object[] args)
	{
		SetActive(true);
		//moveVertsToMutexCounter++;
		Vector2 currentPos = baseVerts[layerIndex * vertexCount + vertexIndex];
		Vector2[] endPoss = GetVertsFromTranslation(vertexIndex, layerIndex, endPos - currentPos, layerMode, vertexMode);
		for (int i = 0; i < baseVerts.Length; i++)
		{
			endPoss[i] -= baseVerts[i];
		}
		StartCoroutine(MoveVertsCoroutine(endPoss, duration, lerpCurve, delay, playCount, moveVertsToMutexCounter, callback, args));
	}
	/*
	public void MoveLayerVerts(int layerIndex,
	                           Vector2[] deltas,
	                           float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                           Lerp lerpCurve = Lerp.Linear,
	                           float delay = 0,
	                           int playCount = 1,
	                           Action<object[]> callback = null,
	                           params object[] args)
	{
		SetActive(true);
		Vector2[] endPoss = GetVertsFromTranslation(vertexIndex, layerIndex, delta, layerMode, vertexMode);
		for (int i = 0; i < baseVerts.Length; i++)
		{
			endPoss[i] -= baseVerts[i];
		}
		StartCoroutine(MoveVertsCoroutine(endPoss, duration, lerpCurve, delay, playCount, moveVertsToMutexCounter, callback, args));
	}

	public void MoveLayerVertsTo(int layerIndex,
	                             Vector2[] endPoss,
	                             float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                             Lerp lerpCurve = Lerp.Linear,
	                             float delay = 0,
	                             int playCount = 1,
	                             Action<object[]> callback = null,
	                             params object[] args)
	{
		SetActive(true);
		moveVertsToMutexCounter++;
		Vector2 currentPos = baseVerts[layerIndex * vertexCount + vertexIndex];
		Vector2[] endPoss = GetVertsFromTranslation(vertexIndex, layerIndex, endPos - currentPos, layerMode, vertexMode);
		for (int i = 0; i < baseVerts.Length; i++)
		{
			endPoss[i] -= baseVerts[i];
		}
		StartCoroutine(MoveVertsCoroutine(endPoss, duration, lerpCurve, delay, playCount, moveVertsToMutexCounter, callback, args));
	}
	*/
	public void MoveAllVerts(Vector2[] deltas,
	                         float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                         Lerp lerpCurve = Lerp.Linear,
	                         float delay = 0,
	                         int playCount = 1,
	                         Action<object[]> callback = null,
	                         params object[] args)
	{
		if (deltas.Length != baseVerts.Length) { Debug.LogWarning("Move command length does not match SuperShape size!"); return; }
		SetActive(true);
		StartCoroutine(MoveVertsCoroutine(deltas, duration, lerpCurve, delay, playCount, moveVertsToMutexCounter, callback, args));
	}

	public void MoveAllVertsTo(Vector2[] endPoss,
	                           float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                           Lerp lerpCurve = Lerp.Linear,
	                           float delay = 0,
	                           int playCount = 1,
	                           Action<object[]> callback = null,
	                           params object[] args)
	{
		if (endPoss.Length != baseVerts.Length) { Debug.LogWarning("Move command length does not match SuperShape size!"); return; }
		moveVertsToMutexCounter++;
		SetActive(true);
		Vector2[] deltas = new Vector2[endPoss.Length];
		for (int i = 0; i < baseVerts.Length; i++)
		{
			deltas[i] = endPoss[i] - baseVerts[i]; //convert to deltas in-place
		}
		StartCoroutine(MoveVertsCoroutine(deltas, duration, lerpCurve, delay, playCount, moveVertsToMutexCounter, callback, args));
	}
	
	public void MoveAllVertsToPreset(SuperShapePreset preset,
	                                 float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                 Lerp lerpCurve = Lerp.Linear,
	                                 float delay = 0,
	                                 Action<object[]> callback = null,
	                                 params object[] args)
	{
		moveVertsToMutexCounter++;
		SetActive(true);
		Vector2[] presetPoints = GetLayeredPresetVerts(preset);
		for (int i = 0; i < baseVerts.Length; i++)
		{
			presetPoints[i] -= baseVerts[i]; //convert to deltas in-place
		}
		StartCoroutine(MoveVertsCoroutine(presetPoints, duration, lerpCurve, delay, 1, moveVertsToMutexCounter, callback, args));
	}

	public void CancelPendingMoveVertsTo()
	{
		moveVertsToMutexCounter++;
	}

	private IEnumerator MoveVertsCoroutine(Vector2[] deltas,
	                                       float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                       Lerp lerpCurve = Lerp.Linear,
	                                       float delay = 0,
	                                       int playCount = 1,
	                                       int moveVertsToMutexRequirement = -1,
	                                       Action<object[]> callback = null,
	                                       params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (baseVerts.Length != deltas.Length) { yield break; } //move data now invalid (before we even started!)
		if (duration <= 0)
		{
			for (int i = 0; i < baseVerts.Length; i++)
			{
				baseVerts[i] += deltas[i];
				prevVerts[i] += deltas[i];
				nextVerts[i] += deltas[i];
			}
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if (moveVertsToMutexRequirement != -1 && moveVertsToMutexRequirement != moveVertsToMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			for (int i = 0; i < baseVerts.Length; i++) //CAN continue with invalid data safely
			{
				if (i >= deltas.Length) { continue; }
				baseVerts[i] += relativeTimeDelta * deltas[i];
				prevVerts[i] += relativeTimeDelta * deltas[i];
				nextVerts[i] += relativeTimeDelta * deltas[i];
			}
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	public void AddLayerOverTime(float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                             Lerp lerpCurve = Lerp.Linear,
	                             float delay = 0,
	                             Action<object[]> callback = null,
	                             params object[] args)
	{
		SetLayerCount(layerCount + 1);
		outerLayerProgress = 0;
		ChangeOuterLayerProgress(1, duration, lerpCurve, delay, 1, callback, args);
	}
	public void RemoveLayerOverTime(float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                Lerp lerpCurve = Lerp.Linear,
	                                float delay = 0) //no callback allowed!
	{
		ChangeOuterLayerProgress(-outerLayerProgress, duration, lerpCurve, delay, 1, new Call(RemoveLayerOverTimeComplete).back);
	}
	private void RemoveLayerOverTimeComplete()
	{
		SetLayerCount(layerCount - 1);
		outerLayerProgress = 1;
	}

	private int changeOuterLayerProgressToMutexCounter = 0;
	public void ChangeOuterLayerProgress(float delta,
	                                     float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                     Lerp lerpCurve = Lerp.Linear,
	                                     float delay = 0,
	                                     int playCount = 1,
	                                     Action<object[]> callback = null,
	                                     params object[] args)
	{
		SetActive(true);
		StartCoroutine(OuterLayerProgressCoroutine(delta, duration, lerpCurve, delay, playCount, changeOuterLayerProgressToMutexCounter, callback, args));
	}
	public void ChangeOuterLayerProgressTo(float targetProgress,
	                                       float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                       Lerp lerpCurve = Lerp.Linear,
	                                       float delay = 0,
	                                       int playCount = 1,
	                                       Action<object[]> callback = null,
	                                       params object[] args)
	{
		changeOuterLayerProgressToMutexCounter++;
		SetActive(true);
		StartCoroutine(OuterLayerProgressCoroutine(targetProgress - outerLayerProgress, duration, lerpCurve, delay, playCount, changeOuterLayerProgressToMutexCounter, callback, args));
	}
	private IEnumerator OuterLayerProgressCoroutine(float delta,
	                                                float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                                Lerp lerpCurve = Lerp.Linear,
	                                                float delay = 0,
	                                                int playCount = 1,
	                                                int changeOuterLayerProgressToMutexRequirement = -1,
	                                                Action<object[]> callback = null,
	                                                params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			outerLayerProgress += delta;
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if (changeOuterLayerProgressToMutexRequirement != -1 && changeOuterLayerProgressToMutexRequirement != changeOuterLayerProgressToMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			outerLayerProgress += relativeTimeDelta * delta;
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	private int changeIgnoreRatioToMutexCounter = 0;
	public void ChangeIgnoreRatio(float delta,
	                                     float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                     Lerp lerpCurve = Lerp.Linear,
	                                     float delay = 0,
	                                     int playCount = 1,
	                                     Action<object[]> callback = null,
	                                     params object[] args)
	{
		SetActive(true);
		StartCoroutine(IgnoreRatioCoroutine(delta, duration, lerpCurve, delay, playCount, changeIgnoreRatioToMutexCounter, callback, args));
	}
	public void ChangeIgnoreRatioTo(float targetProgress,
	                                       float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                       Lerp lerpCurve = Lerp.Linear,
	                                       float delay = 0,
	                                       int playCount = 1,
	                                       Action<object[]> callback = null,
	                                       params object[] args)
	{
		changeIgnoreRatioToMutexCounter++;
		SetActive(true);
		StartCoroutine(IgnoreRatioCoroutine(targetProgress - ignoreRatio, duration, lerpCurve, delay, playCount, changeIgnoreRatioToMutexCounter, callback, args));
	}

	private IEnumerator IgnoreRatioCoroutine(float delta,
	                                                float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                                Lerp lerpCurve = Lerp.Linear,
	                                                float delay = 0,
	                                                int playCount = 1,
	                                                int changeIgnoreRatioToMutexRequirement = -1,
	                                                Action<object[]> callback = null,
	                                                params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			ignoreRatio += delta;
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if (changeIgnoreRatioToMutexRequirement != -1 && changeIgnoreRatioToMutexRequirement != changeIgnoreRatioToMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			ignoreRatio += relativeTimeDelta * delta;
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	public void ResizeSliceAdd(Vector2 delta,
	                           float duration = DEFUALT_RESIZE_DURATION,
	                           Lerp lerpCurve = Lerp.Linear,
	                           float delay = 0,
	                           int playCount = 1,
	                           Action<object[]> callback = null,
	                           params object[] args)
	{
		SetActive(true);
		StartCoroutine(ResizeSliceAddCoroutine(delta, duration, lerpCurve, delay, playCount, resizeSliceToMutexCounter, callback, args));
	}
	private int resizeSliceToMutexCounter = 0;
	public void ResizeSliceTo(Vector2 endDelta,
	                          float duration = DEFUALT_RESIZE_DURATION,
	                          Lerp lerpCurve = Lerp.Linear,
	                          float delay = 0,
	                          Action<object[]> callback = null,
	                          params object[] args)
	{
		resizeSliceToMutexCounter++;
		SetActive(true);
		StartCoroutine(ResizeSliceAddCoroutine(endDelta - slice, duration, lerpCurve, delay, 1, resizeSliceToMutexCounter, callback, args));
	}
	private IEnumerator ResizeSliceAddCoroutine(Vector2 delta,
	                                            float duration = DEFUALT_RESIZE_DURATION,
	                                            Lerp lerpCurve = Lerp.Linear,
	                                            float delay = 0,
	                                            int playCount = 1,
	                                            int resizeToMutexRequirement = -1,
	                                            Action<object[]> callback = null,
	                                            params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			slice += delta;
			if (slice.x < 0) { slice = new Vector2(0, slice.y); }
			if (slice.y < 0) { slice = new Vector2(slice.x, 0); }
		}
		else
		{
			float currentTime = 0;
			while (currentTime < duration && (resizeToMutexRequirement == -1 || resizeToMutexRequirement == resizeSliceToMutexCounter))
			{
				float newTime = isForcingComplete ? duration : currentTime + deltaTime;
				while (newTime >= duration)
				{
					if (playCount != 1)
					{
						newTime -= duration;
						if (playCount > 0) { playCount--; }
					}
					else { newTime = duration; break; }
				}
				float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
				slice += relativeTimeDelta * delta;
				if (slice.x < 0) { slice = new Vector2(0, slice.y); }
				if (slice.y < 0) { slice = new Vector2(slice.x, 0); }
				currentTime = newTime;
				yield return null;
			}
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	public void QuadSkewResizeAdd(Vector2 delta,
	                              float duration = DEFUALT_RESIZE_DURATION,
	                              Lerp lerpCurve = Lerp.Linear,
	                              float delay = 0,
	                              int playCount = 1,
	                              Action<object[]> callback = null,
	                              params object[] args)
	{
		SetActive(true);
		StartCoroutine(QuadSkewResizeAddCoroutine(delta, duration, lerpCurve, delay, playCount, quadSkewResizeToMutexCounter, callback, args));
	}
	private int quadSkewResizeToMutexCounter;
	public void QuadSkewResizeTo(Vector2 endDelta,
	                             float duration = DEFUALT_RESIZE_DURATION,
	                             Lerp lerpCurve = Lerp.Linear,
	                             float delay = 0,
	                             Action<object[]> callback = null,
	                             params object[] args)
	{
		quadSkewResizeToMutexCounter++;
		SetActive(true);
		StartCoroutine(QuadSkewResizeAddCoroutine(endDelta - quadSkew, duration, lerpCurve, delay, 1, quadSkewResizeToMutexCounter, callback, args));
	}
	private IEnumerator QuadSkewResizeAddCoroutine(Vector2 delta,
	                                               float duration = DEFUALT_RESIZE_DURATION,
	                                               Lerp lerpCurve = Lerp.Linear,
	                                               float delay = 0,
	                                               int playCount = 1,
	                                               int resizeToMutexRequirement = -1,
	                                               Action<object[]> callback = null,
	                                               params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			quadSkew += delta;
		}
		else
		{
			float currentTime = 0;
			while (currentTime < duration && (resizeToMutexRequirement == -1 || resizeToMutexRequirement == quadSkewResizeToMutexCounter))
			{
				float newTime = isForcingComplete ? duration : currentTime + deltaTime;
				while (newTime >= duration)
				{
					if (playCount != 1)
					{
						newTime -= duration;
						if (playCount > 0) { playCount--; }
					}
					else { newTime = duration; break; }
				}
				float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
				quadSkew += relativeTimeDelta * delta;
				currentTime = newTime;
				yield return null;
			}
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	private int changeLayerColorsToMutexCounter = 0;
	public void ChangeLayerColorAdd(int layerIndex, Color colorDelta,
	                                float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                Lerp lerpCurve = Lerp.Linear,
	                                float delay = 0,
	                                int playCount = 1,
	                                Action<object[]> callback = null,
	                                params object[] args)
	{
		SetActive(true);
		Color[] colorDeltas = new Color[layerColors.Length];
		colorDeltas[layerIndex] = colorDelta;
		StartCoroutine(ChangeLayerColorCoroutine(colorDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	[ContextMenu("White")] void White() { ChangeLayerColorTo(0, Color.white); }
	public void ChangeLayerColorTo(int layerIndex, Color color,
	                               float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                               Lerp lerpCurve = Lerp.Linear,
	                               float delay = 0,
	                               int playCount = 1,
	                               Action<object[]> callback = null,
	                               params object[] args)
	{
		changeLayerColorsToMutexCounter++;
		SetActive(true);
		Color[] colorDeltas = new Color[layerColors.Length];
		colorDeltas[layerIndex] = color - layerColors[layerIndex];
		StartCoroutine(ChangeLayerColorCoroutine(colorDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	public void ChangeLayers02ColorTo(Color color,
	                                  float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                  Lerp lerpCurve = Lerp.Linear,
	                                  float delay = 0,
	                                  int playCount = 1,
	                                  Action<object[]> callback = null,
	                                  params object[] args)
	{
		changeLayerColorsToMutexCounter++;
		SetActive(true);
		Color[] colorDeltas = new Color[layerColors.Length];
		colorDeltas[0] = color - layerColors[0];
		colorDeltas[2] = color - layerColors[2];
		StartCoroutine(ChangeLayerColorCoroutine(colorDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	public void ChangeAllLayerColorsAdd(Color[] colorDeltas,
	                                    float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                    Lerp lerpCurve = Lerp.Linear,
	                                    float delay = 0,
	                                    int playCount = 1,
	                                    Action<object[]> callback = null,
	                                    params object[] args)
	{
		if (colorDeltas.Length != layerColors.Length) { Debug.LogWarning("LayerColors command does not match SuperShape size!"); return; }
		SetActive(true);
		StartCoroutine(ChangeLayerColorCoroutine(colorDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	public void ChangeAllLayerColorsTo(Color[] targetColors,
	                                   float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                   Lerp lerpCurve = Lerp.Linear,
	                                   float delay = 0,
	                                   int playCount = 1,
	                                   Action<object[]> callback = null,
	                                   params object[] args)
	{
		if (targetColors.Length != layerColors.Length) { Debug.LogWarning("LayerColors command does not match SuperShape size!"); return; }
		changeLayerColorsToMutexCounter++;
		SetActive(true);
		Color[] colorDeltas = new Color[layerColors.Length];
		for (int i = 0; i < layerColors.Length; i++)
		{
			colorDeltas[i] = targetColors[i] - layerColors[i]; //change targets to deltas in-place
		}
		StartCoroutine(ChangeLayerColorCoroutine(colorDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	private IEnumerator ChangeLayerColorCoroutine(Color[] colorDeltas,
	                                              float duration = DEFUALT_MOVE_DURATION,
	                                              Lerp lerpCurve = Lerp.Linear,
	                                              float delay = 0,
	                                              int playCount = 1,
	                                              int changeLayerColorsToMutexRequirement = -1,
	                                              Action<object[]> callback = null,
	                                              params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (layerColors.Length != colorDeltas.Length) { yield break; } //data now invalid (before we even started!)
		if (duration <= 0)
		{
			for (int i = 0; i < layerColors.Length; i++)
			{
				layerColors[i] += colorDeltas[i];
			}
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if (changeLayerColorsToMutexRequirement != -1 && changeLayerColorsToMutexRequirement != changeLayerColorsToMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			for (int i = 0; i < layerColors.Length; i++)
			{
				if (i >= colorDeltas.Length) { continue; }
				layerColors[i] += relativeTimeDelta * colorDeltas[i];
			}
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}
		
	public void ChangeLayerColorAddHSV(int layerIndex, Vector3 colorHSVDelta,
	                                   float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                   Lerp lerpCurve = Lerp.Linear,
	                                   float delay = 0,
	                                   int playCount = 1,
	                                   Action<object[]> callback = null,
	                                   params object[] args)
	{
		SetActive(true);
		Vector3[] colorHSVDeltas = new Vector3[layerColors.Length];
		colorHSVDeltas[layerIndex] = colorHSVDelta;
		StartCoroutine(ChangeLayerColorHSVCoroutine(colorHSVDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	public void ChangeLayerColorToHSV(int layerIndex, Vector3 targetColorHSV,
	                                  float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                  Lerp lerpCurve = Lerp.Linear,
	                                  float delay = 0,
	                                  int playCount = 1,
	                                  Action<object[]> callback = null,
	                                  params object[] args)
	{
		SetActive(true);
		float h, s, v;
		Color.RGBToHSV(layerColors[layerIndex], out h, out s, out v);
		if (targetColorHSV.x <= -99) { targetColorHSV.x = h; }
		if (targetColorHSV.y <= -99) { targetColorHSV.y = s; }
		if (targetColorHSV.z <= -99) { targetColorHSV.z = v; }
		Vector3 currentColorHSV = new Vector3(h, s, v);
		Vector3[] colorHSVDeltas = new Vector3[layerColors.Length];
		colorHSVDeltas[layerIndex] = targetColorHSV - currentColorHSV;
		StartCoroutine(ChangeLayerColorHSVCoroutine(colorHSVDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	public void ChangeAllLayerColorsAddHSV(Vector3[] colorHSVDeltas,
	                                       float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                       Lerp lerpCurve = Lerp.Linear,
	                                       float delay = 0,
	                                       int playCount = 1,
	                                       Action<object[]> callback = null,
	                                       params object[] args)
	{
		if (colorHSVDeltas.Length != layerColors.Length) { Debug.LogWarning("LayerColors HSV command does not match SuperShape size!"); return; }
		SetActive(true);
		StartCoroutine(ChangeLayerColorHSVCoroutine(colorHSVDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	public void ChangeAllLayerColorsAddHSV(Vector3 colorHSVDelta,
	                                       float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                       Lerp lerpCurve = Lerp.Linear,
	                                       float delay = 0,
	                                       int playCount = 1,
	                                       Action<object[]> callback = null,
	                                       params object[] args)
	{
		SetActive(true);
		Vector3[] colorHSVDeltas = new Vector3[layerCount];
		for (int i = 0; i < colorHSVDeltas.Length; i++) { colorHSVDeltas[i] = colorHSVDelta; }
		StartCoroutine(ChangeLayerColorHSVCoroutine(colorHSVDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	public void ChangeAllLayerColorsToHSV(Vector3[] targetColorHSVs,
	                                      float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                      Lerp lerpCurve = Lerp.Linear,
	                                      float delay = 0,
	                                      int playCount = 1,
	                                      Action<object[]> callback = null,
	                                      params object[] args)
	{
		if (targetColorHSVs.Length != layerColors.Length) { Debug.LogWarning("LayerColors HSV command does not match SuperShape size!"); return; }
		SetActive(true);
		Vector3[] colorHSVDeltas = new Vector3[layerCount];
		for (int i = 0; i < layerColors.Length; i++)
		{
			float h, s, v;
			Color.RGBToHSV(layerColors[i], out h, out s, out v);
			if (targetColorHSVs[i].x <= -99) { targetColorHSVs[i].x = h; }
			if (targetColorHSVs[i].y <= -99) { targetColorHSVs[i].y = s; }
			if (targetColorHSVs[i].z <= -99) { targetColorHSVs[i].z = v; }
			Vector3 currentColorHSV = new Vector3(h, s, v);
			colorHSVDeltas[i] = targetColorHSVs[i] - currentColorHSV; //change targets to deltas in-place
		}
		StartCoroutine(ChangeLayerColorHSVCoroutine(colorHSVDeltas, duration, lerpCurve, delay, playCount, changeLayerColorsToMutexCounter, callback, args));
	}
	private IEnumerator ChangeLayerColorHSVCoroutine(Vector3[] colorHSVDeltas,
	                                                 float duration = DEFUALT_MOVE_DURATION,
	                                                 Lerp lerpCurve = Lerp.Linear,
	                                                 float delay = 0,
	                                                 int playCount = 1,
	                                                 int changeLayerColorsToMutexRequirement = -1,
	                                                 Action<object[]> callback = null,
	                                                 params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (layerColors.Length != colorHSVDeltas.Length) { yield break; } //data now invalid (before we even started!)
		if (duration <= 0)
		{
			for (int i = 0; i < layerColors.Length; i++)
			{
				float h, s, v, a;
				Color.RGBToHSV(layerColors[i], out h, out s, out v);
				a = layerColors[0].a;
				Vector3 targetColorHSV = colorHSVDeltas[i] + new Vector3(h, s, v);
				layerColors[i] = Color.HSVToRGB((targetColorHSV.x % 1.0f + 1) % 1.0f, Mathf.Clamp01(targetColorHSV.y), Mathf.Clamp01(targetColorHSV.z));
				layerColors[i].a = a;
			}
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		Vector4[] originalColors = new Vector4[layerCount];
		for (int i = 0; i < layerColors.Length; i++)
		{
			float h, s, v;
			Color.RGBToHSV(layerColors[i], out h, out s, out v);
			originalColors[i] = new Vector4(h, s, v, layerColors[i].a);
		}
		while (currentTime < duration)
		{
			if (changeLayerColorsToMutexRequirement != -1 && changeLayerColorsToMutexRequirement != changeLayerColorsToMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			for (int i = 0; i < layerColors.Length; i++)
			{
				if (i >= colorHSVDeltas.Length) { continue; }
				Vector3 targetColorHSV = colorHSVDeltas[i] + (Vector3)originalColors[i];
				Vector3 hsv = L.erp(originalColors[i], targetColorHSV, newTime / duration, lerpCurve);
				layerColors[i] = Color.HSVToRGB((hsv.x % 1.0f + 1) % 1.0f, Mathf.Clamp01(hsv.y), Mathf.Clamp01(hsv.z));
				layerColors[i].a = originalColors[i].w;
			}
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	private int tailMaxLengthToMutexCounter = 0;
	public void TailMaxLengthAdd(float delta,
	                             float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                             Lerp lerpCurve = Lerp.Linear,
	                             float delay = 0,
	                             int playCount = 1,
	                             Action<object[]> callback = null,
	                             params object[] args)
	{
		SetActive(true);
		StartCoroutine(TailMaxLengthCoroutine(delta, duration, lerpCurve, delay, playCount, tailMaxLengthToMutexCounter, callback, args));
	}
	public void TailMaxLengthTo(float targetLength,
	                            float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                            Lerp lerpCurve = Lerp.Linear,
	                            float delay = 0,
	                            int playCount = 1,
	                            Action<object[]> callback = null,
	                            params object[] args)
	{
		tailBaseWidthToMutexCounter++;
		SetActive(true);
		StartCoroutine(TailMaxLengthCoroutine(targetLength - tailMaxLength, duration, lerpCurve, delay, playCount, tailMaxLengthToMutexCounter, callback, args));
	}
	private IEnumerator TailMaxLengthCoroutine(float delta,
	                                           float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                           Lerp lerpCurve = Lerp.Linear,
	                                           float delay = 0,
	                                           int playCount = 1,
	                                           int tailMaxLengthToMutexRequirement = -1,
	                                           Action<object[]> callback = null,
	                                           params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			tailMaxLength += delta;
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if (tailMaxLengthToMutexRequirement != -1 && tailMaxLengthToMutexRequirement != tailMaxLengthToMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			tailMaxLength += relativeTimeDelta * delta;
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	private int tailProgressToMutexCounter = 0;
	public void TailProgressAdd(float delta,
	                            float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                            Lerp lerpCurve = Lerp.Linear,
	                            float delay = 0,
	                            int playCount = 1,
	                            Action<object[]> callback = null,
	                            params object[] args)
	{
		SetActive(true);
		StartCoroutine(TailProgressCoroutine(delta, duration, lerpCurve, delay, playCount, tailProgressToMutexCounter, callback, args));
	}
	public void TailProgressTo(float targetProgress,
	                           float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                           Lerp lerpCurve = Lerp.Linear,
	                           float delay = 0,
	                           int playCount = 1,
	                           Action<object[]> callback = null,
	                           params object[] args)
	{
		tailProgressToMutexCounter++;
		SetActive(true);
		StartCoroutine(TailProgressCoroutine(targetProgress - tailProgressPercentage, duration, lerpCurve, delay, playCount, tailProgressToMutexCounter, callback, args));
	}
	private IEnumerator TailProgressCoroutine(float delta,
	                                          float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                          Lerp lerpCurve = Lerp.Linear,
	                                          float delay = 0,
	                                          int playCount = 1,
	                                          int tailProgressToMutexRequirement = -1,
	                                          Action<object[]> callback = null,
	                                          params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			tailProgressPercentage += delta;
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if (tailProgressToMutexRequirement != -1 && tailProgressToMutexRequirement != tailProgressToMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			tailProgressPercentage += relativeTimeDelta * delta;
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	private int tailBaseWidthToMutexCounter = 0;
	public void TailBaseWidthAdd(float delta,
	                             float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                             Lerp lerpCurve = Lerp.Linear,
	                             float delay = 0,
	                             int playCount = 1,
	                             Action<object[]> callback = null,
	                             params object[] args)
	{
		SetActive(true);
		StartCoroutine(TailBaseWidthCoroutine(delta, duration, lerpCurve, delay, playCount, tailBaseWidthToMutexCounter, callback, args));
	}
	public void TailBaseWidthTo(float targetWidth,
	                            float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                            Lerp lerpCurve = Lerp.Linear,
	                            float delay = 0,
	                            int playCount = 1,
	                            Action<object[]> callback = null,
	                            params object[] args)
	{
		tailBaseWidthToMutexCounter++;
		SetActive(true);
		StartCoroutine(TailBaseWidthCoroutine(targetWidth - tailBaseWidth, duration, lerpCurve, delay, playCount, tailBaseWidthToMutexCounter, callback, args));
	}
	private IEnumerator TailBaseWidthCoroutine(float delta,
	                                           float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                           Lerp lerpCurve = Lerp.Linear,
	                                           float delay = 0,
	                                           int playCount = 1,
	                                           int tailBaseWidthToMutexRequirement = -1,
	                                           Action<object[]> callback = null,
	                                           params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			tailBaseWidth += delta;
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if (tailBaseWidthToMutexRequirement != -1 && tailBaseWidthToMutexRequirement != tailBaseWidthToMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			tailBaseWidth += relativeTimeDelta * delta;
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	private int cornerAvoidanceToMutexCounter = 0;
	public void CornerAvoidanceAdd(float delta,
	                               float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                               Lerp lerpCurve = Lerp.Linear,
	                               float delay = 0,
	                               int playCount = 1,
	                               Action<object[]> callback = null,
	                               params object[] args)
	{
		SetActive(true);
		StartCoroutine(CornerAvoidanceCoroutine(delta, duration, lerpCurve, delay, playCount, cornerAvoidanceToMutexCounter, callback, args));
	}
	private IEnumerator CornerAvoidanceCoroutine(float delta,
	                                             float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                             Lerp lerpCurve = Lerp.Linear,
	                                             float delay = 0,
	                                             int playCount = 1,
	                                             int cornerAvoidanceToMutexRequirement = -1,
	                                             Action<object[]> callback = null,
	                                             params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			tailBaseCornerAvoidance += delta;
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if (cornerAvoidanceToMutexRequirement != -1 && cornerAvoidanceToMutexRequirement != cornerAvoidanceToMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			tailBaseCornerAvoidance += relativeTimeDelta * delta;
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		if (callback != null) { callback(args); }
	}
	
	private int texture1ScaleToMutexCounter = 0;
	private int texture2ScaleToMutexCounter = 0;
	public void TextureScaleAdd(DMBMotionTargetTexture target,
	                            Vector2 delta,
	                            float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                            Lerp lerpCurve = Lerp.Linear,
	                            float delay = 0,
	                            int playCount = 1,
	                            Action<object[]> callback = null,
	                            params object[] args)
	{
		SetActive(true);
		StartCoroutine(TextureScaleCoroutine(target, delta, duration, lerpCurve, delay, playCount,
		                                     target == DMBMotionTargetTexture.Texture1 ? texture1ScaleToMutexCounter : texture2ScaleToMutexCounter, callback, args));
	}
	public void TextureScaleTo(DMBMotionTargetTexture target,
	                           Vector2 targetScale,
	                           float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                           Lerp lerpCurve = Lerp.Linear,
	                           float delay = 0,
	                           int playCount = 1,
	                           Action<object[]> callback = null,
	                           params object[] args)
	{
		if      (target == DMBMotionTargetTexture.Texture1) { texture1ScaleToMutexCounter++; }
		else if (target == DMBMotionTargetTexture.Texture2) { texture2ScaleToMutexCounter++; }
		SetActive(true);
		StartCoroutine(TextureScaleCoroutine(target, targetScale - (target == DMBMotionTargetTexture.Texture1 ? fillTexture1Scale : fillTexture2Scale), duration, lerpCurve, delay, playCount,
		                                     target == DMBMotionTargetTexture.Texture1 ? texture1ScaleToMutexCounter : texture2ScaleToMutexCounter, callback, args));
	}
	private IEnumerator TextureScaleCoroutine(DMBMotionTargetTexture target,
	                                          Vector2 delta,
	                                          float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                          Lerp lerpCurve = Lerp.Linear,
	                                          float delay = 0,
	                                          int playCount = 1,
	                                          int textureScaleToMutexRequirement = -1,
	                                          Action<object[]> callback = null,
	                                          params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Scale  += delta; }
			else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Scale += delta; }
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if ((target == DMBMotionTargetTexture.Texture1 && textureScaleToMutexRequirement < texture1ScaleToMutexCounter) ||
			    (target == DMBMotionTargetTexture.Texture2 && textureScaleToMutexRequirement < texture2ScaleToMutexCounter))
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Scale  += relativeTimeDelta * delta; }
			else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Scale += relativeTimeDelta * delta; }
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	private int texture1OffsetToMutexCounter = 0;
	private int texture2OffsetToMutexCounter = 0;
	public void TextureOffsetAdd(DMBMotionTargetTexture target,
	                             Vector2 delta,
	                             float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                             Lerp lerpCurve = Lerp.Linear,
	                             float delay = 0,
	                             int playCount = 1,
	                             Action<object[]> callback = null,
	                             params object[] args)
	{
		SetActive(true);
		StartCoroutine(TextureOffsetCoroutine(target, delta, duration, lerpCurve, delay, playCount,
		                                      target == DMBMotionTargetTexture.Texture1 ? texture1OffsetToMutexCounter : texture2OffsetToMutexCounter, callback, args));
	}
	public void TextureOffsetTo(DMBMotionTargetTexture target,
	                            Vector2 targetOffset,
	                            float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                            Lerp lerpCurve = Lerp.Linear,
	                            float delay = 0,
	                            int playCount = 1,
	                            Action<object[]> callback = null,
	                            params object[] args)
	{
		if      (target == DMBMotionTargetTexture.Texture1) { texture1OffsetToMutexCounter++; }
		else if (target == DMBMotionTargetTexture.Texture2) { texture2OffsetToMutexCounter++; }
		SetActive(true);
		StartCoroutine(TextureOffsetCoroutine(target, targetOffset - (target == DMBMotionTargetTexture.Texture1 ? fillTexture1Offset : fillTexture2Offset), duration, lerpCurve, delay, playCount,
		                                      target == DMBMotionTargetTexture.Texture1 ? texture1OffsetToMutexCounter : texture2OffsetToMutexCounter, callback, args));
	}
	private IEnumerator TextureOffsetCoroutine(DMBMotionTargetTexture target,
	                                           Vector2 delta,
	                                           float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                           Lerp lerpCurve = Lerp.Linear,
	                                           float delay = 0,
	                                           int playCount = 1,
	                                           int textureOffsetToMutexRequirement = -1,
	                                           Action<object[]> callback = null,
	                                           params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Offset  += delta; }
			else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Offset += delta; }
			
			SetMeshDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if ((target == DMBMotionTargetTexture.Texture1 && textureOffsetToMutexRequirement < texture1OffsetToMutexCounter) ||
			    (target == DMBMotionTargetTexture.Texture2 && textureOffsetToMutexRequirement < texture2OffsetToMutexCounter))
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Offset  += relativeTimeDelta * delta; }
			else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Offset += relativeTimeDelta * delta; }
			SetMeshDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}
	public void ChangeTextureColorAdd(DMBMotionTargetTexture target,
	                                  Vector3 targetColorRGBDelta,
	                                  float duration = DEFUALT_MOVE_DURATION,
	                                  Lerp lerpCurve = Lerp.Linear,
	                                  float delay = 0,
	                                  Action<object[]> callback = null,
	                                  params object[] args)
	{
		SetActive(true);
		StartCoroutine(TextureColorCoroutine(target, (target == DMBMotionTargetTexture.Texture1 ? fillTexture1Tint : fillTexture2Tint) +
			                                         new Color(targetColorRGBDelta.x, targetColorRGBDelta.y, targetColorRGBDelta.z, target == DMBMotionTargetTexture.Texture1 ? fillTexture1Tint.a : fillTexture2Tint.a), duration, lerpCurve, delay,
		                                     target == DMBMotionTargetTexture.Texture1 ? texture1ColorMutexCounter : texture2ColorMutexCounter, callback, args));
	}
	public void ChangeTextureColorAdd(DMBMotionTargetTexture target, 
	                           Color targetColor,
	                           float duration = DEFUALT_MOVE_DURATION,
	                           Lerp lerpCurve = Lerp.Linear,
	                           float delay = 0,
	                           Action<object[]> callback = null,
	                           params object[] args)
	{
		SetActive(true);
		StartCoroutine(TextureColorCoroutine(target, targetColor + (target == DMBMotionTargetTexture.Texture1 ? fillTexture1Tint : fillTexture2Tint), duration, lerpCurve, delay,
		                                     target == DMBMotionTargetTexture.Texture1 ? texture1ColorMutexCounter : texture2ColorMutexCounter, callback, args));
	}
	private int texture1ColorMutexCounter = 0;
	private int texture2ColorMutexCounter = 0;
	public void ChangeTextureColorTo(DMBMotionTargetTexture target,
	                                 Vector3 targetColorRGB,
	                                 float duration = DEFUALT_MOVE_DURATION,
	                                 Lerp lerpCurve = Lerp.Linear,
	                                 float delay = 0,
	                                 Action<object[]> callback = null,
	                                 params object[] args)
	{
		if      (target == DMBMotionTargetTexture.Texture1) { texture1ColorMutexCounter++; }
		else if (target == DMBMotionTargetTexture.Texture2) { texture2ColorMutexCounter++; }
		SetActive(true);
		StartCoroutine(TextureColorCoroutine(target, new Color(targetColorRGB.x, targetColorRGB.y, targetColorRGB.z, target == DMBMotionTargetTexture.Texture1 ? fillTexture1Tint.a : fillTexture2Tint.a), duration, lerpCurve, delay,
		                                     target == DMBMotionTargetTexture.Texture1 ? texture1ColorMutexCounter: texture2ColorMutexCounter, callback, args));
	}
	public void ChangeTextureColorTo(DMBMotionTargetTexture target,
	                                 Color targetColor,
	                                 float duration = DEFUALT_MOVE_DURATION,
	                                 Lerp lerpCurve = Lerp.Linear,
	                                 float delay = 0,
	                                 Action<object[]> callback = null,
	                                 params object[] args)
	{
		if      (target == DMBMotionTargetTexture.Texture1) { texture1ColorMutexCounter++; }
		else if (target == DMBMotionTargetTexture.Texture2) { texture2ColorMutexCounter++; }
		SetActive(true);
		StartCoroutine(TextureColorCoroutine(target, targetColor, duration, lerpCurve, delay, target == DMBMotionTargetTexture.Texture1 ? texture1ColorMutexCounter : texture2ColorMutexCounter, callback, args));
	}

	private IEnumerator TextureColorCoroutine(DMBMotionTargetTexture target,
	                                          Color targetColor,
	                                          float duration = DEFUALT_RESIZE_DURATION,
	                                          Lerp lerpCurve = Lerp.Linear,
	                                          float delay = 0,
	                                          int colorMutexRequirement = -1,
	                                          Action<object[]> callback = null,
	                                          params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Tint  = targetColor; }
			else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Tint = targetColor; }
		}
		else
		{
			float currentTime = 0;
			Color originalColor = target == DMBMotionTargetTexture.Texture1 ? fillTexture1Tint : fillTexture2Tint;
			while (currentTime < duration && ((target == DMBMotionTargetTexture.Texture1 && (texture1ColorMutexCounter == -1 || colorMutexRequirement == texture1ColorMutexCounter)) ||
			                                  (target == DMBMotionTargetTexture.Texture2 && (texture2ColorMutexCounter == -1 || colorMutexRequirement == texture2ColorMutexCounter))))
			{
				float newTime = isForcingComplete ? duration : Mathf.Clamp(currentTime + deltaTime, 0, duration);
				if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Tint  = Color.Lerp(originalColor, targetColor, L.erp(newTime / duration, lerpCurve)); }
				else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Tint = Color.Lerp(originalColor, targetColor, L.erp(newTime / duration, lerpCurve)); }
				currentTime = newTime;
				yield return null;
			}
		}
		yield return null;
		if (callback != null) { callback(args); }
	}
	
	public void ChangeTextureColorHSVAdd(DMBMotionTargetTexture target, 
	                                     Vector3 targetColorHSVDelta,
	                                     float duration = DEFUALT_MOVE_DURATION,
	                                     Lerp lerpCurve = Lerp.Linear,
	                                     float delay = 0,
	                                     Action<object[]> callback = null,
	                                     params object[] args)
	{
		SetActive(true);
		Color.RGBToHSV(target == DMBMotionTargetTexture.Texture1 ? fillTexture1Tint : fillTexture2Tint, out float h, out float s, out float v);
		StartCoroutine(TextureColorHSVCoroutine(target, targetColorHSVDelta + new Vector3(h, s, v), duration, lerpCurve, delay, target == DMBMotionTargetTexture.Texture1 ? texture1ColorMutexCounter : texture2ColorMutexCounter, callback, args));
	}
	public void ChangeTextureColorHSVTo(DMBMotionTargetTexture target,
	                                    Vector3 targetColorHSV,
	                                    float duration = DEFUALT_MOVE_DURATION,
	                                    Lerp lerpCurve = Lerp.Linear,
	                                    float delay = 0,
	                                    Action<object[]> callback = null,
	                                    params object[] args)
	{
		if      (target == DMBMotionTargetTexture.Texture1) { texture1ColorMutexCounter++; }
		else if (target == DMBMotionTargetTexture.Texture2) { texture2ColorMutexCounter++; }
		SetActive(true);
		Color.RGBToHSV(target == DMBMotionTargetTexture.Texture1 ? fillTexture1Tint : fillTexture2Tint, out float h, out float s, out float v);
		if (targetColorHSV.x <= -99) { targetColorHSV.x = h; }
		if (targetColorHSV.y <= -99) { targetColorHSV.y = s; }
		if (targetColorHSV.z <= -99) { targetColorHSV.z = v; }
		StartCoroutine(TextureColorHSVCoroutine(target, targetColorHSV, duration, lerpCurve, delay, target == DMBMotionTargetTexture.Texture1 ? texture1ColorMutexCounter : texture2ColorMutexCounter, callback, args));
	}
	private IEnumerator TextureColorHSVCoroutine(DMBMotionTargetTexture target,
	                                             Vector3 targetColorHSV,
	                                             float duration = DEFUALT_RESIZE_DURATION,
	                                             Lerp lerpCurve = Lerp.Linear,
	                                             float delay = 0,
	                                             int colorMutexRequirement = -1,
	                                             Action<object[]> callback = null,
	                                             params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Tint  = Color.HSVToRGB((targetColorHSV.x % 1.0f + 1) % 1.0f, Mathf.Clamp01(targetColorHSV.y), Mathf.Clamp01(targetColorHSV.z)); }
			else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Tint = Color.HSVToRGB((targetColorHSV.x % 1.0f + 1) % 1.0f, Mathf.Clamp01(targetColorHSV.y), Mathf.Clamp01(targetColorHSV.z)); }
		}
		else
		{
			float currentTime = 0;
			Color.RGBToHSV(target == DMBMotionTargetTexture.Texture1 ? fillTexture1Tint : fillTexture2Tint, out float h, out float s, out float v);
			Vector3 originalColorHSV = new Vector3(h, s, v);
			while (currentTime < duration && ((target == DMBMotionTargetTexture.Texture1 && (texture1ColorMutexCounter == -1 || colorMutexRequirement == texture1ColorMutexCounter)) ||
			                                  (target == DMBMotionTargetTexture.Texture2 && (texture2ColorMutexCounter == -1 || colorMutexRequirement == texture2ColorMutexCounter))))
			{
				float newTime = isForcingComplete ? duration : Mathf.Clamp(currentTime + deltaTime, 0, duration);
				Vector3 hsv = L.erp(originalColorHSV, targetColorHSV, newTime / duration, lerpCurve);
				if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Tint  = Color.HSVToRGB((hsv.x % 1.0f + 1) % 1.0f, Mathf.Clamp01(hsv.y), Mathf.Clamp01(hsv.z)); }
				else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Tint = Color.HSVToRGB((hsv.x % 1.0f + 1) % 1.0f, Mathf.Clamp01(hsv.y), Mathf.Clamp01(hsv.z)); }
				currentTime = newTime;
				yield return null;
			}
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	private int texture1AlphaToMutexCounter = 0;
	private int texture2AlphaToMutexCounter = 0;
	public void TextureAlphaAdd(DMBMotionTargetTexture target,
	                            float delta,
	                            float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                            Lerp lerpCurve = Lerp.Linear,
	                            float delay = 0,
	                            int playCount = 1,
	                            Action<object[]> callback = null,
	                            params object[] args)
	{
		SetActive(true);
		StartCoroutine(TextureAlphaCoroutine(target, delta, duration, lerpCurve, delay, playCount, target == DMBMotionTargetTexture.Texture1 ? texture1AlphaToMutexCounter : texture2AlphaToMutexCounter, callback, args));
	}
	public void TextureAlphaTo(DMBMotionTargetTexture target,
	                           float alphaTarget,
	                           float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                           Lerp lerpCurve = Lerp.Linear,
	                           float delay = 0,
	                           int playCount = 1,
	                           Action<object[]> callback = null,
	                           params object[] args)
	{
		if      (target == DMBMotionTargetTexture.Texture1) { texture1AlphaToMutexCounter++; }
		else if (target == DMBMotionTargetTexture.Texture2) { texture2AlphaToMutexCounter++; }
		SetActive(true);
		StartCoroutine(TextureAlphaCoroutine(target, alphaTarget - fillTexture1Alpha, duration, lerpCurve, delay, playCount, target == DMBMotionTargetTexture.Texture1 ? texture1AlphaToMutexCounter : texture2AlphaToMutexCounter, callback, args));
	}
	private IEnumerator TextureAlphaCoroutine(DMBMotionTargetTexture target, 
	                                          float delta,
	                                          float duration = DEFUALT_SUPERSHAPE_ANIM_DURATION,
	                                          Lerp lerpCurve = Lerp.Linear,
	                                          float delay = 0,
	                                          int playCount = 1,
	                                          int textureAlphaToMutexRequirement = -1,
	                                          Action<object[]> callback = null,
	                                          params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Alpha  += delta; }
			else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Alpha += delta; }
			SetMaterialDirty();
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if ((target == DMBMotionTargetTexture.Texture1 && textureAlphaToMutexRequirement < texture1AlphaToMutexCounter) ||
			    (target == DMBMotionTargetTexture.Texture2 && textureAlphaToMutexRequirement < texture2AlphaToMutexCounter))
			{
				Debug.Log("break");
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + deltaTime;
			while (newTime >= duration)
			{
				if (playCount != 1)
				{
					newTime -= duration;
					if (playCount > 0) { playCount--; }
				}
				else { newTime = duration; break; }
			}
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			if      (target == DMBMotionTargetTexture.Texture1) { fillTexture1Alpha  += relativeTimeDelta * delta; }
			else if (target == DMBMotionTargetTexture.Texture2) { fillTexture2Alpha += relativeTimeDelta * delta; }
			SetMaterialDirty();
			currentTime = newTime;
			yield return null;
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	public override void CancelAllCoroutines()
	{
		base.CancelAllCoroutines();
		CancelPendingMoveVertsTo();
		resizeSliceToMutexCounter++;
		quadSkewResizeToMutexCounter++;
		changeLayerColorsToMutexCounter++;
		changeOuterLayerProgressToMutexCounter++;
		changeIgnoreRatioToMutexCounter++;
		tailBaseWidthToMutexCounter++;
		tailMaxLengthToMutexCounter++;
		tailProgressToMutexCounter++;
		cornerAvoidanceToMutexCounter++;
		texture1OffsetToMutexCounter++;
		texture2OffsetToMutexCounter++;
		texture1ScaleToMutexCounter++;
		texture2ScaleToMutexCounter++;
		texture1ColorMutexCounter++;
		texture2ColorMutexCounter++;
		texture1AlphaToMutexCounter++;
		texture2AlphaToMutexCounter++;
	}

	#endregion
}