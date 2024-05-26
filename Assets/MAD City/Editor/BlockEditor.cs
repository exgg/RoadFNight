using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(_block))]
public class BlockEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        _block _Block = (_block)target;
        
        GUILayout.Space(10);

        GUILayout.BeginVertical("Rotate", "window");
        EditorGUILayout.BeginHorizontal(GUIStyle.none);

        

        if (GUILayout.Button("<--", GUILayout.Height(25)))
        {
            _Block.transform.Rotate(0, -90, 0);
        }
        if (GUILayout.Button("-->", GUILayout.Height(25)))
        {
            _Block.transform.Rotate(0, +90, 0);
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

}
