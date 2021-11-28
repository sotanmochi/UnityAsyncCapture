using UnityEngine;

namespace AsyncCapture.Samples
{
    public class AsyncCaptureMain : MonoBehaviour
    {
        [SerializeField] AsyncCaptureDriver _captureDriver;

        void Awake()
        {
            UnityEngine.QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = 60;
            Debug.Log($"[AsyncCaptureMain] Target frame rate: {UnityEngine.Application.targetFrameRate}");

            var desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            _captureDriver.Initialize(null, 30, desktopPath);
            _captureDriver.StartAutoCapture();
        }

        void OnDestroy()
        {
            _captureDriver.StopAutoCapture();
        }
    }
}
