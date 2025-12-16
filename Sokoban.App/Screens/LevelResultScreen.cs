using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Sokoban.App.Screens;

public sealed class LevelResultScreen : IGameScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont font;

    private readonly string levelId;
    private readonly int steps;
    private readonly int timeMs;

    private readonly int? bestProfileSteps;
    private readonly int? bestProfileTimeMs;

    private readonly string? bestGlobalPlayerName;
    private readonly int? bestGlobalSteps;
    private readonly int? bestGlobalTimeMs;

    // 1x1 texture for drawing lines/rectangles
    private readonly Texture2D pixel;

    public LevelResultScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont font,
        string levelId,
        int steps,
        int timeMs,
        int? bestProfileSteps,
        int? bestProfileTimeMs,
        string? bestGlobalPlayerName,
        int? bestGlobalSteps,
        int? bestGlobalTimeMs)
    {
        this.graphicsDevice = graphicsDevice;
        this.font = font;

        this.levelId = levelId;
        this.steps = steps;
        this.timeMs = timeMs;

        this.bestProfileSteps = bestProfileSteps;
        this.bestProfileTimeMs = bestProfileTimeMs;

        this.bestGlobalPlayerName = bestGlobalPlayerName;
        this.bestGlobalSteps = bestGlobalSteps;
        this.bestGlobalTimeMs = bestGlobalTimeMs;

        pixel = new Texture2D(graphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
    }

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (IsPressed(Keys.Escape, current, previous) || IsPressed(Keys.Enter, current, previous))
            return new ScreenCommand(ScreenCommandType.GoToLevelSelection);

        return ScreenCommand.None;
    }


    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = graphicsDevice.Viewport;
        var bounds = new Rectangle(0, 0, viewport.Width, viewport.Height);

        // Build the full text block first, then measure it.
        var text = BuildText();
        var measured = font.MeasureString(text);

        // Safe margins so the border doesn't touch screen edges
        const float screenMargin = 20f;

        // Padding inside the box around the text
        const float boxPadding = 20f;

        var targetWidth = bounds.Width - 2f * screenMargin;
        var targetHeight = bounds.Height - 2f * screenMargin;

        // Box size in "unscaled" pixels
        var unscaledBoxWidth = measured.X + 2f * boxPadding;
        var unscaledBoxHeight = measured.Y + 2f * boxPadding;

        // Scale down only when needed
        var scaleX = targetWidth / unscaledBoxWidth;
        var scaleY = targetHeight / unscaledBoxHeight;
        var scale = MathF.Min(1f, MathF.Min(scaleX, scaleY));

        var boxSize = new Vector2(unscaledBoxWidth, unscaledBoxHeight) * scale;

        var boxTopLeft = new Vector2(
            (bounds.Width - boxSize.X) * 0.5f,
            (bounds.Height - boxSize.Y) * 0.5f);

        // Draw box background (slightly transparent)
        var boxRect = new Rectangle(
            (int)boxTopLeft.X,
            (int)boxTopLeft.Y,
            (int)boxSize.X,
            (int)boxSize.Y);

        DrawFilledRect(spriteBatch, boxRect, new Color(0, 0, 0, 220));

        // Draw border (2px relative to scale, but at least 1px)
        var borderThickness = Math.Max(1, (int)MathF.Round(2f * scale));
        DrawRect(spriteBatch, boxRect, Color.White, borderThickness);

        // Draw text inside with padding, scaled
        var textPos = boxTopLeft + new Vector2(boxPadding, boxPadding) * scale;
        spriteBatch.DrawString(font, text, textPos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        // Unified hint at bottom
        var hint = "ENTER - continue   ESC - levels";
        var hintSize = font.MeasureString(hint);
        var hintScale = MathF.Min(1f, (bounds.Width - 2f * screenMargin) / hintSize.X);

        var hintPos = new Vector2(
            (bounds.Width - hintSize.X * hintScale) * 0.5f,
            bounds.Height - screenMargin - hintSize.Y * hintScale);

        spriteBatch.DrawString(font, hint, hintPos, new Color(220, 220, 220), 0f, Vector2.Zero, hintScale, SpriteEffects.None, 0f);
    }

    private string BuildText()
    {
        var yourTime = FormatTimeSeconds(timeMs);
        var bestProfileTime = bestProfileTimeMs.HasValue ? FormatTimeSeconds(bestProfileTimeMs.Value) : "-";
        var bestProfileStepsText = bestProfileSteps.HasValue ? bestProfileSteps.Value.ToString() : "-";

        var globalName = string.IsNullOrWhiteSpace(bestGlobalPlayerName) ? "-" : bestGlobalPlayerName;
        var globalSteps = bestGlobalSteps.HasValue ? bestGlobalSteps.Value.ToString() : "-";
        var globalTime = bestGlobalTimeMs.HasValue ? FormatTimeSeconds(bestGlobalTimeMs.Value) : "-";

        // Keep it monolithic to ensure MeasureString matches exactly what we draw.
        return
            "LEVEL COMPLETED\n\n" +
            $"Your time:  {yourTime}\n" +
            $"Your steps: {steps}\n\n" +
            "Your best on this level:\n" +
            $"  Time:  {bestProfileTime}\n" +
            $"  Steps: {bestProfileStepsText}\n\n" +
            "Global best on this level:\n" +
            $"  Player: {globalName}\n" +
            $"  Steps:  {globalSteps}\n" +
            $"  Time:   {globalTime}\n";
    }

    private static string FormatTimeSeconds(int timeMs)
    {
        // Show like "2.3s" (same style as on your screenshot)
        var seconds = timeMs / 1000f;
        return $"{seconds:0.0}s";
    }

    private static bool IsPressed(Keys key, KeyboardState current, KeyboardState previous)
    {
        return current.IsKeyDown(key) && previous.IsKeyUp(key);
    }

    private void DrawFilledRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, rect, color);
    }

    private void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        // top
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // bottom
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        // left
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // right
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
