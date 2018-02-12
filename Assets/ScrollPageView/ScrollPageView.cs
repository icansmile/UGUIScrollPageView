using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public interface IScrollPageView
{
	void Init(int cellCount);
	void ScrollToIndex(int index, float duration);
	void RefreshAll();
	void ResetPos();
	void GoNextCell();
	void GoPreCell();
	Action<int, ScrollPageViewCell> OnRefresh { get; set; }
	Action<int> OnPageIndexChanged { get; set; }
}

/// <summary>
/// 优化Scroll
/// TODO: 10.page模式下对ScrollBar的支持 12.考虑分离横纵两种模式的代码，从而简化此文件 13.content bounds 14.独立Bound检测功能
/// DONE: 1.配合tween实现的PageMode,定位。(OnEndDrag) 2.对外接口。(init,refresh,scrollto) 3.同一列表的复用（重新Init后，对cells进行增删或重置) 4.cell的加载(外部加载模板后注入) 5.规范锚点(左上角)
///       6.Editor自动检查组件完整性(特例不少，过于繁杂，直接提供模板prefab) 7.确定数据传入方式(_onRefresh, _onPageIndexChanged) 8.Page切换 
/// 	  11.视需求决定 cellsize的是简单的自动获取还是同grid一样硬性配置(增加Spacing,cellSize自动获取)
/// 	  9.page模式下对滚轮的支持 
/// </summary>
[RequireComponent(typeof(ScrollPageViewEditor))]
public class ScrollPageView : ScrollRect, IScrollPageView
{
	class ReusingData
	{
		public int _index;
		public ReusingStatus status;
	}

	enum ReusingStatus
	{
		Show,
		Hide
	}

	private ScrollPageViewEditor _editorValue = null;
	public ScrollPageViewEditor EditorValue
	{
		get
		{
			if (_editorValue == null)
			{
				_editorValue = GetComponent<ScrollPageViewEditor>();
			}
			return _editorValue;
		}
	}

	public bool DebugMode { get { return EditorValue._debugMode; } }
	public bool PageMode { get { return EditorValue._pageMode; } }

	//用于分页定位和动画
	private int _preNearestPageIndex;
	private int _pageCount;

	public RectTransform ScrollTargetPos { get { return EditorValue._scrollTargetPos; } }
	public float PosTweenDuration { get { return EditorValue._posTweenDuration; } }
	public Button PrePageBtn { get { return EditorValue._prePageBtn; } }
	public Button NextPageBtn { get { return EditorValue._nextPageBtn; } }

	//源cell
	public GameObject CellSource { get { return EditorValue._cellSource; } set { EditorValue._cellSource = value; } }
	public int CellCount { get { return EditorValue._cellCount; } set { EditorValue._cellCount = value; } }

	//用于设置cell grid
	public Vector3 CellSize { get; set; }
	public int RowCount { get { return EditorValue._rowCount; } set { EditorValue._rowCount = value; } }
	public int ColumnCount { get { return EditorValue._columnCount; } set { EditorValue._columnCount = value; } }
	public Vector2 Spacing { get { return EditorValue._spacing; } set { EditorValue._spacing = value; } }

	//cell更新回调
	private Action<int, ScrollPageViewCell> _onRefresh;
	public Action<int, ScrollPageViewCell> OnRefresh
	{
		get { return _onRefresh; }
		set { _onRefresh = value; }
	}

	//page更替回调
	private Action<int> _onPageIndexChanged;
	public Action<int> OnPageIndexChanged
	{
		get { return _onPageIndexChanged; }
		set { _onPageIndexChanged = value; }
	}

	public List<ScrollPageViewCell> _cells = new List<ScrollPageViewCell>();
	public List<ScrollPageViewCell> _cellCaches = new List<ScrollPageViewCell>();
	//记录每个cell的状态，
	private List<ReusingData> _indexList = new List<ReusingData>();

	private RectTransform _rectTransform;
	private Bounds _viewBounds;
	public Vector2 Pivot { get; private set; }
	public Vector2 MinAnchors { get; private set; }
	public Vector2 MaxAnchors { get; private set; }

