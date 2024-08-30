using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class Triangle
{
    public List<Vector3> tVertices = new List<Vector3>();
}
public class MarchingCubesSINGLE
{
    private static List<Triangle> meshTriangles = new List<Triangle>();
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
            
            Triangle triangle = new Triangle();
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
            //Debug.Log($"Triangle added at position: {position}, with vertices: {triangle.tVertices[0]}, {triangle.tVertices[1]}, {triangle.tVertices[2]}");
            meshTriangles.Add(triangle);
        }

    }

    static Vector3 InterpolateVertex(Vector3 a, Vector3 b, float x, float y)
    {
        float cutoff = GUIValues.instance.cutoff;
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
                vertices[i * 3 + j] = meshTriangles[i].tVertices[j];
                

            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        GUIValues.instance.meshFilter.sharedMesh = mesh;

        meshTriangles.Clear();
    }
}
