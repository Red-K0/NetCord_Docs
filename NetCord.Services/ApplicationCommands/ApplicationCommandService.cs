﻿using System.Reflection;

namespace NetCord.Services.ApplicationCommands;

public class ApplicationCommandService<TContext> : IService where TContext : IApplicationCommandContext
{
    private readonly ApplicationCommandServiceOptions<TContext> _options;
    internal readonly List<ApplicationCommandInfo<TContext>> _globalCommandsToCreate = new();
    internal readonly List<ApplicationCommandInfo<TContext>> _guildCommandsToCreate = new();
    private readonly Dictionary<DiscordId, ApplicationCommandInfo<TContext>> _commands = new();

    public ApplicationCommandService(ApplicationCommandServiceOptions<TContext>? options = null)
    {
        _options = options ?? new();
    }

    public void AddModules(Assembly assembly)
    {
        Type baseType = typeof(ApplicationCommandModule<TContext>);
        lock (_commands)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAssignableTo(baseType))
                    AddModuleCore(type);
            }
        }
    }

    public void AddModule(Type type)
    {
        if (!type.IsAssignableTo(typeof(ApplicationCommandModule<TContext>)))
            throw new InvalidOperationException($"Modules must inherit from {nameof(ApplicationCommandModule<TContext>)}");

        lock (_commands)
            AddModuleCore(type);
    }

    private void AddModuleCore(Type type)
    {
        foreach (var method in type.GetMethods())
        {
            SlashCommandAttribute? slashCommandAttribute = method.GetCustomAttribute<SlashCommandAttribute>();
            if (slashCommandAttribute != null)
                AddCommandInfo(new(method, slashCommandAttribute, _options));

            UserCommandAttribute? userCommandAttribute = method.GetCustomAttribute<UserCommandAttribute>();
            if (userCommandAttribute != null)
                AddCommandInfo(new(method, userCommandAttribute));

            MessageCommandAttribute? messageCommandAttribute = method.GetCustomAttribute<MessageCommandAttribute>();
            if (messageCommandAttribute != null)
                AddCommandInfo(new(method, messageCommandAttribute));
        }

        void AddCommandInfo(ApplicationCommandInfo<TContext> applicationCommandInfo)
        {
            if (applicationCommandInfo.GuildId.HasValue)
                _guildCommandsToCreate.Add(applicationCommandInfo);
            else
                _globalCommandsToCreate.Add(applicationCommandInfo);
        }
    }

    public async Task CreateCommandsAsync(RestClient client, DiscordId applicationId, bool includeGuildCommands = false)
    {
        var e = (await client.BulkOverwriteGlobalApplicationCommandsAsync(applicationId, _globalCommandsToCreate.Select(c => c.GetRawValue())).ConfigureAwait(false)).Zip(_globalCommandsToCreate);
        lock (_commands)
        {
            foreach (var (First, Second) in e)
                _commands.Add(First.Key, Second);
        }

        if (includeGuildCommands)
        {
            foreach (var c in _guildCommandsToCreate.GroupBy(c => c.GuildId))
            {
                var rawGuildCommands = c.Select(v => v.GetRawValue());

                var newCommands = await client.BulkOverwriteGuildApplicationCommandsAsync(applicationId, c.Key.GetValueOrDefault(), rawGuildCommands).ConfigureAwait(false);
                var permissions = newCommands.Zip(c)
                    .Select(zip => new GuildApplicationCommandPermissionsProperties(zip.First.Key, zip.Second.GetRawPermissions()));
                await client.BulkOverwriteGuildApplicationCommandPermissions(applicationId, c.Key.GetValueOrDefault(), permissions).ConfigureAwait(false);

                lock (_commands)
                {
                    foreach (var (First, Second) in newCommands.Zip(c))
                        _commands.Add(First.Key, Second);
                }
            }
        }
    }

    public IEnumerable<ApplicationCommandProperties> GetGlobalCommandProperties()
        => _globalCommandsToCreate.Select(c => c.GetRawValue());

    internal void AddCommands(IEnumerable<(ApplicationCommand Command, IApplicationCommandInfo CommandInfo)> commands)
    {
        lock (_commands)
        {
            foreach (var (Command, CommandInfo) in commands)
                _commands.Add(Command.Id, (ApplicationCommandInfo<TContext>)CommandInfo);
        }
    }

    internal void AddCommand((ApplicationCommand Command, IApplicationCommandInfo CommandInfo) command)
    {
        lock (_commands)
            _commands.Add(command.Command.Id, (ApplicationCommandInfo<TContext>)command.CommandInfo);
    }

    public async Task ExecuteAsync(TContext context)
    {
        var interaction = context.Interaction;
        ApplicationCommandInfo<TContext>? command;
        lock (_commands)
        {
            if (!_commands.TryGetValue(interaction.Data.Id, out command!))
                throw new ApplicationCommandNotFoundException();
        }

        await command.EnsureCanExecuteAsync(context).ConfigureAwait(false);

        var parameters = command.Parameters;
        object?[]? values;
        if (interaction is SlashCommandInteraction slashCommandInteraction)
        {
            values = new object?[parameters!.Count];
            var options = slashCommandInteraction.Data.Options;
            int optionsCount = options.Count;
            int parameterIndex = 0;
            for (int optionIndex = 0; optionIndex < optionsCount; parameterIndex++)
            {
                var parameter = parameters[parameterIndex];
                var option = options[optionIndex];
                if (parameter.Name != option.Name)
                    values[parameterIndex] = parameter.DefaultValue!;
                else
                {
                    values[parameterIndex] = await parameter.TypeReader.ReadAsync(option.Value!, context, parameter, _options).ConfigureAwait(false);
                    optionIndex++;
                }
            }
            int parametersCount = parameters.Count;
            while (parameterIndex < parametersCount)
            {
                values[parameterIndex] = parameters[parameterIndex].DefaultValue!;
                parameterIndex++;
            }
        }
        else
            values = null;

        var methodClass = (ApplicationCommandModule<TContext>)Activator.CreateInstance(command.DeclaringType)!;
        methodClass.Context = context;
        await command.InvokeAsync(methodClass, values).ConfigureAwait(false);
    }

    public async Task ExecuteAutocompleteAsync(ApplicationCommandAutocompleteInteraction autocompleteInteraction)
    {
        var parameter = autocompleteInteraction.Data.Options.First(o => o.Focused);
        ApplicationCommandInfo<TContext> command;
        lock (_commands)
        {
            if (!_commands.TryGetValue(autocompleteInteraction.Data.Id, out command!))
                throw new ApplicationCommandNotFoundException();
        }
        var autocompletes = command.Autocompletes;
        IAutocompleteProvider autocompleteProvider;
        lock (autocompletes!)
        {
            if (!autocompletes.TryGetValue(parameter.Name, out autocompleteProvider!))
                throw new AutocompleteNotFoundException();
        }
        var choices = await autocompleteProvider.GetChoicesAsync(parameter, autocompleteInteraction).ConfigureAwait(false);
        await autocompleteInteraction.SendResponseAsync(InteractionCallback.ApplicationCommandAutocompleteResult(choices)).ConfigureAwait(false);
    }
}