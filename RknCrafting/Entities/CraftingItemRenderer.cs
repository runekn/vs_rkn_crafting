using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RknCrafting.Entities;

internal class CraftingItemRenderer
{
    private const float PosMin = 0.17f; 
    private const float PosMax = 0.83f; 
    private const float PosMid = 0.5f; 
    private static readonly SurfacePosTransform R1_C1 = new(PosMin, PosMin, 0.95f, 0.9f, 0.01f, 0f);
    private static readonly SurfacePosTransform R2_C1 = new(PosMin, PosMid, 1.01f, 0f, -0.02f, 0.01f);
    private static readonly SurfacePosTransform R3_C1 = new(PosMin, PosMax, 1.02f, -1f, 0.01f, 0f);
    private static readonly SurfacePosTransform R1_C2 = new(PosMid, PosMin, 1.02f, 0f, 0.0f, -0.01f);
    private static readonly SurfacePosTransform R2_C2 = new(PosMid, PosMid, 1, 1.5f, 0.0f, 0.0f);
    private static readonly SurfacePosTransform R3_C2 = new(PosMid, PosMax, 0.98f, 1f, 0.0f, 0.0f);
    private static readonly SurfacePosTransform R1_C3 = new(PosMax, PosMin, 1.02f, -2f, 0.01f, 0.02f);
    private static readonly SurfacePosTransform R2_C3 = new(PosMax, PosMid, 0.97f, -1.1f, 0.005f, 0.0f);
    private static readonly SurfacePosTransform R3_C3 = new(PosMax, PosMax, 1.02f, 0.5f, -0.01f, 0.005f);

    public static float[][] GenTransformationMatrices(InventoryGeneric inventory, string transformCode, bool gridless, Block block, System.Func<ItemSlot, MeshData> getMesh)
    {
        float[][] tfMatrices = new float[inventory.Count][];

        for (int index = 0; index < inventory.Count; index++)
        {
            ItemSlot itemSlot = inventory[index];
            ModelTransform? customTransform = null;
            if (itemSlot.Empty || itemSlot.Itemstack.StackSize <= 0)
            {
                continue;
            }

            FastVec3f scale = Vec3f.One;

            customTransform = itemSlot.Itemstack.Collectible?.Attributes?[transformCode].AsObject<ModelTransform>();
            if (customTransform == null)
            {
                scale = scale.Mul(0.30f);
                MeshData meshData = getMesh(itemSlot);
                if (meshData != null)
                {
                    float itemSize = GetMeshXZSize(meshData);
                    scale = scale.Set(scale.X / itemSize, scale.Y / itemSize, scale.Z / itemSize);
                }
            }

            // Get grid slot translations, and a tiny bit of scale variance.
            SurfacePosTransform posTransform;
            if (gridless)
            {
                posTransform = index switch
                {
                    0 => R2_C2,
                    1 => R1_C1,
                    2 => R1_C3,
                    3 => R3_C2,
                    4 => R3_C3,
                    5 => R2_C1,
                    6 => R1_C2,
                    7 => R2_C3,
                    8 => R3_C1,
                };
            }
            else
            {
                posTransform = index switch
                {
                    0 => R1_C1,
                    1 => R1_C2,
                    2 => R1_C3,
                    3 => R2_C1,
                    4 => R2_C2,
                    5 => R2_C3,
                    6 => R3_C1,
                    7 => R3_C2,
                    8 => R3_C3,
                };
            }

            scale = scale.Mul(posTransform.scaleNoise);
            float x = posTransform.X + posTransform.xNoise;
            float y = posTransform.Y + posTransform.yNoise;

            Matrixf matrixf = new Matrixf()
                .Scale(scale.X, scale.Y, scale.Z) // First scale
                .Translate(-0.5f, 0, -0.5f) // Then center it
                .Translate(x / scale.X, 0, y / scale.Y) // Move to correct slot
                .RotateYDeg(posTransform.rotNoise); // apply rotation noise

            tfMatrices[index] = matrixf.Values;
        }

        return tfMatrices;
    }

    private static float GetMeshXZSize(MeshData mesh)
    {
        Vec3f min = new(float.MaxValue, 0, float.MaxValue);
        Vec3f max = new(float.MinValue, 0, float.MinValue);
        for (int i = 0; i < mesh.VerticesCount; i++)
        {
            int index = i * 3;
            float x = mesh.xyz[index];
            float z = mesh.xyz[index + 2];
            min.X = Math.Min(min.X, x);
            min.Z = Math.Min(min.Z, z);
            max.X = Math.Max(max.X, x);
            max.Z = Math.Max(max.Z, z);
        }
        return Math.Max(max.X - min.X, max.Z - min.Z);
    }
}

internal record SurfacePosTransform(float X, float Y, float scaleNoise, float rotNoise, float xNoise, float yNoise);
