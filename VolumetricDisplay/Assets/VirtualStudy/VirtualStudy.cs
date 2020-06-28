using System;
using System.Collections;
using System.IO;

using Biglab.IO.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VirtualStudy
{
    [Serializable]
    public class StudyConfig
    {
        #region Constants

        private const string _configurationFile = "study_config.json";

        private const string _defaultStudyDirectory = "VirtualStudy";

        private const Camera.StereoscopicEye _defaultPreferredEye = Camera.StereoscopicEye.Right;

        private const Camera.StereoscopicEye _defaultPreferredHand = Camera.StereoscopicEye.Right;

        private const int _defaultParticipantId = int.MinValue;

        #endregion

        public string StudyDirectory;

        public Camera.StereoscopicEye PreferredEye;

        public Camera.StereoscopicEye PreferredHand;

        public int ParticipantId;

        public string GetDataFilepath(string filename)
            => Path.Combine(StudyDirectory, GetUniqueFilename(filename));

        public string GetUniqueFilename(string filename, string extension = "csv")
            => $"{filename} - PID {ParticipantId} - {Guid.NewGuid()}.{extension}";

        /// <summary>
        /// Attempts to keep the values reasonable to preventing erroneous like a null string.
        /// </summary>
        private static void Sanitize(ref StudyConfig config)
        {
            if (config == null) { config = CreateDefault(); }

            config.Sanitize();
        }

        /// <summary>
        /// Attempts to keep the values reasonable to preventing erroneous like a null string.
        /// </summary>
        private void Sanitize()
        {
            if (!PreferredEye.Equals(Camera.StereoscopicEye.Left) &&
                !PreferredEye.Equals(Camera.StereoscopicEye.Right))
            {
                PreferredEye = _defaultPreferredEye;
            }

            if (!PreferredHand.Equals(Camera.StereoscopicEye.Left) &&
                !PreferredHand.Equals(Camera.StereoscopicEye.Right))
            {
                PreferredHand = _defaultPreferredHand;
            }

            if (string.IsNullOrEmpty(StudyDirectory))
            {
                StudyDirectory = _defaultStudyDirectory;
            }
        }

        #region Load/Save/Default
        public static StudyConfig Load()
        {
            if (File.Exists(_configurationFile))
            {
                // Read text
                var json = File.ReadAllText(_configurationFile);

                // Parse JSON
                var config = json.DeserializeJson<StudyConfig>();
                Sanitize(ref config);

                return config;
            }

            // No file
            var defaultConfig = CreateDefault();
            defaultConfig.Save();
            return defaultConfig;
        }

        public void Save()
        {
            // Sanitize configuration
            Sanitize();

            // Encode as JSON ( pretty printed )
            File.WriteAllText(_configurationFile, this.SerializeJson(true));
        }

        /// <summary>
        /// Creates a default configuration object.
        /// </summary>
        public static StudyConfig CreateDefault()
            => new StudyConfig
            {
                PreferredEye = _defaultPreferredEye,
                PreferredHand = _defaultPreferredHand,
                ParticipantId = _defaultParticipantId
            };

        #endregion
    }

    /// <summary>
    /// Gets the current configuration file.
    /// </summary>
    public static StudyConfig Config { get; }

    static VirtualStudy()
    {
        // Attempt to load configuration file.
        Config = StudyConfig.Load();

        // Wire up to save config when the scene changes
        SceneManager.activeSceneChanged += ActiveSceneChanged;
    }

    private static void ActiveSceneChanged(Scene prev, Scene next)
        => Config.Save();

    public static IEnumerator Quit(float inSeconds, Text statusText)
    {
        if (statusText == null) { yield return new WaitForSeconds(inSeconds); }
        else
        {
            var startTime = Time.time;
            var endTime = startTime + inSeconds;
            var timeLeft = endTime - Time.time;
            do
            {
                statusText.text = $"Quitting in: {timeLeft:n2} s";
                timeLeft = endTime - Time.time;
                yield return new WaitForEndOfFrame();
            } while (timeLeft > 0);
        }

        Quit();
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
