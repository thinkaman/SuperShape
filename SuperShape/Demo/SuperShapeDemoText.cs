using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperShapeDemoText : DynamicMonoBehaviour
{
	public void SetText(string textString, float oldTextFadeOutTime, float fadeInTime, Lerp lerp)
	{
		FadeText(oldTextFadeOutTime);
		text.text = textString;
		FadeIn(fadeInTime, lerp, oldTextFadeOutTime);
	}

	public void FadeText(float fadeTime = 0.4f)
	{
		FadeOut(fadeTime);
	}
}
