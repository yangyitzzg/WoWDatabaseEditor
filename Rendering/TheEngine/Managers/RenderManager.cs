﻿using System.Diagnostics;
using OpenGLBindings;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Config;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Primitives;
using TheEngine.Structures;
using TheMaths;

namespace TheEngine.Managers
{
    public class SimpleGrid<T> where T : new()
    {
        private readonly int cellSize;
        private Dictionary<(int, int), T> grid = new();
        
        public SimpleGrid(int cellSize)
        {
            this.cellSize = cellSize;
        }

        private (int, int) GetKey(float x, float z)
        {
            return (((int)(x + cellSize / 2)) / cellSize, ((int)(z + cellSize / 2)) / cellSize);
        }
        
        public bool TryGet(float x, float z, out T? res)
        {
            return grid.TryGetValue(GetKey(x, z), out res);
        }

        public T Get(float x, float z)
        {
            if (grid.TryGetValue(GetKey(x, z), out var r))
                return r;
            return grid[(GetKey(x, z))] = new();
        }

        public IEnumerable<T> GetAround(float x, float z, int distance)
        {
            int minX = (int)(x - distance);
            int maxX = (int)(x + distance);
            int minZ = (int)(z - distance);
            int maxZ = (int)(z + distance);

            var minCell = GetKey(minX, minZ);
            var maxCell = GetKey(maxX, maxZ);

            for (int i = minCell.Item1; i < maxCell.Item1; ++i)
            {
                for (int j = minCell.Item2; j < maxCell.Item2; ++j)
                {
                    if (grid.TryGetValue((i, j), out var r))
                        yield return r;
                }
            }
        }
    }
    
    public class RenderManager : IRenderManager, IDisposable
    {
        private readonly Engine engine;

        private NativeBuffer<SceneBuffer> sceneBuffer;
        private NativeBuffer<ObjectBuffer> objectBuffer;
        //private NativeBuffer<PixelShaderSceneBuffer> pixelShaderSceneBuffer;
        private NativeBuffer<Matrix> instancesBuffer;
        private NativeBuffer<Matrix> instancesInverseBuffer;

        private ObjectBuffer objectData;
        private SceneBuffer sceneData;
        //private PixelShaderSceneBuffer scenePixelData;
        private Matrix[] instancesArray;
        private Matrix[] inverseInstancesArray;

        private ICameraManager cameraManager;
        
        private Dictionary<ShaderHandle, Dictionary<Material, Dictionary<(Mesh mesh, int subMesh), List<Transform>>>> dynamicRenderers;
        private SimpleGrid<Dictionary<ShaderHandle,
            Dictionary<Material, Dictionary<(Mesh mesh, int subMesh), List<Transform>>>>> staticRenderers;

        private Sampler defaultSampler;

        private DepthStencil depthStencilZWrite;
        private DepthStencil depthStencilNoZWrite;

        private bool? currentDepthTest;
        private bool? currentZwrite;
        private CullingMode? currentCulling;
        private (bool enabled, Blending? source, Blending? dest)? currentBlending;

        private RenderTexture renderTexture;

        private RenderTexture outlineTexture;

        private IMesh planeMesh;

        private ShaderHandle blitShader;

        private Material blitMaterial;

        private Material unlitMaterial;

        private Mesh currentMesh = null;

        private Shader currentShader = null;

        private BoundingFrustum culler = new BoundingFrustum();
        
        internal RenderManager(Engine engine)
        {
            this.engine = engine;

            cameraManager = engine.CameraManager;

            sceneBuffer = engine.Device.CreateBuffer<SceneBuffer>(BufferTypeEnum.ConstVertex, 1);
            objectBuffer = engine.Device.CreateBuffer<ObjectBuffer>(BufferTypeEnum.ConstVertex, 1);
            //pixelShaderSceneBuffer = engine.Device.CreateBuffer<PixelShaderSceneBuffer>(BufferTypeEnum.ConstPixel, 1);
            instancesBuffer = engine.Device.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1);
            instancesInverseBuffer = engine.Device.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1);
            engine.Device.device.CheckError("created buffers");

