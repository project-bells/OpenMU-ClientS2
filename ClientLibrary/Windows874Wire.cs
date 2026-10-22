// <copyright file="Windows874Wire.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.Client.Library;

using System;

/// <summary>
/// Encodes classic ten-byte identity fields using the ASCII-compatible Thai
/// Windows-874 layout. This avoids UTF-8 truncation without changing packet sizes.
/// </summary>
internal static class Windows874Wire
{
    /// <summary>
    /// Writes a null-padded identity field without changing the classic packet size.
    /// </summary>
    /// <param name="destination">The packet field to fill.</param>
    /// <param name="value">The Unicode value to encode.</param>
    internal static void Write(Span<byte> destination, string? value)
    {
        destination.Clear();
        if (value is null)
        {
            return;
        }

        int length = Math.Min(destination.Length, value.Length);
        for (int i = 0; i < length; i++)
        {
            destination[i] = Encode(value[i]);
        }
    }

    private static byte Encode(char value) => value switch
    {
        <= '\u007F' => (byte)value,
        >= '\u0E01' and <= '\u0E3A' => (byte)(0xA1 + value - '\u0E01'),
        '\u0E3F' => 0xDF,
        >= '\u0E40' and <= '\u0E5B' => (byte)(0xE0 + value - '\u0E40'),
        _ => (byte)'?',
    };
}
