using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controls atmospheric starry background effects for the Main Menu:
/// 1. Floating, twinkling star particles in front of the camera.
/// 2. Occasional shooting stars (meteors) streaking across the night sky.
/// 3. Subtle slow rotation of the astronomical skybox.
/// 4. Subtle mouse parallax for rich 3D depth.
/// </summary>
public class MainMenuStarEffects : MonoBehaviour
{
    [Header("Skybox Rotation")]
    [Tooltip("Target skybox transform to slowly rotate (auto-finds 'Skybox' if null)")]
    public Transform skyboxTransform;
    [Tooltip("Speed of celestial background rotation in degrees per second")]
    public float skyboxRotationSpeed = 0.35f;

    [Header("Floating Twinkling Stars")]
    [Tooltip("Material for the twinkling stars")]
    public Material starMaterial;
    [Range(20, 150)]
    public int maxStars = 80;
    public float starMinSize = 0.08f;
    public float starMaxSize = 0.28f;
    public float starLifetime = 6.0f;
    public float starSpreadWidth = 14f;
    public float starSpreadHeight = 9f;
    public float starDepth = 6f;

    [Header("Shooting Stars (Meteors)")]
    public bool enableShootingStars = true;
    public Material shootingStarMaterial;
    public float minShootingStarInterval = 4.0f;
    public float maxShootingStarInterval = 8.0f;

    [Header("Subtle Mouse Parallax")]
    public bool enableParallax = true;
    public float parallaxAmount = 1.2f;
    public float parallaxSmoothSpeed = 3.0f;

    // References
    private Camera mainCamera;
    private ParticleSystem ambientStarsPS;
    private ParticleSystem shootingStarsPS;
    private Quaternion initialCamRot;
    private Vector3 initialCamPos;
    private float nextShootingStarTime;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCamera != null)
        {
            initialCamRot = mainCamera.transform.localRotation;
            initialCamPos = mainCamera.transform.localPosition;

            // Ensure camera shows the full skybox with high far clip
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            if (mainCamera.farClipPlane < 2000f)
            {
                mainCamera.farClipPlane = 2500f;
            }
        }

        if (skyboxTransform == null)
        {
            GameObject skyObj = GameObject.Find("Skybox");
            if (skyObj != null)
            {
                skyboxTransform = skyObj.transform;
            }
        }

        // Load materials if unassigned
        if (starMaterial == null)
        {
            starMaterial = Resources.Load<Material>("Mat_StarParticle");
#if UNITY_EDITOR
            if (starMaterial == null)
            {
                starMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_StarParticle.mat");
            }
#endif
        }

        if (shootingStarMaterial == null)
        {
            shootingStarMaterial = Resources.Load<Material>("Mat_ShootingStar");
#if UNITY_EDITOR
            if (shootingStarMaterial == null)
            {
                shootingStarMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_ShootingStar.mat");
            }
#endif
        }

        SetupAmbientStars();
        SetupShootingStars();
        ScheduleNextShootingStar();
    }

    private void Update()
    {
        // 1. Celestial background slow rotation
        if (skyboxTransform != null)
        {
            skyboxTransform.Rotate(Vector3.up, skyboxRotationSpeed * Time.deltaTime, Space.World);
        }

        // 2. Subtle mouse parallax
        if (enableParallax && mainCamera != null)
        {
            HandleParallax();
        }

        // 3. Shooting stars timer
        if (enableShootingStars && Time.time >= nextShootingStarTime)
        {
            TriggerShootingStar();
            ScheduleNextShootingStar();
        }
    }

    private void HandleParallax()
    {
        Vector2 mousePos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            mousePos = Input.mousePosition;
        }
        catch { }
