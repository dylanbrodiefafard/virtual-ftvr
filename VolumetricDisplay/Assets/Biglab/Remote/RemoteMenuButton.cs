using System;

using Biglab.Remote.Client;

using UnityEngine;

namespace Biglab.Remote
{
    public class RemoteMenuButton : RemoteMenuItem<BoolMenuEvent, ButtonData, bool>
    { 
        public string Text
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
        
        protected override void SetLocalElementValue(bool value)
        {
            // Nothing, its a button
        }
    }

    [Serializable]
    public class ButtonData : ElementData
    {
        public override ElementType Type => ElementType.Button;

        [Space] public string Label;
    }
}