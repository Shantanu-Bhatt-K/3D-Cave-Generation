
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using System.Collections.Generic;

struct Triangle
{

    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Vector3 this[int i]
    {
        get
        {
            switch (i)
            {
                case 0:
                    return a;
                case 1:
                    return b;
                default:
                    return c;
            }
        }
        set
        {
            switch (i)
            {
                case 0:
                    a=value;
                    break;
                case 1:
                    b = value ;
                    break;
                default:
                    c = value ;
                    break;
            }
        }
    }
}
static public class MarchingCubesCompute
{
    static private ComputeBuffer pointCloudBuffer;  // Buffer for the scalar field
    static private ComputeBuffer trianglesBuffer;   // Buffer for triangles output
    static private int kernelHandle;
    static private int gridSize;
    static private int numVoxels;
    static List<Triangle> meshTriangles=new List<Triangle>();

    static public void GenerateMarchingCubes(float[] pointCloudData)
    {
        
        
        gridSize = GUIValues.instance.size;
        int chunkSize = (int)Mathf.Pow(2, GUIValues.instance.chunkSize);
        int loopCount=Mathf.CeilToInt(gridSize/chunkSize);

        pointCloudBuffer = new ComputeBuffer(GUIValues.instance.size * GUIValues.instance.size * GUIValues.instance.size, sizeof(float));
        pointCloudBuffer.SetData(pointCloudData);
        ComputeShader marchingCubesShader = GUIValues.instance.Marching_Cube_Shader;
        // Find the kernel
        kernelHandle = marchingCubesShader.FindKernel("CSMain");
        marchingCubesShader.SetFloat("isoLevel", GUIValues.instance.cutoff);
        marchingCubesShader.SetBuffer(kernelHandle, "pointCloud", pointCloudBuffer);
        marchingCubesShader.SetInt("size", gridSize);
        marchingCubesShader.SetInt("chunksize", chunkSize);
        numVoxels = chunkSize * chunkSize * chunkSize;
        int threadGroups = Mathf.CeilToInt(chunkSize / 8.0f);
        for (int i=0;i<loopCount; i++)
        {
            for(int j=0;j<loopCount;j++)
            {
                for (int k=0;k<loopCount;k++)
                {
                    int[] pos = { i*chunkSize, j*chunkSize, k * chunkSize };
                    marchingCubesShader.SetInts("chunkPos",pos);
                    trianglesBuffer = new ComputeBuffer(numVoxels * 5, sizeof(float) * 3 * 3, ComputeBufferType.Append);
                    trianglesBuffer.SetCounterValue(0);
                    marchingCubesShader.SetBuffer(kernelHandle, "triangles", trianglesBuffer);
                    marchingCubesShader.Dispatch(kernelHandle, threadGroups, threadGroups, threadGroups);
                    int triangleCount = GetBufferCount(trianglesBuffer);
                    Triangle[] ChunkTriangles = new Triangle[triangleCount];
                    trianglesBuffer.GetData(ChunkTriangles);
                    for(int a=0;a<triangleCount;a++)
                    {
                        meshTriangles.Add(ChunkTriangles[a]);
                    }
                    trianglesBuffer.Release();
                }
            }
        }
      


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
                vertices[i * 3 + j] = meshTriangles[i][j];


            }
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        GUIValues.instance.meshFilter.sharedMesh = mesh;
        ClearBuffer();
    }

    static void ClearBuffer()
    {
        // Release the compute buffers
        if (pointCloudBuffer != null) pointCloudBuffer.Release();
        
        if (trianglesBuffer != null) trianglesBuffer.Release();
        meshTriangles.Clear();
        // Remove SceneView callback after rendering is done in Edit mode

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


