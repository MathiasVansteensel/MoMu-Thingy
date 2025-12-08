using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libVDisp
{
    public struct ObjectTableEntry
    {
        public string ObjectName { get; set; }
        public long DataBlockOffset { get; set; }
        public long DataBlockLength { get; set; }
        public long VertexCount { get; set; }
    }
}
