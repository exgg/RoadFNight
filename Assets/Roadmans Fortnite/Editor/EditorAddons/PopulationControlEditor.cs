using UnityEditor;
using UnityEngine;

namespace Roadmans_Fortnite.Editor.EditorAddons
{
    [CustomEditor(typeof(Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects.PopulationControl))]
    public class PopulationControlEditor : UnityEditor.Editor
    {
        private const float MinValue = 0;  // Allow zero as the minimum value for each slider
        private const float MaxValue = 100;  // Maximum value for each slider (100%)

        private SerializedProperty _lowerClass;
        private SerializedProperty _middleClass;
        private SerializedProperty _highClass;
        private SerializedProperty _gangsterClass;

        private SerializedProperty _black;
        private SerializedProperty _white;
        private SerializedProperty _asian;
        private SerializedProperty _mixedRace;

        private SerializedProperty _heterosexual;
        private SerializedProperty _homosexual;
        private SerializedProperty _bisexual;
        private SerializedProperty _transsexual;

        private SerializedProperty _standardBehaviour;
        private SerializedProperty _racistBehaviour;
        private SerializedProperty _drunkBehaviour;
        private SerializedProperty _homelessBehaviour;
        private SerializedProperty _druggyBehaviour;

        private SerializedProperty _fat;
        private SerializedProperty _slim;
        private SerializedProperty _muscular;

        private bool[] directSetModes; // Flags for each section to enable/disable direct set mode

