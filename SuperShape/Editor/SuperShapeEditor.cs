using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(SuperShape))]
[CanEditMultipleObjects]
public class SuperShapeEditor : Editor
{
	private const float BUTTON_SIZE = 36f;

	private static readonly GUIContent blendModeContent = new GUIContent("Blend Mode", "Algorithm to be used when blending render layers.");
	private static readonly int blendModeEnumLength = System.Enum.GetNames(typeof(BlendModes.BlendMode)).Length;

	SerializedProperty editorResizeRectOffsetProp, editorPivotVertexIndexProp, editorPivotLayerIndexProp,
	                   tailModeProp, tailTransformProp, tailTipFixedProp, tailBaseFixedProp, tailMaxLengthProp, tailExtraLengthProp, tailBaseWidthProp,
	                   tailCornerAvoidanceProp, tailBannedSideIndexProp,
	                   isTailArrowProp, isTailFlippedProp, tailLayerThicknessProp, tailBoltSegmentDistanceProp, tailProgressPercentageProp,
	                   sliceProp, isSliceLockProp,
	                   quadSkewProp,
	                   bridgeTargetProp, bridgeTargetRectPosDriverProp, bridgeMySideIndexProp, bridgeTargetSideIndexProp,
	                   bridgeLayerInnerWidthProp, bridgeLayerOuterWidthProp,
					   bridgeProgressAProp, bridgeProgressBProp, bridgeProgressCProp, bridgeProgressDProp,


					   isGutterProp, isGutter3DProp, isGutter3DSameSideProp,
	                   gutterLineThicknessProp, gutterSideProp, gutterTotalMovementProp, gutterContainerProp,

	                   blendModeProp, layerColorsProp,
	                   textureCountProp, textureModeProp, textureSubModeProp, fixedUVRangeProp,
	                   texture1Prop, texture1OffsetProp, texture1ScaleProp, texture1AlphaProp,
	                   texture2Prop, texture2OffsetProp, texture2ScaleProp, texture2AlphaProp, 
	                   maskModeProp, isUnifiedGrabProp,

	                   wiggleProfilesProp, isWigglePausedProp, isWigglingInEditorProp,

	                   isRespectingTintGroupProp,

	                   presetProp;

	//foldouts
	bool isShowingResizeOptions = true;
	bool isShowingTailOptions = true;
	bool isShowingGutterOptions = true;
	bool isShowingTextureOptions = true;
	bool isShowingWiggleOptions = true;

	static bool isEditingVerts; // true if we're in vert edit mode in the scene view
	static bool isEditingWiggleZones; // true if we're in zone edit mode in the scene view
	static bool isEditingTailWiggleZones;
	static Tool preEditTool = Tool.None; // the tool the user had selected before clicking edit

	static LayerPropagationMode lPropMode;
	static VertexPropagationMode vPropMode;
	static bool isDeleteMode;
	static bool isDeleteRecalc;

	static Texture buttonTex_LProp_NONE;
	static GUIContent button_LProp_NONE;
	static Texture buttonTex_LProp_OutwardDirect;
	static GUIContent button_LProp_OutwardDirect;
	static Texture buttonTex_LProp_OutwardRadial;
	static GUIContent button_LProp_OutwardRadial;
	static Texture buttonTex_LProp_OutwardPivot;
	static GUIContent button_LProp_OutwardPivot;
	static Texture buttonTex_LProp_InwardDirect;
	static GUIContent button_LProp_InwardDirect;
	static Texture buttonTex_LProp_InwardRadial;
	static GUIContent button_LProp_InwardRadial;
	static Texture buttonTex_LProp_InwardPivot;
	static GUIContent button_LProp_InwardPivot;
	static Texture buttonTex_LProp_AllDirect;
	static GUIContent button_LProp_AllDirect;
	static Texture buttonTex_LProp_AllRadial;
	static GUIContent button_LProp_AllRadial;
	static Texture buttonTex_LProp_AlternatingInverse;
	static GUIContent button_LProp_AlternatingInverse;
	static Texture buttonTex_LProp_NextOnlyInverse;
	static GUIContent button_LProp_NextOnlyInverse;

	static Texture buttonTex_VProp_NONE;
	static GUIContent button_VProp_NONE;
	static Texture buttonTex_VProp_NONE_RecalcNeighbors;
	static GUIContent button_VProp_NONE_RecalcNeighbors;
	static Texture buttonTex_VProp_NextOnly;
	static GUIContent button_VProp_NextOnly;
	static Texture buttonTex_VProp_NextOnly_RecalcNeighbors;
	static GUIContent button_VProp_NextOnly_RecalcNeighbors;
	static Texture buttonTex_VProp_Direct;
	static GUIContent button_VProp_Direct;
	static Texture buttonTex_VProp_Radial;
	static GUIContent button_VProp_Radial;
	static Texture buttonTex_VProp_AlternatingInverse;
	static GUIContent button_VProp_AlternatingInverse;
	static Texture buttonTex_VProp_MirroredXDirect;
	static GUIContent button_VProp_MirroredXDirect;
	static Texture buttonTex_VProp_MirroredXScaled;
	static GUIContent button_VProp_MirroredXScaled;
	static Texture buttonTex_VProp_MirroredYDirect;
	static GUIContent button_VProp_MirroredYDirect;
	static Texture buttonTex_VProp_MirroredYScaled;
	static GUIContent button_VProp_MirroredYScaled;

	static Texture buttonTex_Delete;
	static GUIContent button_Delete;
	static Texture buttonTex_DeleteRecalc;
	static GUIContent button_DeleteRecalc;

