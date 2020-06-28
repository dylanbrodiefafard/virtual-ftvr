using System.Collections;

using Biglab.Extensions;

using UnityEngine;

public class VirtualSurface : MonoBehaviour
{
    private MaterialPropertyBlock _block;
    private Material _material;
    private Renderer _renderer;
    private Color _originalColor;
    private Color _targetColor;
    private bool _isTransitioning;

    #region MonoBehaviour

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _material = GetComponent<MeshRenderer>().sharedMaterial;
        _block = new MaterialPropertyBlock();

        _originalColor = _material.color;
    }

    private void Update()
    {
        if(_isTransitioning)
        {
            _block.SetColor("_Color", _targetColor);
            _renderer.SetPropertyBlock(_block);
        }

        // TODO: remove this once done testing
        if (Input.GetKeyDown(KeyCode.F5))
        {
            FadeToBlackAndBack(1, 1, 1);
        }
    }

    #endregion

    private IEnumerator Transition(float seconds, Color startColor, Color endColor)
    {
        Debug.Log($"Transition from {startColor} to {endColor} over {seconds} s");

        _isTransitioning = true;
        var startTime = Time.time;
        var endTime = startTime + seconds;

        do
        {
            _targetColor = Color.Lerp(startColor, endColor, Time.time.Rescale(startTime, endTime, 0, 1));

            yield return new WaitForEndOfFrame();
        } while (Time.time < endTime);

        _isTransitioning = false;
    }

    private IEnumerator Transition(float secondsToBlack, float secondsAtBlack, float secondsToBack)
    {
        yield return GetWaitFadeToBlack(secondsToBlack);
        yield return new WaitForSeconds(secondsAtBlack);
        yield return GetWaitFadeToOriginal(secondsToBack);
    }

    public void FadeToBlackAndBack(float secondsToBlack, float secondsAtBlack, float secondsToBack)
    {
        if (_isTransitioning)
        {
            return;
        }

        StartCoroutine(Transition(secondsToBlack, secondsAtBlack, secondsToBack));
    }

    public IEnumerator GetWaitFadeToBlack(float seconds)
    {
        yield return Transition(seconds, _originalColor, Color.black);
    }

    public IEnumerator GetWaitFadeToOriginal(float seconds)
    {
        yield return Transition(seconds, Color.black, _originalColor);
    }

    public IEnumerator GetWaitFadeToBlackAndBack(float secondsToBlack, float secondsAtBlack, float secondsToBack)
    {
        if (_isTransitioning)
        {
            yield break;
        }

        yield return Transition(secondsToBlack, secondsAtBlack, secondsToBack);
    }

}
