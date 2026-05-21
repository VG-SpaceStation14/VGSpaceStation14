using Content.Shared._VG;
using Content.Shared._VG.Examine;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._VG.Examine;

public sealed class ExaminableCharacterSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetConfigurationManager _netConfig = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExaminableCharacterComponent, ExaminedEvent>(OnExamineCharacter);
    }

    private void OnExamineCharacter(EntityUid uid, ExaminableCharacterComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryComp<ActorComponent>(args.Examiner, out var actor))
            return;

        var detailed = _netConfig.GetClientCVar(actor.PlayerSession.Channel, VGCCVars.DetailedExamine);
        if (!detailed)
            return;

        var selfAware = args.Examiner == args.Examined;

        string nameLine;
        if (selfAware)
        {
            nameLine = Loc.GetString("examine-name-selfaware");
        }
        else
        {
            var meta = MetaData(uid);
            nameLine = Loc.GetString("examine-name", ("name", meta.EntityName));
        }
        args.PushMarkup($"[font size=11]{nameLine}[/font]", 15);

        var canSee = selfAware
            ? Loc.GetString("examine-can-see-selfaware", ("ent", uid))
            : Loc.GetString("examine-can-see", ("ent", uid));
        args.PushMarkup($"[font size=10]{canSee}[/font]", 14);

        var slotLabels = new Dictionary<string, string>
        {
            { "head", "head" }, { "eyes", "eyes" }, { "mask", "mask" }, { "neck", "neck" },
            { "ears", "ears" }, { "jumpsuit", "jumpsuit" }, { "outerClothing", "outer" },
            { "back", "back" }, { "gloves", "gloves" }, { "belt", "belt" }, { "id", "id" },
            { "shoes", "shoes" }, { "suitstorage", "suitstorage" }
        };

        int priority = 13;
        bool hasAnyItem = false;

        foreach (var slot in slotLabels)
        {
            if (!_inventory.TryGetSlotEntity(uid, slot.Key, out var item))
                continue;

            if (!TryComp<MetaDataComponent>(item, out var meta))
                continue;

            hasAnyItem = true;
            var locKey = slot.Value + "-examine";
            if (selfAware)
                locKey += "-selfaware";

            var line = Loc.GetString(locKey, ("item", meta.EntityName));
            args.PushMarkup($"[font size=10]{line}[/font]", priority);
            priority--;
        }

        if (!hasAnyItem)
        {
            var nothing = selfAware
                ? Loc.GetString("examine-can-see-nothing-selfaware", ("ent", uid))
                : Loc.GetString("examine-can-see-nothing", ("ent", uid));
            args.PushMarkup($"[font size=10]{nothing}[/font]", 13);
        }
    }
}