	void OnEnable()
	{
		SuperShape shape = (SuperShape)serializedObject.targetObject;

		editorResizeRectOffsetProp = serializedObject.FindProperty("editorResizeRectOffset");
		editorPivotVertexIndexProp = serializedObject.FindProperty("editorSetPivotVertex");
		editorPivotLayerIndexProp = serializedObject.FindProperty("editorSetPivotLayer");

		tailModeProp = serializedObject.FindProperty("tailMode");
		tailTransformProp = serializedObject.FindProperty("tailTipTransform");
		tailTipFixedProp = serializedObject.FindProperty("isTailTipFixed");
		tailBaseFixedProp = serializedObject.FindProperty("isTailBaseFixed");
		tailMaxLengthProp = serializedObject.FindProperty("tailMaxLength");
		tailExtraLengthProp = serializedObject.FindProperty("tailExtraLength");
		tailBaseWidthProp = serializedObject.FindProperty("tailBaseWidth");
		tailCornerAvoidanceProp = serializedObject.FindProperty("tailBaseCornerAvoidance");
		tailBannedSideIndexProp = serializedObject.FindProperty("tailBannedSideIndex");
		isTailArrowProp = serializedObject.FindProperty("isTailArrow");
		isTailFlippedProp = serializedObject.FindProperty("isTailFlipped");
		tailLayerThicknessProp = serializedObject.FindProperty("tailLayerThickness");
		tailBoltSegmentDistanceProp = serializedObject.FindProperty("tailBoltSegmentDistance");
		tailProgressPercentageProp = serializedObject.FindProperty("tailProgressPercentage");
		sliceProp = serializedObject.FindProperty("_slice");
		isSliceLockProp = serializedObject.FindProperty("isRectTransformedLockedToSlice");
		quadSkewProp = serializedObject.FindProperty("_quadSkew");

		bridgeTargetProp = serializedObject.FindProperty("bridgeTarget");
		bridgeTargetRectPosDriverProp = serializedObject.FindProperty("bridgeTargetRectPosDriver");
		bridgeMySideIndexProp = serializedObject.FindProperty("bridgeMySideIndex");
		bridgeTargetSideIndexProp = serializedObject.FindProperty("bridgeTargetSideIndex");
		bridgeLayerInnerWidthProp = serializedObject.FindProperty("bridgeLayerInnerWidth");
		bridgeLayerOuterWidthProp = serializedObject.FindProperty("bridgeLayerOuterWidth");
		bridgeProgressAProp = serializedObject.FindProperty("bridgeProgressA");
		bridgeProgressBProp = serializedObject.FindProperty("bridgeProgressB");
		bridgeProgressCProp = serializedObject.FindProperty("bridgeProgressC");
		bridgeProgressDProp = serializedObject.FindProperty("bridgeProgressD");

		isGutterProp = serializedObject.FindProperty("isGutter");
		isGutter3DProp = serializedObject.FindProperty("isGutter3D");
		isGutter3DSameSideProp = serializedObject.FindProperty("isGutter3DSameSide");
		gutterLineThicknessProp = serializedObject.FindProperty("gutterLineThickness");
		gutterSideProp = serializedObject.FindProperty("gutterSide");
		gutterTotalMovementProp = serializedObject.FindProperty("gutterTotalMovement");
		gutterContainerProp = serializedObject.FindProperty("gutterContainer");

		blendModeProp = serializedObject.FindProperty("_blendMode");
		layerColorsProp = serializedObject.FindProperty("_layerColors");
		textureCountProp = serializedObject.FindProperty("_textureCount");
		textureModeProp = serializedObject.FindProperty("_innerTextureMode");
		textureSubModeProp = serializedObject.FindProperty("_innerTextureSubMode");
		fixedUVRangeProp = serializedObject.FindProperty("_fixedUVRange");
		texture1Prop = serializedObject.FindProperty("_fillTexture1");
		texture1OffsetProp = serializedObject.FindProperty("_fillTexture1Offset");
		texture1ScaleProp = serializedObject.FindProperty("_fillTexture1Scale");
		texture1AlphaProp = serializedObject.FindProperty("_fillTexture1Alpha");
		texture2Prop = serializedObject.FindProperty("_fillTexture2");
		texture2OffsetProp = serializedObject.FindProperty("_fillTexture2Offset");
		texture2ScaleProp = serializedObject.FindProperty("_fillTexture2Scale");
		texture2AlphaProp = serializedObject.FindProperty("_fillTexture2Alpha");
		maskModeProp = serializedObject.FindProperty("_maskMode");
		isUnifiedGrabProp = serializedObject.FindProperty("_isUnifiedGrabEnabled");

		wiggleProfilesProp = serializedObject.FindProperty("wiggleProfiles");
		isWigglePausedProp = serializedObject.FindProperty("isWigglePaused");
		isWigglingInEditorProp = serializedObject.FindProperty("isWigglingInEditor");

		isRespectingTintGroupProp = serializedObject.FindProperty("_isRespectingTintGroup");

		presetProp = serializedObject.FindProperty("editorSelectedPreset");

		buttonTex_LProp_NONE = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_NONE.png", typeof(Texture));
		button_LProp_NONE = new GUIContent(buttonTex_LProp_NONE);
		buttonTex_LProp_OutwardDirect = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_OutwardDirect.png", typeof(Texture));
		button_LProp_OutwardDirect = new GUIContent(buttonTex_LProp_OutwardDirect);
		buttonTex_LProp_OutwardRadial = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_OutwardRadial.png", typeof(Texture));
		button_LProp_OutwardRadial = new GUIContent(buttonTex_LProp_OutwardRadial);
		buttonTex_LProp_OutwardPivot = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_OutwardPivot.png", typeof(Texture));
		button_LProp_OutwardPivot = new GUIContent(buttonTex_LProp_OutwardPivot);
		buttonTex_LProp_InwardDirect = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_InwardDirect.png", typeof(Texture));
		button_LProp_InwardDirect = new GUIContent(buttonTex_LProp_InwardDirect);
		buttonTex_LProp_InwardRadial = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_InwardRadial.png", typeof(Texture));
		button_LProp_InwardRadial = new GUIContent(buttonTex_LProp_InwardRadial);
		buttonTex_LProp_InwardPivot = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_InwardPivot.png", typeof(Texture));
		button_LProp_InwardPivot = new GUIContent(buttonTex_LProp_InwardPivot);
		buttonTex_LProp_AllDirect = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_AllDirect.png", typeof(Texture));
		button_LProp_AllDirect = new GUIContent(buttonTex_LProp_AllDirect);
		buttonTex_LProp_AllRadial = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_AllRadial.png", typeof(Texture));
		button_LProp_AllRadial = new GUIContent(buttonTex_LProp_AllRadial);
		buttonTex_LProp_AlternatingInverse = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_AlternatingInverse.png", typeof(Texture));
		button_LProp_AlternatingInverse = new GUIContent(buttonTex_LProp_AlternatingInverse);
		buttonTex_LProp_NextOnlyInverse = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/LPropButton_NextOnlyInverse.png", typeof(Texture));
		button_LProp_NextOnlyInverse = new GUIContent(buttonTex_LProp_NextOnlyInverse);

		buttonTex_VProp_NONE = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_NONE.png", typeof(Texture));
		button_VProp_NONE = new GUIContent(buttonTex_VProp_NONE);
		buttonTex_VProp_NONE_RecalcNeighbors = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_NONE_Recalc.png", typeof(Texture));
		button_VProp_NONE_RecalcNeighbors = new GUIContent(buttonTex_VProp_NONE_RecalcNeighbors);
		buttonTex_VProp_NextOnly = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_NextOnly.png", typeof(Texture));
		button_VProp_NextOnly = new GUIContent(buttonTex_VProp_NextOnly);
		buttonTex_VProp_NextOnly_RecalcNeighbors = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_NextOnly_Recalc.png", typeof(Texture));
		button_VProp_NextOnly_RecalcNeighbors = new GUIContent(buttonTex_VProp_NextOnly_RecalcNeighbors);
		buttonTex_VProp_Direct = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_Direct.png", typeof(Texture));
		button_VProp_Direct = new GUIContent(buttonTex_VProp_Direct);
		buttonTex_VProp_Radial = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_Radial.png", typeof(Texture));
		button_VProp_Radial = new GUIContent(buttonTex_VProp_Radial);
		buttonTex_VProp_AlternatingInverse = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_AlternatingInverse.png", typeof(Texture));
		button_VProp_AlternatingInverse = new GUIContent(buttonTex_VProp_AlternatingInverse);
		buttonTex_VProp_MirroredXDirect = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_MirroredXDirect.png", typeof(Texture));
		button_VProp_MirroredXDirect = new GUIContent(buttonTex_VProp_MirroredXDirect);
		buttonTex_VProp_MirroredXScaled = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_MirroredXScaled.png", typeof(Texture));
		button_VProp_MirroredXScaled = new GUIContent(buttonTex_VProp_MirroredXScaled);
		buttonTex_VProp_MirroredYDirect = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_MirroredYDirect.png", typeof(Texture));
		button_VProp_MirroredYDirect = new GUIContent(buttonTex_VProp_MirroredYDirect);
		buttonTex_VProp_MirroredYScaled = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/VPropButton_MirroredYScaled.png", typeof(Texture));
		button_VProp_MirroredYScaled = new GUIContent(buttonTex_VProp_MirroredYScaled);

		buttonTex_Delete = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/Delete.png", typeof(Texture));
		button_Delete = new GUIContent(buttonTex_Delete);
		buttonTex_DeleteRecalc = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Art/SuperShapeEditorUI/DeleteRecalc.png", typeof(Texture));
		button_DeleteRecalc = new GUIContent(buttonTex_DeleteRecalc);
	}

	private static Color BlendColors(Color dst, Color src)
	{
		Color c = src * src.a + dst * (1 - src.a);
		c.a = src.a + dst.a;
		return c;
	}

	private static void BlendTextures(Texture2D dstTex, Texture2D srcTex)
	{
		for (int x = 0; x < dstTex.width; x++)
		{
			for (int y = 0; y < dstTex.height; y++)
			{
				Color src = srcTex.GetPixel(x, y);
				if (src.a == 0)
				{
					// source pixel is fully transparent, so nothing to do
					continue;
				}
				if (src.a == 1)
				{
					// src pixel is fully opaque, so use the child's
					dstTex.SetPixel(x, y, src);
					continue;
				}
				Color dst = dstTex.GetPixel(x, y);
				Color result;
				if (dst.a == 0)
				{
					// parent pixel is fully transparent, so use the child's
					result = src;
				}
				else
				{
					// both pixels have alpha, so blend them in the same way
					// the shader would
					result = BlendColors(dst, src);
				}
				dstTex.SetPixel(x, y, result);
			}
		}
	}

	private void StartEditingVerts(SuperShape shape)
	{
		shape.storedVerts = null;
		isEditingVerts = true;
		isEditingWiggleZones = false;
		isEditingTailWiggleZones = false;
		preEditTool = Tools.current;
		Tools.current = Tool.None;
		SceneView.RepaintAll();
	}

	private void StartEditingWiggleZones()
	{
		isEditingVerts = false;
		isEditingWiggleZones = true;
		isEditingTailWiggleZones = false;
		preEditTool = Tools.current;
		Tools.current = Tool.None;
		SceneView.RepaintAll();
	}

	private void StartEditingTailWiggleZones()
	{
		isEditingVerts = false;
		isEditingWiggleZones = false;
		isEditingTailWiggleZones = true;
		preEditTool = Tools.current;
		Tools.current = Tool.None;
		SceneView.RepaintAll();
	}

	private void StopEditingShape(bool restoreTool)
	{
		isEditingVerts = false;
		if (restoreTool) Tools.current = preEditTool;
		UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
	}

