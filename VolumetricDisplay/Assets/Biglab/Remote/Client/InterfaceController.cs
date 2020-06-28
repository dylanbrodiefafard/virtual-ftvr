using Biglab.Utility;
using System;
using UnityEngine.UI;

namespace Biglab.Remote.Client
{
    public class InterfaceController
    {
        private readonly RemoteClient _client;
        private readonly RemoteClientMenuController _menu;
        private readonly RateLimiter _limiter;

        public InterfaceController(RemoteClientMenuController menu, RemoteClient client)
        {
            _menu = menu;
            _client = client;
            _limiter = new RateLimiter(1 / 20F); // 20 updates a second
        }

        public void HandleButtonPress(ElementData e)
        {
            // TODO: Safe ident for Disconnect button
            if (e.Id == 0)
            {
                _client.Disconnect();
                _menu.SetMinimalInterface(false);
            }
            else
            {
                e.AddValueToQueue(true);
            }
        }

        public void HandleSliderDrag(Slider slider, SliderData e)
        {
            if (_limiter.CheckElapsedTime())
            {
                e.AddValueToQueue(slider.value);
                e.Value = slider.value;
            }
        }

        public void HandleInputSubmit(InputField input, TextboxData e)
        {
            if (e.Id == 1)
            // TODO: CC: Remove inlined hardcoded behaviour ( extract to self method or callback? )
            {
                var address = input.GetComponentsInChildren<Text>()[1].text;
                if (!_client.Connect(address, _client.RemotePort))
                {
                    throw new Exception("Network Error: Unable to connect to target");
                }
            }
            else
            {
                e.AddValueToQueue(input.GetComponentsInChildren<Text>()[1].text);
                e.Value = input.GetComponentsInChildren<Text>()[1].text;
            }
        }

        public void HandleToggle(Toggle toggle, ToggleData e)
        {
            e.AddValueToQueue(toggle.isOn);
            e.Selected = toggle.isOn;
        }

        public void HandleDropdownChange(Dropdown dropdown, DropdownData e)
        {
            e.AddValueToQueue(dropdown.value);
            e.Selected = dropdown.value;
        }
    }
}