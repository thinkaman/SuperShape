using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SuperShapeDemoAssassin : DynamicMonoBehaviour
{
	public Image eye;
	public Image hand;
	public SpriteRenderer eye2;
	public SpriteRenderer hand2;
	public Transform mouth;

	public Sprite[] poses;

	public Sprite[] eyes;

	public Sprite[] hands;

	public void SetAll(int i) { SetPose(i); SetEyes(i); SetHand(i); }
	public void SetPose(int i) { if (image == null) { spriteRenderer.sprite = poses[i]; } else { image.sprite = poses[i]; image.SetAllDirty(); } }
	public void SetEyes(int i) { if (image == null) { eye2.sprite = eyes[i]; eye2.enabled = eye2.sprite != null; } else { eye.sprite = eyes[i]; eye.enabled = eye.sprite != null; } }
	public void SetHand(int i) { if (image == null) { hand2.sprite = hands[i]; hand2.enabled = hand2.sprite != null; } else { hand.sprite = hands[i]; hand.enabled = hand.sprite != null; } }
}
