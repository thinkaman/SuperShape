using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;


public class PaletteChanger : MonoBehaviour
{
	public TMPro.TextMeshPro linkedText;
	public Color linkedTextReferenceChannel;
	public Texture2D paletteA;
	public Texture2D paletteB;
	[Range(0, 1)]
	public float blend;

	private Renderer _renderer;
	private MaterialPropertyBlock _mpb;

	void Awake()
	{
		SetMaterialPropertyBlock();
	}

	[ContextMenu("Set Palettes")]
	public void SetMaterialPropertyBlock()
	{
		if (_renderer == null)
		{
			_renderer = GetComponent<Renderer>();
		}
		if (_mpb == null)
		{
			_mpb = new MaterialPropertyBlock();
		}
		_renderer.GetPropertyBlock(_mpb);
		_mpb.SetTexture("_PaletteTexA", paletteA);
		_mpb.SetTexture("_PaletteTexB", paletteB == null ? paletteA : paletteB);
		_mpb.SetFloat("_PaletteTexBlend", blend);
		_renderer.SetPropertyBlock(_mpb);
		if (linkedText != null)
		{
			Vector4 v = linkedTextReferenceChannel;
			int x = paletteA.width - 1;
			Color targetColor;
			if (paletteB != null)
			{
				targetColor = v.x * (paletteA.GetPixel(0,0) * (1 - blend) + paletteB.GetPixel(0, 0) * blend) +
				              v.y * (paletteA.GetPixel(x,0) * (1 - blend) + paletteB.GetPixel(x, 0) * blend) +
				              v.z * (paletteA.GetPixel(0,x) * (1 - blend) + paletteB.GetPixel(0, x) * blend);
			}
			else
			{
				targetColor = v.x * paletteA.GetPixel(0,0) +
				              v.y * paletteA.GetPixel(x,0) +
				              v.z * paletteA.GetPixel(0,x);
			}
			linkedText.color = targetColor;
		}
	}

	public void ChangePaletteA(Texture2D newPalette, bool isForcing = true)
	{
		if (isForcing) { blend = 0; }
		else if (paletteA == newPalette) { return; }
		paletteA = newPalette;
		SetMaterialPropertyBlock();
	}

	public void ChangePaletteB(Texture2D newPalette, bool isForcing = true)
	{
		paletteB = newPalette;
		if (isForcing) { blend = 1; }
		SetMaterialPropertyBlock();
	}
}
