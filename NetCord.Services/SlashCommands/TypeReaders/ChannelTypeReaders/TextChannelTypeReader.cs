﻿namespace NetCord.Services.SlashCommands.TypeReaders.ChannelTypeReaders;

public class TextChannelTypeReader<TContext> : SlashCommandTypeReader<TContext> where TContext : ISlashCommandContext
{
    public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.Channel;

    public override Task<object?> ReadAsync(string value, TContext context, SlashCommandParameter<TContext> parameter, SlashCommandServiceOptions<TContext> options)
    {
        return Task.FromResult((object?)context.Interaction.Data.ResolvedData!.Channels![new(value)]);
    }

    public override IEnumerable<ChannelType>? GetAllowedChannelTypes(SlashCommandParameter<TContext> parameter)
    {
        yield return ChannelType.TextGuildChannel;
        yield return ChannelType.DMChannel;
        yield return ChannelType.GroupDMChannel;
        yield return ChannelType.NewsGuildChannel;
        yield return ChannelType.StoreGuildChannel;
        yield return ChannelType.NewsGuildThread;
        yield return ChannelType.PublicGuildThread;
        yield return ChannelType.PrivateGuildThread;
    }
}