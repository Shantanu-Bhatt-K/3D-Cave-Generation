using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MarchingCubes
{
    private static List<Vector3> vertices = new List<Vector3>();
    private static List<int> triangles = new List<int>();
    private static float[,,] values;
    private static float cutoff;
    private static MeshFilter meshFilter;
    private static float width, height, depth;


    public static void DevelopMesh(float[, ,] _mesh_data,float _cutoff, MeshFilter _meshFilter, int _width, int _height, int _depth)
    {
        
        values = _mesh_data;
        cutoff = _cutoff;
        meshFilter = _meshFilter;
        
        width = _width; height = _height; depth = _depth;
        MarchCubes();
        SetMesh();
    }

    static int GetConfigIndex(float[] cubeCorners)
    {
        int configIndex = 0;

        for (int i = 0; i < 8; i++)
        {
            if (cubeCorners[i] > cutoff)
            {
                configIndex |= 1 << i;
            }
        }

        return configIndex;
    }
    static void MarchCubes()
    {
        Debug.Log("Called Func");
        vertices.Clear();
        triangles.Clear();

        for (int x = 0; x < width-1; x++)
        {
            for (int y = 0; y < height-1; y++)
            {
                for (int z = 0; z < depth-1; z++)
                {
                    float[] cubeCorners = new float[8];

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                        cubeCorners[i] = values[corner.x, corner.y, corner.z];
                       
                    }

                    MarchCube(new Vector3(x, y, z), cubeCorners);
                }
            }
        }
    }

    static private void MarchCube(Vector3 position, float[] cubeCorners)
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

    private static Vector3 InterpolateVertex(Vector3 a, Vector3 b, float x, float y)
    {
       
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
        Debug.Log(vertices.Count);
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }
}
