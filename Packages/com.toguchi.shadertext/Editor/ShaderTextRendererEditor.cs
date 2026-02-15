using UnityEditor;
using UnityEngine;
using ShaderText;

namespace ShaderText.Editor
{
    [CustomEditor(typeof(ShaderTextRenderer))]
    [CanEditMultipleObjects]
    public class ShaderTextRendererEditor : UnityEditor.Editor
    {
        private SerializedProperty _maxCharacters;
        private SerializedProperty _characterWidth;
        private SerializedProperty _characterHeight;
        private SerializedProperty _characterSpacing;
        private SerializedProperty _text;
        private SerializedProperty _color;
        private SerializedProperty _raycastTarget;

        private void OnEnable()
        {
            _maxCharacters = serializedObject.FindProperty("_maxCharacters");
            _characterWidth = serializedObject.FindProperty("_characterWidth");
            _characterHeight = serializedObject.FindProperty("_characterHeight");
            _characterSpacing = serializedObject.FindProperty("_characterSpacing");
            _text = serializedObject.FindProperty("_text");
            _color = serializedObject.FindProperty("m_Color");
            _raycastTarget = serializedObject.FindProperty("m_RaycastTarget");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_text, new GUIContent("Text"));
            EditorGUILayout.PropertyField(_maxCharacters, new GUIContent("Max Characters"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_characterWidth, new GUIContent("Char Width"));
            EditorGUILayout.PropertyField(_characterHeight, new GUIContent("Char Height"));
            EditorGUILayout.PropertyField(_characterSpacing, new GUIContent("Char Spacing"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_color, new GUIContent("Color"));
            EditorGUILayout.PropertyField(_raycastTarget, new GUIContent("Raycast Target"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Supported Characters", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("0-9  A-Z  SPACE  :  .  -", EditorStyles.miniLabel);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
