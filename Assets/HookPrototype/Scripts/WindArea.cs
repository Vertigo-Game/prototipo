using System.Collections.Generic;
using UnityEngine;

namespace HookPrototype
{
    /// <summary>A directional wind band positioned in the world, independent of camera movement.</summary>
    [DisallowMultipleComponent]
    public sealed class WindArea : MonoBehaviour
    {
        // Preserve the serialized values of existing horizontal wind areas.
        public enum OriginSide
        {
            [InspectorName("Esquerda → Direita")] Left = 0,
            [InspectorName("Direita → Esquerda")] Right = 1,
            [InspectorName("Baixo → Cima")] Bottom = 2,
            [InspectorName("Cima → Baixo")] Top = 3
        }

        [Header("Área de vento")]
        [Tooltip("Lado de onde o vento sai. O Transform fica no centro desse lado da área.")]
        [SerializeField] private OriginSide origin = OriginSide.Left;
        [SerializeField, Min(0.1f)] private float width = 10f;
        [SerializeField, Min(0.1f)] private float height = 3f;

        [Header("Empurrão")]
        [Tooltip("Aceleração na direção do vento em unidades/s². Zero desliga o vento.")]
        [SerializeField, Min(0f)] private float strength = 15f;
        [Tooltip("Velocidade mínima na direção do vento ao entrar na área. Zero remove o lançamento inicial.")]
        [SerializeField, Min(0f)] private float launchSpeed = 1f;
        [Tooltip("Velocidade máxima na direção do vento. Não freia um jogador que já esteja mais rápido na mesma direção.")]
        [SerializeField, Min(0.1f)] private float maxSpeed = 7f;

        [Header("Intermitência (vento que para e volta)")]
        [Tooltip("Ligado: o vento sopra por um tempo, para por outro e repete. Desligado: sopra sempre.")]
        [SerializeField] private bool intermitente;
        [Tooltip("Segundos soprando em cada ciclo.")]
        [SerializeField, Min(0.1f)] private float tempoLigado = 3f;
        [Tooltip("Segundos parado em cada ciclo.")]
        [SerializeField, Min(0.1f)] private float tempoDesligado = 2f;
        [Tooltip("Desloca o começo do ciclo, em segundos, para vários ventos não ligarem e desligarem juntos. O ciclo reinicia junto com a cena.")]
        [SerializeField, Min(0f)] private float atraso;
        [Tooltip("Segundos que as linhas levam para aparecer quando o vento liga (fade in).")]
        [SerializeField, Min(0.01f)] private float transicaoVisual = 0.4f;
        [Tooltip("Segundos que as linhas levam para sumir quando o vento desliga (fade out). As linhas continuam correndo enquanto somem.")]
        [SerializeField, Min(0.01f)] private float fadeOutAoDesligar = 1f;

        [Header("Linhas de vento")]
        [SerializeField] private Material lineMaterial;
        [SerializeField, Range(1, 80)] private int lineCount = 4;
        [SerializeField] private Color color = new Color(0.8f, 0.95f, 1f, 0.55f);
        [SerializeField, Min(0.01f)] private float lineWidth = 0.01f;
        [SerializeField, Min(0.05f)] private float lineLength = 1.7f;
        [Tooltip("Use zero para linhas retas.")]
        [SerializeField, Min(0f)] private float waveAmplitude = 0.08f;
        [SerializeField, Min(0.1f)] private float visualSpeed = 9f;
        [SerializeField] private int sortingOrder = 8;
        [Tooltip("Resolução da grade visual. Valores menores produzem pixels maiores.")]
        [SerializeField, Range(1, 64)] private int pixelsPerUnit = 16;
        [Tooltip("Distância, em unidades do mundo, em que as linhas surgem aos poucos depois do começo da área (fade in).")]
        [SerializeField, Min(0.1f)] private float fadeInDistance = 2f;
        [Tooltip("Distância, em unidades do mundo, em que as linhas desaparecem antes do fim da área (fade out).")]
        [SerializeField, Min(0.1f)] private float fadeOutDistance = 2f;

        private static readonly HashSet<WindArea> ActiveAreas = new HashSet<WindArea>();
        private MeshRenderer visualRenderer;
        private Mesh pixelMesh;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color32> vertexColors = new List<Color32>();
        private readonly List<int> triangles = new List<int>();
        private Material fallbackMaterial;
        private float animationTime;
        private float visualIntensity = -1f;

