namespace Game1.Scenes
{
    using Microsoft.Xna.Framework.Input;

    using Nez;

    internal class SceneTitle : SceneBase
    {
        public override void Initialize()
        {
            base.Initialize();

            SetDefaultDesignResolution(1280 / 2, 768 / 2, SceneResolutionPolicy.ShowAllPixelPerfect);

            Screen.SetSize(1280, 768);
        }

        public override void Update()
        {
            base.Update();

            if (Input.isKeyPressed(Keys.A))
            {
                Core.startSceneTransition(new WindTransition(() => new SceneMap()));
            }
        }
    }
}