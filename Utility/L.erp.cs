using UnityEngine;

public enum Lerp
{
	Linear,
	Inverse,
	Triangle,
	Quad,
	Sqrt,
	CubeRoot,
	FourthRoot,
	Cubic,
	SinFull,
	SinHalf,
	SinQuarter,
	CosFull,
	CosHalf,
	CosQuarter,
	SinFullSquared,
	SinHalfSquared,
	SinQuarterSquared,
	CosFullSquared,
	CosHalfSquared,
	CosQuarterSquared,
	SinEaseInOut,
	SquishySin,
	SquishyCos,
	Exponential,
	Shake1,
	Shake2,
	Shake3,
	QuadJ,
	QuadJAccel,
	QuadJLite,
	Hop,
	TwoHops,
	TwoHops12,
	TriangleSin,
	ChargeRetract,
	SpeakUp,
	TripleHigh,
	Square_90Pct,
	Triangle_20Pct_Summit,
	Triangle_90Pct_Peak,
	SlowMo_90Pct,
	Spaz4,
	Spaz6,
	Spaz8,
	Spaz10,
	Parabola,
	Parabola01Overshoot,
}

public static class L
{
	private static T[] Slice<T>(T[] data, int startIndex, int length = -1)
	{
		if (length < 0)
		{
			length = data.Length - startIndex;
		}
		T[] result = new T[length];
		System.Array.Copy(data, startIndex, result, 0, length);
		return result;
	}

	public static float erp(float t, Lerp lerpCurve = Lerp.Linear, params Lerp[] additionalCurves)
	{
		if (additionalCurves.Length == 0)
		{
			float x;
			switch (lerpCurve)
			{
				case Lerp.Linear:
					return t;
				case Lerp.Inverse:
					return 1 - t;
				case Lerp.Triangle:
					return t < 0.5f ? 2 * t : 2 - 2 * t;
				case Lerp.Quad:
					return t * t;
				case Lerp.Sqrt:
					return Mathf.Sqrt(t);
				case Lerp.CubeRoot:
					return Mathf.Pow(t, 1.0f / 3);
				case Lerp.FourthRoot:
					return Mathf.Pow(t, 1.0f / 4);
				case Lerp.Cubic:
					return t * t * t;
				case Lerp.SinFull:
					return Mathf.Sin(t * Mathf.PI * 2);
				case Lerp.SinHalf:
					return Mathf.Sin(t * Mathf.PI);
				case Lerp.SinQuarter:
					return Mathf.Sin(t * Mathf.PI / 2);
				case Lerp.CosFull:
					return Mathf.Cos(t * Mathf.PI * 2);
				case Lerp.CosHalf:
					return Mathf.Cos(t * Mathf.PI);
				case Lerp.CosQuarter:
					return Mathf.Cos(t * Mathf.PI / 2);
				case Lerp.SinFullSquared:
					x = Mathf.Sin(t * Mathf.PI * 2);
					return x * x;
				case Lerp.SinHalfSquared:
					x = Mathf.Sin(t * Mathf.PI);
					return x * x;
				case Lerp.SinQuarterSquared:
					x = Mathf.Sin(t * Mathf.PI / 2);
					return x * x;
				case Lerp.CosFullSquared:
					x = Mathf.Cos(t * Mathf.PI * 2);
					return x * x;
				case Lerp.CosHalfSquared:
					x = Mathf.Cos(t * Mathf.PI);
					return x * x;
				case Lerp.CosQuarterSquared:
					x = Mathf.Cos(t * Mathf.PI / 2);
					return x * x;
				case Lerp.SinEaseInOut:
					return t < 0.5f ? 0.5f - Mathf.Cos(t * Mathf.PI) / 2 : 0.5f + Mathf.Sin((t - 0.5f) * Mathf.PI) / 2;
				case Lerp.SquishySin:
					return Mathf.Pow(2, Mathf.Sin(t * Mathf.PI * 2)) - 1;
				case Lerp.SquishyCos:
					return Mathf.Pow(2, Mathf.Cos(t * Mathf.PI * 2)) - 1;
				case Lerp.Exponential:
					return (Mathf.Exp(t) - 1) / (Mathf.Exp(1) - 1);
				case Lerp.Shake1:
					return Mathf.Sin(t * Mathf.PI * 4) * (t - 1) / 2 + 0.5f;
				case Lerp.Shake2:
					return Mathf.Sin(t * Mathf.PI * 8) * (t - 1) / 2 + 0.5f;
				case Lerp.Shake3:
					return Mathf.Sin(t * Mathf.PI * 12) * (t - 1) / 2 + 0.5f;
				case Lerp.QuadJ:
					return (2.25f * t * t - 1.5f * t) * 4 / 3; //min @ 1/3, 0 @ 2/3
				case Lerp.QuadJAccel:
					return (6.75f * t * t * t - 3.0f * t * t) / 3.75f; //min @ 0.296, 0 @ 4/9 
				case Lerp.QuadJLite:
					return (t * t - 0.5f * t) * 2;
				case Lerp.Hop:
					return 1 - 4 * (t - 0.5f) * (t - 0.5f);
				case Lerp.TwoHops:
					return t > 0.5f ? 1 - 16*(t - 0.75f) * (t - 0.75f) : 1 - 16*(t - 0.25f) * (t - 0.25f);
				case Lerp.TwoHops12:
					return t > 0.5f ? 1 - 16 * (t - 0.75f) * (t - 0.75f) : 0.5f - 8 * (t - 0.25f) * (t - 0.25f);
				case Lerp.TriangleSin:
					return t > 0.75f ? 4 * t - 4 : (t > 0.25 ? 2 - 4 * t : 4 * t);
				case Lerp.ChargeRetract:
					return t > 0.1f ? Mathf.Sqrt(1 - t) / Mathf.Sqrt(0.9f) : 10 * t;
				case Lerp.SpeakUp:
					return t > 0.5f ? 4 * (t - 1) * (t - 1) + 1 : 8 * t * t;
				case Lerp.TripleHigh:
					return 3 - (t > 0.5f ? 8 : 12) * (t - 0.5f) * (t - 0.5f);
				case Lerp.Square_90Pct:
					if (t < 0.1f) { return 0; }
					else if (t > 0.9f) { return 0; }
					else { return 1; }
				case Lerp.Triangle_20Pct_Summit:
					if (t < 0.4f) { return t / 0.4f; }
					else if (t < 0.6f) { return 1; }
					else { return 1 - (t - 0.6f) / 0.4f;  }
				case Lerp.Triangle_90Pct_Peak:
					if (t < 0.1f) { return t * 10; }
					else if (t > 0.9f) { return 10f - 10f * t; }
					else { return 1; }
				case Lerp.SlowMo_90Pct:
					if (t < 0.1f) { return t * 4.5f; }
					else if (t > 0.9f) { return 1 - ((1f - t) * 4.5f); }
					else { return 0.45f + ((t - 0.1f) / 8f); }
				case Lerp.Spaz4:
					if (t % (1f/2) > (1f/4)) { return 1f - t % (1f/4) * 4; }
					else { return t % (1f/4) * 4; }
				case Lerp.Spaz6:
					if (t % (1f / 3) > (1f / 6)) { return 1f - t % (1f / 6) * 6; }
					else { return t % (1f / 6) * 6; }
				case Lerp.Spaz8:
					if (t % (1f / 4) > (1f / 8)) { return 1f - t % (1f / 8) * 8; }
					else { return t % (1f / 8) * 8; }
				case Lerp.Spaz10:
					if (t % (1f / 5) > (1f / 10)) { return 1f - t % (1f / 10) * 10; }
					else { return t % (1f / 10) * 10; }
				case Lerp.Parabola:
					return 1.0f - 4 * (t - 0.5f) * (t - 0.5f);
				case Lerp.Parabola01Overshoot:
					return 1.225f - 2.5f * (t - 0.7f) * (t - 0.7f);
				default:
					goto case Lerp.Linear;
			}
		}
		else
		{
			return erp(erp(t, lerpCurve), additionalCurves[0], Slice(additionalCurves, 1));
		}
	}

