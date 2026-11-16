using UnityEngine;
using UnityEngine.Rendering;

public class SwordAfterimage : MonoBehaviour
{
    [Tooltip("Leave empty. Found automatically on the deer when the sword spawns.")]
    [SerializeField] private SandevistanController m_Sande;
    [SerializeField] private Material m_GhostMaterial;
    [Tooltip("_BaseColor for URP shaders, _Color for Built-in.")]
    [SerializeField] private string m_ColorProperty = "_BaseColor";

    [Header("Timing (real seconds)")]
    [SerializeField] private float m_SpawnInterval = 0.02f;
    [SerializeField] private float m_Lifetime = 0.35f;
    [SerializeField] private float m_MinMoveDistance = 0.05f;
    [SerializeField] private int m_PoolSize = 24;

    [Header("Color")]
    [SerializeField] private Color[] m_Colors = { Color.magenta, Color.cyan, Color.green };
    [SerializeField, Range(0f, 1f)] private float m_StartAlpha = 0.6f;

    private class Ghost
    {
        public GameObject root;
        public Transform[] parts;
        public MeshRenderer[] renderers;
        public float spawnTime;
        public Color color;
        public bool active;
    }

    private MeshFilter[] m_Sources;
    private Ghost[] m_Pool;
    private int m_Next;
    private int m_ColorIndex;
    private float m_LastSpawnTime = -999f;
    private Vector3 m_LastSpawnPos;
    private MaterialPropertyBlock m_Block;
    private int m_ColorId;
    private GameObject m_Ticker;

    void Awake()
    {
        FindSande();

        m_Sources = GetComponentsInChildren<MeshFilter>(true);
        if (m_Sources.Length == 0)
            Debug.LogWarning("SwordAfterimage: no MeshFilter found on the sword.", this);
        if (m_GhostMaterial == null)
            Debug.LogWarning("SwordAfterimage: no Ghost Material assigned.", this);

        m_Block = new MaterialPropertyBlock();
        m_ColorId = Shader.PropertyToID(m_ColorProperty);
        BuildPool();

        // The sword gets switched off between swings, which would stop its own Update
        // and freeze the ghosts mid-fade. This helper stays on and keeps them fading.
        m_Ticker = new GameObject("SwordAfterimageTicker");
        m_Ticker.AddComponent<SwordAfterimageTicker>().owner = this;
    }

    void FindSande()
    {
        if (m_Sande != null) return;
        m_Sande = GetComponentInParent<SandevistanController>();
        if (m_Sande == null)
            m_Sande = transform.root.GetComponentInChildren<SandevistanController>();
    }

    void BuildPool()
    {
        m_Pool = new Ghost[m_PoolSize];

        for (int i = 0; i < m_PoolSize; i++)
        {
            var g = new Ghost
            {
                root = new GameObject($"Sword Afterimage {i}"),
                parts = new Transform[m_Sources.Length],
                renderers = new MeshRenderer[m_Sources.Length]
            };

            for (int j = 0; j < m_Sources.Length; j++)
            {
                MeshFilter source = m_Sources[j];

                var part = new GameObject(source.name);
                part.transform.SetParent(g.root.transform, false);
                part.layer = source.gameObject.layer;

                // The sword mesh never deforms, so ghosts can share it directly.
                part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;

                var mr = part.AddComponent<MeshRenderer>();
                int subMeshes = source.sharedMesh != null ? source.sharedMesh.subMeshCount : 1;
                var mats = new Material[subMeshes];
                for (int k = 0; k < subMeshes; k++) mats[k] = m_GhostMaterial;
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                g.parts[j] = part.transform;
                g.renderers[j] = mr;
            }

            g.root.SetActive(false);
            m_Pool[i] = g;
        }
    }

    // Called every frame by SwordAfterimageTicker, even while the sword is hidden.
    public void Tick()
    {
        float now = Time.unscaledTime;

        if (isActiveAndEnabled)
        {
            FindSande();

            bool moved = (transform.position - m_LastSpawnPos).sqrMagnitude
                         >= m_MinMoveDistance * m_MinMoveDistance;

            if (m_Sande != null && m_Sande.SandyOn && moved && now - m_LastSpawnTime >= m_SpawnInterval)
                Spawn(now);
        }

        FadeGhosts(now);
    }

    void Spawn(float now)
    {
        if (m_Pool == null || m_Pool.Length == 0) return;

        Ghost g = m_Pool[m_Next];
        m_Next = (m_Next + 1) % m_Pool.Length;

        for (int j = 0; j < m_Sources.Length; j++)
        {
            Transform source = m_Sources[j].transform;
            g.parts[j].SetPositionAndRotation(source.position, source.rotation);
            g.parts[j].localScale = source.lossyScale;
        }

        if (m_Colors != null && m_Colors.Length > 0)
        {
            g.color = m_Colors[m_ColorIndex % m_Colors.Length];
            m_ColorIndex = (m_ColorIndex + 1) % m_Colors.Length;
        }
        else
        {
            g.color = Color.cyan;
        }

        g.spawnTime = now;
        g.active = true;
        g.root.SetActive(true);

        m_LastSpawnTime = now;
        m_LastSpawnPos = transform.position;
    }

    void FadeGhosts(float now)
    {
        if (m_Pool == null) return;

        foreach (Ghost g in m_Pool)
        {
            if (!g.active) continue;

            float age = (now - g.spawnTime) / Mathf.Max(m_Lifetime, 0.01f);
            if (age >= 1f)
            {
                g.active = false;
                g.root.SetActive(false);
                continue;
            }

            Color c = g.color;
            c.a = m_StartAlpha * (1f - age);
            m_Block.SetColor(m_ColorId, c);

            foreach (MeshRenderer mr in g.renderers)
                mr.SetPropertyBlock(m_Block);
        }
    }

    void OnDestroy()
    {
        if (m_Ticker != null) Destroy(m_Ticker);
        if (m_Pool == null) return;
        foreach (Ghost g in m_Pool)
            if (g.root != null) Destroy(g.root);
    }
}

// Lives on its own always-active object so ghosts keep fading after the sword is hidden.
public class SwordAfterimageTicker : MonoBehaviour
{
    public SwordAfterimage owner;

    void Update()
    {
        if (owner != null) owner.Tick();
    }
}