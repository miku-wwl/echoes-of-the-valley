using System;
using UnityEditor;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>Assigns private [SerializeField] fields through SerializedObject; a mistyped field name throws instead of being skipped.</summary>
    internal static class Wire
    {
        public static void Set(UnityEngine.Object target, string field, object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException(target.GetType().Name + " has no serialized field '" + field + "'");

            switch (value)
            {
                case null: property.objectReferenceValue = null; break;
                case UnityEngine.Object[] objects:
                    property.arraySize = objects.Length;
                    for (int i = 0; i < objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
                    break;
                case UnityEngine.Object asset: property.objectReferenceValue = asset; break;
                case string text: property.stringValue = text; break;
                case float number: property.floatValue = number; break;
                case int integer: property.intValue = integer; break;
                case bool flag: property.boolValue = flag; break;
                case Vector2 vector2: property.vector2Value = vector2; break;
                case Vector3 vector: property.vector3Value = vector; break;
                case Vector4 vector4: property.vector4Value = vector4; break;
                case Color color: property.colorValue = color; break;
                default: throw new ArgumentException("Unsupported value type " + value.GetType());
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
