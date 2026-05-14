using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

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
    void OP_8xy0() // LD Vx, Vy: set Vx = Vy
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);
      V[Vx] = V[Vy];
    }
    void OP_8xy1() // OR Vx, Vy: Set Vx = Vx OR Vy
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);
      V[Vx] |= V[Vy];
    }
    void OP_8xy2() // AND Vx, Vy: Set Vx = Vx AND Vy
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);
      V[Vx] &= V[Vy];
    }
    void OP_8xy3() // XOR Vx, Vy: Set Vx = Vx XOR Vy
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);
      V[Vx] ^= V[Vy];
    }
    void OP_8xy4() // ADD Vx, Vy: Set Vx = Vx + Vy, set VF = carry
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);
      ushort sum = (ushort)(V[Vx] + V[Vy]);
      V[0xF] = (sum > 255u) ? (byte)1u : (byte)0u;
      V[Vx] = (byte)sum;
    }
    void OP_8xy5() // SUB Vx, Vy: Set Vx = Vx - Vy, set VF = !borrow
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);
      V[0xF] = (V[Vx] > V[Vy]) ? (byte)1u : (byte)0u;
      V[Vx] -= V[Vy];
    }
    void OP_8xy6() { // SHR Vx: Set Vx = SHR 1
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      // save LSB in VF
      V[0xF] = (byte)(V[Vx] & 0x1u);
      V[Vx] >>= 1;
    }
    void OP_8xy7() // SUBN Vx, Vy: Set Vx = Vy - Vx, set VF = !borrow
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);
      V[0xF] = (V[Vy] > V[Vx]) ? (byte)1u : (byte)0u;
      V[Vx] = (byte)(V[Vy] - V[Vx]);
    }
    void OP_8xyE() // SHL Vx: Set Vx = Vx SHL 1
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      // save MSB in VF
      V[0xF] = (byte)((V[Vx] & 0x80u) >> 7);
      V[Vx] <<= 1;
    }
    void OP_9xy0() // SNE Vx, Vy: Skip next instruction if Vx != Vy
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);
      if (V[Vx] != V[Vy])
      {
        PC += 2;
      }
    }
    void OP_Annn() // LD I, addr: Set I = nnn
    {
      ushort addr = (ushort)(opcode & 0x0FFFu);
      I = addr;
    }
    void OP_Bnnn() // JP V0, addr: Jump to location nnn + V0
    {
      ushort addr = (ushort)(opcode & 0x0FFFu);
      PC = (ushort)(addr + V[0]);
    }
    void OP_Cxkk() // RND Vx, byte(kk): Set Vx = random byte AND kk
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte kk = (byte)(opcode & 0x00FFu);
      Random rand = new Random();
      V[Vx] = (byte)(rand.Next(0, 256) & kk);
    }
    void OP_Dxyn() // DRW Vx, Vy, nibble: Display n-byte sprite starting at memory location I at (Vx, Vy), set VF = collision
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte Vy = (byte)((opcode & 0x00F0u) >> 4);
      byte height = (byte)(opcode & 0x000Fu);
      
      byte xPos = (byte)(V[Vx] % SCREEN_WIDTH);
      byte yPos = (byte)(V[Vy] % SCREEN_HEIGHT);

      V[0xF] = 0;

      for (uint row = 0; row < height; row++)
      {
        byte spriteByte = Memory[I + row];
        for (int col = 0; col < 8; col++)
        {
          byte spritePixel = (byte)(spriteByte & (0x80u >> col));
          if (spritePixel != 0)
          {
            byte x = (byte)((xPos + col) % SCREEN_WIDTH);
            byte y = (byte)((yPos + row) % SCREEN_HEIGHT);
            if (Display[x, y])
            {
              V[0xF] = 1;
            }
            Display[x, y] ^= true;
          }
        }
      }
    }
    void OP_Ex9E() // SKP Vx: Skip next instruction if key with the value of Vx is pressed
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte key = V[Vx];
      if (Keys[key])
      {
        PC += 2;
      }
    }
    void OP_ExA1() // SKNP Vx: Skip next instruction if key with the value of Vx is not pressed
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte key = V[Vx];
      if (!Keys[key])
      {
        PC += 2;
      }
    }
    void OP_Fx07() // LD Vx, DT: Set Vx = delay timer value
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      V[Vx] = DelayTimer;
    }
    void OP_Fx0A() // LD Vx, K: Wait for a key press, store the value of the key in Vx
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      if (Keys[0]) { V[Vx] = 0; }
      else if (Keys[1]) { V[Vx] = 1; }
      else if (Keys[2]) { V[Vx] = 2; }
      else if (Keys[3]) { V[Vx] = 3; }
      else if (Keys[4]) { V[Vx] = 4; }
      else if (Keys[5]) { V[Vx] = 5; }
      else if (Keys[6]) { V[Vx] = 6; }
      else if (Keys[7]) { V[Vx] = 7; }
      else if (Keys[8]) { V[Vx] = 8; }
      else if (Keys[9]) { V[Vx] = 9; }
      else if (Keys[10]) { V[Vx] = 10; }
      else if (Keys[11]) { V[Vx] = 11; }
      else if (Keys[12]) { V[Vx] = 12; }
      else if (Keys[13]) { V[Vx] = 13; }
      else if (Keys[14]) { V[Vx] = 14; }
      else if (Keys[15]) { V[Vx] = 15; }
      else { PC -= 2; }
    }
    void OP_Fx15() // LD DT, Vx: Set delay timer = Vx
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      DelayTimer = V[Vx];
    }
    void OP_Fx18() // LD ST, Vx: Set sound timer = V
    {
      // Uncommented, not implementing sound
      // byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      // SoundTimer = V[Vx];
    }
    void OP_Fx1E() // ADD I, Vx: Set I = I + Vx
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      I += V[Vx];
    }
    void OP_Fx29() // LD F, Vx: Set I = location of sprite for digit Vx
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte digit = V[Vx];
      I = (ushort)(FONTSET_START_ADDRESS + (digit * 5));
    }
    void OP_Fx33() // LD B, Vx: Store BCD representation of Vx in memory locations I, I+1, and I+2
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      byte value = V[Vx];
      Memory[I + 2] = (byte)(value % 10);
      value /= 10;
      Memory[I + 1] = (byte)(value % 10);
      value /= 10;
      Memory[I] = (byte)(value % 10); 
    }
    void OP_Fx55() // LD [I], Vx: Store registers V0 through Vx in memory starting at location I
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      for (int i = 0; i <= Vx; i++)
      {
        Memory[I + i] = V[i];
      }
    }
    void OP_Fx65() // LD Vx, [I]: Read registers V0 through Vx from memory starting at location I
    {
      byte Vx = (byte)((opcode & 0x0F00u) >> 8);
      for (int i = 0; i <= Vx; i++)
      {
        V[i] = Memory[I + i];
      }
    }
    public void Cycle()
    {
      opcode = (ushort)((Memory[PC] << 8) | Memory[PC + 1]);
      PC += 2;
      if (opcode == 0x00E0)
      {
        OP_00E0();
      }
      else if (opcode == 0x00EE)
      {
        OP_00EE();
      }
      else if ((opcode & 0xF000) == 0x1000)
      {
        OP_1nnn();
      }
      else if ((opcode & 0xF000) == 0x2000)
      {
        OP_2nnn();
      }
      else if ((opcode & 0xF000) == 0x3000)
      {
        OP_3xkk();
      }
      else if ((opcode & 0xF000) == 0x4000)
      {
        OP_4xkk();
      }
      else if ((opcode & 0xF000) == 0x5000)
      {
        OP_5xy0();
      }
      else if ((opcode & 0xF000) == 0x6000)
      {
        OP_6xkk();
      }
      else if ((opcode & 0xF000) == 0x7000)
      {
        OP_7xkk();
      }
      else if ((opcode & 0xF000) == 0x8000)
      {
        switch (opcode & 0x000Fu)
        {
          case 0: OP_8xy0(); break;
          case 1: OP_8xy1(); break;
          case 2: OP_8xy2(); break;
          case 3: OP_8xy3(); break;
          case 4: OP_8xy4(); break;
          case 5: OP_8xy5(); break;
          case 6: OP_8xy6(); break;
          case 7: OP_8xy7(); break;
          case 0xE: OP_8xyE(); break;
        }
      }
      else if ((opcode & 0xF000) == 0x9000)
      {
        OP_9xy0();
      }
      else if ((opcode & 0xF000) == 0xA000)
      {
        OP_Annn();
      }
      else if ((opcode & 0xF000) == 0xB000)
      {
        OP_Bnnn();
      }
      else if ((opcode & 0xF000) == 0xC000)
      {
        OP_Cxkk();
      }
      else if ((opcode & 0xF000) == 0xD000)
      {
        OP_Dxyn();
      }
      else if ((opcode & 0xF000) == 0xE000)
      {
        switch (opcode & 0x00FFu)
        {
          case 0x9E: OP_Ex9E(); break;
          case 0xA1: OP_ExA1(); break;
        }
      }
      else if ((opcode & 0xF000) == 0xF000)
      {
        switch (opcode & 0x00FFu)
        {
          case 0x07: OP_Fx07(); break;
          case 0x0A: OP_Fx0A(); break;
          case 0x15: OP_Fx15(); break;
          case 0x18: OP_Fx18(); break;
          case 0x1E: OP_Fx1E(); break;
          case 0x29: OP_Fx29(); break;
          case 0x33: OP_Fx33(); break;
          case 0x55: OP_Fx55(); break;
          case 0x65: OP_Fx65(); break;
        }
      }
      if (DelayTimer > 0)
      {
        DelayTimer--;
      }
    }
  }
}