	private void StopEditingWiggleZones(bool restoreTool)
	{
		isEditingWiggleZones = false;
		if (restoreTool) Tools.current = preEditTool;
		UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
	}

	private void StopEditingTailWiggleZones(bool restoreTool)
	{
		isEditingTailWiggleZones = false;
		if (restoreTool) Tools.current = preEditTool;
		UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
	}

	private void ZeroTailWiggleZone(SuperShape shape)
	{
		shape.tailTipWiggle = Vector2.zero;
		UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
	}

	int GetClosestLineToPoint(Vector3 pos, Vector3[] verts)
	{
		Vector2 p = HandleUtility.WorldToGUIPoint(pos);
		int closest = -1;
		float distance = -1;
		for (int i = 0; i < verts.Length; i++)
		{
			Vector2 v1 = HandleUtility.WorldToGUIPoint(verts[i]);
			Vector2 v2 = HandleUtility.WorldToGUIPoint(verts[i == verts.Length - 1 ? 0 : i + 1]);
			float testDistance = HandleUtility.DistancePointToLineSegment(p, v1, v2);
			if (closest == -1 || testDistance < distance)
			{
				closest = i;
				distance = testDistance;
			}
		}
		return closest;
	}

	Vector3 GetMouseWorldPos()
	{
		Vector2 mouseScreenPos = Event.current.mousePosition;
		return HandleUtility.GUIPointToWorldRay(mouseScreenPos).origin;
	}

	List<Vector3[]> GetConnectingLineSegmentLists(Vector3[] verts, int layerCount, int vertexCount)
	{
		List<Vector3[]> results = new List<Vector3[]>();
		for (int i = 0; i < layerCount - 1; i++)
		{
			for (int j = 0; j < vertexCount; j++)
			{
				results.Add(new Vector3[2] { verts[i * vertexCount + j], verts[(i + 1) * vertexCount + j] });
			}
		}
		return results;
	}
	List<Vector3[]> GetFillBarConnectingLineSegmentLists(Vector3[] verts)
	{
		List<Vector3[]> results = new List<Vector3[]>();
		for (int j = 0; j < 4; j++)
		{
			results.Add(new Vector3[2] { verts[j], verts[12 + j] });
		}
		return results;
	}

	Vector3[] GetLayerLineSegmentList(Vector3[] verts, int layerIndex, int vertexCount)
	{
		Vector3[] results = new Vector3[vertexCount + 1];
		if (layerIndex < 0)
		{
			//pass, invalid
		}
		else
		{
			for (int i = 0; i < vertexCount; i++)
			{
				results[i] = verts[vertexCount * layerIndex + i];
			}
			results[vertexCount] = verts[vertexCount * layerIndex];
		}
		return results;
	}

	Vector3[] GetRectCornerList(Vector2 pos, Vector2 slice)
	{

		return new Vector3[5] { new Vector3(pos.x - slice.x/2, pos.y - slice.y/2, 0),
		                        new Vector3(pos.x - slice.x/2, pos.y + slice.y/2, 0), 
		                        new Vector3(pos.x + slice.x/2, pos.y + slice.y/2, 0), 
		                        new Vector3(pos.x + slice.x/2, pos.y - slice.y/2, 0), 
		                        new Vector3(pos.x - slice.x/2, pos.y - slice.y/2, 0)};
	}

	Vector3[] GetRectCornerList(Rect r)
	{
		return new Vector3[5] { new Vector3(r.xMin, r.yMin, 0),
		                        new Vector3(r.xMin, r.yMax, 0), 
		                        new Vector3(r.xMax, r.yMax, 0), 
		                        new Vector3(r.xMax, r.yMin, 0), 
		                        new Vector3(r.xMin, r.yMin, 0)};
	}

	List<Vector3[]> GetWiggleZonesCornerLists(Rect[] zones, int layerCount, int vertexCount)
	{
		List<Vector3[]> results = new List<Vector3[]>();
		for (int i = 0; i < layerCount; i++)
		{
			for (int j = 0; j < vertexCount; j++)
			{
				results.Add(GetRectCornerList(zones[i * vertexCount + j]));
			}
		}
		return results;
	}