        private void OnEnable()
        {
            // Cache properties for quick access
            _lowerClass = serializedObject.FindProperty("lowerClass");
            _middleClass = serializedObject.FindProperty("middleClass");
            _highClass = serializedObject.FindProperty("highClass");
            _gangsterClass = serializedObject.FindProperty("gangsterClass");

            _black = serializedObject.FindProperty("black");
            _white = serializedObject.FindProperty("white");
            _asian = serializedObject.FindProperty("asian");
            _mixedRace = serializedObject.FindProperty("mixedRace");

            _heterosexual = serializedObject.FindProperty("heterosexual");
            _homosexual = serializedObject.FindProperty("homosexual");
            _bisexual = serializedObject.FindProperty("bisexual");
            _transsexual = serializedObject.FindProperty("transsexual");

            _standardBehaviour = serializedObject.FindProperty("standardBehaviour");
            _racistBehaviour = serializedObject.FindProperty("racistBehaviour");
            _drunkBehaviour = serializedObject.FindProperty("drunkBehaviour");
            _homelessBehaviour = serializedObject.FindProperty("homelessBehaviour");
            _druggyBehaviour = serializedObject.FindProperty("druggyBehaviour");

            _fat = serializedObject.FindProperty("fat");
            _slim = serializedObject.FindProperty("slim");
            _muscular = serializedObject.FindProperty("muscular");

            // Initialize direct set mode flags for each section
            directSetModes = new bool[5];
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw sections with sliders and ensure they sum up to 100% independently
            DrawSection("Wealth Class Percentages", 0, _lowerClass, _middleClass, _highClass, _gangsterClass);
            DrawSection("Race Percentages", 1, _black, _white, _asian, _mixedRace);
            DrawSection("Sexuality Percentages", 2, _heterosexual, _homosexual, _bisexual, _transsexual);
            DrawSection("Behaviour Type Percentages", 3, _standardBehaviour, _racistBehaviour, _drunkBehaviour, _homelessBehaviour, _druggyBehaviour);
            DrawSection("Body Type Percentages", 4, _fat, _slim, _muscular);

            EditorGUILayout.Space(15);
            DrawGlobalRandomizeButton(); // Draws the global "Randomize All" button

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSection(string sectionName, int sectionIndex, params SerializedProperty[] properties)
        {
            EditorGUILayout.LabelField(sectionName, EditorStyles.boldLabel);

            float total = 0f;
            foreach (var prop in properties)
            {
                total += prop.floatValue;
            }

            // Draw sliders for each property
            foreach (var prop in properties)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(prop.displayName, GUILayout.MaxWidth(150));
                float previousValue = prop.floatValue;

                // Slider for value between 0 and 100
                prop.floatValue = EditorGUILayout.Slider(prop.floatValue, MinValue, MaxValue);

                if (!directSetModes[sectionIndex] && !Mathf.Approximately(previousValue, prop.floatValue))
                {
                    // If value changed and not in direct set mode, update all other properties proportionally
                    AdjustOtherValues(properties, prop, previousValue, prop.floatValue, total);
                }

                EditorGUILayout.EndHorizontal();
            }

            // Display the total percentage for the group
            float totalPercentage = 0f;
            foreach (var prop in properties)
            {
                totalPercentage += prop.floatValue;
            }

            EditorGUILayout.LabelField("Total: " + totalPercentage.ToString("F0") + "%", EditorStyles.helpBox);
            EditorGUILayout.Space(10);

            // Normalize if needed
            if (!directSetModes[sectionIndex] && !Mathf.Approximately(totalPercentage, 100f))
            {
                NormalizeProperties(properties, totalPercentage);
            }

            // Draw the Direct Set button with a red background if enabled for this section
            GUIStyle directSetButtonStyle = new GUIStyle(GUI.skin.button);
            directSetButtonStyle.normal.textColor = Color.white;
            if (directSetModes[sectionIndex])
            {
                directSetButtonStyle.normal.background = MakeTexture(2, 2, Color.red);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Direct Set Mode", directSetButtonStyle))
            {
                directSetModes[sectionIndex] = !directSetModes[sectionIndex];
            }

            // Add a randomize button for this section only
            if (GUILayout.Button("Randomize Section"))
            {
                RandomizeProperties(properties);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        // Global "Randomize All" button
        private void DrawGlobalRandomizeButton()
        {
            if (GUILayout.Button("Randomize All Sections"))
            {
                // Randomize all sections independently
                RandomizeProperties(_lowerClass, _middleClass, _highClass, _gangsterClass);
                RandomizeProperties(_black, _white, _asian, _mixedRace);
                RandomizeProperties(_heterosexual, _homosexual, _bisexual, _transsexual);
                RandomizeProperties(_standardBehaviour, _racistBehaviour, _drunkBehaviour, _homelessBehaviour, _druggyBehaviour);
                RandomizeProperties(_fat, _slim, _muscular);
            }
        }

        // Adjusts other values proportionally when one value changes
        private void AdjustOtherValues(SerializedProperty[] properties, SerializedProperty changedProp, float previousValue, float newValue, float total)
        {
            // Calculate the difference between the new value and the previous value
            float difference = newValue - previousValue;

            // Calculate the available percentage left
            float availablePercentage = total - previousValue;

            // If difference is positive and total would exceed 100%, reduce the other values proportionally
            if (total + difference > 100f)
            {
                difference = 100f - total;
            }

            // Adjust other properties proportionally
            foreach (var prop in properties)
            {
                if (prop != changedProp)
                {
                    float proportionalReduction = (prop.floatValue / availablePercentage) * difference;
                    prop.floatValue = Mathf.Clamp(prop.floatValue - proportionalReduction, MinValue, MaxValue);
                }
            }
        }

        // Normalizes properties to ensure they sum up to 100%
        private void NormalizeProperties(SerializedProperty[] properties, float total)
        {
            if (total <= 0) return;

            foreach (var prop in properties)
            {
                prop.floatValue = Mathf.Clamp((prop.floatValue / total) * 100f, MinValue, MaxValue);
            }
        }

        // Randomizes properties so they sum up to 100%
        private void RandomizeProperties(params SerializedProperty[] properties)
        {
            float total = 100f;
            foreach (var prop in properties)
            {
                float randomValue = Random.Range(MinValue, MaxValue);
                prop.floatValue = randomValue;
                total -= randomValue;
            }

            // Normalize in case there's any remaining value to ensure total is 100%
            NormalizeProperties(properties, total);
        }

        // Creates a texture with the specified color (used for coloring buttons)
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
