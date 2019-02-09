using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorNodeCore
{
    [Serializable]
    public abstract class EditorNodeConnection
    {
        public EditorNode targetNode;
    
        public void Draw(Rect rect, Vector2 offset)
        {
            var targetRect = targetNode.CurrentRect(offset);
            var direction = targetRect.xMin > rect.xMin;
    
            var spoint = new Vector2(direction ? rect.xMax : rect.xMin, (rect.yMin + rect.yMax) / 2);
            var epoint = new Vector2(direction ? targetRect.xMin : targetRect.xMax,
                targetRect.yMin + EditorGUIUtility.singleLineHeight * 1.5f);

            var stangent = spoint + Vector2.right * 50 * (direction ? 1 : -1);
            var etangent = epoint + Vector2.right * 50 * (direction ? -1 : 1);
            
            Handles.DrawBezier(spoint,epoint,stangent,etangent,Color.gray, null, 2);
            
            var middle = (Vector3)Vector2.Lerp(spoint, epoint, .5f)+Vector3.forward*10;
            
            //Handles.ArrowHandleCap(0,middle,Quaternion.LookRotation(epoint-spoint), 50, EventType.Repaint);
    
            var oldbg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(.8f, .8f, .8f);
            GUI.Box(rect, "");
            GUI.backgroundColor = oldbg;
    
            using (new GUILayout.AreaScope(rect))
            {
                LayoutContent();
            }
            
            Handles.color = Color.blue;
            Handles.DrawSolidDisc(spoint,Vector3.forward, 5);
            Handles.color = Color.red;
            Handles.DrawSolidDisc(epoint,Vector3.forward, 2);
        }
    
        public abstract float GetContentHeight();
        protected abstract void LayoutContent();
    }
}