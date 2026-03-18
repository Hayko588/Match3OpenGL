using Match3.Animation;
using Match3.Application;
using Match3.Application.Scenes;
using Match3.Domain;
using Match3.Rendering;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Match3
{
    public static class Program
    {
        private static IWindow window = null!;
        private static IInputContext input = null!;
        private static GL gl = null!;

        private static ShaderProgram shader = null!;
        private static SpriteBatch batch = null!;
        private static GemBatch gemBatch = null!;
        private static FontAtlas fontAtlas = null!;

        private static SceneManager sceneManager = null!;
        private static GameState gameState = null!;
        private static BoardLayout layout = null!;
        private static VfxManager vfx = null!;

        private static uint fbWidth;
        private static uint fbHeight;

        public static void Main()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(GameConfig.WINDOW_WIDTH, GameConfig.WINDOW_HEIGHT);
            options.Title = GameConfig.WINDOW_TITLE;
            options.VSync = true;
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core,
                ContextFlags.Default, new APIVersion(3, 3));

            window = Window.Create(options);
            window.Load += OnLoad;
            window.Update += OnUpdate;
            window.Render += OnRender;
            window.Resize += OnResize;
            window.FramebufferResize += OnFramebufferResize;
            window.Closing += OnClose;
            window.Run();
        }

        private static void OnLoad()
        {
            gl = window.CreateOpenGL();
            input = window.CreateInput();
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            shader = new ShaderProgram(gl, GameConfig.VERTEX_SHADER, GameConfig.FRAGMENT_SHADER);
            batch = new SpriteBatch(gl, shader);
            gemBatch = new GemBatch(gl);
            fontAtlas = new FontAtlas(gl, GameConfig.FONT_SIZE);
            var textRenderer = new TextRenderer(fontAtlas);
            var gemRenderer = new GemRenderer(gemBatch, batch);

            layout = new BoardLayout();
            layout.Recalculate(window.Size.X, window.Size.Y);

            fbWidth = (uint)window.FramebufferSize.X;
            fbHeight = (uint)window.FramebufferSize.Y;

            gameState = new GameState();
            var random = new Random();

            var board = new Board();
            var detector = new MatchDetector();
            var bonusResolver = new BonusResolver();

            vfx = new VfxManager(random);
            var animator = new AnimationUpdater(layout);
            var controller = new GameController(board, detector, bonusResolver, gameState, layout, vfx, random);

            sceneManager = new SceneManager();
            sceneManager.Register(new MainMenuScene(batch, textRenderer, layout, gameState, sceneManager));
            sceneManager.Register(new GameScene(batch, gemBatch, textRenderer, gemRenderer, layout, gameState, controller, animator, vfx, sceneManager));
            sceneManager.Register(new GameOverScene(batch, textRenderer, layout, gameState, sceneManager));

            foreach (var mouse in input.Mice)
            {
                mouse.MouseDown += (m, btn) => { if (btn == MouseButton.Left) sceneManager.OnMouseDown(m.Position.X, m.Position.Y); };
                mouse.MouseUp += (m, btn) => { if (btn == MouseButton.Left) sceneManager.OnMouseUp(m.Position.X, m.Position.Y); };
                mouse.MouseMove += (_, pos) => sceneManager.OnMouseMove(pos.X, pos.Y);
            }

            sceneManager.TransitionTo(SceneId.MainMenu);
        }

        private static void OnUpdate(double dt)
        {
            float fdt = (float)dt;
            gameState.Time += fdt;
            vfx.Update(fdt);
            sceneManager.Update(fdt);
        }

        private static void OnRender(double dt)
        {
            gl.Viewport(0, 0, fbWidth, fbHeight);
            gl.ClearColor(GameConfig.COLOR_BG.X, GameConfig.COLOR_BG.Y, GameConfig.COLOR_BG.Z, GameConfig.COLOR_BG.W);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            sceneManager.Render();
        }

        private static void OnResize(Vector2D<int> size)
        {
            layout.Recalculate(size.X, size.Y);
            sceneManager.OnResize();
        }

        private static void OnFramebufferResize(Vector2D<int> size)
        {
            fbWidth = (uint)size.X;
            fbHeight = (uint)size.Y;
        }

        private static void OnClose()
        {
            fontAtlas.Dispose();
            gemBatch.Dispose();
            batch.Dispose();
            shader.Dispose();
            input.Dispose();
            gl.Dispose();
        }
    }
}