	void EditVerts(SuperShape shape)
	{
		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
		// get the existing verts
		Vector3[] oldVerts = shape.GetWorldVertices();
		List<Vector3> verts = new List<Vector3>(oldVerts);

		if (shape.vertexCount <= SuperShape.MIN_VERTEX_COUNT)
		{
			isDeleteMode = false;
		}

		//draw 9-slice bounds if applicable
		if (shape.sliceWidth > 0 || shape.sliceHeight > 0)
		{
			Handles.color = Color.yellow;
			Handles.DrawAAPolyLine(shape.GetWorldBounds());
		}

		//what color should handles be?
		Handles.color = Color.green;
		Color pink = new Color(1, 0, 0.75f);
		if (isDeleteMode)
		{
			Handles.color = Color.red;
		}
		else if (Event.current.shift)
		{
			//pass
		}
		else if (Event.current.control || Event.current.command)
		{
			//pass
		}

		// draw the shape
		bool isFillBar = shape.GetComponent<SuperShapeFillBar>() != null;
		for (int i = 0; i < shape.layerCount; i++)
		{
			if (isFillBar && i >= 1 && i < 3) { continue; }
			Handles.DrawAAPolyLine(3f, GetLayerLineSegmentList(oldVerts, i, shape.vertexCount));
		}
		List<Vector3[]> lines = isFillBar ? GetFillBarConnectingLineSegmentLists(oldVerts) :
		                                    GetConnectingLineSegmentLists(oldVerts, shape.layerCount, shape.vertexCount);
		foreach (Vector3[] line in lines)
		{
			Handles.DrawLine(line[0], line[1]);
		}

		// drag handle result for getting info from our handles
		CustomHandles.DragHandleResult dhResult = CustomHandles.DragHandleResult.none;
		// draw handles for each existing vert and check if they've been moved or clicked
		int changedIndex = -1;
		Vector2 delta = Vector4.zero;
		Vector3 newTargetWorldPos = Vector3.zero;
		int addDeleteFullIndex = -1;
		for (int i = shape.layerCount * shape.vertexCount - 1; i >= 0; i--)
		{
			if (isFillBar && i >= 4 && i < 12) { continue; }
			Vector3 v = verts[i];
			Vector3 newPos = CustomHandles.DragHandle(i, v, 0.04f * HandleUtility.GetHandleSize(v),
			                                          Handles.DotHandleCap, pink, out dhResult);
			if (isDeleteMode && dhResult == CustomHandles.DragHandleResult.LMBPress)
			{
				// the user clicked on the handle while in delete mode, so delete the vert
				addDeleteFullIndex = i;
			}
			else if (dhResult == CustomHandles.DragHandleResult.LMBDrag ||
			         dhResult == CustomHandles.DragHandleResult.LMBRelease)
			{
				// the handle has been dragged, so move the vert to the new position
				delta = newPos - verts[i];
				newTargetWorldPos = newPos;
				changedIndex = i;
				break;
			}
		}
		// check if the mouse is hovering over a space where we could add a new vert,
		// and draw it if so
		bool snapped = false;
		Vector3[] firstLayerVerts = new Vector3[shape.vertexCount + 1];
		verts.CopyTo(0, firstLayerVerts, 0, shape.vertexCount);
		firstLayerVerts[shape.vertexCount] = firstLayerVerts[0];
		Vector3 closestPos = HandleUtility.ClosestPointToPolyLine(firstLayerVerts);
		float distance = HandleUtility.DistanceToPolyLine(firstLayerVerts);
		bool isCloseToLine = distance < 25;
		if (changedIndex == -1 && isCloseToLine && shape.vertexCount < SuperShape.MAX_VERTEX_COUNT && !isDeleteMode)
		{
			foreach (Vector3 v in verts)
			{
				if (Vector2.Distance(HandleUtility.WorldToGUIPoint(closestPos),
						HandleUtility.WorldToGUIPoint(v)) < 15)
				{
					snapped = true;
					break;
				}
			}
			if (!snapped)
			{
				// not too close to an existing vert, so draw a new one.  don't
				// use an actual handle cause we want to intercept nearby clicks
				// and not just clicks directly on the handle.
				Rect rect = new Rect();
				float dim = 0.05f * HandleUtility.GetHandleSize(closestPos);
				rect.center = closestPos - new Vector3(dim, dim, 0);
				rect.size = new Vector2(dim * 2, dim * 2);
				Handles.color = Color.white; // remove the weird tint it does
				Handles.DrawSolidRectangleWithOutline(rect, Color.green, Color.clear);
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					// the user has clicked the new vert, so add it for real
					// figure out which line segment it's on
					delta = closestPos;
					addDeleteFullIndex = GetClosestLineToPoint(closestPos, firstLayerVerts);
				}
			}
		}
		// something has been changed, so apply the new verts back to the shape
		if (changedIndex > -1)
		{
			Undo.RecordObject(shape, "Edit SuperShape Vertices");
			shape.editorPresetReset = true;
			if (dhResult == CustomHandles.DragHandleResult.LMBRelease)
			{
				shape.storedVerts = null;
			}
			else
			{
				shape.SetVertsFromTranslationToWorldPos(changedIndex, newTargetWorldPos, lPropMode, vPropMode);
			}
			EditorUtility.SetDirty(target);
		}
		else if (addDeleteFullIndex > -1)
		{
			if (isDeleteMode)
			{
				Undo.RecordObject(shape, "Delete SuperShape Vertices");
				Undo.RecordObject(shape.shapeMesh.GetObject(), "Delete SuperShape Vertices");
				if (shape.shapeMesh.GetProxyMaskMesh() != null)
					Undo.RecordObject(shape.shapeMesh.GetProxyMaskMesh().GetGameObject(), "Delete SuperShape Vertices");
				shape.editorPresetReset = true;
				shape.RemoveVertex(addDeleteFullIndex, isDeleteRecalc);
				if (shape.vertexCount <= SuperShape.MIN_VERTEX_COUNT)
				{
					//isDeleteMode = false;
				}
			}
			else
			{
				Undo.RecordObject(shape, "Add SuperShape Vertices");
				Undo.RecordObject(shape.shapeMesh.GetObject(), "Add SuperShape Vertices");
				if (shape.shapeMesh.GetProxyMaskMesh() != null)
					Undo.RecordObject(shape.shapeMesh.GetProxyMaskMesh().GetGameObject(), "Add SuperShape Vertices");
				shape.editorPresetReset = true;
				shape.AddVertex(delta, addDeleteFullIndex);
			}
			EditorUtility.SetDirty(target);
		}
		else
		{
			HandleUtility.Repaint(); // to draw the new vert placeholder handle
			//if (Event.current.type == EventType.MouseDown)
				//StopEditingShape(true);   TODO - check on this
		}
	}

	void EditWiggleZones(SuperShape shape)
	{
		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
		// get the verts and existing wiggleZones
		Vector3[] verts = shape.GetWorldVertices();
		Rect[] oldWiggleZones = shape.GetWorldWiggleZones();
		List<Rect> wiggleZones = new List<Rect>(oldWiggleZones);

		//what color should handles be?
		Color cyan = new Color(0, 0.8f, 0.75f);
		Color pink = new Color(1, 0, 0.75f);
		if (Event.current.shift)
		{
			//pass
		}
		else if (Event.current.control)
		{
			//pass
		}
		else
		{
			Handles.color = cyan;
		}

		// draw the shape
		bool isFillBar = shape.GetComponent<SuperShapeFillBar>() != null;
		for (int i = 0; i < shape.layerCount; i++)
		{
			if (isFillBar && i >= 1 && i < 3) { continue; }
			Handles.DrawAAPolyLine(3f, GetLayerLineSegmentList(verts, i, shape.vertexCount));
		}
		List<Vector3[]> lines = isFillBar ? GetFillBarConnectingLineSegmentLists(verts) :
		                                    GetConnectingLineSegmentLists(verts, shape.layerCount, shape.vertexCount);
		foreach (Vector3[] line in lines)
		{
			Handles.DrawLine(line[0], line[1]);
		}
		//draw verts
		Vector3 vd1 = new Vector3(0.0f, 0.14f, 0.0f);
		Vector3 vd2 = new Vector3(0.1f, -0.075f, 0.0f);
		Vector3 vd3 = new Vector3(-0.1f, -0.075f, 0.0f);
		foreach (Vector3 vert in verts)
		{
			Vector3[] corners = new Vector3[4] { vert + vd1, vert + vd2, vert + vd3, vert + vd1 };
			Handles.DrawAAPolyLine(corners);
		}
		//draw 9-slice bounds if applicable
		if (shape.slice.x > 0 || shape.slice.y > 0)
		{
			Handles.color = Color.green;
			Handles.DrawAAPolyLine(GetRectCornerList(shape.position, shape.slice));
		}

		Handles.color = Color.yellow;

		//draw wiggleZone Rectsforeach (Vector3[] line in lines)
		List<Vector3[]> zones = GetWiggleZonesCornerLists(oldWiggleZones, shape.layerCount, shape.vertexCount);
		foreach (Vector3[] zone in zones)
		{
			//Debug.Log(zone[0]);
			Handles.DrawAAPolyLine(zone);
		}

		// drag handle result for getting info from our handles
		CustomHandles.DragHandleResult dhResult;
		// draw handles for each existing vert and check if they've been moved or clicked
		int changedIndex = -1;
		Vector2 delta = Vector2.zero;
		for (int i = shape.layerCount * shape.vertexCount - 1; i >= 0; i--)
		{
			Vector3 oldPos = wiggleZones[i].position;
			Vector3 newPos = CustomHandles.DragHandle(i, oldPos, 0.03f * HandleUtility.GetHandleSize(oldPos),
			                                          Handles.DotHandleCap, cyan, out dhResult);
			if (newPos != oldPos)
			{
				// the handle has been dragged, so move the zone corner to the new position
				delta = (Vector2)newPos - wiggleZones[i].position;
				//Rect newRect = new Rect(newPos, wiggleZones[i].size - 2 * delta);
				//wiggleZones[i] = newRect;
				changedIndex = i;
				break;
			}
		}
		// something has been changed, so apply the new verts back to the shape
		if (changedIndex > -1)
		{
			int vertexIndex = changedIndex % shape.vertexCount;
			int layerIndex = changedIndex / shape.vertexCount;
			LayerPropagationMode layerMode = lPropMode;
			VertexPropagationMode vertexMode = vPropMode;
			int j2 = vertexIndex == shape.vertexCount - 1 ? 0 : vertexIndex;

			Vector2 basePoint = shape.baseVerts[changedIndex];
			float baseAngle = Mathf.Atan2(basePoint.y, basePoint.x);

			int minLayer = 0;
			if (layerMode == LayerPropagationMode.NONE || layerMode == LayerPropagationMode.NextOnlyInverse ||
			    layerMode == LayerPropagationMode.OutwardDirect || layerMode == LayerPropagationMode.OutwardRadial ||
			    layerMode == LayerPropagationMode.OutwardPivot) { minLayer = layerIndex; }
			int maxLayer = shape.layerCount - 1;
			if (layerMode == LayerPropagationMode.NONE ||
			    layerMode == LayerPropagationMode.InwardDirect || layerMode == LayerPropagationMode.InwardRadial ||
			    layerMode == LayerPropagationMode.InwardPivot) { maxLayer = layerIndex; }
			if (layerMode == LayerPropagationMode.NextOnlyInverse && layerIndex + 1 < shape.layerCount) { maxLayer = layerIndex + 1; }
			bool isLayerRadial = (layerMode == LayerPropagationMode.OutwardRadial || layerMode == LayerPropagationMode.InwardRadial ||
			                      layerMode == LayerPropagationMode.AllRadial ||
			                      layerMode == LayerPropagationMode.OutwardPivot || layerMode == LayerPropagationMode.InwardPivot);
			bool isLayersInverting = layerMode == LayerPropagationMode.AlternatingInverse || layerMode == LayerPropagationMode.NextOnlyInverse;

			int minVertex = 0;
			int maxVertex = shape.vertexCount - 1;
			int specialVertex = -1;
			if (vertexMode == VertexPropagationMode.NONE || vertexMode == VertexPropagationMode.NONE_RecalcNeighbors) { minVertex = vertexIndex; maxVertex = vertexIndex; }
			if (vertexMode == VertexPropagationMode.NextOnly || vertexMode == VertexPropagationMode.NextOnly_RecalcNeighbors) { minVertex = vertexIndex; maxVertex = vertexIndex; specialVertex = j2; }
			bool isVertexRadial = vertexMode == VertexPropagationMode.Radial || vertexMode == VertexPropagationMode.AlternatingInverse;
			bool isVertexInverting = vertexMode == VertexPropagationMode.AlternatingInverse;
			bool isVertexMirrorScaled = vertexMode == VertexPropagationMode.MirroredXScaled || vertexMode == VertexPropagationMode.MirroredYScaled;


			for (int i = 0; i < shape.layerCount; i++)
			{
				bool isLayerInverse = isLayersInverting && (i - layerIndex) % 2 == 1; //odd layer relative to base layerIndex
				for (int j = 0; j < shape.vertexCount; j++)
				{
					if (i < minLayer || i > maxLayer || (j != specialVertex && (j < minVertex || j > maxVertex)))
					{
						continue;
					}
					int k = i * shape.vertexCount + j;
					Vector2 p = shape.baseVerts[k];

					Vector2 myDelta = delta;
					if (isVertexRadial)
					{
						float angle = Mathf.Atan2(p.y, p.x);
						float sin = Mathf.Sin(angle - baseAngle);
						float cos = Mathf.Cos(angle - baseAngle);
						myDelta = new Vector2(cos * delta.x - sin * delta.y, cos * delta.y + sin * delta.x);
					}

					if (isLayerInverse) { myDelta *= -1; }
					if (isVertexInverting && (j - vertexIndex) % 2 == 1) { myDelta *= -1; }
					if (vertexMode == VertexPropagationMode.MirroredXDirect || vertexMode == VertexPropagationMode.MirroredXScaled)
					{
						if (basePoint.y * p.y == 0) { myDelta.y = 0; }
						else if (basePoint.y * p.y < 0) { myDelta.y *= -1; }
					}
					if (vertexMode == VertexPropagationMode.MirroredYDirect || vertexMode == VertexPropagationMode.MirroredYScaled)
					{
						if (basePoint.x * p.x == 0) { myDelta.x = 0; }
						else if (basePoint.x  * p.x < 0) { myDelta.x *= -1; }
					}

					wiggleZones[k] = new Rect(wiggleZones[k].position + myDelta, wiggleZones[k].size - 2 * myDelta);
				}
			}

			Undo.RecordObject(shape, "Edit SuperShape Wiggle Zones");
			shape.SetWorldWiggleZones(wiggleZones.ToArray());

			EditorUtility.SetDirty(target);
		}
		else
		{
			HandleUtility.Repaint(); // to draw the new vert placeholder handle
			                         //if (Event.current.type == EventType.MouseDown)
			                         //StopEditingShape(true);   TODO - check on this
		}
	}

	void EditTailWiggleZones(SuperShape shape)
	{
		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
		// get the verts and existing wiggleZones
		Vector3[] verts = shape.GetWorldVertices();
		Rect oldWiggleZone = shape.GetWorldTailWiggleZone();

		//what color should handles be?
		Color cyan = new Color(0, 0.8f, 0.75f);
		Color pink = new Color(1, 0, 0.75f);
		if (Event.current.shift)
		{
			//pass
		}
		else if (Event.current.control)
		{
			//pass
		}
		else
		{
			Handles.color = cyan;
		}

		// draw the shape
		bool isFillBar = shape.GetComponent<SuperShapeFillBar>() != null;
		for (int i = 0; i < shape.layerCount; i++)
		{
			if (isFillBar && i >= 1 && i < 3) { continue; }
			Handles.DrawAAPolyLine(3f, GetLayerLineSegmentList(verts, i, shape.vertexCount));
		}
		List<Vector3[]> lines = isFillBar ? GetFillBarConnectingLineSegmentLists(verts) :
		                                    GetConnectingLineSegmentLists(verts, shape.layerCount, shape.vertexCount);
		foreach (Vector3[] line in lines)
		{
			Handles.DrawLine(line[0], line[1]);
		}
		//draw verts
		Vector3 vd1 = new Vector3(0.0f, 0.14f, 0.0f);
		Vector3 vd2 = new Vector3(0.1f, -0.075f, 0.0f);
		Vector3 vd3 = new Vector3(-0.1f, -0.075f, 0.0f);
		foreach (Vector3 vert in verts)
		{
			Vector3[] corners = new Vector3[4] { vert + vd1, vert + vd2, vert + vd3, vert + vd1 };
			Handles.DrawAAPolyLine(corners);
		}
		//draw 9-slice bounds if applicable
		if (shape.width > 0 || shape.height > 0)
		{
			Handles.color = Color.green;
			Handles.DrawAAPolyLine(GetRectCornerList(shape.rectTransform.rect));
		}

		Handles.color = Color.yellow;

		//draw wiggleZone Rectsforeach (Vector3[] line in lines)
		Vector3[] zone = GetRectCornerList(oldWiggleZone);
		Handles.DrawAAPolyLine(zone);

		// drag handle result for getting info from our handles
		CustomHandles.DragHandleResult dhResult;
		// draw handles for each existing vert and check if they've been moved or clicked
		int changedIndex = -1;
		Vector2 delta = Vector4.zero;

		Vector3 oldPos = oldWiggleZone.position;
		Vector3 newPos = CustomHandles.DragHandle(0, oldPos, 0.03f * HandleUtility.GetHandleSize(oldPos),
		                                          Handles.DotHandleCap, cyan, out dhResult);
		if (newPos != oldPos)
		{
			delta = newPos - oldPos;
			changedIndex = 1;
		}

		if (changedIndex > -1)
		{
			Undo.RecordObject(shape, "Edit SuperShape Wiggle Zones");
			shape.SetWorldTailWiggleZone(new Rect(oldWiggleZone.position + delta, oldWiggleZone.size - 2 * delta));

			EditorUtility.SetDirty(target);
		}
		else
		{
			HandleUtility.Repaint(); // to draw the new vert placeholder handle
			                         //if (Event.current.type == EventType.MouseDown)
			                         //StopEditingShape(true);   TODO - check on this
		}
	}
	void OnSceneGUI()
	{
		SuperShape shape = (SuperShape)target;
		//shape.SetMeshDirty();
		//shape.SetMaterialDirty();
		if (Tools.current != Tool.None)
		{
			//Debug.Log("here, exiting");
			if (isEditingVerts)           StopEditingShape(false);
			if (isEditingWiggleZones)     StopEditingWiggleZones(false);
			if (isEditingTailWiggleZones) StopEditingTailWiggleZones(false);
			return;
		}
		// draw some borders so the user knows where the shape should live

		if (isEditingVerts)
		{
			SceneShapeVertsPresetButtons(shape);
			EditVerts(shape);
			SceneButtons();
			SceneDeleteVertexButtons(shape);
			SceneVertEditModeSelectedTextDisplay(shape);
		}
		else if (isEditingWiggleZones)
		{
			EditWiggleZones(shape);
			SceneButtons();
		}
		else if (isEditingTailWiggleZones)
		{
			EditTailWiggleZones(shape);
		}
		else
		{
			//Debug.Log("here, default");
			return;
		}

		// this prevents the user selecting another object when they are
		// adding poly/path nodes
		if (Event.current.type == EventType.Layout)
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
	}

	void SceneButtons()
	{
		Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(10, 10, BUTTON_SIZE * 2 + 4 * 3, 450 + 4 * 8));
				var rect = EditorGUILayout.BeginVertical();
					GUI.color = Color.yellow;
					GUI.Box(rect, GUIContent.none);

					GUI.color = Color.white;

					EditorGUI.BeginDisabledGroup(isDeleteMode);
						GUILayout.BeginHorizontal();
							GUILayout.BeginVertical(GUILayout.MinWidth(6));
							GUILayout.EndVertical();
							GUILayout.BeginVertical(GUILayout.Width(BUTTON_SIZE));
								GUILayout.Label("Layer");
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.NONE, button_LProp_NONE, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
									{ lPropMode = LayerPropagationMode.NONE; }
								GUILayout.Space(8);
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.AllDirect, button_LProp_AllDirect, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ lPropMode = LayerPropagationMode.AllDirect; }
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.AllRadial, button_LProp_AllRadial, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ lPropMode = LayerPropagationMode.AllRadial; }
								GUILayout.Space(8);
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.OutwardDirect, button_LProp_OutwardDirect, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
									{ lPropMode = LayerPropagationMode.OutwardDirect; }
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.OutwardRadial, button_LProp_OutwardRadial, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
									{ lPropMode = LayerPropagationMode.OutwardRadial; }
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.OutwardPivot, button_LProp_OutwardPivot, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ lPropMode = LayerPropagationMode.OutwardPivot; }
								GUILayout.Space(8);
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.InwardDirect, button_LProp_InwardDirect, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ lPropMode = LayerPropagationMode.InwardDirect; }
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.InwardRadial, button_LProp_InwardRadial, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ lPropMode = LayerPropagationMode.InwardRadial; }
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.InwardPivot, button_LProp_InwardPivot, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ lPropMode = LayerPropagationMode.InwardPivot; }
								GUILayout.Space(8);
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.AlternatingInverse, button_LProp_AlternatingInverse, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ lPropMode = LayerPropagationMode.AlternatingInverse; }
								if (GUILayout.Toggle(lPropMode == LayerPropagationMode.NextOnlyInverse, button_LProp_NextOnlyInverse, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ lPropMode = LayerPropagationMode.NextOnlyInverse; }
							GUILayout.EndVertical();
							GUILayout.BeginVertical(GUILayout.Width(BUTTON_SIZE));
								GUILayout.Label(" Vert");
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.NONE, button_VProp_NONE, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.NONE; }
								GUILayout.Space(8);
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.NextOnly, button_VProp_NextOnly, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.NextOnly; }
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.Direct, buttonTex_VProp_Direct, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.Direct; }
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.Radial, buttonTex_VProp_Radial, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.Radial; }
								GUILayout.Space(8);
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.NONE_RecalcNeighbors, button_VProp_NONE_RecalcNeighbors, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.NONE_RecalcNeighbors; }
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.NextOnly_RecalcNeighbors, button_VProp_NextOnly_RecalcNeighbors, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.NextOnly_RecalcNeighbors; }
								GUILayout.Space(8);
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.MirroredXDirect, buttonTex_VProp_MirroredXDirect, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.MirroredXDirect; }
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.MirroredXScaled, buttonTex_VProp_MirroredXScaled, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.MirroredXScaled; }
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.MirroredYDirect, buttonTex_VProp_MirroredYDirect, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.MirroredYDirect; }
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.MirroredYScaled, buttonTex_VProp_MirroredYScaled, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.MirroredYScaled; }
								GUILayout.Space(8);
								if (GUILayout.Toggle(vPropMode == VertexPropagationMode.AlternatingInverse, buttonTex_VProp_AlternatingInverse, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
								{ vPropMode = VertexPropagationMode.AlternatingInverse; }
							GUILayout.EndVertical();
							GUILayout.BeginVertical(GUILayout.MinWidth(4));
							GUILayout.EndVertical();
						GUILayout.EndHorizontal();
					EditorGUI.EndDisabledGroup();
				GUILayout.EndVertical();
			GUILayout.EndArea();
		Handles.EndGUI();
	}

	void SceneDeleteVertexButtons(SuperShape shape)
	{
		Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(BUTTON_SIZE * 2 + 12 * 3, 10, BUTTON_SIZE + 4 * 3, 100));
				//GUILayout.BeginArea(new Rect(Screen.width - BUTTON_SIZE - 22, 10, BUTTON_SIZE + 12, 100));
				var rect = EditorGUILayout.BeginVertical();
					GUI.color = Color.red;
					GUI.Box(rect, GUIContent.none);

					GUI.color = Color.white;
					GUILayout.BeginVertical(GUILayout.MinWidth(6));
					GUILayout.EndVertical();
					GUILayout.BeginVertical(GUILayout.Width(BUTTON_SIZE));
						GUILayout.Label("Delete");
						EditorGUI.BeginDisabledGroup(shape.vertexCount <= SuperShape.MIN_VERTEX_COUNT);
							if (GUILayout.Toggle(isDeleteMode && !isDeleteRecalc, button_Delete, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
							{ isDeleteMode = true; isDeleteRecalc = false; }
							else if (!isDeleteRecalc)
							{ isDeleteMode = false; }
							if (GUILayout.Toggle(isDeleteMode && isDeleteRecalc, button_DeleteRecalc, "Button", GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
							{ isDeleteMode = true; isDeleteRecalc = true; }
							else if (isDeleteRecalc)
							{ isDeleteMode = false; }
						EditorGUI.EndDisabledGroup();
					GUILayout.EndVertical();
					GUILayout.BeginVertical(GUILayout.MinWidth(4));
					GUILayout.EndVertical();
				GUILayout.EndVertical();
			GUILayout.EndArea();
		Handles.EndGUI();
	}

	void SceneShapeVertsPresetButtons(SuperShape shape)
	{
		Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(166, 10, 166, 28));
				var rect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(40));
					GUILayout.Space(4);
					GUI.color = Color.cyan;
					GUI.Box(rect, GUIContent.none);
					GUI.color = Color.white;
					rect.yMin += 12;
					GUILayout.Label("Preset:");
					rect.yMin -= 6;
					rect.xMin += 50;
					rect.width -= 4;
					EditorGUI.PropertyField(rect, presetProp, GUIContent.none);
					if (presetProp.intValue != 0 && shape.editorSelectedPreset != (SuperShapePreset)presetProp.intValue)
					{
						Undo.RecordObject(shape, "Reset SuperShape to Preset (" + (SuperShapePreset)presetProp.intValue + ")");
						shape.editorSelectedPreset = (SuperShapePreset)presetProp.intValue;
						shape.SetupPreset(shape.editorSelectedPreset);
					}
				EditorGUILayout.EndVertical();
			GUILayout.EndArea();
		Handles.EndGUI();
	}

	void SceneVertEditModeSelectedTextDisplay(SuperShape shape)
	{
		Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(166, 46, 166, 40));
				var rect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(40));
					GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
					GUI.Box(rect, GUIContent.none);
					GUI.color = Color.white;
					GUILayout.Label(lPropMode ==  LayerPropagationMode.NONE ? "Basic" : lPropMode.ToString());
					GUILayout.Label(vPropMode == VertexPropagationMode.NONE ? "Basic" : vPropMode.ToString());
				EditorGUILayout.EndVertical();
			GUILayout.EndArea();

			if (GUIUtility.hotControl > 0 && CustomHandles.lastClickedManualID >= 0)
			{
				int layerIndex = CustomHandles.lastClickedManualID / shape.vertexCount;
				int vertIndex = CustomHandles.lastClickedManualID % shape.vertexCount;
				GUILayout.BeginArea(new Rect(166, 90, 166, 24));
					rect = EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24));
						GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
						GUI.Box(rect, GUIContent.none);
						GUI.color = Color.white;
						GUILayout.Label("Layer " + layerIndex);
						GUILayout.Label("Vertex " + vertIndex);
					EditorGUILayout.EndHorizontal();
				GUILayout.EndArea();
			}
		Handles.EndGUI();
	}

	public override void OnInspectorGUI()
	{
		float indent = 20f;

		serializedObject.Update();
		SuperShape shape = (SuperShape)serializedObject.targetObject;

		EditorGUILayout.Separator();
		EditorGUI.BeginDisabledGroup(Selection.objects.Length != 1);
			if (GUILayout.Toggle(isEditingVerts, "Edit Shape", "Button"))
			{
				//Debug.Log(1 + " " + isEditingVerts);
				if (!isEditingVerts) StartEditingVerts(shape);
			}
			else
			{
				//Debug.Log(2 + " " + isEditingVerts);
				if (isEditingVerts) StopEditingShape(true);
			}
			if (GUILayout.Toggle(isEditingWiggleZones, "Edit Wiggle Zones", "Button"))
			{
				if (!isEditingWiggleZones) StartEditingWiggleZones();
			}
			else
			{
				if (isEditingWiggleZones) StopEditingWiggleZones(true);
			}
		EditorGUI.EndDisabledGroup();

		EditorGUILayout.Separator();
		EditorGUILayout.BeginHorizontal();
			Rect rect = EditorGUILayout.GetControlRect();
			float fullWidth = rect.width;
			rect.width = (fullWidth - 2) / 2;
			EditorGUI.BeginDisabledGroup(shape.layerCount >= SuperShape.MAX_LAYER_COUNT);
				if (GUI.Button(rect, "Add Layer"))
				{
					shape.SetLayerCount(shape.layerCount + 1);
				}
			EditorGUI.EndDisabledGroup();
			rect.x += rect.width + 2;
			EditorGUI.BeginDisabledGroup(shape.layerCount <= 1);
				if (GUI.Button(rect, "Remove Layer"))
				{
					shape.SetLayerCount(shape.layerCount - 1);
				}
			EditorGUI.EndDisabledGroup();
		EditorGUILayout.EndHorizontal();

		EditorGUI.BeginChangeCheck();
			if (shape.vertexCount == 4)
			{
				EditorGUILayout.BeginHorizontal();
					rect = EditorGUILayout.GetControlRect();
					EditorGUIUtility.labelWidth = 80;
					EditorGUI.PropertyField(rect, quadSkewProp, new GUIContent("Quad Skew"));
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Separator();

			isShowingResizeOptions = EditorGUILayout.Foldout(isShowingResizeOptions, new GUIContent("9 Slice, Size, and Pivot"), true);
			if (isShowingResizeOptions)
			{
				EditorGUILayout.BeginHorizontal();
					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUIUtility.labelWidth = 75;
					EditorGUI.PropertyField(rect, sliceProp, new GUIContent("9 Slice Size"));
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
					rect = EditorGUILayout.GetControlRect();
					fullWidth = rect.width - indent;
					rect.x += indent;
					rect.width = (fullWidth - 20) / 3;
					if (GUI.Button(rect, "De-slice", EditorStyles.miniButton))
					{
						shape.ResizeSliceRect(Vector2.zero, true);
					}
					rect.x += rect.width + 18;
					if (GUI.Button(rect, "Re-slice", EditorStyles.miniButton))
					{
						shape.ResizeSliceRect(shape.editorResizeRectOffset);
					}
					rect.x += rect.width + 2;
					EditorGUIUtility.labelWidth = 40;
					rect.width -= 30f;
					rect.width *= 1.5f;
					EditorGUI.PropertyField(rect, editorResizeRectOffsetProp, new GUIContent("Border"));
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUIUtility.labelWidth = 200;
					EditorGUI.PropertyField(rect, isSliceLockProp, new GUIContent("Lock RectTransform Size To Slice"));
				EditorGUILayout.EndHorizontal();

				EditorGUI.BeginDisabledGroup(shape.isRectTransformedLockedToSlice);
					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 4) / 3;
						if (GUI.Button(rect, "Size to Slice", EditorStyles.miniButton) || shape.isRectTransformedLockedToSlice)
						{
							shape.ResizeRectToSliceRect();
						}
						rect.x += rect.width + 2;
						if (GUI.Button(rect, "Size to Inner", EditorStyles.miniButton))
						{
							shape.ResizeRectToInnerLayer();
						}
						rect.x += rect.width + 2;
						if (GUI.Button(rect, "Size to Outer", EditorStyles.miniButton))
						{
							shape.ResizeRectToOuterLayer();
						}
					EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();

				EditorGUILayout.BeginHorizontal();
					rect = EditorGUILayout.GetControlRect();
					fullWidth = rect.width - indent;
					rect.x += indent;
					rect.width = (fullWidth - 2) / 2;
					EditorGUIUtility.labelWidth = 150;
					if (GUI.Button(rect, "Assign Pivot To Vertex", EditorStyles.miniButton))
					{
						shape.AssignVertexPivot(shape.editorSetPivotVertex, shape.editorSetPivotLayer);
					}
					rect.x += rect.width + 2;
					rect.width = (fullWidth - 6) / 4;
					EditorGUIUtility.labelWidth = 48;
					EditorGUI.PropertyField(rect, editorPivotLayerIndexProp, new GUIContent("Layer #"));
					rect.x += rect.width + 2;
					EditorGUIUtility.labelWidth = 54;
					EditorGUI.PropertyField(rect, editorPivotVertexIndexProp, new GUIContent("Vertex #"));
				EditorGUILayout.EndHorizontal();
			}
			isShowingTailOptions = EditorGUILayout.Foldout(isShowingTailOptions, new GUIContent("Tail Options"), true);
			if (isShowingTailOptions)
			{
				EditorGUILayout.BeginHorizontal();
					rect = EditorGUILayout.GetControlRect();
					fullWidth = rect.width - indent;
					rect.x += indent;
					rect.width = (fullWidth - 4) / 2;
					EditorGUIUtility.labelWidth = 35f;
					EditorGUI.PropertyField(rect, tailModeProp, new GUIContent("Mode"));
					if (shape.tailMode != TailMode.NONE)
					{
						rect.x += rect.width + 2;
						rect.width /= 2;
						EditorGUIUtility.labelWidth = 60f;
						EditorGUI.PropertyField(rect, isTailArrowProp, new GUIContent("Is Arrow"));
						if (shape.tailMode != TailMode.Basic)
						{
							rect.x += rect.width + 2;
							EditorGUIUtility.labelWidth = 70f;
							EditorGUI.PropertyField(rect, isTailFlippedProp, new GUIContent("Is Flipped"));
						}
					}
				EditorGUILayout.EndHorizontal();

				if (shape.tailMode != TailMode.NONE)
				{
					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = fullWidth;
						EditorGUIUtility.labelWidth = 105f;
						EditorGUI.PropertyField(rect, tailTransformProp, new GUIContent("Tail Tip Transform"));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						rect.x += indent;
						rect.width = (fullWidth - 2) / 2;
						EditorGUIUtility.labelWidth = 105;
						EditorGUI.PropertyField(rect, tailLayerThicknessProp, new GUIContent("Layer Thickness"));
						if (tailLayerThicknessProp.floatValue < 1f) { tailLayerThicknessProp.floatValue = 1f; }
						if (shape.tailMode != TailMode.Basic)
						{
							rect.x += rect.width + 2;
							EditorGUIUtility.labelWidth = 135f;
							EditorGUI.PropertyField(rect, tailBoltSegmentDistanceProp, new GUIContent("Bolt Segment Distance"));
							if (tailBoltSegmentDistanceProp.floatValue < 5f) { tailBoltSegmentDistanceProp.floatValue = 5f; }
						}
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 4) / 2;
						EditorGUIUtility.labelWidth = 105;
						EditorGUI.PropertyField(rect, tailBaseWidthProp, new GUIContent("Base Width"));
						rect.x += rect.width + 2;
						rect.width /= 2;
						EditorGUIUtility.labelWidth = 72;
						EditorGUI.PropertyField(rect, tailTipFixedProp, new GUIContent("Freeze Tip"));
						rect.x += rect.width + 2;
						EditorGUIUtility.labelWidth = 78;
						EditorGUI.PropertyField(rect, tailBaseFixedProp, new GUIContent("Freeze Base"));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 2) / 2;
						EditorGUI.BeginDisabledGroup(shape.tailMaxLayer >= SuperShape.MAX_LAYER_COUNT);
							if (GUI.Button(rect, "Add Tail Layer", EditorStyles.miniButton))
							{
								shape.tailMaxLayer++;
							}
							EditorGUI.EndDisabledGroup();
							rect.x += rect.width + 2;
							EditorGUI.BeginDisabledGroup(shape.tailMaxLayer <= 0);
							if (GUI.Button(rect, "Remove Tail Layer", EditorStyles.miniButton))
							{
								if (shape.tailMaxLayer > shape.layerCount) { shape.tailMaxLayer = shape.layerCount; }
								shape.tailMaxLayer--;
							}
						EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 2) / 2;
						EditorGUIUtility.labelWidth = 90;
						EditorGUI.PropertyField(rect, tailMaxLengthProp, new GUIContent("Max Length"));
						rect.x += rect.width + 2;
						EditorGUI.PropertyField(rect, tailExtraLengthProp, new GUIContent("Extra Length"));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 2) / 2;
						EditorGUIUtility.labelWidth = 90;
						EditorGUI.PropertyField(rect, tailCornerAvoidanceProp, new GUIContent("Corner Size"));
						rect.x += rect.width + 2;
						EditorGUI.PropertyField(rect, tailBannedSideIndexProp, new GUIContent("Banned Side #"));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						rect.width -= indent;
						rect.x += indent;
						EditorGUIUtility.labelWidth = 150;
						EditorGUI.Slider(rect, tailProgressPercentageProp, 0, 1f);
					EditorGUILayout.EndHorizontal();

					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width = (fullWidth - 2) / 2;
					if (GUI.Toggle(rect, isEditingTailWiggleZones, "Edit Tail Wiggle Zone", EditorStyles.miniButton))
					{
						if (!isEditingTailWiggleZones) StartEditingTailWiggleZones();
					}
					else
					{
						if (isEditingTailWiggleZones) StopEditingTailWiggleZones(true);
					}
					rect.x += rect.width + 2;
					if (GUI.Button(rect, "Zero Tail Wiggle", EditorStyles.miniButton))
					{
						ZeroTailWiggleZone(shape);
					}
				}

				EditorGUILayout.BeginHorizontal();
					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					EditorGUI.PropertyField(rect, bridgeTargetProp, new GUIContent("Bridge Target"));
				EditorGUILayout.EndHorizontal();
				if (shape.bridgeTarget != null)
				{
					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						rect.x += indent;
						EditorGUI.PropertyField(rect, bridgeTargetRectPosDriverProp, new GUIContent("Bridge Position Driver"));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 2) / 2;
						EditorGUI.PropertyField(rect, bridgeMySideIndexProp, new GUIContent("My Side Index"));
						rect.x += rect.width + 2;
						EditorGUI.PropertyField(rect, bridgeTargetSideIndexProp, new GUIContent("Target Side Index"));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 2) / 2;
						EditorGUI.PropertyField(rect, bridgeLayerInnerWidthProp, new GUIContent("Inner Width"));
						rect.x += rect.width + 2;
						EditorGUI.PropertyField(rect, bridgeLayerOuterWidthProp, new GUIContent("Per-Layer Width"));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 6) / 4;
						EditorGUIUtility.labelWidth = 20;
						EditorGUI.PropertyField(rect, bridgeProgressAProp, new GUIContent("A"));
						rect.x += rect.width + 2;
						EditorGUI.PropertyField(rect, bridgeProgressBProp, new GUIContent("B"));
						rect.x += rect.width + 2;
						EditorGUI.PropertyField(rect, bridgeProgressCProp, new GUIContent("C"));
						rect.x += rect.width + 2;
						EditorGUI.PropertyField(rect, bridgeProgressDProp, new GUIContent("D"));
					EditorGUILayout.EndHorizontal();
				}
			}
			if (shape.GetComponent<Button>() != null || shape.GetComponent<SuperShapeButton>() != null)
			{
				isShowingGutterOptions = EditorGUILayout.Foldout(isShowingGutterOptions, new GUIContent("Gutter Options"), true);
				if (isShowingGutterOptions)
				{
					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 4) / 3;
						EditorGUIUtility.labelWidth = 80;
						EditorGUI.PropertyField(rect, isGutterProp, new GUIContent("Has Gutter"));
						if (shape.isGutter)
						{
							rect.x += rect.width + 2;
							EditorGUI.PropertyField(rect, isGutter3DProp, new GUIContent("3D Gutter"));
							EditorGUI.BeginDisabledGroup(!shape.isGutter3D);
							rect.x += rect.width + 2;
							EditorGUI.PropertyField(rect, isGutter3DSameSideProp, new GUIContent("Same Side"));
							EditorGUI.EndDisabledGroup();
						}
					EditorGUILayout.EndHorizontal();
					if (shape.isGutter)
					{
						rect = EditorGUILayout.GetControlRect();
						rect.x += indent;
						rect.width -= indent;
						EditorGUIUtility.labelWidth = 160;
						EditorGUI.PropertyField(rect, gutterLineThicknessProp, new GUIContent("Gutter Line Thickness"));
						rect = EditorGUILayout.GetControlRect();
						rect.x += indent;
						rect.width -= indent;
						EditorGUI.PropertyField(rect, gutterSideProp, new GUIContent("Gutter Side Index"));
						rect = EditorGUILayout.GetControlRect();
						rect.x += indent;
						rect.width -= indent;
						EditorGUI.PropertyField(rect, gutterTotalMovementProp, new GUIContent("Gutter Click Movement"));
						rect = EditorGUILayout.GetControlRect();
						rect.x += indent;
						rect.width -= indent;
						EditorGUI.PropertyField(rect, gutterContainerProp, new GUIContent("Gutter Container"));
					}
				}
			}
			EditorGUILayout.PropertyField(layerColorsProp, true);
		if (EditorGUI.EndChangeCheck())
		{
			shape.UpdateMesh();
		}
		EditorGUI.BeginChangeCheck();
			isShowingTextureOptions = EditorGUILayout.Foldout(isShowingTextureOptions, new GUIContent("Texture Options"), true);
			if (isShowingTextureOptions)
			{
				rect = EditorGUILayout.GetControlRect();
				rect.x += indent;
				rect.width -= indent;
				EditorGUIUtility.labelWidth = 120f;
				EditorGUI.PropertyField(rect, blendModeProp, blendModeContent);
				rect = EditorGUILayout.GetControlRect();
				fullWidth = rect.width - indent;
				rect.x += indent;
				rect.width = fullWidth / 2 - 1;
				if (GUI.Button(rect, "<<", EditorStyles.miniButton))
				{
					var blendModeIndex = blendModeProp.enumValueIndex;
					blendModeIndex--;
					if (blendModeIndex < 0)
						blendModeIndex = blendModeEnumLength - 1;
					blendModeProp.enumValueIndex = blendModeIndex;
				}
				rect.x += rect.width + 2;
				if (GUI.Button(rect, ">>", EditorStyles.miniButton))
				{
					var blendModeIndex = blendModeProp.enumValueIndex;
					blendModeIndex++;
					if (blendModeIndex >= blendModeEnumLength)
						blendModeIndex = 0;
					blendModeProp.enumValueIndex = blendModeIndex;
				}
				EditorGUIUtility.labelWidth = 70f;
				rect = EditorGUILayout.GetControlRect();
				rect.x += indent;
				rect.width -= indent;
				EditorGUI.PropertyField(rect, maskModeProp);
				rect = EditorGUILayout.GetControlRect();
				rect.x += indent;
				rect.width -= indent;
				EditorGUIUtility.labelWidth = 140f;
				EditorGUI.PropertyField(rect, isUnifiedGrabProp);
				rect = EditorGUILayout.GetControlRect();
				rect.x += indent;
				rect.width -= indent;
				EditorGUI.PropertyField(rect, textureCountProp);
				if ((int)shape.textureCount >= 1)
				{
					EditorGUILayout.BeginHorizontal();
						rect = EditorGUILayout.GetControlRect();
						fullWidth = rect.width - indent;
						rect.x += indent;
						rect.width = (fullWidth - 2) * 3 / 4;
						EditorGUIUtility.labelWidth = 56f;
						EditorGUI.PropertyField(rect, textureModeProp, new GUIContent("UV Mode"));
						EditorGUI.BeginDisabledGroup(shape.innerTextureMode == InnerTextureMode.ScreenSpace ||
						                             shape.innerTextureMode == InnerTextureMode.Fixed ||
						                             shape.innerTextureMode == InnerTextureMode.SliceOnly);
							rect.x += rect.width + 2;
							rect.width /= 3;
							EditorGUI.PropertyField(rect, textureSubModeProp, GUIContent.none);
						EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();

					if (shape.innerTextureMode == InnerTextureMode.Fixed)
					{
						rect = EditorGUILayout.GetControlRect();
						rect.x += indent;
						rect.width -= indent;
						EditorGUI.PropertyField(rect, fixedUVRangeProp);
					}

					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUIUtility.labelWidth = 90f;
					EditorGUI.PropertyField(rect, texture1Prop, new GUIContent("Fill Texture 1"));

					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUIUtility.labelWidth = 120f;
					EditorGUI.PropertyField(rect, texture1OffsetProp, new GUIContent("Fill Texture 1 Offset"));
					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUI.PropertyField(rect, texture1ScaleProp, new GUIContent("Fill Texture 1 Scale"));
					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUI.PropertyField(rect, texture1AlphaProp, new GUIContent("Fill Texture 1 Alpha"));
				}
				if ((int)shape.textureCount >= 2)
				{
					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUIUtility.labelWidth = 90f;
					EditorGUI.PropertyField(rect, texture2Prop);

					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUIUtility.labelWidth = 120f;
					EditorGUI.PropertyField(rect, texture2OffsetProp);
					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUI.PropertyField(rect, texture2ScaleProp);
					rect = EditorGUILayout.GetControlRect();
					rect.x += indent;
					rect.width -= indent;
					EditorGUI.PropertyField(rect, texture2AlphaProp);
				}
			}

			isShowingWiggleOptions = EditorGUILayout.Foldout(isShowingWiggleOptions, new GUIContent("Wiggle Options"), true);
			if (isShowingWiggleOptions)
			{
				for (int i = 0; i < wiggleProfilesProp.arraySize; i++)
				{
					EditorGUILayout.PropertyField(wiggleProfilesProp.GetArrayElementAtIndex(i), new GUIContent("Layer " + (i).ToString()));
				}
				rect = EditorGUILayout.GetControlRect();
				rect.x += indent;
				rect.width -= indent;
				if (GUI.Button(rect, "Reset Wiggle Progress", EditorStyles.miniButton))
				{
					shape.ResetWiggle();
				}
				rect = EditorGUILayout.GetControlRect();
				rect.x += indent;
				rect.width -= indent;
				if (GUI.Button(rect, "Set Zero Wiggle", EditorStyles.miniButton))
				{
					shape.SetWiggle(0);
				}
				rect = EditorGUILayout.GetControlRect();
				rect.x += indent;
				rect.width -= indent;
				if (GUI.Button(rect, "Set Default Wiggle", EditorStyles.miniButton))
				{
					shape.SetWiggle(1);
				}
				rect = EditorGUILayout.GetControlRect();
				rect.x += indent;
				rect.width -= indent;
				EditorGUIUtility.labelWidth = 140f;
				EditorGUI.PropertyField(rect, isWigglePausedProp);
				rect = EditorGUILayout.GetControlRect();
				rect.x += indent;
				rect.width -= indent;
				EditorGUI.PropertyField(rect, isWigglingInEditorProp);
			}
			rect = EditorGUILayout.GetControlRect();
			rect.x += indent;
			rect.width -= indent;

			EditorGUI.PropertyField(rect, isRespectingTintGroupProp);
		if (EditorGUI.EndChangeCheck())
		{
			shape.SetMaterialDirty();
		}

		serializedObject.ApplyModifiedProperties();
		if (shape.editorPresetReset)
		{
			shape.editorPresetReset = true;
			shape.editorSelectedPreset = SuperShapePreset.NONE;
		}
	}
}