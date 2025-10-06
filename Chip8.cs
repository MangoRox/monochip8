using System;
using System.IO;

namespace monochip8
{
  public class Chip8
  {
    // private font related members
    const uint START_ADDRESS = 0x200;
    const uint FONTSET_SIZE = 80;
    const uint FONTSET_START_ADDRESS = 0x50;
    readonly ushort[] fontset =
    [
      0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
      0x20, 0x60, 0x20, 0x20, 0x70, // 1
      0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
      0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
      0x90, 0x90, 0xF0, 0x10, 0x10, // 4
      0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
      0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
      0xF0, 0x10, 0x20, 0x40, 0x40, // 7
      0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
      0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
      0xF0, 0x90, 0xF0, 0x90, 0x90, // A
      0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
      0xF0, 0x80, 0x80, 0x80, 0xF0, // C
      0xE0, 0x90, 0x90, 0x90, 0xE0, // D
      0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
      0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    ];
    // display dimension constants
    const byte SCREEN_WIDTH = 64;
    const byte SCREEN_HEIGHT = 32;
    //  memory related members
    public byte[] V = new byte[16];               // V registers
    public byte[] Memory = new byte[4096];        // 4 KiB memory
    public ushort I;                              // index register
    public ushort PC;                             // program counter
    public bool[,] Display = new bool[64, 32];    // display pixel values
    public ushort[] Stack = new ushort[16];
    public byte SP;                               // stack pointer
    public byte DelayTimer;
    // public byte SoundTimer; // not going to implement sound...
    public bool[] Keys = new bool[16];            // flags for when keys are pressed
    public ushort opcode;



    public Chip8() // set PC to start address on init
    {
      PC = (ushort)START_ADDRESS;
    }
    public void LoadROM(string filename) // Load ROM from file
    {
      try
      {
        byte[] fileBytes = File.ReadAllBytes(filename);
        for (int i = 0; i < fileBytes.Length; i++)
        {
          Memory[START_ADDRESS + i] = fileBytes[i];
        }

        for (uint i = 0; i < FONTSET_SIZE; i++)
        {
          Memory[FONTSET_START_ADDRESS + i] = (byte)fontset[i];
        }
      }
      catch (IOException ex)
      {
        Console.WriteLine($"Unable to read file: {ex.Message}");
      }
    }
    void OP_00E0() // CLS: CLEAR THE DISPLAY
    {
      for (int i = 0; i < SCREEN_WIDTH; i++)
      {
        for (int j = 0; j < SCREEN_HEIGHT; j++)
        {
          Display[i, j] = false;
        }
      }
    }
    void OP_00EE() // RET: RETURN FROM A SUBROUTINE 
    { 
      --SP;
      PC = Stack[SP];
    }
    void OP_1nnn() // JP addr: Jump to location
    { 
      ushort addr = (ushort)(opcode & 0x0FFFu);
      PC = addr;
    }
    void OP_2nnn() // CALL addr: Call subroutine at nnn;
    {
      ushort addr = (ushort)(opcode & 0x0FFFu);
      Stack[SP] = PC;
      ++SP;
      PC = addr;
    }
    void OP_3xkk() // SE Vx, byte(kk): Skip next instr if Vx == kk.
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte kk = (byte)(opcode & 0x00FFu);
      if (V[Vx] == kk)
      {
        PC += 2;
      }
    }
    void OP_4xkk() // SNE Vx, byte(kk): Skip next instr if Vx != kk 
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte kk = (byte)(opcode & 0x00FFu);
      if (V[Vx] != kk)
      {
        PC += 2;
      }
    }
    void OP_5xy0() // SE Vx, Vy: Skip next instr if Vx == Vy
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);

      if (V[Vx] == V[Vy])
      {
        PC += 2;
      }
    }
    void OP_6xkk() // LD Vx, byte(kk) : Set Vx = kk 
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte kk = (byte)(opcode & 0x00FFu);

      V[Vx] = kk;
    }
    void OP_7xkk() // ADD Vx, byte(kk) : Set Vx = Vx + kk
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte kk = (byte)(opcode & 0x00FFu);

      V[Vx] += kk;
    }
  }
}
