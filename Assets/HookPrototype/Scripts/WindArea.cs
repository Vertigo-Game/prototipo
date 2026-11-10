using System.Collections.Generic;
using UnityEngine;

namespace HookPrototype
{
    /// <summary>A horizontal wind band positioned in the world, independent of camera movement.</summary>
    [DisallowMultipleComponent]
    public sealed class WindArea : MonoBehaviour
    {
        public enum OriginSide { Left, Right }

        [Header("Área (posição X = origem; posição Y = centro da faixa)")]
        [SerializeField] private OriginSide origin = OriginSide.Left;
        [SerializeField, Min(0.1f)] private float width = 10f;
        [SerializeField, Min(0.1f)] private float height = 3f;

        [Header("Empurrão")]
        [Tooltip("Aceleração horizontal em unidades/s², independente da aceleração do jogador. Zero desliga o vento.")]
        [SerializeField, Min(0f)] private float strength = 60f;
        [Tooltip("Velocidade mínima na direção do vento ao entrar na área, inclusive por baixo. Zero remove o lançamento inicial.")]
        [SerializeField, Min(0f)] private float launchSpeed = 14f;
        [Tooltip("Velocidade horizontal máxima produzida pelo vento. Não freia um jogador que já esteja mais rápido na mesma direção.")]
        [SerializeField, Min(0.1f)] private float maxSpeed = 30f;

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
        [Tooltip("Resolução da grade visual. Valores menores produzem pixels maiores.")]
        [SerializeField, Range(1, 64)] private int pixelsPerUnit = 16;
        [Tooltip("Distância, em unidades do mundo, em que as linhas desaparecem antes do fim da área.")]
        [SerializeField, Min(0.1f)] private float fadeOutDistance = 2f;

        private static readonly HashSet<WindArea> ActiveAreas = new HashSet<WindArea>();
        private MeshRenderer visualRenderer;
        private Mesh pixelMesh;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color32> vertexColors = new List<Color32>();
        private readonly List<int> triangles = new List<int>();
        private Material fallbackMaterial;
        private float animationTime;

        public readonly struct Influence
        {
            public readonly float Acceleration;
            public readonly float MaxSpeed;
            public readonly float LaunchSpeed;
            public bool Active => !Mathf.Approximately(Acceleration, 0f);

            public Influence(float acceleration, float maxSpeed, float launchSpeed)
            {
                Acceleration = acceleration;
                MaxSpeed = maxSpeed;
                LaunchSpeed = launchSpeed;
            }

            public float Apply(float horizontalVelocity, float deltaTime)
            {
                if (!Active) return horizontalVelocity;
                float direction = Mathf.Sign(Acceleration);
                float speed = horizontalVelocity * direction;
                if (LaunchSpeed > 0f)
                {
                    speed = Mathf.Max(speed, Mathf.Min(LaunchSpeed, MaxSpeed));
                }
                if (speed < MaxSpeed)
                {
                    speed = Mathf.Min(MaxSpeed, speed + Mathf.Abs(Acceleration) * deltaTime);
                }
                return speed * direction;
            }
        }

        private void OnEnable()
        {
            ActiveAreas.Add(this);
        }

        private void OnDisable()
        {
            ActiveAreas.Remove(this);
            SetVisualVisible(false);
        }

        /// <summary>Queries the player's collider directly: no trigger latency or solid wind collider.</summary>
        public static Influence Sample(Collider2D playerCollider, HashSet<WindArea> previousAreas)
        {
            if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy)
            {
                previousAreas.Clear();
                return default;
            }

            Bounds playerBounds = playerCollider.bounds;
            float acceleration = 0f;
            float leftMax = 0f;
            float rightMax = 0f;
            float launch = 0f;
            previousAreas.IntersectWith(ActiveAreas);
            foreach (WindArea area in ActiveAreas)
            {
                if (area == null || !area.isActiveAndEnabled || area.strength <= 0f ||
                    area.gameObject.scene != playerCollider.gameObject.scene || !area.TryGetArea(out Rect rect))
                {
                    previousAreas.Remove(area);
                    continue;
                }

                if (playerBounds.max.x > rect.xMin && playerBounds.min.x < rect.xMax &&
                    playerBounds.max.y > rect.yMin && playerBounds.min.y < rect.yMax)
                {
                    float direction = area.origin == OriginSide.Left ? 1f : -1f;
                    acceleration += direction * area.strength;
                    if (direction > 0f) rightMax = Mathf.Max(rightMax, area.maxSpeed);
                    else leftMax = Mathf.Max(leftMax, area.maxSpeed);
                    if (previousAreas.Add(area)) launch += direction * area.launchSpeed;
                }
                else previousAreas.Remove(area);
            }

            // Entry launch happens once per contact, not once per physics tick or dash.
            float sign = Mathf.Sign(acceleration);
            return new Influence(acceleration, sign > 0f ? rightMax : leftMax, Mathf.Max(0f, sign * launch));
        }

        public bool TryGetArea(out Rect area)
        {
            // The source is a world position. A following camera must never move the wind band.
            float edge = transform.position.x;
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
                SetVisualVisible(false);
                return;
            }

