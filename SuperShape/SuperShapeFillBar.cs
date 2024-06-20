using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteAlways]
public class SuperShapeFillBar : MonoBehaviour
{
    SuperShape _shape;
    public SuperShape shape { get { if (_shape == null) { _shape = GetComponent<SuperShape>(); } return _shape; } }

    public Color highlightBlinkColorA;
    public Color highlightBlinkColorB;
    public float highlightBlinkDuration;
    public Color completeBlinkColorA;
    public Color completeBlinkColorB;
    public float completeFlashDuration;
    public float completeBlinkDuration;

    private Color tempColor0;
    private Color tempColor1;

    [Range(0, 1)] public float baseComplete;
    [Range(0, 1)] public float tweenComplete;

    public float completeTimer;
    public bool isNewlyComplete;

    private bool isUsingAltColors;

    public void Initialize(float value, bool isAnimatedSetup, float tweenBonus = 0)
    {
        value = Mathf.Clamp01(value);
        isNewlyComplete = false;
        tweenComplete = 0;

        if (tweenBonus > value) { tweenBonus = value; }
        value -= tweenBonus;

        if (isAnimatedSetup)
        {
            baseComplete = 0;
            tweenComplete = tweenBonus;
			gameObject.SetActive(true);
            StartCoroutine(TweenCoroutine(true, true, value, value * 0.3f));
        }
        else
        {
            baseComplete = value;
            tweenComplete = value + tweenBonus;
        }
    }

    private Vector2[] _targetVerts = new Vector2[4];

    public void ChangeValue(float newValue)
    {
        newValue = Mathf.Clamp01(newValue);

        baseComplete = newValue;
        tweenComplete = newValue;
    }

    public void Tween(float newValue, float duration, float delay = 0)
    {
        tweenComplete = baseComplete;
        newValue = Mathf.Clamp01(newValue);
        Call call;
        if (newValue == 1)
        {
            call = new Call(TweenFullComplete);
        }
        else
        {
            call = new Call(TweenComplete);
        }
        float delta = newValue - tweenComplete;
        StartCoroutine(TweenCoroutine(false, true, delta, delta * 1.5f, Lerp.Linear, delay, call.back));
    }

    public void TweenComplete()
    {

    }

    public void TweenFullComplete()
    {
        tempColor0 = shape.layerColors[0];
        tempColor1 = shape.layerColors[1];
        isNewlyComplete = true;
    }


    void Update()
    {
		if (Application.isEditor)
		{
			//return;
		}
        if (isNewlyComplete)
        {
            if (completeTimer < completeFlashDuration / 2)
            {
                shape.layerColors[0] = L.erp(tempColor0, completeBlinkColorB, completeTimer / completeFlashDuration * 2, Lerp.SinQuarterSquared);
                shape.layerColors[1] = L.erp(tempColor1, completeBlinkColorB, completeTimer / completeFlashDuration * 2, Lerp.SinQuarterSquared);
            }
            else if (completeTimer < completeFlashDuration)
            {
                shape.layerColors[0] = L.erp(completeBlinkColorB, completeBlinkColorA, completeTimer / completeFlashDuration * 2, Lerp.CosQuarterSquared);
            }
            else
            {
                shape.layerColors[0] = L.erp(completeBlinkColorA, completeBlinkColorB,
                                             (completeTimer % completeBlinkDuration) / completeBlinkDuration, Lerp.SinHalfSquared);
            }
            completeTimer += shape.deltaTime;
            if (completeTimer >= completeFlashDuration / 2) { baseComplete = 1.0f; }
        }
        else
        {
            if (tweenComplete == baseComplete)
            {
                return;
            }
            else if (tweenComplete < baseComplete)
            {
                shape.layerColors[1] = L.erp(completeBlinkColorA, completeBlinkColorB,
                                             (shape.time % highlightBlinkDuration) / highlightBlinkDuration, Lerp.SinHalfSquared);
            }
            else
            {
                shape.layerColors[1] = L.erp(highlightBlinkColorA, highlightBlinkColorB,
                                             (shape.time % highlightBlinkDuration) / highlightBlinkDuration, Lerp.SinHalfSquared);
            }
        }
    }

    public bool isForcingComplete = false;
    private IEnumerator TweenCoroutine(bool isBase, bool isTween,
                                       float delta,
                                       float duration = 1.0f,
                                       Lerp lerpCurve = Lerp.Linear,
                                       float delay = 0,
                                       Action<object[]> callback = null,
                                       params object[] args)
    {
        float delayTimer = delay;
        while (delayTimer > 0)
        {
            delayTimer = isForcingComplete ? 0 : delayTimer - shape.deltaTime;
            yield return null;
        }
        if (duration <= 0)
        {
            if (isBase)  {  baseComplete += delta; }
            if (isTween) { tweenComplete += delta; }
            shape.SetMeshDirty();
            yield break;
        }
        float currentTime = 0;
        while (currentTime < duration)
        {
            float newTime = isForcingComplete ? duration : currentTime + shape.deltaTime;
            if (newTime >= duration) { newTime = duration;  }
            float relativeTimeDelta = L.erpDelta(currentTime, newTime, duration, lerpCurve);
            if (isBase)  {  baseComplete += relativeTimeDelta * delta; }
            if (isTween) { tweenComplete += relativeTimeDelta * delta; }
            shape.SetMeshDirty();
            currentTime = newTime;
            yield return null;
        }
        if (callback != null) { callback(args); }
    }

	public void CatchupTween(float newTarget, float speed, float delay = 0,
                             Action<object[]> callback = null, params object[] args)
	{
		catchupTweenMutexCounter++;
		tweenComplete = newTarget;
		StartCoroutine(CatchupTweenCoroutine(speed, delay, catchupTweenMutexCounter, callback, args));
	}

	private int catchupTweenMutexCounter;
	private IEnumerator CatchupTweenCoroutine(float speed,
											  float delay = 0,
											  int catchupTweenMutexRequirement = -1,
                                              Action<object[]> callback = null,
                                              params object[] args)
    {
        float delayTimer = delay;
        while (delayTimer > 0)
        {
            delayTimer = isForcingComplete ? 0 : delayTimer - shape.deltaTime;
            yield return null;
        }
        if (isForcingComplete)
        {
            baseComplete = tweenComplete;
            shape.SetMeshDirty();
            yield break;
        }
        
		while (baseComplete < Mathf.Min(1, tweenComplete) && (catchupTweenMutexCounter == -1 || catchupTweenMutexCounter == catchupTweenMutexRequirement))
		{
			baseComplete += Mathf.Clamp01(speed * shape.deltaTime);
			shape.SetMeshDirty();
			yield return null;
        }
        if (callback != null) { callback(args); }
    }
}
