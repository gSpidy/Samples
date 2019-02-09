using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomPropertyDrawer(typeof(TranslatedTxt))]
public class TranslatedTxtDrawer : PropertyDrawer {

    string fltr = "";

    public override void OnGUI(Rect pos, SerializedProperty element, GUIContent label) {
        pos.y += 2;

        Translator.Current.LoadTranslation(0);

        EditorGUI.LabelField(new Rect(pos.x, pos.y, 35, EditorGUIUtility.singleLineHeight), "Filter");
        pos.x += 40;
        fltr = EditorGUI.TextField(
            new Rect(pos.x, pos.y, 90, EditorGUIUtility.singleLineHeight), fltr);
        pos.x += 95;       

        var arr = Translator.Current.Translations.Keys.ToList();
        if (fltr != "") arr = arr.FindAll(x => x.Contains(fltr));

        if (arr.Count < 1) arr.Add("NOT FOUND");

        var idx = arr.FindIndex(x => x == element.FindPropertyRelative("text").stringValue);
        if (idx < 0) idx = 0;

        EditorGUI.LabelField(new Rect(pos.x, pos.y, 35, EditorGUIUtility.singleLineHeight), "trID");
        pos.x += 40;

        int newidx = EditorGUI.Popup(
            new Rect(pos.x, pos.y, 120, EditorGUIUtility.singleLineHeight), idx, arr.ToArray());

        element.FindPropertyRelative("text").stringValue = arr[newidx];

    }
}
