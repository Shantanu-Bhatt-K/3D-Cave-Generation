using Unity.VisualScripting;
using UnityEngine;
using static TreeEditor.TreeEditorHelper;
using UnityEngine.UIElements;


public enum GenerationType
{
    Perlin_CPU_Single_Thread,
    Perlin_CPU_Multi_Thread,
    Perlin_GPU,
    Worms_CPU_Single_Thread,
    Worms_CPU_Multi_Thread,
    Worms_GPU,
    Perlin_Worms_CPU_Single_Thread,
    Perlin_Worms_CPU_Multi_Thread,
    Perlin_Worms_GPU,
}
public class GUIValues : MonoBehaviour
{
    [Header("Global Values")]
    [Tooltip("Select the generation type of the mesh data")]
    public GenerationType generationType;
    [Tooltip("Set the seed of the build")]
    public int seed;
    [Tooltip("Set if the render should be chunked")]
    public bool isChunked = false;
    [Tooltip("Sets the render to be in chunks of size 2^n")]
    [Range(0,8)]
    public int chunkSize = 2;
    [Tooltip("Size of Render")]
    public int size;
    [Tooltip("Cutoff for Render")]
    [Range(0f, 1f)]
    public float cutoff;
    [Tooltip("Marching Cubes Mesh Filter")]
    public MeshFilter meshFilter;
    
    
    [Header("ComputeShaders")]
    [Tooltip("The Compute Shader for Perlin Noise Generation")]
    public ComputeShader P_Compute_Shader;
    [Tooltip("The Compute Shader for Worms Generation")]
    public ComputeShader W_Compute_Shader;
    [Tooltip("The Compute Shader for Perlin and Worms Generation ")]
    public ComputeShader PW_Compute_Shader;
    
    
    
    [Header("Perlin Properties")]
    [Tooltip("Sets the scale of the perlin generation")]
    [Min(0.001f)]
    public float p_scale = 100f;
    [Tooltip("No of octaves for noise generation")]
    public int p_octaves = 3;
    [Tooltip("Amplitude multiplier per octave")]
    [Range(0f, 1f)]
    public float p_persistance = 0.5f;
    [Tooltip("Frequency multiplier per octave")]
    [Min(2f)]
    public float p_lacunarity = 2f;


    [Header("Worm Properties")]
    [Tooltip("Sets the scale of the perlin worm generation")]
    [Min(0.001f)]
    public float w_scale = 100f;
    [Tooltip("No of octaves for worm noise generation")]
    public int w_octaves = 3;
    [Tooltip("Amplitude multiplier per octave")]
    [Range(0f, 1f)]
    public float w_persistance = 0.5f;
    [Tooltip("Frequency multiplier per octave")]
    [Min(2f)]
    public float w_lacunarity = 2f;
    [Tooltip("Worm count in render")]
    public int wormCount = 100;
    [Tooltip("Worm length in render")]
    public int wormLength = 200;
    [Tooltip("radius of the worm")]
    public float wormRadius = 3f;
    [HideInInspector]
    public static GUIValues instance;

    public GUIValues()
    {
        if (instance == null) 
            instance = this;
    }

    public void GenerateMap()
    {
        ClearWindow();

        switch (generationType)
        {
            
            case GenerationType.Perlin_CPU_Single_Thread:
                PerlinNoiseData.GenerateMesh();
                break;
               

            default:
                break;

        }
    }

    public void ClearWindow()
    {
        meshFilter.sharedMesh = new Mesh();
    }












}
