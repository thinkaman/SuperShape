using System;
using UnityEngine;

public enum ColorBlockState
{
    Normal,
    Highlighted,
    Pressed,
    Toggled,

    Disabled,
}

[Serializable]
public struct BlinkingColorBlock : IEquatable<BlinkingColorBlock>
{
    [SerializeField]
    private Color m_NormalColor;
    [SerializeField]
    private Color m_HighlightedColor;
    [SerializeField]
    private Color m_PressedColor;
    [SerializeField]
    private Color m_ToggledColor;

    [SerializeField]
    private Color m_BlinkNormalColor;
    [SerializeField]
    private Color m_BlinkHighlightedColor;
    [SerializeField]
    private Color m_BlinkPressedColor;
    [SerializeField]
    private Color m_BlinkToggledColor;

    [SerializeField]
    private Color m_DisabledColor;

    [Range(1, 5)]
    [SerializeField]
    private float m_ColorMultiplier;

    [SerializeField]
    private float m_FadeDuration;
    [SerializeField]
    private float m_BlinkDuration;
    [SerializeField]
    private Lerp m_BlinkLerp;
    [SerializeField]
    private bool m_IsBlinkTimerGlobal;

    [SerializeField]
    private ColorBlockState m_CurrentState;
    [SerializeField]
    private bool m_IsBlinking;

    private float blinkTotalTimer;
    private bool isCommandBlink;
    private float commandBlinkDuration;
    private Lerp commandBlinkLerp;
    private float[] m_ColorFadeRatios; //normal, highlight, pressed, toggled, disabled, overall blink

    public Color normalColor { get { return m_NormalColor; } set { m_NormalColor = value; } }
    public Color highlightedColor { get { return m_HighlightedColor; } set { m_HighlightedColor = value; } }
    public Color pressedColor { get { return m_PressedColor; } set { m_PressedColor = value; } }
    public Color toggledColor { get { return m_ToggledColor; } set { m_ToggledColor = value; } }
    public Color blinkNormalColor { get { return m_BlinkNormalColor; } set { m_BlinkNormalColor = value; } }
    public Color blinkHighlightedColor { get { return m_BlinkHighlightedColor; } set { m_BlinkHighlightedColor = value; } }
    public Color blinkPressedColor { get { return m_BlinkPressedColor; } set { m_BlinkPressedColor = value; } }
    public Color blinkToggledColor { get { return m_BlinkToggledColor; } set { m_BlinkToggledColor = value; } }
    public Color disabledColor { get { return m_DisabledColor; } set { m_DisabledColor = value; } }
    public float colorMultiplier { get { return m_ColorMultiplier; } set { m_ColorMultiplier = value; } }
    public float fadeDuration { get { return m_FadeDuration; } set { m_FadeDuration = value; } }
    public float blinkDuration { get { return m_BlinkDuration; } set { m_BlinkDuration = value; } }
    public Lerp blinkLerp { get { return m_BlinkLerp; } set { m_BlinkLerp = value; } }
    public bool isBlinkTimerGlobal { get { return m_IsBlinkTimerGlobal; } set { m_IsBlinkTimerGlobal = value; } }
    public ColorBlockState currentState { get { return m_CurrentState; } set { m_CurrentState = value; } }
    public bool isBlinking { get { return m_IsBlinking; } set { m_IsBlinking = value; } }
    public float[] colorFadeRatios { get { return m_ColorFadeRatios; } set { m_ColorFadeRatios = value; } }

    public static BlinkingColorBlock defaultColorBlock
    {
        get
        {
            var c = new BlinkingColorBlock
            {
                m_NormalColor = new Color32(240, 240, 240, 255),
                m_HighlightedColor = new Color32(223, 163, 100, 255),
                m_PressedColor = new Color32(84, 233, 43, 255),
                m_ToggledColor = new Color32(135, 193, 219, 255),
                m_BlinkNormalColor = new Color32(223, 255, 138, 255),
                m_BlinkHighlightedColor = new Color32(237, 255, 138, 255),
                m_BlinkPressedColor = new Color32(179, 255, 109, 255),
                m_BlinkToggledColor = new Color32(52, 255, 150, 255),
                m_DisabledColor = new Color32(160, 160, 160, 255),
                m_ColorMultiplier = 1.0f,
                m_FadeDuration = 0.2f,
                m_BlinkDuration = 1.0f,
                m_BlinkLerp = Lerp.SinHalfSquared,
                m_IsBlinkTimerGlobal = false,

                m_CurrentState = ColorBlockState.Normal,
                m_IsBlinking = false,

                blinkTotalTimer = 0,
                m_ColorFadeRatios = new float[6] { 1.0f, 0, 0, 0, 0, 0 }, //normal, highlight, pressed, toggled, disabled, overall blink
            };
            return c;
        }
    }

