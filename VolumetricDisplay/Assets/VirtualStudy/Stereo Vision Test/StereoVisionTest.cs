using System;
using System.Collections;
using System.Collections.Generic;

using Biglab.Extensions;
using Biglab.IO.Logging;
using Biglab.Utility;

using UnityEngine;
using UnityEngine.UI;

public class StereoVisionTest : MonoBehaviour
{
    public const float CentimetersPerMeter = 100.0f / 1.0f;
    public Transform LeftEyeAnchor;
    public Transform RightEyeAnchor;

    public GameObject LeftHand;
    public GameObject RightHand;

    public Transform Screen;
    public Vector2Int ScreenResolution;

    public Transform Table;
    public Material StereoScreenMaterial;

    public TableDescription TestDescriptor;

    public Texture2D Disparity1Left;
    public Texture2D Disparity1Right;
    public Texture2D Disparity1Center;
    public Texture2D Disparity2Left;
    public Texture2D Disparity2Right;
    public Texture2D Disparity2Center;
    public Texture2D BlankImage;

    public Text StatusText;

    private enum TargetPosition
    {
        Left,
        Center,
        Right
    }

    private struct StereoTestImage
    {
        public Texture2D Image;
        public TargetPosition Target;
    }

    private List<StereoTestImage> _disparity1Images;
    private List<StereoTestImage> _disparity2Images;

    private static readonly List<float> _disparity1Distances = new List<float>
    {
        268,
        330,
        429,
        537,
        687
    };

    private static readonly List<float> _disparity2Distances = new List<float>
    {
        107,
        171,
        272,
        343,
        429
    };

    private Queue<float> _imageDistances;
    private float _currentImageDistance;
    private StereoTestImage _currentImage;

    private Vector3 _startPosition;

    private TableWriter _writer;

    #region MonoBehaviour

    private void Awake()
    {
        // Setup writer
        _writer = gameObject.AddComponentWithInit<CsvTableWriter>(script =>
            {
                script.TableDescription = TestDescriptor;
                script.EnableDatePrefix = false;
                script.FilePath = VirtualStudy.Config.GetDataFilepath("Stereo Vision Test");
            });

        // Setup Images
        _disparity1Images = new List<StereoTestImage> {
            new StereoTestImage{Image = Disparity1Left, Target = TargetPosition.Left},
            new StereoTestImage{Image = Disparity1Center, Target = TargetPosition.Center},
            new StereoTestImage{Image = Disparity1Right, Target = TargetPosition.Right}
        };

        _disparity2Images = new List<StereoTestImage> {
            new StereoTestImage{ Image = Disparity2Left, Target = TargetPosition.Left },
            new StereoTestImage { Image = Disparity2Center, Target = TargetPosition.Center },
            new StereoTestImage{ Image = Disparity2Right, Target = TargetPosition.Right }
        };

        // Setup Distances
        _imageDistances = new Queue<float>
        (
            new List<float>{
                _disparity2Distances[0],
                _disparity2Distances[1],
                _disparity2Distances[2],
                _disparity2Distances[3],
                _disparity2Distances[4],
                _disparity1Distances[0],
                _disparity1Distances[1],
                _disparity1Distances[2],
                _disparity1Distances[3],
                _disparity1Distances[4]
            }
        );

        // Setup stereo eye callbacks for multi-pass stereo
        foreach (var eyeCallback in FindObjectsOfType<EyeCallback>())
        {
            eyeCallback.RenderingEye += (eye) =>
            {
                StereoScreenMaterial.SetInt("_RightPass", eye.Equals(Camera.MonoOrStereoscopicEye.Right) ? 1 : 0);
            };
        }

        // Setup pointer prefab on preferred hand
        var selectorParier = FindObjectOfType<SelectorHandPairer>();

        if (VirtualStudy.Config.PreferredHand == Camera.StereoscopicEye.Left)
        {
            selectorParier.PairWithLeftHand();
        }
        else
        {
            selectorParier.PairWithRightHand();
        }

        _startPosition = Table.position;
        StereoScreenMaterial.mainTexture = BlankImage;

        StatusText.text = "Select the red button to start";
    }

    #endregion

    #region Disparity Calculations

    private float CurrentRetinalDisparityArcSec
        => CurrentRetinalDisparity * Mathf.Rad2Deg * 3600;

    public float CurrentRetinalDisparity
        => OnRetinaDisparity(OnScreenDisparity(CurrentPixelParallax, Screen.lossyScale.x * CentimetersPerMeter, ScreenResolution.x), _currentImageDistance);

    public int CurrentPixelParallax
        => _disparity1Distances.Contains(_currentImageDistance) ? 1 : 2;

