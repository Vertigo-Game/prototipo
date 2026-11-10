using System.Collections.Generic;
using UnityEngine;

namespace HookPrototype
{
    /// <summary>A horizontal wind band anchored to a camera edge at a fixed world height.</summary>
    [DisallowMultipleComponent]
    public sealed class WindArea : MonoBehaviour
    {
        public enum OriginSide { Left, Right }

        [Header("Área (unidades do mundo; posição Y define o trecho)")]
        [SerializeField] private OriginSide origin = OriginSide.Left;
        [SerializeField, Min(0.1f)] private float width = 10f;
        [SerializeField, Min(0.1f)] private float height = 3f;
        [SerializeField] private Camera sourceCamera;

        [Header("Empurrão")]
        [Tooltip("Velocidade horizontal adicional, em unidades/s. Zero desliga o empurrão e o visual.")]
        [SerializeField, Min(0f)] private float strength = 4f;

        [Header("Linhas de vento")]
        [SerializeField] private Material lineMaterial;
        [SerializeField, Range(1, 80)] private int lineCount = 24;
        [SerializeField] private Color color = new Color(0.8f, 0.95f, 1f, 0.55f);
        [SerializeField, Min(0.01f)] private float lineWidth = 0.035f;
        [SerializeField, Min(0.05f)] private float lineLength = 1.25f;
        [Tooltip("Use zero para linhas retas.")]
        [SerializeField, Min(0f)] private float waveAmplitude = 0.08f;
        [SerializeField, Min(0.1f)] private float visualSpeed = 5f;
        [SerializeField] private int sortingOrder = 8;

        private const int Segments = 10;
        private static readonly HashSet<WindArea> ActiveAreas = new HashSet<WindArea>();
        private LineRenderer[] lines;
        private Material fallbackMaterial;
        private float animationTime;

        public float HorizontalSpeed => (origin == OriginSide.Left ? 1f : -1f) * Mathf.Max(0f, strength);

        private void OnEnable()
        {
            ActiveAreas.Add(this);
        }

        private void OnDisable()
        {
            ActiveAreas.Remove(this);
            SetLinesVisible(false);
        }

        /// <summary>Queries the player's collider directly: no trigger latency or solid wind collider.</summary>
        public static float Sample(Collider2D playerCollider)
        {
            if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy)
            {
                return 0f;
            }

            Bounds playerBounds = playerCollider.bounds;
            float speed = 0f;
            foreach (WindArea area in ActiveAreas)
            {
                if (area == null || !area.isActiveAndEnabled || area.strength <= 0f ||
                    area.gameObject.scene != playerCollider.gameObject.scene || !area.TryGetArea(out Rect rect))
                {
                    continue;
                }

                if (playerBounds.max.x > rect.xMin && playerBounds.min.x < rect.xMax &&
                    playerBounds.max.y > rect.yMin && playerBounds.min.y < rect.yMax)
                {
                    speed += area.HorizontalSpeed;
                }
            }

            // Opposing areas cancel; areas in the same direction add their configured speeds.
            return speed;
        }

