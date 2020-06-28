using System;

using Biglab.Remote.Client;

using UnityEngine;

namespace Biglab.Remote
{
    public class RemoteMenuTextbox : RemoteMenuItem<StringMenuEvent, TextboxData, string>
    {
        public string Value
        {
            get { return Element.Value; }

            set
            {
                if (Element.Value != value)
                {
                    Element.Value = value;
                    SyncElementData();
                }
            }
        }

        public string Placeholder
        {
            get { return Element.Placeholder; }

            set
            {
                if (Element.Placeholder != value)
                {
                    Element.Placeholder = value;
                    SyncElementData();
                }
            }
        }

        protected override void SetLocalElementValue(string value)
        {
            Element.Value = value;
        }
    }

    [Serializable]
    public class TextboxData : ElementData
    {
        public override ElementType Type => ElementType.InputField;

        [Space]
        public string Placeholder;

        public string Value;
    }
}