using CookedOut.Domain;
using UnityEngine;

namespace CookedOut.Presentation
{
    public sealed class FireIncidentController : MonoBehaviour
    {
        public const float RequiredExtinguishSeconds = 2.5f;
        private KitchenGameController _game;
        private Transform _flameRoot;
        private Transform _foamRoot;
        private float _elapsed;
        private bool _extinguished;

        public float Progress => Mathf.Clamp01(_elapsed / RequiredExtinguishSeconds);
        public bool IsExtinguished => _extinguished;

        public static FireIncidentController Spawn(
            KitchenGameController game,
            Transform parent,
            Vector3 worldPosition)
        {
            var root = new GameObject("Tutorial Fire Hazard");
            root.transform.SetParent(parent, false);
            root.transform.position = worldPosition;

            var anchor = new GameObject("Fire Interaction Anchor").transform;
            anchor.SetParent(root.transform, false);
            anchor.localPosition = new Vector3(0f, 0.25f, 0f);
            var outline = CreatePrimitive(
                "Fire Selection Ring", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, 0.03f, 0f), new Vector3(1.15f, 0.025f, 1.15f),
                new Color(1f, 0.84f, 0.12f));
            outline.SetActive(false);

            var station = root.AddComponent<InteractableStation>();
            station.Initialize(game, StationKind.FireHazard, anchor, outline, null);
            var incident = root.AddComponent<FireIncidentController>();
            incident._game = game;
            incident.BuildVisuals();
            station.AttachFireIncident(incident);
            return incident;
        }

        public bool AdvanceExtinguish(float deltaSeconds, out string reason)
        {
            if (_extinguished)
            {
                reason = "火は消えています";
                return false;
            }
            if (deltaSeconds <= 0f)
            {
                reason = "消火時間は0より大きくしてください";
                return false;
            }

            _elapsed = Mathf.Min(RequiredExtinguishSeconds, _elapsed + deltaSeconds);
            _foamRoot.gameObject.SetActive(true);
            if (_elapsed >= RequiredExtinguishSeconds)
            {
                _extinguished = true;
                _flameRoot.gameObject.SetActive(false);
                GetComponent<InteractableStation>().enabled = false;
                _game.OnFireExtinguished();
            }
            reason = string.Empty;
            return true;
        }