	/// <summary>
	/// 初始化
	/// </summary>
	/// <param name="cellCount"></param>
	public void Init(int cellCount)
	{
		CellCount = cellCount;

		if (CellSource == null)
		{
			Debug.Log("Error : CellSource is null.");
			return;
		}

		if (content == null)
		{
			Debug.Log("Error : content is null.");
			return;
		}

		if (PageMode)
		{
			this.movementType = MovementType.Unrestricted;
		}

		if (NextPageBtn != null)
		{
			NextPageBtn.onClick.RemoveAllListeners();
			NextPageBtn.onClick.AddListener(GoNextCell);
		}

		if (PrePageBtn != null)
		{
			PrePageBtn.onClick.RemoveAllListeners();
			PrePageBtn.onClick.AddListener(GoPreCell);
		}

		if (vertical)
		{
			Pivot = new Vector2(0f, 1f);
			MinAnchors = new Vector2(0f, 1f);
			MaxAnchors = new Vector2(1f, 1f);
		}
		else
		{
			Pivot = new Vector2(0f, 1f);
			MinAnchors = new Vector2(0f, 0f);
			MaxAnchors = new Vector2(0f, 1f);
		}

		Rebuild(CanvasUpdate.Layout);

		UpdateViewBounds();
		SetContentLayout();

		_rectTransform = GetComponent<RectTransform>();
		CellSize = CellSource.GetComponent<RectTransform>().sizeDelta;

		if (vertical)
		{
			RowCount = Mathf.CeilToInt((float)CellCount / ColumnCount);
			_pageCount = RowCount;
		}
		else
		{
			ColumnCount = Mathf.CeilToInt((float)CellCount / RowCount);
			_pageCount = ColumnCount;
		}

		UpdateIndexList(CellCount);
		UpdateConetntSize(CellCount);
		UpdateRollBtn(_preNearestPageIndex);

		while (_cells.Count > 0)
		{
			HideCell(_cells[0]._index, false);
		}
	}

	/// <summary>
	/// 初始化，包括cellSource, 因此会删掉之前的所有cell, 因为cell既然改变了, 表示之前加载的cell不能复用了.
	/// </summary>
	/// <param name="cellSource"></param>
	/// <param name="cellCount"></param>
	public void Init(GameObject cellSource, int cellCount)
	{
		CellSource = cellSource;
		ClearCells();

		Init(cellCount);
	}

	/// <summary>
	/// 初始化，在cellCount，cellSource都设置完毕的情况下初始化。用于编辑器测试。
	/// </summary>
	[ContextMenu("Init")]
	public void Init()
	{
		Init(CellCount);
	}

	/// <summary>
	/// 刷新当前显示的cells
	/// </summary>
	public void RefreshAll()
	{
		if(_onRefresh == null)
			return;

		foreach (var cell in _cells)
		{
			_onRefresh(cell._index, cell);
		}
	}

	/// <summary>
	/// 重置Content坐标
	/// </summary>
	public void ResetPos()
	{
		SetContentAnchoredPosition(Vector2.zero);
	}

	/// <summary>
	/// 清理加载的cells，Init时调用，即sourcecell发生改变，无法复用
	/// </summary>
	public void ClearCells()
	{
		foreach (var i in _cells)
		{
			Destroy(i.gameObject);
		}

		foreach (var i in _cellCaches)
		{
			Destroy(i.gameObject);
		}

		_cells.Clear();
		_cellCaches.Clear();
	}

	/// <summary>
	/// 跳转到Page
	/// </summary>
	/// <param name="pageIndex"></param>
	public void ScrollToPage(int pageIndex)
	{
		ScrollToPage(pageIndex, PosTweenDuration);
	}

	public void ScrollToPage(int pageIndex, float duration)
	{
		pageIndex = Mathf.Clamp(pageIndex, 0, _pageCount - 1);

		if (vertical)
		{
			ScrollToIndex(pageIndex * ColumnCount, duration);
		}
		else
		{
			ScrollToIndex(pageIndex * RowCount, duration);
		}

		if (_preNearestPageIndex != pageIndex)
		{
			if (_onPageIndexChanged != null)
			{
				_onPageIndexChanged(pageIndex);
			}
		}

		_preNearestPageIndex = pageIndex;
		UpdateRollBtn(pageIndex);
	}

	/// <summary>
	/// 跳转index
	/// </summary>
	/// <param name="index"></param>
	public void ScrollToIndex(int index)
	{
		ScrollToIndex(index, PosTweenDuration);
	}

