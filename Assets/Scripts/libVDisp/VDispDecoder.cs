using System.Text;
using System.IO;
using System;

namespace libVDisp
{
    public class VDispDecoder : IDisposable
    {
        //--=Header=--
        //magic bytes(5 bytes)
        //format version(int16)
        //base frame(int32)
        //start frame(int32)
        //end frame(int32)
        //fps(int32)
        //object count(int32)

        //--=Object table = --
        //    *for each object*
        //name length in bytes(int7, 8th bit meaning next byte is also length)
        //object name(string)
        //data block offset from start of file (int64)
        //data block length(int64)
        //vertex count(int64)

        //--=Object data blocks(compressed)=--
        //*foreach object*
        //vert offset(float32) x vertcount x 3

        private const string FormatMagic = "VDISP";
        private const short SupportedFormatVersion = 1;
        private static readonly Encoding FormatEncoding = Encoding.ASCII;

        private VDispHeader? header = null;
        private readonly bool leaveOpen = false;
        public Stream BaseStream { get; }

        public VDispDecoder(string filePath, bool leaveOpen = false) : this(File.OpenRead(filePath), leaveOpen)
        { }

        public VDispDecoder(Stream stream, bool leaveOpen = false)
        {
            BaseStream = stream;
            header = ReadHeader();
        }

        public VDispHeader ReadHeader()
        {
            //if (header is not null && header.HasValue)
            //    return header.Value;

            BaseStream.Seek(0, SeekOrigin.Begin);

            int byteCount = FormatEncoding.GetByteCount(FormatMagic);
            byte[] magicBytes = new byte[byteCount];
            BaseStream.Read(magicBytes, 0, byteCount);
            string magicString = FormatEncoding.GetString(magicBytes);
            if (magicString != FormatMagic)
                throw new InvalidDataException("Invalid VDisp file format.");

            VDispHeader newHeader = new();

            using (BinaryReader reader = new(BaseStream, FormatEncoding, leaveOpen: true))
            {
                newHeader.FormatVersion = reader.ReadInt16();
                if (newHeader.FormatVersion != SupportedFormatVersion)
                    throw new NotSupportedException($"Unsupported VDisp format version: {newHeader.FormatVersion}");

                newHeader.BaseFrame = reader.ReadInt32();
                newHeader.StartFrame = reader.ReadInt32();
                newHeader.EndFrame = reader.ReadInt32();
                newHeader.Fps = reader.ReadInt32();
                newHeader.ObjectCount = reader.ReadInt32();

                //obj table
                ObjectTableEntry[] objectTable = new ObjectTableEntry[newHeader.ObjectCount];

                for (int i = 0; i < objectTable.Length; i++)
                {
                    ObjectTableEntry entry = new();
                    entry.ObjectName = reader.ReadString();
                    entry.DataBlockOffset = reader.ReadInt64();
                    entry.DataBlockLength = reader.ReadInt64();
                    entry.VertexCount = reader.ReadInt64();
                    objectTable[i] = entry;
                }

                newHeader.ObjectTable = objectTable;
            }

            return newHeader;
        }

        //public FrameCache Read(string objectName) 
        //{

        //}

        public void Dispose()
        {
            if (!leaveOpen) BaseStream?.Dispose();
        }
    }
}
