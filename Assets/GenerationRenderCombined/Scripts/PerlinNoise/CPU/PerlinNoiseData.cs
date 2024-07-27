using UnityEngine;
using System;
using System.Drawing;
using Unity.VisualScripting;
using static UnityEngine.Rendering.DebugUI;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.UIElements;
using NUnit.Framework.Internal;

public static class PerlinNoiseData 
{

    private static List<Vector3> vertices = new List<Vector3>();
    private static List<int> triangles = new List<int>();
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
    
    static PerlinNoiseData()
    {
        for (int i = 0; i < 256; i++)
        {
            p[256 + i] = p[i] = permutation[i];
        }
    }
    public static void GenerateMesh()
    {
        System.Random random=new System.Random(GUIValues.instance.seed);
        Vector3[] octaveOffsets= new Vector3[GUIValues.instance.p_octaves];
        for (int i = 0; i < GUIValues.instance.p_octaves; i++)
        {
            octaveOffsets[i].x = random.Next(-100000, 100000);
            octaveOffsets[i].y = random.Next(-100000, 100000);
            octaveOffsets[i].z = random.Next(-100000, 100000);
        }
        int size = GUIValues.instance.size;
        float[,,] pointCloud = new float[size, size, size];
        float min_value = float.MaxValue;
        float max_value = float.MinValue;
        for (int k = 0; k < size; k++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    float amplitude = 1f;
                    float frequency = 1f;
                    
                    for (int l = 0; l < GUIValues.instance.p_octaves; l++)
                    {
                        float val = (float)Noise(i /GUIValues.instance.p_scale*frequency + octaveOffsets[l].x, j / GUIValues.instance.p_scale * frequency + octaveOffsets[l].y, k / GUIValues.instance.p_scale * frequency + octaveOffsets[l].z);
                        pointCloud[i, j, k] += val * amplitude;
                       
                        amplitude *= GUIValues.instance.p_persistance;
                        frequency *= GUIValues.instance.p_lacunarity;
                    }
                    if (pointCloud[i,j,k]<min_value)
                        min_value = pointCloud[i,j,k];
                    if (pointCloud[i,j,k]>max_value)
                        max_value = pointCloud[i,j,k];
                    
                }
            }
        }
       // Debug.Log(max_value);
        //Debug.Log(min_value);
        RescaleValues(pointCloud,min_value,max_value);

        GenerateMarchingCubes(pointCloud);

        SetMesh();
    }


    static public void RescaleValues(float[,,] pointCloud, float min_value,float max_value)
    {
        int size = GUIValues.instance.size; // Cache the size value

        for (int k = 0; k < size; k++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    float value = pointCloud[i, j, k];
                    pointCloud[i, j, k] = (value - min_value) / (max_value - min_value);
                }
            }
        }
    }
    public static double Noise(double x, double y, double z)
    {
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;
        int Z = (int)Math.Floor(z) & 255;

        x -= Math.Floor(x);
        y -= Math.Floor(y);
        z -= Math.Floor(z);

        double u = Fade(x);
        double v = Fade(y);
        double w = Fade(z);

        int A = p[X] + Y;
        int AA = p[A] + Z;
        int AB = p[A + 1] + Z;
        int B = p[X + 1] + Y;
        int BA = p[B] + Z;
        int BB = p[B + 1] + Z;

        return Lerp(w, Lerp(v, Lerp(u, Grad(p[AA], x, y, z), Grad(p[BA], x - 1, y, z)), Lerp(u, Grad(p[AB], x, y - 1, z), Grad(p[BB], x - 1, y - 1, z))), Lerp(v, Lerp(u, Grad(p[AA + 1], x, y, z - 1), Grad(p[BA + 1], x - 1, y, z - 1)), Lerp(u, Grad(p[AB + 1], x, y - 1, z - 1), Grad(p[BB + 1], x - 1, y - 1, z - 1))));
    }

    private static double Fade(double t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static double Lerp(double t, double a, double b)
    {
        return a + t * (b - a);
    }

    private static double Grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : h == 12 || h == 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }


    private static void GenerateMarchingCubes(float[,,] pointCloud)
    {
        int size = GUIValues.instance.size;
        
        for(int i=0;i < size-1;i++)
        {
            for(int j=0;j<size-1;j++)
            {
                for(int k=0;k<size-1;k++)
                {
                    float[] gridVal = new float[8];
                   
                    for(int l=0;l<8;l++)
                    {
                        Vector3Int corner = new Vector3Int(i, j, k) +MarchingTable.Corners[l];
                        
                        gridVal[l] = pointCloud[corner.x,corner.y,corner.z];
                    }

                    PolygonizeCube(new Vector3(i,j,k),gridVal);
                }
            }
        }
       
    }

    static int GetConfigIndex(float[] cubeCorners)
    {
        int configIndex = 0;

        for (int i = 0; i < 8; i++)
        {
            if (cubeCorners[i] > GUIValues.instance.cutoff)
            {
                
                configIndex |= 1 << i;
            }
        }

        return configIndex;
    }
    static private void PolygonizeCube(Vector3 position, float[] cubeCorners)
    {
        int configIndex = GetConfigIndex(cubeCorners);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }
        

        int edgeIndex = 0;
       // Debug.Log("position=" + position);
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];

                if (triTableValue == -1)
                {
                    return;
                }
                
                Vector3 edgeStart = position + MarchingTable.Edges[triTableValue, 0];
                Vector3 edgeEnd = position + MarchingTable.Edges[triTableValue, 1];

                float valueStart = cubeCorners[MarchingTable.CornerIndices[triTableValue, 0]];
                float valueEnd = cubeCorners[MarchingTable.CornerIndices[triTableValue, 1]];

                Vector3 vertex = InterpolateVertex(edgeStart, edgeEnd, valueStart, valueEnd);
                //vertex = (edgeStart + edgeEnd) / 2;
                vertices.Add(vertex);
                triangles.Add(vertices.Count - 1);

                edgeIndex++;
            }
        }

    }

    private static Vector3 InterpolateVertex(Vector3 a, Vector3 b,float x, float y)
    {
        float cutoff= GUIValues.instance.cutoff;
        if (Mathf.Abs(cutoff - x) < 0.00001)
            return (a);
        if (Mathf.Abs(cutoff - y) < 0.00001)
            return (b);
        if (Mathf.Abs(x - y) < 0.00001)
            return (a);

        float mu = (cutoff - x) / (y - x);
        mu = Mathf.Clamp01(mu);  // Ensure t is clamped between 0 and 1
        Vector3 point = a + mu * (b - a);
        //Debug.Log("p1=" + a + ", p2=" + b + ",p=" + point + ",mu=" + mu);
        return point;
        

        
    }

    static private void SetMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        //Debug.Log(vertices.Count);
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        GUIValues.instance.meshFilter.sharedMesh = mesh;
    }
}
