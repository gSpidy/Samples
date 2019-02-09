using System;
using System.Collections.Generic;
using System.Linq;
using RectEx;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EditorNodeCore
{
	[Serializable]
	public abstract class EditorNode
	{
		public string name = Random.Range(0, 1000).ToString();
		public Vector2 position;
		public float nodeWidth = 250;
		public List<EditorNodeConnection> connections = new List<EditorNodeConnection>();
		public bool renaming = false;
	
		public EditorNodeConnection Connect(EditorNode other)
		{
			var connection = CreateConnection(this,other);
			if (connection != null)
			{
				connection.targetNode = other;
				connections.Add(connection);
			}
			return connection;
		}
	
		public void Disconnect(EditorNodeConnection connection)
		{
			connections.Remove(connection);
		}
	
		public void Disconnect(EditorNode other)
		{
			connections.Where(x => x.targetNode == other)
				.ToList()
				.ForEach(x =>
				{
					x.targetNode = null;
					Disconnect(x);
				});
		}
	
		public Rect CurrentRect(Vector2 offset)
		{
			return new Rect(position + offset,
				new Vector2(nodeWidth,
					connections.Aggregate(EditorGUIUtility.singleLineHeight + 2 + GetContentHeight(),
						(x, connection) => x + EditorGUIUtility.singleLineHeight + 2 + connection.GetContentHeight())));
		}
	
		public bool MouseOver(Vector2 point, Vector2 offset)
		{
			return CurrentRect(offset).Contains(point);
		}
	
		public bool MouseOverHeader(Vector2 point, Vector2 offset)
		{
			return !renaming && CurrentRect(offset).FirstLine(EditorGUIUtility.singleLineHeight + 2).Contains(point);
		}
	
		internal void Draw(Vector2 offset)
		{
			var rect = CurrentRect(offset);
			var shadowRect = new Rect(rect);
			shadowRect.center += Vector2.one * 5;
	
			EditorGUI.DrawRect(shadowRect, new Color(0, 0, 0, .15f));
			GUI.Box(rect, "");
			EditorGUI.DrawRect(rect.FirstLine(EditorGUIUtility.singleLineHeight + 2), Color.gray);
	
			var oldcc = GUI.contentColor;
			GUI.contentColor = new Color(.95f, .95f, .95f);
	
			using (new GUILayout.AreaScope(rect))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.Space();
	
					if (renaming)
					{
	
						var newName = EditorGUILayout.DelayedTextField("name:", name);
						if (newName != name)
						{
							renaming = false;
							name = newName;
						}
	
					}
					else
					{
						EditorGUILayout.LabelField(name + " node");
					}
	
					EditorGUILayout.Space();
				}
			}
	
			GUI.contentColor = oldcc;
	
			rect.yMin += EditorGUIUtility.singleLineHeight + 2;
	
			var heights = new[] {GetContentHeight()}
				.Concat(connections.Select(x => EditorGUIUtility.singleLineHeight + 2 + x.GetContentHeight())).ToArray();
	
			var curRect = rect.FirstLine(heights[0]);
			using (new GUILayout.AreaScope(curRect))
			{
				LayoutContent();
			}
	
			var tmpConnections = new List<EditorNodeConnection>(connections);
	
			for (int i = 0; i < tmpConnections.Count; i++)
			{
				rect.yMin += heights[i];
				curRect = rect.FirstLine(heights[i + 1]);
	
				using (new GUILayout.AreaScope(curRect))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.LabelField(">> " + tmpConnections[i].targetNode.name);
						EditorGUILayout.Space();
	
						if (GUILayout.Button("X", GUILayout.MaxWidth(20), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)))
						{
							Disconnect(tmpConnections[i]);
							continue;
						}
					}
				}
	
				curRect.yMin += EditorGUIUtility.singleLineHeight + 2;
	
				tmpConnections[i].Draw(curRect, offset);
			}
		}

		protected abstract EditorNodeConnection CreateConnection(EditorNode from,EditorNode to);
		public abstract float GetContentHeight();
		internal abstract void ProcessContextMenu(GenericMenu menu);
		protected abstract void LayoutContent();
	}
}