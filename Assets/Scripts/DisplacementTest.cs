using System.IO.Compression;
using System.IO;
using System.Text;
using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DisplacementTest : MonoBehaviour
{
    [Tooltip("Material using the DisplacedLit shader. Must have a StructuredBuffer<float3> named _Displacements.")]
    public Material displacementMaterial;

    [Tooltip("Path to .vdisp file. Can be absolute or relative to Application.dataPath.")]
    public string vdispPath;

    [Tooltip("Object name in the .vdisp file to read displacements for.")]
    public string objectName;

    [Tooltip("Frame index to load (0-based).")]
    public int frameIndex = 0;

    [Tooltip("If true, reload automatically when frameIndex changes in inspector at runtime.")]
    public bool autoReload = true;

    private Mesh mesh;
    private ComputeBuffer displacementBuffer;
    private int vertexCount;

    // Internal file index
    private string resolvedPath;
    private long[] frameOffsets; // offsets to compressed frame blocks
    private int frameCount = 0;
    private bool headerLoaded = false;

    // Track inspector changes
    private int lastFrameIndex = -1;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        if (mesh == null)
        {
            Debug.LogError("[VDispLoader] MeshFilter.mesh is null.");
            enabled = false;
            return;
        }

        vertexCount = mesh.vertexCount;
        resolvedPath = ResolvePath(vdispPath);

        if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
        {
            Debug.LogError($"[VDispLoader] .vdisp not found: {vdispPath} (resolved: {resolvedPath})");
            enabled = false;
            return;
        }

        if (displacementMaterial == null)
        {
            Debug.LogError("[VDispLoader] displacementMaterial is not assigned.");
            enabled = false;
            return;
        }

        // Create ComputeBuffer (float3 per vertex)
        displacementBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3, ComputeBufferType.Default);
        displacementMaterial.SetBuffer("_Displacements", displacementBuffer);

        // Read header & table
        try
        {
            LoadHeaderAndTable(resolvedPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[VDispLoader] Failed to read .vdisp header: {e}");
            enabled = false;
            return;
        }

        // Load initial frame
        LoadFrame(frameIndex);
        lastFrameIndex = frameIndex;
    }

    void Update()
    {
        if (autoReload && frameIndex != lastFrameIndex)
        {
            LoadFrame(frameIndex);
            lastFrameIndex = frameIndex;
        }
    }

    void OnDisable()
    {
        if (displacementBuffer != null)
        {
            displacementBuffer.Release();
            displacementBuffer = null;
        }
    }

    // Public call to reload current frame on demand
    public void ReloadCurrentFrame()
    {
        LoadFrame(frameIndex);
        lastFrameIndex = frameIndex;
    }

    // Resolve relative paths (if the path starts with "Assets/" or is relative)
    private string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (Path.IsPathRooted(path)) return path;
        // try relative to project Assets
        string candidate = Path.Combine(Application.dataPath, path);
        if (File.Exists(candidate)) return candidate;
        // fallback: as given
        return Path.Combine(Application.dataPath, path); // still return expected full path for error messages
    }

    // --------- File format reading ----------
    // header layout (writer):
    // Write(FormatMagic as ascii 5 bytes "VDISP")
    // Write(short FormatVersion)
    // Write(cache.Meta.BaseFrame)  (int)
    // Write(cache.Meta.FrameStart) (int)
    // Write(cache.Meta.FrameEnd)   (int)
    // Write(cache.Meta.Fps)        (int)
    // int frameCount
    // then frameCount * (long) placeholder offsets (we will read them)
    private void LoadHeaderAndTable(string path)
    {
        using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true))
        {
            // Magic (5 bytes). The writer used fsw.Write(FormatMagic) which writes the ASCII chars.
            var magicBytes = br.ReadBytes(5);
            string magic = Encoding.ASCII.GetString(magicBytes);
            if (magic != "VDISP")
                throw new InvalidDataException($"Invalid magic: {magic}");

            short version = br.ReadInt16();
            // skip a few ints writer wrote: baseFrame, frameStart, frameEnd, fps
            int baseFrame = br.ReadInt32();
            int frameStart = br.ReadInt32();
            int frameEnd = br.ReadInt32();
            int fps = br.ReadInt32();

            frameCount = br.ReadInt32();
            if (frameCount <= 0) throw new InvalidDataException("Frame count is zero or negative.");

            // Read table of long offsets (writer used long/ulong; BinaryWriter.Write((long)0) later filled them)
            frameOffsets = new long[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                frameOffsets[i] = br.ReadInt64();
            }

            headerLoaded = true;
        }
    }

    private void LoadFrame(int frameIdx)
    {
        if (!headerLoaded) throw new InvalidOperationException("Header not loaded.");
        if (frameIdx < 0 || frameIdx >= frameCount)
        {
            Debug.LogError($"[VDispLoader] frameIndex {frameIdx} out of range (0..{frameCount - 1}).");
            return;
        }

        long frameOffset = frameOffsets[frameIdx];
        if (frameOffset <= 0)
        {
            Debug.LogError($"[VDispLoader] invalid frame offset for frame {frameIdx}: {frameOffset}");
            return;
        }

        Vector3[] displacements = new Vector3[vertexCount];
        bool foundObject = false;

        using (var fs = File.Open(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            fs.Seek(frameOffset, SeekOrigin.Begin);

            // Each frame chunk in the file is gzipped (writer wrapped a GZipStream there).
            using (var gz = new GZipStream(fs, CompressionMode.Decompress, leaveOpen: true))
            using (var br = new BinaryReader(gz, Encoding.ASCII))
            {
                // The decompressed chunk contains a sequence of objects:
                // for each object:
                //   int nameSize
                //   int vertexCount
                //   nameSize bytes (ASCII)
                //   (vertexCount * 3) floats (x,y,z) sequentially
                // We'll iterate objects until we hit end of the gzip stream (exception on read).
                try
                {
                    while (true)
                    {
                        int nameSize = br.ReadInt32();
                        int objVertexCount = br.ReadInt32();

                        // read exactly nameSize bytes
                        byte[] nameBytes = br.ReadBytes(nameSize);
                        if (nameBytes.Length != nameSize)
                            throw new EndOfStreamException("Unexpected EOF while reading name bytes.");
                        string name = Encoding.ASCII.GetString(nameBytes);

                        // Sanity check: objVertexCount should be >= 0
                        if (objVertexCount < 0)
                            throw new InvalidDataException($"Negative vertex count for object {name}");

                        // If this is the object we need, read its positions into the target array.
                        if (!foundObject && string.Equals(name, objectName, StringComparison.Ordinal))
                        {
                            foundObject = true;

                            if (objVertexCount != vertexCount)
                            {
                                Debug.LogWarning($"[VDispLoader] object vertex count {objVertexCount} != mesh vertex count {vertexCount}. " +
                                                 $"If they differ, we'll try to read min(objVertexCount, mesh.vertexCount) and ignore the rest.");
                            }

                            int readVerts = Math.Min(objVertexCount, vertexCount);

                            for (int v = 0; v < readVerts; v++)
                            {
                                float x = br.ReadSingle();
                                float y = br.ReadSingle();
                                float z = br.ReadSingle();
                                displacements[v] = new Vector3(x, y, z);
                            }

                            // if objVertexCount > mesh.vertexCount, skip remaining floats
                            if (objVertexCount > readVerts)
                            {
                                long toSkip = (long)(objVertexCount - readVerts) * 3L * sizeof(float);
                                SkipBytes(br, toSkip);
                            }
                        }
                        else
                        {
                            // not our object: skip objVertexCount * 3 floats
                            long bytesToSkip = (long)objVertexCount * 3L * sizeof(float);
                            SkipBytes(br, bytesToSkip);
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    // Reached end of this gzip chunk - normal termination for frame.
                }
                catch (IOException)
                {
                    // GZipStream might throw IOException on truncated reads; treat as end-of-chunk
                }
            } // gz, br disposed
        } // fs disposed

        if (!foundObject)
        {
            Debug.LogWarning($"[VDispLoader] Object '{objectName}' not found in frame {frameIdx} of {Path.GetFileName(resolvedPath)}.");
            // we still upload zeroed or last data to GPU to avoid undefined buffer content
        }

        // Upload to GPU
        displacementBuffer.SetData(displacements);
    }

    // BinaryReader doesn't have Seek on inner stream so we read and discard in blocks
    private void SkipBytes(BinaryReader br, long byteCount)
    {
        const int CHUNK = 4096;
        long remaining = byteCount;
        byte[] tmp = new byte[CHUNK];
        while (remaining > 0)
        {
            int r = br.Read(tmp, 0, (int)Math.Min(CHUNK, remaining));
            if (r <= 0) throw new EndOfStreamException("SkipBytes hit EOF");
            remaining -= r;
        }
    }
}
