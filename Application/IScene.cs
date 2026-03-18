namespace Match3.Application
{
    public enum SceneId { MainMenu, Game, GameOver }

    public interface IScene
    {
        SceneId Id { get; }
        void Enter();
        void Update(float dt);
        void Render();
        void OnMouseDown(float x, float y);
        void OnMouseUp(float x, float y);
        void OnMouseMove(float x, float y);
        void OnResize() { } // default empty implementation
    }
}