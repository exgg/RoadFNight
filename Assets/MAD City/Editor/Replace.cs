using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Replace : EditorWindow
{
    private const float HEIGHT = 175f;
    int chosenTab;
    string[] Toolbarstrings = { "Selected", "All by name"};
    string PrefabName = "";
    
   

    [SerializeField]
    private Object sourceObject;
    private List<Transform> transformz;

    [MenuItem("Tools/MAD City/Replace", false, 1)]
    public static void OpenWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        Replace window = (Replace)EditorWindow.GetWindow(typeof(Replace), true);

        //Options
        window.autoRepaintOnSceneChange = true;
        window.maxSize = new Vector2(237.5f, HEIGHT);
        window.minSize = window.maxSize;
        window.titleContent.image = EditorGUIUtility.IconContent("GameObject Icon").image;
        window.titleContent.text = "Replace selected";

        window.Show();
    }

    private void OnGUI()
    {

        if (Selection.gameObjects.Length == 0)
        {
            EditorGUILayout.HelpBox("Nothing selected", MessageType.Info);
            return;
        }
        else
        {
            GUIContent Arrow = new GUIContent(EditorGUIUtility.FindTexture("tab_next@2x"));


            chosenTab = GUILayout.Toolbar(chosenTab, Toolbarstrings);

            if (chosenTab == 0)
            {
                sourceObject = (Object)EditorGUILayout.ObjectField(sourceObject, typeof(GameObject), true);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(AssetPreview.GetAssetPreview(Selection.activeGameObject), GUILayout.Height(75), GUILayout.Width(75)))
                {
                }

                if (GUILayout.Button(Arrow, GUILayout.Height(75), GUILayout.Width(75)))
                {
                }

                if (GUILayout.Button(AssetPreview.GetAssetPreview(sourceObject), GUILayout.Height(75), GUILayout.Width(75)))
                {
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Replace", GUILayout.Height(50)))
                {
                    Transform[] newPosition = Selection.transforms;

                    
                        foreach (Transform obj in newPosition)
                        {
                            Vector3 newPos = obj.transform.position;
                            Instantiate(sourceObject, newPos, obj.rotation, obj.parent);

                        }
                        foreach (Transform go in Selection.transforms)
                        {
                            DestroyImmediate(go.gameObject);
                        }
                    }
                }
            }
            if (chosenTab ==1)
            {
            if (GUILayout.Button("Get Selected Prefab Name"))
            {
                PrefabName = Selection.activeTransform.name;
            }

            
            //_AllObjects = GUILayout.Toggle(_AllObjects, "All Objects in scene" + "(" + transformz.Count + ")");
            GUILayout.Label("Replace All Prefabs Containing:");
            PrefabName = GUILayout.TextField(PrefabName);
            

            GUILayout.Label("With:");
            sourceObject = (Object)EditorGUILayout.ObjectField(sourceObject, typeof(GameObject), true);

            if (GUILayout.Button("Replace", GUILayout.Height(50)))
            {
                transformz.Clear();
                foreach (Transform gameObj in FindObjectsOfType(typeof(Transform)) as Transform[])
                {
                    if (gameObj.name.Contains(PrefabName))
                    {
                        
                        transformz.Add(gameObj);
                    }
                }
                //Transform[] newPosition = Selection.transforms;
                foreach (Transform obj in transformz)
                {
                    Vector3 newPos = obj.transform.position;
                    Instantiate(sourceObject, newPos, obj.rotation, obj.parent);

                }
                foreach (Transform go in transformz)
                {
                    DestroyImmediate(go.gameObject);
                }
            }
        }
    }

    private void OnSelectionChange()
    {
        this.Repaint();
    }

}


