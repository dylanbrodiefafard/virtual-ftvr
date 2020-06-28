using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

namespace Biglab.Utility
{
    [CreateAssetMenu(menuName = "Biglab/Custom Input Mapping")]
    public class CustomInput : ScriptableObject
    {
        private const float axisButtonThreshold = 0.5F;

        [SerializeField]
        private List<InputMapping> _inputs;

        private Dictionary<string, ButtonState> _buttonValues;
        private Dictionary<string, float> _axisValues;
        private int _lastFrame = 0;

        private void OnEnable()
        {
            // Create button lookup dictionary
            _axisValues = new Dictionary<string, float>();
            _buttonValues = new Dictionary<string, ButtonState>();

            // TODO: Start coroutine on Scheduler?
        }

        private void OnDisable()
        {
            // TODO: Stop coroutine on Scheduler?
        }

        #region Polling Input State

        private void PollDevices()
        {
            if (_lastFrame != Time.frameCount)
            {
                _lastFrame = Time.frameCount;

                // Update each input 
                foreach (var input in _inputs)
                {
                    // 
                    switch (input.InputType)
                    {
                        //  Axis
                        case InputType.Axis:
                            _axisValues[input.Name] = ReadAxisInput(input);
                            break;

                        // Buttons
                        case InputType.Button:

                            // Update button state
                            UpdateButtonState(input, ReadButtonInput(input));

                            break;

                        // Axis act like buttons
                        case InputType.ButtonAxisNegative:
                        case InputType.ButtonAxisPositive:

                            // 
                            var isAxisPressed =
                                (input.InputType == InputType.ButtonAxisPositive && ReadAxisInput(input) > +axisButtonThreshold) ||
                                (input.InputType == InputType.ButtonAxisNegative && ReadAxisInput(input) < -axisButtonThreshold);

                            // Update button state
                            UpdateButtonState(input, isAxisPressed);

                            break;

                        default:
                            throw new InvalidOperationException("Somehow using a custom input type with an invalid enum.");
                    }
                }
            }
        }

        private void UpdateButtonState(InputMapping input, bool isDown)
        {
            // Get previously known button state
            var prevState = ButtonState.Up;
            if (_buttonValues.ContainsKey(input.Name))
            {
                prevState = _buttonValues[input.Name];
            }

            // Check and adjust button state with previously known state
            if (isDown)
            {
                _buttonValues[input.Name] = ButtonState.Down;

                // Was prevously up, so flag that it changed this frame.
                if (prevState == ButtonState.Up)
                {
                    _buttonValues[input.Name] |= ButtonState.Now;
                }
            }
            else
            {
                // Now in an up state
                _buttonValues[input.Name] = ButtonState.Up;

                // Was prevously down, so flag that it changed this frame.
                if (prevState == ButtonState.Down)
                {
                    _buttonValues[input.Name] |= ButtonState.Now;
                }
            }
        }

        #endregion

        #region Read Input

        private static bool ReadButtonInput(InputMapping input)
        {
            // TODO: Alternate input sources such as Wiimote?
            return Input.GetButton(input.UnityInputName);
        }

        private static float ReadAxisInput(InputMapping input)
        {
            // TODO: Alternate input sources such as Wiimote?
            return Input.GetAxis(input.UnityInputName);
        }

        #endregion

        #region Public - GetAxis, GetButton, Etc

        public float GetAxis(string axis)
        {
            PollDevices();

            if (_axisValues.ContainsKey(axis))
            {
                return _axisValues[axis];
            }
            else
            {
                Debug.LogWarning($"Unknown custom axis named '{axis}'.");
                return 0;
            }
        }

        public bool GetButton(string button)
        {
            PollDevices();

            if (_buttonValues.ContainsKey(button))
            {
                return _buttonValues[button].HasFlag(ButtonState.Down);
            }
            else
            {
                Debug.LogWarning($"Unknown custom button named '{button}'.");
                return false;
            }
        }

        public bool GetButtonUp(string button)
        {
            PollDevices();

            if (_buttonValues.ContainsKey(button))
            {
                var state = _buttonValues[button];
                return state == (ButtonState.Up | ButtonState.Now);
            }
            else
            {
                Debug.LogWarning($"Unknown custom button named '{button}'.");
                return false;
            }
        }

        public bool GetButtonDown(string button)
        {
            PollDevices();

            if (_buttonValues.ContainsKey(button))
            {
                var state = _buttonValues[button];
                return state == (ButtonState.Down | ButtonState.Now);
            }
            else
            {
                Debug.LogWarning($"Unknown custom button named '{button}'.");
                return false;
            }
        }

        #endregion

        [Serializable]
        private class InputMapping
        {
            [SerializeField]
            [FormerlySerializedAs("Name")]
            private string _name;

            [SerializeField]
            [FormerlySerializedAs("UnityInputName")]
            private string _unityInputName;

            [SerializeField]
            [FormerlySerializedAs("InputType")]
            private InputType _inputType;

            public string Name
            {
                get { return _name; }

                set { _name = value; }
            }

            public string UnityInputName
            {
                get { return _unityInputName; }

                set { _unityInputName = value; }
            }

            public InputType InputType
            {
                get { return _inputType; }
                set { _inputType = value; }
            }
        }

        [Flags]
        private enum ButtonState
        {
            Down = 1 << 1,
            Up = 1 << 2,
            Now = 1 << 3
        }

        private enum InputType
        {
            Button,
            ButtonAxisPositive,
            ButtonAxisNegative,
            Axis
        }
    }
}