using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SuperliminalTAS
{
    /// <summary>
    /// Binary format read/write. Keeps DemoRecorder free of serialization clutter.
    /// </summary>
    public static class DemoSerializer
    {
        private const string Magic = "SUPERLIMINALTAS1";

        public static byte[] Serialize(DemoData data)
        {
            if (data == null || data.FrameCount < 1) return null;

            var magicBytes = Encoding.ASCII.GetBytes(Magic);
            var lengthBytes = BitConverter.GetBytes(data.FrameCount);

            using var ms = new MemoryStream(capacity: 16 + 4 + (data.FrameCount * ((4 * DemoActions.Axes.Length) + (3 * 3)))); // rough

            ms.Write(magicBytes, 0, magicBytes.Length);
            ms.Write(lengthBytes, 0, lengthBytes.Length);

            // Axes (float32 * length)
            foreach (var a in DemoActions.Axes)
                WriteFloatList(ms, data.Axes[a]);

            // Buttons (bool * length)
            foreach (var b in DemoActions.Buttons)
                WriteBoolList(ms, data.Buttons[b]);

            // ButtonDown
            foreach (var b in DemoActions.Buttons)
                WriteBoolList(ms, data.ButtonsDown[b]);

            // ButtonUp
            foreach (var b in DemoActions.Buttons)
                WriteBoolList(ms, data.ButtonsUp[b]);

            return ms.ToArray();
        }

        public static DemoData Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 20)
                throw new InvalidDataException("File too small.");

            using var ms = new MemoryStream(bytes);
            using var br = new BinaryReader(ms);

            var magicBytes = br.ReadBytes(16);
            var magic = Encoding.ASCII.GetString(magicBytes);
            if (magic != Magic)
                throw new InvalidDataException($"Bad magic header: '{magic}'.");

            int length = br.ReadInt32();
            if (length < 0) throw new InvalidDataException("Negative length.");

            var axes = new Dictionary<string, List<float>>(DemoActions.Axes.Length);
            foreach (var a in DemoActions.Axes)
                axes[a] = ReadFloatList(br, length);

            var buttons = new Dictionary<string, List<bool>>(DemoActions.Buttons.Length);
            foreach (var b in DemoActions.Buttons)
                buttons[b] = ReadBoolList(br, length);

            var buttonsDown = new Dictionary<string, List<bool>>(DemoActions.Buttons.Length);
            foreach (var b in DemoActions.Buttons)
                buttonsDown[b] = ReadBoolList(br, length);

            var buttonsUp = new Dictionary<string, List<bool>>(DemoActions.Buttons.Length);
            foreach (var b in DemoActions.Buttons)
                buttonsUp[b] = ReadBoolList(br, length);

            var data = DemoData.CreateEmpty();
            data.ReplaceAll(axes, buttons, buttonsDown, buttonsUp);
            return data;
        }

        private static void WriteFloatList(Stream s, List<float> list)
        {
            // Faster than per-item MemoryStream writes.
            var buffer = new byte[list.Count * 4];
            for (int i = 0; i < list.Count; i++)
                Buffer.BlockCopy(BitConverter.GetBytes(list[i]), 0, buffer, i * 4, 4);

            s.Write(buffer, 0, buffer.Length);
        }

        private static void WriteBoolList(Stream s, List<bool> list)
        {
            var buffer = new byte[list.Count];
            for (int i = 0; i < list.Count; i++)
                buffer[i] = list[i] ? (byte)1 : (byte)0;

            s.Write(buffer, 0, buffer.Length);
        }

        private static List<float> ReadFloatList(BinaryReader br, int length)
        {
            var buffer = br.ReadBytes(length * 4);
            if (buffer.Length != length * 4)
                throw new EndOfStreamException("Unexpected EOF while reading floats.");

            var result = new List<float>(length);
            for (int i = 0; i < length; i++)
                result.Add(BitConverter.ToSingle(buffer, i * 4));

            return result;
        }

        private static List<bool> ReadBoolList(BinaryReader br, int length)
        {
            var buffer = br.ReadBytes(length);
            if (buffer.Length != length)
                throw new EndOfStreamException("Unexpected EOF while reading bools.");

            var result = new List<bool>(length);
            for (int i = 0; i < length; i++)
                result.Add(buffer[i] != 0);

            return result;
        }
    }
}