	/// <summary>
	/// 跳转到index
	/// </summary>
	/// <param name="realIndex"></param>
	/// <param name="delay"></param>
	/// <param name="duration"></param>
	public void ScrollToIndex(int index, float duration)
	{
		index = Mathf.Clamp(index, 0, CellCount - 1);

		Vector2 from = this.content.anchoredPosition;
		Vector2 to;
		if (vertical)
		{
			to = new Vector2(this.content.anchoredPosition.x, GetOffsetToTarget(index).y);
		}
		else
		{
			to = new Vector2(GetOffsetToTarget(index).x, this.content.anchoredPosition.y);
		}

		//duration够小时，tweener不会play，导致Update中的SetCellDisplay不被执行，content没更新
		if (duration == 0f)
		{
			SetContentAnchoredPosition(to);
		}
		else
		{
			TweenPosTo(from, to, duration);
		}
	}

	public void GoNextCell()
	{
		ScrollToPage(_preNearestPageIndex + 1);
	}

	public void GoPreCell()
	{
		ScrollToPage(_preNearestPageIndex - 1);
	}

	void UpdateRollBtn(int pageIndex)
	{
		if (PrePageBtn != null)
		{
			PrePageBtn.gameObject.SetActive(pageIndex > 0);
		}

		if (NextPageBtn != null)
		{
			if (vertical)
			{
				NextPageBtn.gameObject.SetActive(pageIndex < RowCount - 1);
			}
			else
			{
				NextPageBtn.gameObject.SetActive(pageIndex < ColumnCount - 1);
			}
		}
	}

	bool m_rebuild = false;
	bool isPlaying = false;
	Vector2 to = Vector2.zero;
	Vector2 from = Vector2.zero;
	float totalTime = 0f;
	float duration = 0f;

	private void TweenPosTo(Vector2 from, Vector2 to, float duration)
	{
		content.anchoredPosition = from;
		this.to = to;
		this.from = from;
		isPlaying = true;
		totalTime = 0f;
		this.duration = duration;
	}

