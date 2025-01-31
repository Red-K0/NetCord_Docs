﻿using System.Diagnostics.CodeAnalysis;

namespace NetCord;

public readonly struct GuildNavigation(GuildNavigationType type) : IEquatable<GuildNavigation>
{
    public GuildNavigationType Type { get; } = type;

    public static bool TryParse(ReadOnlySpan<char> value, out GuildNavigation guildNavigation)
    {
        if (value is ['<', 'i', 'd', ':', .., '>'] && TryParseType(value[4..^1], out var type))
        {
            guildNavigation = new(type);
            return true;
        }

        guildNavigation = default;
        return false;
    }

    public static GuildNavigation Parse(ReadOnlySpan<char> value)
    {
        if (TryParse(value, out var guildNavigation))
            return guildNavigation;
        else
            throw new FormatException($"Cannot parse '{nameof(GuildNavigation)}'.");
    }

    private static bool TryParseType(ReadOnlySpan<char> value, out GuildNavigationType type)
    {
        switch (value)
        {
            case "customize":
                type = GuildNavigationType.Customize;
                return true;
            case "browse":
                type = GuildNavigationType.Browse;
                return true;
            case "guide":
                type = GuildNavigationType.Guide;
                return true;
            default:
                type = default;
                return false;
        }
    }

    public static bool operator ==(GuildNavigation left, GuildNavigation right) => left.Equals(right);

    public static bool operator !=(GuildNavigation left, GuildNavigation right) => !(left == right);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is GuildNavigation guildNavigation && Equals(guildNavigation);

    public bool Equals(GuildNavigation other) => Type == other.Type;

    public override int GetHashCode() => Type.GetHashCode();

    public override string ToString()
    {
        return Type switch
        {
            GuildNavigationType.Customize => "<id:customize>",
            GuildNavigationType.Browse => "<id:browse>",
            GuildNavigationType.Guide => "<id:guide>",
            _ => throw new System.ComponentModel.InvalidEnumArgumentException($"Invalid '{nameof(GuildNavigationType)}'."),
        };
    }
}
