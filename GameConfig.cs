using Match3.Domain;
using System.Numerics;

namespace Match3
{
    public static class GameConfig
    {
        public const int BOARD_SIZE = 8;
        public const int GEM_COUNT = 5;
        public const int MAX_FILL_ATTEMPTS = 50;

        public const float GAME_DURATION = 60f;
        public const float COMBO_DECAY_TIME = 2f;

        public const int POINTS_PER_GEM = 10;

        public const float SWAP_DURATION = 0.20f;
        public const float SWAP_BACK_DURATION = 0.15f;
        public const float DESTROY_DURATION = 0.25f;
        public const float BOMB_EXPLODE_DURATION = 0.25f;
        public const float GRAVITY_DURATION = 0.25f;
        public const float SPAWN_DURATION = 0.30f;

        public const float DRAG_THRESHOLD = 0.25f;
        public const int KEY_UNDO = 90;

        public const float DESTROYER_SPEED_MULTIPLIER = 14f;
        public const float DESTROYER_LIFETIME = 0.6f;
        public const float EXPLOSION_DURATION = 0.4f;
        public const float EXPLOSION_RADIUS_MULTIPLIER = 2f;
        public const int PARTICLES_PER_GEM = 4;
        public const float PARTICLE_GRAVITY = 400f;
        public const float SCORE_POPUP_LIFETIME = 1.2f;
        public const float SCORE_POPUP_RISE_SPEED = 50f;

        public const float GEM_RADIUS_FACTOR = 0.36f;
        public const float SELECTION_OUTER_RADIUS = 0.48f;
        public const float SELECTION_PULSE_SPEED = 6f;
        public const float SELECTION_SCALE_SPEED = 5f;
        public const float SELECTION_SCALE_AMOUNT = 0.08f;

        public const float HUD_HEIGHT_FACTOR = 0.12f;
        public const float BOARD_PADDING = 30f;
        public const float BOARD_BORDER = 4f;

        public const float FONT_SIZE = 48f;

        public const int WINDOW_WIDTH = 720;
        public const int WINDOW_HEIGHT = 800;
        public const string WINDOW_TITLE = "Match-3";

        private static readonly Vector4[] GEM_COLOR_TABLE =
        [
            default,
            new(0.90f, 0.22f, 0.21f, 1f),
            new(0.20f, 0.47f, 0.96f, 1f),
            new(0.30f, 0.80f, 0.35f, 1f),
            new(0.98f, 0.78f, 0.15f, 1f),
            new(0.68f, 0.32f, 0.87f, 1f),
    ];

        public static Vector4 GetGemColor(GemType gem)
        {
            int idx = (int)gem;
            return (uint)idx < (uint)GEM_COLOR_TABLE.Length
                ? GEM_COLOR_TABLE[idx]
                : COLOR_WHITE;
        }

        public static readonly Vector4 COLOR_ACCENT = new(0.35f, 0.65f, 1f, 1f);
        public static readonly Vector4 COLOR_DANGER = new(0.95f, 0.30f, 0.25f, 1f);
        public static readonly Vector4 COLOR_PANEL = new(0.15f, 0.15f, 0.20f, 0.9f);
        public static readonly Vector4 COLOR_WHITE = new(1f, 1f, 1f, 1f);
        public static readonly Vector4 COLOR_BG = new(0.08f, 0.08f, 0.10f, 1f);
        public static readonly Vector4 COLOR_BOARD_BORDER = new(0.25f, 0.25f, 0.32f, 1f);
        public static readonly Vector4 COLOR_CELL_DARK = new(0.14f, 0.14f, 0.18f, 1f);
        public static readonly Vector4 COLOR_CELL_LIGHT = new(0.19f, 0.19f, 0.24f, 1f);
        public static readonly Vector4 COLOR_LABEL = new(0.6f, 0.6f, 0.65f, 1f);
        public static readonly Vector4 COLOR_BAR_BG = new(0.10f, 0.10f, 0.13f, 1f);
        public static readonly Vector4 COLOR_EXPLOSION = new(1f, 0.8f, 0.3f, 1f);
        public static readonly Vector4 COLOR_COMBO = new(1f, 0.85f, 0.2f, 1f);

        public const string VERTEX_SHADER = @"#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec4 aColor;
uniform mat4 uProjection;
out vec2 vTexCoord;
out vec4 vColor;
void main() {
    gl_Position = uProjection * vec4(aPos, 0.0, 1.0);
    vTexCoord = aTexCoord;
    vColor = aColor;
}";

        public const string FRAGMENT_SHADER = @"#version 330 core
in vec2 vTexCoord;
in vec4 vColor;
uniform sampler2D uTexture;
uniform int uUseTexture;
out vec4 FragColor;
void main() {
    if (uUseTexture == 1) {
        vec4 tex = texture(uTexture, vTexCoord);
        FragColor = vec4(vColor.rgb, vColor.a * tex.a);
    } else {
        FragColor = vColor;
    }
}";
    }
}