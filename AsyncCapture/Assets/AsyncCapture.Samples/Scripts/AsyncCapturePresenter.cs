using UnityEngine;

namespace AsyncCapture.Samples
{
    public sealed class AsyncCapturePresenter : MonoBehaviour
    {
        [SerializeField] AsyncCaptureUIView _uiView;

        private uint _frameCount;

        void Update()
        {
            _uiView.SetFrameCount(_frameCount++);
        }
    }
}
