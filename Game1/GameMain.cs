namespace Game1
{
    using Game1.Scenes;

    using Nez;

    /// <summary>
    ///     This is the main type for your game.
    /// </summary>
    public class GameMain : Core
    {
        protected override void Initialize()
        {
            base.Initialize();
            Scene = new SceneMap();
        }
    }
}