using Content.Shared._VG.Mood; // VG-Tweak
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Slippery;

[UsedImplicitly]
public sealed class SlipperySystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SpeedModifierContactsSystem _speedModifier = default!;

    private EntityQuery<KnockedDownComponent> _knockedDownQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<SlidingComponent> _slidingQuery;

    public override void Initialize()
    {
        base.Initialize();

        _knockedDownQuery = GetEntityQuery<KnockedDownComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _slidingQuery = GetEntityQuery<SlidingComponent>();

        SubscribeLocalEvent<SlipperyComponent, StepTriggerAttemptEvent>(HandleAttemptCollide);
        SubscribeLocalEvent<SlipperyComponent, StepTriggeredOffEvent>(HandleStepTrigger);
        SubscribeLocalEvent<NoSlipComponent, SlipAttemptEvent>(OnNoSlipAttempt);
        SubscribeLocalEvent<SlowedOverSlipperyComponent, SlipAttemptEvent>(OnSlowedOverSlipAttempt);
        SubscribeLocalEvent<ThrownItemComponent, SlipCausingAttemptEvent>(OnThrownSlipAttempt);
        SubscribeLocalEvent<NoSlipComponent, InventoryRelayedEvent<SlipAttemptEvent>>((e, c, ev) => OnNoSlipAttempt(e, c, ev.Args));
        SubscribeLocalEvent<SlowedOverSlipperyComponent, InventoryRelayedEvent<SlipAttemptEvent>>((e, c, ev) => OnSlowedOverSlipAttempt(e, c, ev.Args));
        SubscribeLocalEvent<SlowedOverSlipperyComponent, InventoryRelayedEvent<GetSlowedOverSlipperyModifierEvent>>(OnGetSlowedOverSlipperyModifier);
        SubscribeLocalEvent<SlipperyComponent, EndCollideEvent>(OnEntityExit);
    }

    private void HandleStepTrigger(EntityUid uid, SlipperyComponent component, ref StepTriggeredOffEvent args)
    {
        TrySlip(uid, component, args.Tripper);
    }

    private void HandleAttemptCollide(
        EntityUid uid,
        SlipperyComponent component,
        ref StepTriggerAttemptEvent args)
    {
        args.Continue |= CanSlip(uid, args.Tripper);
    }

    private static void OnNoSlipAttempt(EntityUid uid, NoSlipComponent component, SlipAttemptEvent args)
    {
        args.NoSlip = true;
    }

    private void OnSlowedOverSlipAttempt(EntityUid uid, SlowedOverSlipperyComponent component, SlipAttemptEvent args)
    {
        args.SlowOverSlippery = true;
    }

    private void OnThrownSlipAttempt(EntityUid uid, ThrownItemComponent comp, ref SlipCausingAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnGetSlowedOverSlipperyModifier(EntityUid uid, SlowedOverSlipperyComponent comp, ref InventoryRelayedEvent<GetSlowedOverSlipperyModifierEvent> args)
    {
        args.Args.SlowdownModifier *= comp.SlowdownModifier;
    }

    private void OnEntityExit(EntityUid uid, SlipperyComponent component, ref EndCollideEvent args)
    {
        if (HasComp<SpeedModifiedByContactComponent>(args.OtherEntity))
            _speedModifier.AddModifiedEntity(args.OtherEntity);
    }

    private bool CanSlip(EntityUid uid, EntityUid toSlip)
    {
        return !_container.IsEntityInContainer(uid)
                && _status.CanAddStatusEffect(toSlip, SharedStunSystem.StunId);
    }

    public void TrySlip(EntityUid uid, SlipperyComponent component, EntityUid other, bool requiresContact = true)
    {
        var knockedDown = _knockedDownQuery.HasComp(other);
        if (knockedDown && !component.SlipData.SuperSlippery)
            return;

        var attemptEv = new SlipAttemptEvent(uid, targetSlots: component.IgnoreSlots ? SlotFlags.NONE : SlotFlags.FEET);
        RaiseLocalEvent(other, attemptEv);
        if (attemptEv.SlowOverSlippery)
            _speedModifier.AddModifiedEntity(other);

        if (attemptEv.NoSlip)
            return;

        var attemptCausingEv = new SlipCausingAttemptEvent();
        RaiseLocalEvent(uid, ref attemptCausingEv);
        if (attemptCausingEv.Cancelled)
            return;

        var ev = new SlipEvent(other);
        RaiseLocalEvent(uid, ref ev);

        if (_physicsQuery.TryComp(other, out var physics) && !_slidingQuery.HasComp(other))
        {
            _physics.SetLinearVelocity(other, physics.LinearVelocity * component.SlipData.LaunchForwardsMultiplier, body: physics);

            if (component.AffectsSliding && requiresContact)
                EnsureComp<SlidingComponent>(other);
        }

        if (!knockedDown)
        {
            _stun.TryUpdateStunDuration(other, component.SlipData.StunTime);

            if (!MathHelper.CloseTo(component.SlipData.SlipFriction, 1f))
            {
                _movementMod.TryUpdateFrictionModDuration(
                    other,
                    component.FrictionStatusTime,
                    component.SlipData.SlipFriction
                );
            }

            _stamina.TakeStaminaDamage(other, component.StaminaDamage);

            RaiseLocalEvent(other, new MoodEffectEvent("MobSlipped")); // VG-Tweak

            _audio.PlayPredicted(component.SlipSound, other, other);
        }

        _stun.TryKnockdown(other, component.SlipData.KnockdownTime, force: true);

        _adminLogger.Add(LogType.Slip, LogImpact.Low, $"{ToPrettyString(other):mob} slipped on collision with {ToPrettyString(uid):entity}");
    }
}

public sealed class SlipAttemptEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool NoSlip;
    public bool SlowOverSlippery;
    public EntityUid? SlipCausingEntity;
    public SlotFlags TargetSlots { get; } = SlotFlags.FEET;

    public SlipAttemptEvent(EntityUid? slipCausingEntity, SlotFlags targetSlots)
    {
        SlipCausingEntity = slipCausingEntity;
        TargetSlots = targetSlots;
    }
}

[ByRefEvent]
public record struct SlipCausingAttemptEvent (bool Cancelled);

[ByRefEvent]
public readonly record struct SlipEvent(EntityUid Slipped);