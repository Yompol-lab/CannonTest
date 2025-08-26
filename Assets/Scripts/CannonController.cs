using UnityEngine;
using UnityEngine.UI;

public class CannonController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Transform baseTransform;   // Se desliza en X (mundo)
    [SerializeField] Transform barrelPivot;     // Solo pivote de elevación (rotación local)
    [SerializeField] Transform muzzle;          // Boca del cañón (su Z/forward = dirección de disparo)

    [Header("UI")]
    [SerializeField] Slider horizontalSlider;
    [SerializeField] Slider verticalSlider;
    [SerializeField] bool invertVerticalSlider = false;

    [Header("Rangos de movimiento")]
    [SerializeField] float minX = -10f;
    [SerializeField] float maxX = 10f;
    [SerializeField] float minElevation = 0f;   // grados
    [SerializeField] float maxElevation = 60f;  // grados

    public enum Axis { X, Y, Z }
    [SerializeField] Axis elevationAxis = Axis.X;

    [Header("Disparo")]
    [SerializeField] Rigidbody projectilePrefab;
    [SerializeField] float muzzleVelocity = 30f;
    [SerializeField] float fireCooldown = 0.3f;
    [SerializeField] float projectileLife = 10f;
    [SerializeField] float spawnForwardOffset = 0.25f; // empuja el spawn fuera del cañón
    [SerializeField] bool ignoreCollisionWithCannon = true;
    [SerializeField] AudioSource fireSfx;

    float nextFireTime;
    Quaternion barrelInitialLocalRot;

    void Reset()
    {
        baseTransform = transform;
        barrelPivot = transform;
        muzzle = transform;
    }

    void Awake()
    {
        barrelInitialLocalRot = barrelPivot ? barrelPivot.localRotation : Quaternion.identity;

        if (horizontalSlider)
        {
            horizontalSlider.minValue = 0f;
            horizontalSlider.maxValue = 1f;
            horizontalSlider.onValueChanged.AddListener(SetHorizontalNormalized);
        }
        if (verticalSlider)
        {
            verticalSlider.minValue = 0f;
            verticalSlider.maxValue = 1f;
            verticalSlider.onValueChanged.AddListener(SetElevationNormalized);
        }
    }

    void Start()
    {
        if (baseTransform && horizontalSlider)
        {
            float t = Mathf.InverseLerp(minX, maxX, baseTransform.position.x);
            horizontalSlider.SetValueWithoutNotify(Mathf.Clamp01(t));
            SetHorizontalNormalized(horizontalSlider.value);
        }

        if (barrelPivot && verticalSlider)
        {
            // arranca en el mínimo por defecto
            float t = 0f;
            verticalSlider.SetValueWithoutNotify(invertVerticalSlider ? 1f - t : t);
            SetElevationNormalized(verticalSlider.value);
        }
    }

    public void SetHorizontalNormalized(float t)
    {
        float x = Mathf.Lerp(minX, maxX, Mathf.Clamp01(t));
        if (baseTransform)
        {
            var p = baseTransform.position;
            p.x = x;
            baseTransform.position = p;
        }
    }

    public void SetElevationNormalized(float t)
    {
        if (invertVerticalSlider) t = 1f - t;
        float angle = Mathf.Lerp(minElevation, maxElevation, Mathf.Clamp01(t));

        if (!barrelPivot) return;

        Vector3 axis = Vector3.right;           // X
        if (elevationAxis == Axis.Y) axis = Vector3.up;
        else if (elevationAxis == Axis.Z) axis = Vector3.forward;

        // Rotación local estable: orientación base * giro en eje local elegido
        barrelPivot.localRotation = barrelInitialLocalRot * Quaternion.AngleAxis(angle, axis);
    }

    public void Fire()
    {
        if (Time.time < nextFireTime) return;
        if (!projectilePrefab || !muzzle) return;

        nextFireTime = Time.time + fireCooldown;

        // Spawnea un poco por delante del muzzle
        Vector3 spawnPos = muzzle.position + muzzle.forward * Mathf.Max(0f, spawnForwardOffset);
        Quaternion rot = muzzle.rotation;

        Rigidbody rb = Instantiate(projectilePrefab, spawnPos, rot);
        rb.velocity = muzzle.forward * muzzleVelocity;

        // Evita golpear al cañón al salir
        if (ignoreCollisionWithCannon)
        {
            var projCols = rb.GetComponentsInChildren<Collider>();
            var cannonCols = (baseTransform ? baseTransform : transform).GetComponentsInChildren<Collider>();
            foreach (var pc in projCols)
                foreach (var cc in cannonCols)
                    if (pc && cc) Physics.IgnoreCollision(pc, cc, true);
        }

        if (projectileLife > 0f) Destroy(rb.gameObject, projectileLife);
        if (fireSfx) fireSfx.Play();
    }

    void Update()
    {
        // Controles de prueba (A/D, W/S, Space)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (horizontalSlider && Mathf.Abs(h) > 0.01f)
            horizontalSlider.value = Mathf.Clamp01(horizontalSlider.value + h * Time.deltaTime);
        if (verticalSlider && Mathf.Abs(v) > 0.01f)
            verticalSlider.value = Mathf.Clamp01(verticalSlider.value + v * Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Space)) Fire();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (maxX < minX) maxX = minX;
        if (maxElevation < minElevation) maxElevation = minElevation;
        if (horizontalSlider) SetHorizontalNormalized(horizontalSlider.value);
        if (verticalSlider) SetElevationNormalized(verticalSlider.value);
    }
#endif
}
