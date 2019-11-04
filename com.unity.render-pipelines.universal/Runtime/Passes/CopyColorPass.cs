using System;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Copy the given color buffer to the given destination color buffer.
    ///
    /// You can use this pass to copy a color buffer to the destination,
    /// so you can use it later in rendering. For example, you can copy
    /// the opaque texture to use it for distortion effects.
    /// </summary>
    public class CopyColorPass : ScriptableRenderPass
    {
        int m_SampleOffsetShaderHandle;
        Material m_SamplingMaterial;
        Downsampling m_DownsamplingMethod;
        Material m_CopyColorMaterial;

        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle destination { get; set; }
        const string m_ProfilerTag = "Copy Color";

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public CopyColorPass(RenderPassEvent evt, Material samplingMaterial, Material copyColorMaterial)
        {
            m_SamplingMaterial = samplingMaterial;
            m_CopyColorMaterial = copyColorMaterial;
            m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
            renderPassEvent = evt;
            m_DownsamplingMethod = Downsampling.None;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Downsampling downsampling)
        {
            this.source = source;
            this.destination = destination;
            m_DownsamplingMethod = downsampling;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
        {
            RenderTextureDescriptor descriptor = cameraTextureDescripor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            if (m_DownsamplingMethod == Downsampling._2xBilinear)
            {
                descriptor.width /= 2;
                descriptor.height /= 2;
            }
            else if (m_DownsamplingMethod == Downsampling._4xBox || m_DownsamplingMethod == Downsampling._4xBilinear)
            {
                descriptor.width /= 4;
                descriptor.height /= 4;
            }

            cmd.GetTemporaryRT(destination.id, descriptor, m_DownsamplingMethod == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_SamplingMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_SamplingMaterial, GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            RenderTargetIdentifier opaqueColorRT = destination.Identifier();

            // Render the lut
            if (URPCameraMode.isPureURP)
            {
                Matrix4x4 projMatrix;
                Matrix4x4 viewMatrix;
                projMatrix = GL.GetGPUProjectionMatrix(Matrix4x4.identity, true);
                viewMatrix = Matrix4x4.identity;
                Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
                Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_ViewMatrix"), viewMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewMatrix"), Matrix4x4.Inverse(viewMatrix));
                cmd.SetGlobalMatrix(Shader.PropertyToID("_ProjMatrix"), projMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_InvProjMatrix"), Matrix4x4.Inverse(projMatrix));
                cmd.SetGlobalMatrix(Shader.PropertyToID("_ViewProjMatrix"), viewProjMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewProjMatrix"), Matrix4x4.Inverse(viewProjMatrix));

                switch (m_DownsamplingMethod)
                {
                    case Downsampling.None:
                        CoreUtils.SetRenderTarget(cmd, opaqueColorRT);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyColorMaterial);
                        break;
                    case Downsampling._2xBilinear:
                        CoreUtils.SetRenderTarget(cmd, opaqueColorRT);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyColorMaterial);
                        break;
                    case Downsampling._4xBox:
                        CoreUtils.SetRenderTarget(cmd, opaqueColorRT);
                        m_SamplingMaterial.SetFloat(m_SampleOffsetShaderHandle, 2);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_SamplingMaterial);
                        break;
                    case Downsampling._4xBilinear:
                        CoreUtils.SetRenderTarget(cmd, opaqueColorRT);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyColorMaterial);
                        break;
                }
            }
            else
            {
                switch (m_DownsamplingMethod)
                {
                    case Downsampling.None:
                        Blit(cmd, source, opaqueColorRT);
                        break;
                    case Downsampling._2xBilinear:
                        Blit(cmd, source, opaqueColorRT);
                        break;
                    case Downsampling._4xBox:
                        m_SamplingMaterial.SetFloat(m_SampleOffsetShaderHandle, 2);
                        Blit(cmd, source, opaqueColorRT, m_SamplingMaterial);
                        break;
                    case Downsampling._4xBilinear:
                        Blit(cmd, source, opaqueColorRT);
                        break;
                }
            }
           
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (destination != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(destination.id);
                destination = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
