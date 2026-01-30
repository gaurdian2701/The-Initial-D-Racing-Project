using System;
using UnityEngine;

namespace ExternalForInspector
{
    /// <summary>
    /// Attribute to make a field show in the inspector only if another boolean field is true.
    /// Usage: [ShowIf("otherBoolFieldName")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        // Drawer expects this exact member name: `conditionalField`
        public string conditionalField;

        // Optional: allow inverting the condition if needed later
        public bool inverse;

        // Constructor: required conditional field name, optional inverse flag
        public ShowIfAttribute(string conditionalField, bool inverse = false)
        {
            this.conditionalField = conditionalField;
            this.inverse = inverse;
        }
    }
}