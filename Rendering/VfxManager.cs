using Match3.Animation;
using Match3.Domain;
using System.Numerics;

namespace Match3.Rendering;

public sealed class VfxManager
{
    public sealed class Destroyer
    {
        public float X, Y, VelX, VelY, Life;
        public Vector4 Color;
        public bool Horizontal;

        public void Reset()
        {
            X = 0; Y = 0; VelX = 0; VelY = 0;
            Life = 0; Color = default; Horizontal = false;
        }
    }

    public sealed class Explosion
    {
        public float X, Y, Timer, MaxTimer, Radius;

        public void Reset()
        {
            X = 0; Y = 0; Timer = 0; MaxTimer = 0; Radius = 0;
        }
    }

    public sealed class Particle
    {
        public float X, Y, VelX, VelY, Life, MaxLife, Size;
        public Vector4 Color;

        public void Reset()
        {
            X = 0; Y = 0; VelX = 0; VelY = 0;
            Life = 0; MaxLife = 0; Size = 0; Color = default;
        }
    }

    private readonly ObjectPool<Destroyer> destroyerPool;
    private readonly ObjectPool<Explosion> explosionPool;
    private readonly ObjectPool<Particle> particlePool;

    private readonly List<Destroyer> destroyers = [];
    private readonly List<Explosion> explosions = [];
    private readonly List<Particle> particles = [];

    private readonly Random rng;

    public VfxManager(Random rng)
    {
        this.rng = rng;

        destroyerPool = new(() => new Destroyer(), d => d.Reset(), prewarm: 8);
        explosionPool = new(() => new Explosion(), e => e.Reset(), prewarm: 4);
        particlePool = new(() => new Particle(), p => p.Reset(), prewarm: 64);
    }

    public void Clear()
    {
        ReturnAll(destroyers, destroyerPool);
        ReturnAll(explosions, explosionPool);
        ReturnAll(particles, particlePool);
    }

    public void Update(float dt)
    {
        UpdateAndRecycle(destroyers, destroyerPool, d =>
        {
            d.X += d.VelX * dt;
            d.Y += d.VelY * dt;
            d.Life -= dt;
            return d.Life > 0;
        });

        UpdateAndRecycle(explosions, explosionPool, e =>
        {
            e.Timer -= dt;
            return e.Timer > 0;
        });

        UpdateAndRecycle(particles, particlePool, p =>
        {
            p.X += p.VelX * dt;
            p.Y += p.VelY * dt;
            p.VelY += GameConfig.PARTICLE_GRAVITY * dt;
            p.Life -= dt;
            return p.Life > 0;
        });
    }

    public void SpawnDestroyers(Vector2 origin, bool horizontal, Vector4 color, float cellSize)
    {
        float speed = cellSize * GameConfig.DESTROYER_SPEED_MULTIPLIER;
        float life = GameConfig.DESTROYER_LIFETIME;

        var d1 = destroyerPool.Get();
        d1.X = origin.X; d1.Y = origin.Y; d1.Color = color;
        d1.Life = life; d1.Horizontal = horizontal;
        d1.VelX = horizontal ? -speed : 0;
        d1.VelY = horizontal ? 0 : -speed;
        destroyers.Add(d1);

        var d2 = destroyerPool.Get();
        d2.X = origin.X; d2.Y = origin.Y; d2.Color = color;
        d2.Life = life; d2.Horizontal = horizontal;
        d2.VelX = horizontal ? speed : 0;
        d2.VelY = horizontal ? 0 : speed;
        destroyers.Add(d2);
    }

    public void SpawnExplosion(Vector2 center, float cellSize)
    {
        float dur = GameConfig.EXPLOSION_DURATION;
        var ex = explosionPool.Get();
        ex.X = center.X; ex.Y = center.Y;
        ex.Timer = dur; ex.MaxTimer = dur;
        ex.Radius = cellSize * GameConfig.EXPLOSION_RADIUS_MULTIPLIER;
        explosions.Add(ex);
    }

    public void SpawnMatchParticles(HashSet<Pos> positions, GemVisual?[,] visuals)
    {
        foreach (var pos in positions)
        {
            var vis = visuals[pos.Row, pos.Col];
            if (vis is null) continue;

            var color = vis.Color;
            for (int i = 0; i < GameConfig.PARTICLES_PER_GEM; i++)
            {
                float angle = rng.NextSingle() * MathF.PI * 2f;
                float speed = 80f + rng.NextSingle() * 200f;

                var p = particlePool.Get();
                p.X = vis.X; p.Y = vis.Y;
                p.VelX = MathF.Cos(angle) * speed;
                p.VelY = MathF.Sin(angle) * speed - 100f;
                p.Life = 0.4f + rng.NextSingle() * 0.3f;
                p.MaxLife = 0.7f;
                p.Size = 2f + rng.NextSingle() * 4f;
                p.Color = color;
                particles.Add(p);
            }
        }
    }

    public void Render(SpriteBatch batch, TextRenderer text, float time)
    {
        foreach (var d in destroyers)
        {
            float alpha = Math.Clamp(d.Life * 3f, 0f, 1f);
            var col = new Vector4(d.Color.X, d.Color.Y, d.Color.Z, alpha);
            var glow = new Vector4(col.X, col.Y, col.Z, alpha * 0.3f);
            const float SZ = 8f;

            if (d.Horizontal)
            {
                batch.DrawRect(d.X - SZ * 2f, d.Y - SZ * 0.5f, SZ * 4f, SZ, col);
                batch.DrawRect(d.X - SZ * 3f, d.Y - SZ * 0.3f, SZ * 6f, SZ * 0.6f, glow);
            }
            else
            {
                batch.DrawRect(d.X - SZ * 0.5f, d.Y - SZ * 2f, SZ, SZ * 4f, col);
                batch.DrawRect(d.X - SZ * 0.3f, d.Y - SZ * 3f, SZ * 0.6f, SZ * 6f, glow);
            }
        }

        foreach (var ex in explosions)
        {
            float progress = 1f - ex.Timer / ex.MaxTimer;
            float r = ex.Radius * Easing.QuadOut(progress);
            float alpha = (1f - progress) * 0.6f;
            batch.DrawCircle(ex.X, ex.Y, r, new Vector4(1f, 0.8f, 0.3f, alpha), 20);
            batch.DrawCircle(ex.X, ex.Y, r * 0.6f, new Vector4(1f, 0.95f, 0.7f, alpha * 0.5f), 16);
        }

        foreach (var p in particles)
        {
            float alpha = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            batch.DrawRect(p.X - p.Size * 0.5f, p.Y - p.Size * 0.5f, p.Size, p.Size,
                new Vector4(p.Color.X, p.Color.Y, p.Color.Z, alpha));
        }
    }

    private static void UpdateAndRecycle<T>(
        List<T> active, ObjectPool<T> pool, Func<T, bool> updateAndKeep) where T : class
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (!updateAndKeep(active[i]))
            {
                pool.Return(active[i]);
                active.RemoveAt(i);
            }
        }
    }

    private static void ReturnAll<T>(List<T> active, ObjectPool<T> pool) where T : class
    {
        foreach (var item in active)
            pool.Return(item);
        active.Clear();
    }
}
