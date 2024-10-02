using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public static class PerlinWormsGPU
{
    static System.Random random = new System.Random(GUIValues.instance.seed);
   
    static readonly int[] p = new int[512];
    static readonly int[] permutation = {
        151,160,137,91,90,15, 131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33, 88,237,149,56,87,174,20,
        125,136,171,168, 68,175,74,165,71,134,139,48,27,166, 77,146,158,231,83,111,229,122,60,211,133,230,
        220,105,92,41,55,46,245,40,244,102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,
        200,196,135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123, 5,202,38,147,
        118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,119,248,152, 2,44,
        154,163, 70,221,153,101,155,167, 43,172,9, 129,22,39,253, 19,98,108,110,79,113,224,232,178,185,
        112,104,218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,
        107, 49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,138,236,205,93,
        222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    static PerlinWormsGPU()
    {
        for (int i = 0; i < 256; i++)
        {
            p[256 + i] = p[i] = permutation[i];
        }
    }
    static public  void GenWorms(float[] pointCloud)
    {
       
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        //referennce compute shader from GUIValues 
        ComputeShader perlinWormShader = GUIValues.instance.PW_Compute_Shader;
        
        //setup kernels
        int localMaximaHandle = perlinWormShader.FindKernel("FindLocalMaxima");
        int perlinWormsHandle = perlinWormShader.FindKernel("PerlinWorms");
        //set up buffer for sending point cloud data
        ComputeBuffer pointCloudBuffer=new ComputeBuffer(pointCloud.Length,sizeof(float));
        pointCloudBuffer.SetData(pointCloud);
        
        //set up buffer for output of local maxima list
        ComputeBuffer localMaximaBuffer = new ComputeBuffer(pointCloud.Length / GUIValues.instance.maximaRadius, sizeof(int) * 3,ComputeBufferType.Append);
        localMaximaBuffer.SetCounterValue(0);

        //send buffers to compute shader
        perlinWormShader.SetBuffer(localMaximaHandle, "pointCloud", pointCloudBuffer);
        perlinWormShader.SetBuffer(localMaximaHandle, "localMaximaBuffer", localMaximaBuffer);
        perlinWormShader.SetFloat("cutoff", GUIValues.instance.cutoff);
        perlinWormShader.SetInt("size",GUIValues.instance.size);
        perlinWormShader.SetFloat("localMaximaRadius", GUIValues.instance.maximaRadius);
        
        //calculate thread groups for compute shader and dispatch
        int threadGroups = Mathf.CeilToInt(GUIValues.instance.size / 8.0f);
        perlinWormShader.Dispatch(localMaximaHandle,threadGroups, threadGroups, threadGroups);
        // get buffer count from GPU
        // source https://discussions.unity.com/t/appendstructuredbuffer-count-is-the-same-as-allocated-amount-for-the-mirroring-computebuffer-while-i-append-less-times/257627
        int localMaximaCount =GetBufferCount(localMaximaBuffer);
        UnityEngine.Debug.Log(localMaximaCount);
        Vector3Int[] localMaxima= new Vector3Int[localMaximaCount];
        
        //fetch data from buffer
        localMaximaBuffer.GetData(localMaxima);
        stopwatch.Stop();
        UnityEngine.Debug.Log("localMaximaSearch took " + stopwatch.ElapsedMilliseconds + " milliseconds");

        //set local maxima for perlin worms kernel
        ComputeBuffer localMaximaStructured = new ComputeBuffer(localMaximaCount, sizeof(int) * 3);
        localMaximaStructured.SetData(localMaxima);

        ComputeBuffer permutationBuffer = new ComputeBuffer(512, sizeof(int));
        permutationBuffer.SetData(p);

        Vector3[] octaveOffsets = new Vector3[GUIValues.instance.w_octaves];
        for (int i = 0; i < GUIValues.instance.w_octaves; i++)
        {
            octaveOffsets[i] = new Vector3(
               (float)random.Next(-100000, 100000),
                (float)random.Next(-100000, 100000),
                (float)random.Next(-100000, 100000)
            );
        }
        ComputeBuffer octaveOffsetsBuffer = new ComputeBuffer(GUIValues.instance.w_octaves, sizeof(float) * 3);
        octaveOffsetsBuffer.SetData(octaveOffsets);
        //set data for perlin worms kernel
        perlinWormShader.SetBuffer(perlinWormsHandle, "localMaxima", localMaximaStructured);
        perlinWormShader.SetBuffer(perlinWormsHandle, "octaveOffsets", octaveOffsetsBuffer);
        perlinWormShader.SetBuffer(perlinWormsHandle, "p", permutationBuffer);
        perlinWormShader.SetBuffer(perlinWormsHandle, "pointCloud", pointCloudBuffer);
        
        perlinWormShader.SetFloat ("fillRadius", GUIValues.instance.wormRadius);
        perlinWormShader.SetFloat("persistance", GUIValues.instance.w_persistance);
        perlinWormShader.SetFloat("lacunarity", GUIValues.instance.w_lacunarity);
        perlinWormShader.SetFloat("scale",GUIValues.instance.w_scale);
        perlinWormShader.SetInt("octaves", GUIValues.instance.w_octaves);
        perlinWormShader.SetInt("seed",GUIValues.instance.seed);
        perlinWormShader.SetInt("maximaCount",localMaximaCount);
        perlinWormShader.SetInt("wormLength", GUIValues.instance.wormLength);
        perlinWormShader.SetFloat("radiusFalloff",GUIValues.instance.falloff);

        threadGroups = Mathf.CeilToInt(GUIValues.instance.wormCount / 8);
        //dispatch perlin worms kernel
        perlinWormShader.Dispatch(perlinWormsHandle, threadGroups, 1, 1);
        // fetch point cloud data from buffer
        pointCloudBuffer.GetData(pointCloud);

        //realease buffers.
        localMaximaBuffer.Release();
        pointCloudBuffer.Release();
        localMaximaStructured.Release();
        permutationBuffer.Release();
        octaveOffsetsBuffer.Release();

       
       
    }

    static private int GetBufferCount(ComputeBuffer buffer)
    {
        // Create a temporary buffer to read the count
        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(buffer, countBuffer, 0);
        int[] countArray = { 0 };
        countBuffer.GetData(countArray);
        countBuffer.Release();

        return countArray[0];
    }

}
