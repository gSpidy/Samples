using System.Collections;
using System.Collections.Generic;
using EditorNodeCore;
using UnityEditor;
using UnityEngine;


public class ExampleNodeConnection : EditorNodeConnection
{
	public GameObject someReference; 
	
	public override float GetContentHeight()
	{
		return (EditorGUIUtility.singleLineHeight + 2) * 2;
	}
    
	protected override void LayoutContent()
	{
		EditorGUILayout.LabelField("connection body");
		someReference = EditorGUILayout.ObjectField("some ref: ", someReference, typeof(GameObject), true) as GameObject;
	}
}
