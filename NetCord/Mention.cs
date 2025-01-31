﻿using System.Diagnostics.CodeAnalysis;

namespace NetCord;

public static class Mention
{
    public static bool TryParseUser(ReadOnlySpan<char> mention, out ulong id)
    {
        if (mention is ['<', '@', _, .., '>'])
        {
            mention = mention[2] is '!' ? mention[3..^1] : mention[2..^1];

            if (Snowflake.TryParse(mention, out id))
                return true;
        }
        else
            id = default;

        return false;
    }

    public static ulong ParseUser(ReadOnlySpan<char> mention)
    {
        if (TryParseUser(mention, out ulong id))
            return id;
        else
            throw new FormatException("Cannot parse the mention.");
    }

    public static bool TryParseChannel(ReadOnlySpan<char> mention, out ulong id)
    {
        if (mention is ['<', '#', .., '>'])
        {
            if (Snowflake.TryParse(mention[2..^1], out id))
                return true;
        }
        else
            id = default;

        return false;
    }

    public static ulong ParseChannel(ReadOnlySpan<char> mention)
    {
        if (TryParseChannel(mention, out ulong id))
            return id;
        else
            throw new FormatException("Cannot parse the mention.");
    }

    public static bool TryParseRole(ReadOnlySpan<char> mention, out ulong id)
    {
        if (mention is ['<', '@', '&', .., '>'])
        {
            if (Snowflake.TryParse(mention[3..^1], out id))
                return true;
        }
        else
            id = default;

        return false;
    }

    public static ulong ParseRole(ReadOnlySpan<char> mention)
    {
        if (TryParseRole(mention, out ulong id))
            return id;
        else
            throw new FormatException("Cannot parse the mention.");
    }

    public static bool TryParseSlashCommand(ReadOnlySpan<char> mention, [MaybeNullWhen(false)] out SlashCommandMention result)
    {
        if (mention is ['<', '/', .., '>'])
        {
            mention = mention[2..^1];
            int index = mention.IndexOf(':');
            if (index == -1)
                goto Fail;

            var names = mention[..index];
            var s = new string[3];
            int i = 0;
            while (i < 3)
            {
                int x = names.IndexOf(' ');
                if (x == -1)
                {
                    s[i] = names.ToString();
                    goto Skip;
                }
                else
                {
                    s[i] = names[..x].ToString();
                    names = names[(x + 1)..];
                }
                i++;
            }

            if (!names.IsEmpty)
                goto Fail;

            Skip:
            if (!Snowflake.TryParse(mention[(index + 1)..], out var id))
                goto Fail;

            result = i switch
            {
                0 => new(id, s[0]),
                1 => new(id, s[0], s[1]),
                _ => new(id, s[0], s[1], s[2]),
            };
            return true;
        }

        Fail:
        result = null;
        return false;
    }

    public static SlashCommandMention ParseSlashCommand(ReadOnlySpan<char> mention)
    {
        if (TryParseSlashCommand(mention, out var result))
            return result;
        else
            throw new FormatException("Cannot parse the mention.");
    }

    public static bool TryParseTimestamp(ReadOnlySpan<char> mention, out Timestamp result)
    {
        return Timestamp.TryParse(mention, out result);
    }

    public static Timestamp ParseTimestamp(ReadOnlySpan<char> mention)
    {
        if (TryParseTimestamp(mention, out var result))
            return result;
        else
            throw new FormatException("Cannot parse the mention.");
    }

    public static bool TryParseGuildNavigation(ReadOnlySpan<char> mention, out GuildNavigation result)
    {
        return GuildNavigation.TryParse(mention, out result);
    }

    public static GuildNavigation ParseGuildNavigation(ReadOnlySpan<char> mention)
    {
        if (TryParseGuildNavigation(mention, out var result))
            return result;
        else
            throw new FormatException("Cannot parse the mention.");
    }

    public static bool TryFormatUser(Span<char> destination, out int charsWritten, ulong id)
    {
        return TryFormat(destination, out charsWritten, id, "<@", ">");
    }

    public static bool TryFormatChannel(Span<char> destination, out int charsWritten, ulong id)
    {
        return TryFormat(destination, out charsWritten, id, "<#", ">");
    }

    public static bool TryFormatRole(Span<char> destination, out int charsWritten, ulong id)
    {
        return TryFormat(destination, out charsWritten, id, "<@&", ">");
    }

    private static bool TryFormat(Span<char> destination, out int charsWritten, ulong id, ReadOnlySpan<char> prefix, ReadOnlySpan<char> suffix)
    {
        if (destination.Length <= prefix.Length + suffix.Length || !id.TryFormat(destination[prefix.Length..^suffix.Length], out int length))
        {
            charsWritten = 0;
            return false;
        }

        prefix.CopyTo(destination);
        suffix.CopyTo(destination[(prefix.Length + length)..]);
        charsWritten = prefix.Length + length + suffix.Length;
        return true;
    }
}
