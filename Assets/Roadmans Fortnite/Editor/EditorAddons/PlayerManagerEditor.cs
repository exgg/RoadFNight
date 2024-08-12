using Mirror;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using Roadmans_Fortnite.EditorClasses;
using Roadmans_Fortnite.Scripts.Classes.Player.Managers;
using UnityEditor;
using UnityEngine;

namespace Roadmans_Fortnite.Editor.Editor_Addons
{
    [CustomEditor(typeof(PlayerManager))]
    public class PlayerManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PlayerManager playerManager = (PlayerManager)target;

            if (playerManager.classRef != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Player Class Dictionary", EditorStyles.boldLabel);

                foreach (var classRef in playerManager.classRef)
                {
                    classRef.category = (ClassReference.Category)EditorGUILayout.EnumPopup("Category", classRef.category);
                    classRef.key = (ClassReference.Keys)EditorGUILayout.EnumPopup("Key", classRef.key);
                    classRef.aClass = (NetworkBehaviour)EditorGUILayout.ObjectField("Class", classRef.aClass, typeof(NetworkBehaviour), true);
               
                }

                if (GUILayout.Button("Add Class"))
                {
                    playerManager.classRef.Add(new ClassReference());
                }
         
                if(GUILayout.Button("Remove Class"))
                {
                    if (playerManager.classRef.Count > 0)
                    {
                        playerManager.classRef.RemoveAt(playerManager.classRef.Count - 1);
                    }
                }
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
