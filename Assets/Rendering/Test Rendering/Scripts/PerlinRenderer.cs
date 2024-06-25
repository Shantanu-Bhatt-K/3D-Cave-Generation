using System.Diagnostics;
using UnityEngine;

public class PerlinRenderer : MonoBehaviour
{
    public GameObject renderObject3D;
    public Transform renderBase;
    public void Clear()
    {
        Stopwatch st = new Stopwatch();
        st.Start();
        for (int i = renderBase.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(renderBase.GetChild(i).gameObject);
        }
        st.Stop();
        UnityEngine.Debug.Log(string.Format("Clearing CPU 3D Perlin took {0} ms to complete", st.ElapsedMilliseconds));
    }
    public void RenderCPU3D(float[,,] meshData,Vector3 size,float cutoff)
    {
        Stopwatch st = new Stopwatch();
        st.Start();
        for (int k=0;k<size.z;k++)
        {
            for(int j=0;j<size.y;j++) 
            {
                for(int i=0;i<size.x;i++)
                {
                    if (meshData[i, j, k] <= cutoff)
                        continue;
                    GameObject cube = Instantiate(renderObject3D);
                    cube.transform.position = new Vector3(i, j, k);
                    //cube.transform.localScale =new Vector3( meshData[i, j, k], meshData[i, j, k], meshData[i, j, k]);
                    cube.transform.parent=renderBase;
                }

            }
        }
        st.Stop();
        UnityEngine.Debug.Log(string.Format("Rendering CPU 3D Perlin took {0} ms to complete", st.ElapsedMilliseconds));
    }

    
}
