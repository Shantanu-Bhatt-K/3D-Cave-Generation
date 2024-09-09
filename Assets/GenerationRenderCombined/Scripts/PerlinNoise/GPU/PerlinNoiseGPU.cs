using System;
using System.Drawing;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public static class PerlinNoiseGPU
{
   

    private static ComputeBuffer octaveOffsetBuffer;
    private static ComputeBuffer outputBuffer;
    private static ComputeBuffer pBuffer ;
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
    static readonly int[] p = new int[512];

    static  PerlinNoiseGPU()
    {
        for (int i = 0; i < 256; i++)
        {
            p[256 + i] = p[i] = permutation[i];
        }
    }
    static float CalculateValue(float distance, float maxDistance)
    {
        if (distance > maxDistance)
        {
            return 0f; // Outside the sphere
        }
        return 1f - (distance / maxDistance); // Linearly decrease from 1 to 0
    }

    // Function to flatten 3D indices (x, y, z) into a 1D index for the array
    static int FlattenIndex(int x, int y, int z, int size)
    {
        return z * size * size + y * size + x;
    }
    static void FillSphere(double[] array, int size, float radius)
    {
        // Calculate the center of the grid
        float center = (size - 1) / 2.0f;

        // Loop through each point in the 3D grid
        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Calculate the distance from the center of the grid
                    float distance = Vector3.Distance(new Vector3(x, y, z), new Vector3(center, center, center));

                    // Calculate the value based on the distance
                    float value = CalculateValue(distance, radius);

                    // Fill the value into the flattened array
                    int index = FlattenIndex(x, y, z, size);
                    array[index] = value;
                }
            }
        }
    }

    public static void GenerateMesh()
    {
        
            
        System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
        st.Start();
        pBuffer = new ComputeBuffer(512, sizeof(int));
        pBuffer.SetData(p);
        ComputeShader perlinNoiseCompute = GUIValues.instance.P_Compute_Shader;
        
        outputBuffer = new ComputeBuffer(GUIValues.instance.size * GUIValues.instance.size * GUIValues.instance.size, sizeof(double));
        
        Vector3[] octaveOffsets = new Vector3[GUIValues.instance.p_octaves];
        System.Random random = new System.Random(GUIValues.instance.seed);
        for (int i = 0; i < GUIValues.instance.p_octaves; i++)
        {
            octaveOffsets[i].x = random.Next(-100000, 100000);
            octaveOffsets[i].y = random.Next(-100000, 100000);
            octaveOffsets[i].z = random.Next(-100000, 100000);
        }
        octaveOffsetBuffer = new ComputeBuffer(GUIValues.instance.p_octaves, 3 * sizeof(float));
        octaveOffsetBuffer.SetData(octaveOffsets);
        
        perlinNoiseCompute.SetInt("size", GUIValues.instance.size);
        perlinNoiseCompute.SetFloat("scale", GUIValues.instance.p_scale);
        perlinNoiseCompute.SetInt("octaves", GUIValues.instance.p_octaves);
        perlinNoiseCompute.SetFloat("persistance", GUIValues.instance.p_persistance);
        perlinNoiseCompute.SetFloat("lacunarity", GUIValues.instance.p_lacunarity);
        perlinNoiseCompute.SetInt("seed", GUIValues.instance.seed);
        perlinNoiseCompute.SetBuffer(0, "octaveOffsets", octaveOffsetBuffer);
        perlinNoiseCompute.SetBuffer(0, "p", pBuffer);
        perlinNoiseCompute.SetBuffer(0, "outputBuffer", outputBuffer);
        
        
        int threadGroups = Mathf.CeilToInt(GUIValues.instance.size / 8.0f);

        perlinNoiseCompute.Dispatch(0, threadGroups, threadGroups, threadGroups);

        double[] pointCloud = new double[GUIValues.instance.size * GUIValues.instance.size * GUIValues.instance.size];
        st.Stop();
        Debug.Log("GPU Generation of point cloud took " + st.ElapsedMilliseconds + " milliseconds");
        st.Restart();
        if (GUIValues.instance.isDebug)
        {
            FillSphere(pointCloud, GUIValues.instance.size, (GUIValues.instance.size / 2) - 1);
        }
        else
        {
            outputBuffer.GetData(pointCloud);
           
        }
        st.Stop();
        Debug.Log("fetching of data took " + st.ElapsedMilliseconds + " milliseconds");
        st.Restart();
        RescaleValues(pointCloud);
        //PerlinWorms.GenWorms(noiseValues);
        st.Stop();
        Debug.Log("Rescaling of point cloud took " + st.ElapsedMilliseconds + " milliseconds");
        st.Restart();
        MarchingCubesCompute.GenerateMarchingCubes(pointCloud);
       
        st.Stop();
        Debug.Log("marching Cubes took " + st.ElapsedMilliseconds + " milliseconds");
        st.Restart();
        MarchingCubesCompute.SetMesh();
        st.Stop();
        Debug.Log("mesh Setup took" + st.ElapsedMilliseconds + " milliseconds");
        outputBuffer.Release();
        octaveOffsetBuffer.Release();
        pBuffer.Release();
    }

    static void RescaleValues(double[] noiseValues)
    {
        double minValue = double.MaxValue;
        double maxValue = double.MinValue;

        for (int i = 0; i < noiseValues.Length; i++)
        {
            
            if (noiseValues[i] < minValue) minValue = noiseValues[i];
            if (noiseValues[i] > maxValue) maxValue = noiseValues[i];
        }

        // Rescale the values
        for (int i = 0; i < noiseValues.Length; i++)
        {
            noiseValues[i] = (noiseValues[i] - minValue) / (maxValue - minValue);
        }
    }
    
}