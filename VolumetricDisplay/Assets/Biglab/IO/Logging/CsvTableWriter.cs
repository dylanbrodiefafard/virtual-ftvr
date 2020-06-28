using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Biglab.IO.Logging
{
    /// <inheritdoc />
    /// <summary>
    /// Utility class for writing well-formatted CSV files.
    /// </summary> 
    public class CsvTableWriter : TableWriter
        // Author: Christopher Chamberlain - 2017
    {
        [Header("File Configuration")] [Tooltip("Enables prefixing the file name with a date string.")]
        public bool EnableDatePrefix = true;

        [Tooltip("Configures how the date is prefixed using the C# DateTime.ToString string format.")]
        public string DatePrefixFormat = "yyyy.M.dd - HH.mm";

        /// <summary>
        /// Path to the file being written.
        /// </summary>
        public string FilePath
        {
            get { return _path; }
            set { _path = value; }
        }

        [SerializeField] private string _path = "record.csv";

        private List<object[]> _lineBuffer;

        // If the headers have been written already?
        private bool _hasWrittenOnceAlready;

        /// <summary>
        /// The number of lines awaiting to be written to the disk.
        /// </summary>
        public int PendingLines => _lineBuffer.Count;

        private const int _lineBufferCapacity = 100;

        private const string _delimiter = ",";

        protected override void EnableWriter()
        {
            _hasWrittenOnceAlready = false;
            _lineBuffer = new List<object[]>();
        }

        protected override void Commit(ICollection<KeyValuePair<string, object>> row, bool force)
        {
            var line = new List<object>();
            var dict = row.ToDictionary(x => x.Key);

            var sortedKeys = dict.Keys.ToList().OrderBy(key => FieldNames.ToList().IndexOf(key)).ToList();
            foreach (var key in sortedKeys)
            {
                // Have an entry
                line.Add(dict.ContainsKey(key) ? dict[key].Value : string.Empty);
            }

            // Submit line set
            _lineBuffer.Add(line.ToArray());

            // If line buffer grows too large, append to file
            if (_lineBuffer.Count > _lineBufferCapacity || force)
            {
                WriteToFile();
            }
        }

        private void WriteToFile()
        {
            try
            {
                WriteToFile(FilePath);
            }
            catch (Exception e)
            {
                var tempPath = Path.GetTempFileName();
                Debug.LogWarning($"Unable to write to file due to exception: {e}. Attempting to write to temporary file instead: {tempPath}");
                WriteToFile(tempPath);
            }
        }

        /// <summary>
        /// Writes the current line buffer to a file
        /// </summary>
        private void WriteToFile(string path)
        {
            // Ensure file has the CSV extension.
            //var path = Path.ChangeExtension(FilePath, "csv");
            path = Path.ChangeExtension(path, "csv");

            // Construct path, prefixing date if enabled. 
            if (EnableDatePrefix)
            {
                path = PrefixDate(path, DatePrefixFormat);
            }

            // TODO: Validate path?
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Unale to write, file path must be non-null and non-empty");
            }

            var sb = new StringBuilder();

            Debug.Log($"Appending to File '{path}'");
            //foreach( var name in FieldNames )
            //    Debug.LogFormat( "Field: '{0}'", name );

            // Write field names
            if (_hasWrittenOnceAlready == false)
            {
                sb.AppendLine(CreateDelimitedString(FieldNames.ToArray(), _delimiter));
            }

            // Write each line
            foreach (var line in _lineBuffer)
            {
                sb.AppendLine(CreateDelimitedString(line, _delimiter));
            }

            // Write content to the file
            if (_hasWrittenOnceAlready)
            {
                File.AppendAllText(path, sb.ToString());
            }
            else
            {
                File.WriteAllText(path, sb.ToString());
                _hasWrittenOnceAlready = true;
            }

            // 
            _lineBuffer.Clear();
        }

        /// <summary>
        /// Creates a delimited string where each entry is quoted separated by a delimiter.
        /// </summary>
        private static string CreateDelimitedString<T>(T[] objects, string delimiter)
        {
            // 
            if (objects == null)
            {
                throw new ArgumentNullException();
            }

            if (delimiter == null)
            {
                throw new ArgumentNullException();
            }

            //
            var fields = objects.Select(Stringify);
            return string.Join(delimiter, fields.ToArray());
        }

        /// <summary>
        /// Wraps the given object string representation in quotes and escapes existing quotes.
        /// </summary>
        private static string Stringify<T>(T obj)
        {
            // Null, return empty
            if (obj == null)
            {
                return string.Empty;
            }

            // Wrap string representation
            var msg = obj.ToString();
            msg = msg.Replace("\"", "\"\"");
            return $"\"{msg}\"";
        }

        /// <summary>
        /// Prefixes the file name on the given path with the current date string.
        /// </summary>
        private static string PrefixDate(string path, string prefixFormat)
        {
            path = Path.GetFullPath(path);

            // Split path
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);

            // 
            var prefix = $"{DateTime.Now.ToString(prefixFormat)} - ";
            path = dir + Path.DirectorySeparatorChar + prefix + name;

            return path;
        }
    }
}