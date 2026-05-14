using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace monochip8;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Chip8 _chip8;
    private Texture2D _displayTexture; // 64x32 CHIP-8 display
    private readonly int _scale = 5;  // scale factor: 64*10=640, 32*10=320
    private KeyboardState _previousKeyboardState; // track previous frame for key release detection
    private Color[] _displayColors; // 64x32 pixel buffer for the display texture
    private const double CHIP8_HZ = 500.0; // CHIP-8 CPU runs at ~500 Hz
    private const double CYCLE_DURATION = 1.0 / CHIP8_HZ; // 0.002s per instruction
    private double _accumulator = 0.0; // accumulates elapsed time for fixed-timestep loop
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Set window size to CHIP-8 display scaled up
        _graphics.PreferredBackBufferWidth = 64 * _scale;
        _graphics.PreferredBackBufferHeight = 32 * _scale;
        _graphics.ApplyChanges();

        _chip8 = new Chip8();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create a 64x32 texture for the CHIP-8 display.
        // Each pixel will be set to white (on) or black (off).
        _displayTexture = new Texture2D(GraphicsDevice, 64, 32);

        // Pre-allocate the color buffer (64 * 32 = 2048 pixels)
        _displayColors = new Color[64 * 32];

        // Load the CHIP-8 ROM into the emulator
        _chip8.LoadROM("snake.ch8");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        KeyboardState keyboardState = Keyboard.GetState();

        // Map physical keyboard keys to CHIP-8 key indices (0–15)
        // CHIP-8 keypad layout:
        //   1  2  3  C
        //   4  5  6  D
        //   7  8  9  E
        //   A  0  B  F
        var keyMap = new Dictionary<Keys, byte>
        {
            { Keys.D1, 0x1 }, { Keys.D2, 0x2 }, { Keys.D3, 0x3 }, { Keys.D4,    0xC },
            { Keys.Q, 0x4 }, { Keys.W, 0x5 }, { Keys.E, 0x6 }, { Keys.R,    0xD },
            { Keys.A, 0x7 }, { Keys.S, 0x8 }, { Keys.D, 0x9 }, { Keys.F,    0xE },
            { Keys.Z, 0xA }, { Keys.X, 0x0 }, { Keys.C, 0xB }, { Keys.V,    0xF }
        };

        // Update CHIP-8 key state: check both current and previous frame
        foreach (var entry in keyMap)
        {
            bool isPressed = keyboardState.IsKeyDown(entry.Key);
            _chip8.Keys[entry.Value] = isPressed;
        }

        // Clear any CHIP-8 keys not in our map (e.g. keys the user doesn't care about)
        for (int i = 0; i < 16; i++)
        {
            if (!keyMap.ContainsValue((byte)i))
            {
                _chip8.Keys[i] = false;
            }
        }

        _previousKeyboardState = keyboardState;

        // Fixed-timestep emulation loop: run CHIP-8 cycles at a constant 500 Hz
        // regardless of the render frame rate.
        _accumulator += gameTime.ElapsedGameTime.TotalSeconds;
        while (_accumulator >= CYCLE_DURATION)
        {
            _chip8.Cycle();
            _accumulator -= CYCLE_DURATION;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // Copy CHIP-8 display state into the color buffer
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                int index = y * 64 + x;
                _displayColors[index] = _chip8.Display[x, y] ? Color.White : Color.Black;
            }
        }

        // Push the color buffer into the texture
        _displayTexture.SetData(_displayColors);

        // Draw the 64x32 texture scaled up to 640x320
        // SamplerState.Point = nearest-neighbor filtering (sharp pixels)
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(
            _displayTexture,
            new Rectangle(0, 0, 64 * _scale, 32 * _scale),
            Color.White
        );
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
