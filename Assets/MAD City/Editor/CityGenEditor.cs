using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(CityGen))]
public class CityGenEditor : Editor
{
    protected static bool showCityPerimeters = true;
    public RegionsData[] rdata;

    public int __GridX;
    public int __GridZ;
    public int __riverPosition;
    public float gridOffset;
    private CityGen.OverrideClass item;

    public void OnEnable()
    {
        __GridX = PlayerPrefs.GetInt("__GridX");
        __GridZ = PlayerPrefs.GetInt("__GridZ");
    }

    public override void OnInspectorGUI()
    {

        GUILayout.BeginVertical("", "window");
        Sprite prefab = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/MAD City/Resources/TitleBlock1.png", typeof(Sprite));
        GUILayout.Box(prefab.texture,GUILayout.ExpandWidth(true),GUILayout.Height(100)); //Or draw the texture
        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
        //base.OnInspectorGUI();
        EditorStyles.label.wordWrap = true;
        EditorStyles.label.alignment = TextAnchor.MiddleCenter;

        GUILayout.Space(20);
        CityGen myScript = (CityGen)target;

        #region Grid Sizes
        GUILayout.BeginVertical("City Grids", "window");
        EditorGUILayout.LabelField("Selet the base sie of your city" + "\n" + "Information about your city can be found below in the city description");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Grid X: ");
        __GridX = (int)EditorGUILayout.Slider(__GridX, 1, 9);
        myScript.gridX = __GridX;

        if (__GridX % 2 == 0)
        {
            __GridX = __GridX-1;
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Grid Z: ");
        __GridZ = (int)EditorGUILayout.Slider(__GridZ, 1, 9);
        myScript.gridZ = __GridZ;
        GUILayout.EndHorizontal();

        PlayerPrefs.SetInt("__GridX", __GridX);
        PlayerPrefs.SetInt("__GridZ", __GridZ);

        
        gridOffset = myScript.gridOffset;
        
        float XMiles = __GridX * gridOffset / 1609;
        float ZMiles = __GridZ * gridOffset / 1609;
        

        GUILayout.BeginHorizontal();
        GUILayout.Label("Grid Offset: ");
        float __gridOffset = EditorGUILayout.FloatField(myScript.gridOffset);
        myScript.gridOffset = __gridOffset;
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
        GUILayout.Label("Grid Origin: ");
        Vector3 __gridOrigin = EditorGUILayout.Vector3Field("",myScript.gridOrigin);
        myScript.gridOrigin = __gridOrigin;
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

       
        GUILayout.EndVertical();
        #endregion

        GUILayout.Space(20);

        #region City Features

        GUILayout.BeginVertical("City Features", "window");
        EditorGUILayout.LabelField("Select the Features you would like to see in your city");
        GUILayout.Space(10);

        GUILayout.BeginVertical("City Walls", "window");
        GUILayout.BeginHorizontal();
        bool __hasCityWalls = EditorGUILayout.Toggle("City Walls ", myScript.cityEdgeWalls);
        myScript.cityEdgeWalls = __hasCityWalls;

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.BeginVertical("Exit Roads", "window");
        GUILayout.BeginHorizontal();

        bool __hasExit = EditorGUILayout.Toggle("Exit Roads: ", myScript.hasExit);
        myScript.hasExit = __hasExit;

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.BeginVertical("Beaches", "window");
        GUILayout.BeginHorizontal();
        
        bool __hasbeach = EditorGUILayout.Toggle("Beach: ",myScript.hasBeach);
        myScript.hasBeach = __hasbeach;
        
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        
        if (__hasbeach)
        {
            bool __hasPier = EditorGUILayout.Toggle("Piers: ", myScript.hasPier);
            myScript.hasPier = __hasPier;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.BeginVertical("River", "window");
        bool __hasRiver = EditorGUILayout.Toggle("River", myScript.AddRiver);
        myScript.AddRiver = __hasRiver;

        if (__hasRiver)
        {
            bool __riverRandomPosition = EditorGUILayout.Toggle("Random Position", myScript.RiverPositionRandom);
            myScript.RiverPositionRandom = __riverRandomPosition;

            if (!__riverRandomPosition)
            {
                __riverPosition = (int)EditorGUILayout.Slider(__riverPosition, 1, myScript.gridX - 2);
                myScript.RiverPositionX = __riverPosition;
            }
        }

       
            GUILayout.EndVertical();

        //GUILayout.BeginVertical("Ocean", "window");
        //bool __hasOcean = EditorGUILayout.Toggle("Add Ocean", myScript.OceanTile);
       // myScript.OceanTile = __hasOcean;
        //GUILayout.EndVertical();



        GUILayout.EndVertical();
        #endregion

        GUILayout.Space(20);

        #region Regions
        GUILayout.BeginVertical("Regions", "window");
        EditorGUILayout.LabelField
            ("Add the Regions you would like to see in your city."+"\n"+" Region colours match the gizmo volumes in the editor." + "\n" + "You can have up to 8 Regions");
        serializedObject.Update();

        GUI.backgroundColor = Color.blue;
        if (myScript._regions.Count >= 1)
        {
           EditorGUILayout.PropertyField(serializedObject.FindProperty("_regions").GetArrayElementAtIndex(0),true);
        }
        GUI.backgroundColor = Color.red;
        if (myScript._regions.Count >= 2)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_regions").GetArrayElementAtIndex(1),true);
        }
        GUI.backgroundColor = Color.green;
        if (myScript._regions.Count >= 3)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_regions").GetArrayElementAtIndex(2),true);
        }
        GUI.backgroundColor = Color.yellow;
        if (myScript._regions.Count >= 4)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_regions").GetArrayElementAtIndex(3),true);
        }
        if (myScript._regions.Count >= 5)
        {
            GUI.backgroundColor = Color.magenta;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_regions").GetArrayElementAtIndex(4));
        }
        if (myScript._regions.Count >= 6)
        {
            GUI.backgroundColor = Color.cyan;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_regions").GetArrayElementAtIndex(5));
        }
        if (myScript._regions.Count >= 7)
        {
            GUI.backgroundColor = Color.white;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_regions").GetArrayElementAtIndex(6));
        }
        if (myScript._regions.Count >= 8)
        {
            GUI.backgroundColor = Color.black;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_regions").GetArrayElementAtIndex(7));
        }
        GUI.backgroundColor = Color.white;
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.BeginHorizontal();

