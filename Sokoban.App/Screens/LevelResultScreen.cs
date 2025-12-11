using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Sokoban.App.Screens;

public sealed class LevelResultScreen : IGameScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D overlayTexture;

    private readonly string levelId;
    private readonly int steps;
    private readonly int elapsedMilliseconds;

    private readonly int? bestProfileSteps;
    private readonly int? bestProfileMilliseconds;

    private readonly string? bestGlobalPlayerName;
    private readonly int? bestGlobalSteps;
    private readonly int? bestGlobalMilliseconds;

    public LevelResultScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        string levelId,
        int steps,
        int elapsedMilliseconds,
        int? bestProfileSteps,
        int? bestProfileMilliseconds,
        string? bestGlobalPlayerName,
        int? bestGlobalSteps,
        int? bestGlobalMilliseconds)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;

        this.levelId = levelId;
        this.steps = steps;
        this.elapsedMilliseconds = elapsedMilliseconds;

        this.bestProfileSteps = bestProfileSteps;
        this.bestProfileMilliseconds = bestProfileMilliseconds;

        this.bestGlobalPlayerName = bestGlobalPlayerName;
        this.bestGlobalSteps = bestGlobalSteps;
        this.bestGlobalMilliseconds = bestGlobalMilliseconds;

        overlayTexture = new Texture2D(graphicsDevice, 1, 1);
        overlayTexture.SetData(new[] { Color.White });
    }

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (IsConfirmPressed(current, previous) || IsExitPressed(current, previous))
            return new ScreenCommand(ScreenCommandType.GoToLevelSelection);

        return ScreenCommand.None;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var backBuffer = graphicsDevice.PresentationParameters;
        var screenWidth = backBuffer.BackBufferWidth;
        var screenHeight = backBuffer.BackBufferHeight;

        DrawOverlay(spriteBatch, screenWidth, screenHeight);
        DrawWindow(spriteBatch, screenWidth, screenHeight);
    }

    private void DrawOverlay(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
    {
        var fullscreen = new Rectangle(0, 0, screenWidth, screenHeight);
        var backgroundColor = new Color(0, 0, 0, 180);
        spriteBatch.Draw(overlayTexture, fullscreen, backgroundColor);
    }

    private void DrawWindow(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
    {
        var lines = BuildLines();
        var lineSpacing = 8f;

        var maxLineWidth = 0f;
        foreach (var line in lines)
        {
            var size = uiFont.MeasureString(line);
            if (size.X > maxLineWidth)
                maxLineWidth = size.X;
        }

        var padding = 24f;
        var windowWidth = maxLineWidth + padding * 2f;
        var windowHeight = lines.Count * uiFont.LineSpacing + (lines.Count - 1) * lineSpacing + padding * 2f;

        if (windowWidth > screenWidth - 40f)
            windowWidth = screenWidth - 40f;

        if (windowHeight > screenHeight - 40f)
            windowHeight = screenHeight - 40f;

        var windowX = (screenWidth - windowWidth) / 2f;
        var windowY = (screenHeight - windowHeight) / 2f;

        var windowRect = new Rectangle(
            (int)windowX,
            (int)windowY,
            (int)windowWidth,
            (int)windowHeight);

        var windowColor = new Color(20, 20, 20, 240);
        spriteBatch.Draw(overlayTexture, windowRect, windowColor);

        var borderThickness = 2;
        var borderColor = new Color(200, 200, 200);

        DrawBorder(spriteBatch, windowRect, borderThickness, borderColor);

        var currentY = windowY + padding;

        foreach (var line in lines)
        {
            var truncated = TruncateToFit(line, windowWidth - padding * 2f);
            var size = uiFont.MeasureString(truncated);
            var x = windowX + (windowWidth - size.X) / 2f;

            spriteBatch.DrawString(uiFont, truncated, new Vector2(x, currentY), Color.White);
            currentY += uiFont.LineSpacing + lineSpacing;
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
    {
        var top = new Rectangle(rect.Left, rect.Top, rect.Width, thickness);
        var bottom = new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness);
        var left = new Rectangle(rect.Left, rect.Top, thickness, rect.Height);
        var right = new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height);

        spriteBatch.Draw(overlayTexture, top, color);
        spriteBatch.Draw(overlayTexture, bottom, color);
        spriteBatch.Draw(overlayTexture, left, color);
        spriteBatch.Draw(overlayTexture, right, color);
    }

    private List<string> BuildLines()
    {
        var lines = new List<string>();

        lines.Add("LEVEL COMPLETED");
        lines.Add(string.Empty);

        var seconds = elapsedMilliseconds / 1000.0;
        var yourTime = $"Your time: {seconds:0.0}s";
        var yourSteps = $"Your steps: {steps}";

        lines.Add(yourTime);
        lines.Add(yourSteps);
        lines.Add(string.Empty);

        if (bestProfileMilliseconds.HasValue || bestProfileSteps.HasValue)
        {
            var bestProfileTimeText = bestProfileMilliseconds.HasValue
                ? $"{bestProfileMilliseconds.Value / 1000.0:0.0}s"
                : "—";

            var bestProfileStepsText = bestProfileSteps.HasValue
                ? bestProfileSteps.Value.ToString()
                : "—";

            lines.Add("Your best on this level:");
            lines.Add($"  Time:  {bestProfileTimeText}");
            lines.Add($"  Steps: {bestProfileStepsText}");
            lines.Add(string.Empty);
        }

        if (bestGlobalSteps.HasValue || bestGlobalMilliseconds.HasValue)
        {
            var bestGlobalTimeText = bestGlobalMilliseconds.HasValue
                ? $"{bestGlobalMilliseconds.Value / 1000.0:0.0}s"
                : "—";

            var bestGlobalStepsText = bestGlobalSteps.HasValue
                ? bestGlobalSteps.Value.ToString()
                : "—";

            var playerName = string.IsNullOrWhiteSpace(bestGlobalPlayerName)
                ? "Unknown player"
                : bestGlobalPlayerName;

            lines.Add("Global best on this level:");
            lines.Add($"  Player: {playerName}");
            lines.Add($"  Steps:  {bestGlobalStepsText}");
            lines.Add($"  Time:   {bestGlobalTimeText}");
            lines.Add(string.Empty);
        }

        lines.Add("ENTER / SPACE - continue");
        lines.Add("Q / ESC - levels");

        return lines;
    }

    private string TruncateToFit(string text, float maxWidth)
    {
        var width = uiFont.MeasureString(text).X;
        if (width <= maxWidth)
            return text;

        const string ellipsis = "...";
        var ellipsisWidth = uiFont.MeasureString(ellipsis).X;

        var maxTextWidth = maxWidth - ellipsisWidth;
        if (maxTextWidth <= 0)
            return ellipsis;

        var result = text;
        while (result.Length > 0)
        {
            result = result.Substring(0, result.Length - 1);
            var candidate = result + ellipsis;
            if (uiFont.MeasureString(candidate).X <= maxTextWidth)
                return candidate;
        }

        return ellipsis;
    }

    private static bool IsConfirmPressed(KeyboardState current, KeyboardState previous)
    {
        return IsKeyPressed(Keys.Enter, current, previous) ||
               IsKeyPressed(Keys.Space, current, previous);
    }

    private static bool IsExitPressed(KeyboardState current, KeyboardState previous)
    {
        return IsKeyPressed(Keys.Q, current, previous) ||
               IsKeyPressed(Keys.Escape, current, previous);
    }

    private static bool IsKeyPressed(Keys key, KeyboardState current, KeyboardState previous)
    {
        return current.IsKeyDown(key) && !previous.IsKeyDown(key);
    }
}
