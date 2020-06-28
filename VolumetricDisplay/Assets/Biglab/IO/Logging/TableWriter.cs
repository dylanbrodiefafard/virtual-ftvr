using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Biglab.IO.Logging
{
    /// <inheritdoc />
    /// <summary>
    /// An abstract class for writing table formatted data.
    /// See <see cref="T:Biglab.IO.Logging.TableDescription" /> and <see cref="T:Biglab.IO.Logging.CsvTableWriter" />.
    /// </summary>
    public abstract class TableWriter : MonoBehaviour
        // Author: Christopher Chamberlain - 2017
    {
        private Dictionary<string, object> _fields;
        private HashSet<string> _fieldNames;

        /// <summary>
        /// The 
        /// </summary>
        public TableDescription TableDescription;

        public bool AutoCommit;

        public bool IsReady { get; private set; }

        protected IEnumerable<string> FieldNames => _fieldNames;

        protected virtual void Start()
        {
            if (TableDescription == null)
            {
                throw new ArgumentNullException(nameof(TableDescription));
            }


            // Validate each field name
            if (TableDescription.Fields.Select(f => f.Name).Any(string.IsNullOrEmpty))
            {
                throw new ArgumentException("Field name must be non-null and non-empty.");
            }

            // 
            _fields = new Dictionary<string, object>();
            _fieldNames = new HashSet<string>(TableDescription.Fields.Select(x => x.Name));

            //
            EnableWriter();
            IsReady = true;
        }

        /// <summary>
        /// Called when the object is enabled.
        /// Use this to create the lists, buffers, maps, etc.
        /// </summary>
        protected abstract void EnableWriter();

        /// <summary>
        /// Commits a row of information.
        /// </summary>
        /// <param name="row">The row of information.</param>
        /// <param name="force">Force flag, to commit all potentially buffered data to disk.</param>
        protected abstract void Commit(ICollection<KeyValuePair<string, object>> row, bool force);

        /// <summary>
        /// Commits the values in the field set to memory, and clears the field set.
        /// </summary>
        public void Commit(bool force = false)
        {
            if (_fields.Count > 0 || force)
            {
                //Debug.Log( "Commit Writer" );
                //foreach( var name in _Names )
                //    Debug.LogFormat( "Field: '{0}'", name );

                Commit(_fields, force);
                _fields.Clear();
            }
        }

        /// <summary>
        /// Sets a field in the current line/row being written.
        /// </summary>
        /// <param name="fieldName"> Some field name specified in the constructor. </param>
        /// <param name="value"> Some value to write. </param>
        public void SetField(string fieldName, object value)
        {
            // 
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException("Field name must be non-null and non-empty.");
            }

            // Record value
            if (_fieldNames.Contains(fieldName))
            {
                _fields[fieldName] = value;
            }
            // Throw exception
            else
            {
                throw new InvalidOperationException($"Unable to set field '{fieldName}', field name unknown.");
            }
        }

        /// <summary>
        /// Sets a field in the current line/row being written.
        /// </summary>
        /// <param name="fieldName"> Some field name specified in the constructor. </param>
        /// <returns> The value in the given field. </returns>
        public object GetField(string fieldName)
        {
            // 
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException("Field name must be non-null and non-empty.");
            }

            // Return value
            if (_fieldNames.Contains(fieldName))
            {
                if (_fields.ContainsKey(fieldName))
                {
                    return _fields[fieldName];
                }
                else
                {
                    return null;
                }
            }
            // Throw exception
            else
            {
                throw new InvalidOperationException($"Unable to get field '{fieldName}', field name unknown.");
            }
        }

        // Updates changes made this frame.
        private void LateUpdate()
        {
            if(AutoCommit)
            {
                Commit();
            }
        }


        // Forces the commit of content
        private void OnDestroy()
            => Commit(true);

#if UNITY_EDITOR

        [CustomEditor(typeof(TableWriter), true)]
        class TableWriterEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                var writer = target as TableWriter;
                if (writer == null)
                {
                    return;
                }

                DrawDefaultInspector();

                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Table Description", EditorStyles.boldLabel);
                if (writer.TableDescription != null)
                {
                    foreach (var f in writer.TableDescription.Fields)
                    {
                        EditorGUILayout.HelpBox($"{f.Name} ( {f.Type} )", MessageType.None);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select a table description", MessageType.Error);
                }
            }
        }

#endif
    }
}