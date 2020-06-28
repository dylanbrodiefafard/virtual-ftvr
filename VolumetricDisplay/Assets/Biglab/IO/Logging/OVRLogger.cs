using System;
using System.Collections;
using System.IO;

using Biglab.Extensions;
using Biglab.IO.Logging;
using Biglab.Utility;

using UnityEngine;
using UnityEngine.SceneManagement;

public class OVRLogger : MonoBehaviour
{
    [Header("Writer")]
    public string Directory;
    public int RecordsPerSecond;

    private const string _headPositionFieldName = "Head Position";
    private const string _headRotationFieldName = "Head Rotation";
    private const string _headSpeedFieldName = "Head Speed (m/s)";
    private const string _headVelocityFieldName = "Head Velocity (m/s)";
    private const string _headAccelerationFieldName = "Head Acceleration (m/s2)";
    private const string _headAngularVelocityFieldName = "Head Angular Velocity (deg/s)";
    private const string _leftViewpointPositionFieldName = "Left Viewpoint Position";
    private const string _leftViewpointRotationFieldName = "Left Viewpoint Rotation";
    private const string _rightViewpointPositionFieldName = "Right Viewpoint Position";
    private const string _rightViewpointRotationFieldName = "Right Viewpoint Rotation";
    private const string _viewpointSeparationFieldName = "Viewpoint Separation";
    private const string _timeFieldName = "Time (s)";

    private const string _numberFormat = "G4";

    private VelocityTracker _velocityTracker;
    private Transform _head => _rig.centerEyeAnchor;

    private Vector3 _leftViewpointPosition;
    private Vector3 _rightViewpointPosition;
    private Vector3 _leftViewpointRotation;
    private Vector3 _rightViewpointRotation;

    private OVRCameraRig _rig;
    private TableDescription _viewerDescription;
    private CsvTableWriter _writer;
    private bool _isReady;

    private void OnAnchorsUpdated()
    {
        _leftViewpointPosition = _rig.leftEyeAnchor.position;
        _leftViewpointRotation = _rig.leftEyeAnchor.rotation.eulerAngles;

        _rightViewpointPosition = _rig.rightEyeAnchor.position;
        _rightViewpointRotation = _rig.rightEyeAnchor.rotation.eulerAngles;
    }

    private IEnumerator Start()
    {
        // Wait for an OVR camera rig
        do
        {
            _rig = FindObjectOfType<OVRCameraRig>();
            yield return new WaitForEndOfFrame();
        } while (_rig == null);

        _rig.UpdatedAnchors += rig => OnAnchorsUpdated();

        _velocityTracker = _head.gameObject.GetOrAddComponent<VelocityTracker>();
        _velocityTracker.SmoothingFactor = RecordsPerSecond; // Smooth things over a second

        _isReady = true;
    }

    private void OnEnable()
    {
        _viewerDescription = ScriptableObject.CreateInstance<TableDescription>();
        _viewerDescription.Init();

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _timeFieldName,
            Type = TableDescription.FieldType.Number
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _headPositionFieldName,
            Type = TableDescription.FieldType.String
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _headRotationFieldName,
            Type = TableDescription.FieldType.String
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _headSpeedFieldName,
            Type = TableDescription.FieldType.Number
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _headVelocityFieldName,
            Type = TableDescription.FieldType.String
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _headAccelerationFieldName,
            Type = TableDescription.FieldType.String
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _headAngularVelocityFieldName,
            Type = TableDescription.FieldType.Number
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _leftViewpointPositionFieldName,
            Type = TableDescription.FieldType.String
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _leftViewpointRotationFieldName,
            Type = TableDescription.FieldType.String
        });


        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _rightViewpointPositionFieldName,
            Type = TableDescription.FieldType.String
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _rightViewpointRotationFieldName,
            Type = TableDescription.FieldType.String
        });

        _viewerDescription.Fields.Add(new TableDescription.Field
        {
            Name = _viewpointSeparationFieldName,
            Type = TableDescription.FieldType.Number
        });

        _writer = gameObject.AddComponent<CsvTableWriter>();
        _writer.TableDescription = _viewerDescription;
        _writer.EnableDatePrefix = false;
        _writer.AutoCommit = false;
        _writer.FilePath = Path.Combine(Directory, $"{SceneManager.GetActiveScene().name} - {gameObject.name} - {Guid.NewGuid()}.csv");

        StartCoroutine(UpdateRecord());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (_viewerDescription != null)
        {
            Destroy(_viewerDescription);
        }

        if (_writer != null)
        {
            Destroy(_writer);
        }
    }

    private IEnumerator UpdateRecord()
    {
        yield return new WaitUntil(() => _writer.IsReady && _isReady);

        while (enabled && _writer != null)
        {
            _writer.SetField(_timeFieldName, Time.time);

            // Write Head related fields
            _writer.SetField(_headPositionFieldName, _head.position.ToString(_numberFormat));
            _writer.SetField(_headRotationFieldName, _head.rotation.eulerAngles.ToString(_numberFormat));
            _writer.SetField(_headSpeedFieldName, _velocityTracker.Speed);
            _writer.SetField(_headVelocityFieldName, _velocityTracker.Velocity.ToString(_numberFormat));
            _writer.SetField(_headAngularVelocityFieldName, _velocityTracker.AngularVelocity);
            _writer.SetField(_headAccelerationFieldName, _velocityTracker.Acceleration.ToString(_numberFormat));

            // Write left viewpoint related fields
            _writer.SetField(_leftViewpointPositionFieldName, _leftViewpointPosition.ToString(_numberFormat));
            _writer.SetField(_leftViewpointRotationFieldName, _leftViewpointRotation.ToString(_numberFormat));

            // Write right viewpoint related fields
            _writer.SetField(_rightViewpointPositionFieldName, _rightViewpointPosition.ToString(_numberFormat));
            _writer.SetField(_rightViewpointRotationFieldName, _rightViewpointRotation.ToString(_numberFormat));

            // Write stereo related fields
            _writer.SetField(_viewpointSeparationFieldName, Vector3.Distance(_leftViewpointPosition, _rightViewpointPosition));

            // Record
            _writer.Commit();

            yield return new WaitForSeconds(1f / RecordsPerSecond);
        }
    }
}

