using UnityEngine;

public static class MarchingGPU
{
   public static void GenerateMesh(float[,,] values, int width,int height,int depth,int cutoff,int chunkSize,ComputeShader computeShader)
    {
        int totalsize = width * height * depth;
        ComputeBuffer valuesBuffer = new ComputeBuffer(totalsize, sizeof(float));
        ComputeBuffer vertexBuffer = new ComputeBuffer(totalsize * 3, sizeof(float) * 3, ComputeBufferType.Append);
        ComputeBuffer indexBuffer = new ComputeBuffer(totalsize * 3, sizeof(int), ComputeBufferType.Append);
        int numThreadsPerAxis = 8;

        float[] flattenedValues= new float[totalsize];
        for(int i = 0;i<depth;i++) 
        {
            for(int j=0;j<height;j++)
            {
                for(int k=0;k<width;k++)
                    flattenedValues[k+width*(j+depth*i)] = values[i,j,k];
            }
        }
        valuesBuffer.SetData(flattenedValues);
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
        computeShader.SetInt("depth", depth);
        computeShader.SetFloat("cutoff", cutoff);
        computeShader.SetBuffer(0, "values", valuesBuffer);
        computeShader.SetBuffer(0, "vertices", vertexBuffer);
        computeShader.SetBuffer(0, "indices", indexBuffer);

        // Dispatch compute shader for each chunk
        for (int x = 0; x < width; x += chunkSize)
        {
            for (int y = 0; y < height; y += chunkSize)
            {
                for (int z = 0; z < depth; z += chunkSize)
                {
                    Vector3Int chunkOffset = new Vector3Int(x, y, z);
                    computeShader.SetVector("chunkOffset", new Vector4(chunkOffset.x, chunkOffset.y, chunkOffset.z, 0));

                    int threadGroupsX = Mathf.CeilToInt(chunkSize / (float)numThreadsPerAxis);
                    int threadGroupsY = Mathf.CeilToInt(chunkSize / (float)numThreadsPerAxis);
                    int threadGroupsZ = Mathf.CeilToInt(chunkSize / (float)numThreadsPerAxis);
                    computeShader.Dispatch(0, threadGroupsX, threadGroupsY, threadGroupsZ);
                }
            }
        }

    }
}
