using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Sokoban.App.Screens;

public interface IGameScreen
{
    ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous);
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}