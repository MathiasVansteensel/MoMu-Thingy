using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace libVDisp
{
    public class FrameCache
    {
        public string ObjectName { get; }
        public int StartFrame { get; }
        public int EndFrame { get; }
        public int BaseFrame { get; }
        public int Fps { get; }
        public Vector3[][] Frames { get; }

        public FrameCache(Vector3[][] frames)
        {
            Frames = frames;
        }

        //public Vector3[] GetFrame(int index)
        //{

        //}
    }
}