using System.Collections.Generic;
using Content.Shared._VG.Tools;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Audio;

namespace Content.Server._VG.Tools;

/// <summary>
/// Система для спавна эффектов сварки (искры, анимация) и проигрывания звука.
/// </summary>
public sealed class WeldingSparksSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // Храним активные DoAfter и связанные с ними данные
    private readonly Dictionary<(EntityUid user, ushort index), WeldingSparksData> _activeWelding = new();

    private struct WeldingSparksData
    {
        public EntityUid EffectEntity;
        public EntityUid ToolEntity;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DoAfterComponent>();
        while (query.MoveNext(out var uid, out var doAfterComp))
        {
            // Проходим по всем DoAfter в компоненте
            foreach (var (idx, doAfter) in doAfterComp.DoAfters)
            {
                var key = (uid, idx);

                // Если DoAfter завершён или отменён
                if (doAfter.Completed || doAfter.Cancelled)
                {
                    if (_activeWelding.TryGetValue(key, out var data))
                    {
                        // Останавливаем звук
                        StopWeldingSound(data.ToolEntity);
                        
                        // Удаляем эффект
                        if (Exists(data.EffectEntity))
                            QueueDel(data.EffectEntity);
                        
                        _activeWelding.Remove(key);
                    }
                    continue;
                }

                // Если DoAfter активен и ещё не обработан
                if (!_activeWelding.ContainsKey(key))
                {
                    // Проверяем, что инструмент (Used) имеет компонент WeldingSparks
                    if (doAfter.Args.Used != null && 
                        TryComp<WeldingSparksComponent>(doAfter.Args.Used, out var sparks) &&
                        doAfter.Args.Target is { } target)
                    {
                        // Создаём эффект на координатах цели
                        var effect = Spawn(sparks.EffectPrototype, Transform(target).Coordinates);
                        
                        // Запускаем звук сварки
                        StartWeldingSound(doAfter.Args.Used.Value);
                        
                        // Сохраняем данные
                        _activeWelding[key] = new WeldingSparksData
                        {
                            EffectEntity = effect,
                            ToolEntity = doAfter.Args.Used.Value
                        };

                        // Отправляем клиенту событие для анимации
                        RaiseNetworkEvent(new SpawnedWeldingSparksEvent(
                            GetNetEntity(target),
                            GetNetEntity(effect),
                            doAfter.Args.Delay
                        ));
                    }
                    else
                    {
                        // Неподходящий инструмент или нет цели — помечаем как обработанный
                        _activeWelding[key] = new WeldingSparksData
                        {
                            EffectEntity = EntityUid.Invalid,
                            ToolEntity = EntityUid.Invalid
                        };
                    }
                }
            }
        }

        // Очищаем устаревшие записи с Invalid
        CleanupInvalidEntries();
    }

    /// <summary>
    /// Запускает зацикленный звук сварки на инструменте.
    /// </summary>
    private void StartWeldingSound(EntityUid tool)
    {
        if (!TryComp<WeldingSoundComponent>(tool, out var soundComp))
            return;

        // Если звук уже играет, не запускаем новый
        if (soundComp.StreamHandle != null && Exists(soundComp.StreamHandle))
            return;

        var audioParams = AudioParams.Default.WithVolume(soundComp.Volume).WithLoop(true);
        var stream = _audio.PlayPvs(soundComp.Sound, tool, audioParams);
        
        if (stream != null)
        {
            soundComp.StreamHandle = stream.Value.Entity;
            Dirty(tool, soundComp);
        }
    }

    /// <summary>
    /// Останавливает зацикленный звук сварки на инструменте.
    /// </summary>
    private void StopWeldingSound(EntityUid tool)
    {
        if (!TryComp<WeldingSoundComponent>(tool, out var soundComp))
            return;

        if (soundComp.StreamHandle != null)
        {
            _audio.Stop(soundComp.StreamHandle.Value);
            soundComp.StreamHandle = null;
            Dirty(tool, soundComp);
        }
    }

    /// <summary>
    /// Очищает записи с невалидными сущностями.
    /// </summary>
    private void CleanupInvalidEntries()
    {
        var toRemove = new List<(EntityUid, ushort)>();
        
        foreach (var kvp in _activeWelding)
        {
            if (kvp.Value.EffectEntity == EntityUid.Invalid && 
                kvp.Value.ToolEntity == EntityUid.Invalid)
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in toRemove)
        {
            _activeWelding.Remove(key);
        }
    }
}