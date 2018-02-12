using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollPageView))]
public class ScrollPageViewEditor : MonoBehaviour 
{
    [Header("=====Init=====")]
    public GameObject _cellSource;
    public int _cellCount;
    public int _rowCount;
    public int _columnCount;
    public Vector2 _spacing;

    #region 用于分页模式

    [Header("=====PageMode=====")]
    public bool _pageMode = false;
    public RectTransform _scrollTargetPos;
	public float _posTweenDuration = 0.3f;
    public Button _prePageBtn;
    public Button _nextPageBtn;
    #endregion


    [Header("=====DebugMode=====")]
    public bool _debugMode = false;
    // 代码约束参数太繁杂,直接提供完整模板prefab

    // void Reset()
    // {
    //     // Debug.Log("脚本添加事件");
    // }
 
    // void OnValidate()
    // {
    //     // Debug.Log("脚本对象数据发生改变事件");
    //     // CheckIntegrity();
    // }

    // void CheckIntegrity()
    // {
    //     var scrollRect = this.GetComponent<NewScrollRect>();

    //     if(scrollRect.content == null)
    //     {
    //         GameObject go;
    //         var trans = transform.Find("Content");

    //         if(trans == null)
    //         {
    //             go = new GameObject("Content", typeof(RectTransform));
    //             go.transform.SetParent(this.transform);
    //         }
    //         else
    //         {
    //             go = trans.gameObject;
    //         }

    //         scrollRect.content = go.GetComponent<RectTransform>();
    //     }

    //     if(!_pageMode)
    //         return;
        
    //     if(_scrollTargetPos == null)
    //     {
    //         GameObject go;
    //         var trans = transform.Find("targetPos");

    //         if(trans == null)
    //         {
    //             go = new GameObject("targetPos", typeof(RectTransform));
    //             go.transform.SetParent(this.transform);
    //         }
    //         else
    //         {
    //             go = trans.gameObject;
    //         }

    //         _scrollTargetPos = go.GetComponent<RectTransform>();
    //     }

    //     if(_scrollPosTweener == null)
    //     {
    //         GameObject go;
    //         var trans = transform.Find("scrollPosUITweener");

    //         if(trans == null)
    //         {
    //             go = new GameObject("scrollPosUITweener", typeof(UITweener));
    //             go.transform.SetParent(this.transform);
    //         }
    //         else
    //         {
    //             go = trans.gameObject;
    //         }

    //         _scrollPosTweener = go.GetComponent<UITweener>();
    //     }

    //     _scrollPosTweener.wrapMode = WhiteCat.Tween.WrapMode.Clamp;

    //     if(_scrollPosTween == null)
    //     {
    //         TweenAnchoredPosition posTween = scrollRect.content.GetComponent<TweenAnchoredPosition>();
    //         if(posTween == null)
    //         {
    //             posTween = scrollRect.content.gameObject.AddComponent<TweenAnchoredPosition>();
    //         }

    //         _scrollPosTween = posTween;
    //     }

    //     _scrollPosTween.toggleX = scrollRect.horizontal;
    //     _scrollPosTween.toggleY = scrollRect.vertical;

    //     if(_scrollPosTween.tweener == null)
    //     {
    //         _scrollPosTween.tweener = _scrollPosTweener;
    //     }
    // }
}