	public void Update()
	{
		if (m_rebuild)
		{
			m_rebuild = false;
			SetCellDisplay();
		}
		else
		if (isPlaying)
		{
			if (vertical)
			{
				content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Lerp(from.y, to.y, totalTime / duration));
			}
			else
			{
				content.anchoredPosition = new Vector2(Mathf.Lerp(from.x, to.x, totalTime / duration), content.anchoredPosition.y);
			}
			SetCellDisplay();
			if (totalTime >= duration)
			{
				content.anchoredPosition = to;
				isPlaying = false;
			}
			else
			{
				totalTime += Time.deltaTime;
			}
		}
	}

	/// <summary>
	/// scrollview触发显示会rebuild
	/// </summary>
	/// <param name="executing"></param>
	public override void Rebuild(CanvasUpdate executing)
	{
		base.Rebuild(executing);

		UpdateViewBounds();
		m_rebuild = true;
	}

	/// <summary>
	/// scrollbar 改变 value
	/// </summary>
	/// <param name="value"></param>
	/// <param name="axis"></param>
	protected override void SetNormalizedPosition(float value, int axis)
	{
		base.SetNormalizedPosition(value, axis);
		SetCellDisplay();
	}

	/// <summary>
	/// content的 anchoredPosition改变的回调
	/// </summary>
	/// <param name="position"></param>
	protected override void SetContentAnchoredPosition(Vector2 position)
	{
		base.SetContentAnchoredPosition(position);
		SetCellDisplay();
	}

	/// <summary>
	/// 鼠标滚轮改变content回调
	/// </summary>
	/// <param name="data"></param>
	public override void OnScroll(UnityEngine.EventSystems.PointerEventData data)
	{
		if (PageMode)
		{
			if (!isPlaying)
			{
				if (data.scrollDelta.y < 0)
					GoNextCell();
				else
					GoPreCell();
			}
		}
		else
		{
			base.OnScroll(data);
			SetCellDisplay();
		}
	}

	/// <summary>
	/// 拖拽结束回调，用于页面定位
	/// </summary>
	/// <param name="eventData"></param>
	public override void OnEndDrag(PointerEventData eventData)
	{
		base.OnEndDrag(eventData);

		if (!PageMode)
			return;

		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		StopMovement();

		int curNearestPageIndex = GetNearsetPageIndex();
		if (vertical)
		{
			if (_preNearestPageIndex == curNearestPageIndex && Mathf.Abs(eventData.delta.y) > 0.1f)
			{
				curNearestPageIndex += (int)Mathf.Sign(eventData.delta.y);
				curNearestPageIndex = Mathf.Clamp(curNearestPageIndex, 0, RowCount - 1);
			}
		}
		else
		{
			if (_preNearestPageIndex == curNearestPageIndex && Mathf.Abs(eventData.delta.x) > 0.1f)
			{
				curNearestPageIndex -= (int)Mathf.Sign(eventData.delta.x);
				curNearestPageIndex = Mathf.Clamp(curNearestPageIndex, 0, ColumnCount - 1);
			}
		}

		ScrollToPage(curNearestPageIndex);
	}

	Vector3 GetOffsetToTarget(int index)
	{
		Vector3 pos = ScrollTargetPos.anchoredPosition;
		return -GetCellPos(index) + pos;
	}

	/// <summary>
	/// 离targetPos最近的cell index
	/// </summary>
	/// <returns></returns>
	int GetNearsetPageIndex()
	{
		float minDistance = float.MaxValue;
		int nearestIndex = 0;

		if (vertical)
		{
			for (int i = 0; i < RowCount; ++i)
			{
				float cellPosY = GetCellPos(i * ColumnCount).y;
				float distance = cellPosY + content.anchoredPosition.y - ScrollTargetPos.anchoredPosition.y;

				if (Mathf.Abs(distance) < Mathf.Abs(minDistance))
				{
					minDistance = distance;
					nearestIndex = i;
				}
			}
		}
		else
		{
			for (int i = 0; i < ColumnCount; ++i)
			{
				float cellPosX = GetCellPos(i * RowCount).x;
				float distance = cellPosX + content.anchoredPosition.x - ScrollTargetPos.anchoredPosition.x;

				if (Mathf.Abs(distance) < Mathf.Abs(minDistance))
				{
					minDistance = distance;
					nearestIndex = i;
				}
			}
		}

		return nearestIndex;
	}

	/// <summary>
	/// 设置Content的RectTransform属性
	/// </summary>
	void SetContentLayout()
	{
		content.anchorMin = MinAnchors;
		content.anchorMax = MaxAnchors;
		content.pivot = Pivot;
		content.anchoredPosition3D = Vector3.zero;
	}

	/// <summary>
	/// 更新显示区域的Bounds
	/// </summary>
	void UpdateViewBounds()
	{
		_viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
	}

	/// <summary>
	/// 更新content的size, [cell size]*[cell count]
	/// </summary>
	/// <param name="count"></param>
	void UpdateConetntSize(int count)
	{
		Vector3 size = CellSize;

		if (vertical)
		{
			size.y = (size.y + Spacing.y) * RowCount;
			size.x = 0;
		}
		else
		{
			size.x = (size.x + Spacing.x) * ColumnCount;
			size.y = 0;
		}

		content.sizeDelta = size;
	}

	/// <summary>
	/// 补足 m_indexList, 每个数据会有对应一个ReusingData,用来记录index的状态
	/// </summary>
	/// <param name="count"></param>
	void UpdateIndexList(int count)
	{
		_indexList.Clear();

		for (int i = 0; i < count; i++)
		{
			ReusingData reusingTmp = null;
			if (_indexList.Count > i)
			{
				reusingTmp = _indexList[i];
			}
			else
			{
				reusingTmp = new ReusingData();
				_indexList.Add(reusingTmp);
			}

			reusingTmp._index = i;
			reusingTmp.status = ReusingStatus.Hide;
		}
	}

	/// <summary>
	/// 更新cell
	/// </summary>
	/// <param name="isRebuild"></param>
	void SetCellDisplay(bool isRebuild = false)
	{
		if (content == null)
			return;

		//计算已显示的哪些需要隐藏
		for (int i = 0; i < _cells.Count; i++)
		{
			//已经完全离开了显示区域
			if (!_viewBounds.Intersects(GetCellBounds(_cells[i]._index)))
			{
				_indexList[_cells[i]._index].status = ReusingStatus.Hide;
			}
		}

		foreach (var indexData in _indexList)
		{
			if (indexData.status == ReusingStatus.Hide)
			{
				HideCell(indexData._index, isRebuild);
			}
		}

		//计算出哪些需要显示
		//如果有未显示的则显示出来，从对象池取出对象
		for (int i = 0; i < _indexList.Count; i++)
		{
			var indexData = _indexList[i];
			if (indexData.status == ReusingStatus.Hide)
			{
				if (_viewBounds.Intersects(GetCellBounds(indexData._index)))
				{
					indexData.status = ReusingStatus.Show;
					ShowCell(i, isRebuild);
				}
			}
		}
	}

	/// <summary>
	/// 显示Cell
	/// </summary>
	/// <param name="index"></param>
	/// <param name="isRebuild"></param>
	/// <param name="data"></param>
	void ShowCell(int index, bool isRebuild)
	{
		ScrollPageViewCell cellTmp = GetCell();

		cellTmp.transform.SetParent(content);
		cellTmp.transform.localScale = Vector3.one;
		cellTmp.RectTransform.anchoredPosition3D = GetCellPos(index);

		if (!isRebuild)
		{
			cellTmp._index = index;
			cellTmp.gameObject.SetActive(true);
			cellTmp.Show();

			if (_onRefresh != null)
			{
				_onRefresh(index, cellTmp);
			}

		}

		_cells.Add(cellTmp);
	}

	/// <summary>
	/// 隐藏cell
	/// </summary>
	/// <param name="index"></param>
	/// <param name="isRebuild"></param>
	void HideCell(int index, bool isRebuild)
	{
		ScrollPageViewCell cellTmp = _cells.Find(i => i._index == index);

		if (cellTmp == null)
			return;

		if (!isRebuild)
		{
			// cellTmp.gameObject.SetActive(false);
			cellTmp.Hide();
		}

		_cells.Remove(cellTmp);
		_cellCaches.Add(cellTmp);
	}

	/// <summary>
	/// 从缓存中获取cell
	/// </summary>
	/// <returns></returns>
	ScrollPageViewCell GetCell()
	{
		ScrollPageViewCell result = null;

		if (_cellCaches.Count > 0)
		{
			//从caches中获取
			result = _cellCaches[0];
			_cellCaches.RemoveAt(0);
		}
		else
		{
			//新建
			var go = Instantiate(CellSource) as GameObject;
			result = go.GetComponent<ScrollPageViewCell>();
		}

		return result;
	}

	/// <summary>
	/// cell在content的坐标
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	Vector3 GetCellPos(int index)
	{
		int cellRow = 0;
		int cellColumn = 0;

		if (vertical && ColumnCount > 0)
		{
			cellRow = index / ColumnCount;
			cellColumn = index % ColumnCount;
		}
		else if (horizontal && RowCount > 0)
		{
			cellColumn = index / RowCount;
			cellRow = index % RowCount;
		}

		Vector3 offset = Vector3.zero;
		offset.x = cellColumn * (CellSize.x + Spacing.x);
		offset.y = cellRow * -(CellSize.y + Spacing.y);

		return offset;
	}

	/// <summary>
	/// 获取cell的Bounds
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	Bounds GetCellBounds(int index)
	{
		return new Bounds(GetCellBoundsPos(index), CellSize);
	}

	/// <summary>
	/// 获取cell坐标, 用于Bounds
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	Vector3 GetCellBoundsPos(int index)
	{
		if (content == null)
		{
			return Vector3.zero;
		}

		return GetCellPos(index) + GetCellBoundsOffset() + content.localPosition;
	}

	/// <summary>
	/// 获取cell中心点, 用于Bounds
	/// </summary>
	/// <returns></returns>
	Vector3 GetCellBoundsOffset()
	{
		Vector3 offset;
		offset = new Vector3(CellSize.x / 2, -CellSize.y / 2, 0);
		return offset;
	}

