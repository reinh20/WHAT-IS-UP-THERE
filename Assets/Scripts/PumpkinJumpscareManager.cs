using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coordinates the jumpscare sequence when the player is caught by a red pumpkin,
/// including pumpkin face lunge, expanding blinding red light consuming the screen,
/// camera shake, audio sting, and smooth player respawn at the active checkpoint.
/// </summary>
public class PumpkinJumpscareManager : MonoBehaviour
{
    private static PumpkinJumpscareManager _instance;
    public static PumpkinJumpscareManager Instance => _instance;

    [Header("Jumpscare Settings")]
    [Tooltip("Total duration of the jumpscare before respawning the player")]
    public float jumpscareDuration = 1.8f;
    [Tooltip("Delay in seconds while screen is obscured before respawning player at checkpoint")]
    public float deathRespawnDelay = 3.0f;
    [Tooltip("Duration of the fade-out from red light back to normal view after respawn")]
    public float fadeOutDuration = 0.6f;
    [Tooltip("Intensity of the violent camera shake during jumpscare")]
    public float cameraShakeIntensity = 0.35f;

    [Header("Blinding Red Light ('Light Consuming Player')")]
    [Tooltip("Dedicated point light used for the blinding screen flash. Created automatically if null.")]
    public Light jumpscareLight;
    public Color jumpscareLightColor = new Color(1.0f, 0.02f, 0.02f, 1.0f);
    public float peakLightIntensity = 35000f;
    public float peakLightRange = 40f;

    [Header("Audio")]
    [Tooltip("Optional horror audio clip. If null, a procedural horror sting is generated.")]
    public AudioClip jumpscareAudioClip;
    [Range(0f, 1f)] public float jumpscareAudioVolume = 1.0f;
    private AudioSource _audioSource;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Canvas jumpscareCanvas;
    [SerializeField] private Image screenRedOverlay;

    private bool _isJumpscareActive = false;
    private GameObject _jumpscarePumpkinInstance;

