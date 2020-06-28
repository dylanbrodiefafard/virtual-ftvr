using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biglab.IO.Logging
{
    /// <summary>
    /// Class that describes what columns a table has. 
    /// To use, you can right click within Unity -> Create -> Data -> Table Description.
    /// </summary>
    [CreateAssetMenu(menuName = "Logging/Table Description")]
    public class TableDescription : ScriptableObject
        // Author: Christopher Chamberlain - 2017
    {
        [SerializeField] private List<Field> _fields;

        /// <summary>
        /// All fields/columns this table has.
        /// </summary>
        public IList<Field> Fields => _fields;

        public void Init() 
            => _fields = new List<Field>();

        [Serializable]
        public class Field
        {
            /// <summary>
            /// Column/field name.
            /// </summary>
            [Tooltip("Column name.")] public string Name;

            /// <summary>
            /// Column/field type.
            /// </summary>
            [Tooltip("Column type.")] public FieldType Type;
        }

        public enum FieldType
        {
            String,
            Number
        }
    }
}