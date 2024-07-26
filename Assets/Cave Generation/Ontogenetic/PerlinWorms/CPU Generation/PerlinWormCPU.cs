using UnityEngine;
using System;

public static class PerlinWormCPU
{
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

    static PerlinWormCPU()
    {
        for (int i = 0; i < 256; i++)
        {
            p[256 + i] = p[i] = permutation[i];
        }
    }

    public static void MeshData_Gen(int width, int height, int depth, float scale, int seed, int octaves, float lacunarity, float persistance, ref float[,,] mesh_data, int wormCount, int wormLength, float radiusMultiplier)
    {
        System.Random random = new System.Random(seed);
        Vector3[] octaveOffsets = new Vector3[octaves];
        for (int i = 0; i < octaves; i++)
        {
            octaveOffsets[i] = new Vector3(
                (float)random.Next(-100000, 100000),
                (float)random.Next(-100000, 100000),
                (float)random.Next(-100000, 100000)
            );
        }

        for (int i = 0; i < wormCount; i++)
        {
            Vector3 position = new Vector3(
               random.Next(0, width),
               random.Next(0, height),
               random.Next(0, depth)
           );
            float radius = radiusMultiplier;
            for (int j = 0; j < wormLength; j++)
            {
                EditMeshData(ref mesh_data, position, radius, width, height, depth);
                Vector3 dir = GetPerlinDirection(position, scale, seed, octaves, lacunarity, persistance, octaveOffsets);
                position += dir * radius;
                radius = radiusMultiplier;

            }
        }
    }

    static void EditMeshData(ref float[,,] mesh_data, Vector3 position, float radius, int width, int height, int depth)
    {
        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y);
        int z = Mathf.RoundToInt(position.z);

        for (int i = -Mathf.CeilToInt(radius); i <= Mathf.CeilToInt(radius); i++)
        {
            for (int j = -Mathf.CeilToInt(radius); j <= Mathf.CeilToInt(radius); j++)
            {
                for (int k = -Mathf.CeilToInt(radius); k <= Mathf.CeilToInt(radius); k++)
                {
                    int nx = x + i;
                    int ny = y + j;
                    int nz = z + k;

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && nz >= 0 && nz < depth)
                    {
                        Vector3 neighborPos = new Vector3(nx, ny, nz);
                        if (Vector3.Distance(neighborPos, position) <= radius)
                        {
                            mesh_data[nx, ny, nz] = 1;
                        }
                    }
                }
            }
        }
    }

    static Vector3 GetPerlinDirection(Vector3 position, float scale, int seed, int octaves, float lacunarity, float persistance, Vector3[] octaveOffsets)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float angleX = 0, angleY = 0, angleZ = 0;

        for (int l = 0; l < octaves; l++)
        {
            float noiseX = (float)Noise((position.x + octaveOffsets[l].x) / scale * frequency, (position.y + octaveOffsets[l].y) / scale * frequency);
            float noiseY = (float)Noise((position.y + octaveOffsets[l].y) / scale * frequency, (position.z + octaveOffsets[l].z) / scale * frequency);
            float noiseZ = (float)Noise((position.z + octaveOffsets[l].z) / scale * frequency, (position.x + octaveOffsets[l].x) / scale * frequency);

            angleX += noiseX * amplitude ;
            angleY += noiseY * amplitude ;
            angleZ += noiseZ * amplitude;

            amplitude *= persistance;
            frequency *= lacunarity;
        }

        return new Vector3(angleX,angleY,angleZ).normalized;
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
}