    private static float OnScreenDisparity(int pixelParallax, float widthCm, float horizontalPixels)
        => widthCm / horizontalPixels * pixelParallax;

    private static float OnRetinaDisparity(float onScreenDisparity, float distanceToObserver)
        => 2 * Mathf.Atan(onScreenDisparity / (2 * distanceToObserver));

    #endregion

    public IEnumerator GotoNextImage()
    {
        if (_imageDistances.Count == 0)
        {
            StereoScreenMaterial.mainTexture = BlankImage;
            StartCoroutine(MoveToPosition(Table, _startPosition, 1.0f));
            Debug.Log("All done. Quitting application");
            Scheduler.StartCoroutine(VirtualStudy.Quit(5, StatusText));
            yield break;
        }

        _currentImageDistance = _imageDistances.Dequeue();

        if (_disparity1Distances.Contains(_currentImageDistance))
        {
            _currentImage = _disparity1Images.RandomElement();
        }
        else if (_disparity2Distances.Contains(_currentImageDistance))
        {
            _currentImage = _disparity2Images.RandomElement();
        }
        else
        {
            throw new InvalidOperationException($"Distance {_currentImageDistance} was not found in any disparity list.");
        }

        _writer.SetField("Expected Distance", _currentImageDistance);
        _writer.SetField("Image Name", _currentImage.Image.name);
        _writer.SetField("Disparity (Pixels)", CurrentPixelParallax);
        _writer.SetField("Disparity (Arc Sec)", CurrentRetinalDisparityArcSec);
        _writer.SetField("Target", _currentImage.Target);

        StereoScreenMaterial.mainTexture = _currentImage.Image;

        var targetPosition = Table.position + Table.forward * ((_currentImageDistance - ComputeViewerDistance()) / 100.0f);
        yield return StartCoroutine(MoveToPosition(Table, targetPosition, 2.0f));

        _writer.SetField("Actual Distance", ComputeViewerDistance());
    }

    public IEnumerator MoveToPosition(Transform target, Vector3 position, float timeToMove)
    {
        var originalTexture = StereoScreenMaterial.mainTexture;
        StereoScreenMaterial.mainTexture = BlankImage; // Go blank while moving
        var currentPos = target.position;
        var t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / timeToMove;
            target.position = Vector3.Lerp(currentPos, position, t);
            yield return null;
        }

        StereoScreenMaterial.mainTexture = originalTexture;
    }

    protected float ComputeViewerDistance()
    {
        var leftDistanceMeters = Vector3.Distance(LeftEyeAnchor.position, Screen.position);
        var rightDistanceMeters = Vector3.Distance(RightEyeAnchor.position, Screen.position);
        var meanDistanceMeters = (leftDistanceMeters + rightDistanceMeters) / 2.0f;
        var meanDistanceCentimeters = meanDistanceMeters * CentimetersPerMeter;
        return meanDistanceCentimeters;
    }

    #region EventHandlers

    public void OnLeftSelected()
    {
        Debug.Log("Left Selected");
        if (_currentImage.Image == null || _currentImage.Image == BlankImage) { return; }

        Debug.Log(_currentImage.Target.Equals(TargetPosition.Left) ? "Selected correctly." : "Selected incorrectly.");

        _writer.SetField("Answer", TargetPosition.Left);
        _writer.Commit();

        StartCoroutine(GotoNextImage());
    }

    public void OnCenterSelected()
    {
        Debug.Log("Center Selected");
        if (_currentImage.Image == null || StereoScreenMaterial.mainTexture == BlankImage) { return; }

        Debug.Log(_currentImage.Target.Equals(TargetPosition.Center) ? "Selected correctly." : "Selected incorrectly.");

        _writer.SetField("Answer", TargetPosition.Center);
        _writer.Commit();

        StartCoroutine(GotoNextImage());
    }

    public void OnRightSelected()
    {
        Debug.Log("Right Selected");
        if (_currentImage.Image == null || StereoScreenMaterial.mainTexture == BlankImage) { return; }

        Debug.Log(_currentImage.Target.Equals(TargetPosition.Right) ? "Selected correctly." : "Selected incorrectly.");

        _writer.SetField("Answer", TargetPosition.Right);
        _writer.Commit();

        StartCoroutine(GotoNextImage());
    }

    public void OnNoneSelected()
    {
        Debug.Log("None Selected");

        if (_currentImage.Image == null)
        {
            StatusText.text = string.Empty;
            // No image is set yet, let's start the test!
            StartCoroutine(GotoNextImage());
            return;
        }

        // ignore selections when there is a blank image.
        if (StereoScreenMaterial.mainTexture == BlankImage) { return; }

        _writer.SetField("Answer", "None");
        _writer.Commit();

        StartCoroutine(GotoNextImage());
    }

    #endregion
}
