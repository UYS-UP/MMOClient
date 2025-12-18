using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Unity.AI.Navigation;

/// <summary>
/// 使用navmesh surface对场景进行烘焙之后导出场景数据供服务端使用
/// </summary>
public class NavMeshVolumeBaker : EditorWindow
{
    [Header("体素配置")]
    private NavMeshSurface surface;
    private float voxelSize = 1.0f;
    private Vector3 volumeSize;
    private Vector3 volumeOrigin;
    private bool showVoxels = true;

    [Header("采样参数")] 
    private int areaMask = NavMesh.AllAreas;
    private float sampleRadius = 0.4f;
    private bool autoUseNavMeshBounds = true;
    
    [Header("生成点/出生点配置")]
    private readonly List<EntitySpawnConfig> entitySpawnDataList = new List<EntitySpawnConfig>();

    private bool[,,] voxelGrid;
    private BitArray bits;

    private readonly List<Matrix4x4> voxelMatrices = new List<Matrix4x4>(4096);
    private Mesh cubeMesh;
    private Material instanceMaterial;
    private const int MaxBatchCount = 1023;

    private const int FILE_MAGIC = 0X564F584C;
    private const int FILE_VERSION = 1;
    
    [Serializable]
    private class EntitySpawnConfig
    {
        public string entityId;
        public int entityType;
        public Vector3 position;
        public Quaternion rotation;

        public int spawnPattern = 0;
        public int count = 1;

        public float radius = 2f;

        public Vector2 rectangleSize = new Vector2(4f, 4f);
        public float lineLength = 5f;
    }
    
    [Serializable]
    private class SpawnExportItem
    {
        public string entityId;
        public int entityType;
        public int pattern;
        public Quaternion rotation;
        public List<Vector3> positions;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T data;
    }
    
