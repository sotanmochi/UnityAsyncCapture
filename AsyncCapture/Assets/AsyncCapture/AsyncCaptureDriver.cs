using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UniRx;

namespace AsyncCapture
{
    public sealed class AsyncCaptureDriver : MonoBehaviour
    {
        public event Action<Capture> OnCapture;

        public bool Initialized => _initialized;
        private bool _initialized;

        public bool AutoDrive => _autoDrive;
        private bool _autoDrive;

        private uint _processedFrameCount;
        private bool _screenCapture;

        private (RenderTexture grab, RenderTexture flip) _rt;
        private Vector2 _scScale = new Vector2(1, -1);
        private Vector2 _scOffset = new Vector2(0, 1);

        private Texture _captureTargetTexture;
        private int _bytesPerPixel;
        private int _width, _height;
        private GraphicsFormat _format;

        private uint _captureFrameRate = 1;
        private uint _bufferCount = 0;
        private AsyncGPUReadbackRequest[] _gpuReadbackRequestBuffer;
        private NativeArray<byte>[] _nativeArrayBuffer;
        private List<byte[]> _rawDateBuffer;

        private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        public void Initialize(Texture captureTargetTexture, uint captureFrameRate)
        {
            _captureTargetTexture = captureTargetTexture;

            if (_captureTargetTexture is null)
            {
                _width = Screen.width;
                _height = Screen.height;
                _format = GraphicsFormat.R8G8B8A8_UNorm;

                _screenCapture = true;
                _rt.grab = new RenderTexture(_width, _height, 0, _format);
            }
            else
            {
                _width = _captureTargetTexture.width;
                _height = _captureTargetTexture.height;
                _format = _captureTargetTexture.graphicsFormat;
            }

            _format = _format switch
            {
                GraphicsFormat.RGBA_BC7_SRGB => GraphicsFormat.R8G8B8A8_SRGB,
                GraphicsFormat.R8G8B8A8_SRGB => GraphicsFormat.R8G8B8A8_SRGB,
                GraphicsFormat.R8G8B8A8_UNorm => GraphicsFormat.R8G8B8A8_UNorm,
                GraphicsFormat.R32G32B32A32_SFloat => GraphicsFormat.R32G32B32A32_SFloat,
                _ => throw new InvalidOperationException()
            };

            _bytesPerPixel = _format switch
            {
                GraphicsFormat.R8G8B8A8_SRGB => 32 / 8, // 32[bit]
                GraphicsFormat.R8G8B8A8_UNorm => 32 / 8, // 32[bit]
                GraphicsFormat.R32G32B32A32_SFloat => 128 / 8, // 128[bit] 
                _ => throw new InvalidOperationException()
            };

            _rt.flip = new RenderTexture(_width, _height, 0, _format);

            _captureFrameRate = (captureFrameRate <  1) ?  1 : captureFrameRate;
            _captureFrameRate = (captureFrameRate > 30) ? 30 : captureFrameRate;
            // _captureFrameRate = (captureFrameRate > 60) ? 60 : captureFrameRate;
            Debug.Log($"[AsyncCaptureDriver] Capture frame rate: {_captureFrameRate}");

            _bufferCount = (uint)Mathf.Ceil(_captureFrameRate / 10.0f); // 15.0f? 20.0f?
            _bufferCount = (_bufferCount < 1) ? 1 : _bufferCount;

            _gpuReadbackRequestBuffer = new AsyncGPUReadbackRequest[_bufferCount];
            _nativeArrayBuffer = new NativeArray<byte>[_bufferCount];
            _rawDateBuffer = new List<byte[]>((int)_bufferCount);

            for (int i = 0; i < _bufferCount; i++)
            {
                _nativeArrayBuffer[i] = new NativeArray<byte>(_width * _height * _bytesPerPixel, 
                                            Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
                _rawDateBuffer.Add(new byte[_width * _height * _bytesPerPixel]);
            }

            Debug.Log($"[AsyncCaptureDriver] Capture target texture size: {_width}x{_height}");
            Debug.Log($"[AsyncCaptureDriver] Capture target texture format: {_format}");

            _initialized = true;
        }

        public void StartAutoCapture()
        {
            _autoDrive = true;
        }

        public void StopAutoCapture()
        {
            _autoDrive = false;
        }

        public IEnumerator RequestCaptureCoroutine()
        {
            yield return _waitForEndOfFrame;

            if (_initialized && enabled)
            {
                RequestCapture();
            }
        }

        public void RequestCapture()
        {
            var index = _processedFrameCount++ % _bufferCount;

            if (_gpuReadbackRequestBuffer[index].done && _gpuReadbackRequestBuffer[index].hasError)
            {
                if (_screenCapture)
                {
                    ScreenCapture.CaptureScreenshotIntoRenderTexture(_rt.grab);
                    Graphics.Blit(_rt.grab, _rt.flip, _scScale, _scOffset);
                }
                else
                {
                    Graphics.Blit(_captureTargetTexture, _rt.flip);
                }

                var request = AsyncGPUReadback.RequestIntoNativeArray(ref _nativeArrayBuffer[index], _rt.flip);
                _gpuReadbackRequestBuffer[index] = request;
            }
        }

#region MonoBehaviour

        private void Start()
        {
            Debug.Log($"[AsyncCaptureDriver] Start");
            Debug.Log($"[AsyncCaptureDriver] Interval: {1000.0f / _captureFrameRate} [ms]");

            Observable
                .Interval(TimeSpan.FromMilliseconds(1000.0f / _captureFrameRate), Scheduler.ThreadPool)
                // .Do(_ => Debug.Log($"[AsyncCaptureDriver] Interval thread Id: {System.Threading.Thread.CurrentThread.ManagedThreadId}"))
                .TakeUntilDestroy(this)
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    if (_autoDrive)
                    {
                        StartCoroutine(RequestCaptureCoroutine());
                    }
                });
        }

        private void Update()
        {
            if (!_initialized){ return; }

            for (int i = 0; i < _bufferCount; i++)
            {
                var request = _gpuReadbackRequestBuffer[i];
                // Debug.Log($"[AsyncCaptureDriver] Request[{i}] - Error: {request.hasError}, Done: {request.done}");

                if (!request.hasError && request.done) // NOTE: Faster (Unity 2020.3.20f1)
                // if (request.done) // NOTE: Slower (Unity 2020.3.20f1)
                {
                    // Debug.Log($"[AsyncCaptureDriver] ***** Request[{i}] - Error: {request.hasError}, Done: {request.done}");
                    _nativeArrayBuffer[i].CopyTo(_rawDateBuffer[i]);
                    OnCapture?.Invoke(new Capture(){ Data = _rawDateBuffer[i], Format = _format, Width = _width, Height = _height });
                }
            }
        }

        private void OnDestroy()
        {
            AsyncGPUReadback.WaitAllRequests();

            if (_nativeArrayBuffer != null)
            {
                for (int i = 0; i < _nativeArrayBuffer.Length; i++)
                {
                    _nativeArrayBuffer[i].Dispose();
                }
            }

            _rawDateBuffer?.Clear();

            Debug.Log($"[AsyncCaptureDriver] OnDestroy");
        }

#endregion

    }
}
