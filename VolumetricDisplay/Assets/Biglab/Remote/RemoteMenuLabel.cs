using System;

using Biglab.IO.Networking;
using Biglab.Remote.Client;

using UnityEngine;
using UnityEngine.Events;

namespace Biglab.Remote
{
    public class RemoteMenuLabel : RemoteMenuItem<BoolMenuEvent, LabelData, bool>
    {
        [Serializable]
        public class Event : UnityEvent<bool, INetworkConnection>
        { }

        #region Properties

        public string Text
        {
            get { return Element.Text; }

            set
            {
                if (Element.Text != value)
                {
                    Element.Text = value;
                    SyncElementData();
                }
            }
        }

        #endregion

        protected override void SetLocalElementValue(bool value)
        {
            // Nothing, its a label
        }
    }

    [Serializable]
    public class LabelData : ElementData
    {
        public override ElementType Type => ElementType.Label;

        [Space, Multiline]
        public string Text;
    }
}