using System.Collections;
using System.Collections.Generic;
using EditorNodeCore;
using UnityEditor;
using UnityEngine;

public class ExampleNodeRef : EditorNode {
	public GameObject someReference;
	
	protected override EditorNodeConnection CreateConnection(EditorNode editorNode, EditorNode to)
	{
		return new ExampleNodeConnection();
	}
	
	public override float GetContentHeight()
	{
		return (EditorGUIUtility.singleLineHeight + 2)*2;
	}
	
	internal override void ProcessContextMenu(GenericMenu menu)
	{
	}
	
	protected override void LayoutContent()
	{
		EditorGUILayout.LabelField("node body");
		someReference = EditorGUILayout.ObjectField("some ref: ", someReference, typeof(GameObject), true) as GameObject;
	}
}
