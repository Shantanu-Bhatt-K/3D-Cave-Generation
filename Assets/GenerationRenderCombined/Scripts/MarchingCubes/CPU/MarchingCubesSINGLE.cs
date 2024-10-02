using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;


public class MarchingCubesSINGLE
{
    //referenced from Paul Bourke and sebastian lague
    //source https://paulbourke.net/geometry/polygonise/
    //source https://www.youtube.com/watch?v=M3iI2l0ltbE&t=109s
    private static List<Triangle> meshTriangles = new List<Triangle>();
    
    // loop throught the grid, setting cubes 
    public static void GenerateMarchingCubes(float[,,] pointCloud)
    {
        int size = GUIValues.instance.size;

        for (int i = 0; i < size - 1; i++)
        {
            for (int j = 0; j < size - 1; j++)
            {
                for (int k = 0; k < size - 1; k++)
                {
                    float[] gridVal = new float[8];

                    for (int l = 0; l < 8; l++)
                    {
                        Vector3Int corner = new Vector3Int(i, j, k) + MarchingTable.Corners[l];

                        gridVal[l] = pointCloud[corner.x, corner.y, corner.z];
                    }

                    PolygonizeCube(new Vector3(i, j, k), gridVal);
                }
            }
        }

    }
    public static void GenerateMarchingCubes(float[] pointCloud)
    {
        int size = GUIValues.instance.size;

        for (int i = 0; i < size - 1; i++)
        {
            for (int j = 0; j < size - 1; j++)
            {
                for (int k = 0; k < size - 1; k++)
                {
                    float[] gridVal = new float[8];

                    for (int l = 0; l < 8; l++)
                    {
                        Vector3Int corner = new Vector3Int(i, j, k) + MarchingTable.Corners[l];

                        gridVal[l] = pointCloud[corner.x + size * (corner.y + size * corner.z)];
                    }

                    PolygonizeCube(new Vector3(i, j, k), gridVal);
                }
            }
        }


    }
    // find configuration of the cube out of 256 configurations
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
        for(int i = 0;i < 16;i+=3) 
        {
            //tri table index is used to find the edges that are being intersected 
            int triTableValue = MarchingTable.Triangles[configIndex,i];
            if (triTableValue == -1)
                break;
            Triangle triangle = new Triangle();
            triangle[0] = InterpolateVertex(position, cubeCorners, MarchingTable.Triangles[configIndex,i]);
            triangle[1] = InterpolateVertex(position, cubeCorners, MarchingTable.Triangles[configIndex,i+1]);
            triangle[2] = InterpolateVertex(position, cubeCorners, MarchingTable.Triangles[configIndex,i+2]);
            meshTriangles.Add(triangle);
        }
        
        

    }
    //used to find the position of the intersection point on the edge based on vertex values
    static Vector3 InterpolateVertex(Vector3 id, float[] gridVal, int edgeIndex)
    {
       
        Vector3 p1 = id + MarchingTable.Edges[edgeIndex,0];
        Vector3 p2 = id + MarchingTable.Edges[edgeIndex, 1];

        float val1 = gridVal[MarchingTable.CornerIndices[edgeIndex,0]];
        float val2 = gridVal[MarchingTable.CornerIndices[edgeIndex,1]];

        if (Mathf.Abs(GUIValues.instance.cutoff - val1) < 0.00001)
            return p1;
        if (Mathf.Abs(GUIValues.instance.cutoff - val2) < 0.00001)
            return p2;
        if (Mathf.Abs(val1 - val2) < 0.00001)
            return p1;

        float t = (GUIValues.instance.cutoff - val1) / (val2 - val1);
        return Vector3.Lerp(p1, p2, t);
    }

    // set up values from the triangle list to vertices,indices and set them to unity mesh
    static public void SetMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        Vector3[] vertices = new Vector3[meshTriangles.Count * 3];
        int[] triangles = new int[meshTriangles.Count * 3];
        for (int i = 0; i < meshTriangles.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                triangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = meshTriangles[i][j];
                

            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        GUIValues.instance.meshFilter.sharedMesh = mesh;
        //GUIValues.instance.meshCollider.sharedMesh = mesh;
        meshTriangles.Clear();
    }
}
