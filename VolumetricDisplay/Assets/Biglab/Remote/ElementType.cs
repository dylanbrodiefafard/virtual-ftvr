using System;

namespace Biglab.Remote.Client
{
    [Serializable]
    public enum ElementType
    {
        Button,
        InputField,
        Dropdown,
        Slider,
        Toggle,
        Label
    };
}