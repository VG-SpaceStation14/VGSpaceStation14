using Content.Shared.Roles;
using JetBrains.Annotations;

namespace Content.Server._VG.SimpleSkills;

[UsedImplicitly]
public sealed partial class AddSkillGroupSpecial : JobSpecial
{
    [DataField(required: true)]
    public string Group { get; private set; } = string.Empty;

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var skillSystem = entMan.System<SimpleSkillSystem>();
        skillSystem.ApplySkillGroup(mob, Group);
    }
}