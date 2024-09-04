using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;


static public class PerlinWorms 
{
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
        localMaximas = localMaximas.Where(pos => pointCloud[size * size * pos.z + size * pos.y + pos.x] >=(GUIValues.instance.maximaCutoff)).OrderBy(pos => pointCloud[size * size * pos.z + size * pos.y + pos.x]).Take(30).ToList();
        if(GUIValues.instance.isDebug) 
        {
            foreach (var pos in localMaximas)
            {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.transform.localPosition = pos;
                obj.transform.localScale = Vector3.one * GUIValues.instance.size / 50f;
            }
        }
        
        //create loop for number of worms 
        //select two local maximas
        //find direction vector between the two
        //scale steps based on dist from two points
        //add the dir vector to the perlin noise dir
        //slowly increase strength as getting closer to the point


    }

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
