namespace Match3.Application
{
    public sealed class SceneManager
    {
        private readonly Dictionary<SceneId, IScene> scenes = new();
        private IScene? current;
        private SceneId? pending;

        public void Register(IScene scene) => scenes[scene.Id] = scene;

        public void TransitionTo(SceneId id) => pending = id;

        public void Update(float dt)
        {
            if (pending is { } nextId && scenes.TryGetValue(nextId, out var next))
            {
                pending = null;
                current = next;
                current.Enter();
            }

            current?.Update(dt);
        }

        public void Render() => current?.Render();
        public void OnMouseDown(float x, float y) => current?.OnMouseDown(x, y);
        public void OnMouseUp(float x, float y) => current?.OnMouseUp(x, y);
        public void OnMouseMove(float x, float y) => current?.OnMouseMove(x, y);
        public void OnResize() => current?.OnResize();
    }
}