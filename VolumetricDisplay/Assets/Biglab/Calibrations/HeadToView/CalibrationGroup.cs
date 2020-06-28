using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Biglab.IO.Serialization;
using Biglab.Math;
using JetBrains.Annotations;
using UnityEngine;

namespace Biglab.Calibrations.HeadToView
{
    [Serializable]
    public class CalibrationGroup
    {
        [SerializeField] private List<Calibration> _headToViewCalibrations;

        public int Count => _headToViewCalibrations.Count;

        public IEnumerable<Calibration> Calibrations => _headToViewCalibrations.AsReadOnly();

        //private Calibration _averageCalibration;

        public CalibrationGroup(params Calibration[] calibrations)
        {
            _headToViewCalibrations = new List<Calibration>(calibrations);
        }

        public Calibration GetAverageCalibration()
        {

            var averageCalibration = Calibration.CreateIdentity();

            averageCalibration.Error = _headToViewCalibrations.Average(calibration => calibration.Error);

            var offsets = _headToViewCalibrations.Select(calibration => calibration.OffsetInView).ToList();

            averageCalibration.OffsetInView = new Vector3(
                    offsets.Average(offset => offset.x),
                    offsets.Average(offset => offset.y),
                    offsets.Average(offset => offset.z));

            var rotations = _headToViewCalibrations.Select(calibration => calibration.ViewToHeadRotation).ToList();

            averageCalibration.ViewToHeadRotation = MathB.ComputeMeanWeightedRotation(rotations);

            return averageCalibration;
        }

        public CalibrationGroup Clone()
        {
            var group = new CalibrationGroup();

            foreach (var original in _headToViewCalibrations)
            {
                var clone = Calibration.CreateIdentity();
                clone.ViewToHeadRotation = original.ViewToHeadRotation;
                clone.OffsetInView = original.OffsetInView;
                clone.Error = original.Error;

                group.AddCalibration(clone);
            }

            return group;
        }

        public Calibration GetCalibration(int index)
        {
            return _headToViewCalibrations[index];
        }

        public void SetCalibration(int index, Calibration calibration)
        {
            _headToViewCalibrations[index] = calibration;
        }

        public int AddCalibration(Calibration calibration)
        {
            _headToViewCalibrations.Add(calibration);
            return _headToViewCalibrations.Count - 1;
        }

        public static CalibrationGroup LoadFromFile(string filepath)
        {
            var json = File.ReadAllText(filepath);
            return json.DeserializeJson<CalibrationGroup>();
        }

        public static void SaveToFile([NotNull] CalibrationGroup calibrations, [NotNull] string filepath)
        {
            if (calibrations == null)
            {
                throw new ArgumentNullException(nameof(calibrations));
            }

            if (filepath == null)
            {
                throw new ArgumentNullException(nameof(filepath));
            }

            var json = calibrations.SerializeJson(true);
            File.WriteAllText(Path.ChangeExtension(filepath, ".json"), json);
        }
    }
}