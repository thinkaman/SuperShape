using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class DynamicMonoBehaviour : MonoBehaviour
{
	public const float DEFUALT_MOVE_DURATION = 1f;
	private const float DEFUALT_MOVE_SPEED = 2000f;
	protected const float DEFUALT_RESCALE_DURATION = 1f;
	public const float DEFUALT_RESIZE_DURATION = 1f;

	public Transform effectiveTransform { get { return transform; } }
	public RectTransform effectiveRectTransform { get { return rectTransform; } }
	private Transform prevParent;
	public bool isOverlayActive { get { return prevParent != null; } }
	public Image effectiveImage { get { return image; } }
	public RawImage effectiveRawImage { get { return rawImage; } }
	public TMPro.TMP_Text effectiveText { get { return text; } }
	public CanvasGroup effectiveCanvasGroup { get { return canvasGroup; } }

	protected bool isForcingComplete; //when set to true, forces all transitions to complete immediately

	private bool isMenuTime { get { return false; } }
	[HideInInspector] public float personalDeltaTimeScale = 1;
	public float time { get { if (isMenuTime) return TheGameTime.menuTime; else return TheGameTime.time; } }
	public float deltaTime { get { if (isMenuTime) return TheGameTime.menuDeltaTime * personalDeltaTimeScale; else return TheGameTime.deltaTime * personalDeltaTimeScale; } }
	public float unscaledDeltaTime { get { if (isMenuTime) return TheGameTime.menuDeltaTime; else return TheGameTime.deltaTime; } }

	public static int GetMiddleValue(int a, int b, int c)
	{
		if (a >= b)
		{
			if (c >= a) { return a; }
			else if (b >= c) { return b; }
			else { return c; }
		}
		else
		{
			if (a >= c) { return a; }
			else if (c >= b) { return b; }
			else { return c; }
		}
	}
	protected bool IsInOrder(int a, int b, int c) { return a <= b && b <= c; }
	protected bool IsInOrder(float a, float b, float c) { return a <= b && b <= c; }

	private TransformDataCache _transformData;
	public TransformDataCache transformData
	{
		get
		{
			if (_transformData == null) { _transformData = GetComponent<TransformDataCache>(); }
			return _transformData;
		}
	}
	private int lastTransitionIndex = 0;

	private SuperShapeKeyframes _shapeKeyframes;
	public SuperShapeKeyframes shapeKeyframes
	{
		get
		{
			if (_shapeKeyframes == null) { _shapeKeyframes = GetComponent<SuperShapeKeyframes>(); }
			return _shapeKeyframes;
		}
	}

	private bool isNoRectTransform = false;
	private RectTransform _rectTransform;
	public RectTransform rectTransform
	{
		get
		{
			if (isNoRectTransform) { return null; }
			if (_rectTransform == null)
			{
				_rectTransform = GetComponent<RectTransform>();
				if (_rectTransform == null) { isNoRectTransform = true; }
			}
			return _rectTransform;
		}
	}
	public Vector2 anchoredPosition
	{
		get { return isNoRectTransform ? transform.localPosition : rectTransform.anchoredPosition; }
		set { if (isNoRectTransform) { transform.localPosition = value; } else { rectTransform.anchoredPosition = value; } }
	}
	public Vector2 effectiveAnchoredPosition
	{
		get { return rectTransform == null ? (Vector2)localPosition : effectiveRectTransform.anchoredPosition; }
		set
		{
			if (rectTransform == null)
			{
				localPosition = value;
			}
			else
			{
				effectiveRectTransform.anchoredPosition = value;
			}
		}
	}
	public Vector2 sizeDelta { get { return rectTransform.sizeDelta; } set { rectTransform.sizeDelta = value; } }
	public Vector2 anchoredWorldPosition
	{
		get
		{
			Vector2 pos = transform.position;
			pos -= sizeDelta * transform.lossyScale * rectTransform.pivot;
			return pos;
		}
	}

	private bool isTestedParentGLG;
	private GridLayoutGroup parentGLG;
	public float width {
		get
		{
			if (!isTestedParentGLG && transform.parent != null) { parentGLG = transform.parent.GetComponent<GridLayoutGroup>(); isTestedParentGLG = true; }
			if (parentGLG != null) { return parentGLG.cellSize.x; }
			return rectTransform.rect.width;
		}
		set
		{
			sizeDelta = new Vector2(value, sizeDelta.y);
		}
	}
	public float height
	{
		get
		{
			if (!isTestedParentGLG && transform.parent != null) { parentGLG = transform.parent.GetComponent<GridLayoutGroup>(); isTestedParentGLG = true; }
			if (parentGLG != null) { return parentGLG.cellSize.y; }
			return rectTransform.rect.height;
		}
		set
		{
			sizeDelta = new Vector2(sizeDelta.x, value);
		}
	}

	private TMPro.TMP_Text _text;
	public TMPro.TMP_Text text
	{
		get
		{
			if (_text != null) { return _text; }
			else { _text = GetComponent<TMPro.TextMeshPro>(); }
			if (_text == null) { _text = GetComponent<TMPro.TextMeshProUGUI>(); }
			return _text;
		}
	}

	private Image _image;
	public Image image
	{
		get
		{
			if (_image == null) { _image = GetComponent<Image>(); }
			return _image;
		}
	}

	private SpriteRenderer _spriteRenderer;
	public SpriteRenderer spriteRenderer
	{
		get
		{
			if (_spriteRenderer == null) { _spriteRenderer = GetComponent<SpriteRenderer>(); }
			return _spriteRenderer;
		}
	}

	[ContextMenu("Scale To Size")]
	public void ScaleToSize() { ScaleToSize(-1, -1); }
	public void ScaleToSize(float overrideX, float overrideY)
	{
		if (spriteRenderer == null) return;
		if (spriteRenderer.sprite == null) return;
		if (rectTransform == null) return;
		Vector2 parentScale = transform.parent == null ? Vector3.one : transform.parent.lossyScale;
		float w = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
		float h = spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit;
		float x = (overrideX >= 0 ? overrideX : width) / w;
		float y = (overrideY >= 0 ? overrideY : height) / h;
		localScale = new Vector3(x, y, 1);
	}
	public void ScaleToGreaterSize()
	{
		ScaleToSize();
		float m = Mathf.Max(localScale.x, localScale.y);
		localScale = new Vector3(m, m, 1);
	}

	private RawImage _rawImage;
	public RawImage rawImage
	{
		get
		{
			if (_rawImage == null) { _rawImage = GetComponent<RawImage>(); }
			return _rawImage;
		}
	}

	private Mask _mask;
	public Mask mask
	{
		get
		{
			if (_mask == null) { _mask = GetComponent<Mask>(); }
			return _mask;
		}
	}

	private SuperShape _shape;
	public SuperShape shape
	{
		get
		{
			if (_shape == null) { _shape = GetComponent<SuperShape>(); }
			return _shape;
		}
	}

	private Button _button;
	public Button button
	{
		get
		{
			if (_button == null) { _button = GetComponent<Button>(); }
			return _button;
		}
	}
	private SuperShapeButton _superShapeButton;
	public SuperShapeButton superShapeButton
	{
		get
		{
			if (_superShapeButton == null) { _superShapeButton = GetComponent<SuperShapeButton>(); }
			return _superShapeButton;
		}
	}
	private bool _interactable;
	public bool interactable {
		get
		{
			if (superShapeButton != null) { return superShapeButton.interactable; }
			else if (button != null) { return button.interactable; }
			else if (canvasGroup != null) { return canvasGroup.interactable; }
			else { return _interactable; }
		}
		set
		{
			if (superShapeButton != null) { superShapeButton.interactable = value; }
			else if (button != null) { button.interactable = value; }
			else if (canvasGroup != null) { canvasGroup.interactable = value; }
			else { _interactable = value; }
		}
	}
	private bool _blocksRaycasts;
	public bool blocksRaycasts
	{
		get
		{
			if (canvasGroup != null) { return canvasGroup.blocksRaycasts; }
			else { return _blocksRaycasts; }
		}
		set
		{
			if (canvasGroup != null) { canvasGroup.blocksRaycasts = value; }
			else { _blocksRaycasts = value; }
		}
	}

	private CanvasGroup _canvasGroup;
	public CanvasGroup canvasGroup
	{
		get
		{
			if (_canvasGroup == null) { _canvasGroup = GetComponent<CanvasGroup>(); }
			return _canvasGroup;
		}
	}

	private PaletteChanger _paletteChanger;
	public PaletteChanger paletteChanger
	{
		get
		{
			if (_paletteChanger == null) { _paletteChanger = GetComponent<PaletteChanger>(); }
			return _paletteChanger;
		}
	}

	private TintGroup _tintGroup;
	public TintGroup tintGroup
	{
		get
		{
			if (_tintGroup == null) { _tintGroup = GetComponent<TintGroup>(); }
			return _tintGroup;
		}
	}

	public Color color
	{
		get
		{
			if (tintGroup != null)
			{
				return tintGroup.color;
			}
			else if (spriteRenderer != null)
			{
				return spriteRenderer.color;
			}
			else if (text != null)
			{
				return effectiveText.color;
			}
			else if (image != null)
			{
				return effectiveImage.color;
			}
			else if (rawImage != null)
			{
				return rawImage.color;
			}
			return Color.black;
		}
		set
		{
			if (tintGroup != null)
			{
				tintGroup.color = value;
			}
			else if (spriteRenderer != null)
			{
				Color c = spriteRenderer.color;
				spriteRenderer.color = new Color(value.r, value.g, value.b, c.a);
			}
			else if (text != null)
			{
				Color c = effectiveText.color;
				effectiveText.color = new Color(value.r, value.g, value.b, c.a);
			}
			else if (image != null)
			{
				Color c = effectiveImage.color;
				effectiveImage.color = new Color(value.r, value.g, value.b, c.a);
			}
			else if (rawImage != null)
			{
				Color c = effectiveRawImage.color;
				effectiveRawImage.color = new Color(value.r, value.g, value.b, c.a);
			}
		}
	}

	public float alpha
	{
		get
		{
			if (tintGroup != null)
			{
				return tintGroup.alpha;
			}
			else if (spriteRenderer != null)
			{
				return spriteRenderer.color.a;
			}
			else if (canvasGroup != null)
			{
				return effectiveCanvasGroup.alpha;
			}
			else if (text != null)
			{
				return effectiveText.color.a;
			}
			else if (image != null)
			{
				return effectiveImage.color.a;
			}
			else if (rawImage != null)
			{
				return rawImage.color.a;
			}
			return 1;
		}
		set
		{
			if (tintGroup != null)
			{
				tintGroup.alpha = value;
			}
			else if (spriteRenderer != null)
			{
				Color c = spriteRenderer.color;
				spriteRenderer.color = new Color(c.r, c.g, c.b, value);
			}
			else if (canvasGroup != null)
			{
				effectiveCanvasGroup.alpha = value;
			}
			else if (text != null)
			{
				Color c = effectiveText.color;
				effectiveText.color = new Color(c.r, c.g, c.b, value);
			}
			else if (image != null)
			{
				Color c = effectiveImage.color;
				effectiveImage.color = new Color(c.r, c.g, c.b, value);
			}
			else if (rawImage != null)
			{
				Color c = effectiveRawImage.color;
				effectiveRawImage.color = new Color(c.r, c.g, c.b, value);
			}
		}
	}

	private UnityEngine.Rendering.SortingGroup _sortingGroup;
	public UnityEngine.Rendering.SortingGroup sortingGroup
	{
		get
		{
			if (_sortingGroup == null) { _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>(); }
			return _sortingGroup;
		}
	}
	public int sortingLayerID {
		get { return sortingGroup != null ? sortingGroup.sortingLayerID : spriteRenderer.sortingLayerID; }
		set { if (sortingGroup != null) { sortingGroup.sortingLayerID = value; }
		      else                      { spriteRenderer.sortingLayerID = value; } }
	}
	public int sortingOrder {
		get { return sortingGroup != null ? sortingGroup.sortingOrder : spriteRenderer.sortingOrder; }
		set { if (sortingGroup != null) { sortingGroup.sortingOrder = value; }
		      else                      { spriteRenderer.sortingOrder = value; } }
	}
	private DebugKeyMethodCaller dmce { get { return GetComponent<DebugKeyMethodCaller>(); } }

	public Vector3 testVector
	{
		get { if (dmce != null) { return dmce.testVectors[0]; }
			else { return Vector3.zero; } }
	}
	public Vector3 GetTestVector(int index)
	{
		if (dmce != null) { return dmce.testVectors[index]; }
		else { return Vector3.zero; }
	}
	public Lerp testLerp
	{
		get { if (dmce != null) { return dmce.testLerps[0]; }
			else { return Lerp.Linear; } }
	}
	public Lerp GetTestLerp(int index)
	{
		if (dmce != null) { return dmce.testLerps[index]; }
		else { return Lerp.Linear; }
	}
	public float testDuration
	{
		get { if (dmce != null) { return dmce.testDurations[0]; }
			else { return 1.0f; } }
	}
	public float GetTestDuration(int index)
	{
		if (dmce != null) { return dmce.testDurations[index]; }
		else { return 1.0f; }
	}
	public float testDelay
	{
		get { if (dmce != null) { return dmce.testDelays[0]; }
			else { return 1.0f; } }
	}
	public float GetTestDelay(int index)
	{
		if (dmce != null) { return dmce.testDelays[index]; }
		else { return 1.0f; }
	}

	protected float random { get { return UnityEngine.Random.value; } }
	protected int Rand(int max) { return UnityEngine.Random.Range(0, max); }
	protected int Rand(int min, int max) { return UnityEngine.Random.Range(min, max); }
	protected float Rand(float max) { return UnityEngine.Random.Range(0, max); }
	protected float Rand(float min, float max) { return UnityEngine.Random.Range(min, max); }
	protected float randomExtreme { get { return random > 0.5f ? UnityEngine.Random.Range(-1.0f, -0.4f) : UnityEngine.Random.Range(0.4f, 1.0f); } }

	public void SetToActive() { SetActive(true); }
	public void SetToInactive() { SetActive(false); }
	public void SetActive(bool isActive) { gameObject.SetActive(isActive); }
	public Transform parent { get { return transform.parent; } set { transform.SetParent(value); } }
	public T[] Children<T>() { return GetComponentsInChildren<T>(true); }
	public int childCount { get { return transform.childCount; } }
	public Transform GetChild(int i) { return transform.GetChild(i); }
	public void SetAsFirstSibling() { transform.SetAsFirstSibling(); }
	public void SetAsLastSibling() { transform.SetAsLastSibling(); }
	public int GetSiblingIndex() { return transform.GetSiblingIndex(); }

	public float screenScale { get { return transform.root.localScale.x; } }
	public Vector3 position { get { return effectiveTransform.position; } set { effectiveTransform.position = value; } }
	public Vector3 localPosition { get { return effectiveTransform.localPosition; } set { effectiveTransform.localPosition = value; } }
	public void SetX(float newX)
	{
		localPosition = new Vector3(newX, localPosition.y, localPosition.z);
	}
	public void SetY(float newY)
	{
		localPosition = new Vector3(localPosition.x, newY, localPosition.z);
	}
	public void SetZ(float newZ)
	{
		localPosition = new Vector3(localPosition.x, localPosition.y, newZ);
	}
	public Vector3 normalizedPosition { get { return effectiveTransform.position / screenScale; } set { effectiveTransform.position = value * screenScale; } }
	[HideInInspector] public Vector3 totalPendingMoveDelta; //does NOT account for rotation or scale changes!
	public Quaternion rotation { get { return effectiveTransform.rotation; } set { effectiveTransform.rotation = value; } }
	public Quaternion localRotation { get { return effectiveTransform.localRotation; } set { effectiveTransform.localRotation = value; } }
	public Vector3 eulerAngles { get { return effectiveTransform.eulerAngles; } set { effectiveTransform.eulerAngles = value; } }
	public Vector3 localEulerAngles { get { return effectiveTransform.localEulerAngles; } set { effectiveTransform.localEulerAngles = value; } }
	public Vector3 localScale { get { return effectiveTransform.localScale; } set { effectiveTransform.localScale = value; } }
	public void Rotate(Vector3 eulerAngles) { effectiveTransform.Rotate(eulerAngles); }

	public void FlipX() { Rotate(new Vector3(180, 0, 0)); }
	public void FlipY() { Rotate(new Vector3(0, 180, 0)); }
	public void FlipZ() { Rotate(new Vector3(0, 0, 180)); }

	public void SetLocalEulerAngleX(float x) { localEulerAngles = new Vector3(x, localEulerAngles.y, localEulerAngles.z); }
	public void SetLocalEulerAngleY(float y) { localEulerAngles = new Vector3(localEulerAngles.x, y, localEulerAngles.z); }
	public void SetLocalEulerAngleZ(float z) { localEulerAngles = new Vector3(localEulerAngles.x, localEulerAngles.y, z); }

	[HideInInspector]
	public List<DynamicMonoBehaviour> movementTriggerTargetList = new List<DynamicMonoBehaviour>();
	[HideInInspector]
	public bool isMovementTriggered = false;
	public virtual void MovementTrigger(DynamicMonoBehaviour cause) { isMovementTriggered = true; }

	public void SetZero()
	{
		SetZeroLocalPosition();
		SetZeroRotation();
	}
	public void SetZeroLocalPosition() { localPosition = Vector3.zero; }
	public void SetZeroRotation() { rotation = Quaternion.Euler(Vector3.zero); }
	public void SetOneScale() { localScale = Vector3.one; }

	public void Move(Vector3 delta,
					 float duration,
					 Lerp lerpCurve,
					 float delay,
					 Action<object[]> callback,
					 params object[] args)
	{
		Move(delta, duration, lerpCurve, delay, 1, callback, args);
	}
	public void Move(Vector3 delta,
					 float duration = DEFUALT_MOVE_DURATION,
					 Lerp lerpCurve = Lerp.Linear,
					 float delay = 0,
					 int playCount = 1,
					 Action<object[]> callback = null,
					 params object[] args)
	{
		SetActive(true);
		/*
		if (overlayCloneTransform != null)
		{
			if (transform.parent.rotation != overlayCloneTransform.parent.rotation)
			{
				float angleDeg = transform.parent.rotation.eulerAngles.z - overlayCloneTransform.parent.rotation.z;
				float sin = Mathf.Sin(angleDeg * Mathf.Deg2Rad);
				float cos = Mathf.Cos(angleDeg * Mathf.Deg2Rad);
				delta = new Vector3(cos * delta.x - sin * delta.y, sin * delta.x + cos * delta.y, 0);
			}
		}
		*/

		StartCoroutine(MoveCoroutine(null, delta, duration, lerpCurve, delay, playCount, moveToMutexCounter, callback, args));
	}
	private int moveToMutexCounter = 0;
	public void MoveTo(Vector3 endPos,
					   float duration = DEFUALT_MOVE_DURATION,
					   Lerp lerpCurve = Lerp.Linear,
					   float delay = 0,
					   Action<object[]> callback = null,
					   params object[] args)
	{
		moveToMutexCounter++;
		totalPendingMoveDelta = Vector2.zero;
		SetActive(true);
		StartCoroutine(MoveCoroutine(null, endPos - normalizedPosition, duration, lerpCurve, delay, 1, moveToMutexCounter, callback, args));
	}
	public void MoveToLocal(Vector3 endPos,
							float duration = DEFUALT_MOVE_DURATION,
							Lerp lerpCurve = Lerp.Linear,
							float delay = 0,
							int playCount = 1,
							Action<object[]> callback = null,
							params object[] args)
	{
		moveToMutexCounter++;
		totalPendingMoveDelta = Vector2.zero;
		SetActive(true);
		StartCoroutine(MoveCoroutine(null, (endPos - (Vector3)effectiveAnchoredPosition), duration, lerpCurve, delay, playCount, moveToMutexCounter, callback, args));
	}
	public void MoveToTransform(Transform target,
								float duration = DEFUALT_MOVE_DURATION,
								Lerp lerpCurve = Lerp.Linear,
								float delay = 0,
								Action<object[]> callback = null,
								params object[] args)
	{
		moveToMutexCounter++;
		totalPendingMoveDelta = Vector2.zero;
		SetActive(true);
		StartCoroutine(MoveCoroutine(target, Vector3.zero, duration, lerpCurve, delay, 1, moveToMutexCounter, callback, args));
	}
	public void CancelPendingMoveTo()
	{
		moveToMutexCounter++;
		totalPendingMoveDelta = Vector2.zero;
	}
	private IEnumerator MoveCoroutine(Transform parentTransform,
									  Vector3 delta,
									  float duration = DEFUALT_MOVE_DURATION,
									  Lerp lerpCurve = Lerp.Linear,
									  float delay = 0,
									  int playCount = 1,
									  int moveToMutexRequirement = -1,
									  Action<object[]> callback = null,
									  params object[] args)
	{
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (parentTransform != null) { delta = parentTransform.position - normalizedPosition; }
		if (duration <= 0)
		{
			effectiveAnchoredPosition += (Vector2)delta;
			//instant moves do not check against movement triggers!
		}
		else
		{
			totalPendingMoveDelta += delta;
			float currentTime = 0;
			while (currentTime < duration)
			{
				if (moveToMutexRequirement != -1 && moveToMutexRequirement != moveToMutexCounter)
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
				Vector3 oldPos = effectiveTransform.position;
				effectiveAnchoredPosition += relativeTimeDelta * (Vector2)delta;
				if (delta.z != 0) { SetZ(localPosition.z + relativeTimeDelta * delta.z); }
				totalPendingMoveDelta -= relativeTimeDelta * delta;
				currentTime = newTime;
				for (int i = movementTriggerTargetList.Count - 1; i > -1; i--)
				{
					DynamicMonoBehaviour target = movementTriggerTargetList[i];
					Vector3 triggerDeltaPre = target.position - oldPos;
					Vector3 triggerDeltaPost = target.position - effectiveTransform.position;
					if (triggerDeltaPre.x * triggerDeltaPost.x < 0 || triggerDeltaPre.y * triggerDeltaPost.y < 0)
					{
						movementTriggerTargetList.RemoveAt(i);
						target.MovementTrigger(this);
					}
				}
				if (currentTime < duration)
				yield return null;
			}
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	public void Rotate(float zDelta,
					   float duration,
					   Lerp lerpCurve = Lerp.Linear,
					   float delay = 0,
					   int playCount = 1,
					   Action<object[]> callback = null,
					   params object[] args)
	{
		SetActive(true);
		StartCoroutine(RotateCoroutine(new Vector3(0, 0, zDelta), duration, lerpCurve, delay, playCount, rotateToMutexCounter, callback, args));
	}

	public void Rotate(Vector3 eulerDelta,
					   float duration,
					   Lerp lerpCurve = Lerp.Linear,
					   float delay = 0,
					   int playCount = 1,
					   Action<object[]> callback = null,
					   params object[] args)
	{
		SetActive(true);
		StartCoroutine(RotateCoroutine(eulerDelta, duration, lerpCurve, delay, playCount, rotateToMutexCounter, callback, args));
	}
	private int rotateToMutexCounter = 0;
	public void RotateTo(float zDelta,
						 float duration,
						 Lerp lerpCurve = Lerp.Linear,
						 float delay = 0,
						 Action<object[]> callback = null,
						 params object[] args)
	{
		rotateToMutexCounter++;
		SetActive(true);
		StartCoroutine(RotateCoroutine(new Vector3(0, 0, zDelta - effectiveTransform.eulerAngles.z), duration, lerpCurve, delay, 1, rotateToMutexCounter, callback, args));
	}
	public void RotateTo(Vector3 endRot,
						 float duration,
						 Lerp lerpCurve = Lerp.Linear,
						 float delay = 0,
						 Action<object[]> callback = null,
						 params object[] args)
	{
		rotateToMutexCounter++;
		SetActive(true);
		StartCoroutine(RotateCoroutine(endRot - effectiveTransform.eulerAngles, duration, lerpCurve, delay, 1, rotateToMutexCounter, callback, args));
	}
	public void RotateToLocal(Vector3 endRot,
							  float duration,
							  Lerp lerpCurve = Lerp.Linear,
							  float delay = 0,
							  int playCount = 1,
							  Action<object[]> callback = null,
							  params object[] args)
	{
		rotateToMutexCounter++;
		SetActive(true);
		Vector3 adjustedEulerAngles = effectiveTransform.localEulerAngles;
		while (adjustedEulerAngles.x > 180) { adjustedEulerAngles.x -= 360; }
		while (adjustedEulerAngles.y > 180) { adjustedEulerAngles.y -= 360; }
		while (adjustedEulerAngles.z > 180) { adjustedEulerAngles.z -= 360; }
		StartCoroutine(RotateCoroutine(endRot - adjustedEulerAngles, duration, lerpCurve, delay, playCount, rotateToMutexCounter, callback, args));
	}
	private IEnumerator RotateCoroutine(Vector3 eulerDelta,
										float duration,
										Lerp lerpCurve = Lerp.Linear,
										float delay = 0,
										int playCount = 1,
										float rotateToMutexRequirement = -1,
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
			effectiveTransform.Rotate(eulerDelta);
		}
		else
		{
			float currentTime = 0;
			while (currentTime < duration && (rotateToMutexRequirement == -1 || rotateToMutexRequirement == rotateToMutexCounter))
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
				effectiveTransform.Rotate(relativeTimeDelta * eulerDelta);
				currentTime = newTime;
				yield return null;
			}
		}
		yield return null;
		if (callback != null) { callback(args); }
	}

	public void RotateAroundTransform(Vector3 centerOfRotation,
									  Vector3 axisOfRotation,
									  float angleDeg,
									  float duration,
									  Lerp lerpCurve = Lerp.Linear,
									  float delay = 0,
									  int playCount = 1,
									  Action<object[]> callback = null,
									  params object[] args)
	{
		StartCoroutine(RotateAroundTransformCoroutine(centerOfRotation, axisOfRotation, angleDeg, duration, lerpCurve, delay, playCount, moveToMutexCounter, rotateToMutexCounter, callback, args));
	}

	private IEnumerator RotateAroundTransformCoroutine(Vector3 centerOfRotation,
													   Vector3 axisOfRotation,
													   float angleDeg,
													   float duration,
													   Lerp lerpCurve = Lerp.Linear,
													   float delay = 0,
													   int playCount = 1,
													   float moveToMutexRequirement = -1,
													   float rotateToMutexRequirement = -1,
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
			effectiveTransform.RotateAround(centerOfRotation, axisOfRotation, angleDeg);
		}
		else
		{
			float currentTime = 0;
			while (currentTime < duration && (rotateToMutexRequirement == -1 || rotateToMutexRequirement == rotateToMutexCounter))
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
				effectiveTransform.RotateAround(centerOfRotation, axisOfRotation, relativeTimeDelta * angleDeg);

				currentTime = newTime;
				if (currentTime < duration)
				yield return null;
			}
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	public void RescaleAdd(float delta,
						   float duration = DEFUALT_RESCALE_DURATION,
						   Lerp lerpCurve = Lerp.Linear,
						   float delay = 0,
						   int playCount = 1,
						   Action<object[]> callback = null,
						   params object[] args)
	{
		SetActive(true);
		StartCoroutine(RescaleAddCoroutine(new Vector3(delta, delta, delta), duration, lerpCurve, delay, playCount, rescaleToMutexCounter, callback, args));
	}

	public void RescaleAdd(Vector3 delta,
						   float duration = DEFUALT_RESCALE_DURATION,
						   Lerp lerpCurve = Lerp.Linear,
						   float delay = 0,
						   int playCount = 1,
						   Action<object[]> callback = null,
						   params object[] args)
	{
		SetActive(true);
		StartCoroutine(RescaleAddCoroutine(delta, duration, lerpCurve, delay, playCount, rescaleToMutexCounter, callback, args));
	}
	private int rescaleToMutexCounter;
	public void RescaleTo(float endScale,
						  float duration = DEFUALT_RESCALE_DURATION,
						  Lerp lerpCurve = Lerp.Linear,
						  float delay = 0,
						  Action<object[]> callback = null,
						  params object[] args)
	{
		rescaleToMutexCounter++;
		SetActive(true);
		StartCoroutine(RescaleAddCoroutine(new Vector3(endScale, endScale, endScale) - effectiveTransform.localScale, duration, lerpCurve, delay, 1, rescaleToMutexCounter, callback, args));
	}
	public void RescaleTo(Vector3 endScale,
						  float duration = DEFUALT_RESCALE_DURATION,
						  Lerp lerpCurve = Lerp.Linear,
						  float delay = 0,
						  Action<object[]> callback = null,
						  params object[] args)
	{
		rescaleToMutexCounter++;
		SetActive(true);
		StartCoroutine(RescaleAddCoroutine(endScale - effectiveTransform.localScale, duration, lerpCurve, delay, 1, rescaleToMutexCounter, callback, args));
	}
	private IEnumerator RescaleAddCoroutine(Vector3 delta,
											float duration = DEFUALT_RESCALE_DURATION,
											Lerp lerpCurve = Lerp.Linear,
											float delay = 0,
											int playCount = 1,
											int rescaleToMutexRequirement = -1,
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
			effectiveTransform.localScale += delta;
		}
		else
		{
			float currentTime = 0;
			while (currentTime < duration && (rescaleToMutexRequirement == -1 || rescaleToMutexRequirement == rescaleToMutexCounter))
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
				effectiveTransform.localScale += relativeTimeDelta * delta;
				currentTime = newTime;
				if (currentTime < duration)
					yield return null;
			}
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	public void RescaleMult(float multiplier,
							float duration = DEFUALT_RESIZE_DURATION,
							Lerp lerpCurve = Lerp.Linear,
							float delay = 0,
							int playCount = 1,
							Action<object[]> callback = null,
							params object[] args)
	{
		SetActive(true);
		StartCoroutine(RescaleMultCoroutine(multiplier, duration, lerpCurve, delay, playCount, rescaleToMutexCounter, callback, args));
	}
	private IEnumerator RescaleMultCoroutine(float multiplier,
											 float duration = DEFUALT_RESCALE_DURATION,
											 Lerp lerpCurve = Lerp.Linear,
											 float delay = 0,
											 int playCount = 1,
											 int rescaleToMutexRequirement = -1,
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
			effectiveTransform.localScale *= multiplier;
		}
		else
		{
			float currentTime = 0;
			Vector3 originalScale = effectiveTransform.localScale;
			while (currentTime < duration && (rescaleToMutexRequirement == -1 || rescaleToMutexRequirement == rescaleToMutexCounter))
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
				effectiveTransform.localScale = L.erp(originalScale, originalScale * multiplier, newTime / duration, lerpCurve);
				currentTime = newTime;
				if (currentTime < duration)
				yield return null;
			}
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	public void ResizeAdd(Vector2 delta,
						  float duration = DEFUALT_RESIZE_DURATION,
						  Lerp lerpCurve = Lerp.Linear,
						  float delay = 0,
						  int playCount = 1,
						  Action<object[]> callback = null,
						  params object[] args)
	{
		SetActive(true);
		StartCoroutine(ResizeAddCoroutine(delta, duration, lerpCurve, delay, playCount, rescaleToMutexCounter, callback, args));
	}
	private int resizeToMutexCounter;
	public void ResizeTo(Vector2 endDelta,
						 float duration = DEFUALT_RESIZE_DURATION,
						 Lerp lerpCurve = Lerp.Linear,
						 float delay = 0,
						 Action<object[]> callback = null,
						 params object[] args)
	{
		rescaleToMutexCounter++;
		SetActive(true);
		StartCoroutine(ResizeAddCoroutine(endDelta - sizeDelta, duration, lerpCurve, delay, 1, rescaleToMutexCounter, callback, args));
	}
	private IEnumerator ResizeAddCoroutine(Vector2 delta,
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
			sizeDelta += delta;
			if (sizeDelta.x < 0) { sizeDelta = new Vector2(0, sizeDelta.y); }
			if (sizeDelta.y < 0) { sizeDelta = new Vector2(sizeDelta.x, 0); }
		}
		else
		{
			float currentTime = 0;
			while (currentTime < duration && (resizeToMutexRequirement == -1 || resizeToMutexRequirement == rescaleToMutexCounter))
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
				sizeDelta += relativeTimeDelta * delta;
				if (sizeDelta.x < 0) { sizeDelta = new Vector2(0, sizeDelta.y); }
				if (sizeDelta.y < 0) { sizeDelta = new Vector2(sizeDelta.x, 0); }
				currentTime = newTime;
				if (currentTime < duration) yield return null;
			}
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	public void ResizeMult(float multiplier,
						   float duration = DEFUALT_RESIZE_DURATION,
						   Lerp lerpCurve = Lerp.Linear,
						   float delay = 0,
						   int playCount = 1,
						   Action<object[]> callback = null,
						   params object[] args)
	{
		SetActive(true);
		StartCoroutine(ResizeMultCoroutine(multiplier, duration, lerpCurve, delay, playCount, rescaleToMutexCounter, callback, args));
	}
	private IEnumerator ResizeMultCoroutine(float multiplier,
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
			sizeDelta *= multiplier;
		}
		else
		{
			float currentTime = 0;
			Vector2 originalSize = sizeDelta;
			while (currentTime < duration && (resizeToMutexRequirement == -1 || resizeToMutexRequirement == rescaleToMutexCounter))
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
				sizeDelta = L.erp(originalSize, originalSize * multiplier, newTime / duration, lerpCurve);
				currentTime = newTime;

				if (currentTime < duration)
					yield return null;
			}
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	public void Fade(float delta,
					 float duration = DEFUALT_MOVE_DURATION,
					 Lerp lerpCurve = Lerp.Linear,
					 float delay = 0,
					 Action<object[]> callback = null,
					 params object[] args)
	{
		SetActive(true);
		StartCoroutine(FadeCoroutine(delta, duration, lerpCurve, delay, fadeToMutexCounter, callback, args));
	}

	private int fadeToMutexCounter = 0;
	public void FadeTo(float targetAlpha,
					   float duration = DEFUALT_MOVE_DURATION,
					   Lerp lerpCurve = Lerp.Linear,
					   float delay = 0,
					   Action<object[]> callback = null,
					   params object[] args)
	{
		fadeToMutexCounter++;
		SetActive(true);
		StartCoroutine(FadeCoroutine(targetAlpha - alpha, duration, lerpCurve, delay, fadeToMutexCounter, callback, args));
	}

	public void FadeIn(float duration = DEFUALT_MOVE_DURATION,
					   Lerp lerpCurve = Lerp.Linear,
					   float delay = 0,
					   Action<object[]> callback = null,
					   params object[] args)
	{
		SetActive(true);
		FadeTo(1, duration, lerpCurve, delay, callback, args);
	}

	public void FadeOut(float duration = DEFUALT_MOVE_DURATION,
						Lerp lerpCurve = Lerp.Linear,
						float delay = 0,
						Action<object[]> callback = null,
						params object[] args)
	{
		SetActive(true);
		FadeTo(0, duration, lerpCurve, delay, callback, args);
	}

	private IEnumerator FadeCoroutine(float delta,
									  float duration = DEFUALT_RESIZE_DURATION,
									  Lerp lerpCurve = Lerp.Linear,
									  float delay = 0,
									  int fadeToMutexRequirement = -1,
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
			alpha += delta;
		}
		else
		{
			float currentTime = 0;
			while (currentTime < duration && (fadeToMutexRequirement == -1 || fadeToMutexRequirement == fadeToMutexCounter))
			{
				float newTime = isForcingComplete ? duration : Mathf.Clamp(currentTime + deltaTime, 0, duration);
				float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
				alpha += relativeTimeDelta * delta;
				currentTime = newTime;

				if (currentTime < duration)
					yield return null;
			}
		}

		if (alpha < 0.001f) { alpha = 0; }
		if (alpha > 0.999f) { alpha = 1; }
		//yield return null;
		if (callback != null) { callback(args); }
	}


	public void ChangeColorAdd(Vector3 targetColorRGBDelta,
							   float duration = DEFUALT_MOVE_DURATION,
							   Lerp lerpCurve = Lerp.Linear,
							   float delay = 0,
							   Action<object[]> callback = null,
							   params object[] args)
	{
		SetActive(true);
		StartCoroutine(ColorCoroutine(effectiveImage.color + new Color(targetColorRGBDelta.x, targetColorRGBDelta.y, targetColorRGBDelta.z, effectiveImage.color.a), duration, lerpCurve, delay, colorMutexCounter, callback, args));
	}
	public void ChangeColorAdd(Color targetColor,
							   float duration = DEFUALT_MOVE_DURATION,
							   Lerp lerpCurve = Lerp.Linear,
							   float delay = 0,
							   Action<object[]> callback = null,
							   params object[] args)
	{
		SetActive(true);
		StartCoroutine(ColorCoroutine(targetColor + color, duration, lerpCurve, delay, colorMutexCounter, callback, args));
	}

	private int colorMutexCounter = 0;
	public void ChangeColorTo(Vector3 targetColorRGB,
							  float duration = DEFUALT_MOVE_DURATION,
							  Lerp lerpCurve = Lerp.Linear,
							  float delay = 0,
							  Action<object[]> callback = null,
							  params object[] args)
	{
		colorMutexCounter++;
		SetActive(true);
		StartCoroutine(ColorCoroutine(new Color(targetColorRGB.x, targetColorRGB.y, targetColorRGB.z, color.a), duration, lerpCurve, delay, colorMutexCounter, callback, args));
	}
	public void ChangeColorTo(Color targetColor,
							  float duration = DEFUALT_MOVE_DURATION,
							  Lerp lerpCurve = Lerp.Linear,
							  float delay = 0,
							  Action<object[]> callback = null,
							  params object[] args)
	{
		colorMutexCounter++;
		SetActive(true);
		StartCoroutine(ColorCoroutine(targetColor, duration, lerpCurve, delay, colorMutexCounter, callback, args));
	}

	private IEnumerator ColorCoroutine(Color targetColor,
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
			color = targetColor;
		}
		else
		{
			float currentTime = 0;
			Color originalColor = color;
			while (currentTime < duration && (colorMutexRequirement == -1 || colorMutexRequirement == colorMutexCounter))
			{
				float newTime = isForcingComplete ? duration : Mathf.Clamp(currentTime + deltaTime, 0, duration);
				color = Color.Lerp(originalColor, targetColor, L.erp(newTime / duration, lerpCurve));
				currentTime = newTime;

				if (currentTime < duration) yield return null;
			}
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	public void ChangeColorHSVAdd(Vector3 targetColorHSVDelta,
								 float duration = DEFUALT_MOVE_DURATION,
								 Lerp lerpCurve = Lerp.Linear,
								 float delay = 0,
								 Action<object[]> callback = null,
								 params object[] args)
	{
		SetActive(true);
		Color.RGBToHSV(effectiveImage.color, out float h, out float s, out float v);
		StartCoroutine(ColorHSVCoroutine(targetColorHSVDelta + new Vector3(h, s, v), duration, lerpCurve, delay, colorMutexCounter, callback, args));
	}
	public void ChangeColorHSVTo(Vector3 targetColorHSV,
								 float duration = DEFUALT_MOVE_DURATION,
								 Lerp lerpCurve = Lerp.Linear,
								 float delay = 0,
								 Action<object[]> callback = null,
								 params object[] args)
	{
		colorMutexCounter++;
		SetActive(true);
		Color.RGBToHSV(effectiveImage.color, out float h, out float s, out float v);
		if (targetColorHSV.x <= -99) { targetColorHSV.x = h; }
		if (targetColorHSV.y <= -99) { targetColorHSV.y = s; }
		if (targetColorHSV.z <= -99) { targetColorHSV.z = v; }
		StartCoroutine(ColorHSVCoroutine(targetColorHSV, duration, lerpCurve, delay, colorMutexCounter, callback, args));
	}
	private IEnumerator ColorHSVCoroutine(Vector3 targetColorHSV,
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
			color = Color.HSVToRGB((targetColorHSV.x % 1.0f + 1) % 1.0f, Mathf.Clamp01(targetColorHSV.y), Mathf.Clamp01(targetColorHSV.z));
		}
		else
		{
			float currentTime = 0;
			Color.RGBToHSV(color, out float h, out float s, out float v);
			Vector3 originalColorHSV = new Vector3(h, s, v);
			while (currentTime < duration && (colorMutexRequirement == -1 || colorMutexRequirement == colorMutexCounter))
			{
				float newTime = isForcingComplete ? duration : Mathf.Clamp(currentTime + deltaTime, 0, duration);
				Vector3 hsv = L.erp(originalColorHSV, targetColorHSV, newTime / duration, lerpCurve);
				color = Color.HSVToRGB((hsv.x % 1.0f + 1) % 1.0f, Mathf.Clamp01(hsv.y), Mathf.Clamp01(hsv.z));
				currentTime = newTime;

				if (currentTime < duration) yield return null;
			}
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	private int timeScaleMutexCounter = 0;
	public void ChangeTimeScaleAdd(float timeScaleDelta,
								   float duration = DEFUALT_MOVE_DURATION,
								   Lerp lerpCurve = Lerp.Linear,
								   float delay = 0,
								   Action<object[]> callback = null,
								   params object[] args)
	{
		SetActive(true);
		StartCoroutine(TimeScaleCoroutine(timeScaleDelta, duration, lerpCurve, delay, timeScaleMutexCounter, callback, args));
	}
	public void ChangeTimeScaleTo(float targetTimeScale,
								  float duration = DEFUALT_MOVE_DURATION,
								  Lerp lerpCurve = Lerp.Linear,
								  float delay = 0,
								  Action<object[]> callback = null,
								  params object[] args)
	{
		timeScaleMutexCounter++;
		SetActive(true);
		StartCoroutine(TimeScaleCoroutine(targetTimeScale - personalDeltaTimeScale, duration, lerpCurve, delay, timeScaleMutexCounter, callback, args));
	}
	private IEnumerator TimeScaleCoroutine(float delta,
										   float duration = DEFUALT_RESIZE_DURATION,
										   Lerp lerpCurve = Lerp.Linear,
										   float delay = 0,
										   int timeScaleMutexRequirement = -1,
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
			personalDeltaTimeScale += delta;
			yield break;
		}
		float currentTime = 0;
		while (currentTime < duration)
		{
			if (timeScaleMutexRequirement != -1 && timeScaleMutexRequirement != timeScaleMutexCounter)
			{
				break;
			}
			float newTime = isForcingComplete ? duration : currentTime + unscaledDeltaTime;
			if (newTime >= duration) { newTime = duration; }
			float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
			personalDeltaTimeScale += relativeTimeDelta * delta;
			currentTime = newTime;

			if (currentTime < duration) yield return null;
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	public virtual void Transition(int dataIndex = 0,
								   float duration = DEFUALT_MOVE_DURATION,
								   Lerp lerpCurve = Lerp.Linear,
								   float delay = 0,
								   Call callback = null,
								   params object[] args)
	{
		if (localPosition != transformData.data[dataIndex].localPosition)
		{
			MoveToLocal(transformData.data[dataIndex].localPosition, duration, lerpCurve, delay, 1, callback == null ? null : callback.backs, args);
		}
		if (localRotation.eulerAngles != transformData.data[dataIndex].localRotationEuler)
		{
			RotateToLocal(transformData.data[dataIndex].localRotationEuler, duration, lerpCurve, delay, 1, callback == null ? null : callback.backs, args);
		}
		if (localScale != transformData.data[dataIndex].localScale)
		{
			RescaleTo(transformData.data[dataIndex].localScale, duration, lerpCurve, delay, callback == null ? null : callback.backs, args);
		}
		if (alpha != transformData.data[dataIndex].alpha)
		{
			FadeTo(transformData.data[dataIndex].alpha, duration, lerpCurve, delay, callback == null ? null : callback.backs, args);
		}
	}

	public void SlideIn(float slideDuration, float delay = 0, Call call = null)
	{
		SetActive(true);
		Transition(1, slideDuration, Lerp.Linear, delay + 0.05f, call);
	}

	public void SlideOut(float slideDuration, bool isDisabling = false, float delay = 0, Call call = null)
	{
		Transition(0, slideDuration, Lerp.Linear, delay, isDisabling ? new Call(Disable) : call);
	}


	public void TextTween(float startingValue,
						  float targetValue,
						  bool isRandomNoise,
						  string format,
						  float duration,
						  Lerp lerpCurve = Lerp.Linear,
						  float delay = 0,
						  DynamicMonoBehaviour sisterText = null,
						  Action<object[]> callback = null,
						  params object[] args)
	{
		SetActive(true);
		StartCoroutine(TextTweenCoroutine(startingValue, targetValue, isRandomNoise, format,
		               duration, lerpCurve, delay, sisterText, callback, args));
	}

	private IEnumerator TextTweenCoroutine(float startingValue,
										   float targetValue,
										   bool isRandomNoise,
										   string format = null,
										   float duration = DEFUALT_RESIZE_DURATION,
										   Lerp lerpCurve = Lerp.Linear,
										   float delay = 0,
										   DynamicMonoBehaviour sisterText = null,
										   Action<object[]> callback = null,
										   params object[] args)
	{
		if (format == null) { format = "0"; }
		float delayTimer = delay;
		while (delayTimer > 0)
		{
			delayTimer = isForcingComplete ? 0 : delayTimer - deltaTime;
			yield return null;
		}
		if (duration <= 0)
		{
			text.text = startingValue.ToString(format);
			if (sisterText != null) { sisterText.text.text = text.text; }
		}
		else
		{
			float currentTime = 0;
			while (currentTime < duration)
			{
				float newTime = isForcingComplete ? duration : Mathf.Clamp(currentTime + deltaTime, 0, duration);
				float myNumber = L.erp(startingValue, targetValue, newTime / duration, lerpCurve);
				if (isRandomNoise)
				{
					//TODO
				}
				text.text = myNumber.ToString(format);
				if (sisterText != null) { sisterText.text.text = text.text; }
				currentTime = newTime;

				if (currentTime < duration) yield return null;
			}
		}
		//yield return null;
		if (callback != null) { callback(args); }
	}

	public virtual void CancelAllCoroutines()
	{
		totalPendingMoveDelta = Vector2.zero;
		moveToMutexCounter++;
		rotateToMutexCounter++;
		rescaleToMutexCounter++;
		resizeToMutexCounter++;
		fadeToMutexCounter++;
		colorMutexCounter++;
		timeScaleMutexCounter++;
	}

	public void Disable() { Disable(-1); } //manually provide no-argument method signature to allow easy callback
	public void Disable(int transformDataResetIndex)
	{
		if (transformData != null)
		{
			transformData.LoadLocalState(transformDataResetIndex < 0 ? lastTransitionIndex : transformDataResetIndex);
		}
		gameObject.SetActive(false);
	}

	public void ForceComplete()
	{
		SetActive(true);
		StartCoroutine(ForceCompleteCoroutine());
	}

	public IEnumerator ForceCompleteCoroutine()
	{
		isForcingComplete = true;
		yield return null; //wait two frames to ensure all computations are completed
		yield return null;
		isForcingComplete = false;
	}
}
