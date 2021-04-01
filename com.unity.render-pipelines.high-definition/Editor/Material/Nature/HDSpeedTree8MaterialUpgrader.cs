using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// SpeedTree8 material upgrader for HDRP.
    /// </summary>
    class HDSpeedTree8MaterialUpgrader : SpeedTree8MaterialUpgrader
    {
        /// <summary>
        /// Creates a SpeedTree8 material upgrader for HDRP.
        /// </summary>
        /// <param name="sourceShaderName">Original shader name.</param>
        /// <param name="destShaderName">Upgrade shader name.</param>
        public HDSpeedTree8MaterialUpgrader(string sourceShaderName, string destShaderName)
            : base(sourceShaderName, destShaderName, HDSpeedTree8MaterialFinalizer)
        {
        }

        public static void HDSpeedTree8MaterialFinalizer(Material mat)
        {
            SetHDSpeedTree8Defaults(mat);
            SpeedTree8MaterialFinalizer(mat);
            HDShaderUtils.ResetMaterialKeywords(mat);
        }

        /// <summary>
        /// Checks if a given material is an HD SpeedTree8 material.
        /// </summary>
        /// <param name="mat">Material to check.</param>
        /// <returns></returns>
        public static bool IsHDSpeedTree8Material(Material mat)
        {
            return (mat.shader.name == "HDRP/Nature/SpeedTree8");
        }

        /// <summary>
        /// Restores SpeedTree8-specific material properties and keywords that were set during import and should not be reset.
        /// </summary>
        /// <param name="mat">SpeedTree8 material.</param>
        public static void RestoreHDSpeedTree8Keywords(Material mat)
        {
            if (mat.name.Contains("Billboard")) // Hacky but it'll hold until newer versions of shadergraph with keyword toggle support
            {
                mat.EnableKeyword("EFFECT_BILLBOARD");
            }
        }

        private static void SetHDSpeedTree8Defaults(Material mat)
        {
            if (mat.IsKeywordEnabled("EFFECT_BILLBOARD"))
            {
                mat.SetFloat("_DoubleSidedEnable", 0.0f);
            }
            else
            {
                mat.SetFloat("_DoubleSidedEnable", 1.0f);
            }
            mat.SetFloat("_DoubleSidedNormalMode", (int)DoubleSidedNormalMode.Mirror);

            SetDefaultDiffusionProfile(mat);
        }

        // Should match HDRenderPipelineEditorResources.defaultDiffusionProfileSettingsList[foliageIdx]
        private const string kFoliageDiffusionProfilePath = "Runtime/RenderPipelineResources/FoliageDiffusionProfile.asset";
        // Should match HDRenderPipelineEditorResources.defaultDiffusionProfileSettingsList[foliageIdx].name
        private const string kDefaultDiffusionProfileName = "Foliage";
        private static void SetDefaultDiffusionProfile(Material mat)
        {
            string guid = "";
            long localID;
            uint diffusionProfileHash = 0;
            foreach (var diffusionProfileAsset in HDRenderPipeline.defaultAsset.diffusionProfileSettingsList)
            {
                if (diffusionProfileAsset != null && diffusionProfileAsset.name.Equals(kDefaultDiffusionProfileName))
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier<DiffusionProfileSettings>(diffusionProfileAsset, out guid, out localID))
                    {
                        diffusionProfileHash = diffusionProfileAsset.profile.hash;
                        break;
                    }
                }
            }

            if (diffusionProfileHash == 0)
            {
                // If the user doesn't have a foliage diffusion profile defined, grab the foliage diffusion profile that comes with HD.
                // This won't work until the user adds it to their default diffusion profiles list,
                // but there is a nice "fix" button on the material to help with that.
                DiffusionProfileSettings foliageSettings = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(HDUtils.GetHDRenderPipelinePath() + kFoliageDiffusionProfilePath);
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier<DiffusionProfileSettings>(foliageSettings, out guid, out localID))
                {
                    diffusionProfileHash = foliageSettings.profile.hash;
                }
            }

            if (diffusionProfileHash != 0)
            {
                mat.SetVector(HDShaderIDs._DiffusionProfileAsset, HDUtils.ConvertGUIDToVector4(guid));
                mat.SetFloat(HDShaderIDs._DiffusionProfileHash, HDShadowUtils.Asfloat(diffusionProfileHash));
            }
        }
    }
}
