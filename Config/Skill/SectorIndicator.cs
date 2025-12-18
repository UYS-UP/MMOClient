
using UnityEngine;

public class SectorIndicator : MonoBehaviour
{
    [Range(0.1f, 20f)]
    public float radius = 3f;
    
    [Range(1f, 360f)]
    public float angle = 90f;

    [Range(3, 100)] 
    public int segmentCount = 30;
    
    private Mesh mesh;
    private MeshFilter mf;
    private MeshRenderer mr;
    
    [Header("Colors")]
    public Color inRangeColor  = new Color(0f, 1f, 0f, 0.3f);  // 绿色带点透明
    public Color outRangeColor = new Color(1f, 0f, 0f, 0.3f);  // 红色带点透明
    
    private void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "SectorMesh";
        }
        mf.mesh = mesh;

        GenerateMesh();
    }
    
    
    public void SetInRange(bool inRange)
    {
        if (mr == null) return;
        var mat = mr.material;
        var color = inRange ? inRangeColor : outRangeColor;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
    }
    
    
    public void GenerateMesh()
    {
        mesh.Clear();

        int vertexCount = segmentCount + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segmentCount * 3];

        // 中心点
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        float rad = angle * Mathf.Deg2Rad;
        float startAngle = -rad / 2f;    // 以Z轴正方向为中心

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float cur = startAngle + t * rad;

            float x = Mathf.Sin(cur) * radius;
            float z = Mathf.Cos(cur) * radius;

            int idx = i + 1;
            vertices[idx] = new Vector3(x, 0f, z);
            uvs[idx] = new Vector2((x / (radius * 2f)) + 0.5f, (z / (radius * 2f)) + 0.5f);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int tri = i * 3;
            triangles[tri]     = 0;
            triangles[tri + 1] = i + 1;
            triangles[tri + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public void SetParams(float r, float a)
    {
        radius = r;
        angle = a;
        GenerateMesh();
    }
}
