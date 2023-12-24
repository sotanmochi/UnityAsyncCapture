using UniRx;
using UnityEngine;

namespace AsyncCapture.Samples
{
    public sealed class AsyncCapturePresenter : MonoBehaviour
    {
        [SerializeField] AsyncCaptureUIView _uiView;
        [SerializeField] AsyncCaptureMain _captureMain;

        private uint _frameCount;
        private bool _enableFrameCounter = true;

        void Awake()
        {
            _uiView.OnStop.Subscribe(_ =>
            {
                _enableFrameCounter = false;
                _captureMain.Stop();
            });
        }

        void Update()
        {
            if (!_enableFrameCounter) return;
            _uiView.SetFrameCount(_frameCount++);
        }
    }
}
