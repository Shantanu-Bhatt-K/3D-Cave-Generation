
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
struct TriangleGPU
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
    }
}
static public class MarchingCubesCompute
{
    static private ComputeBuffer pointCloudBuffer;  // Buffer for the scalar field
    static private ComputeBuffer trianglesBuffer;   // Buffer for triangles output
    static private int kernelHandle;
    static private int gridSize;
    static private int numVoxels;

    static public void GenerateMarchingCubes(float[] pointCloudData)
    {
        gridSize = GUIValues.instance.size;
        numVoxels = gridSize * gridSize * gridSize;

        pointCloudBuffer = new ComputeBuffer(numVoxels, sizeof(float));
        
        trianglesBuffer = new ComputeBuffer(numVoxels * 5, sizeof(float)*3*3, ComputeBufferType.Append);
        trianglesBuffer.SetCounterValue(0);
        // Set initial data or fill with noise, density field, etc.
        pointCloudBuffer.SetData(pointCloudData);

        ComputeShader marchingCubesShader = GUIValues.instance.Marching_Cube_Shader;
        // Find the kernel
        kernelHandle = marchingCubesShader.FindKernel("CSMain");

        // Set the shader parameters
        marchingCubesShader.SetInt("size", gridSize);
        marchingCubesShader.SetFloat("isoLevel", GUIValues.instance.cutoff);
        marchingCubesShader.SetBuffer(kernelHandle, "pointCloud", pointCloudBuffer);
        marchingCubesShader.SetBuffer(kernelHandle, "triangles", trianglesBuffer);

        // Dispatch the compute shader
        int threadGroups = Mathf.CeilToInt(gridSize / 8.0f);
        marchingCubesShader.Dispatch(kernelHandle, threadGroups, threadGroups, threadGroups);

        // Render in Edit mode using SceneView callbacks


    }



    static public void SetMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        int triangleCount = GetBufferCount(trianglesBuffer);
        TriangleGPU[] meshTriangles= new TriangleGPU[triangleCount];
        trianglesBuffer.GetData(meshTriangles);
        Vector3[] vertices = new Vector3[triangleCount * 3];
        int[] triangles = new int[triangleCount * 3];
        for (int i = 0; i < triangleCount; i++)
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

    }

    static void ClearBuffer()
    {
        // Release the compute buffers
        if (pointCloudBuffer != null) pointCloudBuffer.Release();
        
        if (trianglesBuffer != null) trianglesBuffer.Release();

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


