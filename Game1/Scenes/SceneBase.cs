namespace Game1.Scenes
{
    using Microsoft.Xna.Framework;

    using Nez;

    public abstract class SceneBase : Scene
    {
        public const int ScreenSpaceRenderLayer = 999;

        protected SceneBase()
        {
            this.clearColor = Color.IndianRed;

            // renders every RenderableComponent that is enabled in your scene.
            this.addRenderer(new DefaultRenderer());
        }
    }
}