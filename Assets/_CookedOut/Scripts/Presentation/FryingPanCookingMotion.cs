using CookedOut.Domain;
using UnityEngine;

namespace CookedOut.Presentation
{
    public sealed class FryingPanCookingMotion : MonoBehaviour
    {
        private const int SteamCount = 3;
        private readonly Transform[] _steam = new Transform[SteamCount];
        private readonly Renderer[] _steamRenderers = new Renderer[SteamCount];
        private FryingPan _pan;
        private Transform _patty;
        private Renderer _pattyRenderer;
        private Vector3 _pattyPosition;
        private Vector3 _pattyScale;

        public void Initialize(FryingPan pan, Transform patty, Renderer pattyRenderer)
        {
            _pan = pan;
            _patty = patty;
            _pattyRenderer = pattyRenderer;
            _pattyPosition = patty.localPosition;
            _pattyScale = patty.localScale;

            for (var index = 0; index < SteamCount; index++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Pan Steam Puff " + (index + 1);
                puff.transform.SetParent(transform, false);
                puff.transform.localScale = Vector3.one * (0.075f + index * 0.012f);
                var collider = puff.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                var renderer = puff.GetComponent<Renderer>();
                renderer.material = GrayboxMaterials.CreateTransparent(new Color(0.92f, 0.96f, 1f, 0f));
                _steam[index] = puff.transform;
                _steamRenderers[index] = renderer;
            }
        }

        private void Update()
        {
            if (_pan == null || _patty == null || _pan.IsEmpty || _pan.IsBurned)
            {
                SetSteamVisible(false);
                return;
            }

            var time = Time.time;
            var cooking = !_pan.IsCooked;
            var warning = _pan.IsCooked && _pan.RemainingBurnSeconds <= 2f;
            var sizzle = cooking ? 0.018f : 0.007f;
            _patty.localPosition = _pattyPosition + new Vector3(
                Mathf.Sin(time * 15f) * sizzle,
                Mathf.Abs(Mathf.Sin(time * 12f)) * sizzle,
                Mathf.Cos(time * 13f) * sizzle);
            _patty.localEulerAngles = new Vector3(0f, Mathf.Sin(time * 7f) * (cooking ? 2.2f : 0.8f), 0f);

            var pulse = warning ? 1f + Mathf.Sin(time * 11f) * 0.07f : 1f;
            _patty.localScale = _pattyScale * pulse;
            _pattyRenderer.material.color = warning
                ? Color.Lerp(KitchenArtPalette.BeefCooked, new Color(0.88f, 0.20f, 0.035f),
                    0.28f + 0.22f * (Mathf.Sin(time * 11f) + 1f))
                : Color.Lerp(KitchenArtPalette.BeefRaw, KitchenArtPalette.BeefCooked, _pan.CookProgress);

            UpdateSteam(time, _pan.CookProgress);
        }

        private void UpdateSteam(float time, float cookProgress)
        {
            var visible = cookProgress > 0.08f;
            for (var index = 0; index < SteamCount; index++)
            {
                var cycle = Mathf.Repeat(time * 0.42f + index / (float)SteamCount, 1f);
                var side = index - 1;
                _steam[index].gameObject.SetActive(visible);
                _steam[index].localPosition = new Vector3(
                    side * 0.14f + Mathf.Sin(time * 2.3f + index) * 0.035f,
                    0.39f + cycle * 0.48f,
                    (index % 2 == 0 ? -0.09f : 0.08f));
                var size = Mathf.Lerp(0.65f, 1.45f, cycle);
                _steam[index].localScale = Vector3.one * (0.08f * size);
                var alpha = Mathf.Sin(cycle * Mathf.PI) * Mathf.Lerp(0.18f, 0.52f, cookProgress);
                _steamRenderers[index].material.color = new Color(0.92f, 0.96f, 1f, alpha);
            }
        }

        private void SetSteamVisible(bool visible)
        {
            for (var index = 0; index < SteamCount; index++)
            {
                if (_steam[index] != null)
                {
                    _steam[index].gameObject.SetActive(visible);
                }
            }
        }
    }
}
