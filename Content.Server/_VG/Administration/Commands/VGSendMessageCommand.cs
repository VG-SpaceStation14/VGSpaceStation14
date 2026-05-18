using Content.Server.Administration;
using Content.Server._VG.LobbyMessage;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._VG.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class VGSendMessageCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public string Command => "vgsendmessage";

    public string Description =>
        "Отправляет сообщение в центр лобби.";

    public string Help =>
        "vgsendmessage <текст>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = _systems.GetEntitySystem<VGMessageSystem>();

        if (args.Length == 0)
        {
            system.SendMessage(string.Empty);

            shell.WriteLine("Сообщение очищено.");

            return;
        }

        var text = string.Join(" ", args);

        if (text.Length > 100)
            text = text[..100];

        system.SendMessage(text);

        shell.WriteLine($"Отправлено: {text}");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.FromHint("Текст сообщения");
    }
}