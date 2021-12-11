using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace AsyncCapture.Samples
{
    public class AsyncCaptureMain : MonoBehaviour
    {
        [SerializeField] AsyncCaptureDriver _captureDriver;

        private string _saveDirectoryPath;
        private int _saveImageCount;

        void Awake()
        {
            UnityEngine.QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = 60;
            Debug.Log($"[AsyncCaptureMain] Target frame rate: {UnityEngine.Application.targetFrameRate}");

            var desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            _saveDirectoryPath = $"{desktopPath}/AsyncCapture_{DateTime.Now.ToString("yyyy_MMdd_HHmmss")}";
            Directory.CreateDirectory(_saveDirectoryPath);

            Debug.Log($"[AsyncCaptureMain] Save directory path: {_saveDirectoryPath}");

            _captureDriver.Initialize(null, 30);
            _captureDriver.OnCapture += OnCapture;
            _captureDriver.StartAutoCapture();
        }

        void OnDestroy()
        {
            _captureDriver.OnCapture -= OnCapture;
            _captureDriver.StopAutoCapture();
        }

        private void OnCapture(Capture capture)
        {
            // Run another thread
            Task.Run(() => 
            {
                try
                {
                    // Debug.Log($"[AsyncCaptureDriver] Output processing thread id: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    var pngImageBytes = ImageConversion.EncodeArrayToPNG(capture.Data, capture.Format, (uint)capture.Width, (uint)capture.Height);
                    SavePngImage(pngImageBytes);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.StackTrace);
                }
            });
        }

        private async void SavePngImage(byte[] pngImageBytes)
        {
			var savePath = $"{_saveDirectoryPath}/AsyncCaptureImage_{_saveImageCount++}.png";

            using (var sourceStream = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 8192, true))
            {
                await sourceStream.WriteAsync(pngImageBytes, 0, pngImageBytes.Length);
            }
        }
    }
}
