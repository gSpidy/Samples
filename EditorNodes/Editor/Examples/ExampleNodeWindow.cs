using System.Collections;
using System.Collections.Generic;
using EditorNodeCore;
using UnityEditor;
using UnityEngine;

public class ExampleNodeWindow : EditorNodeWindow {
	
	[MenuItem("Window/Example node window")]
	public static void Init()
	{
		var wnd = GetWindow<ExampleNodeWindow>();
		wnd.Show();
	}
	
	public override void ProcessContextMenu(GenericMenu menu, Vector2 mpos)
	{
		menu.AddItem(new GUIContent("Add ref node"), false, () => nodes.Add(new ExampleNodeRef{position = mpos}));
		menu.AddItem(new GUIContent("Add str node"), false, () => nodes.Add(new ExampleNodeStr{position = mpos}));
	}
}
