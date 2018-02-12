using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollCellTest
{
	public string text = "";
	public ScrollCellTest(string t)
	{
		text = t;
	}
}

public class ReuseScrollTest : MonoBehaviour
{
	public ReuseScrollRect scrollRect;
	// Use this for initialization
	void Start()
	{
		var data = new List<ScrollCellTest>();
		for (int i = 0; i < 37; ++i)
		{
			data.Add(new ScrollCellTest("@" + i));
		}

		//Resouces中要有cell的prefab
		scrollRect.Init(data.Count);
		scrollRect.OnRefresh = (index, scrollCell) =>
		{
			scrollCell.gameObject.name = index.ToString();
			scrollCell.transform.Find("Text").GetComponent<Text>().text = index.ToString();

			// Debug.Log("Refresh cell " + index);
		};

		scrollRect.OnPageIndexChanged = (index) =>
		{
			// Debug.Log("To Page-" + index);
		};
	}

	// void OnGUI()
	// {
	// 	if (GUILayout.Button("Reset"))
	// 	{
	// 		var data = new List<ScrollCellTest>();
	// 		for (int i = 0; i < 21; ++i)
	// 		{
	// 			data.Add(new ScrollCellTest("@@" + i));
	// 		}

	// 		//Resouces中要有cell的prefab
	// 		scrollRect.Init(Resources.Load("cell2") as GameObject, data.Count);
	// 		scrollRect._onRefresh = (index, scrollCell) =>
	// 		{
	// 			scrollCell.gameObject.name = index.ToString();
	// 			UITool.SetText(scrollCell.transform.Find("TXT_detail"), data[index].text);
	// 			// Debug.Log("Refresh cell2 " + index);
	// 		};
	// 	}

	// 	if (GUILayout.Button("Refresh"))
	// 	{
	// 		scrollRect.RefreshAll();
	// 	}

	// 	if (GUILayout.Button("ResetPos"))
	// 	{
	// 		scrollRect.ResetPos();
	// 	}
	// }
}
