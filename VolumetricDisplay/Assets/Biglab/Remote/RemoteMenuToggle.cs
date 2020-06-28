using System;

using Biglab.Remote.Client;

using UnityEngine;

namespace Biglab.Remote
{
    public class RemoteMenuToggle : RemoteMenuItem<BoolMenuEvent, ToggleData, bool>
    {
        public string Label
        {
            get { return Element.Label; }

            set
            {
                if (Element.Label != value)
                {
                    Element.Label = value;
                    SyncElementData();
                }
            }
        }

        public bool IsSelected
        {
            get { return Element.Selected; }

            set
            {
                if (Element.Selected != value)
                {
                    Element.Selected = value;
                    SyncElementData();
                }
            }
        }

        protected override void SetLocalElementValue(bool value)
        {
            Element.Selected = value;
        }
    }

    [Serializable]
    public class ToggleData : ElementData
    {
        public override ElementType Type => ElementType.Toggle;

        [Space]
        public string Label;

        public bool Selected;
    }
}