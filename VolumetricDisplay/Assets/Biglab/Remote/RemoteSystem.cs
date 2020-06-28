using System;
using System.Collections.Generic;
using System.Linq;

using Biglab.Collections;
using Biglab.Displays;
using Biglab.Extensions;
using Biglab.IO.Networking;
using Biglab.IO.Networking.TCP;
using Biglab.IO.Serialization;
using Biglab.Remote.Client;
using Biglab.Tracking;
using Biglab.Utility;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biglab.Remote
{
    public class RemoteSystem : ImmortalMonobehaviour<RemoteSystem>
    {
        public INetworkListener Listener { get; private set; }

        public event Action<Viewer, INetworkConnection> Connected;

        public event Action<Viewer, INetworkConnection> Disconnected;

        /// <summary>
        /// The currently known connections with a connected state.
        /// </summary>
        public IEnumerable<INetworkConnection> Connections
            => Listener.Connections.Values.Where(c => c.State == ConnectionState.Connected);

        /// <summary>
        /// The currently known viewers.
        /// </summary>
        public IEnumerable<Viewer> Viewers => _viewerInfo.TargetKeys.Select(info => info.Viewer);

        private RateLimiter _stateUpdateRate;
        private BiDictionary<int, RemoteClientInfo> _viewerInfo;
        private Menu _menu;

        #region MonoBehaviour

        protected override void Awake()
        {
            base.Awake();

            SceneManager.activeSceneChanged += ActiveSceneChanged;

            InitializeSystems();
        }

        private void Start()
        {
            gameObject.AddComponent<RemoteMenuSceneSwitcher>();
            gameObject.AddComponent<RemoteMenuStreamSwitcher>();
            gameObject.AddComponent<RemoteViewpointMenu>();
        }

        private void Update()
        {
            _menu.AddAndRemoveItems();

            // If enough time has passed, send update
            if (_stateUpdateRate.CheckElapsedTime())
            {
                var name = SceneManager.GetActiveScene().name;
                var count = Listener.Connections.Count;

                // Update each connection with information
                foreach (var connection in Connections)
                {

                    if (HasClientInfo(connection.Id))
                    {
                        var viewer = GetViewer(connection.Id);
                        var obj = viewer.GetComponent<TrackedObject>();

                        var desc = string.Empty;
                        if (obj != null)
                        {
                            desc = $"Tracking: {obj.ObjectKind} {obj.ObjectIndex}";
                        }

                        var state = new RemoteMenuStateMessage(connection.Id, count, name, desc);
                        connection.Send((byte)MessageType.ServerState, ClassSerializer.Serialize(state));
                    }
                }
            }

            // Kronk! Throw ze' lever!
            if (Input.GetKeyDown(KeyCode.K))
            {
                KillEverything();
            }
        }

        #endregion 

        private void ActiveSceneChanged(Scene prev, Scene current)
        {
            if (Listener == null) { InitializeSystems(); }
            CreateAlreadyConnectedViewers();
        }

        private void InitializeSystems()
        {
            _menu = new Menu();

            // Create TCP Server
            Listener = new TCPNetworkListener(Config.RemoteViewer.Address, Config.RemoteViewer.Port);

            // Subscribe to Server Events 
            Listener.Connected += OnConnected;
            Listener.Disconnected += OnDisconnected;

            // A rate limiter to send some server state text to the clients
            _stateUpdateRate = new RateLimiter(1 / 3F);

            // Subscribe to scene manager events
            CreateAlreadyConnectedViewers();
        }

        private void CreateAlreadyConnectedViewers()
        {
            // Clear ( or create ) viewers list 
            _viewerInfo = new BiDictionary<int, RemoteClientInfo>();

            // Create viewers for each known connection
            foreach (var kv in Listener.Connections)
            {
                CreateViewer(kv.Key, kv.Value, false);
            }
        }

        /// <summary>
        /// Gets the viewer associated with the given connection number.
        /// </summary>
        public Viewer GetViewer(int id)
        {
            if (_viewerInfo.Contains(id))
            {
                return _viewerInfo[id].Viewer;
            }

            throw new KeyNotFoundException($"No known connection for id {id}. Unable to get viewer.");
        }

        /// <summary>
        /// Gets the client info (viewer, connection, sleep, rate) associated with the given connection number.
        /// </summary>
        public RemoteClientInfo GetClientInfo(int id)
        {
            if (_viewerInfo.Contains(id))
            {
                return _viewerInfo[id];
            }

            throw new KeyNotFoundException($"No known connection for id {id}. Unable to get client info.");
        }

        /// <summary>
        /// Gets a connection by its connection number.
        /// </summary>
        public INetworkConnection GetConnection(int id)
        {
            if (Listener.Connections.ContainsKey(id))
            {
                return Listener.Connections[id];
            }

            throw new KeyNotFoundException($"No known connection for id {id}. Unable to get connection.");
        }

        /// <summary>
        /// Determines if a connection exists by its connection number.
        /// </summary>
        public bool HasClientInfo(int id)
        {
            return _viewerInfo.Contains(id);
        }

        /// <summary>
        /// Determines if a connection exists by its connection number.
        /// </summary>
        public bool HasConnection(int id)
        {
            return Listener.Connections.ContainsKey(id);
        }

        #region Network Messages

        private void OnConnected(INetworkConnection connection)
        {
            Debug.Log($"Connected: {connection.Id}.");

            // Bind connection events
            connection.MessageReceived += MessageReceived;

            // Bind menu change events
            _menu.ItemAdded += item => SendMessage(connection.Id, MessageType.InterfaceAddition, ClassSerializer.Serialize(item));
            _menu.ItemRemoved += item => SendMessage(connection.Id, MessageType.InterfaceRemoval, ClassSerializer.Serialize(item));

            // TODO: The rejection case for a MaxRemoteViewers (if needed)
            //Scheduler.DeferLaterFrame(() =>
            //{
            CreateViewer(connection.Id, connection, true);
            //});
        }

        private void OnDisconnected(INetworkConnection connection, DisconnectReason reason)
        {
            // 
            Debug.Log($"Disconnected: {connection.Id} because {reason}.");
            Disconnected?.Invoke(GetViewer(connection.Id), connection);

            // Get the viewer and remove it
            var viewer = GetViewer(connection.Id);
            _viewerInfo.Remove(connection.Id);

            Destroy(viewer.gameObject);
        }

        private void SendRenderedFrame(int id, Texture2D texture)
        {
            if (HasConnection(id))
            {
                var info = GetClientInfo(id);
                if (info.ImageLimiter.CheckElapsedTime())
                {
                    // Send frame ONLY if our rate limiter ticks
                    SendMessage(id, MessageType.Image, texture.SerializeTexture(Config.RemoteViewer.Quality));
                }
            }
        }

        private void SendMessage(int id, MessageType type, byte[] data)
        {
            if (HasConnection(id))
            {
                var connection = GetConnection(id);
                if (connection.State == ConnectionState.Connected)
                {
                    connection.Send((byte)type, data);
                }
            }
        }

        private void MessageReceived(INetworkConnection connection, Message message)
        {
            switch ((MessageType)message.Type)
            {
                default:
                    Debug.LogWarning($"Received unexpected message '{(MessageType)message.Type}' from connection {connection.Id}.");
                    break;

                case MessageType.InterfaceAddition:
                case MessageType.InterfaceRemoval:
                    // Also do nothing if interface construction commands are sent back
                    break;

                case MessageType.ValueChanged:
                    var valueMessage = ClassSerializer.Deserialize<RemoteMenuValueChangeMessage>(message.Data);
                    _menu.HandleMessage(connection, valueMessage);
                    break;

                case MessageType.TouchEvent:
                    var touches = message.Data.DeserializeBytesAsArray<RemoteTouch>();
                    RemoteInput.NotifyTouches(connection.Id, touches);
                    break;

                case MessageType.StreamState:
                    var stateMessage = ClassSerializer.Deserialize<RemoteMenuStreamStateMessage>(message.Data);
                    AdjustStreamingState(connection.Id, stateMessage);
                    break;
            }
        }

        private void AdjustStreamingState(int id, RemoteMenuStreamStateMessage state)
        {
            var info = GetClientInfo(id);

            info.ImageLimiter.Duration = Mathf.Min(1F, 1F / state.DeltaRate); // 1F/22 for example

            // 
            if (state.DeltaRate > state.ImageRate) { info.Viewer.CaptureFrequency = Mathf.Max(1F, state.ImageRate + 3); }
            else { info.Viewer.CaptureFrequency = Mathf.Max(1F, state.ImageRate - 3); }
        }

        #endregion

        private void CreateViewer(int id, INetworkConnection connection, bool needsToFindMenu)
        {
            // Create game object
            var viewerGameObject = new GameObject($"Remote Viewer: {connection.Id}");

            // Add tracked object
            var trackedObject = viewerGameObject.AddComponent<TrackedObject>();
            trackedObject.ObjectKind = TrackedObjectKind.Object;
            trackedObject.ObjectIndex = connection.Id;

            // Create perspective viewer and store
            var viewer = viewerGameObject.AddComponentWithInit<Viewer>(v =>
            {
                // True 1.77 aspect resolutions 
                //     128 x 72
                //     256 x 144
                //     384 x 216
                //     512 x 288
                //     640 x 360

                // 
                var width = Config.RemoteViewer.RenderWidth;
                var height = Config.RemoteViewer.RenderHeight;
                // 
                v.FrustumMode = Viewer.FrustumFittingMode.Fixed;
                v.TextureSize = new Vector2Int(width, height);
                v.EnabledStereoRendering = false;
                v.NonStereoFallbackEye = Camera.MonoOrStereoscopicEye.Left;
                v.EnableFrameCapture = false;
                v.Role = ViewerRole.Remote;

                // When the viewer renders a frame submit
                v.RenderedFrame += texture => SendRenderedFrame(id, texture);
            });

            // Create connection info
            _viewerInfo[connection.Id] = new RemoteClientInfo(viewer, connection);

            // Add remote viewpoint
            var remoteViewpoint = viewerGameObject.AddComponent<RemoteViewpoint>();
            remoteViewpoint.Alias = $"Viewer Remote {id}";
            remoteViewpoint.Anchor = viewer.LeftAnchor;

            // Trigger connection event ( connected first time or reconnected on scene switch )
            Connected?.Invoke(viewer, connection);

            if (needsToFindMenu)
            {
                // Inform viewer of currently enabled menu items
                foreach (var item in FindObjectsOfType<RemoteMenuItem>().Where(c => c.isActiveAndEnabled))
                {
                    SendMessage(id, MessageType.InterfaceAddition, ClassSerializer.Serialize(item.Data));
                }
            }
        }

        internal void RegisterElement(RemoteMenuItem item)
            => _menu.RegisterElement(item);

        internal void UpdateElement(RemoteMenuItem item)
            => _menu.UpdateElement(item);

        internal void UnregisterElement(RemoteMenuItem item)
            => _menu.UnregisterElement(item);

        /// <summary>
        /// A straight up panic button function to reset the entire state of the remote control system.
        /// </summary>
        public void KillEverything()
        {
            // Trash each viewer
            foreach (var client in _viewerInfo.TargetKeys)
            {
                // Kill viewer
                Destroy(client.Viewer.gameObject);

                // Kill connection
                client.Connection.Disconnect();
            }

            // Trash listening server
            Listener.Connected -= OnConnected;
            Listener.Disconnected -= OnDisconnected;
            Listener.Dispose();

            // Reinitialize connection
            InitializeSystems();
        }

        private class Menu
        {
            public ICollection<RemoteMenuItem> Items => _items.Values;

            private readonly Dictionary<int, RemoteMenuItem> _items;

            public event Action<ElementData> ItemAdded;
            public event Action<ElementData> ItemRemoved;

            private readonly HashSet<ElementData> _adds;
            private readonly HashSet<ElementData> _rems;

            public Menu()
            {
                _adds = new HashSet<ElementData>();
                _rems = new HashSet<ElementData>();

                _items = new Dictionary<int, RemoteMenuItem>();
            }

            public void AddAndRemoveItems()
            {
                foreach (var element in _adds)
                {
                    ItemAdded?.Invoke(element);
                }

                _adds.Clear();

                foreach (var element in _rems)
                {
                    ItemRemoved?.Invoke(element);
                }

                _rems.Clear();
            }

            public void RegisterElement(RemoteMenuItem item)
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                // Force the element id to be the instance id
                var identifier = item.GetInstanceID();
                item.Data.Id = identifier;

                // Do we not know about this menu item?
                if (_items.ContainsKey(identifier) == false)
                {
                    // Store menu item
                    // Debug.Log($"Registering menu item on '{item.name}' ({item.Data.Type}).");
                    _items[identifier] = item;
                }

                // 
                UpdateElement(item);
            }

            public void UpdateElement(RemoteMenuItem item)
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                // Do we already know about this menu item?
                if (_items.ContainsKey(item.Data.Id))
                {
                    // Add to send add/update set
                    _adds.Add(item.Data);
                }
                else
                {
                    // Debug.LogWarning($"Updating menu item on '{item.name}' ({item.Data.Type}), but item was not registered.");
                }
            }

            public void UnregisterElement(RemoteMenuItem item)
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                // Try to remove item
                if (_items.Remove(item.Data.Id))
                {
                    // Debug.Log($"Unregistering menu item on '{item.name}' ({item.Data.Type}).");
                    _rems.Add(item.Data);
                }
                else
                {
                    // Debug.Log($"Unregistering menu item on '{item.name}' ({item.Data.Type}) [ALREADY REMOVED].");
                }
            }

            public void HandleMessage(INetworkConnection connection, RemoteMenuValueChangeMessage message)
            {
                if (_items.ContainsKey(message.Id))
                {
                    // 
                    _items[message.Id].HandleElementValueChange(connection, message.Value);
                }
                else
                {
                    Debug.LogWarning($"Received menu message from unknown id {message.Id}.");
                }
            }
        }

        public class RemoteClientInfo
        {
            public Viewer Viewer;
            public INetworkConnection Connection;
            public bool IsPaused;

            public RateLimiter ImageLimiter;

            public RemoteClientInfo(Viewer viewer, INetworkConnection connection)
            {
                Viewer = viewer;
                Connection = connection;
                ImageLimiter = new RateLimiter(1 / 60F);
            }
        }
    }
}