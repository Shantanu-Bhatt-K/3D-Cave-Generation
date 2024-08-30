using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MarchingCubesMulti
{
    private static List<Vector3> vertices = new List<Vector3>();
    private static List<int> triangles = new List<int>();
    private static object lockObject = new object();  // Lock object for synchronization

    public static void GenerateMarchingCubes(float[] pointCloud)
    {
        int size = GUIValues.instance.size;
        float cutoff = GUIValues.instance.cutoff;

        Parallel.For(0, (size - 1) * (size - 1) * (size - 1), index =>
        {
            int k = index / ((size - 1) * (size - 1));
            int j = (index / (size - 1)) % (size - 1);
            int i = index % (size - 1);

            float[] gridVal = new float[8];
            Vector3Int position = new Vector3Int(i, j, k);
            for (int l = 0; l < 8; l++)
            {
                Vector3Int corner = position + MarchingTable.Corners[l];
                gridVal[l] = pointCloud[corner.x + size * (corner.y + size * corner.z)];
            }

            List<Vector3> localVertices = new List<Vector3>();
            List<int> localTriangles = new List<int>();

            PolygonizeCube(new Vector3(i, j, k), gridVal, localVertices, localTriangles, cutoff);

            lock (lockObject)
            {
                int vertexOffset = vertices.Count;
                vertices.AddRange(localVertices);
                foreach (var triangle in localTriangles)
                {
                    triangles.Add(triangle + vertexOffset);
                }
            }
        });
    }

    static int GetConfigIndex(float[] cubeCorners, float cutoff)
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

    static private void PolygonizeCube(Vector3 position, float[] cubeCorners, List<Vector3> localVertices, List<int> localTriangles, float cutoff)
    {
        int configIndex = GetConfigIndex(cubeCorners, cutoff);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;
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

                Vector3 vertex = InterpolateVertex(edgeStart, edgeEnd, valueStart, valueEnd, cutoff);
                localVertices.Add(vertex);
                localTriangles.Add(localVertices.Count - 1);

                edgeIndex++;
            }
        }
    }

    private static Vector3 InterpolateVertex(Vector3 a, Vector3 b, float x, float y, float cutoff)
    {
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

    public static void SetMesh()
    {
        
        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        GUIValues.instance.meshFilter.sharedMesh = mesh;

        vertices.Clear();
        triangles.Clear();
    }
}
