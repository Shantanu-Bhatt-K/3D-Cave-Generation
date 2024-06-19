using UnityEngine;

public class PerlinRenderer : MonoBehaviour
{
    public GameObject renderObject3D;
    public Transform renderBase;
    public void Clear()
    {
        for (int i = renderBase.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(renderBase.GetChild(i).gameObject);
        }
    }
    public void RenderCPU3D(float[,,] meshData,Vector3 size)
    {
        
      for (int k=0;k<size.z;k++)
        {
            for(int j=0;j<size.y;j++) 
            {
                for(int i=0;i<size.x;i++)
                {
                    GameObject cube = Instantiate(renderObject3D);
                    cube.transform.position = new Vector3(i, j, k);
                    cube.transform.localScale =new Vector3( meshData[i, j, k], meshData[i, j, k], meshData[i, j, k]);
                    cube.transform.parent=renderBase;
                }

            }
        }

    }
}
