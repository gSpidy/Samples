using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EditorNodeCore
{
	public abstract class EditorNodeWindow : EditorWindow
	{
		public List<EditorNode> nodes = new List<EditorNode>();
		private Vector2 offset;
		
		public void RemoveNode(EditorNode node)
		{
			nodes.Remove(node);
			nodes.ForEach(x => x.Disconnect(node));
		}
	
		private void OnGUI()
		{
			ProcessEvents();
			Render();
		}
	
		void Render()
		{
			nodes.ForEach(x => x.Draw(offset));
		}
	
		void ProcessEvents()
		{
			var evt = Event.current;
	
			if (!evt.isMouse) return;
	
			switch (evt.button)
			{
				case 0:
					ProcessMoveEvents(evt);
					break;
				case 1:
					ProcessConnectEvents(evt);
					break;
			}
		}
	
		[NonSerialized] private EditorNode draggedNode;
	
		void ProcessMoveEvents(Event evt)
		{
			var overNode = nodes.Any(x => x.MouseOver(evt.mousePosition, offset));
	
			switch (evt.type)
			{
				case EventType.mouseDown:
					draggedNode = nodes.LastOrDefault(x => x.MouseOverHeader(evt.mousePosition, offset));
					if (draggedNode != null) evt.Use();
					break;
				case EventType.mouseUp:
					if (draggedNode != null)
					{
						draggedNode = null;
						evt.Use();
					}
					break;
				case EventType.mouseDrag:
					if (draggedNode != null)
					{
						draggedNode.position += evt.delta;
					}
					else
					{
						if (overNode) break;
						offset += evt.delta;
					}
	
					Repaint();
					evt.Use();
					break;
			}
		}
	
		[NonSerialized] private EditorNode inConnectionNode;
	
		void ProcessConnectEvents(Event evt)
		{
			switch (evt.type)
			{
				case EventType.MouseDown:
					inConnectionNode = nodes.LastOrDefault(x => x.MouseOver(evt.mousePosition, offset));
					if (inConnectionNode != null) evt.Use();
					break;
				case EventType.MouseUp:
					var outNode = nodes.LastOrDefault(x => x.MouseOver(evt.mousePosition, offset));
	
					if (outNode == null) //global context
					{
						var menu = new GenericMenu();
						ProcessContextMenu(menu, evt.mousePosition - offset);
						menu.ShowAsContext();
	
						evt.Use();
						break;
					}
	
					if (outNode == inConnectionNode) //clicked node context
					{
						var menu = new GenericMenu();
						menu.AddItem(new GUIContent("Rename this node"), false, () => outNode.renaming = true);
						menu.AddSeparator("");
						outNode.ProcessContextMenu(menu);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Remove node"), false, () =>RemoveNode(outNode));
						menu.ShowAsContext();
	
						inConnectionNode = null;
	
						evt.Use();
						break;
					}
	
					inConnectionNode.Connect(outNode);
					inConnectionNode = null;
					evt.Use();
					break;
			}
		}

		public abstract void ProcessContextMenu(GenericMenu menu, Vector2 mpos);
	}
}