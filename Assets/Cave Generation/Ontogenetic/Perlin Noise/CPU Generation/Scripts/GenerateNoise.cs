using UnityEngine;
public enum NoiseType
{
    CPU2D,
    CPU3D,
    CPU3D_Multi,
    GPU_3D
}
public class GenerateNoise : MonoBehaviour
{
    public NoiseType noiseType=NoiseType.CPU3D;
    public int width = 100;
    public int height = 100;
    public int depth = 100;
    public float scale = 0.3f;
    [Range(0f, 1f)]
    public float cutoff = 0.5f;
    
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
                float[,,] meshData= ImprovedNoise.Perlin3D(width, height, depth,cutoff,scale);
                gameObject.GetComponent<PerlinRenderer>().RenderCPU3D(meshData, new Vector3(width, height, depth));
                break;
            case NoiseType.CPU3D_Multi:
                break;
            case NoiseType.GPU_3D:
                break;
            default:
                break;

        }
    }

    public void ClearWindow()
    {
        gameObject.GetComponent<PerlinRenderer>().Clear();
    }

    
}
