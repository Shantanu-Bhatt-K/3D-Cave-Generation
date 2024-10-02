using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static UnityEngine.Mesh;
using UnityEngine.UIElements;
using Unity.Mathematics;


static public class PerlinWorms 
{
    static System.Random random = new System.Random(GUIValues.instance.seed);
    //directions for checking of local maxima
    static public List<Vector3Int> directions = new List<Vector3Int>
    {
        new(-1, -1, -1),
        new(-1, -1, 0),
        new(-1, -1, 1),
        new(-1, 0, -1),
        new(-1, 0, 0),
        new(-1, 0, 1),
        new(-1, 1, -1),
        new(-1, 1, 0),
        new(-1, 1, 1),

        new(0, -1, -1),
        new(0, -1, 0),
        new(0, -1, 1),
        new(0, 0, -1),
        new(0, 0, 1),
        new(0, 1, -1),
        new(0, 1, 0),
        new(0, 1, 1),

        new(1, -1, -1),
        new(1, -1, 0),
        new(1, -1, 1),
        new(1, 0, -1),
        new(1, 0, 0),
        new(1, 0, 1),
        new(1, 1, -1),
        new(1, 1, 0),
        new(1, 1, 1)
    };
    static readonly int[] p = new int[512];
    static readonly int[] permutation = {
        151,160,137,91,90,15, 131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33, 88,237,149,56,87,174,20,
        125,136,171,168, 68,175,74,165,71,134,139,48,27,166, 77,146,158,231,83,111,229,122,60,211,133,230,
        220,105,92,41,55,46,245,40,244,102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,
        200,196,135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123, 5,202,38,147,
        118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,119,248,152, 2,44,
        154,163, 70,221,153,101,155,167, 43,172,9, 129,22,39,253, 19,98,108,110,79,113,224,232,178,185,
        112,104,218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,
        107, 49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,138,236,205,93,
        222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    static PerlinWorms()
    {
        for (int i = 0; i < 256; i++)
        {
            p[256 + i] = p[i] = permutation[i];
        }
    }
   
    static public void GenWorms(float[] pointCloud)
    {
        int size = GUIValues.instance.size;
        //find local maximas....
        List<Vector3Int> localMaximas = FindLocalMaxima(pointCloud);
        Debug.Log("LocalMaximasCount "+ localMaximas.Count);
        //filter and select local maxima
        localMaximas = localMaximas.Where(pos => pointCloud[size * size * pos.z + size * pos.y + pos.x] >= (GUIValues.instance.maximaCutoff)).OrderBy(pos => pointCloud[size * size * pos.z + size * pos.y + pos.x]).Take(150).ToList();
        //debug to check maxima locations
        if (GUIValues.instance.wormDebug)
        {
            foreach (var pos in localMaximas)
            {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.transform.localPosition = pos;
                obj.transform.localScale = Vector3.one * GUIValues.instance.size / 50f;
            }
        }
        
        Vector3[] octaveOffsets = new Vector3[GUIValues.instance.w_octaves];
        for (int i = 0; i < GUIValues.instance.w_octaves; i++)
        {
            octaveOffsets[i] = new Vector3(
               (float)random.Next(-100000, 100000),
                (float)random.Next(-100000, 100000),
                (float)random.Next(-100000, 100000)
            );
        }
        //loop over each worm
        for (int i = 0; i < GUIValues.instance.wormCount; i++)
        {
            //select start and end position
            Vector3 startPosition = localMaximas[random.Next(localMaximas.Count)];
            Vector3 position = startPosition;
            Vector3 endPosition = localMaximas
                .Where(pos => pos != startPosition)     // Exclude startPosition
                .OrderBy(_ => random.Next())            // Shuffle the remaining positions
                .First();
            //calculate bias direction
            Vector3 tunnelDir = (endPosition - position).normalized;
            //calculate radius based on distance of current position
            float distPoint = Mathf.Min((startPosition - position).sqrMagnitude, (endPosition - position).sqrMagnitude);
            float radiusMultiplier =GUIValues.instance.falloff/(distPoint+1) ;
            float radius = GUIValues.instance.wormRadius;

            //loop over length of worm
            for (int j = 0; j < GUIValues.instance.wormLength; j++)
            {
                

                EditMeshData(pointCloud, position,radius);
                Vector3 dir = GetPerlinDirection(position, octaveOffsets) + tunnelDir;
                
                position += dir.normalized * radius;
                radius = GUIValues.instance.wormRadius + radiusMultiplier;
                distPoint = Mathf.Min((startPosition - position).sqrMagnitude, (endPosition - position).sqrMagnitude);
                radiusMultiplier = GUIValues.instance.falloff /(distPoint+1);
                //Debug.Log(radius+" " + i);
            }
        }
    }
        //fill mesh around current position
        static void EditMeshData(float[] pointCloud, Vector3 position,float radius)
        {
            int x = Mathf.RoundToInt(position.x);
            int y = Mathf.RoundToInt(position.y);
            int z = Mathf.RoundToInt(position.z);
            
            int size = GUIValues.instance.size;
            for (int i = -Mathf.CeilToInt(radius); i <= Mathf.CeilToInt(radius); i++)
            {
                for (int j = -Mathf.CeilToInt(radius); j <= Mathf.CeilToInt(radius); j++)
                {
                    for (int k = -Mathf.CeilToInt(radius); k <= Mathf.CeilToInt(radius); k++)
                    {
                        int nx = x + i;
                        int ny = y + j;
                        int nz = z + k;
                        int index = size * size * nz + size * ny + nx;
                        if (nx >= 0 && nx < size && ny >= 0 && ny < size && nz >= 0 && nz < size)
                        {
                            Vector3 neighborPos = new Vector3(nx, ny, nz);
                            if (Vector3.Distance(neighborPos, position) <= radius)
                            {
                                pointCloud[index] += 0.05f  * (radius - Vector3.Distance(neighborPos, position)) ;
                            }
                        if (nx == 0 || nx == size - 1 || ny == 0 || ny == size - 1 || nz == 0 || nz == size - 1)
                        {
                            pointCloud[index] = 0;
                        }
                    }
                    }
                }
            }
        }
    //get a perlin direction on the current position. modified version of 3D perlin noise
    static Vector3 GetPerlinDirection(Vector3 position, Vector3[] octaveOffsets)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float angleX = 0, angleY = 0, angleZ = 0;
        float scale = GUIValues.instance.w_scale;
        
        for (int l = 0; l < GUIValues.instance.w_octaves; l++)
        {
            float noiseX = (float)Noise((position.x + octaveOffsets[l].x) / scale * frequency, (position.y + octaveOffsets[l].y) / scale * frequency);
            float noiseY = (float)Noise((position.y + octaveOffsets[l].y) / scale * frequency, (position.z + octaveOffsets[l].z) / scale * frequency);
            float noiseZ = (float)Noise((position.z + octaveOffsets[l].z) / scale * frequency, (position.x + octaveOffsets[l].x) / scale * frequency);

            angleX += noiseX * amplitude;
            angleY += noiseY * amplitude;
            angleZ += noiseZ * amplitude;

            amplitude *= GUIValues.instance.w_persistance;
            frequency *= GUIValues.instance.w_lacunarity;
        }

        return new Vector3(angleX, angleY, angleZ).normalized;
    }

