using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SuperShapeDemo : MonoBehaviour
{
	const float DEFAULT_TEXT_FADE_IN_TIME = 0.3f;
	const float DEFAULT_TEXT_FADE_OUT_TIME = 0.3f;

	public Transform mainDemoContainer;
	public SuperShapeDemoText demoText;
	public SuperShapeDemoText demoTextBubble;
	public DynamicMonoBehaviour debugTimerText;

	public SuperShape shape1;
	public SuperShape shape2;
	public SuperShape shape3a;
	public SuperShape shape3b;
	public SuperShape shape3c;
	public SuperShape shape3d1;
	public SuperShape shape3d2;
	public SuperShape shape4;
	public SuperShape shape4b1;
	public SuperShape shape4b2;
	public SuperShape shape4d;
	public SuperShape shape5;
	public SuperShape shapeP;
	public CanvasLockOn perfContainer;
	public SuperShape[] row = new SuperShape[9];
	public SuperShape[] all = new SuperShape[9 * 13];

	public Texture2D speechBubbleTexture;
	public SuperShapeDemoAssassin monday;
	public SuperShapeDemoAssassin tuesday;
	public SuperShapeDemoAssassin wednesday;
	public SuperShapeDemoAssassin thursday;
	public SuperShapeDemoAssassin friday;
	public SuperShapeDemoAssassin saturday;
	public SuperShapeDemoAssassin sunday;
	public Transform tempTailTip;

	public SuperShape restartButton;

	float startDelay;
	public float startTime = 0;
	bool isStarted = false;

	float currentTime = 0;
	float prevTime = -1;

	private Vector2[] undoTempA;
	private Vector2[] undoTempB;

	void StartMusic()
	{
		TheMusicPlayer.PlayMusic();
		TheMusicPlayer.instance.backgroundMusicAudioSource.time = startTime;
	}

	bool IsNewTime(float time)
	{
		return (time >= startTime && currentTime >= time && prevTime < time);
	}

	private void Start()
	{
		startDelay = TheGameTime.time + 1;
		Cursor.visible = false;
		if (!Application.isEditor) { startTime = 0; }

		shape1.SetupPreset(SuperShapePreset.Triangle);
		shape1.SetLayerCount(0);

		shape2.SetupPreset(SuperShapePreset.Rectangle, 5);
		shape2.SetActive(false);
		shape3a.SetActive(false);
		shape3b.SetActive(false);
		shape3c.SetActive(false);
		shape3d1.SetActive(false);
		shape3d2.SetActive(false);
		shape4.SetActive(false);
		shape5.SetActive(false);

		monday.SetActive(false);
		tuesday.SetActive(false);
		wednesday.SetActive(false);
		thursday.SetActive(false);
		friday.SetActive(false);
		saturday.SetActive(false);
		//sunday.SetActive(false);

		restartButton.SetActive(false);
	}

	void Update()
	{
		if (!isStarted)
		{
			currentTime = TheGameTime.time - startDelay;
			if (currentTime < 0)
			{
				debugTimerText.text.text = currentTime.ToString();
				return;
			}
			StartMusic();
			isStarted = true;
			shape1.SetActive(startTime <= 36.05f);
		}

		currentTime = TheGameTime.time + startTime - startDelay; // TheMusicPlayer.instance.backgroundMusicAudioSource.time;
		debugTimerText.text.text = currentTime.ToString();

		if (IsNewTime(0.00f))
		{
			shape1.SetActive(true);
			//shape1.MoveLoop(new Vector3(-300, 0, 0), 3.5f, Lerp.SinFull);
			//shape1.MoveLoop(new Vector3(0, 150, 0), 3.5f/ 2, Lerp.SinFull);
			//shape1.Rotate(360 * 2, 3.5f);
			Text("SuperShape Demo");
		}
		if (IsNewTime(3.50f))
		{
			shape1.wiggleProfiles[0].pattern = WigglePattern.NONE;
			shape1.wiggleProfiles[0].musicOption = WiggleMusicOption.Basic;

			demoText.FadeText();
		}
		if (IsNewTime(6.80f))
		{
			shape1.Move(new Vector3(-600,0,0), 3.6f, Lerp.SinFull);
			shape1.Rotate(-1080, 3.6f, Lerp.Linear);
		}
		if (IsNewTime(10.40f))
		{
			shape1.wiggleProfiles[0].musicOption = WiggleMusicOption.NONE;
			shape1.ResetWiggle();
			shape1.CancelAllCoroutines();
			shape1.SetZero();
			shape1.AddLayerOverTime(0.1f);
			Text("Layers");
		}
		if (IsNewTime(12.90f))
		{
			shape1.AddLayerOverTime(0.1f);
		}
		if (IsNewTime(13.80f))
		{
			shape1.AddLayerOverTime(0.1f);
			demoText.FadeText();
		}
		if (IsNewTime(15.50f))
		{
			shape1.AddLayerOverTime(0.1f);
		}
		if (IsNewTime(17.10f))
		{
			shape1.AddLayerOverTime(0.1f);
		}
		if (IsNewTime(19.60f))
		{
			shape1.MorphToPreset(SuperShapePreset.Square);
			Text("Custom Shapes");
		}
		if (IsNewTime(20.60f))
		{
			shape1.MorphToPreset(SuperShapePreset.Pentagon);
		}
		if (IsNewTime(22.10f))
		{
			shape1.MorphToPreset(SuperShapePreset.Hexagon);
		}
		if (IsNewTime(23.00f))
		{
			demoText.FadeText();
			shape1.MorphToPreset(SuperShapePreset.Octagon);
		}
		if (IsNewTime(23.90f))
		{
			shape1.RemoveLayerOverTime(0.1f);
			shape1.ChangeLayerColorTo(0, Color.red, 1.0f);
			//shape1.ignoreVerts = new List<int>() { 1, 3, 5, 7 };
			//shape1.MorphToPreset(SuperShapePreset.Square);
		}

		//shape changes
		if (IsNewTime(26.60f))
		{
			Text("Concave Shapes");
			shape1.MorphToPreset(SuperShapePreset.C);
		}
		if (IsNewTime(27.50f))
		{
			shape1.ignoreVerts = new List<int>() { 6 };
			shape1.MorphToPreset(SuperShapePreset.C7);
		}
		if (IsNewTime(29.20f))
		{
			//shape1.wiggleProfiles[0].rotateOption = WiggleRotateOption.Clockwise;
			shape1.MorphToPreset(SuperShapePreset.Sigma);
		}
		if (IsNewTime(30.90f))
		{
			demoText.FadeText();
			//shape1.wiggleProfiles[1].rotateOption = WiggleRotateOption.CClockwise;
			shape1.MorphToPreset(SuperShapePreset.X);
		}
		if (IsNewTime(33.45f))
		{
			//shape1.wiggleProfiles[2].rotateOption = WiggleRotateOption.Clockwise;
			//shape1.wiggleProfiles[4].rotateOption = WiggleRotateOption.CClockwise;
			shape1.ignoreVerts = new List<int>() { 1, 5 };
			shape1.MorphToPreset(SuperShapePreset.Chevron);
			shape1.RescaleTo(0.75f);
		}
		if (IsNewTime(34.35f))
		{
			shape2 = shape1;
			//shape1.wiggleProfiles[0].rotateOption = WiggleRotateOption.NoRotate;
			//shape1.wiggleProfiles[1].rotateOption = WiggleRotateOption.NoRotate;
			//shape1.wiggleProfiles[2].rotateOption = WiggleRotateOption.NoRotate;
			//shape1.wiggleProfiles[4].rotateOption = WiggleRotateOption.NoRotate;
			shape1.ignoreVerts = new List<int>() { 2, 5 };
			shape1.MorphToPreset(SuperShapePreset.Trapazoid);
			shape1.RescaleTo(0.9f);
		}
		if (IsNewTime(36.05f))
		{
			shape1.MorphToPreset(SuperShapePreset.Rectangle);
		}

		//color changes
		if (IsNewTime(25.80f))
		{
			shape1.ChangeLayerColorTo(1,Color.yellow,0.2f);
		}
		if (IsNewTime(26.40f))
		{
			shape1.ChangeLayerColorTo(2, Color.red, 0.2f);
		}
		if (IsNewTime(27.10f))
		{
			shape1.ChangeLayerColorTo(3, Color.cyan, 0.2f);
		}
		if (IsNewTime(27.40f))
		{
			shape1.ChangeLayerColorTo(4, Color.magenta, 0.2f);
		}
		if (IsNewTime(29.10f))
		{
			shape1.ChangeAllLayerColorsAddHSV(new Vector3(0.25f, -0.1f, 0), 0.2f);
		}
		if (IsNewTime(29.80f))
		{
			shape1.ChangeAllLayerColorsAddHSV(new Vector3(0.25f, -0.1f, 0), 0.2f);
		}
		if (IsNewTime(30.35f))
		{
			shape1.ChangeAllLayerColorsAddHSV(new Vector3(0.25f, -0.1f, 0), 0.2f);
		}
		if (IsNewTime(30.90f))
		{
			shape1.ChangeAllLayerColorsAddHSV(new Vector3(0.25f, -0.1f, 0), 0.2f);
		}
		if (IsNewTime(32.60f))
		{
			shape1.ChangeAllLayerColorsAddHSV(new Vector3(-0.50f, -0.1f, 0), 0.2f);
		}
		if (IsNewTime(33.25f))
		{
			shape1.ChangeAllLayerColorsAddHSV(new Vector3(-0.50f, -0.1f, 0), 0.2f);
		}
		if (IsNewTime(33.90f))
		{
			shape1.ChangeAllLayerColorsAddHSV(new Vector3(-0.50f, 0.2f, 0), 0.2f);
		}
		if (IsNewTime(34.35f))
		{
			shape1.ChangeAllLayerColorsAddHSV(new Vector3(-0.50f, 0.2f, 0), 0.2f);
		}
		if (IsNewTime(36.05f))
		{
			shape1.ChangeAllLayerColorsAddHSV(new Vector3(-0.50f, 0.2f, 0), 0.2f);
		}
		if (IsNewTime(36.70f))
		{
			shape2.ChangeLayerColorTo(0, Color.white, 0.4f);
			shape2.ChangeLayerColorTo(1, Color.black, 0.4f);
			shape2.ChangeLayerColorTo(2, Color.white, 0.4f);
			shape2.ChangeLayerColorTo(3, Color.blue, 0.4f);
			shape2.ChangeLayerColorTo(4, Color.green, 0.4f);
			shape2.RescaleTo(1, 0.4f);
		}

		const float moveTime = 0.8f;
		if (IsNewTime(36.20f))
		{
			shape2.SetActive(true);
			shape2.localPosition = Vector3.zero;
			shape2.sizeDelta = Vector2.zero;
			if (shape1 != shape2)
			{
				shape1.SetActive(false);
				shape2.SetupPreset(SuperShapePreset.Rectangle, 5);
			}
		}
		if (IsNewTime(37.80f))
		{
			Text("11 Vertex Movement Modes");
			shape2.isWigglePaused = true;
			shape2.ResetWiggle();
			shape2.MoveVertex(2, 1, LayerPropagationMode.NONE, VertexPropagationMode.NONE, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(39.10f))
		{
			shape2.MoveVertex(2, 1, LayerPropagationMode.NONE, VertexPropagationMode.Direct, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(40.70f))
		{
			shape2.MoveVertex(2, 1, LayerPropagationMode.NONE, VertexPropagationMode.Radial, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(41.80f))
		{
			shape2.MoveVertex(2, 1, LayerPropagationMode.NONE, VertexPropagationMode.MirroredYDirect, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(42.70f))
		{
			shape2.MoveVertex(2, 1, LayerPropagationMode.NONE, VertexPropagationMode.MirroredXDirect, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}

		if (IsNewTime(44.00f))
		{
			Text("11 Layer Movement Modes");
			shape2.localPosition = Vector3.zero;
			shape2.sizeDelta = Vector2.zero;
			shape2.MoveVertex(2, 1, LayerPropagationMode.AllDirect, VertexPropagationMode.NONE, new Vector2(300, 0), moveTime, Lerp.Triangle_20Pct_Summit);
		}

		if (IsNewTime(45.90f))
		{
			shape2.MoveVertex(2, 1, LayerPropagationMode.AllRadial, VertexPropagationMode.NONE, new Vector2(300, 0), moveTime, Lerp.Triangle_20Pct_Summit);
		}

		if (IsNewTime(47.60f))
		{
			shape2.MoveVertex(0, 1, LayerPropagationMode.OutwardPivot, VertexPropagationMode.NONE, new Vector2(-500, -100), 0.5f, Lerp.Linear);
		}

		if (IsNewTime(49.00f))
		{
			Text("Full Unity Undo/Redo Support");
			System.Array.Resize(ref undoTempA, shape2.baseVerts.Length);
			shape2.baseVerts.CopyTo(undoTempA, 0);
			shape2.MoveVertex(0, 1, LayerPropagationMode.OutwardPivot, VertexPropagationMode.NONE, new Vector2(-100, 300), 0.5f, Lerp.Linear);
		}

		if (IsNewTime(49.80f))
		{
			undoTempA.CopyTo(shape2.baseVerts, 0);
			undoTempA.CopyTo(shape2.prevVerts, 0);
			undoTempA.CopyTo(shape2.nextVerts, 0);
			shape2.MoveVertex(0, 1, LayerPropagationMode.OutwardPivot, VertexPropagationMode.NONE, new Vector2(-600, 100), 0.5f, Lerp.Linear);
		}

		if (IsNewTime(50.60f))
		{
			undoTempA.CopyTo(shape2.baseVerts, 0);
			undoTempA.CopyTo(shape2.prevVerts, 0);
			undoTempA.CopyTo(shape2.nextVerts, 0);
			shape2.MoveVertex(0, 1, LayerPropagationMode.OutwardPivot, VertexPropagationMode.NONE, new Vector2(200, -200), 0.5f, Lerp.Linear);
		}

		if (IsNewTime(51.80f))
		{
			System.Array.Resize(ref undoTempB, shape2.baseVerts.Length);
			shape2.baseVerts.CopyTo(undoTempB, 0);
			undoTempA.CopyTo(shape2.baseVerts, 0);
			undoTempA.CopyTo(shape2.prevVerts, 0);
			undoTempA.CopyTo(shape2.nextVerts, 0);
			shape2.SetMeshDirty();
		}

		if (IsNewTime(52.30f))
		{
			undoTempB.CopyTo(shape2.baseVerts, 0);
			undoTempB.CopyTo(shape2.prevVerts, 0);
			undoTempB.CopyTo(shape2.nextVerts, 0);
			shape2.SetMeshDirty();
		}

		if (IsNewTime(52.80f))
		{
			undoTempA.CopyTo(shape2.baseVerts, 0);
			undoTempA.CopyTo(shape2.prevVerts, 0);
			undoTempA.CopyTo(shape2.nextVerts, 0);
			shape2.SetMeshDirty();
		}

		/*
		if (IsNewTime(53.20f))
		{
			undoTempB.CopyTo(shape2.baseVerts, 0);
			undoTempB.CopyTo(shape2.prevVerts, 0);
			undoTempB.CopyTo(shape2.nextVerts, 0);
		}
		if (IsNewTime(53.60f))
		{
			undoTempA.CopyTo(shape2.baseVerts, 0);
			undoTempA.CopyTo(shape2.prevVerts, 0);
			undoTempA.CopyTo(shape2.nextVerts, 0);
		}
		*/
		if (IsNewTime(53.20f))
		{
			shape2.localPosition = Vector3.one;
			shape2.localScale = Vector2.one;
			shape2.localRotation = Quaternion.Euler(Vector3.zero);
			shape2.sizeDelta = Vector2.zero;
			shape2.SetupPreset(SuperShapePreset.Rectangle, 5);
		}


		if (IsNewTime(54.90f))
		{
			Text("Combine Vertex + Layer Movement Modes");
			shape2.MoveVertex(3, 1, LayerPropagationMode.InwardDirect, VertexPropagationMode.Direct, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(56.90f))
		{
			shape2.MoveVertex(3, 1, LayerPropagationMode.OutwardDirect, VertexPropagationMode.Radial, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(57.90f))
		{
			shape2.MoveVertex(3, 1, LayerPropagationMode.OutwardRadial, VertexPropagationMode.Direct, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(58.90f))
		{
			shape2.MoveVertex(3, 1, LayerPropagationMode.InwardRadial, VertexPropagationMode.Radial, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(59.90f))
		{
			shape2.MoveVertex(1, 1, LayerPropagationMode.OutwardPivot, VertexPropagationMode.Direct, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(60.90f))
		{
			shape2.MoveVertex(1, 1, LayerPropagationMode.OutwardPivot, VertexPropagationMode.Radial, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(61.90f))
		{
			shape2.MoveVertex(4, 1, LayerPropagationMode.InwardPivot, VertexPropagationMode.Direct, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(62.90f))
		{
			shape2.MoveVertex(4, 1, LayerPropagationMode.InwardPivot, VertexPropagationMode.Radial, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(63.90f))
		{
			shape2.MoveVertex(4, 1, LayerPropagationMode.AllRadial, VertexPropagationMode.NextOnly, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(64.90f))
		{
			shape2.MoveVertex(4, 1, LayerPropagationMode.AlternatingInverse, VertexPropagationMode.Radial, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(65.90f))
		{
			shape2.MoveVertex(4, 1, LayerPropagationMode.AllDirect, VertexPropagationMode.AlternatingInverse, new Vector2(300, 300), moveTime, Lerp.Triangle_20Pct_Summit);
		}

		if (IsNewTime(67.00f))
		{
			Text("Standard Unity Rotation");
			shape2.SetActive(true);
			shape2.localPosition = Vector3.zero;
			shape2.localScale = Vector2.one;
			shape2.localRotation = Quaternion.Euler(Vector3.zero);
			shape2.sizeDelta = Vector2.zero;
			shape2.Rotate(new Vector3(60, 0, 0), 0.4f);
			shape2.MorphToPreset(SuperShapePreset.Square, 0.4f);
		}
		if (IsNewTime(68.20f))
		{
			shape2.Rotate(new Vector3(-120, 0, 0), 0.4f);
		}
		if (IsNewTime(68.40f))
		{
			shape2.Rotate(new Vector3(0, 300, 0), 2.4f);
		}
		if (IsNewTime(69.90f))
		{
			//shape2.Rotate(new Vector3(120, 0, 0), 0.4f);
		}
		if (IsNewTime(70.10f))
		{
			demoText.FadeOut();
			shape2.Rotate(new Vector3(0, 0, 180), 71.00f - 69.90f);
		}
		if (IsNewTime(71.00f))
		{
			shape2.Rotate(new Vector3(0, 0, -180), 71.80f - 71.00f);
		}
		if (IsNewTime(71.80f))
		{

		}
		if (IsNewTime(72.10f))
		{
			shape2.Rotate(new Vector3(0, 0, -shape2.localRotation.eulerAngles.z), 0.25f);
		}
		if (IsNewTime(72.40f))
		{
			shape2.Rotate(new Vector3(0, -shape2.localRotation.eulerAngles.y, 0), 0.25f);
		}
		if (IsNewTime(72.70f))
		{
			shape2.Rotate(new Vector3(-shape2.localRotation.eulerAngles.x, 0, 0), 0.25f);
		}
		if (IsNewTime(73.00f))
		{
			shape2.Rotate(new Vector3(0, 0, -shape2.localRotation.eulerAngles.z), 0.25f);
		}
		if (IsNewTime(73.30f))
		{
			Text("Standard Unity Scale");
			shape2.SetZeroRotation();
			shape2.RescaleAdd(new Vector3(0.9f,0,0), 75.05f - 73.30f, Lerp.SinFull);
		}
		if (IsNewTime(75.05f))
		{
			shape2.RescaleAdd(new Vector3(0f, 0.8f, 0), 76.75f - 75.05f, Lerp.SinFull);
		}
		if (IsNewTime(75.25f))
		{

		}
		if (IsNewTime(76.75f))
		{
			demoText.FadeOut();
			shape2.RescaleAdd(new Vector3(-0.8f, 0.8f, 0), 76.75f - 75.05f, Lerp.SinFull);
		}
		if (IsNewTime(77.00f))
		{

		}
		if (IsNewTime(78.35f))
		{
			shape2.RescaleAdd(new Vector3(-0.8f, 0.8f, 0), 76.75f - 75.05f, Lerp.SinFull);
		}

		if (IsNewTime(79.00f))
		{
		}

		if (IsNewTime(80.60f))
		{
			shape2.SetupPreset(SuperShapePreset.Square, 5);
			shape2.SetZero();
			shape2.RescaleTo(Vector3.one, 0.1f);
			shape2.ResetWiggle();
			shape2.isWigglePaused = true;

			Text("9-Sliced By Default");
			shape2.ResizeSliceTo(new Vector2(400, 0), 0.2f);
		}
		if (IsNewTime(82.30f))
		{
			shape2.ResizeSliceTo(new Vector2(0, 400), 0.2f);
		}
		if (IsNewTime(84.00f))
		{
			Text("Automated Re-Slicing Tool");
			shape2.ResizeSliceRect(Vector2.zero);
			shape2.ResizeSliceTo(new Vector2(0, 0), 0.2f);
		}

		if (IsNewTime(85.80f))
		{
			shape2.ResizeSliceTo(new Vector2(400, 200), 0.2f);
		}
		if (IsNewTime(86.40f))
		{

		}

		const float pivotDuration = 1.4f;
		if (IsNewTime(87.00f))
		{
			shape2.ResetWiggle();
			shape2.wiggleProfiles[0].pattern = WigglePattern.NONE;
			shape2.localPosition = Vector3.one;
			shape2.localScale = Vector2.one;
			shape2.localRotation = Quaternion.Euler(Vector3.zero);
			shape2.sizeDelta = new Vector2(400,200);
			Text("Assign Pivot To Any Vertex");
			shape2.AssignVertexPivot(3, 4);
			shape2.Rotate(-110f, pivotDuration, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(89.00f))
		{
			shape2.AssignVertexPivot(1, 2);
			shape2.Rotate(110f, pivotDuration, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(90.70f))
		{
			shape2.AssignVertexPivot(0, 0);
			shape2.Rotate(360f, pivotDuration, Lerp.Triangle_20Pct_Summit);
		}
		if (IsNewTime(92.20f))
		{
			shape2.AssignVertexPivot(2, 3);
			shape2.Rotate(360f, pivotDuration, Lerp.Triangle_20Pct_Summit);
		}


		if (IsNewTime(93.30f))
		{
			Text("Rotation Wiggle Modes");
			shape2.isWigglePaused = false;
			shape2.wiggleProfiles[0].pattern = WigglePattern.NONE;
			shape2.wiggleProfiles[0].rotateOption = WiggleRotateOption.Clockwise;
			shape2.wiggleProfiles[0].speed = 1.0f / 0.85f;
			shape2.wiggleProfiles[1].speed = 1.0f / 0.85f;
			shape2.wiggleProfiles[2].speed = 1.0f / 0.85f;
			shape2.wiggleProfiles[3].speed = 1.0f / 0.85f;
			shape2.wiggleProfiles[4].speed = 1.0f / 0.85f;
		}
		if (IsNewTime(95.00f))
		{
			shape2.wiggleProfiles[1].rotateOption = WiggleRotateOption.CClockwise;
		}
		if (IsNewTime(95.85f))
		{
			shape2.wiggleProfiles[2].rotateOption = WiggleRotateOption.Clockwise;
		}
		if (IsNewTime(96.70f))
		{
			demoText.FadeText();
			shape2.wiggleProfiles[4].rotateOption = WiggleRotateOption.CClockwise;
		}

		if (IsNewTime(97.00f))
		{
			shape2.AssignVertexPivot(-99, -99);
			shape2.wiggleProfiles[0].pattern = WigglePattern.NONE;
			shape2.wiggleProfiles[0].isStoppingWiggleRotation = true;
			shape2.wiggleProfiles[1].isStoppingWiggleRotation = true;
			shape2.wiggleProfiles[2].isStoppingWiggleRotation = true;
			shape2.wiggleProfiles[3].isStoppingWiggleRotation = true;
			shape2.wiggleProfiles[4].isStoppingWiggleRotation = true;
			shape2.ResizeSliceTo(new Vector2(400, 400), 0.2f);
			shape2.MoveToLocal(Vector3.zero, 0.8f);
			shape2.fillTexture1Alpha = 0;
			shape2.fillTexture1 = shape3a.fillTexture1;
			shape2.sliceHost = shape3a.sliceHost;
			shape3a = shape2;
		}

		if (IsNewTime(98.00f))
		{
			shape3a.SetActive(true);
			shape3b.SetActive(true);
			shape3c.SetActive(true);
			shape3b.localPosition = new Vector3(-1300, 0, 0);
			shape3c.localPosition = new Vector3( 1300, 0, 0);
			shape3a.sizeDelta = new Vector2(400, 400);
			shape3a.blendMode = BlendModes.BlendMode.NONE;
			shape3b.blendMode = BlendModes.BlendMode.NONE;
			shape3c.blendMode = BlendModes.BlendMode.NONE;
			shape3a.layerColors[0] = Color.white;
			shape3b.layerColors[0] = Color.white;
			shape3c.layerColors[0] = Color.white;
			shape3a.textureCount = SuperShapeTextureCount.One;
			shape3b.textureCount = SuperShapeTextureCount.One;
			shape3c.textureCount = SuperShapeTextureCount.One;
			shape3a.fillTexture1Alpha = 0;
			shape3b.fillTexture1Alpha = 1;
			shape3c.fillTexture1Alpha = 1;
			shape3a.innerTextureMode = InnerTextureMode.Fixed;
		}

		if (IsNewTime(99.50f))
		{
			//next section -- quiet raindrops - textures
			shape3a.TextureAlphaTo(DMBMotionTargetTexture.Texture1, 1.0f, 1.5f);
			Text("Fill Texture");
		}

		if (IsNewTime(103.00f))
		{
			demoText.FadeOut();
			float myMoveTime = 1.2f;
			//next section -- quiet raindrops - textures
			shape3b.Move(new Vector3( 500, 0, 0), myMoveTime);
			shape3c.Move(new Vector3(-500, 0, 0), myMoveTime);
		}

		if (IsNewTime(104.00f))
		{
			Text("Fixed UVs");
			float myMoveTime = 2.0f;
			shape3a.innerTextureMode = InnerTextureMode.Fixed;
			shape3a.ResizeAdd(new Vector2(200, 200), myMoveTime, Lerp.Triangle_20Pct_Summit);
		}

		if (IsNewTime(106.00f))
		{
			Text("Vertex-based UVs");
			float myMoveTime = 2.0f;
			shape3a.innerTextureMode = InnerTextureMode.MinMaxVerts;
			shape3a.fillTexture1Scale = Vector2.one;
			shape3a.ResizeAdd(new Vector2(200, 200), myMoveTime, Lerp.Triangle_20Pct_Summit);
			shape3b.ResizeAdd(new Vector2(-200, 200), myMoveTime, Lerp.Triangle_20Pct_Summit);
			shape3c.ResizeAdd(new Vector2(200, -200), myMoveTime, Lerp.Triangle_20Pct_Summit);
		}

		if (IsNewTime(108.10f))
		{
			Text("Screenspace UVs");
			shape3d1.SetActive(true);
			shape3d2.SetActive(true);
			shape3d1.localPosition = new Vector3(-400, -900, 0);
			shape3d2.localPosition = new Vector3( 400,  900, 0);
			shape3d1.fillTexture1Alpha = 1;
			shape3d2.fillTexture1Alpha = 1;
			//next section -- rising energy - screen space textures
			float myMoveTime = 5.0f;
			shape3d1.Move(new Vector3(0,  1800, 0), myMoveTime);
			shape3d2.Move(new Vector3(0, -1800, 0), myMoveTime);
			shape3d1.Rotate(-720, myMoveTime);
			shape3d2.Rotate( 720, myMoveTime);

			shape3a.ChangeLayerColorAddHSV(0, new Vector3(-300f / 360f, 1.0f, 0), 3f);
			shape3b.ChangeLayerColorAddHSV(0, new Vector3(-300f / 360f, 1.0f, 0), 3f);
			shape3c.ChangeLayerColorAddHSV(0, new Vector3(-300f / 360f, 1.0f, 0), 3f);
			shape3a.MoveTo(new Vector3(0, 150, 0), myMoveTime, Lerp.Linear);
			shape3a.ResizeAdd(new Vector2(300, 300), myMoveTime, Lerp.Linear);
			shape3b.ResizeAdd(new Vector2(-300, -300), myMoveTime, Lerp.Linear);
			shape3c.ResizeAdd(new Vector2(-300, -300), myMoveTime, Lerp.Linear);
			shape3b.Move(new Vector2(-400, 0), myMoveTime, Lerp.Linear);
			shape3c.Move(new Vector2( 400, 0), myMoveTime, Lerp.Linear);
			shape3a.TextureAlphaAdd(DMBMotionTargetTexture.Texture1, -1.0f, 2.0f, Lerp.Linear, 3.5f);
		}

		if (IsNewTime(114.50f))
		{
			shape3a.SetActive(true);
			shape3a.fillTexture1Alpha = 0;
			shape3a.localPosition = new Vector3(0,150,0);
			shape3a.sizeDelta = new Vector2(700, 700);
			wednesday.SetAll(0);
			wednesday.localPosition = new Vector3(0, -1050, 0);
			wednesday.Move(new Vector3(0, 1000, 0), 0.1f);
		}

		if (IsNewTime(114.90f))
		{
			Text("Blend Modes");
			wednesday.SetAll(0); // excited
			shape3a.layerColors[0] = Color.yellow;
			shape3a.blendMode = BlendModes.BlendMode.ColorBurn;
		}

		if (IsNewTime(116.20f))
		{
			wednesday.SetAll(1); // furious
			shape3a.layerColors[0] = Color.red;
			shape3a.blendMode = BlendModes.BlendMode.Darken;
		}

		if (IsNewTime(118.10f))
		{
			wednesday.SetAll(2); // bored
			shape3a.layerColors[0] = Color.blue;
			shape3a.blendMode = BlendModes.BlendMode.Hue;
		}

		if (IsNewTime(119.00f))
		{
			wednesday.SetAll(3); // pistol
			shape3a.layerColors[0] = Color.green;
			shape3a.blendMode = BlendModes.BlendMode.ColorDodge;
		}

		if (IsNewTime(119.80f))
		{
			wednesday.SetAll(4); // thinking
			shape3a.layerColors[0] = Color.yellow;
			shape3a.blendMode = BlendModes.BlendMode.DarkerColor;
		}

		if (IsNewTime(121.75f))
		{
			wednesday.SetAll(5); // smug
			shape3a.layerColors[0] = Color.cyan;
			shape3a.blendMode = BlendModes.BlendMode.Color;
			shape3a.Rotate(-405f, 127.00f - 121.75f, Lerp.Quad);
		}

		if (IsNewTime(123.25f))
		{
			demoText.FadeOut();
			wednesday.SetAll(6); // injured
			shape3a.layerColors[0] = Color.red;
			shape3a.blendMode = BlendModes.BlendMode.Difference;
		}

		/*
		if (IsNewTime(124.75f))
		{
			wednesday.SetAll(7); // storyA
			shape3a.layerColors[0] = Color.white;
			shape3a.blendMode = BlendModes.BlendMode.Saturation;
		}
		*/

		if (IsNewTime(124.95f))
		{
			wednesday.SetAll(8); // storyB
			shape3a.layerColors[0] = Color.green;
			shape3a.blendMode = BlendModes.BlendMode.Subtract;
		}

		/*
		if (IsNewTime(125.60f))
		{
			wednesday.SetAll(10); // sad
			shape3a.layerColors[0] = Color.green;
			shape3a.blendMode = BlendModes.BlendMode.Luminosity;
		}
		*/

		if (IsNewTime(125.80f))
		{
			wednesday.SetAll(10); // storyC
			shape3a.layerColors[0] = Color.magenta;
			shape3a.blendMode = BlendModes.BlendMode.LinearBurn;
		}

		if (IsNewTime(126.45f))
		{
			wednesday.SetAll(11); // shocked
			shape3a.layerColors[0] = Color.white; //makes greyscale
			shape3a.blendMode = BlendModes.BlendMode.Saturation;
		}

		if (IsNewTime(127.00f))
		{
			shape3a.ResizeSliceRect(Vector2.zero, true);
			shape3a.Slice(1, 3);
			shape3a.Move(new Vector3(0, 30f, 0), 0.05f);
			shape3a.Rotate(60f, 1.0f, Lerp.Linear, 0.3f);
			shape3a.Move(new Vector3(-1200, 0, 0), 1.0f, Lerp.Linear, 0.3f);
			shape3a.Move(new Vector3(0, -700f, 0), 1.0f, Lerp.Quad, 0.3f);
			shape3a.sliceHost.Move(new Vector3(0, -30f, 0), 0.05f);
			shape3a.sliceHost.Rotate(-60f, 1.8f, Lerp.Linear, 0.4f);
			shape3a.sliceHost.Move(new Vector3(1200, 0, 0), 1.0f, Lerp.Linear, 0.3f);
			shape3a.sliceHost.Move(new Vector3(0, -700f, 0), 1.0f, Lerp.Quad, 0.3f);
			wednesday.MoveTo(new Vector3(0, -1100f, 0), 0.6f, Lerp.QuadJ, 0.3f);
		}


		if (IsNewTime(127.40f))
		{
			shape4.ResetWiggle();
			shape4.isWigglePaused = true;
			shape4.localPosition = new Vector3(0, -800, 0);
			shape4.localRotation = Quaternion.Euler(new Vector3(60, 0, 0));
			shape4.MoveTo(new Vector3(0, 600, 0),1.2f, Lerp.SinEaseInOut);
			shape4.Rotate(new Vector3(0, 30, 0), 1.0f, Lerp.SinFull);
			shape4.RescaleMult(2, 0.4f,Lerp.Linear, 1.0f);
		}


		if (IsNewTime(128.60f))
		{
			shape4.shapeMesh.GetProxyMaskMesh().GetGameObject().SetActive(true);
			Text("Dynamic Proxy Masking");
			shape4.isWigglePaused = false;
			shape4.Rotate(360, 2, Lerp.Linear, 0, -1); //loops
			shape4.wiggleProfiles[0].rotateOption = WiggleRotateOption.Clockwise;
			shape4.wiggleProfiles[1].rotateOption = WiggleRotateOption.CClockwise;
			shape4.MoveTo(new Vector3(0, -150, 0), 2.4f, Lerp.Linear);
			sunday.SetActive(true);
			sunday.Move(new Vector3(0, 80, 0), 0.5f, Lerp.TwoHops, 2.4f);
		}

		if (IsNewTime(132.90f))
		{
			Text("Movement Sync Toggles");
			shape4.shapeMesh.GetProxyMaskMesh().SetIsMatchingParentPosition(true);
			shape4.Move(new Vector3(-400,0,0), 2.0f, Lerp.SinFull);
			shape4.Move(new Vector3(0,-200,0), 2.0f, Lerp.SinHalf);
		}


		if (IsNewTime(133.60f))
		{
			saturday.SetActive(true);
			saturday.localPosition = new Vector3(100, 180, 0);
			saturday.localScale = new Vector3(-1, 1, 1);
			shape4d.localPosition = new Vector3(-1570, -250, 0);
			shape4d.localRotation = Quaternion.Euler(new Vector3(0f,0f,-20f));
			shape4d.shapeMesh.GetProxyMaskMesh().SetIsMatchingParentPosition(true);
			shape4d.shapeMesh.GetProxyMaskMesh().SetIsMatchingParentRotation(true);
			shape4d.MoveTo(new Vector3(0,-200,0), 0.8f);
			shape4d.Rotate(20, 0.4f);
		}


		if (IsNewTime(134.40f))
		{
			shape4.CancelAllCoroutines();

			shape4d.shapeMesh.GetProxyMaskMesh().SetIsMatchingParentPosition(false);
			//shape4d.mesh.proxyMaskMesh.isMatchingParentRotation = false;
			saturday.SetAll(1);
			sunday.SetAll(1);
			saturday.localScale = Vector3.one;
			shape4d.Move(new Vector3(-100, 30, 0), 0.1f);
			//shape4d.Rotate(40, 0.5f, Lerp.TripleHigh);
			saturday.Move(new Vector3(-200, 0, 0), 0.3f, Lerp.Linear);
			saturday.Move(new Vector3(0, 250, 0), 0.3f, Lerp.TwoHops);

			sunday.localScale = new Vector3(-1, 1, 1);
			shape4.Move(new Vector3(50, 0, 0), 0.1f, Lerp.Sqrt);
			shape4.Move(new Vector3(50, 0, 0), 0.4f, Lerp.Shake2);
		}

		if (IsNewTime(135.20f))
		{
			demoText.FadeOut();
			saturday.localScale = new Vector3(-1,1,1);
			saturday.SetAll(2);
			shape4d.shapeMesh.GetProxyMaskMesh().SetIsMatchingParentPosition(true);
			shape4d.MoveTo(new Vector3(-1550, 500, 0), 1.4f, Lerp.Linear, 0.2f);
		}


		if (IsNewTime(135.80f))
		{
			sunday.SetAll(2);
			shape4.MoveTo(new Vector3(0,-100,0), 2.0f, Lerp.Linear, 0.2f);
		}

		if (IsNewTime(137.00f))
		{
			Text("Blend Masks");
			float myMoveTime = 0.6f;
			monday.localPosition = new Vector3(-1300f, -150, 0);
			monday.MoveToLocal(new Vector3(-500, -50, 0), myMoveTime);
			tuesday.localPosition = new Vector3(1300f, -150, 0);
			tuesday.MoveToLocal(new Vector3(500, -50, 0), myMoveTime);
			shape4b1.localPosition = new Vector3(0, 800, 0);
			shape4b1.Move(new Vector3(0, -600, 0), myMoveTime / 2, Lerp.Linear, myMoveTime / 2);
			shape4b2.localPosition = new Vector3(-30, 900, 0);
			shape4b2.Move(new Vector3(0, -600, 0), myMoveTime / 2, Lerp.Linear, myMoveTime / 2);
			shape4b1.blendMode = BlendModes.BlendMode.Screen;
			shape4b2.blendMode = BlendModes.BlendMode.Screen;
		}
		if (IsNewTime(138.50f))
		{
			shape4b1.blendMode = BlendModes.BlendMode.Difference;
			shape4b2.blendMode = BlendModes.BlendMode.Difference;
		}
		if (IsNewTime(138.90f))
		{
			shape4b1.blendMode = BlendModes.BlendMode.LinearBurn;
			shape4b2.blendMode = BlendModes.BlendMode.LinearBurn;
		}
		if (IsNewTime(140.60f))
		{
			demoText.FadeText();
			shape4b1.blendMode = BlendModes.BlendMode.Subtract;
			shape4b2.blendMode = BlendModes.BlendMode.Subtract;
		}
		if (IsNewTime(141.25f))
		{
			shape4b1.blendMode = BlendModes.BlendMode.Luminosity;
			shape4b2.blendMode = BlendModes.BlendMode.Luminosity;
		}
		if (IsNewTime(141.90f))
		{
			shape4b1.blendMode = BlendModes.BlendMode.Divide;
			shape4b2.blendMode = BlendModes.BlendMode.Divide;
		}
		if (IsNewTime(142.30f))
		{
			shape4b1.blendMode = BlendModes.BlendMode.Hue;
			shape4b2.blendMode = BlendModes.BlendMode.Hue;
		}
		if (IsNewTime(143.00f))
		{
			shape4b1.blendMode = BlendModes.BlendMode.Saturation;
			shape4b2.blendMode = BlendModes.BlendMode.Saturation;
		}
		if (IsNewTime(144.90f))
		{
			shape4b1.blendMode = BlendModes.BlendMode.NONE;
			shape4b2.blendMode = BlendModes.BlendMode.NONE;
		}
		if (IsNewTime(145.75f))
		{
			shape4b1.blendMode = BlendModes.BlendMode.PASS;
			shape4b2.blendMode = BlendModes.BlendMode.PASS;
		}
		if (IsNewTime(146.40f))
		{
			shape4.shapeMesh.GetProxyMaskMesh().SetIsMatchingParentPosition(false);
			float myMoveTime = 0.8f;
			monday.MoveTo(new Vector3(-1300, -400, 0), myMoveTime);
			tuesday.MoveTo(new Vector3(1400, -400, 0), myMoveTime);
			monday.MoveTo(new Vector3(-1400, -400, 0), myMoveTime);
			shape4.Move(new Vector3(0,1050,0), myMoveTime, Lerp.QuadJ, 1.4f);
		}

		const int GRID_X = 13;
		const int GRID_Y = 9;
		const int CELL_X = 130;
		const int CELL_Y = 110;
		const int START_OFFSET_Y = 10;
		if (IsNewTime(147.10f))
		{
			perfContainer.target.localPosition = new Vector3(-CELL_X * (GRID_X - 1) / 2, -CELL_Y * ((GRID_Y - 1) / 2 + START_OFFSET_Y - 1), 0);
		}
		if (IsNewTime(148.60f))
		{
			//shape4.mesh.proxyMaskMesh.gameObject.SetActive(false);

			float myMoveTime = 162.70f - 148.60f;
			shapeP.wiggleProfiles[0].musicOption = WiggleMusicOption.NONE;
			shapeP.localPosition = new Vector3(0,-CELL_Y,0);
			SimpleRotationScript.isGlobalEnabled = false;
			shapeP.MoveToLocal(new Vector3(-CELL_X * (GRID_X - 1) / 2, CELL_Y * (GRID_Y - 1)/2, 0), myMoveTime);
			shapeP.ChangeLayerColorAddHSV(0, new Vector3(1.5f, 0, 0), myMoveTime - 4.1f, Lerp.Linear, 4.1f);
			perfContainer.target.MoveToLocal(new Vector3(-CELL_X * (GRID_X - 1) / 2, CELL_Y * (GRID_Y - 1) / 2, 0), myMoveTime - 0.75f, Lerp.Linear, 0.75f);
		}

		if (IsNewTime(148.60f))
		{
			SimpleRotationScript.isGlobalEnabled = true;
		}

		if (IsNewTime(152.7f))
		{
			shape4.SetActive(false);
			if (shape4 != null && shape4.shapeMesh != null && shape4.shapeMesh.GetProxyMaskMesh() != null) shape4.shapeMesh.GetProxyMaskMesh().GetGameObject().SetActive(false);
			shapeP.wiggleProfiles[0].musicOption = WiggleMusicOption.Basic;
			shapeP.wiggleProfiles[0].musicBand = 0;
		}

		if (IsNewTime(156.10f)) { AddPrefRow(0); Text("Performance"); }
		if (IsNewTime(156.90f)) { AddPrefRow(1); }
		if (IsNewTime(157.70f)) { AddPrefRow(2); }
		if (IsNewTime(158.50f)) { AddPrefRow(3); }

		if (IsNewTime(159.40f)) { AddPrefRow(4); perfContainer.Rotate(90f, 162.70f - 159.40f); }
		if (IsNewTime(160.20f)) { AddPrefRow(5); }
		if (IsNewTime(161.00f)) { AddPrefRow(6); }
		if (IsNewTime(161.80f)) { AddPrefRow(7); demoText.FadeOut(); }

		if (IsNewTime(162.70f))
		{
			AddPrefRow(8);
			shapeP.SetActive(false);
			float myMoveTime = 172.70f - 162.70f;
			for (int j = 0; j < 9; j++)
			{
				row[j].Move(new Vector3((GRID_X - 1) * CELL_X, 0, 0), myMoveTime);
				row[j].ChangeLayerColorAddHSV(0, new Vector3(0, -0.5f, -0.5f), myMoveTime);
			}
			perfContainer.target.Move(new Vector3((GRID_X - 1 - 2) * CELL_X, -CELL_Y, 0), myMoveTime);
			perfContainer.Rotate(90f, 166.10f - 162.70f);
			perfContainer.RescaleTo(1.8f, myMoveTime);
			AddPrefColumn(1);
		}
		if (IsNewTime(163.60f)) { AddPrefColumn(2); }
		if (IsNewTime(164.40f)) { AddPrefColumn(3); }
		if (IsNewTime(165.20f)) { AddPrefColumn(4); }

		if (IsNewTime(166.10f))
		{
			float myMoveTime = 169.40f - 166.10f;
			perfContainer.target.Move(new Vector3(-CELL_X, -CELL_Y * 1.5f, 0), myMoveTime);
			perfContainer.Rotate(90f, myMoveTime);
			AddPrefColumn(5);
		}
		if (IsNewTime(166.90f)) { AddPrefColumn(6); }
		if (IsNewTime(167.70f)) { AddPrefColumn(7); }
		if (IsNewTime(168.50f)) { AddPrefColumn(8); }

		if (IsNewTime(169.40f))
		{
			float myMoveTime = 174.80f - 169.40f;
			perfContainer.Rotate(90f, myMoveTime);
			AddPrefColumn(9);
		}
		if (IsNewTime(170.30f)) { AddPrefColumn(10); }
		if (IsNewTime(171.00f)) { AddPrefColumn(11); }
		if (IsNewTime(171.80f))
		{
			AddPrefColumn(12);
		}
		if (IsNewTime(172.70f))
		{
			float myMoveTime = 174.80f - 172.70f;
			perfContainer.target.MoveToLocal(Vector3.zero, myMoveTime); //all[7*9+4].transform.localPosition
			perfContainer.RescaleTo(1, myMoveTime);
		}


		if (IsNewTime(174.80f))
		{
			SimpleRotationScript.isGlobalEnabled = false;
			foreach (SuperShape s in all)
			{
				s.wiggleProfiles[3].volumeRatio = 20;
			}
		}

		if (IsNewTime(176.80f))
		{
			foreach (SuperShape s in all)
			{
				s.wiggleProfiles[3].volumeRatio = 1;
			}
			float myJumpTime = 0.6f;
			float myMoveTime = 0.4f;
			perfContainer.RescaleAdd(-0.2f, myJumpTime, Lerp.SinQuarter);
			perfContainer.Rotate(360f, myMoveTime + 0.1f, Lerp.Linear, myJumpTime);
			perfContainer.RescaleAdd(9.2f, myMoveTime, Lerp.SinQuarterSquared, myJumpTime + 0.1f);
			perfContainer.target = null;
			perfContainer.MoveToLocal(all[7 * 9 + 4].transform.localPosition * -10, myMoveTime, Lerp.SinQuarterSquared, myJumpTime + 0.1f);
			all[6 * 9 + 4].Move(new Vector3(-400, 0, 0), 0.4f, Lerp.Linear, myJumpTime + myMoveTime);
			all[8 * 9 + 4].Move(new Vector3( 400, 0, 0), 0.4f, Lerp.Linear, myJumpTime + myMoveTime);
			shape5 = all[7 * 9 + 4];
			shape5.name = "SuperShape 5";
			shape5.RotateTo(0, myMoveTime, Lerp.Linear, myJumpTime);
		}

		if (IsNewTime(177.0f))
		{
			shape5.SetActive(true);
			shape5.tailMaxLength = 0;
			shape5.tailExtraLength = -50f;
			shape5.tailMaxLayer = 4;
			shape5.tailMode = TailMode.Basic;
			shape5.tailTipTransform = tempTailTip;
			shape5._mesh.SetIsLimitingOverdraw(false);
		}

		if (IsNewTime(178.20f))
		{
			shape5.rectTransform.SetParent(mainDemoContainer);
			demoTextBubble.rectTransform.SetParent(shape5.transform);
			demoTextBubble.localPosition = new Vector3(0, 0, 0);
			demoTextBubble.SetOneScale();
			perfContainer.SetActive(false);
			shape5.SetOneScale();
			BubbleText("Tails", 0f, 0.1f);
			shape5.tailTipTransform = friday.mouth;
			shape5.textureCount = SuperShapeTextureCount.One;
			shape5.fillTexture1 = speechBubbleTexture;
			shape5.tailMaxLayer = 3;
			shape5.fixedUVRange = new Vector2(120, 120);
			shape5.tailBoltSegmentDistance = 100;
			shape5.TailMaxLengthAdd(1000, 0.2f);
			shape5.MoveTo(new Vector3(400, 200, 0), 0.2f);
			shape5.AddVertexAuto();
			shape5.MoveAllVertsToPreset(SuperShapePreset.Parallelogram, 0.3f);
			shape5.ResizeSliceTo(new Vector2(200, 0), 0.4f);
			shape5.wiggleProfiles[0].volumeRatio = 2;
			//shape5.Rotate(-60, 0.1f);
			friday.localPosition = new Vector3(-450,-1050,0);
			friday.Move(new Vector3(0, 800, 0), 0.1f);
		}

		if (IsNewTime(180.00f))
		{
			float myMoveTime = 0.6f;
			shape5.tailTipTransform = friday.mouth;
			friday.Move(new Vector3(0, -200, 0), myMoveTime);
			shape5.Move(new Vector3(-800, 0, 0), myMoveTime * 6, Lerp.Triangle_20Pct_Summit, myMoveTime);
			friday.Move(new Vector3(800, 0, 0), myMoveTime * 6, Lerp.Triangle_20Pct_Summit, myMoveTime);
		}

		if (IsNewTime(181.20f))
		{
			BubbleText("Automatic Mouth Tracking");
		}

		if (IsNewTime(185.00f))
		{
			shape5.Rotate(-360, 1.6f);
		}

		if (IsNewTime(187.00f))
		{
			BubbleText("Base Controls and Corner Avoidance");
			float myMoveTime = 2.0f;
			shape5.tailTipTransform = friday.mouth;
			shape5.tailBannedSideIndex = 0;
			shape5.TailBaseWidthAdd(300, myMoveTime, Lerp.Triangle_20Pct_Summit);
			shape5.CornerAvoidanceAdd(300, myMoveTime, Lerp.Triangle_20Pct_Summit, myMoveTime + 1.0f);
		}


		if (IsNewTime(192.00f))
		{
			shape5.Rotate(-25, 0.2f);
			shape5.CornerAvoidanceAdd(300, 0.2f);
			BubbleText("Bolts");
			shape5.tailMode = TailMode.BoltTaperedHalf;
		}
		if (IsNewTime(192.21f))
		{
			//shape5.isTailBaseFixed = true;
		}
		if (IsNewTime(193.35f))
		{
			shape5.tailMode = TailMode.Square;
			shape5.tailBoltSegmentDistance = 135f;
		}
		if (IsNewTime(195.00f))
		{
			BubbleText("Arrows");
			shape5.isTailArrow = true;
		}
		if (IsNewTime(196.10f))
		{
			shape5.tailMode = TailMode.Basic;
		}
		if (IsNewTime(197.00f))
		{
			shape5.tailMode = TailMode.BoltTaperedHalf;
			shape5.tailBoltSegmentDistance = 100f;
		}
		if (IsNewTime(198.90f))
		{
			BubbleText("Separate Max Length and Progress Controls");
			shape5.tailMode = TailMode.BoltTaperedHalf;
			shape5.TailMaxLengthTo(0, 1.2f, Lerp.SinHalf);
		}
		if (IsNewTime(200.20f))
		{
			shape5.TailProgressTo(0, 1.5f, Lerp.SinHalf);
		}
		if (IsNewTime(200.40f))
		{

		}
		if (IsNewTime(201.90f))
		{
			BubbleText("Tail Layer Control");
			shape5.tailMaxLayer = 4;
		}
		if (IsNewTime(202.10f))
		{
			shape5.tailMaxLayer = 3;
		}
		if (IsNewTime(202.75f))
		{
			shape5.tailMaxLayer = 2;
		}
		if (IsNewTime(203.60f))
		{
			demoTextBubble.FadeOut();
			shape5.tailMaxLayer = 3;
		}

		float fastTime = 0.1f;
		if (IsNewTime(204.5f))
		{
			shape5.RotateTo(0, 0.4f, Lerp.SinEaseInOut, 0.4f);
			shape5.TailProgressTo(0, 0.5f);
			//shape5.TailBaseWidthTo(0, 0.3f);
			friday.localScale = new Vector3(-1, 1, 1);
			friday.Move(new Vector3(0, 200, 0), 0.3f, Lerp.Hop);
			friday.Move(new Vector3(-800, -50, 0), 0.3f, Lerp.Linear, 0.4f);
			shape5.MoveTo(Vector3.zero, 0.8f, Lerp.SinEaseInOut, 0.4f);
			shape5.ResizeSliceTo(Vector2.zero, 0.8f);
			shape5.textureCount = SuperShapeTextureCount.Zero;
		}

		if (IsNewTime(205.80f))
		{
			shape5.tailMode = TailMode.NONE;
			shape5.isWigglePaused = true;
			shape5.ResetWiggle();
			shape5.MorphToPreset(SuperShapePreset.Parallelogram, fastTime);
		}
		if (IsNewTime(207.00f))
		{
			shape5.ChangeLayerColorTo(0, Color.magenta, fastTime);
			shape5.ChangeLayerColorTo(1, Color.cyan, fastTime);
			shape5.ChangeLayerColorTo(2, Color.red, fastTime);
			shape5.ChangeLayerColorTo(3, Color.blue, fastTime);
			shape5.MorphToPreset(SuperShapePreset.Rectangle, fastTime);
		}
		if (IsNewTime(208.75f))
		{
			shape5.AddLayerOverTime(fastTime);
			shape5.ChangeAllLayerColorsAddHSV(new Vector3(0.3f, 0f, 0f), fastTime);
			shape5.MorphToPreset(SuperShapePreset.Trapazoid, fastTime);
		}
		if (IsNewTime(209.20f))
		{
			shape5.AddLayerOverTime(fastTime);
			shape5.ChangeAllLayerColorsAddHSV(new Vector3(0.3f, 0f, -0.1f), fastTime);
			shape5.MorphToPreset(SuperShapePreset.Pentagon, fastTime);
		}
		if (IsNewTime(209.45f))
		{
			shape5.AddLayerOverTime(fastTime);
			shape5.ChangeAllLayerColorsAddHSV(new Vector3(0.3f, -0.4f, -0.1f), fastTime);
			shape5.MorphToPreset(SuperShapePreset.Hexagon, fastTime);
			shape5.RescaleTo(0.9f, fastTime);
		}
		if (IsNewTime(209.75f))
		{
			shape5.RemoveLayerOverTime(fastTime);
			shape5.ChangeAllLayerColorsAddHSV(new Vector3(0.3f, 0.3f, 0.2f), fastTime);
			shape5.MorphToPreset(SuperShapePreset.Chevron, fastTime);
			shape5.RescaleTo(0.8f, fastTime);
		}
		if (IsNewTime(209.90f))
		{
			shape5.RemoveLayerOverTime(fastTime);
			shape5.ChangeAllLayerColorsAddHSV(new Vector3(0.3f, 0f, 0f), fastTime);
			shape5.MorphToPreset(SuperShapePreset.Octagon, fastTime);
			shape5.RescaleTo(0.7f, fastTime);
		}
		if (IsNewTime(210.05f))
		{
			shape5.RemoveLayerOverTime(fastTime);
			shape5.ChangeAllLayerColorsAddHSV(new Vector3(0.3f, 0f, 0f), fastTime);
			shape5.MorphToPreset(SuperShapePreset.X, fastTime);
			shape5.RescaleTo(0.6f, fastTime);
		}
		if (IsNewTime(210.35f))
		{

			shape5.RemoveLayerOverTime(fastTime);
			shape5.ChangeAllLayerColorsAddHSV(new Vector3(0.3f, 0.1f, 0f), fastTime);
			shape5.ignoreVerts = new List<int>() { 1, 3, 5, 7 };
			shape5.MorphToPreset(SuperShapePreset.Rectangle, fastTime);
			shape5.RescaleTo(0.5f, fastTime);
		}
		if (IsNewTime(210.65f))
		{

			shape5.RemoveLayerOverTime(fastTime);
			shape5.ChangeAllLayerColorsAddHSV(new Vector3(0.3f, 0f, 0f), fastTime);
			shape5.MorphToPreset(SuperShapePreset.Square, fastTime);
		}
		if (IsNewTime(210.95f))
		{
			shape5.RemoveLayerOverTime(fastTime);
			shape5.ChangeLayerColorTo(0, Color.white, fastTime / 2);
			shape5.MorphToPreset(SuperShapePreset.Triangle, fastTime / 2);
		}

		if (IsNewTime(213.95f))
		{
			Cursor.visible = true;
			restartButton.Transition(1, 0.3f, Lerp.Parabola01Overshoot);
		}

		prevTime = currentTime;
	}

	void AddPrefRow(int rowIndex)
	{
		GameObject go = Instantiate(shapeP.gameObject, perfContainer.transform);
		go.transform.SetAsFirstSibling();
		SuperShape newShape = go.GetComponent<SuperShape>();
		row[rowIndex] = newShape;
		all[rowIndex] = newShape;
	}

	void AddPrefColumn(int columnIndex)
	{
		for (int rowIndex = 0; rowIndex < 9; rowIndex++)
		{
			GameObject go = Instantiate(row[rowIndex].gameObject, perfContainer.transform);
			go.transform.SetAsFirstSibling();
			SuperShape newShape = go.GetComponent<SuperShape>();
			all[columnIndex * 9 + rowIndex] = newShape;
		}
	}

	void Text(string textString, float oldTextFadeOutTime = DEFAULT_TEXT_FADE_OUT_TIME, float fadeInTime = DEFAULT_TEXT_FADE_IN_TIME,
	          Lerp lerp = Lerp.Linear)
	{
		demoText.SetText(textString, oldTextFadeOutTime, fadeInTime, lerp);
	}
	void BubbleText(string textString, float oldTextFadeOutTime = DEFAULT_TEXT_FADE_OUT_TIME, float fadeInTime = DEFAULT_TEXT_FADE_IN_TIME,
	                Lerp lerp = Lerp.Linear)
	{
		demoTextBubble.SetText(textString, oldTextFadeOutTime, fadeInTime, lerp);
	}

	bool isRestarting = false;
	public void ReloadScene()
	{
		if (isRestarting) { return; }
		StartCoroutine(ActualReloadScene());
		isRestarting = true;
	}
	private IEnumerator ActualReloadScene()
	{
		restartButton.TailMaxLengthTo(0, 0.5f);
		yield return new WaitForSeconds(0.5f);
		restartButton.Transition(2, 0.9f, Lerp.QuadJ);
		yield return new WaitForSeconds(1.0f);
		Scene scene = SceneManager.GetActiveScene();
		SceneManager.LoadScene(scene.name);
	}
}
