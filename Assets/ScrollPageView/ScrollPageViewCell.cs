using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ScrollPageViewCell : MonoBehaviour
{
	public int _index;

	RectTransform _rectTransform;
	public RectTransform RectTransform
	{
		get
		{
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}

	public System.Action OnShow { get; set; }
	public System.Action OnHide { get; set; }
	public System.Action OnRefresh { get; set; }

	public void Show()
	{
		if(OnShow != null)
			OnShow();
	}

	public void Hide()
	{
		if(OnHide != null)
			OnHide();
	}

	public void Refresh()
	{
		if (OnRefresh != null)
			OnRefresh();
	}
}