            instancesArray = new Matrix[1];
            inverseInstancesArray = new Matrix[1];

            dynamicRenderers = new Dictionary<ShaderHandle, Dictionary<Material, Dictionary<(Mesh, int), List<Transform>>>>();
            staticRenderers =
                new SimpleGrid<Dictionary<ShaderHandle,
                    Dictionary<Material, Dictionary<(Mesh mesh, int subMesh), List<Transform>>>>>(500);

            sceneData = new SceneBuffer();

            defaultSampler = engine.Device.CreateSampler();
            engine.Device.device.CheckError("create sampler");

            depthStencilZWrite = engine.Device.CreateDepthStencilState(true);
            depthStencilNoZWrite = engine.Device.CreateDepthStencilState(false);
            engine.Device.device.CheckError("create depth stencil");

            renderTexture = engine.Device.CreateRenderTexture((int)engine.WindowHost.WindowWidth, (int)engine.WindowHost.WindowHeight);
            engine.Device.device.CheckError("create render tex");

            //outlineTexture = engine.Device.CreateRenderTexture((int)engine.WindowHost.WindowWidth, (int)engine.WindowHost.WindowHeight);

            planeMesh = engine.MeshManager.CreateMesh(in ScreenPlane.Instance);
            engine.Device.device.CheckError("create mesh");

            blitShader = engine.ShaderManager.LoadShader("data/blit.json");
            engine.Device.device.CheckError("load shader");
            blitMaterial = engine.MaterialManager.CreateMaterial(blitShader);

