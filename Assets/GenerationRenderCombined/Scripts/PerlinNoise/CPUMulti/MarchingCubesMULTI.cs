using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class MarchingCubesMULTI
{

    private static  List<Triangle> meshTriangles= new List<Triangle>();
    static int index;
    public static void GenerateMarchingCubes(float[] pointCloud)
    {
        index = 0;
        int size = GUIValues.instance.size;


        Parallel.For(0, (size - 1) * (size - 1) * (size - 1), index =>
        {
            int k = index / ((size - 1) * (size - 1));
            int j = (index / (size - 1)) % (size - 1);
            int i = index % (size - 1);
            //Debug.Log(string.Format("indexes i, {0} , j, {1}, k, {2}", i, j, k));
            float[] gridVal = new float[8];
            Vector3Int position = new Vector3Int(i, j, k);
            for (int l = 0; l < 8; l++)
            {
                Vector3Int corner = position + MarchingTable.Corners[l];
                gridVal[l] = pointCloud[corner.x + size * (corner.y + size * corner.z)];
            }

            List<Triangle> threadTriangles = new List<Triangle>();   
            PolygonizeCube(new Vector3(i, j, k), gridVal,threadTriangles);
            meshTriangles.AddRange(threadTriangles);
        });

           
           

        
        
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

    static private void PolygonizeCube(Vector3 position, float[] cubeCorners, List<Triangle> threadTriangles)
    {
        int configIndex = GetConfigIndex(cubeCorners);
        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
        {
            Triangle triangle = new Triangle() ;
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
                triangle.tVertices.Add(vertex);
                
               
                edgeIndex++;
            }
            threadTriangles.Add(triangle);
        }
    }

    private static Vector3 InterpolateVertex(Vector3 a, Vector3 b, float x, float y)
    {
        float cutoff = GUIValues.instance.cutoff;
        if (Mathf.Abs(cutoff - x) < 0.00001f)
            return a;
        if (Mathf.Abs(cutoff - y) < 0.00001f)
            return b;
        if (Mathf.Abs(x - y) < 0.00001f)
            return a;

        float mu = (cutoff - x) / (y - x);
        mu = Mathf.Clamp01(mu);
        return a + mu * (b - a);
    }

    static public void SetMesh()
    {
        Vector3[] vertices = new Vector3[meshTriangles.Count * 3];
        int[] triangles = new int[meshTriangles.Count * 3];
        for (int i = 0; i < meshTriangles.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                triangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = meshTriangles[i].tVertices[j];


            }
        }
        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
       
        //foreach(var vertex in vertices)
        //{
        //    Debug.Log(vertex);
        //}
        
        GUIValues.instance.meshFilter.sharedMesh = mesh;
    }
}
