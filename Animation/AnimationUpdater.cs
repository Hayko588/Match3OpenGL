using Match3.Application;
using Match3.Domain;
using Match3.Rendering;

namespace Match3.Animation
{
    public class AnimationUpdater(BoardLayout layout)
    {
        public void Update(GameState state, float easedT)
        {
            switch (state.Phase)
            {
                case AnimationPhase.Swapping:
                case AnimationPhase.SwapBack:
                    InterpolateSwap(state, easedT);
                    break;
                case AnimationPhase.Destroying:
                    InterpolateDestroy(state.DestroySet, state.Visuals, easedT);
                    break;
                case AnimationPhase.BombExploding:
                    InterpolateDestroy(state.BombDestroySet, state.Visuals, easedT);
                    break;
                case AnimationPhase.Gravity:
                    InterpolateFall(state.FallData, easedT);
                    break;
                case AnimationPhase.Spawning:
                    InterpolateFall(state.SpawnData, easedT);
                    break;
            }
        }

        private void InterpolateSwap(GameState state, float t)
        {
            var p1 = layout.CellCenter(state.SwapFrom);
            var p2 = layout.CellCenter(state.SwapTo);

            var v1 = state.Visuals[state.SwapFrom.Row, state.SwapFrom.Col];
            if (v1 != null)
            {
                v1.X = p1.X + (p2.X - p1.X) * t;
                v1.Y = p1.Y + (p2.Y - p1.Y) * t;
            }

            var v2 = state.Visuals[state.SwapTo.Row, state.SwapTo.Col];
            if (v2 != null)
            {
                v2.X = p2.X + (p1.X - p2.X) * t;
                v2.Y = p2.Y + (p1.Y - p2.Y) * t;
            }
        }

        private static void InterpolateDestroy(HashSet<Pos> positions, GemVisual?[,] visuals, float t)
        {
            foreach (var pos in positions)
            {
                var vis = visuals[pos.Row, pos.Col];
                if (vis == null) continue;
                vis.Scale = 1f - t;
                vis.Alpha = 1f - t;
            }
        }

        private static void InterpolateFall(List<(GemVisual Vis, float FromY, float ToY)> data, float t)
        {
            foreach (var (vis, fromY, toY) in data)
                vis.Y = fromY + (toY - fromY) * t;
        }
    }
}