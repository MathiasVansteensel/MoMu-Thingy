using libVDisp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libVDisp
{
    public struct VDispHeader
    {
        public short FormatVersion { get; internal set; }
        public int BaseFrame { get; internal set; }
        public int StartFrame { get; internal set; }
        public int EndFrame { get; internal set; }
        public int Fps { get; internal set; }
        public int ObjectCount { get; internal set; }

        public ObjectTableEntry[] ObjectTable { get; internal set; }
    }
}
