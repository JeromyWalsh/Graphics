using System;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    //Class representing lights in the context of a view.
    internal partial class HDVisibleLightEntities : IDisposable
    {
        static private HashSet<HDVisibleLightEntities> s_Instances = new HashSet<HDVisibleLightEntities>();

        public const int ArrayCapacity = 32;
        int m_Capacity = 0;
        int m_Size = 0;

        internal enum ProcessLightsCountSlots
        {
            ProcessedLights,
            DirectionalLights,
            PunctualLights,
            AreaLightCounts,
            ShadowLights,
        }

        [Flags]
        internal enum ShadowMapFlags
        {
            None = 0,
            WillRenderShadowMap = 1 << 0,
            WillRenderScreenSpaceShadow = 1 << 1,
            WillRenderRayTracedShadow = 1 << 2
        }


        NativeArray<int> m_ProcessVisibleLightCounts;

        #region visible lights SoA
        NativeArray<HDLightEntityData> m_VisibleEntities;
        NativeArray<LightBakingOutput> m_VisibleLightBakingOutput;
        NativeArray<LightShadows>      m_VisibleLightShadows;
        NativeArray<int>               m_VisibleLightProcessedIndices;
        #endregion

        #region ProcessedLight data SoA
        //TODO: check perf and see if this needs to be a SoA
        NativeArray<HDLightType>     m_ProcessedLightTypes;
        NativeArray<int>             m_ProcessedLightVisibleIndices;
        NativeArray<LightCategory>   m_ProcessedLightCategories;
        NativeArray<GPULightType>    m_ProcessedGPULightType;
        NativeArray<LightVolumeType> m_ProcessedLightVolumeType;
        NativeArray<float>           m_ProcessedLightDistanceToCamera;
        NativeArray<float>           m_ProcessedLightDistanceFade;
        NativeArray<float>           m_ProcessedLightVolumetricDistanceFade;
        NativeArray<bool>            m_ProcessedLightIsBakedShadowMask;
        NativeArray<ShadowMapFlags>  m_ProcessedShadowMapFlags;
        NativeArray<uint>            m_SortKeys;
        NativeArray<uint>            m_SortSupportArray;
        NativeArray<int>             m_ShadowLightsDataIndices;

        public int preprocessedLightCounts => m_ProcessVisibleLightCounts.IsCreated ? m_ProcessVisibleLightCounts[(int)ProcessLightsCountSlots.ProcessedLights] : 0;

        public NativeArray<HDLightEntityData> visibleEntities => m_VisibleEntities;
        public NativeArray<int>             visibleLightProcessedIndices => m_VisibleLightProcessedIndices;
        public NativeArray<int>             processedLightVisibleIndices => m_ProcessedLightVisibleIndices;
        public NativeArray<HDLightType>     processedLightTypes => m_ProcessedLightTypes;
        public NativeArray<LightCategory>   processedLightCategories => m_ProcessedLightCategories;
        public NativeArray<GPULightType>    processedGPULightType => m_ProcessedGPULightType;
        public NativeArray<LightVolumeType> processedLightVolumeType => m_ProcessedLightVolumeType;
        public NativeArray<float>           processedLightDistanceToCamera => m_ProcessedLightDistanceToCamera;
        public NativeArray<float>           processedLightDistanceFade => m_ProcessedLightDistanceFade;
        public NativeArray<float>           processedLightVolumetricDistanceFade => m_ProcessedLightVolumetricDistanceFade;
        public NativeArray<bool>            processedLightIsBakedShadowMask => m_ProcessedLightIsBakedShadowMask;
        public NativeArray<ShadowMapFlags>  processedShadowMapFlags => m_ProcessedShadowMapFlags;
        public NativeArray<uint>            sortKeys => m_SortKeys;
        public NativeArray<uint>            sortSupportArray => m_SortSupportArray;
        public NativeArray<int>             shadowLightsDataIndices => m_ShadowLightsDataIndices;

        private void ResizeArrays(int newCapacity)
        {
            m_Capacity = Math.Max(Math.Max(newCapacity, ArrayCapacity), m_Capacity * 2);
            m_VisibleEntities.ResizeArray(m_Capacity);
            m_VisibleLightBakingOutput.ResizeArray(m_Capacity);
            m_VisibleLightShadows.ResizeArray(m_Capacity);
            m_VisibleLightProcessedIndices.ResizeArray(m_Capacity);

            m_ProcessedLightTypes.ResizeArray(m_Capacity);
            m_ProcessedLightVisibleIndices.ResizeArray(m_Capacity);
            m_ProcessedLightCategories.ResizeArray(m_Capacity);
            m_ProcessedGPULightType.ResizeArray(m_Capacity);
            m_ProcessedLightVolumeType.ResizeArray(m_Capacity);
            m_ProcessedLightDistanceToCamera.ResizeArray(m_Capacity);
            m_ProcessedLightDistanceFade.ResizeArray(m_Capacity);
            m_ProcessedLightVolumetricDistanceFade.ResizeArray(m_Capacity);
            m_ProcessedLightIsBakedShadowMask.ResizeArray(m_Capacity);
            m_ProcessedShadowMapFlags.ResizeArray(m_Capacity);
            m_SortKeys.ResizeArray(m_Capacity);
            m_ShadowLightsDataIndices.ResizeArray(m_Capacity);
        }

        private void DisposeArrays()
        {
            if (m_SortSupportArray.IsCreated)
                m_SortSupportArray.Dispose();

            if (m_Capacity == 0)
                return;

            m_ProcessVisibleLightCounts.Dispose();

            m_VisibleEntities.Dispose();
            m_VisibleLightBakingOutput.Dispose();
            m_VisibleLightShadows.Dispose();
            m_VisibleLightProcessedIndices.Dispose();

            m_ProcessedLightTypes.Dispose();
            m_ProcessedLightVisibleIndices.Dispose();
            m_ProcessedLightCategories.Dispose();
            m_ProcessedGPULightType.Dispose();
            m_ProcessedLightVolumeType.Dispose();
            m_ProcessedLightDistanceToCamera.Dispose();
            m_ProcessedLightDistanceFade.Dispose();
            m_ProcessedLightVolumetricDistanceFade.Dispose();
            m_ProcessedLightIsBakedShadowMask.Dispose();
            m_ProcessedShadowMapFlags.Dispose();
            m_SortKeys.Dispose();
            m_ShadowLightsDataIndices.Dispose();

            m_Capacity = 0;
            m_Size = 0;
        }

        ~HDVisibleLightEntities()
        {
            Dispose();
        }

        #endregion

        public static HDVisibleLightEntities Get()
        {
            var instance = UnsafeGenericPool<HDVisibleLightEntities>.Get();
            s_Instances.Add(instance);
            return instance;
        }

        public static void Release(HDVisibleLightEntities obj)
        {
            UnsafeGenericPool<HDVisibleLightEntities>.Release(obj);
        }

        private void SortLightKeys()
        {
            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.SortVisibleLights)))
            {
                //Tunning against ps4 console,
                //32 items insertion sort has a workst case of 3 micro seconds.
                //200 non recursive merge sort has around 23 micro seconds.
                //From 200 and more, Radix sort beats everything.
                var sortSize = preprocessedLightCounts;
                if (sortSize <= 32)
                    CoreUnsafeUtils.InsertionSort(m_SortKeys, sortSize);
                else if (m_Size <= 200)
                    CoreUnsafeUtils.MergeSort(m_SortKeys, sortSize, ref m_SortSupportArray);
                else
                    CoreUnsafeUtils.RadixSort(m_SortKeys, sortSize, ref m_SortSupportArray);
            }
        }

        public void PreprocessVisibleLights(
            HDCamera hdCamera,
            in CullingResults cullingResult,
            HDShadowManager shadowManager,
            in HDShadowInitParameters inShadowInitParameters,
            in AOVRequestData aovRequestData,
            in GlobalLightLoopSettings lightLoopSettings,
            DebugDisplaySettings debugDisplaySettings)
        {
            BuildVisibleLightEntities(cullingResult);

            if (m_Size == 0)
                return;

            FilterVisibleLightsByAOV(aovRequestData);
            StartProcessVisibleLightJob(hdCamera, cullingResult.visibleLights, lightLoopSettings, debugDisplaySettings);
            CompleteProcessVisibleLightJob();

            ProcessShadows(hdCamera, shadowManager, inShadowInitParameters, cullingResult);
            FilterProcessedLightsByDebugFilter(debugDisplaySettings);

            SortLightKeys();
        }

        public static void Cleanup()
        {
            foreach (var obj in s_Instances)
            {
                obj.Dispose();
            }
            s_Instances.Clear();
        }

        public void Dispose()
        {
            DisposeArrays();
        }

        public void Reset()
        {
            m_Size = 0;
            //Track object if its recycled. This avoids memory leaks between pipeline sessions (during cleanup all the buffers can get freed).
            if (!s_Instances.Contains(this))
                s_Instances.Add(this);
        }

        #region Internal implementation

        private void BuildVisibleLightEntities(in CullingResults cullResults)
        {
            m_Size = 0;
            HDLightEntityCollection.instance.CompleteLightTransformDataJobs();
            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.BuildVisibleLightEntities)))
            {
                if (cullResults.visibleLights.Length == 0
                    || HDLightEntityCollection.instance == null)
                    return;

                if (cullResults.visibleLights.Length > m_Capacity)
                {
                    ResizeArrays(cullResults.visibleLights.Length);
                }

                m_Size = cullResults.visibleLights.Length;

                //TODO: this should be accelerated by a c++ API
                for (int i = 0; i < cullResults.visibleLights.Length; ++i)
                {
                    Light light = cullResults.visibleLights[i].light;
                    var entityData = HDLightEntityCollection.instance.FindEntity(light);
                    if (!entityData.valid)
                    {
                        var defaultEntity = HDLightEntityCollection.instance.GetDefaultLightEntity();
                        entityData = HDLightEntityCollection.instance.GetEntityData(defaultEntity);
                    }

                    m_VisibleEntities[i] = entityData;
                    m_VisibleLightBakingOutput[i] = light.bakingOutput;
                    m_VisibleLightShadows[i] = light.shadows;
                }
            }
        }

        private void ProcessShadows(
            HDCamera hdCamera,
            HDShadowManager shadowManager,
            in HDShadowInitParameters inShadowInitParameters,
            in CullingResults cullResults)
        {
            int shadowLights = m_ProcessVisibleLightCounts[(int)ProcessLightsCountSlots.ShadowLights];
            if (shadowLights == 0)
                return;

            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.ProcessShadows)))
            {
                NativeArray<VisibleLight> visibleLights = cullResults.visibleLights;
                var hdShadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();

                for (int i = 0; i < shadowLights; ++i)
                {
                    int visibleLightIndex = m_ShadowLightsDataIndices[i];
                    int processedLightIndex = m_VisibleLightProcessedIndices[visibleLightIndex];
                    if (!cullResults.GetShadowCasterBounds(visibleLightIndex, out var bounds))
                    {
                        m_ProcessedShadowMapFlags[processedLightIndex] = ShadowMapFlags.None;
                        continue;
                    }

                    HDLightEntityData entityData = m_VisibleEntities[visibleLightIndex];
                    HDAdditionalLightData additionalLightData = HDLightEntityCollection.instance.hdAdditionalLightData[entityData.dataIndex];
                    if (additionalLightData == null)
                        continue;

                    VisibleLight visibleLight = visibleLights[visibleLightIndex];
                    additionalLightData.ReserveShadowMap(hdCamera.camera, shadowManager, hdShadowSettings, inShadowInitParameters, visibleLight, m_ProcessedLightTypes[processedLightIndex]);
                }
            }
        }

        private void FilterVisibleLightsByAOV(AOVRequestData aovRequest)
        {
            if (!aovRequest.hasLightFilter)
                return;

            for (int i = 0; i < m_Size; ++i)
            {
                var visibleEntity = m_VisibleEntities[i];
                var go = HDLightEntityCollection.instance.aovGameObjects[visibleEntity.dataIndex];
                if (go == null)
                    continue;

                if (!aovRequest.IsLightEnabled(go))
                    m_VisibleEntities[i] = HDLightEntityData.Invalid;
            }
        }

        private void FilterProcessedLightsByDebugFilter(DebugDisplaySettings debugDisplaySettings)
        {
            var debugLightFilter = debugDisplaySettings.GetDebugLightFilterMode();
            if (debugLightFilter == DebugLightFilterMode.None)
                return;

            for (int i = 0; i < preprocessedLightCounts; ++i)
            {
                HDLightEntityData entity = m_VisibleEntities[i];
                if (!entity.valid)
                    continue;

                if (!debugLightFilter.IsEnabledFor(m_ProcessedGPULightType[i], HDLightEntityCollection.instance.spotLightShapes[entity.dataIndex]))
                    m_VisibleEntities[i] = HDLightEntityData.Invalid;
            }
        }

        #endregion
    }
}
