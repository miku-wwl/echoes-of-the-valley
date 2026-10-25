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
                case UnityEngine.Object asset: property.objectReferenceValue = asset; break;
                case string text: property.stringValue = text; break;
                case float number: property.floatValue = number; break;
                case int integer: property.intValue = integer; break;
                case bool flag: property.boolValue = flag; break;
                case Vector3 vector: property.vector3Value = vector; break;
                case Color color: property.colorValue = color; break;
                default: throw new ArgumentException("Unsupported value type " + value.GetType());
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
