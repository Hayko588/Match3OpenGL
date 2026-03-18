using Match3.Domain;
using Match3.Rendering;
using System.Numerics;

namespace Match3.Animation;

/// <summary>
/// Visual state for a gem. Pre-resolves color and SDF shape ID on Init()
/// so the render loop avoids any enum casts or array lookups per frame.
/// </summary>
public sealed class GemVisual
{
    // ── Domain identity ──
    public GemType Gem { get; private set; }
    public BonusType Bonus { get; private set; }

    // ── Pre-resolved render data (set once in Init, read every frame) ──
    public Vector4 Color { get; private set; }
    public float ShapeId { get; private set; }

    // ── Animated properties (mutated by AnimationUpdater) ──
    public float X { get; set; }
    public float Y { get; set; }
    public float Scale { get; set; } = 1f;
    public float Alpha { get; set; } = 1f;

    // ── Shape lookup table (indexed by GemType byte value, avoids per-frame cast) ──
    private static readonly float[] ShapeTable =
    [
        -1f,                        // None = 0
        GemBatch.SHAPE_CIRCLE,      // Red = 1
        GemBatch.SHAPE_SQUARE,      // Blue = 2
        GemBatch.SHAPE_DIAMOND,     // Green = 3
        GemBatch.SHAPE_HEXAGON,     // Yellow = 4
        GemBatch.SHAPE_STAR,        // Purple = 5
    ];

    public void Reset()
    {
        Gem = GemType.None;
        Bonus = BonusType.None;
        Color = default;
        ShapeId = -1f;
        X = 0; Y = 0;
        Scale = 1f;
        Alpha = 1f;
    }

    public void Init(GemType gem, BonusType bonus, float x, float y)
    {
        Gem = gem;
        Bonus = bonus;
        Color = GameConfig.GetGemColor(gem);
        ShapeId = ShapeTable[(byte)gem];
        X = x; Y = y;
        Scale = 1f;
        Alpha = 1f;
    }

    public bool IsVisible => Alpha > 0.01f && ShapeId >= 0f;
}
