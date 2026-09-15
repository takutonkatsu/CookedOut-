using UnityEngine;

namespace CookedOut.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PlayerIdentityMarker : MonoBehaviour
    {
        public const float RingOuterRadius = 0.70f;
        public const float RingThickness = 0.255f;

        private static readonly Color[] PlayerColors =
        {
            new Color(0.12f, 0.58f, 1f, 0.62f),
            new Color(0.96f, 0.22f, 0.20f, 0.62f),
            new Color(0.19f, 0.78f, 0.35f, 0.62f),
            new Color(1f, 0.78f, 0.10f, 0.62f)
        };

        private MeshRenderer _renderer;

        public int PlayerIndex { get; private set; }
        public Color MarkerColor => _renderer == null ? Color.clear : _renderer.sharedMaterial.color;
        public MeshRenderer MarkerRenderer => _renderer;

        public void Initialize(int playerIndex)
        {
            PlayerIndex = Mathf.Clamp(playerIndex, 1, PlayerColors.Length);
            var marker = new GameObject("Player Position Marker");
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            marker.transform.localRotation = Quaternion.identity;

            var filter = marker.AddComponent<MeshFilter>();
            filter.sharedMesh = BuildRingMesh();
            _renderer = marker.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = CreateTransparentMaterial(
                "Player " + PlayerIndex + " Marker",
                PlayerColors[PlayerIndex - 1]);
        }

        private static Mesh BuildRingMesh()
        {
            const int segments = 32;
            const float outerRadius = RingOuterRadius;
            const float innerRadius = RingOuterRadius - RingThickness;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (var index = 0; index < segments; index++)
            {
                var angle = Mathf.PI * 2f * index / segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[index * 2] = direction * outerRadius;
                vertices[index * 2 + 1] = direction * innerRadius;

                var next = (index + 1) % segments;
                var triangle = index * 6;
                triangles[triangle] = index * 2;
                triangles[triangle + 1] = next * 2 + 1;
                triangles[triangle + 2] = next * 2;
                triangles[triangle + 3] = index * 2;
                triangles[triangle + 4] = index * 2 + 1;
                triangles[triangle + 5] = next * 2 + 1;
            }

            var mesh = new Mesh { name = "Player Identity Ring" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        internal static Material CreateTransparentMaterial(string name, Color color)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader)
            {
                name = name,
                color = color,
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = 3000
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }
            return material;
        }
    }

    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    public sealed class DashSpeedEffect : MonoBehaviour
    {
        public const float SmokeScaleMultiplier = 1.30f;
        private PlayerController _player;
        private GameObject _effectRoot;
        private GameObject _smokeRoot;
        private readonly Transform[] _smokePuffs = new Transform[5];
        private readonly Material[] _smokeMaterials = new Material[5];
        private float _smokeClock;

        public bool IsVisible => _effectRoot != null && _effectRoot.activeSelf;
        public Transform EffectRoot => _effectRoot == null ? null : _effectRoot.transform;
        public Transform SmokeRoot => _smokeRoot == null ? null : _smokeRoot.transform;

        public void Initialize(PlayerController player)
        {
            _player = player;
            _effectRoot = new GameObject("Dash Speed Effect");
            _effectRoot.transform.SetParent(transform, false);
            _effectRoot.transform.localPosition = Vector3.zero;
            _effectRoot.transform.localRotation = Quaternion.identity;

            CreateSmokePuffs();
            _effectRoot.SetActive(false);
            _smokeRoot.SetActive(false);
        }

        private void LateUpdate()
        {
            var isDashing = _player != null && _player.IsDashing && !_player.IsRidingBike;
            if (_effectRoot != null)
            {
                _effectRoot.SetActive(isDashing);
            }

            if (_smokeRoot == null)
            {
                return;
            }

            _smokeRoot.SetActive(isDashing);
            if (!isDashing)
            {
                _smokeClock = 0f;
                return;
            }

            _smokeClock += Time.deltaTime;
            for (var index = 0; index < _smokePuffs.Length; index++)
            {
                var phase = Mathf.Repeat(_smokeClock * 3.2f + index / (float)_smokePuffs.Length, 1f);
                var side = index % 2 == 0 ? -1f : 1f;
                var puff = _smokePuffs[index];
                puff.localPosition = new Vector3(
                    side * (0.10f + phase * 0.13f),
                    0.13f + phase * 0.30f,
                    -0.48f - phase * 0.92f);
                var size = Mathf.Lerp(0.16f, 0.42f, phase) * SmokeScaleMultiplier;
                puff.localScale = new Vector3(size, size * 0.72f, size);

                var color = _smokeMaterials[index].color;
                color.a = Mathf.Lerp(0.56f, 0f, phase);
                _smokeMaterials[index].color = color;
                if (_smokeMaterials[index].HasProperty("_BaseColor"))
                {
                    _smokeMaterials[index].SetColor("_BaseColor", color);
                }
            }
        }

        private void OnDisable()
        {
            if (_effectRoot != null)
            {
                _effectRoot.SetActive(false);
            }

            if (_smokeRoot != null)
            {
                _smokeRoot.SetActive(false);
            }
        }

        private void CreateSmokePuffs()
        {
            _smokeRoot = new GameObject("Dash Smoke Puffs");
            _smokeRoot.transform.SetParent(_effectRoot.transform, false);
            _smokeRoot.transform.localPosition = Vector3.zero;
            _smokeRoot.transform.localRotation = Quaternion.identity;

            for (var index = 0; index < _smokePuffs.Length; index++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Dash Smoke Puff " + (index + 1);
                puff.transform.SetParent(_smokeRoot.transform, false);
                var collider = puff.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                var material = PlayerIdentityMarker.CreateTransparentMaterial(
                    "Dash Smoke " + (index + 1),
                    index % 2 == 0
                        ? new Color(0.92f, 0.93f, 0.88f, 0.56f)
                        : new Color(0.76f, 0.82f, 0.80f, 0.48f));
                puff.GetComponent<Renderer>().sharedMaterial = material;
                _smokePuffs[index] = puff.transform;
                _smokeMaterials[index] = material;
            }
        }

    }
}