        private void Update()
        {
            if (_flameRoot != null && !_extinguished)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.10f;
                var remaining = Mathf.Lerp(0.28f, 1f, 1f - Progress);
                _flameRoot.localScale = Vector3.one * pulse * remaining;
                _flameRoot.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 7f) * 9f, 0f);
            }
            if (_foamRoot != null && _foamRoot.gameObject.activeSelf)
            {
                _foamRoot.localRotation *= Quaternion.Euler(0f, Time.deltaTime * 160f, 0f);
                _foamRoot.localScale = Vector3.one * (0.9f + Mathf.Sin(Time.time * 18f) * 0.12f);
            }
        }

        private void BuildVisuals()
        {
            _flameRoot = new GameObject("Animated Flames").transform;
            _flameRoot.SetParent(transform, false);
            for (var index = 0; index < 7; index++)
            {
                var angle = index * Mathf.PI * 2f / 7f;
                var radius = index == 0 ? 0f : 0.34f;
                var flame = CreatePrimitive(
                    "Flame " + (index + 1), PrimitiveType.Sphere, _flameRoot,
                    new Vector3(Mathf.Cos(angle) * radius, 0.36f + (index % 3) * 0.16f, Mathf.Sin(angle) * radius),
                    new Vector3(0.32f, 0.75f, 0.32f),
                    index % 2 == 0 ? new Color(1f, 0.20f, 0.025f) : new Color(1f, 0.67f, 0.04f));
                flame.transform.localRotation = Quaternion.Euler(0f, index * 31f, index % 2 == 0 ? 12f : -12f);
            }

            _foamRoot = new GameObject("Animated Extinguisher Foam").transform;
            _foamRoot.SetParent(transform, false);
            _foamRoot.localPosition = new Vector3(0f, 0.52f, -0.35f);
            for (var index = 0; index < 9; index++)
            {
                var angle = index * Mathf.PI * 2f / 9f;
                CreatePrimitive(
                    "Foam Puff " + (index + 1), PrimitiveType.Sphere, _foamRoot,
                    new Vector3(Mathf.Cos(angle) * 0.32f, (index % 3) * 0.10f, Mathf.Sin(angle) * 0.23f),
                    Vector3.one * (0.13f + (index % 2) * 0.05f), Color.white);
            }
            _foamRoot.gameObject.SetActive(false);
        }

        private static GameObject CreatePrimitive(
            string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            var collider = visual.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            visual.GetComponent<Renderer>().material = GrayboxMaterials.Create(color);
            return visual;
        }
    }

    public sealed class DishwashingMotion : MonoBehaviour
    {
        private DeliveryContainer _plate;
        private Vector3 _restPosition;
        private Transform _bubbles;

        public void Initialize(DeliveryContainer plate)
        {
            _plate = plate;
            _restPosition = transform.localPosition;
            _bubbles = new GameObject("Soap Bubbles").transform;
            _bubbles.SetParent(transform, false);
            _bubbles.localPosition = Vector3.up * 0.18f;
            for (var index = 0; index < 6; index++)
            {
                var bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bubble.name = "Bubble " + (index + 1);
                bubble.transform.SetParent(_bubbles, false);
                bubble.transform.localPosition = new Vector3((index - 2.5f) * 0.13f, (index % 2) * 0.10f, 0f);
                bubble.transform.localScale = Vector3.one * (0.09f + (index % 3) * 0.025f);
                var collider = bubble.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                bubble.GetComponent<Renderer>().material = GrayboxMaterials.Create(new Color(0.58f, 0.90f, 1f, 0.85f));
            }
        }

        private void Update()
        {
            if (_plate == null || !_plate.IsDirty) return;
            transform.localPosition = _restPosition + Vector3.right * Mathf.Sin(Time.time * 17f) * 0.055f;
            transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 13f) * 12f, 0f);
            if (_bubbles != null) _bubbles.localRotation *= Quaternion.Euler(0f, Time.deltaTime * 120f, 0f);
        }
    }

    public sealed class CourierDispatchMotion : MonoBehaviour
    {
        private Vector3 _origin;
        private float _elapsed;

        public static CourierDispatchMotion Spawn(Vector3 origin)
        {
            var root = new GameObject("Courier Pickup Animation");
            root.transform.position = origin;
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Insulated Meal Carrier";
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = new Vector3(0.70f, 0.42f, 0.55f);
            box.GetComponent<Renderer>().material = GrayboxMaterials.Create(new Color(0.10f, 0.67f, 0.70f));
            Destroy(box.GetComponent<Collider>());
            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Carrier Lid";
            lid.transform.SetParent(root.transform, false);
            lid.transform.localPosition = Vector3.up * 0.25f;
            lid.transform.localScale = new Vector3(0.74f, 0.10f, 0.59f);
            lid.GetComponent<Renderer>().material = GrayboxMaterials.Create(new Color(0.94f, 0.88f, 0.72f));
            Destroy(lid.GetComponent<Collider>());
            var motion = root.AddComponent<CourierDispatchMotion>();
            motion._origin = origin;
            return motion;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / 1.15f);
            transform.position = _origin + new Vector3(t * 3.2f, Mathf.Sin(t * Mathf.PI) * 1.4f, t * 1.7f);
            transform.rotation = Quaternion.Euler(0f, t * 220f, 0f);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, t);
            if (t >= 1f) Destroy(gameObject);
        }
    }

    public sealed class DynamicKitchenConnector : MonoBehaviour
    {
        public const float WarningSeconds = 2f;
        public const float TravelSeconds = 3f;
        public const float RestSeconds = 5f;
        private Vector3 _left;
        private Vector3 _right;
        private float _cycle;
        private Renderer[] _renderers;

        public float WarningProgress => Mathf.Clamp01(_cycle / WarningSeconds);
        public int TransitionCount { get; private set; }

        public static DynamicKitchenConnector Spawn(Transform parent, KitchenGridRuntime grid, GridCoordinate anchor)
        {
            var root = new GameObject("Slow Dynamic Kitchen Connector");
            root.transform.SetParent(parent, false);
            var component = root.AddComponent<DynamicKitchenConnector>();
            var center = grid.CellCenter(anchor, 0.42f);
            component._left = center + Vector3.left * grid.Definition.Space.CellSize * 0.55f;
            component._right = center + Vector3.right * grid.Definition.Space.CellSize * 0.55f;
            root.transform.position = component._left;
            if (!KitchenProductionAssetFactory.TryInstantiateSpecial(
                    "DynamicBridge", "station.dynamic.bridge", root.transform, grid.Definition.Space.CellSize, out _))
            {
                var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bridge.name = "Moving Bridge";
                bridge.transform.SetParent(root.transform, false);
                bridge.transform.localScale = new Vector3(1.45f, 0.25f, 1.1f);
                bridge.GetComponent<Renderer>().material = GrayboxMaterials.Create(new Color(0.08f, 0.62f, 0.67f));
                Destroy(bridge.GetComponent<Collider>());
            }
            var collider = root.AddComponent<BoxCollider>();
            collider.center = Vector3.up * 0.15f;
            collider.size = new Vector3(1.45f, 0.55f, 1.1f);
            component._renderers = root.GetComponentsInChildren<Renderer>(true);
            return component;
        }

        private void Update()
        {
            _cycle += Time.deltaTime;
            var forward = TransitionCount % 2 == 0;
            if (_cycle < WarningSeconds)
            {
                var color = Mathf.FloorToInt(_cycle * 6f) % 2 == 0
                    ? new Color(1f, 0.58f, 0.05f)
                    : new Color(0.10f, 0.67f, 0.70f);
                foreach (var renderer in _renderers) renderer.material.color = color;
                return;
            }

            var travel = Mathf.Clamp01((_cycle - WarningSeconds) / TravelSeconds);
            var eased = travel * travel * (3f - 2f * travel);
            transform.position = Vector3.Lerp(forward ? _left : _right, forward ? _right : _left, eased);
            if (_cycle >= WarningSeconds + TravelSeconds + RestSeconds)
            {
                _cycle = 0f;
                TransitionCount++;
            }
        }
    }
}
