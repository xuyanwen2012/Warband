namespace Game1.Scenes
{
    using Microsoft.Xna.Framework;

    using Nez;

    public abstract class SceneBase : Scene
    {
        private const int ScreenSpaceRenderLayer = 999;

        protected SceneBase()
        {
            this.AddRenderer(new ScreenSpaceRenderer(100, ScreenSpaceRenderLayer));
            this.AddRenderer(new RenderLayerExcludeRenderer(0, ScreenSpaceRenderLayer));
        }

        public override void Initialize()
        {
            base.Initialize();

            this.clearColor = Color.IndianRed;
            this.SetDesignResolution(1280 / 2, 768 / 2, SceneResolutionPolicy.ShowAllPixelPerfect);
            Screen.SetSize(1280, 768);
        }
    }
}