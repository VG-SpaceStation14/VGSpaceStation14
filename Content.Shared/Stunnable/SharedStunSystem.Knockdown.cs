using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Gravity;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stunnable;

/// <summary>
/// This contains the knockdown logic for the stun system for organization purposes.
/// </summary>
public abstract partial class SharedStunSystem
{
    private EntityQuery<CrawlerComponent> _crawlerQuery;

    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;

    public static readonly ProtoId<AlertPrototype> KnockdownAlert = "Knockdown";

    private void InitializeKnockdown()
    {
        _crawlerQuery = GetEntityQuery<CrawlerComponent>();

        SubscribeLocalEvent<KnockedDownComponent, RejuvenateEvent>(OnRejuvenate);

        // Startup and Shutdown
        SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
        SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);

        // Action blockers
        SubscribeLocalEvent<KnockedDownComponent, BuckleAttemptEvent>(OnBuckleAttempt);
        SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandAttempt);

        // Updating movement and friction
        SubscribeLocalEvent<KnockedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshKnockedSpeed);
        SubscribeLocalEvent<KnockedDownComponent, RefreshFrictionModifiersEvent>(OnRefreshFriction);
        SubscribeLocalEvent<KnockedDownComponent, TileFrictionEvent>(OnKnockedTileFriction);

        // DoAfter event subscriptions
        SubscribeLocalEvent<KnockedDownComponent, TryStandDoAfterEvent>(OnStandDoAfter);

        // Crawling
        SubscribeLocalEvent<CrawlerComponent, KnockedDownRefreshEvent>(OnKnockdownRefresh);
        SubscribeLocalEvent<CrawlerComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<KnockedDownComponent, WeightlessnessChangedEvent>(OnWeightlessnessChanged);
        SubscribeLocalEvent<KnockedDownComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<KnockedDownComponent, DidUnequipHandEvent>(OnHandUnequipped);
        SubscribeLocalEvent<KnockedDownComponent, HandCountChangedEvent>(OnHandCountChanged);
        SubscribeLocalEvent<GravityAffectedComponent, KnockDownAttemptEvent>(OnKnockdownAttempt);
        SubscribeLocalEvent<GravityAffectedComponent, GetStandUpTimeEvent>(OnGetStandUpTime);

        // Handling Alternative Inputs
        SubscribeAllEvent<ForceStandUpEvent>(OnForceStandup);
        SubscribeLocalEvent<KnockedDownComponent, KnockedDownAlertEvent>(OnKnockedDownAlert);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleKnockdown, InputCmdHandler.FromDelegate(HandleToggleKnockdown, handle: false))
            .Bind(ContentKeyFunctions.ForceStand, InputCmdHandler.FromDelegate(HandleForceStand, handle: false))
            .Register<SharedStunSystem>();
    }

    public override void Update(float frameTime)
    {
        // Updates are handled by the systems themselves now
    }

    private void OnRejuvenate(Entity<KnockedDownComponent> entity, ref RejuvenateEvent args)
    {
        SetKnockdownNextUpdate(entity, GameTiming.CurTime);

        if (entity.Comp.AutoStand)
            RemComp<KnockedDownComponent>(entity);
    }

    #region Startup and Shutdown

    private void OnKnockInit(Entity<KnockedDownComponent> entity, ref ComponentInit args)
    {
        _standingState.Down(entity, true, false);
        RefreshKnockedMovement(entity);
    }

    private void OnKnockShutdown(Entity<KnockedDownComponent> entity, ref ComponentShutdown args)
    {
        entity.Comp.FrictionModifier = 1f;
        entity.Comp.SpeedModifier = 1f;

        _standingState.Stand(entity);
        Alerts.ClearAlert(entity.Owner, KnockdownAlert);
    }

    #endregion

    #region API

    /// <summary>
    /// Cancels the DoAfter of an entity with the <see cref="KnockedDownComponent"/> who is trying to stand.
    /// </summary>
    public void CancelKnockdownDoAfter(Entity<KnockedDownComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (entity.Comp.DoAfterId == null)
            return;

        DoAfter.Cancel(entity.Owner, entity.Comp.DoAfterId.Value);
        entity.Comp.DoAfterId = null;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.DoAfterId));
    }

    /// <summary>
    /// Sets the time left of the knockdown timer to the inputted value.
    /// </summary>
    public void SetKnockdownTime(Entity<KnockedDownComponent?> entity, TimeSpan time)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        SetKnockdownNextUpdate((entity, entity.Comp), GameTiming.CurTime + time);
    }

    /// <summary>
    /// Updates the knockdown timer of a knocked down entity with a given inputted time, then dirties the time.
    /// </summary>
    public void UpdateKnockdownTime(Entity<KnockedDownComponent?> entity, TimeSpan time, bool refresh = true)
    {
        if (refresh)
            RefreshKnockdownTime(entity, time);
        else
            AddKnockdownTime(entity, time);
    }

    /// <summary>
    /// Refreshes the amount of time an entity is knocked down to the inputted time, if it is greater than
    /// the current time left.
    /// </summary>
    public void RefreshKnockdownTime(Entity<KnockedDownComponent?> entity, TimeSpan time)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        var knockedTime = GameTiming.CurTime + time;
        if (entity.Comp.NextUpdate < knockedTime)
            SetKnockdownNextUpdate((entity, entity.Comp), knockedTime);
    }

    /// <summary>
    /// Adds our inputted time to an entity's knocked down timer, or sets it to the given time if their timer has expired.
    /// </summary>
    public void AddKnockdownTime(Entity<KnockedDownComponent?> entity, TimeSpan time)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (entity.Comp.NextUpdate < GameTiming.CurTime)
        {
            SetKnockdownNextUpdate((entity, entity.Comp), GameTiming.CurTime + time);
            return;
        }

        SetKnockdownNextUpdate((entity, entity.Comp), entity.Comp.NextUpdate + time);
    }

    #endregion

    #region Knockdown Logic

    /// <summary>
    /// Sets the next update datafield of an entity's <see cref="KnockedDownComponent"/> to a specific time.
    /// </summary>
    private void SetKnockdownNextUpdate(Entity<KnockedDownComponent> entity, TimeSpan time)
    {
        if (GameTiming.CurTime > time)
            time = GameTiming.CurTime;

        entity.Comp.NextUpdate = time;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.NextUpdate));
        Alerts.UpdateAlert(entity.Owner, KnockdownAlert, null, entity.Comp.NextUpdate);
    }

    private void HandleToggleKnockdown(ICommonSession? session)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } playerEnt || !Exists(playerEnt))
            return;

        ToggleKnockdown(playerEnt);
    }

    private void HandleForceStand(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } playerEnt || !Exists(playerEnt))
            return;

        if (!TryComp<KnockedDownComponent>(playerEnt, out _) || HasComp<StunnedComponent>(playerEnt))
            return;

        HandleStandAttempt(playerEnt);
    }

    /// <summary>
    /// Handles an entity trying to make itself fall down.
    /// C key - always slow standing with DoAfter
    /// </summary>
    private void ToggleKnockdown(Entity<CrawlerComponent?, KnockedDownComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp1, false) || !_cfgManager.GetCVar(CCVars.MovementCrawling))
            return;

        if (!Resolve(entity, ref entity.Comp2, false))
        {
            TryKnockdown(entity.Owner, entity.Comp1.DefaultKnockedDuration, true, false, false);
            return;
        }

        var stand = !entity.Comp2.DoAfterId.HasValue;
        
        if (_standingState.IsDown(entity.Owner) && !HasComp<StunnedComponent>(entity))
        {
            if (stand && !TryStanding((entity, entity.Comp2)))
                CancelKnockdownDoAfter((entity, entity.Comp2));
            return;
        }

        if (!stand || !TryStanding((entity, entity.Comp2)))
            CancelKnockdownDoAfter((entity, entity.Comp2));
    }

    private void HandleStandAttempt(EntityUid uid)
    {
        // Проверяем, свободна ли хотя бы одна рука
        if (_hands.TryGetEmptyHand(uid, out _))
        {
            // Есть свободная рука - быстрое вставание через ForceStandUp
            ForceStandUp(uid);
        }
        else
        {
            // Обе руки заняты - медленное вставание через DoAfter
            TryStanding(uid);
        }
    }

    public bool TryStanding(Entity<KnockedDownComponent?> entity, bool DoDoAfter = true)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return true;

        if (!KnockdownOver((entity, entity.Comp)))
            return false;
        if (!DoDoAfter) return true;

        if (!_crawlerQuery.TryComp(entity, out var crawler) || !_cfgManager.GetCVar(CCVars.MovementCrawling))
        {
            RemComp<KnockedDownComponent>(entity);
            _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(entity):user} has stood up from knockdown.");
            return true;
        }

        if (!TryStand((entity, entity.Comp)))
            return false;

        var baseTime = crawler.StandTime;
        if (TryComp<StaminaComponent>(entity, out var stamina))
        {
            var staminaDamageRatio = Math.Clamp(stamina.StaminaDamage / stamina.CritThreshold, 0f, 1f);
            var timeMultiplier = MathHelper.Lerp(0.5f, 2.0f, staminaDamageRatio);
            baseTime *= timeMultiplier;
        }

        if (baseTime > TimeSpan.FromSeconds(2))
            baseTime = TimeSpan.FromSeconds(2);

        var ev = new GetStandUpTimeEvent(baseTime);
        RaiseLocalEvent(entity, ref ev);

        var doAfterArgs = new DoAfterArgs(EntityManager, entity, ev.DoAfterTime, new TryStandDoAfterEvent(), entity, entity)
        {
            BreakOnDamage = true,
            DamageThreshold = 5,
            CancelDuplicate = true,
            RequireCanInteract = false,
            BreakOnHandChange = true
        };

        if (!DoAfter.TryStartDoAfter(doAfterArgs, out var doAfterId))
            return false;

        entity.Comp.DoAfterId = doAfterId.Value.Index;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.DoAfterId));
        return true;
    }

    public bool KnockdownOver(Entity<KnockedDownComponent> entity)
    {
        if (entity.Comp.NextUpdate > GameTiming.CurTime)
            return false;

        return Blocker.CanMove(entity);
    }

    /// <summary>
    /// A variant of <see cref="CanStand"/> used when we're actually trying to stand.
    /// </summary>
    public bool TryStand(Entity<KnockedDownComponent> entity)
    {
        if (!KnockdownOver(entity))
            return false;

        var ev = new StandUpAttemptEvent(entity.Comp.AutoStand);
        RaiseLocalEvent(entity, ref ev);

        if (ev.Message != null)
        {
            _popup.PopupClient(ev.Message.Value.Item1, entity, entity, ev.Message.Value.Item2);
        }

        return !ev.Cancelled;
    }

    /// <summary>
    /// Checks if an entity is able to stand, returns true if it can, returns false if it cannot.
    /// </summary>
    public bool CanStand(Entity<KnockedDownComponent> entity)
    {
        if (!KnockdownOver(entity))
            return false;

        var ev = new StandUpAttemptEvent();
        RaiseLocalEvent(entity, ref ev);

        return !ev.Cancelled;
    }

    private bool StandingBlocked(Entity<KnockedDownComponent> entity)
    {
        if (!TryStand(entity))
            return true;

        if (!IntersectingStandingColliders(entity.Owner))
            return false;

        _popup.PopupClient(Loc.GetString("knockdown-component-stand-no-room"), entity, entity, PopupType.SmallCaution);
        return true;
    }

    private void OnForceStandup(ForceStandUpEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        HandleStandAttempt(user);
    }

    /// <summary>
    /// Force standing up. Costs 30% of stamina threshold, requires a free hand and no stun.
    /// </summary>
    public void ForceStandUp(Entity<KnockedDownComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (HasComp<StunnedComponent>(entity))
            return;

        if (StandingBlocked((entity, entity.Comp)))
            return;

        if (!_hands.TryGetEmptyHand(entity.Owner, out _))
            return;

        if (!TryForceStand(entity.Owner))
            return;

        CancelKnockdownDoAfter(entity);
        RemComp<KnockedDownComponent>(entity);
        _standingState.Stand(entity);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(entity):user} has force stood up from knockdown.");
    }

    private void OnKnockedDownAlert(Entity<KnockedDownComponent> entity, ref KnockedDownAlertEvent args)
    {
        if (args.Handled)
            return;

        HandleStandAttempt(entity.Owner);
        args.Handled = true;
    }

    private bool TryForceStand(Entity<StaminaComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        var staminaCost = entity.Comp.CritThreshold * 0.30f;
        var ev = new TryForceStandEvent(staminaCost);
        RaiseLocalEvent(entity, ref ev);

        if (!Stamina.TryTakeStamina(entity, ev.Stamina, entity.Comp, visual: true))
        {
            _popup.PopupClient(Loc.GetString("knockdown-component-pushup-failure"), entity, entity, PopupType.MediumCaution);
            return false;
        }

        _popup.PopupClient(Loc.GetString("knockdown-component-pushup-success"), entity, entity);
        _audio.PlayPredicted(entity.Comp.ForceStandSuccessSound, entity.Owner, entity.Owner, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));

        return true;
    }

    /// <summary>
    ///     Checks if standing would cause us to collide with something and potentially get stuck.
    /// </summary>
    private bool IntersectingStandingColliders(Entity<TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var intersecting = _physics.GetEntitiesIntersectingBody(entity, StandingStateSystem.StandingCollisionLayer, false);

        if (intersecting.Count == 0)
            return false;

        var fixtureQuery = GetEntityQuery<FixturesComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var ourAABB = _entityLookup.GetAABBNoContainer(entity, entity.Comp.LocalPosition, entity.Comp.LocalRotation);

        foreach (var ent in intersecting)
        {
            if (!fixtureQuery.TryGetComponent(ent, out var fixtures))
                continue;

            if (!xformQuery.TryComp(ent, out var xformComp))
                continue;

            var xform = new Transform(xformComp.LocalPosition, xformComp.LocalRotation);

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard || (fixture.CollisionMask & StandingStateSystem.StandingCollisionLayer) != StandingStateSystem.StandingCollisionLayer)
                    continue;

                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    var intersection = fixture.Shape.ComputeAABB(xform, i).IntersectPercentage(ourAABB);
                    if (intersection > 0.1f)
                        return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Crawling

    private void OnDamaged(Entity<CrawlerComponent> entity, ref DamageChangedEvent args)
    {
        if (!args.InterruptsDoAfters || !args.DamageIncreased || args.DamageDelta == null || GameTiming.ApplyingState)
            return;

        if (args.DamageDelta.GetTotal() >= entity.Comp.KnockdownDamageThreshold)
            RefreshKnockdownTime(entity.Owner, entity.Comp.DefaultKnockedDuration);
    }

    private void OnKnockdownRefresh(Entity<CrawlerComponent> entity, ref KnockedDownRefreshEvent args)
    {
        args.FrictionModifier *= entity.Comp.FrictionModifier;
        args.SpeedModifier *= entity.Comp.SpeedModifier;
    }

    private void OnWeightlessnessChanged(Entity<KnockedDownComponent> entity, ref WeightlessnessChangedEvent args)
    {
        if (!args.Weightless)
            return;

        CancelKnockdownDoAfter((entity, entity.Comp));
        RemCompDeferred<KnockedDownComponent>(entity);
    }

    private void OnHandEquipped(Entity<KnockedDownComponent> entity, ref DidEquipHandEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        RefreshKnockedMovement(entity);
    }

    private void OnHandUnequipped(Entity<KnockedDownComponent> entity, ref DidUnequipHandEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        RefreshKnockedMovement(entity);
    }

    private void OnHandCountChanged(Entity<KnockedDownComponent> entity, ref HandCountChangedEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        RefreshKnockedMovement(entity);
    }

    private void OnKnockdownAttempt(Entity<GravityAffectedComponent> entity, ref KnockDownAttemptEvent args)
    {
        if (entity.Comp.Weightless)
            args.Cancelled = true;
    }

    private void OnGetStandUpTime(Entity<GravityAffectedComponent> entity, ref GetStandUpTimeEvent args)
    {
        if (entity.Comp.Weightless)
            args.DoAfterTime = TimeSpan.Zero;
    }

    #endregion

    #region Action Blockers

    private void OnStandAttempt(Entity<KnockedDownComponent> entity, ref StandAttemptEvent args)
    {
        if (entity.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnBuckleAttempt(Entity<KnockedDownComponent> entity, ref BuckleAttemptEvent args)
    {
        if (args.User == entity && entity.Comp.NextUpdate > GameTiming.CurTime)
            args.Cancelled = true;
    }

    #endregion

    #region DoAfter

    private void OnStandDoAfter(Entity<KnockedDownComponent> entity, ref TryStandDoAfterEvent args)
    {
        entity.Comp.DoAfterId = null;

        if (args.Cancelled || StandingBlocked(entity))
        {
            DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.DoAfterId));
            return;
        }

        RemComp<KnockedDownComponent>(entity);
        _standingState.Stand(entity);

        RemComp<KnockedDownComponent>(entity);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(entity):user} has stood up from knockdown.");
    }

    #endregion

    #region Movement and Friction

    private void RefreshKnockedMovement(Entity<KnockedDownComponent> ent)
    {
        var ev = new KnockedDownRefreshEvent();
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.SpeedModifier = ev.SpeedModifier;
        ent.Comp.FrictionModifier = ev.FrictionModifier;
        Dirty(ent);

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        _movementSpeedModifier.RefreshFrictionModifiers(ent);
    }

    private void OnRefreshKnockedSpeed(Entity<KnockedDownComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(entity.Comp.SpeedModifier);
    }

    private void OnKnockedTileFriction(Entity<KnockedDownComponent> entity, ref TileFrictionEvent args)
    {
        args.Modifier *= entity.Comp.FrictionModifier;
    }

    private void OnRefreshFriction(Entity<KnockedDownComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(entity.Comp.FrictionModifier);
        args.ModifyAcceleration(entity.Comp.FrictionModifier);
    }

    #endregion
}