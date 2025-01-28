using System;
using LCVR.Assets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LCVR.Rendering
{
    [Serializable, VolumeComponentMenu("Post-processing/Custom/Vignette")]
    public sealed class Vignette : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        private static readonly int VignetteIntensity = Shader.PropertyToID("_VignetteIntensity");
        private static readonly int VignetteSoftness = Shader.PropertyToID("_VignetteSoftness");
        private static readonly int VignetteColor = Shader.PropertyToID("_VignetteColor");
        
        public ClampedFloatParameter intensity = new(0.0f, 0, 1);
        public ClampedFloatParameter softness = new(0.0f, 0, 1);
        public ColorParameter color = new(Color.black);

        private Material m_Material;

        public bool IsActive() => m_Material != null && intensity.value > 0;

        public override CustomPostProcessInjectionPoint injectionPoint =>
            CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup()
        {
            m_Material = new Material(AssetManager.VignettePostProcess);
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            if (m_Material == null)
                return;
            
            m_Material.SetFloat(VignetteIntensity, intensity.value);
            m_Material.SetFloat(VignetteSoftness, softness.value);
            m_Material.SetColor(VignetteColor, color.value);
            
            cmd.Blit(source, destination, m_Material, 0);
        }

        public override void Cleanup() => CoreUtils.Destroy(m_Material);
    }
}