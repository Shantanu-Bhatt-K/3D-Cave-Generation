using UnityEngine;
public enum NoiseType
{
    CPU2D,
    CPU3D,
    CPU3D_Multi,
    GPU_3D,
    Worm3D,
    WormMulti,
    Worm_Perlin
}
public class GenerateNoise : MonoBehaviour
{
    public NoiseType noiseType=NoiseType.CPU3D;
    public bool RenderNoise = true;
    public int width = 100;
    public int height = 100;
    public int depth = 100;
    public float scale = 0.3f;
    public int seed;
    public int octaves;
    public float lacunarity;
    public float persistance;
    public ComputeShader noiseShader;
    [Range(0f, 1f)]
    public float cutoff = 0.5f;
   

    [Header("Worm Data")]
    public int wormCount=10;
    public int wormLength = 30;
    public float wormRadius = 3f;


    [Header("Marching Cubes Data")]
    public MeshFilter meshFilter;
    public bool isMarchingCubes;

    
    public void GenerateMap()
    {
        if (scale <= 0)
            scale = 0.0001f;
        ClearWindow();

        switch (noiseType)
        {
            case NoiseType.CPU2D:
                break;
            case NoiseType.CPU3D:
                float[,,] meshData= Perlin_3d_Calc.MeshData_Gen(width, height, depth,scale,seed,octaves,lacunarity,persistance);
                if(RenderNoise)
                    if(!isMarchingCubes)
                        gameObject.GetComponent<PerlinRenderer>().RenderCPU3D(meshData, new Vector3(width, height, depth),cutoff);
                    else
                        MarchingCubes.DevelopMesh(meshData,cutoff,meshFilter,width,height,depth);
                break;
            case NoiseType.CPU3D_Multi:
                float[,,] meshDataMulti = Perlin_3D_Multi.MeshDataGen(width, height, depth, scale, seed, octaves, lacunarity, persistance);
                if (RenderNoise)
                    if(!isMarchingCubes)
                        gameObject.GetComponent<PerlinRenderer>().RenderCPU3D(meshDataMulti, new Vector3(width, height, depth), cutoff);
                    else
                        MarchingCubes.DevelopMesh(meshDataMulti, cutoff, meshFilter, width, height, depth);
                break;
            case NoiseType.GPU_3D:
                float[,,] meshDataGPU = Perlin_3D_GPU.MeshDataGen(width, height, depth, scale, seed, octaves, lacunarity, persistance,noiseShader);
                if (RenderNoise)
                    if (!isMarchingCubes)
                        gameObject.GetComponent<PerlinRenderer>().RenderCPU3D(meshDataGPU, new Vector3(width, height, depth), cutoff);
                    else
                        MarchingCubes.DevelopMesh(meshDataGPU, cutoff, meshFilter, width, height, depth);
                break;
            case NoiseType.Worm3D:
                float[,,] meshData_Worm=new float[width, height, depth];
                PerlinWormCPU.MeshData_Gen(width, height, depth, scale, seed, octaves, lacunarity, persistance, ref meshData_Worm, wormCount, wormLength, wormRadius);
                if (RenderNoise)
                    if (!isMarchingCubes)
                        gameObject.GetComponent<PerlinRenderer>().RenderCPU3D(meshData_Worm, new Vector3(width, height, depth), cutoff);
                    else
                        MarchingCubes.DevelopMesh(meshData_Worm, cutoff, meshFilter, width, height, depth);
                break;
            case NoiseType.WormMulti:
                float[,,] meshData_WormMulti = new float[width, height, depth];
                PerlinWormMulti.MeshData_Gen(width, height, depth, scale, seed, octaves, lacunarity, persistance, meshData_WormMulti, wormCount, wormLength, wormRadius);
                if (RenderNoise)
                    if (!isMarchingCubes)
                        gameObject.GetComponent<PerlinRenderer>().RenderCPU3D(meshData_WormMulti, new Vector3(width, height, depth), cutoff);
                    else
                        MarchingCubes.DevelopMesh(meshData_WormMulti, cutoff, meshFilter, width, height, depth);
                break;
            case NoiseType.Worm_Perlin:
                float[,,] meshData_WormPerlin = Perlin_3D_GPU.MeshDataGen(width, height, depth, scale, seed, octaves, lacunarity, persistance, noiseShader);
                PerlinWormCPU.MeshData_Gen(width, height, depth, scale, seed, octaves, lacunarity, persistance, ref meshData_WormPerlin, wormCount, wormLength, wormRadius);
                if (RenderNoise)
                    if (!isMarchingCubes)
                        gameObject.GetComponent<PerlinRenderer>().RenderCPU3D(meshData_WormPerlin, new Vector3(width, height, depth), cutoff);
                    else
                        MarchingCubes.DevelopMesh(meshData_WormPerlin, cutoff, meshFilter, width, height, depth);
                break;

            default:
                break;

        }
    }

    public void ClearWindow()
    {
        gameObject.GetComponent<PerlinRenderer>().Clear();
        meshFilter.sharedMesh = new Mesh();
    }

    
}
