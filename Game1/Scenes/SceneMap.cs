namespace Game1.Scenes
{
    using Microsoft.Xna.Framework.Input;

    using Nez;

    internal class SceneMap : SceneBase
    {
        public override void Update()
        {
            base.Update();

            if (Input.isKeyPressed(Keys.A))
            {
                Core.startSceneTransition(new WindTransition(() => new SceneTitle()));
            }
        }
    }
}