    public static double Noise(double x, double y)
    {
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;

        x -= Math.Floor(x);
        y -= Math.Floor(y);

        double u = Fade(x);
        double v = Fade(y);

        int A = p[X] + Y;
        int AA = p[A];
        int AB = p[A + 1];
        int B = p[X + 1] + Y;
        int BA = p[B];
        int BB = p[B + 1];

        return Lerp(v, Lerp(u, Grad(p[AA], x, y), Grad(p[BA], x - 1, y)), Lerp(u, Grad(p[AB], x, y - 1), Grad(p[BB], x - 1, y - 1)));
    }

    private static double Fade(double t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static double Lerp(double t, double a, double b)
    {
        return a + t * (b - a);
    }

    private static double Grad(int hash, double x, double y)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }


    //Function to find local maxima 
    //source https://www.youtube.com/watch?v=B8qarIAuE6M&t=32s
    public static List<Vector3Int> FindLocalMaxima(float[] pointCloud)
    {
        int size= GUIValues.instance.size;
        List<Vector3Int> localMaximas = new List<Vector3Int>();
        for(int x=0;x<size;x++)
        {
            for(int y=0;y<size;y++)
            {
                for (int z=0;z<size;z++)
                {
                    float noiseVal = pointCloud[size * size * z + size * y + x];
                    if(CheckNeighbours(x,y,z,pointCloud,(neighbourNoise)=>neighbourNoise>noiseVal))
                    {
                        localMaximas.Add(new Vector3Int(x,y,z));
                    }
                }
            }
        }



        return localMaximas;
    }
    private static bool CheckNeighbours(int x, int y, int z, float[] pointCloud, Func<float,bool> failCondition)
    {
        int size = GUIValues.instance.size;
        foreach (Vector3Int dir in directions)
        {
            Vector3Int newPosition= new Vector3Int(x+dir.x,y+dir.y,z+dir.z);
            if(newPosition.x < 0 || newPosition.x>=size || newPosition.y < 0 || newPosition.y >= size|| newPosition.z < 0 || newPosition.z >= size)
            {
                continue;
            }
            if (failCondition(pointCloud[size * size * newPosition.z + size * newPosition.y + newPosition.x]))
            {
                return false;
            }
        }
        return true;
    }
}