	public static float erp(float a, float b, float t, Lerp lerpCurve = Lerp.Linear)
	{
		return Mathf.Lerp(a, b, erp(t, lerpCurve));
	}

	public static float erp(float a, float b, float t, Lerp lerpCurve, params Lerp[] additionalCurves)
	{
		return Mathf.Lerp(a, b, erp(t, lerpCurve, additionalCurves));
	}

	public static Vector3 erp(Vector3 a, Vector3 b, float t, Lerp lerpCurve = Lerp.Linear)
	{
		return Vector3.Lerp(a, b, erp(t, lerpCurve));
	}

	public static Vector3 erp(Vector3 a, Vector3 b, float t, Lerp lerpCurve, params Lerp[] additionalCurves)
	{
		return Vector3.Lerp(a, b, erp(t, lerpCurve, additionalCurves));
	}

	public static Color erp(Color a, Color b, float t, Lerp lerpCurve = Lerp.Linear)
	{
		return Color.Lerp(a, b, erp(t, lerpCurve));
	}

	public static Color erp(Color a, Color b, float t, Lerp lerpCurve, params Lerp[] additionalCurves)
	{
		return Color.Lerp(a, b, erp(t, lerpCurve, additionalCurves));
	}

	public static float erpDelta(float t1, float t2, Lerp lerpCurve)
	{
		return erp(t2, lerpCurve) - erp(t1, lerpCurve);
	}

	public static float erpDelta(float t1, float t2, Lerp lerpCurve, params Lerp[] additionalCurves)
	{
		return erp(t2, lerpCurve) - erp(t1, lerpCurve, additionalCurves);
	}

	public static float erpDelta(float t1, float t2, float duration, Lerp lerpCurve)
	{
		return erp(t2 / duration, lerpCurve) - erp(t1 / duration, lerpCurve);
	}

	public static float erpDelta(float t1, float t2, float duration, Lerp lerpCurve, params Lerp[] additionalCurves)
	{
		return erp(t2 / duration, lerpCurve) - erp(t1 / duration, lerpCurve, additionalCurves);
	}
}
