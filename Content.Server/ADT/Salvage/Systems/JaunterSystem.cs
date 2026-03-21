using Content.Server.ADT.Salvage.Components;
using Content.Shared.Damage.Systems;
using Content.Server.Medical;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Interaction.Events;
using Content.Server.Interaction;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chasm;
using Content.Shared.Inventory;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Medical;
// VG-Tweak Start
using Content.Server.Mech.Components;
using Content.Shared.Mech.Components;
using Content.Server.Mech.Systems;
// VG-Tweak End

namespace Content.Server.ADT.Salvage.Systems;

public sealed class JaunterSystem : EntitySystem
{
    [Dependency] private readonly JaunterPortalSystem _portal = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    // VG-Tweak Start
    [Dependency] private readonly MechSystem _mech = default!;
    // VG-Tweak End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JaunterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<JaunterComponent, BeforeChasmFallingEvent>(OnBeforeFall);
        SubscribeLocalEvent<InventoryComponent, BeforeChasmFallingEvent>(OnInventoryBeforeFall);
        // VG-Tweak Start: обработка падения мехов
        SubscribeLocalEvent<MechComponent, BeforeChasmFallingEvent>(OnMechBeforeFall);
        // VG-Tweak End
    }

    private void OnUseInHand(EntityUid uid, JaunterComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        _portal.SpawnLinkedPortal(args.User);
        QueueDel(uid);
        args.Handled = true;
    }

    // Обработка падения самого джаунтера (например, если он выброшен на пол и падает в яму)
    private void OnBeforeFall(EntityUid uid, JaunterComponent comp, ref BeforeChasmFallingEvent args)
    {
        args.Cancelled = true;
        TeleportEntity(uid, comp, uid); // Сам джаунтер телепортируется
    }

    // Новый обработчик для сущности с инвентарём (игрок, животное и т.п.)
    private void OnInventoryBeforeFall(EntityUid uid, InventoryComponent comp, ref BeforeChasmFallingEvent args)
    {
        if (args.Cancelled)
            return;

        // Ищем джаунтер в инвентаре и вложенных контейнерах
        if (TryFindJaunterInInventory(uid, out var jaunter))
        {
            args.Cancelled = true;
            if (TryComp<JaunterComponent>(jaunter, out var jaunterComp))
            {
                TeleportEntity(uid, jaunterComp, jaunter);
            }
        }
    }

    // Вспомогательный метод для поиска джаунтера во всех контейнерах сущности
    private bool TryFindJaunterInInventory(EntityUid entity, out EntityUid jaunter)
    {
        jaunter = default;

        // Проверяем сам объект
        if (TryComp<JaunterComponent>(entity, out _))
        {
            jaunter = entity;
            return true;
        }

        // Проверяем все контейнеры (рюкзак, ящики, карманы и т.д.)
        if (!TryComp<ContainerManagerComponent>(entity, out var containerManager))
            return false;

        foreach (var container in containerManager.Containers.Values)
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (TryFindJaunterInInventory(contained, out jaunter))
                    return true;
            }
        }

        return false;
    }

    // Основная логика телепортации сущности с использованием джаунтера
    private void TeleportEntity(EntityUid target, JaunterComponent comp, EntityUid jaunterUsed)
    {
        var coordsValid = false;
        var currentCoords = Transform(target).Coordinates;
        EntityCoordinates newCoords;

        while (!coordsValid)
        {
            var randomBeacon = _portal.GetRandomBeacon();
            if (randomBeacon != null)
            {
                newCoords = Transform(randomBeacon.Value).Coordinates;
                comp.BeaconMode = true;
            }
            else
            {
                newCoords = new EntityCoordinates(Transform(target).ParentUid,
                    currentCoords.X + _random.NextFloat(-5f, 5f),
                    currentCoords.Y + _random.NextFloat(-5f, 5f));
            }

            // Проверка на отсутствие препятствий и ям
            if (_interaction.InRangeUnobstructed(target, newCoords, -1f) &&
                _lookup.GetEntitiesInRange<ChasmComponent>(newCoords, 1f).Count <= 0 ||
                comp.BeaconMode)
            {
                _transform.SetCoordinates(target, newCoords);
                _transform.AttachToGridOrMap(target, Transform(target));
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg"), target);

                // Эффекты стамины
                if (TryComp<StaminaComponent>(target, out var stam))
                {
                    var need = MathF.Max(0.01f, stam.CritThreshold - stam.StaminaDamage);
                    _stamina.TakeStaminaDamage(target, need, stam);
                }

                // Рвота для живых существ
                if (HasComp<BodyComponent>(target) && HasComp<HungerComponent>(target))
                {
                    _vomit.Vomit(target);
                }

                // Удаление использованного джаунтера, если нужно
                if (comp.DeleteOnUse && target != jaunterUsed)
                {
                    QueueDel(jaunterUsed);
                }

                coordsValid = true;
            }
        }
    }

    // VG-Tweak Start
    private void OnMechBeforeFall(EntityUid uid, MechComponent mech, ref BeforeChasmFallingEvent args)
    {
        if (args.Cancelled)
            return;

        var pilot = mech.PilotSlot.ContainedEntity;
        if (pilot == null)
            return;

        // Ищем джаунтер у пилота
        if (!TryFindJaunterInInventory(pilot.Value, out var jaunter))
            return;

        // Выбрасываем пилота из меха
        if (!_mech.TryEject(uid, mech))
            return;

        // Телепортируем пилота
        if (TryComp<JaunterComponent>(jaunter, out var jaunterComp))
        {
            TeleportEntity(pilot.Value, jaunterComp, jaunter);
        }
    }
    // VG-Tweak End
}