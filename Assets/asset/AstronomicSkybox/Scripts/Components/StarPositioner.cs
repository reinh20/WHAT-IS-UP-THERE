using System;
using UnityEngine;

namespace Astronomy
{
    [ExecuteInEditMode]
    public class StarPositioner : MonoBehaviour
    {
        [Header("Rotators")]
        public Transform moonRotator;
        public Transform sunRotator;
        public Transform equatorialCoordinates;
        public Transform eclipticCoordinates;
        public Transform ambientCluster;
        [Header("Lighting")]
        public Material starMaterial;
        public Light sunLight;
        public Light moonLight;
        public Light[] ambientLights;
        [Tooltip("Maximum brightness of the sun")]
        public float sunIntensity = 1f;
        public float moonIntensity = 0.25f;
        public float ambientIntensity = 0.02f;
        public Color dayColor = new Color(1, 1, 1);
        public Color duskColor = new Color(1f, 0.9f, 0.8f);
        public Color dayFogColor;
        public Color nightFogColor;
        [Header("Location")]
        public float latitude = -6.2f;
        public float longitude = 106.8f;

        [Header("Night Only Mode")]
        [Tooltip("Force skybox and lighting to always remain at night")]
        public bool alwaysNight = true;

        private DateTime lastTime;

        private void OnValidate()
        {
            SetSky(lastTime = TimeManager.currentTime.ToUniversalTime());
        }

        private void Awake()
        {
            if (sunLight != null) RenderSettings.sun = sunLight;
            SetSky(lastTime = TimeManager.currentTime.ToUniversalTime());
        }

        private void Start()
        {
            if (sunLight != null) RenderSettings.sun = sunLight;
            SetSky(lastTime = TimeManager.currentTime.ToUniversalTime());
        }

        private void Update()
        {
            if (alwaysNight && sunLight != null && RenderSettings.sun != sunLight)
            {
                RenderSettings.sun = sunLight;
            }

            DateTime sampleTime = TimeManager.currentTime.ToUniversalTime();

            if(alwaysNight || lastTime != sampleTime)
                SetSky(lastTime = sampleTime);
        }

        public void SetSky(DateTime time)
        {
            // Rotations
            if (equatorialCoordinates && equatorialCoordinates.gameObject.activeSelf)
                equatorialCoordinates.rotation = StarMath.HorizontalFromEquatorial(0, 0, latitude, longitude, time);
            if (eclipticCoordinates && eclipticCoordinates.gameObject.activeSelf)
                eclipticCoordinates.rotation = StarMath.HorizontalFromEcliptic(0, 0, latitude, longitude, time);
            if(sunRotator && sunRotator.gameObject.activeSelf)
            {
                if (alwaysNight)
                {
                    // Strictly rotate sun below the horizon (-80 deg) so procedural skybox is pitch black night
                    sunRotator.rotation = Quaternion.Euler(-80f, sunRotator.rotation.eulerAngles.y, 0);
                }
                else
                {
                    sunRotator.rotation = StarMath.SolarPosition(time, latitude, longitude);
                }
            }
            if(moonRotator && moonRotator.gameObject.activeSelf)
            {
                moonRotator.rotation = StarMath.LunarPosition(time, latitude, longitude);
                if (alwaysNight)
                {
                    float moonAlt = Vector3.Angle(moonRotator.rotation * transform.forward, transform.up) - 90f;
                    if (moonAlt < 15f)
                    {
                        moonRotator.rotation = Quaternion.Euler(-45f, moonRotator.rotation.eulerAngles.y, 0);
                    }
                }
            }
            if(ambientCluster && ambientCluster.gameObject.activeSelf)
                ambientCluster.rotation = Quaternion.Euler(0, sunRotator.rotation.y, 0);

            // Sunlight
            if (sunRotator && sunLight)
            {
                float sunAlt = Vector3.Angle(sunLight.transform.rotation * transform.forward, transform.up) - 90f;
                float sunBrightness = sunIntensity;

                if (alwaysNight)
                {
                    sunBrightness = 0f;
                }
                else if (sunLight.gameObject.activeSelf)
                {
                    sunBrightness *= Mathf.Clamp01(sunAlt / 15f);
                }
                else
                {
                    sunBrightness = 0f;
                }

                sunLight.color = Color.Lerp(duskColor, dayColor, Mathf.Clamp01(sunBrightness / 0.5f));
                sunLight.intensity = sunBrightness;

                // Child SkyboxLight
                var skyboxLight = sunLight.transform.Find("SkyboxLight");
                if (skyboxLight != null)
                {
                    var sl = skyboxLight.GetComponent<Light>();
                    if (sl != null) sl.intensity = alwaysNight ? 0f : sunBrightness * 2f;
                }

                if (RenderSettings.fog)
                    RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, Mathf.Clamp01(sunBrightness / 0.5f));

                // Moonlight
                if (moonLight)
                {
                    float moonAlt = Vector3.Angle(moonRotator.rotation * transform.forward, transform.up) - 90f;
                    float mFactor = alwaysNight ? 1f : (1f - sunBrightness / Mathf.Max(0.01f, sunIntensity));
                    moonLight.intensity = mFactor * Mathf.Clamp01((-moonAlt + 5f) / 5f) * moonIntensity;
                }

                // Starlight
                if (starMaterial)
                {
                    float starScale = alwaysNight ? 1f : Mathf.Pow(1f - Mathf.Clamp01(sunBrightness * 10), 2);
                    starMaterial.SetFloat("_Scale", starScale);
                }

                // AmbientLight
                if (ambientLights.Length != 0)
                {
                    float ambintensity = Mathf.Pow(Mathf.Clamp01((sunAlt + 12f) / 8f), 2f) * ambientIntensity;
                    if (alwaysNight) ambintensity = ambientIntensity * 0.2f;
                    for (int i = 0; i < ambientLights.Length; i++)
                        ambientLights[i].intensity = ambintensity * (i == 0 ? 2 : 1);
                }
            }
        }
    }
}