        public bool TryGetArea(out Rect area)
        {
            Camera camera = sourceCamera != null ? sourceCamera : Camera.main;
            if (camera == null || !camera.orthographic)
            {
                area = default;
                return false;
            }

            float depth = Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward);
            float edge = camera.ViewportToWorldPoint(new Vector3(origin == OriginSide.Left ? 0f : 1f, 0.5f, depth)).x;
            float areaWidth = Mathf.Max(0.1f, width);
            float areaHeight = Mathf.Max(0.1f, height);
            area = new Rect(origin == OriginSide.Left ? edge : edge - areaWidth,
                transform.position.y - areaHeight * 0.5f, areaWidth, areaHeight);
            return true;
        }

        private void LateUpdate()
        {
            if (strength <= 0f || !TryGetArea(out Rect area))
            {
                SetLinesVisible(false);
                return;
            }

            if (lines == null || lines.Length != lineCount)
            {
                CreateLines();
            }

            animationTime += Time.deltaTime;
            float speed = visualSpeed + strength * 0.3f;
            for (int i = 0; i < lines.Length; i++)
            {
                LineRenderer line = lines[i];
                float seed = Mathf.Repeat(i * 0.618034f, 1f);
                float progress = Mathf.Repeat(seed + animationTime * speed / area.width, 1f);
                float length = Mathf.Min(lineLength * (0.65f + seed * 0.7f), area.width);
                float head = progress * (area.width + length);
                float row = Mathf.Repeat(i * 0.381966f + 0.13f, 1f);
                float y = Mathf.Lerp(area.yMin, area.yMax, row);
                float amplitude = Mathf.Min(waveAmplitude, area.height * 0.15f);
                Color tint = color;
                tint.a *= Mathf.Clamp01(progress * 8f) * Mathf.Clamp01((1f - progress) * 8f);
                line.enabled = true;
                line.startColor = new Color(tint.r, tint.g, tint.b, tint.a * 0.25f);
                line.endColor = tint;
                line.startWidth = lineWidth * 0.5f;
                line.endWidth = lineWidth;
                line.sortingOrder = sortingOrder;
                for (int point = 0; point <= Segments; point++)
                {
                    float t = point / (float)Segments;
                    float distance = Mathf.Clamp(head - length + length * t, 0f, area.width);
                    float x = origin == OriginSide.Left ? area.xMin + distance : area.xMax - distance;
                    float wave = Mathf.Sin(t * Mathf.PI * 2f + seed * 8f + animationTime * 3f) * amplitude;
                    line.SetPosition(point, new Vector3(x, Mathf.Clamp(y + wave, area.yMin, area.yMax), transform.position.z));
                }
            }
        }

        private void CreateLines()
        {
            if (lines != null)
            {
                foreach (LineRenderer line in lines)
                {
                    if (line != null)
                    {
                        line.enabled = false;
                        Destroy(line.gameObject);
                    }
                }
            }

            Material material = lineMaterial;
            if (material == null)
            {
                if (fallbackMaterial == null)
                {
                    fallbackMaterial = new Material(Shader.Find("Sprites/Default"));
                }
                material = fallbackMaterial;
            }

            lines = new LineRenderer[Mathf.Clamp(lineCount, 1, 80)];
            for (int i = 0; i < lines.Length; i++)
            {
                GameObject stroke = new GameObject("Wind Stroke " + (i + 1));
                stroke.transform.SetParent(transform, false);
                LineRenderer line = stroke.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.positionCount = Segments + 1;
                line.numCapVertices = 3;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                lines[i] = line;
            }
        }

        private void SetLinesVisible(bool visible)
        {
            if (lines == null) return;
            foreach (LineRenderer line in lines)
            {
                if (line != null) line.enabled = visible;
            }
        }

        private void OnDestroy()
        {
            ActiveAreas.Remove(this);
            if (fallbackMaterial != null) Destroy(fallbackMaterial);
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.1f, height);
            strength = Mathf.Max(0f, strength);
            lineCount = Mathf.Clamp(lineCount, 1, 80);
        }

        private void OnDrawGizmos()
        {
            if (!TryGetArea(out Rect area)) return;
            Color previous = Gizmos.color;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.08f);
            Vector3 center = new Vector3(area.center.x, area.center.y, transform.position.z);
            Gizmos.DrawCube(center, new Vector3(area.width, area.height, 0f));
            Gizmos.color = new Color(color.r, color.g, color.b, 0.8f);
            Gizmos.DrawWireCube(center, new Vector3(area.width, area.height, 0f));
            float direction = origin == OriginSide.Left ? 1f : -1f;
            Vector3 tip = center + Vector3.right * direction * Mathf.Min(1f, area.width * 0.3f);
            Gizmos.DrawLine(center, tip);
            Gizmos.DrawLine(tip, tip + new Vector3(-direction * 0.2f, 0.15f, 0f));
            Gizmos.DrawLine(tip, tip + new Vector3(-direction * 0.2f, -0.15f, 0f));
            Gizmos.color = previous;
        }
    }
}