    [MenuItem("Tools/Optimized NavMesh Volume Baker")]
    public static void ShowWindow()
    {
        GetWindow<NavMeshVolumeBaker>(false, "NavMesh Volume Baker Optimized");
    }
    
    
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("NavMesh 输入与体素配置", EditorStyles.boldLabel);
        surface = (NavMeshSurface)EditorGUILayout.ObjectField("NavMesh Surface", surface, typeof(NavMeshSurface), true);
        voxelSize = Mathf.Max(0.05f, EditorGUILayout.FloatField("Voxel Size", voxelSize));
        showVoxels = EditorGUILayout.Toggle("Show Voxels", showVoxels);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("采样参数", EditorStyles.boldLabel);
        areaMask = EditorGUILayout.IntField("Area Mask", areaMask);
        sampleRadius = EditorGUILayout.Slider("Sample Radius", sampleRadius, 0.01f, Mathf.Max(0.01f, voxelSize * 0.5f));
        autoUseNavMeshBounds = EditorGUILayout.Toggle("Use NavMesh Bounds", autoUseNavMeshBounds);

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Calculate Bounds")) CalculateNavMeshBoundsIfPossible();
        if (GUILayout.Button("Bake Voxel Grid")) BakeNavMeshVolumeOptimized();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Voxel Data")) SaveVoxelDataOptimized();
        if (GUILayout.Button("Load Voxel Data")) LoadVoxelDataOptimized();
        EditorGUILayout.EndHorizontal();
    }


    private void CalculateNavMeshBoundsIfPossible()
    {
        if(!autoUseNavMeshBounds || surface == null) return;

        var tri0 = NavMesh.CalculateTriangulation();
        if (tri0.vertices == null || tri0.vertices.Length == 0)
        {
            surface.BuildNavMesh();
        }

        var tri = NavMesh.CalculateTriangulation();
        if (tri.vertices == null || tri.vertices.Length == 0) return;
        var b = new Bounds(tri.vertices[0], Vector3.zero);
        for (int i = 1; i < tri.vertices.Length; i++) b.Encapsulate(tri.vertices[i]);
        
        float padding = Mathf.Max(0.0f, voxelSize * 5f);
        volumeSize = b.size + new Vector3(padding, padding, padding);
        volumeOrigin = b.min - new Vector3(padding * 0.5f, padding * 0.5f, padding * 0.5f);
        
        volumeOrigin = new Vector3(
            Mathf.Floor(volumeOrigin.x / voxelSize) * voxelSize,
            Mathf.Floor(volumeOrigin.y / voxelSize) * voxelSize,
            Mathf.Floor(volumeOrigin.z / voxelSize) * voxelSize
        );
    }

    private void BakeNavMeshVolumeOptimized()
    {
        if (surface == null)
        {
            Debug.LogError("请选择一个NavMesh Surface!");
            return;
        }
        
        float minSafe = Mathf.Sqrt(2f) * (voxelSize * 0.5f) * 1.1f;
        sampleRadius = Mathf.Max(minSafe, sampleRadius);
        CalculateNavMeshBoundsIfPossible();
        
        int xSize = Mathf.CeilToInt(volumeSize.x / voxelSize);
        int ySize = Mathf.CeilToInt(volumeSize.y / voxelSize);
        int zSize = Mathf.CeilToInt(volumeSize.z / voxelSize);
        
        if (xSize <= 0 || ySize <= 0 || zSize <= 0)
        {
            Debug.LogWarning("体素尺寸/范围不合法，无法烘焙。");
            return;
        }
        
        voxelGrid = new bool[xSize, ySize, zSize];
        bits = new BitArray(xSize * ySize * zSize);
        voxelMatrices.Clear();
        EnsureInstancingResources();
        
        int total = xSize * ySize * zSize;
        int processed = 0;
        bool canceled = false;
        
        try
        {
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        if ((processed & 0x1FFF) == 0)
                        {
                            if (EditorUtility.DisplayCancelableProgressBar(
                                "Baking Voxels",
                                $"Sampling {processed}/{total} ({(processed/(float)total):P0})",
                                processed / (float)total))
                            { canceled = true; break; }
                        }

                        // 体素中心点（更稳定的采样）
                        Vector3 worldPos = volumeOrigin + new Vector3(
                            x * voxelSize + voxelSize * 0.5f,
                            y * voxelSize + voxelSize * 0.5f,
                            z * voxelSize + voxelSize * 0.5f
                        );

                        // 使用 NavMesh.SamplePosition，尊重 areaMask 与半径
                        if (NavMesh.SamplePosition(worldPos, out var hit, sampleRadius, areaMask))
                        {
                            voxelGrid[x, y, z] = true;
                            int flat = (x * ySize + y) * zSize + z; // 统一映射到一维索引
                            bits[flat] = true;

                            if (showVoxels)
                            {
                                var m = Matrix4x4.TRS(worldPos, Quaternion.identity, Vector3.one * voxelSize);
                                voxelMatrices.Add(m);
                            }
                        }

                        processed++;
                    }
                    if (canceled) break;
                }
                if (canceled) break;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (canceled) Debug.LogWarning("Bake canceled.");
        Debug.Log($"Volume baked: {xSize}x{ySize}x{zSize}, filled={voxelMatrices.Count}");
        SceneView.RepaintAll();
    }
    
    private void EnsureInstancingResources()
    {
        if (cubeMesh == null)
        {
            // 使用内置 Primitive 的 Mesh，避免对外部资源路径的依赖
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
        }
        if (instanceMaterial == null)
        {
            instanceMaterial = new Material(Shader.Find("Standard"))
            {
                enableInstancing = true
            };
            instanceMaterial.color = Color.magenta; // 可自定义可视化颜色
            instanceMaterial.name = "VoxelInstancedMat";
        }
    }
    
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (showVoxels && voxelMatrices.Count > 0)
        {
            EnsureInstancingResources();
            // 分批绘制，避免超过 1023 的限制
            for (int i = 0; i < voxelMatrices.Count; i += MaxBatchCount)
            {
                int count = Mathf.Min(MaxBatchCount, voxelMatrices.Count - i);
                Graphics.DrawMeshInstanced(cubeMesh, 0, instanceMaterial, voxelMatrices.GetRange(i, count));
            }
        }
    }
    
    private void SaveVoxelDataOptimized()
    {
        if (bits == null || voxelGrid == null)
        {
            Debug.LogError("No voxel data to save!");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Save Voxel Data", "", "voxel_data", "bin");
        if (string.IsNullOrEmpty(path)) return;

        using var fs = new FileStream(path, FileMode.Create);
        using var gzip = new GZipStream(fs, CompressionMode.Compress);
        using var bw = new BinaryWriter(gzip);

        bw.Write(FILE_MAGIC);
        bw.Write(FILE_VERSION);

        int x = voxelGrid.GetLength(0);
        int y = voxelGrid.GetLength(1);
        int z = voxelGrid.GetLength(2);
        bw.Write(x); bw.Write(y); bw.Write(z);

        bw.Write(volumeOrigin.x); bw.Write(volumeOrigin.y); bw.Write(volumeOrigin.z);
        bw.Write(voxelSize);
        bw.Write(areaMask);
        bw.Write(sampleRadius);

        // 将 BitArray 拷贝到字节数组（8 个体素布尔 -> 1 字节）
        byte[] bytes = new byte[(bits.Length + 7) / 8];
        bits.CopyTo(bytes, 0);
        bw.Write(bytes.Length);
        bw.Write(bytes);

        Debug.Log($"Voxel data saved to {path} (bit-packed).");
    }

    private void LoadVoxelDataOptimized()
    {
        string path = EditorUtility.OpenFilePanel("Load Voxel Data", "", "bin");
        if (string.IsNullOrEmpty(path)) return;

        using var fs = new FileStream(path, FileMode.Open);
        using var gzip = new GZipStream(fs, CompressionMode.Decompress);
        using var br = new BinaryReader(gzip);

        int magic = br.ReadInt32();
        int version = br.ReadInt32();
        if (magic != FILE_MAGIC)
        {
            Debug.LogError("Invalid voxel file.");
            return;
        }
        // 如需要，可根据 version 执行兼容迁移逻辑

        int x = br.ReadInt32();
        int y = br.ReadInt32();
        int z = br.ReadInt32();

        volumeOrigin = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        voxelSize = br.ReadSingle();
        areaMask = br.ReadInt32();
        sampleRadius = br.ReadSingle();

        int byteLen = br.ReadInt32();
        byte[] bytes = br.ReadBytes(byteLen);

        voxelGrid = new bool[x, y, z];
        bits = new BitArray(bytes) { Length = x * y * z };

        // 仅在需要显示时重建渲染缓存（避免不必要的计算）
        voxelMatrices.Clear();
        if (showVoxels)
        {
            EnsureInstancingResources();
            for (int i = 0; i < bits.Length; i++)
            {
                if (!bits[i]) continue;
                int xi = i / (y * z);
                int yi = (i / z) % y;
                int zi = i % z;

                Vector3 worldPos = volumeOrigin + new Vector3(
                    xi * voxelSize + voxelSize * 0.5f,
                    yi * voxelSize + voxelSize * 0.5f,
                    zi * voxelSize + voxelSize * 0.5f
                );
                voxelMatrices.Add(Matrix4x4.TRS(worldPos, Quaternion.identity, Vector3.one * voxelSize));
                voxelGrid[xi, yi, zi] = true;
            }
        }

        volumeSize = new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
        Debug.Log("Voxel data loaded (bit-packed).");
        SceneView.RepaintAll();
    }

}