#if UNITY_EDITOR
	string toIndex = "";
	void OnGUI()
	{
		if (!DebugMode)
		{
			return;
		}

		toIndex = GUILayout.TextField(toIndex);
		if (GUILayout.Button("ScrollToIndex"))
		{
			ScrollToIndex(int.Parse(toIndex));
		}
	}

	// Bounds GetContentBounds()
	// {
	// 	var bounds = new Bounds();
	// 	bounds.size = content.rect.size;
	// 	bounds.center = content.rect.center;
	// 	return bounds;
	// }

	/// <summary>
	/// test,画出cell和view的边框，用于调试
	/// </summary>
	void OnDrawGizmos()
	{
		if (!DebugMode)
		{
			return;
		}

		// Gizmos.color = Color.gray;
		// Gizmos.DrawCube(GetContentBounds().center, GetContentBounds().size);

		Gizmos.color = new Color(1, 0, 0, 0.5f);
		Gizmos.DrawCube(_viewBounds.center, _viewBounds.size);

		Gizmos.color = Color.green;
		Gizmos.DrawCube(GetCellBounds(_preNearestPageIndex).center, GetCellBounds(_preNearestPageIndex).size);

		Gizmos.color = new Color(1, 1, 0, 0.5f);

		for (int i = 0; i < CellCount; i++)
		{
			Gizmos.color -= new Color(0.01f, 0, 0, 0);
			Gizmos.DrawCube(GetCellBounds(i).center, GetCellBounds(i).size);
		}
	}
#endif

}
