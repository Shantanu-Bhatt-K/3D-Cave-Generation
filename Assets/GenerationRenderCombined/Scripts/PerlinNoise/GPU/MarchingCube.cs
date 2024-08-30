#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Unity.Mathematics;

static public class MarchingCubesCompute
{
    static private ComputeBuffer pointCloudBuffer;  // Buffer for the scalar field
    static private ComputeBuffer verticesBuffer;    // Buffer for vertices output
    static private ComputeBuffer trianglesBuffer;   // Buffer for triangles output
    static private int kernelHandle;
    static private int gridSize;
    static private int numVoxels;

    static public void GenerateMarchingCubes(float[] pointCloudData)
    {
        gridSize = GUIValues.instance.size;
        numVoxels = gridSize * gridSize * gridSize;

        pointCloudBuffer = new ComputeBuffer(numVoxels, sizeof(float));
        verticesBuffer = new ComputeBuffer(numVoxels * 5, sizeof(float) * 3, ComputeBufferType.Append);
        trianglesBuffer = new ComputeBuffer(numVoxels * 5, sizeof(int), ComputeBufferType.Append);

        // Set initial data or fill with noise, density field, etc.
        pointCloudBuffer.SetData(pointCloudData);

        ComputeShader marchingCubesShader = GUIValues.instance.Marching_Cube_Shader;
        // Find the kernel
        kernelHandle = marchingCubesShader.FindKernel("CSMain");

        // Set the shader parameters
        marchingCubesShader.SetInt("size", gridSize);
        marchingCubesShader.SetFloat("isoLevel", GUIValues.instance.cutoff);
        marchingCubesShader.SetBuffer(kernelHandle, "pointCloud", pointCloudBuffer);
        marchingCubesShader.SetBuffer(kernelHandle, "vertices", verticesBuffer);
        marchingCubesShader.SetBuffer(kernelHandle, "triangles", trianglesBuffer);

        // Dispatch the compute shader
        int threadGroups = Mathf.CeilToInt(gridSize / 8.0f);
        marchingCubesShader.Dispatch(kernelHandle, threadGroups, threadGroups, threadGroups);

        // Render in Edit mode using SceneView callbacks
#if UNITY_EDITOR
        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.RepaintAll();
#endif
    }

#if UNITY_EDITOR
    static private void OnSceneGUI(SceneView sceneView)
    {
        RenderMesh();
    }
#endif

    static private void RenderMesh()
    {
        int vertexCount = GetBufferCount(verticesBuffer);
        int triangleCount = GetBufferCount(trianglesBuffer);

        // Print out the counts
        Debug.Log($"Generated {vertexCount} vertices and {triangleCount / 3} triangles.");
        Material material = new Material(Shader.Find("Custom/MarchingCubesShader"));

        material.SetBuffer("verticesBuffer", verticesBuffer);
        material.SetBuffer("trianglesBuffer", trianglesBuffer);

        // Render the procedural mesh
        Graphics.DrawProceduralNow(MeshTopology.Triangles, trianglesBuffer.count, 1);

        // Optional: Clear buffers after rendering in Edit mode
#if UNITY_EDITOR
        ClearBuffer();
#endif
    }

    static void ClearBuffer()
    {
        // Release the compute buffers
        if (pointCloudBuffer != null) pointCloudBuffer.Release();
        if (verticesBuffer != null) verticesBuffer.Release();
        if (trianglesBuffer != null) trianglesBuffer.Release();

        // Remove SceneView callback after rendering is done in Edit mode
#if UNITY_EDITOR
        SceneView.duringSceneGui -= OnSceneGUI;
#endif
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


