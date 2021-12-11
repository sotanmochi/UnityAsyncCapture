using UnityEngine.Experimental.Rendering;

namespace AsyncCapture
{
    public struct Capture
    {
        public byte[] Data;
        public int Width;
        public int Height;
        public GraphicsFormat Format;
    }
}