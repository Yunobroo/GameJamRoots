using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class CartoonLook : MonoBehaviour
{
    [Serializable] public class Surface
    {
        public Renderer renderer;
        public Material[] original;
        public Material[] cartoon;
    }
    [Serializable] public class Lamp
    {
        public Light light;
        public Color color;
        public float intensity;
        public float shadowStrength;
        public LightShadows shadows;
    }
    [HideInInspector] public Surface[] surfaces = Array.Empty<Surface>();
    [HideInInspector] public Lamp[] lamps = Array.Empty<Lamp>();
    [HideInInspector] public Color originalAmbient;
    [HideInInspector] public AmbientMode originalAmbientMode;
    [HideInInspector] public float originalAmbientIntensity;
    [HideInInspector] public bool initialized;

    private void OnEnable() { if (initialized) Apply(true); }
    private void OnDisable() { if (initialized) Apply(false); }

    public void Apply(bool cartoon)
    {
        foreach (Surface surface in surfaces)
            if (surface.renderer != null)
                surface.renderer.sharedMaterials = cartoon ? surface.cartoon : surface.original;
        foreach (Lamp lamp in lamps)
        {
            if (lamp.light == null) continue;
            lamp.light.color = cartoon ? Color.Lerp(lamp.color, new Color(1f, 0.92f, 0.79f), 0.25f) : lamp.color;
            lamp.light.intensity = cartoon ? lamp.intensity * 0.85f : lamp.intensity;
            lamp.light.shadowStrength = cartoon ? 0.55f : lamp.shadowStrength;
            lamp.light.shadows = cartoon && lamp.shadows != LightShadows.None ? LightShadows.Soft : lamp.shadows;
        }
        RenderSettings.ambientMode = cartoon ? AmbientMode.Flat : originalAmbientMode;
        RenderSettings.ambientLight = cartoon ? new Color(0.48f, 0.55f, 0.68f) : originalAmbient;
        RenderSettings.ambientIntensity = cartoon ? 1f : originalAmbientIntensity;
    }
}
