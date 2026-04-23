using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Stunnable;
using Content.Server.Temperature.Systems;
using Content.Server.Damage.Components;
using Content.Shared._VG.Mood; // VG-Tweak
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Temperature.Components;
using Robust.Server.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Server.ADT.Temperature;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed class FlammableSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;
        [Dependency] private readonly SharedIgnitionSourceSystem _ignitionSourceSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly FixtureSystem _fixture = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private EntityQuery<InventoryComponent> _inventoryQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;

        private const float UpdateTime = 1f;
        private float _timer;

        private readonly Dictionary<Entity<FlammableComponent>, float> _fireEvents = new();

        public override void Initialize()
        {
            UpdatesAfter.Add(typeof(AtmosphereSystem));

            _inventoryQuery = GetEntityQuery<InventoryComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            SubscribeLocalEvent<FlammableComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<FlammableComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<FlammableComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<FlammableComponent, IsHotEvent>(OnIsHot);
            SubscribeLocalEvent<FlammableComponent, TileFireEvent>(OnTileFire);
            SubscribeLocalEvent<FlammableComponent, RejuvenateEvent>(OnRejuvenate);
            SubscribeLocalEvent<FlammableComponent, ResistFireAlertEvent>(OnResistFireAlert);
            Subs.SubscribeWithRelay<FlammableComponent, ExtinguishEvent>(OnExtinguishEvent);

            SubscribeLocalEvent<IgniteOnCollideComponent, StartCollideEvent>(IgniteOnCollide);
            SubscribeLocalEvent<IgniteOnCollideComponent, LandEvent>(OnIgniteLand);

            SubscribeLocalEvent<IgniteOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);

            SubscribeLocalEvent<ExtinguishOnInteractComponent, ActivateInWorldEvent>(OnExtinguishActivateInWorld);

            SubscribeLocalEvent<IgniteOnHeatDamageComponent, DamageChangedEvent>(OnDamageChanged);
        }

        private void OnExtinguishEvent(Entity<FlammableComponent> ent, ref ExtinguishEvent args)
        {
            Extinguish(ent, ent.Comp);
            AdjustFireStacks(ent, args.FireStacksAdjustment, ent.Comp);
        }

        private void OnMeleeHit(EntityUid uid, IgniteOnMeleeHitComponent component, MeleeHitEvent args)
        {
            foreach (var entity in args.HitEntities)
            {
                if (!TryComp<FlammableComponent>(entity, out var flammable))
                    continue;

                AdjustFireStacks(entity, component.FireStacks, flammable);
                if (component.FireStacks >= 0)
                    Ignite(entity, args.Weapon, flammable, args.User);
            }
        }

        private void OnIgniteLand(EntityUid uid, IgniteOnCollideComponent component, ref LandEvent args)
        {
            RemCompDeferred<IgniteOnCollideComponent>(uid);
        }

        private void IgniteOnCollide(EntityUid uid, IgniteOnCollideComponent component, ref StartCollideEvent args)
        {
            if (!args.OtherFixture.Hard || component.Count == 0)
                return;

            var otherEnt = args.OtherEntity;

            if (!TryComp(otherEnt, out FlammableComponent? flammable))
                return;

            if (args.OurFixtureId != component.FixtureId && args.OurFixtureId != SharedProjectileSystem.ProjectileFixture)
            {
                return;
            }

            flammable.FireStacks += component.FireStacks;
            Ignite(otherEnt, uid, flammable);
            component.Count--;

            if (component.Count == 0)
                RemCompDeferred<IgniteOnCollideComponent>(uid);
        }

        private void OnMapInit(EntityUid uid, FlammableComponent component, MapInitEvent args)
        {
            if (!TryComp<PhysicsComponent>(uid, out var body))
                return;

            _fixture.TryCreateFixture(uid, component.FlammableCollisionShape, component.FlammableFixtureID, hard: false,
                collisionMask: (int) CollisionGroup.FullTileLayer, body: body);
        }

        private void OnInteractUsing(EntityUid uid, FlammableComponent flammable, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent);

            if (!isHotEvent.IsHot)
                return;

            Ignite(uid, args.Used, flammable, args.User);
            args.Handled = true;
        }

        private void OnExtinguishActivateInWorld(EntityUid uid, ExtinguishOnInteractComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            if (!TryComp(uid, out FlammableComponent? flammable))
                return;

            if (!flammable.OnFire)
                return;

            args.Handled = true;

            if (!TryComp(uid, out UseDelayComponent? useDelay) || !_useDelay.TryResetDelay((uid, useDelay), true))
                return;

            _audio.PlayPvs(component.ExtinguishAttemptSound, uid);

            if (_random.Prob(component.Probability))
            {
                AdjustFireStacks(uid, component.StackDelta, flammable);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString(component.ExtinguishFailed), uid);
            }
        }

        private void OnCollide(EntityUid uid, FlammableComponent flammable, ref StartCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            if (otherUid.Id < uid.Id)
                return;

            if (args.OtherFixtureId != flammable.FlammableFixtureID && args.OurFixtureId != flammable.FlammableFixtureID)
                return;

            if (!flammable.FireSpread)
                return;

            if (!TryComp(otherUid, out FlammableComponent? otherFlammable) || !otherFlammable.FireSpread)
                return;

            if (!flammable.OnFire && !otherFlammable.OnFire)
                return;

            var mass1 = 1f;
            var mass2 = 1f;
            if (_physicsQuery.TryComp(uid, out var physics) && _physicsQuery.TryComp(otherUid, out var otherPhys))
            {
                mass1 = physics.Mass;
                mass2 = otherPhys.Mass;
            }

            var total = mass1 + mass2;
            var avg = (flammable.FireStacks + otherFlammable.FireStacks) / total;

            var (src, dest) = flammable.FireStacks * mass1 > otherFlammable.FireStacks * mass2
                ? (-1f, 1f)
                : (1f, -1f);

            AdjustFireStacks(uid, src * avg * mass2, flammable, ignite: true);
            AdjustFireStacks(otherUid, dest * avg * mass1, otherFlammable, ignite: true);
        }

        private void OnIsHot(EntityUid uid, FlammableComponent flammable, IsHotEvent args)
        {
            args.IsHot = flammable.OnFire;
        }

        private void OnTileFire(Entity<FlammableComponent> ent, ref TileFireEvent args)
        {
            var tempDelta = args.Temperature - ent.Comp.MinIgnitionTemperature;

            _fireEvents.TryGetValue(ent, out var maxTemp);

            if (tempDelta > maxTemp)
                _fireEvents[ent] = tempDelta;
        }

        private void OnRejuvenate(EntityUid uid, FlammableComponent component, RejuvenateEvent args)
        {
            Extinguish(uid, component);
        }

        private void OnResistFireAlert(Entity<FlammableComponent> ent, ref ResistFireAlertEvent args)
        {
            if (args.Handled)
                return;

            Resist(ent, ent);
            args.Handled = true;
        }

        public void UpdateAppearance(EntityUid uid, FlammableComponent? flammable = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref flammable, ref appearance))
                return;

            _appearance.SetData(uid, FireVisuals.OnFire, flammable.OnFire, appearance);
            _appearance.SetData(uid, FireVisuals.FireStacks, flammable.FireStacks, appearance);
            _appearance.SetData(uid, ToggleableVisuals.Enabled, flammable.OnFire, appearance);
        }

        public void AdjustFireStacks(EntityUid uid, float relativeFireStacks, FlammableComponent? flammable = null, bool ignite = false)
        {
            if (!Resolve(uid, ref flammable))
                return;

            SetFireStacks(uid, flammable.FireStacks + relativeFireStacks, flammable, ignite);
        }

        public void SetFireStacks(EntityUid uid, float stacks, FlammableComponent? flammable = null, bool ignite = false)
        {
            if (!Resolve(uid, ref flammable))
                return;

            flammable.FireStacks = MathF.Min(MathF.Max(flammable.MinimumFireStacks, stacks), flammable.MaximumFireStacks);

            if (flammable.FireStacks <= 0)
            {
                Extinguish(uid, flammable);
            }
            else
            {
                flammable.OnFire |= ignite;
                UpdateAppearance(uid, flammable);
            }
        }

        public void Extinguish(EntityUid uid, FlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (!flammable.OnFire || !flammable.CanExtinguish)
                return;

            _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):entity} stopped being on fire damage");
            flammable.OnFire = false;
            flammable.FireStacks = 0;

            _ignitionSourceSystem.SetIgnited(uid, false);

            var extinguished = new ExtinguishedEvent();
            RaiseLocalEvent(uid, ref extinguished);

            var ev = new OnFireChangedEvent(flammable.OnFire);
            RaiseLocalEvent(uid, ref ev);

            UpdateAppearance(uid, flammable);
        }

        public void Ignite(EntityUid uid, EntityUid ignitionSource, FlammableComponent? flammable = null,
            EntityUid? ignitionSourceUser = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (flammable.AlwaysCombustible)
            {
                flammable.FireStacks = Math.Max(flammable.FirestacksOnIgnite, flammable.FireStacks);
            }

            if (flammable.FireStacks > 0 && !flammable.OnFire)
            {
                if (ignitionSourceUser != null)
                    _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on fire by {ToPrettyString(ignitionSourceUser.Value):actor} with {ToPrettyString(ignitionSource):tool}");
                else
                    _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on fire by {ToPrettyString(ignitionSource):actor}");
                flammable.OnFire = true;

                var extinguished = new IgnitedEvent();
                RaiseLocalEvent(uid, ref extinguished);

                var ev = new OnFireChangedEvent(flammable.OnFire);
                RaiseLocalEvent(uid, ref ev);
            }

            UpdateAppearance(uid, flammable);

            if (flammable.FirestackFadeOnIgnite != null)
                flammable.FirestackFade = flammable.FirestackFadeOnIgnite.Value;
        }

        private void OnDamageChanged(EntityUid uid, IgniteOnHeatDamageComponent component, DamageChangedEvent args)
        {
            if (!TryComp<FlammableComponent>(uid, out var flammable))
                return;

            if (args.DamageDelta == null)
                return;

            if (args.DamageDelta.DamageDict.TryGetValue("Heat", out FixedPoint2 value))
            {
                if (value <= component.Threshold)
                    return;

                flammable.FireStacks += component.FireStacks;
                Ignite(uid, uid, flammable);
            }
        }

        public void Resist(EntityUid uid,
            FlammableComponent? flammable = null)
        {
            if (!Resolve(uid, ref flammable))
                return;

            if (!flammable.OnFire || !_actionBlockerSystem.CanInteract(uid, null) || flammable.Resisting)
                return;

            flammable.Resisting = true;

            _popup.PopupEntity(Loc.GetString("flammable-component-resist-message"), uid, uid);
            _stunSystem.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(2f));

            uid.SpawnTimer(2000, () =>
            {
                flammable.Resisting = false;
                flammable.FireStacks -= 1f;
                UpdateAppearance(uid, flammable);
            });
        }

        public override void Update(float frameTime)
        {
            foreach (var (flammable, deltaTemp) in _fireEvents)
            {
                var fireStackMod = Math.Max(MathF.Log2(deltaTemp / 100) + 1, 0);
                var fireStackDelta = fireStackMod - flammable.Comp.FireStacks;
                var flammableEntity = flammable.Owner;
                if (fireStackDelta > 0)
                {
                    AdjustFireStacks(flammableEntity, fireStackDelta, flammable);
                }
                Ignite(flammableEntity, flammableEntity, flammable);
            }
            _fireEvents.Clear();

            _timer += frameTime;

            if (_timer < UpdateTime)
                return;

            _timer -= UpdateTime;

            var query = EntityQueryEnumerator<FlammableComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var flammable, out _))
            {
                if (flammable.FireStacks < 0)
                {
                    flammable.FireStacks = MathF.Min(0, flammable.FireStacks + 1);
                }

                if (!flammable.OnFire)
                {
                    _alertsSystem.ClearAlert(uid, flammable.FireAlert);
                    continue;
                }

                _alertsSystem.ShowAlert(uid, flammable.FireAlert);

                RaiseLocalEvent(uid, new MoodEffectEvent("OnFire")); // VG-Tweak

                if (flammable.FireStacks > 0)
                {
                    var air = _atmosphereSystem.GetContainingMixture(uid);

                    if (air == null || air.GetMoles(Gas.Oxygen) < 1f)
                    {
                        Extinguish(uid, flammable);
                        continue;
                    }

                    var source = EnsureComp<IgnitionSourceComponent>(uid);
                    _ignitionSourceSystem.SetIgnited((uid, source));

                    if (TryComp(uid, out TemperatureComponent? temp))
                        _temperatureSystem.ChangeHeat(uid, 12500 * flammable.FireStacks, false, temp);

                    var ev = new GetFireProtectionEvent();
                    RaiseLocalEvent(uid, ref ev);
                    if (_inventoryQuery.TryComp(uid, out var inv))
                        _inventory.RelayEvent((uid, inv), ref ev);

                    _damageableSystem.TryChangeDamage(uid, flammable.Damage * flammable.FireStacks * ev.Multiplier, interruptsDoAfters: false);

                    AdjustFireStacks(uid, flammable.FirestackFade * (flammable.Resisting ? 10f : 1f), flammable, flammable.OnFire);

                    if (flammable.FirestackFadeFade != 0)
                        flammable.FirestackFade += flammable.FirestackFadeFade * frameTime;
                }
                else
                {
                    Extinguish(uid, flammable);
                }
            }
        }
    }
}