using System;
using RKN.Crafting.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace RknCrafting.Entities;

public class GridCountRenderer : IRenderer
{
    public double RenderOrder => 0.5;
    public int RenderRange => 10;

    private readonly ICoreClientAPI api;
    private readonly CairoFont cairoFont;
    private readonly CairoFont shadowFont;
    private readonly LoadedTexture[] textures;
    private readonly LoadedTexture[] texturesShadow;
    
    public GridCountRenderer(ICoreClientAPI api)
    {
        this.api = api;
        cairoFont = new CairoFont();
        cairoFont.Color = (double[]) GuiStyle.DialogDefaultTextColor.Clone();
        cairoFont.Fontname = GuiStyle.StandardFontName;
        cairoFont.UnscaledFontsize = GuiStyle.SmallFontSize;
        shadowFont = cairoFont.Clone();
        shadowFont.Color = ColorUtil.Hex2Doubles("#000000", 1.0);
        textures = new LoadedTexture[9];
        texturesShadow = new LoadedTexture[9];
        for (int i = 0; i < 9; i++)
        {
            textures[i] = api.Gui.TextTexture.GenTextTexture("", cairoFont);
            texturesShadow[i] = api.Gui.TextTexture.GenTextTexture("", shadowFont);            
        }
    }

    public void Dispose()
    {
        api.Event.UnregisterRenderer(this, EnumRenderStage.Ortho);
        textures.Foreach(tex => tex.Dispose());
        texturesShadow.Foreach(tex => tex.Dispose());
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        BlockSelection blockSel = api.World.Player.CurrentBlockSelection;
        BlockEntityCraftingSurface be;
        if (blockSel == null)
        {
            return;
        }
        be = api.World.BlockAccessor.GetBlockEntity<BlockEntityCraftingSurface>(blockSel.Position);
        if (be == null)
        {
            return;
        }
        
        for (int i = 0; i < be.Inventory.Count; i++)
        {
            ItemSlot itemSlot = be.Inventory[i]!;
            if (itemSlot.Empty)
            {
                continue;
            }

            LoadedTexture texture = textures[i];
            LoadedTexture textureShadow = texturesShadow[i];

            string text = itemSlot.Itemstack.StackSize.ToString();
            api.Gui.TextTexture.GenOrUpdateTextTexture(itemSlot.Itemstack.StackSize.ToString(), cairoFont, ref texture);
            api.Gui.TextTexture.GenOrUpdateTextTexture(text, shadowFont, ref textureShadow);

            float xOffset = (i % 3) switch
            {
                2 => 0.85f,
                1 => 0.5f,
                _ => 0.15f,
            };
            float zOffset = i switch
            {
                >= 6 => 0.85f,
                >= 3 => 0.5f,
                _ => 0.15f
            };

            Vec3d screen = MatrixToolsd.Project(
                new Vec3d(be.Pos.X + xOffset, be.Pos.Y + 0.1f, be.Pos.Z + zOffset),
                api.Render.PerspectiveProjectionMat,
                api.Render.PerspectiveViewMat,
                api.Render.FrameWidth,
                api.Render.FrameHeight
            );

            if (screen.Z < 0)
            {
                return;
            }
        
            float x = ((float)screen.X - texture.Width / 2f) + 0f;
            float y = (api.Render.FrameHeight - (float)screen.Y) - texture.Height / 2f;

            float opacity = 0.5f;
        
            api.Render.Render2DTexturePremultipliedAlpha(
                textureShadow.TextureId,
                x + 1, y + 1,
                textureShadow.Width, textureShadow.Height,
                49,
                new Vec4f(opacity, opacity, opacity, opacity)
            );

            opacity = 1f;
        
            api.Render.Render2DTexturePremultipliedAlpha(
                texture.TextureId,
                x, y,
                texture.Width, texture.Height,
                51,
                new Vec4f(opacity, opacity, opacity, opacity)
            );
        }
    }
}