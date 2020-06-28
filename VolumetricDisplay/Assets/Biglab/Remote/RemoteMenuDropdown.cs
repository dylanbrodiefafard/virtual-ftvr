using System;
using System.Collections.Generic;
using System.Linq;

using Biglab.Remote.Client;

using UnityEngine;

namespace Biglab.Remote
{
    public class RemoteMenuDropdown : RemoteMenuItem<IntMenuEvent, DropdownData, int>
    {
        public int Selected
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

        public IReadOnlyList<string> Options
        {
            get { return Element.OptionList; }

            set
            {
                // If null, replace with empty array
                if (value == null)
                {
                    value = Array.Empty<string>();
                }

                // 
                var existing = Element.OptionList as IReadOnlyList<string> ?? Array.Empty<string>();

                // If sequences aren't equivalent, consider it changed
                if (!value.SequenceEqual(existing))
                {
                    Element.OptionList = value.ToList();
                    SyncElementData();
                }
            }
        }

        protected override void SetLocalElementValue(int value)
        {
            Debug.Log($"Selected: {value} on '{name}'");
            Element.Selected = value;
        }
    }

    [Serializable]
    public class DropdownData : ElementData
    {
        public override ElementType Type => ElementType.Dropdown;

        [Space]
        public int Selected = -1;

        [Space]
        public List<string> OptionList;
    }
}