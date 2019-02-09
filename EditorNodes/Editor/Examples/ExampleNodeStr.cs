using EditorNodeCore;
using UnityEditor;

public class ExampleNodeStr : EditorNode
{
	public string someText = "some text";
	
	protected override EditorNodeConnection CreateConnection(EditorNode editorNode, EditorNode to)
	{
		return new ExampleNodeConnection();
	}
	
	public override float GetContentHeight()
	{
		return (EditorGUIUtility.singleLineHeight + 2)*10;
	}
	
	internal override void ProcessContextMenu(GenericMenu menu){}
	
	protected override void LayoutContent()
	{
		someText = EditorGUILayout.TextField("some text: ", someText);
	}
}
