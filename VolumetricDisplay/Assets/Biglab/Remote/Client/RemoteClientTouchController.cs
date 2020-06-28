using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Biglab.Extensions;
using Biglab.IO.Networking;
using Biglab.IO.Serialization;
using Biglab.Utility;
using UnityEngine;

namespace Biglab.Remote.Client
{
    public class RemoteClientTouchController : MonoBehaviour
    {
        private RemoteClient _rdc;
        private RemoteClientMenuController _menu;

        private RateLimiter _rateLimiter;

        /// <summary>
        /// Converts touches in image to canvas space ( assumes canvas is fullscreen ).
        /// </summary>
        private IEnumerable<RemoteTouch> NormalizeTouchPoints(Touch[] touches)
        {
            var canvasRect = _rdc.Image.canvas.pixelRect;
            var imageRect = _rdc.Image.rectTransform.rect;

            // Uncenter
            imageRect.x += canvasRect.width / 2F;
            imageRect.y += canvasRect.height / 2F;

            foreach (var touch in touches)
            {
                // Remap touch from image to canvas space ( assumes canvas is fullscreen )
                var x = touch.position.x.Rescale(imageRect.xMin, imageRect.xMax, canvasRect.xMin, canvasRect.xMax) / canvasRect.width;
                var y = touch.position.y.Rescale(imageRect.yMin, imageRect.yMax, canvasRect.yMin, canvasRect.yMax) / canvasRect.height;

                // Only process if it is a valid touch ( within image bounds )
                if (x > 0 && x < 1 && y > 0 && y < 1)
                {
                    yield return new RemoteTouch(new Vector2(x, y), touch.fingerId, touch.phase, touch.deltaPosition);
                }
            }
        }

        private bool IsCriticalTouch(Touch[] touches)
        {
            foreach (var touch in touches)
            {
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Began)
                {
                    return true;
                }
            }

            return false;
        }

        #region MonoBehaviour

        IEnumerator Start()
        {
            _rateLimiter = new RateLimiter(1 / 30f);

            yield return null;

            _menu = FindObjectOfType<RemoteClientMenuController>();
            _rdc = FindObjectOfType<RemoteClient>();
        }

        void Update()
        {
            if (Input.touchCount > 0 && !_menu.OverlayActive)
            {
                if (_rateLimiter.CheckElapsedTime() || IsCriticalTouch(Input.touches))
                {
                    var normalizeTouches = NormalizeTouchPoints(Input.touches).ToArray();

                    var data = normalizeTouches.SerializeBytes();
                    if (data.Length > 0)
                    {
                        _rdc.Send(MessageType.TouchEvent, data);
                    }
                }
            }
        }

        #endregion
    }
}