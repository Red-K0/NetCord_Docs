﻿namespace NetCord;

public partial class RestClient
{
    public async Task<IReadOnlyDictionary<DiscordId, GuildEmoji>> GetGuildEmojisAsync(DiscordId guildId, RequestProperties? options = null)
        => (await SendRequestAsync(HttpMethod.Get, $"/guilds/{guildId}/emojis", options).ConfigureAwait(false)).ToObject<IEnumerable<JsonModels.JsonEmoji>>().ToDictionary(e => e.Id.GetValueOrDefault(), e => new GuildEmoji(e, guildId, this));

    public async Task<GuildEmoji> GetGuildEmojiAsync(DiscordId guildId, DiscordId emojiId, RequestProperties? options = null)
        => new((await SendRequestAsync(HttpMethod.Get, $"/guilds/{guildId}/emojis/{emojiId}", options).ConfigureAwait(false)).ToObject<JsonModels.JsonEmoji>(), guildId, this);

    public async Task<GuildEmoji> CreateGuildEmojiAsync(DiscordId guildId, GuildEmojiProperties properties, RequestProperties? options = null)
        => new((await SendRequestAsync(HttpMethod.Post, new JsonContent(properties), $"/guilds/{guildId}/emojis", options).ConfigureAwait(false)).ToObject<JsonModels.JsonEmoji>(), guildId, this);

    public async Task<GuildEmoji> ModifyGuildEmojiAsync(DiscordId guildId, DiscordId emojiId, Action<GuildEmojiOptions> action, RequestProperties? options = null)
    {
        GuildEmojiOptions guildEmojiOptions = new();
        action(guildEmojiOptions);
        return new((await SendRequestAsync(HttpMethod.Patch, new JsonContent(guildEmojiOptions), $"/guilds/{guildId}/emojis/{emojiId}", options).ConfigureAwait(false)).ToObject<JsonModels.JsonEmoji>(), guildId, this);
    }

    public Task DeleteGuildEmojiAsync(DiscordId guildId, DiscordId emojiId, RequestProperties? options = null)
        => SendRequestAsync(HttpMethod.Delete, $"/guilds/{guildId}/emojis/{emojiId}", options);
}