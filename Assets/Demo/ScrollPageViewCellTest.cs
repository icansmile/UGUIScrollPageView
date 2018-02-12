using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollPageViewCellTest : ScrollPageViewCell
{
	void Awake()
	{
		OnShow = () => { this.gameObject.SetActive(true); };
		OnHide = () => { };
	}
}
