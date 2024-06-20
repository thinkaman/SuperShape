using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct Tri { public int a, b, c; public Tri(int a, int b, int c) { this.a = a; this.b = b; this.c = c; } }

public class TriangulationUtility
{
	static List<int> candidates = null;
	static List<int> ears = null;
	static List<Tri> tris = null;

	static readonly Vector2[] defaultUVs = new Vector2[5] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0.5f, 0.5f) };
	public static List<Tri> TriangulateFull(Vector2[] points, Color color, Vector2 UVmin, Vector2 UVmax, List<Tri> triCache = null, VertexHelper vh = null)
	{
		if (points.Length < 3) { return null; }

		int x = 0;
		if (vh != null)
		{
			x += vh.currentVertCount;
			UIVertex vert = new UIVertex();
			vert.color = color;
			vert.uv0 = Vector2.zero;

			//record vert data
			for (int i = 0; i < points.Length; i++)
			{
				vert.position = points[i];
				if (UVmax - UVmin != Vector2.zero)
				{
					vert.uv0 = (points[i] - UVmin) / (UVmax - UVmin);
				}
				vh.AddVert(vert);
			}
		}

		if (triCache == null)
		{
			triCache = GetTriList(points);
		}
		if (vh != null)
		{
			for (int i = 0; i < triCache.Count; i++)
			{
				vh.AddTriangle(x + triCache[i].a, x + triCache[i].b, x + triCache[i].c);
			}
		}
		return triCache;
	}

	public static void TriangulateFast(VertexHelper vh, Vector2[] points, Color color, bool isUVsZero)
	{
		int x = vh.currentVertCount;
		UIVertex vert = new UIVertex();
		vert.color = color;
		vert.uv0 = Vector2.zero;

		for (int i = 0; i < points.Length; i++)
		{
			vert.position = points[i];
			if (!isUVsZero)
			{
				//TODO: UVs
				if (points.Length <= 5)
				{
					vert.uv0 = defaultUVs[i];
				}
			}
			vh.AddVert(vert);
		}

		if (points.Length == 3)
		{
			vh.AddTriangle(x + 0, x + 1, x + 2);
		}
		else if (points.Length == 4)
		{
			vh.AddTriangle(x + 0, x + 1, x + 2);
			vh.AddTriangle(x + 2, x + 3, x + 0);
		}
		else if (points.Length == 5)
		{
			vh.AddTriangle(x + 0, x + 1, x + 4);
			vh.AddTriangle(x + 1, x + 2, x + 4);
			vh.AddTriangle(x + 2, x + 3, x + 4);
		}
		else if (points.Length == 6)
		{
			vh.AddTriangle(x + 0, x + 1, x + 5);
			vh.AddTriangle(x + 1, x + 4, x + 5);
			vh.AddTriangle(x + 1, x + 2, x + 4);
			vh.AddTriangle(x + 2, x + 3, x + 4);
		}
	}

	public static void TriangulateBorderAround(Vector2[] points, Vector2[] prevPoints, Color color, VertexHelper vh = null)
	{
		int c = points.Length;
		UIVertex vert = new UIVertex();
		vert.color = color;
		vert.uv0 = Vector2.zero;

		//duplicate prev vert data with our new color and UV data
		for (int i = 0; i < c; i++)
		{
			vert.position = prevPoints[i];
			vh.AddVert(vert);
		}
		int x = vh.currentVertCount;

		//record vert data
		for (int i = 0; i < c; i++)
		{
			vert.position = points[i];
			vh.AddVert(vert);
		}

		int j = c - 1;
		for (int i = 0; i < c; j = i++)
		{
			vh.AddTriangle(x + i, x - c + j, x + j);
			vh.AddTriangle(x + i, x - c + j, x - c + i);
		}
		return;
	}


	public static List<Tri> TriangulateBorderList(int c)
	{
		if (tris == null) { tris = new List<Tri>(); }
		tris.Clear();
		int j = c - 1;
		for (int i = 0; i < c; j = i++)
		{
			tris.Add(new Tri(i, -c + j, j));
			tris.Add(new Tri(i, -c + j, -c + i));
		}
		return tris;
	}

	static bool IsReflexVertex(Vector2 prev, Vector2 vertex, Vector2 next)
	{
		Vector2 v1 = next - vertex;
		Vector2 v2 = vertex - prev;
		float delta = Mathf.Atan2(v1.y, v1.x) - Mathf.Atan2(v2.y, v2.x);
		//Debug.Log(prev + " " + vertex + " " + next + " " + Mathf.Atan2(v1.y, v1.x) + "       " + Mathf.Atan2(v2.y, v2.x) + "        " + delta);
		return delta < -Mathf.PI || (delta > 0 && delta < Mathf.PI);
	}

	static int IsAddEar(Vector2[] points, bool[] isReflexVertex, int a, int b, int c, int addIndex)
	{
		if (isReflexVertex[b]) { return 0; }
		bool isFoundInsidePoint = false;
		for (int j = 0; j < candidates.Count; j++)
		{
			int x = candidates[j];
			if (x == a || x == b || x == c || !isReflexVertex[x]) { continue; }
			if (IsPointInTriangle(points[x], points[a], points[b], points[c]))
			{
				isFoundInsidePoint = true;
				//Debug.Log("Tri " + a + " " + b + " " + c + " has inside point " + x);
				break;
			}
		}
		if (isFoundInsidePoint)
		{
			if (ears.Contains(b))
			{
				int x = ears[0] == b ? -1 : 0;
				ears.Remove(b);
				return x;
			}
		}
		else
		{
			if (!ears.Contains(b))
			{
				ears.Insert(addIndex, b);
				return 1;
			}
		}
		return 0;
	}

	static bool[] isReflexVertex;
	public static List<Tri> GetTriList(Vector2[] points)
	{
		System.Array.Resize(ref isReflexVertex, points.Length);
		if (candidates == null)
		{
			candidates = new List<int>(points.Length);
			ears = new List<int>(points.Length);
			tris = new List<Tri>(points.Length-2);
		}
		else
		{
			candidates.Clear();
			ears.Clear();
			tris.Clear();
		}

		//do initial reflex (concavity) tests
		for (int i = 0; i < points.Length; i++)
		{
			int iPrev = i > 0 ? i - 1 : points.Length - 1;
			int iNext = i < points.Length - 1 ? i + 1 : 0;
			isReflexVertex[i] = IsReflexVertex(points[iPrev], points[i], points[iNext]);
			candidates.Add(i);
		}

		//now test for ears
		for (int i = 0; i < points.Length; i++)
		{
			int iPrev = i > 0 ? i - 1 : points.Length - 1;
			int iNext = i < points.Length - 1 ? i + 1 : 0;

			IsAddEar(points, isReflexVertex, iPrev, i, iNext, ears.Count);
		}

		/*
		string s = "";
		for (int i = 0; i < isReflexVertex.Length; i++)
		{
			s += isReflexVertex[i] + " ";
		}
		Debug.Log(s + ":    (" + ears.Count + ")");
		*/

		//start removing ears + recording tris
		//as long as we have nontrivial data left, run fresh reflex test and then an ear test on adjacent points
		while (candidates.Count > 3 && ears.Count > 0)
		{
			int ear = ears[0];
			int earIndex = candidates.IndexOf(ear);
			int earPrev = candidates[earIndex > 0 ? earIndex - 1 : candidates.Count - 1];
			int earPrev2 = candidates[earIndex > 1 ? earIndex - 2 : candidates.Count - 2 + earIndex];
			int earNext = candidates[earIndex < candidates.Count - 1 ? earIndex + 1 : 0];
			int earNext2 = candidates[earIndex < candidates.Count - 2 ? earIndex + 2 : 2 - (candidates.Count - earIndex)];
			tris.Add(new Tri(earPrev, ear, earNext));


			/*
			string q = "";
			foreach (int i in candidates) { q += i; }
			q += " ";
			foreach (int i in ears) { q += i; }
			q += "::: " + ear + " " + earIndex + " " + earPrev + " " + earPrev2 + " " + earNext + " " + earNext2;
			Debug.Log(q);
			*/

			//now, before we remove the data for the ear and changes indexes around...
			//run fresh reflex tests on the adjacent points
			isReflexVertex[earPrev] = IsReflexVertex(points[earPrev2], points[earPrev], points[earNext]);
			isReflexVertex[earNext] = IsReflexVertex(points[earPrev], points[earNext], points[earNext2]);
			//run ear test on the adjacent points
			int earChange = IsAddEar(points, isReflexVertex, earPrev2, earPrev, earNext, 0);
			int earChangeb = IsAddEar(points, isReflexVertex, earPrev, earNext, earNext2, 1 + earChange);

			/*
			q = "";
			foreach (int i in candidates) { q += i; }
			q += " ";
			foreach (int i in ears) { q += i; }
			q += " ";
			foreach (bool b in isReflexVertex) { q += b ? 1 : 0; }
			q += "::: " + ear + " " + earIndex + " " + earPrev + " " + earPrev2 + " " + earNext + " " + earNext2 + " " + earChange + " " + earChangeb;
			Debug.Log(q);
			*/

			ears.RemoveAt(earChange == 1 ? 1 : 0);
			candidates.RemoveAt(earIndex);
		}
		//grab the last 3 points as a guaranteed freebie
		tris.Add(new Tri(candidates[0], candidates[1], candidates[2]));

		return tris;
	}

	static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
	{
		float d1, d2, d3;
		bool has_neg, has_pos;

		d1 = Sign(p, a, b);
		d2 = Sign(p, b, c);
		d3 = Sign(p, c, a);

		has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
		has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

		return !(has_neg && has_pos);
	}

	static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
	{
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}

}
