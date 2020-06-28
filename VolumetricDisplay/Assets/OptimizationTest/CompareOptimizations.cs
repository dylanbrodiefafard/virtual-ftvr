using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Biglab.Calibrations.InteractiveVisual;
using Biglab.Extensions;
using Biglab.IO.Logging;
using Biglab.Math;
using UnityEngine;
using Debug = UnityEngine.Debug;
using HVCalibration = Biglab.Calibrations.HeadToView.Calibration;
using Random = UnityEngine.Random;
using TDCalibration = Biglab.Calibrations.TrackingToDisplay.Calibration;

public class CompareOptimizations : MonoBehaviour
{
    public Parameters CalibrationParameters;
    public TableDescription OptimizerTableDescription;
    public int NumSamples = 100;

    private Stopwatch _stopWatch;

    private CsvTableWriter _alglibWriter;

    private static void WriteResultsToTable(TableWriter table,
        TDCalibration gtTd,
        HVCalibration gtHv,
        Optimization.OptimizedCalibrations estimate,
        double elapsedTime)
    {
        var dTd = Vector3.Distance(gtTd.TrackerToDisplayTransformation.ToTranslation(),
            estimate.TrackingToDisplay.TrackerToDisplayTransformation.ToTranslation());

        var rTd = MathB.GeodesicDistanceBetweenRotations(gtTd.TrackerToDisplayRotation,
            estimate.TrackingToDisplay.TrackerToDisplayRotation);

        var dVh = Vector2.Distance(gtHv.OffsetInView,
            estimate.HeadToView.OffsetInView);

        var rVh = MathB.GeodesicDistanceBetweenRotations(gtHv.ViewToHeadRotation,
            estimate.HeadToView.ViewToHeadRotation);

        table.SetField("Time (ms)", elapsedTime);
        table.SetField("dTD (cm)", dTd * 100D);
        table.SetField("rTD (deg)", rTd * Mathf.Rad2Deg);
        table.SetField("dVH (cm)", dVh * 100D);
        table.SetField("rVH (deg)", rVh * Mathf.Rad2Deg);

        table.Commit();
    }

    private void Awake()
    {
        _stopWatch = new Stopwatch();
        // Create writer
        _alglibWriter = new GameObject().AddComponent<CsvTableWriter>();
        _alglibWriter.TableDescription = OptimizerTableDescription;
        _alglibWriter.EnableDatePrefix = false;
        _alglibWriter.FilePath = "";
    }

    // Use this for initialization
    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        // Generate the calibration positions to use for a fake calibration
        var calibPos = Calibrator.GenerateCalibrationPositions(CalibrationParameters);

        for (var i = 0; i < NumSamples + 1; i++)
        {
            var displayRotation = Random.rotation;
            var displayTranslation = Random.rotation * Vector3.up * Random.Range(-5, 5);
            var offset = Random.rotation * Vector3.up * Random.Range(-0.075f, 0.075f);
            var headToViewRotation = Random.rotation;

            List<Vector3> leftPos;
            List<Quaternion> leftRot;
            List<Vector3> rightPos;
            List<Quaternion> rightRot;
            Model.GenerateSyntheticTrackedData(
                displayRotation,
                displayTranslation,
                Vector3.one,
                offset,
                Vector3.zero,
                headToViewRotation,
                Quaternion.identity,
                calibPos,
                new Vector3(0.005f, 0.005f, 0.05f),
                false,
                out leftPos,
                out leftRot,
                out rightPos,
                out rightRot
            );

            var gtTd = TDCalibration.CreateIdentity();
            gtTd.TrackerToDisplayTransformation = Matrix4x4.TRS(displayTranslation, displayRotation, Vector3.one);
            gtTd.Error = 0;

            var gtHv = HVCalibration.CreateIdentity();
            gtHv.OffsetInView = offset;
            gtHv.ViewToHeadRotation = Quaternion.Inverse(headToViewRotation);
            gtHv.Error = 0;

            _stopWatch.Restart();
            var alglibResults = Optimization.Optimize(calibPos, leftPos, leftRot, 1);
            _stopWatch.Stop();

            if (i != 0)
            {
                WriteResultsToTable(_alglibWriter, gtTd, gtHv, alglibResults, _stopWatch.ElapsedMilliseconds);
            }
        }

        Debug.Log("Done");
    }
}