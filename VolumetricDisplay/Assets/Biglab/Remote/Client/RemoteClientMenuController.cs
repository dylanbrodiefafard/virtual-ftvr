using System;
using System.Collections.Generic;

using Biglab.Extensions;
using Biglab.IO.Networking;
using Biglab.IO.Serialization;

using UnityEngine;

namespace Biglab.Remote.Client
{
    public class RemoteClientMenuController : MonoBehaviour
    {
        public InterfaceController Controller { get; private set; }

        public InterfaceView View { get; private set; }

        public IReadOnlyDictionary<int, ElementData> InterfaceElements => _elements;

        public event Action ElementsChanged;

        private Dictionary<int, ElementData> _elements;
        private bool _changeFlag;

        private RemoteClient _client;

        public bool OverlayActive;

        // The IP connection input is a static field that should always display before everything else
        private static readonly TextboxData _connectionField = new TextboxData
        {
            Placeholder = "127.0.0.1",
            Group = null,
            Order = int.MinValue,
            Id = 1
        };

        // The disconectBtn is the same but it is shown ONLY when we are connected to a server.
        // The button should appear directly under the IP field for now (can be changed in the future)
        private static readonly ButtonData _disconnectButton = new ButtonData
        {
            Label = "Disconnect",
            Group = null,
            Order = int.MinValue,
            Id = 0
        };

        #region MonoBehaviour

        // Use this for initialization
        void Start()
        {
            this.FindComponentReference(ref _client);

            _elements = new Dictionary<int, ElementData>();

            // Create VC of MVC
            Controller = new InterfaceController(this, _client);
            View = new InterfaceView(this);

            SetMinimalInterface(false);
        }

        void Update()
        {
            if (_changeFlag)
            {
                // Invoke menu change event
                ElementsChanged?.Invoke();
                _changeFlag = false;
            }

            // For each element, try to get changes/commands
            foreach (var element in InterfaceElements.Values)
            {
                RemoteMenuValueChangeMessage message;
                while (element.TryGetLatestCommand(out message))
                {
                    // Send that some element has issued a "command"
                    _client.Send(MessageType.ValueChanged, ClassSerializer.Serialize(message));
                }
            }
        }

        #endregion

        #region Element Collection Mutators

        public void Add(ElementData element)
        {
            _elements[element.Id] = element;
            _changeFlag = true;
        }

        public void Remove(ElementData element)
        {
            if (_elements.ContainsKey(element.Id))
            {
                _elements.Remove(element.Id);
                _changeFlag = true;
            }
        }

        public void Clear()
        {
            _elements.Clear();
            _changeFlag = true;
        }

        #endregion

        public void SetMinimalInterface(bool disconnect)
        {
            Clear();

            // Check if we have saved connection prefs here
            if (PlayerPrefs.HasKey("ip"))
            {
                ((TextboxData)_connectionField).Value = PlayerPrefs.GetString("ip");
            }

            if (disconnect)
            {
                Add(_disconnectButton);
            }
            else
            {
                Add(_connectionField);
            }
        }
    }
}