    public bool IsJumpscareActive => _isJumpscareActive;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        FindPlayerAndCamera();
        EnsureJumpscareCanvas();
        EnsureJumpscareLight();
        EnsureAudioSource();
    }

    private void FindPlayerAndCamera()
    {
        if (playerMovement == null)
        {
            playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null) playerMovement = p.GetComponent<PlayerMovement>();
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null && playerMovement != null)
            {
                playerCamera = playerMovement.GetComponentInChildren<Camera>();
            }
        }
    }

    private void EnsureJumpscareCanvas()
    {
        if (screenRedOverlay != null) return;

        // Try finding existing HUD_Canvas
        GameObject canvasGo = GameObject.Find("HUD_Canvas");
        if (canvasGo != null)
        {
            jumpscareCanvas = canvasGo.GetComponent<Canvas>();
        }

        if (jumpscareCanvas == null)
        {
            GameObject newCanvasGo = new GameObject("PumpkinJumpscare_Canvas");
            jumpscareCanvas = newCanvasGo.AddComponent<Canvas>();
            jumpscareCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            jumpscareCanvas.sortingOrder = 999; // Top-most overlay
            newCanvasGo.AddComponent<CanvasScaler>();
            newCanvasGo.AddComponent<GraphicRaycaster>();
        }

        Transform overlayTr = jumpscareCanvas.transform.Find("RedScreenOverlay");
        if (overlayTr != null)
        {
            screenRedOverlay = overlayTr.GetComponent<Image>();
        }
        else
        {
            GameObject overlayGo = new GameObject("RedScreenOverlay");
            overlayGo.transform.SetParent(jumpscareCanvas.transform, false);
            screenRedOverlay = overlayGo.AddComponent<Image>();
            screenRedOverlay.color = new Color(0.95f, 0.05f, 0.02f, 0.0f);
            screenRedOverlay.raycastTarget = false;

            RectTransform rt = overlayGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
    }

    private void EnsureJumpscareLight()
    {
        if (jumpscareLight != null) return;

        Transform camTr = (playerCamera != null) ? playerCamera.transform : transform;
        Transform existingLight = camTr.Find("Jumpscare_RedLight");
        if (existingLight != null)
        {
            jumpscareLight = existingLight.GetComponent<Light>();
        }
        else
        {
            GameObject lightGo = new GameObject("Jumpscare_RedLight");
            lightGo.transform.SetParent(camTr, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, 0.5f);

            jumpscareLight = lightGo.AddComponent<Light>();
            jumpscareLight.type = LightType.Point;
            jumpscareLight.color = jumpscareLightColor;
            jumpscareLight.intensity = 0f;
            jumpscareLight.range = 5f;
            jumpscareLight.shadows = LightShadows.None;
            jumpscareLight.enabled = false;
        }
    }

    private void EnsureAudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D Full stereo horror sound
        }

        if (jumpscareAudioClip == null)
        {
            jumpscareAudioClip = GenerateHorrorAudioClip();
        }
    }

    /// <summary>
    /// Generates a terrifying deep bass rumble with rising discordant horror screech
    /// so jumpscare always has sound without external asset dependencies.
    /// </summary>
    private AudioClip GenerateHorrorAudioClip()
    {
        int sampleRate = 44100;
        float duration = 1.6f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 2.2f); // Sharp decay envelope

            // Sub bass impact (55Hz - 80Hz)
            float bass = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.7f;

            // Harsh distorted screech (dissonant frequencies)
            float f1 = Mathf.Lerp(300f, 950f, Mathf.Sqrt(t / duration));
            float f2 = f1 * 1.414f; // Tritone
            float screech = (Mathf.Sin(2f * Mathf.PI * f1 * t) + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.7f) * 0.35f;

            // White noise crunch
            float noise = (Random.value * 2f - 1f) * 0.2f;

            samples[i] = Mathf.Clamp((bass + screech + noise) * env, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Procedural_JumpscareSting", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Triggers the full jumpscare sequence. Called by PumpkinWatcher when player is detected in red phase.
    /// </summary>
    public void TriggerJumpscare(PumpkinWatcher attackingPumpkin, GameObject caughtPlayer = null)
    {
        if (_isJumpscareActive || FinishLine.IsGameFinished) return;

        StartCoroutine(ExecuteJumpscareRoutine(attackingPumpkin, attackingPumpkin != null ? attackingPumpkin.gameObject : null));
    }

    /// <summary>
    /// Triggers the full jumpscare sequence from any attacking GameObject (e.g. PumpkinChaser).
    /// </summary>
    public void TriggerJumpscare(GameObject attackingObject, GameObject caughtPlayer = null)
    {
        if (_isJumpscareActive || FinishLine.IsGameFinished) return;

        PumpkinWatcher pw = attackingObject != null ? attackingObject.GetComponent<PumpkinWatcher>() : null;
        StartCoroutine(ExecuteJumpscareRoutine(pw, attackingObject));
    }

    private IEnumerator ExecuteJumpscareRoutine(PumpkinWatcher pumpkin, GameObject attackerObj = null)
    {
        _isJumpscareActive = true;
        FindPlayerAndCamera();

        // 1. Lock player movement, silence audio, and stop physics
        if (playerMovement != null)
        {
            playerMovement.StopAllAudio();
            playerMovement.enabled = false;
            Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }
        }

        // 2. Play horror audio (PumpkinChaser can override with its own clip)
        AudioClip clipToPlay = jumpscareAudioClip;
        float volumeToPlay = jumpscareAudioVolume;
        PumpkinChaser chaser = attackerObj != null ? attackerObj.GetComponent<PumpkinChaser>() : null;
        if (chaser != null && chaser.jumpscareAudioClip != null)
        {
            clipToPlay = chaser.jumpscareAudioClip;
            volumeToPlay = chaser.jumpscareAudioVolume;
        }

        if (_audioSource != null && clipToPlay != null)
        {
            _audioSource.PlayOneShot(clipToPlay, volumeToPlay);
        }

        // 3. Spawn/Position Jumpscare Pumpkin head lunging towards camera
        Transform camTr = (playerCamera != null) ? playerCamera.transform : transform;
        Vector3 camOriginalLocalPos = camTr.localPosition;
        Quaternion camOriginalLocalRot = camTr.localRotation;

        SpawnJumpscarePumpkinFace(pumpkin, attackerObj, camTr);

        // Turn on expanding jumpscare light
        if (jumpscareLight != null)
        {
            jumpscareLight.enabled = true;
            jumpscareLight.color = jumpscareLightColor;
        }

        // 4. Animate light expansion, pumpkin lunging, and red screen consuming player
        float elapsed = 0.0f;
        while (elapsed < jumpscareDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / jumpscareDuration);

            // Light expands exponentially to consume the player's view
            float lightCurve = Mathf.Pow(progress, 1.8f);
            if (jumpscareLight != null)
            {
                jumpscareLight.intensity = Mathf.Lerp(100f, peakLightIntensity, lightCurve);
                jumpscareLight.range = Mathf.Lerp(4f, peakLightRange, progress);
            }

            // Screen red overlay consumes the screen
            if (screenRedOverlay != null)
            {
                // Curves from 0 to 1, reaching pure solid red at the peak
                float alpha = Mathf.Clamp01(Mathf.Pow(progress, 1.4f) * 1.35f);
                screenRedOverlay.color = new Color(0.95f, 0.03f, 0.02f, alpha);
            }

            // Lunge jumpscare pumpkin closer to player's face
            if (_jumpscarePumpkinInstance != null)
            {
                // Lerp from 2.5m away to 0.65m right in front of camera
                float lunge = Mathf.SmoothStep(0f, 1f, progress);
                Vector3 targetLocalPos = new Vector3(0f, -0.1f, Mathf.Lerp(2.5f, 0.65f, lunge));
                _jumpscarePumpkinInstance.transform.localPosition = targetLocalPos;

                // Violent twitching/jitter
                Vector3 jitter = new Vector3(
                    (Random.value - 0.5f) * 0.08f,
                    (Random.value - 0.5f) * 0.08f,
                    0f
                );
                _jumpscarePumpkinInstance.transform.localPosition += jitter;
            }

            // Violent camera shake (applied to local position so parent offsets are preserved)
            if (playerCamera != null)
            {
                float shakeMag = cameraShakeIntensity * (1f - progress * 0.4f);
                Vector3 shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 45f, 0f) - 0.5f) * shakeMag,
                    (Mathf.PerlinNoise(0f, Time.time * 45f) - 0.5f) * shakeMag,
                    0f
                );
                playerCamera.transform.localPosition = camOriginalLocalPos + shakeOffset;
            }

            yield return null;
        }

        // Screen is now 100% consumed by red light
        if (screenRedOverlay != null)
        {
            screenRedOverlay.color = new Color(0.95f, 0.03f, 0.02f, 1.0f);
        }

        // Restore camera local transform cleanly while screen is obscured
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = camOriginalLocalPos;
            playerCamera.transform.localRotation = camOriginalLocalRot;
        }

        // Destroy jumpscare face model while screen is completely covered
        if (_jumpscarePumpkinInstance != null)
        {
            Destroy(_jumpscarePumpkinInstance);
            _jumpscarePumpkinInstance = null;
        }

        // Hold on covered screen for the specified delay (3 seconds) before respawning
        yield return new WaitForSeconds(deathRespawnDelay);

        // 5. RESPAWN PLAYER AT LAST ACTIVE CHECKPOINT
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer();
        }
        else if (playerMovement != null)
        {
            // Fallback: reset velocity
            Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
            }
        }

        // Reset all pumpkins back to initial safe orange state
        ResetAllPumpkins();

        // 6. Smooth fade-out from blinding red back to game view
        elapsed = 0.0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1.0f - Mathf.Clamp01(elapsed / fadeOutDuration);

            if (screenRedOverlay != null)
            {
                screenRedOverlay.color = new Color(0.95f, 0.03f, 0.02f, t);
            }

            if (jumpscareLight != null)
            {
                jumpscareLight.intensity = peakLightIntensity * t;
            }

            yield return null;
        }

        // Turn off overlay and jumpscare light completely
        if (screenRedOverlay != null)
        {
            screenRedOverlay.color = new Color(0.95f, 0.03f, 0.02f, 0.0f);
        }
        if (jumpscareLight != null)
        {
            jumpscareLight.enabled = false;
            jumpscareLight.intensity = 0f;
        }

        // Re-enable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        _isJumpscareActive = false;
        Debug.Log("[PumpkinJumpscareManager] Jumpscare complete! Player respawned and ready.");
    }

    private void SpawnJumpscarePumpkinFace(PumpkinWatcher sourcePumpkin, GameObject attackerObj, Transform cameraTransform)
    {
        if (cameraTransform == null) return;

        // Create jumpscare child object attached to camera
        _jumpscarePumpkinInstance = new GameObject("Jumpscare_PumpkinFace");
        _jumpscarePumpkinInstance.transform.SetParent(cameraTransform, false);
        _jumpscarePumpkinInstance.transform.localPosition = new Vector3(0f, -0.1f, 2.5f);
        _jumpscarePumpkinInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Face towards camera
        _jumpscarePumpkinInstance.transform.localScale = Vector3.one * 0.45f;

        // Copy pumpkin mesh and materials from watcher, chaser, or any pumpkin in scene
        MeshFilter sourceMf = (sourcePumpkin != null) 
            ? sourcePumpkin.GetComponentInChildren<MeshFilter>() 
            : (attackerObj != null ? attackerObj.GetComponentInChildren<MeshFilter>() : null);

        if (sourceMf == null)
        {
            var anyWatcher = FindAnyObjectByType<PumpkinWatcher>();
            if (anyWatcher != null) sourceMf = anyWatcher.GetComponentInChildren<MeshFilter>();
        }

        Renderer sourceRenderer = (sourcePumpkin != null && sourcePumpkin.pumpkinRenderer != null) 
            ? sourcePumpkin.pumpkinRenderer 
            : (attackerObj != null ? attackerObj.GetComponentInChildren<Renderer>() : null);

        if (sourceRenderer == null && sourceMf != null)
        {
            sourceRenderer = sourceMf.GetComponent<Renderer>();
        }

        if (sourceMf != null && sourceMf.sharedMesh != null)
        {
            MeshFilter mf = _jumpscarePumpkinInstance.AddComponent<MeshFilter>();
            mf.sharedMesh = sourceMf.sharedMesh;

            MeshRenderer mr = _jumpscarePumpkinInstance.AddComponent<MeshRenderer>();
            if (sourceRenderer != null)
            {
                Material[] copiedMats = new Material[sourceRenderer.sharedMaterials.Length];
                for (int i = 0; i < sourceRenderer.sharedMaterials.Length; i++)
                {
                    Material srcMat = sourceRenderer.sharedMaterials[i];
                    if (srcMat != null)
                    {
                        Material instMat = new Material(srcMat);
                        // Make eyes/face blazing blood red
                        if (i == 1 || srcMat.name.Contains("002"))
                        {
                            instMat.EnableKeyword("_EMISSION");
                            if (instMat.HasProperty("_BaseColor"))
                                instMat.SetColor("_BaseColor", new Color(1.0f, 0.02f, 0.02f, 1.0f));
                            if (instMat.HasProperty("_EmissionColor"))
                                instMat.SetColor("_EmissionColor", new Color(5.0f, 0.2f, 0.2f, 1.0f));
                        }
                        copiedMats[i] = instMat;
                    }
                }
                mr.materials = copiedMats;
            }
        }
    }

    private void ResetAllPumpkins()
    {
        PumpkinWatcher[] allPumpkins = FindObjectsByType<PumpkinWatcher>(FindObjectsInactive.Include);
        foreach (PumpkinWatcher pw in allPumpkins)
        {
            pw.ResetToInitialState();
        }
    }
}
