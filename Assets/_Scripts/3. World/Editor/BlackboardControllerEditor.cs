using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace World 
{
    [CustomEditor(typeof(BlackboardController))]
    public class BlackboardControllerEditor : Editor
    {
        Editor dataEditor;

        public override void OnInspectorGUI()
        {
            // Dibuja el inspector normal (la referencia al asset)
            DrawDefaultInspector();

            var controller = (BlackboardController)target;

            // Acceder al BlackboardData
            var blackboardDataField = serializedObject.FindProperty("blackboardData");
            var data = blackboardDataField.objectReferenceValue;

            if (data != null)
            {
                if (dataEditor == null || dataEditor.target != data)
                {
                    dataEditor = CreateEditor(data);
                }

                GUILayout.Space(10);
                GUILayout.Label("Blackboard Data (Inline)", EditorStyles.boldLabel);
                dataEditor.OnInspectorGUI();
            }
        }
    }

}

