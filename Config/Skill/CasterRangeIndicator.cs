using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CasterRangeIndicator : MonoBehaviour
{
    public float radius = 5f;
    [Range(6, 120)]
    public int segmentCount = 60;

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
            mesh.name = "CasterRangeMesh";
        }
        mf.mesh = mesh;
        GenerateMesh();
        SetInRange(true);
    }
    

    public void GenerateMesh()
    {
        mesh.Clear();

        int vertexCount = segmentCount + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segmentCount * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        float radTotal = Mathf.PI * 2f;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float cur = t * radTotal;

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

    public void SetRadius(float r)
    {
        radius = r;
        GenerateMesh();
    }
    
    public void SetInRange(bool inRange)
    {
        if (mr == null) return;

        var mat = mr.material;    // 简单粗暴版，够用了；大规模可用 PropertyBlock
        var color = inRange ? inRangeColor : outRangeColor;

        // URP/Unlit 默认颜色属性是 _BaseColor，保险起见兼容 _Color
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
    }
}