    public override bool Equals(object obj)
    {
        if (!(obj is BlinkingColorBlock))
            return false;

        return Equals((BlinkingColorBlock)obj);
    }

    public bool Equals(BlinkingColorBlock other)
    {
        return normalColor == other.normalColor &&
               highlightedColor == other.highlightedColor &&
               pressedColor == other.pressedColor &&
               toggledColor == other.toggledColor &&
               blinkNormalColor == other.blinkNormalColor &&
               blinkHighlightedColor == other.blinkHighlightedColor &&
               blinkPressedColor == other.blinkPressedColor &&
               blinkToggledColor == other.blinkToggledColor &&
               disabledColor == other.disabledColor &&
               colorMultiplier == other.colorMultiplier &&
               fadeDuration == other.fadeDuration &&
               blinkDuration == other.blinkDuration &&
               blinkLerp == other.blinkLerp &&
               isBlinkTimerGlobal == other.isBlinkTimerGlobal;
    }

    public static bool operator ==(BlinkingColorBlock point1, BlinkingColorBlock point2)
    {
        return point1.Equals(point2);
    }

    public static bool operator !=(BlinkingColorBlock point1, BlinkingColorBlock point2)
    {
        return !point1.Equals(point2);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
	private float total;

    public void Update(float deltaTime)
    {
        if (colorFadeRatios == null || colorFadeRatios.Length != 6) { colorFadeRatios = new float[6] { 1.0f, 0, 0, 0, 0, 0 }; }
        blinkTotalTimer += deltaTime;
        float delta = deltaTime / fadeDuration;
        colorFadeRatios[0] = Mathf.Clamp01(colorFadeRatios[0] + delta * (m_CurrentState == ColorBlockState.Normal ? 1 : -1));
        colorFadeRatios[1] = Mathf.Clamp01(colorFadeRatios[1] + delta * (m_CurrentState == ColorBlockState.Highlighted ? 1 : -1));
        colorFadeRatios[2] = Mathf.Clamp01(colorFadeRatios[2] + delta * (m_CurrentState == ColorBlockState.Pressed ? 1 : -1));
        colorFadeRatios[3] = Mathf.Clamp01(colorFadeRatios[3] + delta * (m_CurrentState == ColorBlockState.Toggled ? 1 : -1));
        colorFadeRatios[4] = Mathf.Clamp01(colorFadeRatios[4] + delta * (m_CurrentState == ColorBlockState.Disabled ? 1 : -1));
        colorFadeRatios[5] = Mathf.Clamp01(colorFadeRatios[5] + delta * (isBlinking ? 1 : -1));
		total = 1 / (colorFadeRatios[0] + colorFadeRatios[1] + colorFadeRatios[2] + colorFadeRatios[3] + colorFadeRatios[4]);

		if (isCommandBlink && blinkTotalTimer >= commandBlinkDuration) { isCommandBlink = false; }
    }

    public void CommandBlink(float duration = -1, Lerp lerp = Lerp.Triangle)
    {
        isCommandBlink = true;
        blinkTotalTimer = 0;
        commandBlinkDuration = duration < 0 ? blinkDuration : duration;
        commandBlinkLerp = lerp;
    }

    public Color GetColor()
    {
        float t = isBlinkTimerGlobal ? Time.time : blinkTotalTimer;
        float b;
        if (isCommandBlink)
        {
            b = L.erp((blinkTotalTimer % commandBlinkDuration) / commandBlinkDuration, commandBlinkLerp);
        }
        else
        {
            b = colorFadeRatios[5] * L.erp((t % m_BlinkDuration) / m_BlinkDuration, m_BlinkLerp);
        }
        return (colorFadeRatios[0] * total * ((1 - b) * m_NormalColor + b * m_BlinkNormalColor) +
                colorFadeRatios[1] * total * ((1 - b) * m_HighlightedColor + b * m_BlinkHighlightedColor) +
                colorFadeRatios[2] * total * ((1 - b) * m_PressedColor + b * m_BlinkPressedColor) +
                colorFadeRatios[3] * total * ((1 - b) * m_ToggledColor + b * m_BlinkToggledColor) +
                colorFadeRatios[4] * total * m_DisabledColor) * m_ColorMultiplier;
    }
}
