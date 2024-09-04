using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class MarchingCubesMULTI
{

      static ConcurrentBag<Triangle> meshTriangles = new ConcurrentBag<Triangle>();
    
    public static void GenerateMarchingCubes(float[] pointCloud)
    {
        
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
            foreach(Triangle triangle in threadTriangles)
            {
                meshTriangles.Add(triangle);
            }
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
        
        for (int i = 0; i < 16; i += 3)
        {
            int triTableValue = MarchingTable.Triangles[configIndex, i];
            if (triTableValue == -1)
                break;
            Triangle triangle = new Triangle();
            triangle[0] = InterpolateVertex(position, cubeCorners, MarchingTable.Triangles[configIndex, i]);
            triangle[1] = InterpolateVertex(position, cubeCorners, MarchingTable.Triangles[configIndex, i + 1]);
            triangle[2] = InterpolateVertex(position, cubeCorners, MarchingTable.Triangles[configIndex, i + 2]);
            meshTriangles.Add(triangle);
        }
    }

    static Vector3 InterpolateVertex(Vector3 id, float[] gridVal, int edgeIndex)
    {

        Vector3 p1 = id + MarchingTable.Edges[edgeIndex, 0];
        Vector3 p2 = id + MarchingTable.Edges[edgeIndex, 1];

        float val1 = gridVal[MarchingTable.CornerIndices[edgeIndex, 0]];
        float val2 = gridVal[MarchingTable.CornerIndices[edgeIndex, 1]];

        if (Mathf.Abs(GUIValues.instance.cutoff - val1) < 0.00001)
            return p1;
        if (Mathf.Abs(GUIValues.instance.cutoff - val2) < 0.00001)
            return p2;
        if (Mathf.Abs(val1 - val2) < 0.00001)
            return p1;

        float t = (GUIValues.instance.cutoff - val1) / (val2 - val1);
        return Vector3.Lerp(p1, p2, t);
    }

    static public void SetMesh()
    {
        Triangle[] trianglesArray=meshTriangles.ToArray();
        Vector3[] vertices = new Vector3[meshTriangles.Count * 3];
        int[] triangles = new int[meshTriangles.Count * 3];
        for (int i = 0; i < meshTriangles.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                triangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = trianglesArray[i][j];


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
        meshTriangles.Clear();
    }
}
