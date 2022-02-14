﻿namespace NetCord.Services.Interactions.TypeReaders;

public class TimestampTypeReader<TContext> : InteractionTypeReader<TContext> where TContext : InteractionContext
{
    public override Task<object?> ReadAsync(string input, TContext context, InteractionParameter<TContext> parameter, InteractionServiceOptions<TContext> options) => Task.FromResult((object?)Timestamp.Parse(input));
}