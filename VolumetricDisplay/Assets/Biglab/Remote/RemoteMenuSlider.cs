using System;

using Biglab.Remote.Client;

using UnityEngine;

namespace Biglab.Remote
{
    public class RemoteMenuSlider : RemoteMenuItem<FloatMenuEvent, SliderData, float>
    {
        #region Properties

        public string Label
        {
            get { return Element.Label; }

            set
            {
                if (Element.Label == value)
                {
                    return;
                }

                Element.Label = value;
                SyncElementData();
            }
        }

        public float Value
        {
            get { return Element.Value; }

            set
            {
                if (Mathf.Approximately(Element.Value, value))
                {
                    return;
                }

                Element.Value = value;
                SyncElementData();
            }
        }

        public float MaxValue
        {
            get { return Element.MaxValue; }

            set
            {
                if (Mathf.Approximately(Element.MaxValue, value))
                {
                    return;
                }

                Element.MaxValue = value;
                SyncElementData();
            }
        }

        public float MinValue
        {
            get { return Element.MinValue; }

            set
            {
                if (Mathf.Approximately(Element.MinValue, value))
                {
                    return;
                }

                Element.MinValue = value;
                SyncElementData();
            }
        }

        #endregion

        protected override void SetLocalElementValue(float value)
            => Element.Value = value;
    }

    [Serializable]
    public class SliderData : ElementData
    {
        public override ElementType Type => ElementType.Slider;

        [Space]
        public string Label;

        public float MinValue;

        public float MaxValue;

        public float Value;
    }
}