        GUILayout.Space(20);
        if (GUILayout.Button("+", GUILayout.Height(25)))
            {
                if (myScript._regions.Count < 8)
            { 
                myScript._regions.Add(ScriptableObject.CreateInstance<RegionsData>());
            }
               
            }
        GUILayout.Space(20);
        if (GUILayout.Button("-", GUILayout.Height(25)))
            {
            if (myScript._regions.Count > 1)
            {
                myScript._regions.RemoveAt(myScript._regions.Count - 1);
            }
            }
        GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();
            
        
        GUILayout.EndVertical();
        #endregion

        GUILayout.Space(20);

        #region Override
        GUILayout.BeginVertical("Overrides", "window");
        EditorGUILayout.LabelField
            ("You can replace a block in the city with any gameobject, it could be a customised block or a block that you want to see in a fixed position.");
        serializedObject.Update();

        if (myScript.Overrides.Count >= 1)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Overrides"), true);
        }
        
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.BeginHorizontal();

        GUILayout.Space(20);
        if (GUILayout.Button("+", GUILayout.Height(25)))
        {
            if (myScript.Overrides.Count < 10)
            {
                myScript.Overrides.Add(item);
            }

        }
        GUILayout.Space(20);
        if (GUILayout.Button("-", GUILayout.Height(25)))
        {
            if (myScript.Overrides.Count > 0 )
            {
                myScript.Overrides.RemoveAt(myScript.Overrides.Count -1);
            }
        }
        GUILayout.Space(20);
        EditorGUILayout.EndHorizontal();


        GUILayout.EndVertical();
        #endregion

        GUILayout.Space(20);

        #region Description
        GUILayout.BeginVertical("City Descrition", "window");
        EditorGUILayout.LabelField("This City is " + XMiles + " Miles Wide by " + ZMiles + " Long." + "\n" + "\n" + "It will take around " + __GridX * 10 + " seconds to walk the X direction," + "\n" + "and " + __GridZ * 10 + " seconds to walk the Z direction. " + "\n" + "\n" + "Your city contains " + myScript.listOfBlocks.Length + " city blocks." + "\n" + "  Your city contains " + myScript.listOfBuildings.Length + " buildings.");
        GUILayout.EndVertical();
        #endregion

        GUILayout.Space(20);

        #region Information
        GUILayout.BeginVertical();
        GUILayout.Label("City Information");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("listOfBlocks"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("listOfBuildings"));
        GUILayout.Space(20);
        //GUILayout.Label("Coming Soon...");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("listOfLargeBuildings"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("listOfPOIs"));
        GUILayout.EndVertical();
        #endregion

        GUILayout.Space(20);

        #region Buttons
        EditorGUILayout.LabelField("Generate City");

        if (GUILayout.Button("Generate Your City"))
        {
            myScript.ClearBuildings();
            myScript.ClearBlocks();
            myScript.ClearPerimeter();
            myScript.Generate();
            myScript.GenerateBuildings();
        }


        if (GUILayout.Button("Generate Random City"))
        {
            myScript.ClearBuildings();
            myScript.ClearBlocks();
            myScript.ClearPerimeter();
            myScript.RandomValues();
            myScript.Generate();
            myScript.GenerateBuildings();
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Generate City Parts");

        if (GUILayout.Button("Generate City Blocks"))
        {
            myScript.ClearBuildings();
            myScript.ClearBlocks();
            myScript.ClearPerimeter();
            myScript.Generate();

        }

        
       

        if (GUILayout.Button("Generate Buildings"))
        {
            myScript.ClearBuildings();
            myScript.GenerateBuildings();

        }   

        /*if (GUILayout.Button("Generate POIs"))
        {
            myScript.ClearPOIs();
            myScript.AddPOIs();
        }
        
        if (GUILayout.Button("Combine Meshes"))
        {
            myScript.combineMeshes();
        }

        if (GUILayout.Button("Add Ocean"))
        {
            myScript.AddOcean();
        }*/


        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Remove");

        if (GUILayout.Button("Clear Blocks"))
        {
            myScript.ClearBlocks();
        }

        if (GUILayout.Button("ClearBuildings"))
        {
            myScript.ClearBuildings();
        }

        if (GUILayout.Button("ClearPerimeter"))
        {
            myScript.ClearPerimeter();
        }

        /*
        EditorGUILayout.HelpBox("Clean up removes all building blocks and should only be pressed when your happy with the current layout.", MessageType.Warning);
        if (GUILayout.Button("Clean Up"))
        {
            myScript.CleanUp();
        }*/

        #endregion
    }
}
