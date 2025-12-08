using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using libVDisp;

[RequireComponent(typeof(MeshFilter), typeof(Renderer))]
public class VDispMeshPlayer : MonoBehaviour
{
    [Header("VDISP Source")]
    public string vdispFilePath;
    public string objectName;

    [Header("Playback")]
    public int frame;
    public bool realtimePlayback = true;
    public float displacementScale = 100f;

    [Header("Material")]
    public Material targetMaterial;

    private VDispDecoder decoder;
    private VDispHeader header;
    private ObjectTableEntry objectEntry;

    private ComputeBuffer displacementBuffer;

    private float[] decompressedDisplacements; // ALL frames, decompressed once

    private int vertexCount;
    private int frameCount;
    private float frameTimer;

    private int floatsPerFrame;

    void Start()
    {
        if (string.IsNullOrEmpty(vdispFilePath))
            throw new Exception("VDISP file path not set.");

        if (targetMaterial == null)
            throw new Exception("Target material not assigned.");

        decoder = new VDispDecoder(vdispFilePath);
        header = decoder.ReadHeader();

        objectEntry = header.ObjectTable.FirstOrDefault(o => o.ObjectName == objectName);
        if (string.IsNullOrEmpty(objectEntry.ObjectName))
            throw new Exception($"Object '{objectName}' not found in VDISP.");

        vertexCount = (int)objectEntry.VertexCount;
        frameCount = header.EndFrame - header.StartFrame + 1;
        floatsPerFrame = vertexCount * 3;

        // --- Decompress object block ONCE ---
        decompressedDisplacements = ReadAndDecompressObjectBlock(objectEntry);

        displacementBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        targetMaterial.SetBuffer("_Displacements", displacementBuffer);

        var mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh.vertexCount != vertexCount)
            Debug.LogError($"VERTEX COUNT MISMATCH: mesh={mesh.vertexCount} vdisp={vertexCount}");

        ApplyFrame(frame);
    }

    void Update()
    {
        if (!realtimePlayback)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / header.Fps;

        if (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frame++;

            if (frame >= frameCount)
                frame = 0;

            ApplyFrame(frame);
        }
    }

    void ApplyFrame(int targetFrame)
    {
        targetFrame = Mathf.Clamp(targetFrame, 0, frameCount - 1);

        int sourceOffset = targetFrame * floatsPerFrame;

        // Zero allocations. Direct slice upload to GPU.
        displacementBuffer.SetData(decompressedDisplacements, sourceOffset, 0, floatsPerFrame);
    }

    float[] ReadAndDecompressObjectBlock(ObjectTableEntry entry)
    {
        decoder.BaseStream.Seek(entry.DataBlockOffset, SeekOrigin.Begin);

        byte[] compressed = new byte[entry.DataBlockLength];
        decoder.BaseStream.Read(compressed, 0, compressed.Length);

        using MemoryStream compressedStream = new MemoryStream(compressed);
        using GZipStream gzip = new GZipStream(compressedStream, CompressionMode.Decompress);
        using BinaryReader reader = new BinaryReader(gzip);

        int totalFloatCount = frameCount * floatsPerFrame;
        float[] result = new float[totalFloatCount];

        for (int i = 0; i < totalFloatCount; i += 3) 
        {
            //swap y and z
            result[i] = reader.ReadSingle();
            result[i + 2] = reader.ReadSingle();
            result[i + 1] = reader.ReadSingle();
        }

        return result;
    }

    void OnWillRenderObject()
    {
        targetMaterial.SetFloat("_DisplacementScale", displacementScale);
    }

    void OnDestroy()
    {
        displacementBuffer?.Release();
        decoder?.Dispose();
    }
}
