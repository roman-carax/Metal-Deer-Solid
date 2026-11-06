using UnityEngine;
using UnityEngine.Rendering;

public class AfterimageSpawner : MonoBehaviour
{
    [SerializeField] private SandevistanController m_Sande;

    [SerializeField] private SkinnedMeshRenderer[] m_Renderers;
    [SerializeField] private Material m_GhostMaterial;

    [SerializeField] private string m_ColorProperty = "_BaseColor";


    [SerializeField] private float m_SpawnInterval = 0.05f;
    [SerializeField] private float m_Lifetime = 0.35f;
    [SerializeField] private float m_MinMoveDistance = 0.05f;
    [SerializeField] private int m_PoolSize = 16;


    [SerializeField] private Gradient m_ColorCycle;
    [SerializeField] private float m_CycleSpeed = 1f;
    [SerializeField, Range(0f, 1f)] private float m_StartAlpha = 0.6f;

    private class Ghost
    {
        public GameObject root;
        public Mesh[] meshes;
        public MeshRenderer[] parts;
        public float spawnTime;
        public Color color;
        public bool active;
    }

    private Ghost[] m_Pool;
    private int m_Next;
    private float m_LastSpawnTime = -999f;
    private Vector3 m_LastSpawnPos;
    private MaterialPropertyBlock m_Block;
    private int m_ColorId;

    void Awake()
    {
        if (m_Sande == null)
            m_Sande = GetComponent<SandevistanController>();
        if (m_Renderers == null || m_Renderers.Length == 0)
            m_Renderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        m_Block = new MaterialPropertyBlock();
        m_ColorId = Shader.PropertyToID(m_ColorProperty);
        BuildPool();
    }

    void BuildPool()
    {
        m_Pool = new Ghost[m_PoolSize];

        for (int i = 0; i < m_PoolSize; i++)
        {
            var g = new Ghost
            {
                root = new GameObject($"Afterimage {i}"),
                meshes = new Mesh[m_Renderers.Length],
                parts = new MeshRenderer[m_Renderers.Length]
            };

            for (int j = 0; j < m_Renderers.Length; j++)
            {
                SkinnedMeshRenderer source = m_Renderers[j];

                var part = new GameObject(source.name);
                part.transform.SetParent(g.root.transform, false);
                part.layer = source.gameObject.layer;   // so first-person aim hiding also hides ghosts

                g.meshes[j] = new Mesh();
                part.AddComponent<MeshFilter>().sharedMesh = g.meshes[j];

                var mr = part.AddComponent<MeshRenderer>();
                int subMeshes = source.sharedMesh != null ? source.sharedMesh.subMeshCount : 1;
                var mats = new Material[subMeshes];
                for (int k = 0; k < subMeshes; k++) mats[k] = m_GhostMaterial;
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                g.parts[j] = mr;
            }

            g.root.SetActive(false);
            m_Pool[i] = g;
        }
    }

    void Update()
    {
        float now = Time.unscaledTime;

        bool moved = (transform.position - m_LastSpawnPos).sqrMagnitude
                     >= m_MinMoveDistance * m_MinMoveDistance;

        if (m_Sande != null && m_Sande.SandyOn && moved && now - m_LastSpawnTime >= m_SpawnInterval)
            Spawn(now);

        FadeGhosts(now);
    }

    void Spawn(float now)
    {
        Ghost g = m_Pool[m_Next];
        m_Next = (m_Next + 1) % m_Pool.Length;

        for (int j = 0; j < m_Renderers.Length; j++)
        {
            SkinnedMeshRenderer source = m_Renderers[j];
            source.BakeMesh(g.meshes[j], true);
            g.parts[j].transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        }

        g.color = m_ColorCycle.Evaluate(Mathf.Repeat(now * m_CycleSpeed, 1f));
        g.spawnTime = now;
        g.active = true;
        g.root.SetActive(true);

        m_LastSpawnTime = now;
        m_LastSpawnPos = transform.position;
    }

    void FadeGhosts(float now)
    {
        foreach (Ghost g in m_Pool)
        {
            if (!g.active) continue;

            float age = (now - g.spawnTime) / m_Lifetime;
            if (age >= 1f)
            {
                g.active = false;
                g.root.SetActive(false);
                continue;
            }

            Color c = g.color;
            c.a = m_StartAlpha * (1f - age);
            m_Block.SetColor(m_ColorId, c);

            foreach (MeshRenderer part in g.parts)
                part.SetPropertyBlock(m_Block);
        }
    }

    void OnDestroy()
    {
        if (m_Pool == null) return;
        foreach (Ghost g in m_Pool)
        {
            foreach (Mesh m in g.meshes) Destroy(m);
            Destroy(g.root);
        }
    }
}