            //unlitMaterial = engine.MaterialManager.CreateMaterial(engine.ShaderManager.LoadShader("../internalShaders/unlit.shader"));
        }

        public void Dispose()
        {
            //outlineTexture.Dispose();
            engine.meshManager.DisposeMesh(planeMesh);
            renderTexture.Dispose();

            depthStencilZWrite.Dispose();
            depthStencilNoZWrite.Dispose();
            defaultSampler.Dispose();
            instancesInverseBuffer.Dispose();
            instancesBuffer.Dispose();
            //pixelShaderSceneBuffer.Dispose();
            objectBuffer.Dispose();
            sceneBuffer.Dispose();
        }
        
        private void SetBlending(bool enabled, Blending source, Blending dest)
        {
            if (currentBlending.HasValue && currentBlending.Value.enabled == enabled &&
                currentBlending.Value.source == source && currentBlending.Value.dest == dest) 
                return;

            if (currentBlending.HasValue && currentBlending.Value.enabled == enabled)
            {
                engine.Device.device.BlendFunc((BlendingFactorSrc)source, ((BlendingFactorDest)dest));
                currentBlending = (enabled, source, dest);
            }
            else if (enabled)
            {
                engine.Device.device.Enable(EnableCap.Blend);
                engine.Device.device.BlendFunc((BlendingFactorSrc)source, ((BlendingFactorDest)dest));
                currentBlending = (enabled, source, dest);
            }
            else
            {
                engine.Device.device.Disable(EnableCap.Blend);
                currentBlending = (enabled, null, null);
            }
        }

        private void SetCulling(CullingMode culling)
        {
            if (!currentCulling.HasValue || currentCulling.Value != culling)
            {
                if (culling == CullingMode.Off)
                {
                    engine.Device.device.Disable(EnableCap.CullFace);
                }
                else
                {
                    if (!currentCulling.HasValue || currentCulling == CullingMode.Off)
                        engine.Device.device.Enable(EnableCap.CullFace);
                    engine.Device.device.CullFace(currentCulling == CullingMode.Front ? CullFaceMode.Front : CullFaceMode.Back);
                }
                currentCulling = culling;
            }
        }
        
        private void SetZWrite(bool zwrite)
        {
            if (!currentZwrite.HasValue || currentZwrite.Value != zwrite)
            {
                if (zwrite)
                    depthStencilZWrite.Activate();
                else
                    depthStencilNoZWrite.Activate();
                currentZwrite = zwrite;
            }
        }

        private void SetDepthTest(bool depthTest)
        {
            if (!currentDepthTest.HasValue || currentDepthTest.Value != depthTest)
            {
                if (depthTest)
                    engine.Device.device.Enable(EnableCap.DepthTest);
                else
                    engine.Device.device.Disable(EnableCap.DepthTest);
                currentDepthTest = depthTest;
            }
        }

        public void BeginFrame()
        {
            // better not assume state was saved from the previous frame...
            currentCulling = null;
            currentZwrite = null;
            currentDepthTest = null;
            currentBlending = null;
            cameraManager.MainCamera.Aspect = engine.WindowHost.Aspect;
        }

        private bool inRenderingLoop = false;
        public void PrepareRendering(int dstFrameBuffer)
        {
            inRenderingLoop = true;
            engine.Device.device.CheckError("pre UpdateSceneBuffer");
            UpdateSceneBuffer();
            
            Stats = default;
            engine.Device.device.CheckError("Render begin");
            
            engine.Device.RenderClearBuffer();

            if (renderTexture.Width != (int)engine.WindowHost.WindowWidth ||
                renderTexture.Height != (int)engine.WindowHost.WindowHeight)
            {
                renderTexture.Dispose();
                renderTexture = engine.Device.CreateRenderTexture((int)engine.WindowHost.WindowWidth, (int)engine.WindowHost.WindowHeight);
            }
            
            engine.Device.SetRenderTexture(renderTexture, dstFrameBuffer);
            renderTexture.Clear(1, 1, 1, 1);

            sceneBuffer.UpdateBuffer(ref sceneData);
            sceneBuffer.Activate(Constants.SCENE_BUFFER_INDEX);

            //pixelShaderSceneBuffer.UpdateBuffer(ref scenePixelData);
            //pixelShaderSceneBuffer.Activate(Constants.PIXEL_SCENE_BUFFER_INDEX);

            objectBuffer.Activate(Constants.OBJECT_BUFFER_INDEX);
        }

        public void FinalizeRendering(int dstFrameBuffer)
        {
            engine.Device.SetRenderTexture(null, dstFrameBuffer);

            engine.Device.device.CheckError("Blitz");
            blitMaterial.Shader.Activate();
            SetZWrite(false);
            renderTexture.Activate(0);
            //outlineTexture.Activate(1);
            engine.meshManager.GetMeshByHandle(planeMesh.Handle).Activate();
            engine.Device.DrawIndexed(engine.meshManager.GetMeshByHandle(planeMesh.Handle).IndexCount(0), 0, 0);

            inRenderingLoop = false;
        }

        public struct RenderStats
        {
            public int ShaderSwitches = 0;
            public int MaterialActivations = 0;
            public int MeshSwitches = 0;
            public int InstancedDraws = 0;
            public int NonInstancedDraws = 0;
            public int InstancedDrawSaved = 0;
            public int TrianglesDrawn = 0;
            public int IndicesDrawn = 0;
        }

        public RenderStats Stats;
        
        internal void RenderWorld(int dstFrameBuffer)
        {
            //culler.Update(cameraManager.MainCamera);

            //defaultSampler.Activate(Constants.DEFAULT_SAMPLER);

            engine.Device.device.CheckError("Before render all");
            RenderAll(null);

            //engine.Device.RenderClearBuffer();
            //engine.Device.SetRenderTexture(outlineTexture);
            //outlineTexture.Clear(0, 0, 0, 0);

            //RenderAll(unlitMaterial);
            //engine.Device.RenderBlitBuffer();
        }

        private void RenderAll(Material overrideMaterial)
        {
            currentMesh = null;
            currentShader = null;

            RenderRenderers(overrideMaterial, dynamicRenderers, float.MaxValue);
            var cameraPosition = cameraManager.MainCamera.Transform.Position;
            foreach (var renderers in staticRenderers.GetAround(cameraPosition.X, cameraPosition.Z, 1500))
            {
                RenderRenderers(overrideMaterial, renderers, 500);
            }

            /*tatsString = @"ShaderSwitches = " + Stats.ShaderSwitches + @"
MaterialActivations = " + Stats.MaterialActivations + @"
MeshSwitches = " + Stats.MeshSwitches + @"
InstancedDraws = " + Stats.InstancedDraws + @"
NonInstancedDraws = " + Stats.NonInstancedDraws + @"
InstancedDrawSaved = " + Stats.InstancedDrawSaved + @"
TrianglesDrawn = " + Stats.TrianglesDrawn + @"
IndicesDrawn = " + Stats.IndicesDrawn;*/
        }

        public string StatsString;

        private void EnableMaterial(Material material)
        {
            SetZWrite(material.Shader.ZWrite);
            SetDepthTest(material.Shader.DepthTest);
            SetCulling(material.Culling);
            SetBlending(false, Blending.One, Blending.Zero);
            material.ActivateUniforms();
            Stats.MaterialActivations++;
        }

        private void RenderRenderers(Material? overrideMaterial, 
            Dictionary<ShaderHandle, Dictionary<Material, Dictionary<(Mesh mesh, int subMesh), List<Transform>>>> renderers,
            float drawDistance)
        {
            objectBuffer.Activate(Constants.OBJECT_BUFFER_INDEX);

            foreach (var shaderPair in renderers)
            {
                if (!engine.shaderManager.GetShaderByHandle(shaderPair.Key).WriteMask && overrideMaterial != null)
                    continue;

                if (overrideMaterial != null)
                    currentShader = engine.shaderManager.GetShaderByHandle(overrideMaterial.ShaderHandle);
                else
                    currentShader = engine.shaderManager.GetShaderByHandle(shaderPair.Key);

                Stats.ShaderSwitches++;
                currentShader.Activate();

                foreach (var materialPair in shaderPair.Value)
                {
                    bool materialIsSet = false;
                    foreach (var meshPair in materialPair.Value)
                    {
                        if (currentShader.Instancing)
                        {
                            if (currentMesh != meshPair.Key.mesh)
                            {
                                Stats.MeshSwitches++;
                                meshPair.Key.mesh.Activate();
                                currentMesh = meshPair.Key.mesh;
                            }
                            if (!materialIsSet)
                            {
                                EnableMaterial(materialPair.Key);
                                materialIsSet = true;
                            }
                            Stats.InstancedDraws++;
                            Stats.InstancedDrawSaved += meshPair.Value.Count - 1;
                            if (instancesArray.Length < meshPair.Value.Count)
                            {
                                instancesArray = new Matrix[meshPair.Value.Count];
                                inverseInstancesArray = new Matrix[meshPair.Value.Count];
                            }

                            for (int i = 0; i < meshPair.Value.Count; ++i)
                            {
                                instancesArray[i] = meshPair.Value[i].LocalToWorldMatrix;
                                inverseInstancesArray[i] = meshPair.Value[i].WorldToLocalMatrix;
                            }

                            instancesBuffer.UpdateBuffer(instancesArray);
                            instancesInverseBuffer.UpdateBuffer(inverseInstancesArray);
                            
                            instancesBuffer.Activate(materialPair.Key.SlotCount);
                            instancesInverseBuffer.Activate(materialPair.Key.SlotCount + 1);
                            engine.Device.device.Uniform1I(currentShader.GetUniformLocation("InstancingModels"),
                                materialPair.Key.SlotCount);
                            engine.Device.device.Uniform1I(currentShader.GetUniformLocation("InstancingInverseModels"),
                                materialPair.Key.SlotCount + 1);

                            Stats.TrianglesDrawn += meshPair.Key.mesh.IndexCount(meshPair.Key.subMesh) / 3 * meshPair.Value.Count;
                            Stats.IndicesDrawn += meshPair.Key.mesh.IndexCount(meshPair.Key.subMesh) * meshPair.Value.Count;
                            
                            engine.Device.DrawIndexedInstanced(meshPair.Key.mesh.IndexCount(meshPair.Key.subMesh),
                                meshPair.Value.Count, meshPair.Key.mesh.IndexStart(meshPair.Key.subMesh), 0, 0);
                        }
                        else
                        {
                            foreach (var instance in meshPair.Value)
                            {
                                if ((cameraManager.MainCamera.Transform.Position - instance.Position).Length() > drawDistance)
                                    continue;
                                
                                if (!materialIsSet)
                                {
                                    EnableMaterial(materialPair.Key);
                                    materialIsSet = true;
                                }
                                if (currentMesh != meshPair.Key.mesh)
                                {
                                    Stats.MeshSwitches++;
                                    meshPair.Key.mesh.Activate();
                                    currentMesh = meshPair.Key.mesh;
                                }
                                
                                Stats.NonInstancedDraws++;
                                objectData.WorldMatrix = instance.LocalToWorldMatrix;
                                objectData.InverseWorldMatrix = instance.WorldToLocalMatrix;
                                objectBuffer.UpdateBuffer(ref objectData);
                                engine.Device.device.CheckError("before draw");
                                currentShader.Validate();
                                var start = meshPair.Key.mesh.IndexStart(meshPair.Key.subMesh);
                                var count = meshPair.Key.mesh.IndexCount(meshPair.Key.subMesh);
                                if (start < 0 || start >= meshPair.Key.mesh.IndicesBuffer.Length ||
                                    count <= 0 || start + count > meshPair.Key.mesh.IndicesBuffer.Length)
                                {
                                    Debug.Assert(start >= 0 && start < meshPair.Key.mesh.IndicesBuffer.Length);
                                    Debug.Assert(count > 0 && start + count <= meshPair.Key.mesh.IndicesBuffer.Length);   
                                }
                                else
                                    engine.Device.DrawIndexed(count, start, 0);

                                Stats.TrianglesDrawn += meshPair.Key.mesh.IndexCount(meshPair.Key.subMesh) / 3;
                                Stats.IndicesDrawn += meshPair.Key.mesh.IndexCount(meshPair.Key.subMesh);

                                engine.Device.device.CheckError("After draw");
                            }
                        }
                    }
                }
            }
        }

        public void Render(IMesh mesh, Material material, int submesh, Transform transform)
        {
            Debug.Assert(inRenderingLoop);
            material.Shader.Activate();
            EnableMaterial(material);
            mesh.Activate();
            objectData.WorldMatrix = transform.LocalToWorldMatrix;
            objectData.InverseWorldMatrix = transform.WorldToLocalMatrix;
            objectBuffer.UpdateBuffer(ref objectData);
            var start = mesh.IndexStart(submesh);
            var count = mesh.IndexCount(submesh);
            engine.Device.DrawIndexed(count, start, 0);
        }
        
        private void UpdateSceneBuffer()
        {
            var camera = cameraManager.MainCamera;
            //var proj = Matrix.PerspectiveFovLH(MathUtil.DegreesToRadians(camera.FOV), engine.WindowHost.Aspect, camera.NearClip, camera.FarClip);
            var proj = camera.ProjectionMatrix;
            var vm = camera.Transform.WorldToLocalMatrix;

            sceneData.ViewMatrix = vm;
            sceneData.ProjectionMatrix = proj;
            sceneData.LightPosition = engine.lightManager.MainLight.LightPosition;
            sceneData.CameraPosition = new Vector4(engine.CameraManager.MainCamera.Transform.Position, 1);
            sceneData.LightDirection = new Vector4(Vector3.ForwardLH * engine.lightManager.MainLight.LightRotation, 0);
            sceneData.LightColor = engine.lightManager.MainLight.LightColor;
            sceneData.Time = (float)engine.TotalTime;

            // scenePixelData.CameraPosition = sceneData.CameraPosition;
            // scenePixelData.LightDirection = new Vector4(Vector3.ForwardLH * engine.lightManager.MainLight.LightRotation, 0);
            // scenePixelData.LightColor = engine.lightManager.MainLight.LightColor;
            // scenePixelData.LightPosition = engine.lightManager.MainLight.LightPosition;
            // scenePixelData.Time = (float)engine.TotalTime;
            // scenePixelData.ScreenWidth = engine.WindowHost.WindowWidth;
            // scenePixelData.ScreenHeight = engine.WindowHost.WindowHeight;
        }

        public StaticRenderHandle RegisterStaticRenderer(MeshHandle meshHandle, Material material, int subMesh, Transform transform)
        {
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            var shader = material.ShaderHandle;

            var renderers = staticRenderers.Get(transform.Position.X, transform.Position.Z);
            
            AddRenderer(material, subMesh, transform, renderers, shader, mesh);

            var handle = new StaticRenderHandle(staticHandles.Count + 1);
            staticHandles.Add((material, meshHandle, subMesh, transform));
            return handle;
        }

        private static void AddRenderer(Material material, int subMesh, Transform transform, Dictionary<ShaderHandle, Dictionary<Material, Dictionary<(Mesh mesh, int subMesh), List<Transform>>>> renderers,
            ShaderHandle shader, Mesh mesh)
        {
            if (!renderers.ContainsKey(shader))
                renderers[shader] = new Dictionary<Material, Dictionary<(Mesh, int), List<Transform>>>();

            if (!renderers[shader].ContainsKey(material))
                renderers[shader][material] = new Dictionary<(Mesh, int), List<Transform>>();

            if (!renderers[shader][material].ContainsKey((mesh, subMesh)))
                renderers[shader][material][(mesh, subMesh)] = new List<Transform>();

            renderers[shader][material][(mesh, subMesh)].Add(transform);
        }

        public void UnregisterStaticRenderer(StaticRenderHandle handle)
        {
            if (handle.Handle == 0)
                return;
            var data = staticHandles[handle.Handle - 1];

            var mesh = engine.meshManager.GetMeshByHandle(data.Value.Item2);
            var material = data.Value.Item1;
            var shader = material.ShaderHandle;
            var submesh = data.Value.submesh;
            var transform = data.Value.transform;
            var renderers = staticRenderers.Get(transform.Position.X, transform.Position.Z);
            RemoveFromRenderers(renderers, shader, material, mesh, submesh, transform);

            staticHandles[handle.Handle - 1] = null;
        }

        private static void RemoveFromRenderers(Dictionary<ShaderHandle, Dictionary<Material, Dictionary<(Mesh mesh, int subMesh), List<Transform>>>> renderers, ShaderHandle shader, Material material, Mesh mesh,
            int submesh, Transform transform)
        {
            renderers[shader][material][(mesh, submesh)].Remove(transform);
            if (renderers[shader][material][(mesh, submesh)].Count == 0)
            {
                renderers[shader][material].Remove((mesh, submesh));
                if (renderers[shader][material].Count == 0)
                {
                    renderers[shader].Remove(material);
                    if (renderers.Count == 0)
                        renderers.Remove(shader);
                }
            }
        }

        private List<(Material, MeshHandle, int submesh, Transform transform)?> staticHandles = new();
        private List<(Material, MeshHandle, int submesh, Transform transform)?> dynamicHandles = new();

        public DynamicRenderHandle RegisterDynamicRenderer(MeshHandle meshHandle, Material material, int subMesh, Transform transform)
        {
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            var shader = material.ShaderHandle;
            AddRenderer(material, subMesh, transform, dynamicRenderers, shader, mesh);
            var handle = new DynamicRenderHandle(dynamicHandles.Count + 1);
            dynamicHandles.Add((material, meshHandle, subMesh, transform));
            return handle;
        }

        public void UnregisterDynamicRenderer(DynamicRenderHandle handle)
        {
            if (handle.Handle == 0)
                return;
            var data = dynamicHandles[handle.Handle - 1];

            var mesh = engine.meshManager.GetMeshByHandle(data.Value.Item2);
            var material = data.Value.Item1;
            var shader = material.ShaderHandle;
            var submesh = data.Value.submesh;
            var transform = data.Value.transform;
            RemoveFromRenderers(dynamicRenderers, shader, material, mesh, submesh, transform);

            dynamicHandles[handle.Handle - 1] = null;
        }
    }
}