            if (visualRenderer == null)
            {
                CreateVisual();
            }

            animationTime += Time.deltaTime;
            vertices.Clear();
            vertexColors.Clear();
            triangles.Clear();
            float pixelSize = 1f / Mathf.Max(1, pixelsPerUnit);
            float thickness = Mathf.Max(1f, Mathf.Round(lineWidth / pixelSize)) * pixelSize;
            float fadeDistance = Mathf.Min(Mathf.Max(pixelSize, fadeOutDistance), area.width);
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            for (int i = 0; i < lineCount; i++)
            {
                float seed = Mathf.Repeat(i * 0.618034f, 1f);
                float length = Mathf.Min(lineLength * (0.65f + seed * 0.7f), area.width);
                float travelLength = area.width + length;
                float progress = Mathf.Repeat(seed + animationTime * visualSpeed / travelLength, 1f);
                float head = progress * travelLength;
                float row = Mathf.Repeat(i * 0.381966f + 0.13f, 1f);
                float y = Mathf.Lerp(area.yMin, area.yMax, row);
                float amplitude = Mathf.Min(waveAmplitude, area.height * 0.15f);
                int columns = Mathf.Clamp(Mathf.CeilToInt(length / pixelSize), 1, 512);
                for (int column = 0; column <= columns; column++)
                {
                    float t = column / (float)columns;
                    float distance = head - length + column * pixelSize;
                    // Never clamp points to an edge: that stacks them into a visible wall.
                    if (distance < 0f || distance >= area.width) continue;
                    float x = origin == OriginSide.Left ? area.xMin + distance : area.xMax - distance;
                    float wave = Mathf.Sin(t * Mathf.PI * 2f + seed * 8f + animationTime * 3f) * amplitude;
                    x = Mathf.Floor(x / pixelSize) * pixelSize;
                    float pixelY = Mathf.Floor((y + wave) / pixelSize) * pixelSize;
                    if (pixelY < area.yMin || pixelY + thickness > area.yMax) continue;

                    float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((area.width - distance) / fadeDistance));
                    float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance / Mathf.Min(pixelSize * 3f, area.width)));
                    Color tint = color;
                    tint.a *= fadeIn * fadeOut * Mathf.Lerp(0.25f, 1f, t);
                    if (tint.a < 1f / 255f) continue;
                    AddPixel(x, pixelY, pixelSize, thickness, tint, worldToLocal);
                }
            }

            pixelMesh.Clear();
            pixelMesh.SetVertices(vertices);
            pixelMesh.SetColors(vertexColors);
            pixelMesh.SetTriangles(triangles, 0);
            visualRenderer.sortingOrder = sortingOrder;
            visualRenderer.enabled = true;
        }

        private void AddPixel(float x, float y, float width, float height, Color tint, Matrix4x4 worldToLocal)
        {
            int start = vertices.Count;
            float z = transform.position.z;
            vertices.Add(worldToLocal.MultiplyPoint3x4(new Vector3(x, y, z)));
            vertices.Add(worldToLocal.MultiplyPoint3x4(new Vector3(x, y + height, z)));
            vertices.Add(worldToLocal.MultiplyPoint3x4(new Vector3(x + width, y + height, z)));
            vertices.Add(worldToLocal.MultiplyPoint3x4(new Vector3(x + width, y, z)));
            for (int corner = 0; corner < 4; corner++) vertexColors.Add(tint);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private void CreateVisual()
        {
            Material material = lineMaterial;
            if (material == null)
            {
                if (fallbackMaterial == null)
                {
                    fallbackMaterial = new Material(Shader.Find("Sprites/Default"));
                }
                material = fallbackMaterial;
            }

            GameObject visual = new GameObject("Wind Pixels");
            visual.transform.SetParent(transform, false);
            pixelMesh = new Mesh { name = "Wind Pixel Strokes", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            pixelMesh.MarkDynamic();
            visual.AddComponent<MeshFilter>().sharedMesh = pixelMesh;
            visualRenderer = visual.AddComponent<MeshRenderer>();
            visualRenderer.sharedMaterial = material;
            visualRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            visualRenderer.receiveShadows = false;
        }

        private void SetVisualVisible(bool visible)
        {
            if (visualRenderer != null) visualRenderer.enabled = visible;
        }

        private void OnDestroy()
        {
            ActiveAreas.Remove(this);
            if (visualRenderer != null) Destroy(visualRenderer.gameObject);
            if (pixelMesh != null) Destroy(pixelMesh);
            if (fallbackMaterial != null) Destroy(fallbackMaterial);
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.1f, height);
            strength = Mathf.Max(0f, strength);
            launchSpeed = Mathf.Max(0f, launchSpeed);
            maxSpeed = Mathf.Max(0.1f, maxSpeed);
            lineCount = Mathf.Clamp(lineCount, 1, 80);
            pixelsPerUnit = Mathf.Clamp(pixelsPerUnit, 1, 64);
            fadeOutDistance = Mathf.Max(0.1f, fadeOutDistance);
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
