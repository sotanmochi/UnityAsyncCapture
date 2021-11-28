using UnityEngine;
using UnityEngine.UI;

namespace AsyncCapture.Samples
{
    public sealed class AsyncCaptureUIView : MonoBehaviour
    {
        [SerializeField] Text _frameCount;

        public void SetFrameCount(uint frameCount)
        {
            _frameCount.text = $"FrameCount: {frameCount}";
        }
    }
}
