namespace Game1
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Nez;
    using Nez.Sprites;

    /// <summary>
    ///     This is the main type for your game.
    /// </summary>
    public class GameMain : Core
    {
        public GameMain()
            : base(1280, 768)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            Scene.setDefaultDesignResolution(1280 / 2, 768 / 2, Scene.SceneResolutionPolicy.ShowAllPixelPerfect);

            var myScene = Scene.createWithDefaultRenderer(Color.IndianRed);
            var actorTexture = this.Content.Load<Texture2D>("Actor1");

            var entity = myScene.createEntity("Player");
            entity.addComponent(new Sprite(actorTexture));
            entity.transform.position = new Vector2(640 / 2f, 480 / 2f);

            scene = myScene;
        }
    }
}