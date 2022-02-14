﻿using System.Text.Json;

namespace NetCord;

public class ApplicationCommandOptionChoice
{
    private readonly JsonModels.JsonApplicationCommandOptionChoice _jsonEntity;

    public string Name => _jsonEntity.Name;

    public string Value { get; }

    internal ApplicationCommandOptionChoice(JsonModels.JsonApplicationCommandOptionChoice jsonEntity)
    {
        _jsonEntity = jsonEntity;
        JsonElement value = jsonEntity.Value;
        Value = value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();
    }
}