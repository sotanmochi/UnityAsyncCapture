using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace AsyncCapture.Samples
{
    public sealed class AsyncCaptureUIView : MonoBehaviour
    {
        [SerializeField] Text _frameCount;
        [SerializeField] Button _stop;

        public IObservable<Unit> OnStop => _stop.OnClickAsObservable();

        public void SetFrameCount(uint frameCount)
        {
            _frameCount.text = $"FrameCount: {frameCount}";
        }
    }
}