        /// <summary>False enquanto um vento intermitente está parado: sem empurrão e sem linhas.</summary>
        public bool Soprando
        {
            get
            {
                if (!intermitente) return true;
                float ciclo = tempoLigado + tempoDesligado;
                return Mathf.Repeat(Time.timeSinceLevelLoad + atraso, ciclo) < tempoLigado;
            }
        }

        public Vector2 Direction => origin switch
        {
            OriginSide.Right => Vector2.left,
            OriginSide.Bottom => Vector2.up,
            OriginSide.Top => Vector2.down,
            _ => Vector2.right
        };

        public readonly struct Influence
        {
            public readonly Vector2 Acceleration;
            public readonly Vector2 MaxSpeed;
            public readonly Vector2 LaunchSpeed;
            public bool HorizontalActive => !Mathf.Approximately(Acceleration.x, 0f);
            public bool VerticalActive => !Mathf.Approximately(Acceleration.y, 0f);
            public bool Active => HorizontalActive || VerticalActive;

            public Influence(Vector2 acceleration, Vector2 maxSpeed, Vector2 launchSpeed)
            {
                Acceleration = acceleration;
                MaxSpeed = maxSpeed;
                LaunchSpeed = launchSpeed;
            }

            public Vector2 Apply(Vector2 velocity, float deltaTime)
            {
                return new Vector2(
                    ApplyAxis(velocity.x, Acceleration.x, MaxSpeed.x, LaunchSpeed.x, deltaTime),
                    ApplyAxis(velocity.y, Acceleration.y, MaxSpeed.y, LaunchSpeed.y, deltaTime));
            }

            private static float ApplyAxis(float velocity, float acceleration, float maxSpeed, float launchSpeed, float deltaTime)
            {
                if (Mathf.Approximately(acceleration, 0f)) return velocity;
                float direction = Mathf.Sign(acceleration);
                float speed = velocity * direction;
                if (launchSpeed > 0f)
                {
                    speed = Mathf.Max(speed, Mathf.Min(launchSpeed, maxSpeed));
                }
                if (speed < maxSpeed)
                {
                    speed = Mathf.Min(maxSpeed, speed + Mathf.Abs(acceleration) * deltaTime);
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
            Vector2 acceleration = Vector2.zero;
            Vector2 negativeMax = Vector2.zero;
            Vector2 positiveMax = Vector2.zero;
            Vector2 launch = Vector2.zero;
            previousAreas.IntersectWith(ActiveAreas);
            foreach (WindArea area in ActiveAreas)
            {
                if (area == null || !area.isActiveAndEnabled || area.strength <= 0f || !area.Soprando ||
                    area.gameObject.scene != playerCollider.gameObject.scene || !area.TryGetArea(out Rect rect))
                {
                    previousAreas.Remove(area);
                    continue;
                }

                if (playerBounds.max.x > rect.xMin && playerBounds.min.x < rect.xMax &&
                    playerBounds.max.y > rect.yMin && playerBounds.min.y < rect.yMax)
                {
                    Vector2 direction = area.Direction;
                    acceleration += direction * area.strength;
                    positiveMax = Vector2.Max(positiveMax, direction * area.maxSpeed);
                    negativeMax = Vector2.Max(negativeMax, -direction * area.maxSpeed);
                    if (previousAreas.Add(area)) launch += direction * area.launchSpeed;
                }
                else previousAreas.Remove(area);
            }

            // Entry launch happens once per contact, not once per physics tick or dash.
            Vector2 max = new Vector2(acceleration.x > 0f ? positiveMax.x : negativeMax.x,
                acceleration.y > 0f ? positiveMax.y : negativeMax.y);
            Vector2 entry = new Vector2(Mathf.Max(0f, Mathf.Sign(acceleration.x) * launch.x),
                Mathf.Max(0f, Mathf.Sign(acceleration.y) * launch.y));
            return new Influence(acceleration, max, entry);
        }

        public bool TryGetArea(out Rect area)
        {
            // The source is a world position. A following camera must never move the wind band.
            float areaWidth = Mathf.Max(0.1f, width);
            float areaHeight = Mathf.Max(0.1f, height);
            Vector2 size = new Vector2(areaWidth, areaHeight);
            Vector2 center = (Vector2)transform.position + Vector2.Scale(Direction, size) * 0.5f;
            area = new Rect(center - size * 0.5f, size);
            return true;
        }

        private void LateUpdate()
        {
            if (strength <= 0f || !TryGetArea(out Rect area))
            {
                SetVisualVisible(false);
                return;
            }

            // As linhas aparecem e somem aos poucos quando o vento liga e desliga.
            float alvoVisual = Soprando ? 1f : 0f;
            float duracaoVisual = Soprando ? transicaoVisual : fadeOutAoDesligar;
            visualIntensity = visualIntensity < 0f
                ? alvoVisual
                : Mathf.MoveTowards(visualIntensity, alvoVisual, Time.deltaTime / Mathf.Max(0.01f, duracaoVisual));
            if (visualIntensity <= 0f)
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
            Vector2 direction = Direction;
            bool horizontal = direction.x != 0f;
            float span = horizontal ? area.width : area.height;
            float crossSpan = horizontal ? area.height : area.width;
            float fadeInLength = Mathf.Min(Mathf.Max(pixelSize, fadeInDistance), span);
            float fadeOutLength = Mathf.Min(Mathf.Max(pixelSize, fadeOutDistance), span);
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            for (int i = 0; i < lineCount; i++)
            {
                float seed = Mathf.Repeat(i * 0.618034f, 1f);
                float length = Mathf.Min(lineLength * (0.65f + seed * 0.7f), span);
                float travelLength = span + length;
                float progress = Mathf.Repeat(seed + animationTime * visualSpeed / travelLength, 1f);
                float head = progress * travelLength;
                float row = Mathf.Repeat(i * 0.381966f + 0.13f, 1f);
                float cross = horizontal ? Mathf.Lerp(area.yMin, area.yMax, row) : Mathf.Lerp(area.xMin, area.xMax, row);
                float amplitude = Mathf.Min(waveAmplitude, crossSpan * 0.15f);
                int columns = Mathf.Clamp(Mathf.CeilToInt(length / pixelSize), 1, 512);
                for (int column = 0; column <= columns; column++)
                {
                    float t = column / (float)columns;
                    float distance = head - length + column * pixelSize;
                    // Never clamp points to an edge: that stacks them into a visible wall.
                    if (distance < 0f || distance >= span) continue;
                    float wave = Mathf.Sin(t * Mathf.PI * 2f + seed * 8f + animationTime * 3f) * amplitude;
                    float along = horizontal
                        ? (direction.x > 0f ? area.xMin + distance : area.xMax - distance)
                        : (direction.y > 0f ? area.yMin + distance : area.yMax - distance);
                    float x = horizontal ? along : cross + wave;
                    float y = horizontal ? cross + wave : along;
                    x = Mathf.Floor(x / pixelSize) * pixelSize;
                    y = Mathf.Floor(y / pixelSize) * pixelSize;
                    float pixelWidth = horizontal ? pixelSize : thickness;
                    float pixelHeight = horizontal ? thickness : pixelSize;
                    if (x < area.xMin || x + pixelWidth > area.xMax || y < area.yMin || y + pixelHeight > area.yMax) continue;

                    float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((span - distance) / fadeOutLength));
                    float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance / fadeInLength));
                    Color tint = color;
                    tint.a *= fadeIn * fadeOut * Mathf.Lerp(0.25f, 1f, t) * Mathf.SmoothStep(0f, 1f, visualIntensity);
                    if (tint.a < 1f / 255f) continue;
                    AddPixel(x, y, pixelWidth, pixelHeight, tint, worldToLocal);
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
            tempoLigado = Mathf.Max(0.1f, tempoLigado);
            tempoDesligado = Mathf.Max(0.1f, tempoDesligado);
            transicaoVisual = Mathf.Max(0.01f, transicaoVisual);
            fadeOutAoDesligar = Mathf.Max(0.01f, fadeOutAoDesligar);
            fadeInDistance = Mathf.Max(0.1f, fadeInDistance);
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
            Vector3 direction = Direction;
            Vector3 across = new Vector3(-direction.y, direction.x, 0f);
            float span = direction.x != 0f ? area.width : area.height;
            Vector3 tip = center + direction * Mathf.Min(1f, span * 0.3f);
            Gizmos.DrawLine(center, tip);
            Gizmos.DrawLine(tip, tip - direction * 0.2f + across * 0.15f);
            Gizmos.DrawLine(tip, tip - direction * 0.2f - across * 0.15f);
            Gizmos.color = previous;
        }
    }
}
