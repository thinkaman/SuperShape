using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TintGroup : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] float _alpha = 1;
    [SerializeField] public Color _color = Color.white;
	[SerializeField] float _prevAlpha = 1;
	[SerializeField] public Color _prevColor = Color.white;

	public float alpha { get { return _alpha; } set { _alpha = value; OnValidate(); } }
    public Color color { get { return _color; } set { _color = value; OnValidate(); } }

    public float groupAlpha { get { return tintGroupParent != null ? _alpha * tintGroupParent.groupAlpha : _alpha; } }
    public Color groupColor { get { return tintGroupParent != null ? _color * tintGroupParent.groupColor : _color; } }

    public List<SpriteRenderer> spriteList = new List<SpriteRenderer>();
    public List<MeshRenderer> meshList = new List<MeshRenderer>();
	public List<TMPro.TextMeshPro> textList = new List<TMPro.TextMeshPro>();
	public TintGroup tintGroupParent;
    public List<TintGroup> tintGroupChildrenList = new List<TintGroup>();

    private SortingGroup _sortingGroup;
    private MaterialPropertyBlock _propBlock;

    private void Clear()
    {
        if (spriteList == null)            { spriteList = new List<SpriteRenderer>();       } else { spriteList.Clear(); }
        if (meshList == null)              { meshList = new List<MeshRenderer>();           } else { meshList.Clear(); }
        if (textList == null)              { textList = new List<TMPro.TextMeshPro>();      } else { textList.Clear(); }
		if (tintGroupChildrenList == null) { tintGroupChildrenList = new List<TintGroup>(); } else { tintGroupChildrenList.Clear(); }
        tintGroupParent = null;
    }

	[ContextMenu("Register")]
	public void BeginScan()
    {
        TintGroup top = this;
        TintGroup search = this;
        while (top.transform.parent != null)
        {
            search = top.transform.parent.GetComponentInParent<TintGroup>();
            if (search != null) { top = search; }
            else { break; }
        }
        top.Clear();
		//Debug.Log(top.name);
        ScanChildren(top.transform);
		UpdateValues();
    }
    public void ScanChildren(Transform t)
    {
        TintGroup tg = t.GetComponent<TintGroup>();
		if (tg != null && tg != this)
		{
			tintGroupChildrenList.Add(tg);
			tg.Clear();
			tg.tintGroupParent = this;
			tg.ScanChildren(tg.transform);
		}
		else
		{
			SpriteRenderer sr = t.GetComponent<SpriteRenderer>(); if (sr != null) { spriteList.Add(sr); }
			TMPro.TextMeshPro tmp = t.GetComponent<TMPro.TextMeshPro>(); if (tmp != null) { textList.Add(tmp); }
			else {
				MeshRenderer mr = t.GetComponent<MeshRenderer>(); if (mr != null) { meshList.Add(mr); }
			}

			foreach (Transform child in t)
			{
				ScanChildren(child);
			}
		}
	}

	private void UpdateValues()
	{
        _alpha = Mathf.Clamp01(_alpha);
        _color.a = alpha;
        ApplyMaterialPropertyBlock();
    }

    public void ApplyMaterialPropertyBlock(int targetBGIndex = -10)
    {
        if (_sortingGroup == null) { _sortingGroup = GetComponent<SortingGroup>(); }
        if (_propBlock == null) { _propBlock = new MaterialPropertyBlock(); }

		if (targetBGIndex == -10)
		{
			targetBGIndex = 0;
			if (_sortingGroup != null)
			{
				targetBGIndex = 1;
			}
		}

        Color myColor = groupColor;
        myColor.a = groupAlpha;

        foreach (SpriteRenderer sr in spriteList)
        {
            if (sr == null) { continue; }
            sr.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_GroupColor", myColor);
            _propBlock.SetInt("_GroupBGIndex", targetBGIndex);
            sr.SetPropertyBlock(_propBlock);
        }

        foreach (MeshRenderer mr in meshList)
        {
            if (mr == null) { continue; }
			mr.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_GroupColor", myColor);
            _propBlock.SetInt("_GroupBGIndex", targetBGIndex);
            mr.SetPropertyBlock(_propBlock);
		}

		foreach (TMPro.TextMeshPro tmp in textList)
		{
			if (tmp == null) { continue; }
			tmp.alpha = myColor.a;
		}

		foreach (TintGroup child in tintGroupChildrenList)
		{
			if (child != null && child != this) child.ApplyMaterialPropertyBlock(targetBGIndex);
		}
	}

    public void SetMaskInteraction(SpriteMaskInteraction interaction)
	{
        foreach (SpriteRenderer sr in spriteList)
		{
            sr.maskInteraction = interaction;
		}
	}

    private void OnValidate()
    {
		if (_alpha == _prevAlpha && _color == _prevColor) { return; }
		_prevAlpha = _alpha;
		_prevColor = _color;
        UpdateValues();
    }
}
