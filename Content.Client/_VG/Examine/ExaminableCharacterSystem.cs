// Content.Client._VG.Examine/ExaminableCharacterSystem.cs
using Content.Shared._VG.Examine;
using Robust.Client.UserInterface;

namespace Content.Client._VG.Examine;

public sealed class ExaminableCharacterSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ExaminableCharacterInfoMessage>(OnExamineInfo);
    }

    private void OnExamineInfo(ExaminableCharacterInfoMessage ev)
    {

    }
}