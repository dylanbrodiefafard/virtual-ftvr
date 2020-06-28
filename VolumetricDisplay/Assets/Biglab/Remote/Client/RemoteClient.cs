using System;
using System.Net;
using Biglab.IO.Networking;
using Biglab.IO.Networking.TCP;
using Biglab.IO.Serialization;
using Biglab.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Biglab.Remote.Client
{
    public class RemoteClient : MonoBehaviour
    {
        /// <summary>
        /// The connection to the remote host ( remote endpoint ).
        /// </summary>
        public TCPNetworkClient Client { get; private set; }

        public string RemoteAddress
        {
            get { return _remoteAddress; }
            private set { _remoteAddress = value; }
        }

        public int RemotePort
        {
            get { return _remotePort; }
            private set { _remotePort = value; }
        }

        [SerializeField, ReadOnly] private RemoteClientMenuController _menuController;

        [SerializeField, ReadOnly] private RemoteClientTouchController _touchController;

        [Header("Server Address")]

        [SerializeField] private string _remoteAddress;

        [SerializeField] private int _remotePort = 32032;

        [Header("Texture Display")]

        public AspectRatioFitter AspectRatioFitter;

        public RawImage Image;

        [Space]

        public PhoneCamera Webcam;

        /// <summary>
        /// Is the specified address and port valid?
        /// </summary>
        public bool HasValidConnectionInfo => !string.IsNullOrWhiteSpace(RemoteAddress) && RemotePort > 0;

        //
        private float _lastTextureUpdate;
        private Texture2D _texture;

        private RemoteMenuStateMessage _state;
        string _rightMessage;
        string _leftMessage;

        // 
        private RateLimiter _stateInterval;
        private float _deltaRate = 0F;
        private float _imageRate = 0F;
        private int _deltaCount = 0;
        private int _imageCount = 0;

        #region MonoBehaviour

        void Start()
        {
            // 
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;

            // 
            Application.targetFrameRate = 60;

            //
            _stateInterval = new RateLimiter(0.5F);

            // Create
            _menuController = CreateChildObject<RemoteClientMenuController>();
            _touchController = CreateChildObject<RemoteClientTouchController>();

            _lastTextureUpdate = Time.time;
        }

        private void Update()
        {
            // Prevents app from sleeping when connected
            Screen.sleepTimeout = Client?.State == ConnectionState.Connected ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;

            _deltaCount += 1;

            // 
            if (_stateInterval.CheckElapsedTime())
            {
                // Get how many images where sent
                _imageRate = _imageCount / _stateInterval.Duration;
                _deltaRate = _deltaCount / _stateInterval.Duration;
                _imageCount = 0;
                _deltaCount = 0;

                if (Client?.State == ConnectionState.Connected)
                {
                    // Send stream health report
                    var bytes = ClassSerializer.Serialize(new RemoteMenuStreamStateMessage(_imageRate, _deltaRate));
                    Send(MessageType.StreamState, bytes);
                }
            }

            // Set connection status
            var ipInfo = $"Connection: {RemoteAddress}:{RemotePort}";

            if (_state == null)
            {
                // No state
                _rightMessage = ipInfo;
                _leftMessage = "No Description";
            }
            else
            {
                // Update state and description messages
                _rightMessage = $"Client #{_state.Id} of {_state.ConnectionCount}\n{ipInfo}";
                _leftMessage = $"{_deltaRate.ToString("00.00")} Updates\n{_imageRate.ToString("00.00")} Images\n{_state.SceneName}\n{_state.Description}";
            }

            if (Webcam.IsCameraAvailable)
            {
                if (Time.time - _lastTextureUpdate > 1F) // Time since last frame was more than a second old, revert to webcam
                {
                    // Have not received a texture recently, start webcam
                    if (Webcam.HasCameraStarted == false)
                    {
                        Webcam.StartCamera();
                    }
                }
                else
                {
                    // Received a texture recently, stop webcam
                    if (Webcam.HasCameraStarted)
                    {
                        Webcam.StopCamera();
                    }
                }
            }

            // Scale texture to fit webcam orientation
            if (Webcam.IsCameraAvailable && Webcam.HasCameraStarted)
            {
                // Set webcam texture
                Image.texture = Webcam.Texture;

                // Vertical Flipping and Rotation
                Image.rectTransform.localScale = new Vector3(1, Webcam.TextureScaleVerticalFlip, 1);
                Image.rectTransform.localEulerAngles = new Vector3(0, 0, Webcam.TextureOrientation);
            }
            // Set texture to fit streaming texture 
            else
            {
                // Set streaming texture
                Image.texture = _texture;

                // Reset Transform
                Image.rectTransform.localEulerAngles = new Vector3(0, 0, 0);
                Image.rectTransform.localScale = new Vector3(1, 1, 1);
            }

            // Set aspect Ratio
            if (Image.texture != null)
            {
                var ratio = Image.texture.width / (float)Image.texture.height;
                AspectRatioFitter.aspectRatio = ratio;
            }
        }

        private void OnGUI()
        {
            // Draw connection state info
            var _stateDims = GUI.skin.label.CalcSize(new GUIContent(_rightMessage)); // TODO: Right align?
            GUI.Label(new Rect(Screen.width - _stateDims.x - 2, Screen.height - _stateDims.y, 500, 500), _rightMessage);

            // Draw description label
            var _descDims = GUI.skin.label.CalcSize(new GUIContent(_leftMessage));
            GUI.Label(new Rect(2, Screen.height - _descDims.y, 500, 500), _leftMessage);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Disconnect();
            }
        }

        private void OnApplicationQuit()
        {
            Client?.Disconnect();
        }

        #endregion

        private T CreateChildObject<T>() where T : MonoBehaviour
        {
            // 
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(transform);
            return go.AddComponent<T>();
        }

        /// <summary>
        /// Attempts to connect to host application (with new connection information).
        /// </summary>
        public bool Connect(string address, int port)
        {
            SetConnectionInformation(address, port);
            return Reconnect();
        }

        /// <summary>
        /// Attempts to reconnect to host application.
        /// </summary>
        public bool Reconnect()
        {
            // If address is valid and we have good connection information
            if (HasValidConnectionInfo)
            {
                // If a previous connection is known, disconnect
                if (Client?.State != ConnectionState.Disconnected)
                {
                    Disconnect();
                }

                try
                {
                    // Attempt to open connnection
                    Client = new TCPNetworkClient(RemoteAddress, RemotePort);

                    // Bind connection events
                    Client.MessageReceived += Connection_MessageReceived;
                    Client.Disconnected += Connection_Disconnected;

                    // 
                    _menuController.SetMinimalInterface(true);
                }
                catch (Exception e)
                {
                    _rightMessage = $"Failed To Connect.\n{e.Message}";
                    Debug.LogError(e);
                    return false;
                }

                return true;
            }
            else
            {
                Debug.LogError("Unable to connect. (Invalid connection info)");
                return false;
            }
        }

        /// <summary>
        /// Attempts to disconnect from the host application.
        /// </summary>
        public void Disconnect()
        {
            if (Client != null)
            {
                Client.Disconnect();
                Client.MessageReceived -= Connection_MessageReceived;
                Client.Disconnected -= Connection_Disconnected;
                Client = null;

                // 
                _menuController.SetMinimalInterface(false);
            }

            _state = null;
        }

        /// <summary>
        /// Attempts to set the connection information, only modifying values if information is considered valid.
        /// Returns true if the connection information is modified, otherwise false.
        /// </summary>
        private bool SetConnectionInformation(string address, int port)
        {
            // Throw a fit
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            IPAddress ip;
            if (NetworkUtility.TryParseAddress(address, out ip))
            {
                if (port > 0)
                {
                    // Set connection information
                    RemoteAddress = ip.MapToIPv4().ToString();
                    RemotePort = port;

                    // Save this information in player prefs on good connect params
                    PlayerPrefs.SetString("ip", address);

                    return true;
                }
                else
                {
                    // Invalid Port
                    Debug.LogError($"Invalid Port: '{port}'.");

                    RemotePort = -1;
                    return false;
                }
            }
            else
            {
                // Invalid Address
                Debug.LogError($"Invalid Address: '{address}'.");

                RemoteAddress = null;
                return false;
            }
        }

        private void Connection_Disconnected(INetworkConnection connection, DisconnectReason reason)
        {
            Debug.Log($"Disconnected: {reason}");

            // 
            _menuController.SetMinimalInterface(reason == DisconnectReason.Timeout);

            Client = null;
            _texture = null;
            _state = null;
        }

        private void Connection_MessageReceived(INetworkConnection connection, Message message)
        {
            switch ((MessageType)message.Type)
            {
                // STATE MESSAGE
                case MessageType.ServerState:
                    _state = ClassSerializer.Deserialize<RemoteMenuStateMessage>(message.Data);
                    break;

                // IMAGE STREAMING
                case MessageType.Image:
                    message.Data.DeserializeTexture(ref _texture);
                    _lastTextureUpdate = Time.time;
                    _imageCount++;
                    break;

                // MENU ITEM ADD
                case MessageType.InterfaceAddition:
                    _menuController.Add(ClassSerializer.Deserialize<ElementData>(message.Data));
                    break;

                // MENU ITEM REMOVE
                case MessageType.InterfaceRemoval:
                    _menuController.Remove(ClassSerializer.Deserialize<ElementData>(message.Data));

                    break;

                case MessageType.TouchEvent:
                    break;
            }
        }

        public void Send(MessageType type, byte[] data)
        {
            if (Client != null && Client.State == ConnectionState.Connected)
            {
                Client.Send((byte)type, data);
            }
            else
            {
                Debug.LogError($"Attempting to send data to server when disconnected.");
            }
        }
    }
}