#endif

        float normX = (mousePos.x / Screen.width - 0.5f) * 2f;
        float normY = (mousePos.y / Screen.height - 0.5f) * 2f;

        Quaternion targetRot = initialCamRot * Quaternion.Euler(-normY * parallaxAmount, normX * parallaxAmount, 0f);
        Vector3 targetPos = initialCamPos + new Vector3(normX * 0.08f, normY * 0.05f, 0f);

        mainCamera.transform.localRotation = Quaternion.Slerp(mainCamera.transform.localRotation, targetRot, Time.deltaTime * parallaxSmoothSpeed);
        mainCamera.transform.localPosition = Vector3.Lerp(mainCamera.transform.localPosition, targetPos, Time.deltaTime * parallaxSmoothSpeed);
    }

    private void ScheduleNextShootingStar()
    {
        nextShootingStarTime = Time.time + Random.Range(minShootingStarInterval, maxShootingStarInterval);
    }

    public void SetupAmbientStars()
    {
        Transform existing = transform.Find("AmbientStarParticles");
        if (existing != null)
        {
            ambientStarsPS = existing.GetComponent<ParticleSystem>();
            if (ambientStarsPS != null)
            {
                var vel = ambientStarsPS.velocityOverLifetime;
                if (vel.enabled)
                {
                    vel.space = ParticleSystemSimulationSpace.Local;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
                    vel.y = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                }
            }
            return;
        }

        GameObject psObj = new GameObject("AmbientStarParticles");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = new Vector3(0f, 0f, starDepth);
        psObj.transform.localRotation = Quaternion.identity;

        ambientStarsPS = psObj.AddComponent<ParticleSystem>();
        var main = ambientStarsPS.main;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = maxStars;
        main.startLifetime = new ParticleSystem.MinMaxCurve(starLifetime * 0.7f, starLifetime * 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(starMinSize, starMaxSize);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        // Gradient colors: white, pale cyan, warm golden, gentle lilac
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 1f), 0.0f),
                new GradientColorKey(new Color(0.85f, 0.95f, 1f), 0.35f),
                new GradientColorKey(new Color(1f, 0.92f, 0.75f), 0.7f),
                new GradientColorKey(new Color(0.9f, 0.82f, 1f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(1f, 0.3f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        // Emission
        var emission = ambientStarsPS.emission;
        emission.rateOverTime = maxStars / (starLifetime * 0.8f);

        // Shape: Box covering screen view
        var shape = ambientStarsPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(starSpreadWidth, starSpreadHeight, starDepth * 0.6f);

        // Color over lifetime: smooth fade in, twinkle pulse, smooth fade out
        var colOverLifetime = ambientStarsPS.colorOverLifetime;
        colOverLifetime.enabled = true;
        Gradient alphaPulse = new Gradient();
        alphaPulse.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(0.4f, 0.2f),
                new GradientAlphaKey(1.0f, 0.5f), // twinkle peak
                new GradientAlphaKey(0.3f, 0.75f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        colOverLifetime.color = new ParticleSystem.MinMaxGradient(alphaPulse);

        // Size over lifetime: subtle twinkle pulsation
        var sizeOverLifetime = ambientStarsPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.1f);
        sizeCurve.AddKey(0.2f, 0.7f);
        sizeCurve.AddKey(0.45f, 1.0f); // bright pulse
        sizeCurve.AddKey(0.65f, 0.6f); // gentle dip
        sizeCurve.AddKey(0.85f, 0.9f); // secondary twinkle
        sizeCurve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Velocity over lifetime: gentle drift upward (all curves must share the same mode: TwoConstants)
        var velOverLifetime = ambientStarsPS.velocityOverLifetime;
        velOverLifetime.enabled = true;
        velOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        velOverLifetime.y = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        velOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Renderer
        var renderer = psObj.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && starMaterial != null)
        {
            renderer.sharedMaterial = starMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        ambientStarsPS.Play();
    }

    public void SetupShootingStars()
    {
        Transform existing = transform.Find("ShootingStarParticles");
        if (existing != null)
        {
            shootingStarsPS = existing.GetComponent<ParticleSystem>();
            return;
        }

        GameObject psObj = new GameObject("ShootingStarParticles");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = new Vector3(0f, 0f, 10f);
        psObj.transform.localRotation = Quaternion.identity;

        shootingStarsPS = psObj.AddComponent<ParticleSystem>();
        var main = shootingStarsPS.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = 10;
        main.startLifetime = 0.9f;
        main.startSpeed = 0f;
        main.startSize = 1.0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = shootingStarsPS.emission;
        emission.rateOverTime = 0; // Triggered programmatically via Emit

        var renderer = psObj.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && shootingStarMaterial != null)
        {
            renderer.sharedMaterial = shootingStarMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }

    public void TriggerShootingStar()
    {
        if (shootingStarsPS == null) return;

        // Random starting position in upper screen
        float startX = Random.Range(-starSpreadWidth * 0.45f, starSpreadWidth * 0.2f);
        float startY = Random.Range(starSpreadHeight * 0.15f, starSpreadHeight * 0.45f);
        float startZ = Random.Range(8f, 14f);

        // Diagonal downward angle (e.g. 25-35 degrees downward-right)
        float angleDeg = Random.Range(20f, 35f);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float speed = Random.Range(16f, 24f);

        Vector3 vel = new Vector3(Mathf.Cos(angleRad) * speed, -Mathf.Sin(angleRad) * speed, 0f);

        // Emit particle
        var emitParams = new ParticleSystem.EmitParams();
        emitParams.position = new Vector3(startX, startY, startZ);
        emitParams.velocity = vel;
        emitParams.startLifetime = Random.Range(0.7f, 1.1f);
        emitParams.startSize3D = new Vector3(Random.Range(2.5f, 4.0f), Random.Range(0.25f, 0.45f), 1f);
        emitParams.rotation = -angleDeg; // Aligns streak with trajectory
        emitParams.startColor = new Color(1f, 0.95f, 0.85f, 0.95f);

        shootingStarsPS.Emit(emitParams, 1);
    }
}
