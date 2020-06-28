using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace Biglab.Utility
{
    /// <summary>
    /// Schedules actions to be invoked on the Unity main thread with different invocation behaviours.
    /// </summary>
    public static class Scheduler
    {
        private static SchedulerBehaviour _instance;

        public delegate IEnumerator CoroutineAction();

        #region Coroutines

        /// <summary>
        /// Begin a coroutine on the scheduler instance.
        /// </summary>
        public static Coroutine StartCoroutine(IEnumerator enumerator)
            => _instance.StartCoroutine(enumerator);

        /// <summary>
        /// Begin a coroutine on the scheduler instance.
        /// </summary>
        public static Coroutine StartCoroutine(CoroutineAction coroutineAction)
            => _instance.StartCoroutine(coroutineAction());

        /// <summary>
        /// Stop a coroutine by reference on the scheduler instance.
        /// </summary>
        public static void StopCoroutine(Coroutine coroutine)
            => _instance.StopCoroutine(coroutine);

        /// <summary>
        /// Stop a coroutine by reference on the scheduler instance.
        /// </summary>
        public static void StopAllCoroutines()
            => _instance.StopAllCoroutines();

        #endregion

        #region Deferred Actions ( Unity Update )

        /// <summary>
        /// Schedules an action to be executed at some later frame. <para/>
        /// The scheduler will execute one "later" task during the next update.
        /// </summary>
        public static void DeferLaterFrame(Action task)
            => _instance.DeferLaterFrame(task);

        /// <summary>
        /// Schedules an action to be executed at the next immedate frame. <para/>
        /// The scheduler will execute all "next" tasks during the next update.
        /// </summary>
        public static void DeferNextFrame(Action task)
            => _instance.DeferNextFrame(task);

        #endregion

        #region Deferred Actions ( Unity Update with Return )

        /// <summary>
        /// Schedules an action for the Unity thread, blocking the current thread until a result is returned.
        /// </summary>
        public static TReturn InvokeOnUnityThread<TReturn>(Func<TReturn> method)
            => (TReturn)_instance.Invoke(method, Array.Empty<object>());

        /// <summary>
        /// Schedules an action for the Unity thread, blocking the current thread until a result is returned.
        /// </summary>
        public static object InvokeOnUnityThread(Delegate method, params object[] args)
            => _instance.Invoke(method, args);

        #endregion

        [RuntimeInitializeOnLoadMethod]
        private static void InstantiateScheduler()
        {
            var gameObject = new GameObject(nameof(Scheduler));
            Object.DontDestroyOnLoad(gameObject);

            // Add scheduler
            _instance = gameObject.AddComponent<SchedulerBehaviour>();

            // Clear tasks on scene change
            SceneManager.activeSceneChanged += ActiveSceneChanged;
        }

        private static void ActiveSceneChanged(Scene prev, Scene current)
        {
            _instance.ClearDeferredTasks();
            StopAllCoroutines();
        }

        private class AsyncResult : IAsyncResult
        {
            public Delegate Method;

            public object[] Args;

            public ManualResetEvent ManualResetEvent;

            public bool IsCompleted { get; set; }

            public WaitHandle AsyncWaitHandle => ManualResetEvent;

            public object AsyncState { get; private set; }

            public bool CompletedSynchronously { get; set; }

            public void Invoke()
            {
                AsyncState = Method.DynamicInvoke(Args);
                IsCompleted = true;
                ManualResetEvent.Set();
            }
        }

        private class SchedulerBehaviour : MonoBehaviour
        {
            private readonly Queue<Action> _laterTasks = new Queue<Action>();
            private readonly Queue<Action> _nextTasks = new Queue<Action>();

            private readonly Queue<AsyncResult> _synchronizedTasks = new Queue<AsyncResult>();
            private Thread _unityThread;

            private bool InvokeRequired => _unityThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId;

            private void Awake()
                => _unityThread = Thread.CurrentThread;

            private void Update()
            {
                ProcessSynchronizedQueue();
                ProcessDeferredQueue();
            }

            #region Syncronized Tasks

            private void ProcessSynchronizedQueue()
            {
                if (Thread.CurrentThread != _unityThread)
                {
                    throw new Exception(
                        "must be called from the same thread it was created on " +
                        "(created on thread id: " + _unityThread.ManagedThreadId + ", called from thread id: " +
                        Thread.CurrentThread.ManagedThreadId
                    );
                }

                while (true)
                {
                    AsyncResult data;
                    lock (_synchronizedTasks)
                    {
                        if (_synchronizedTasks.Count == 0)
                        {
                            break;
                        }

                        data = _synchronizedTasks.Dequeue();
                    }

                    data.Invoke();
                }
            }

            public IAsyncResult BeginInvoke(Delegate method, object[] args)
            {
                var asyncResult = new AsyncResult()
                {
                    Method = method,
                    Args = args,
                    IsCompleted = false,
                    ManualResetEvent = new ManualResetEvent(false),
                };

                if (InvokeRequired)
                {
                    lock (_synchronizedTasks)
                    {
                        _synchronizedTasks.Enqueue(asyncResult);
                    }
                }
                else
                {
                    asyncResult.Invoke();
                    asyncResult.CompletedSynchronously = true;
                }

                return asyncResult;
            }

            public static object EndInvoke(IAsyncResult result)
            {
                if (!result.IsCompleted)
                {
                    result.AsyncWaitHandle.WaitOne();
                }

                return result.AsyncState;
            }

            public object Invoke(Delegate method, object[] args)
            {
                if (InvokeRequired)
                {
                    var asyncResult = BeginInvoke(method, args);
                    return EndInvoke(asyncResult);
                }
                else
                {
                    return method.DynamicInvoke(args);
                }
            }

            #endregion

            #region Deferred Tasks

            private void ProcessDeferredQueue()
            {
                lock (_laterTasks)
                {
                    // Execute next "layer" action
                    if (_laterTasks.Count > 0)
                    {
                        _laterTasks.Dequeue()();
                    }
                }

                lock (_nextTasks)
                {
                    // Execute all deferred "next" actions
                    while (_nextTasks.Count > 0)
                    {
                        _nextTasks.Dequeue()();
                    }
                }
            }

            public void ClearDeferredTasks()
            {
                lock (_laterTasks)
                {
                    _laterTasks.Clear();
                }

                lock (_nextTasks)
                {
                    _nextTasks.Clear();
                }
            }

            public void DeferLaterFrame(Action task)
            {
                lock (_laterTasks)
                {
                    // Enqueue task
                    _laterTasks.Enqueue(task);

                    // 
                    if (_laterTasks.Count > 100)
                    {
                        Debug.LogWarning("Exceeded 100 'later' tasks!");
                    }
                }
            }

            public void DeferNextFrame(Action task)
            {
                lock (_nextTasks)
                {
                    _nextTasks.Enqueue(task);
                }
            }

            #endregion
        }
    }
}