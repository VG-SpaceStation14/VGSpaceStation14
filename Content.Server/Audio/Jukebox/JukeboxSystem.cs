using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using JukeboxComponent = Content.Shared.Audio.Jukebox.JukeboxComponent;

namespace Content.Server.Audio.Jukebox;

public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!; // VG-Tweak

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetVolumeMessage>(OnJukeboxSetVolume); /// ADT-Tweak
        // VG-Tweak start
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetRepeatMessage>(OnJukeboxSetRepeat);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetShuffleMessage>(OnJukeboxSetShuffle);
        // VG-Tweak end
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);
        // VG-Tweak start - новые подписки
        SubscribeLocalEvent<JukeboxComponent, JukeboxNextTrackMessage>(OnJukeboxNextTrack);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPrevTrackMessage>(OnJukeboxPrevTrack);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlaySelectedMessage>(OnJukeboxPlaySelected);
        // VG-Tweak end

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);
    }

    // VG-Tweak start
    private void OnJukeboxNextTrack(EntityUid uid, JukeboxComponent component, JukeboxNextTrackMessage args)
    {
        if (!component.AutoAdvance || component.AudioStream == null)
            return;

        var nextTrack = GetNextTrack(uid, component);
        if (nextTrack != null && _protoManager.Resolve(nextTrack.Value, out var nextProto))
        {
            component.SelectedSongId = nextTrack.Value;
            component.AudioStream = Audio.Stop(component.AudioStream);
            component.AudioStream = Audio.PlayPvs(nextProto.Path, uid, AudioParams.Default.WithMaxDistance(10f).WithVolume(MapToRange(component.Volume, component.MinSlider, component.MaxSlider, component.MinVolume, component.MaxVolume)))?.Entity;
            component.AutoAdvance = true;
            Dirty(uid, component);
        }
    }

    private void OnJukeboxPrevTrack(EntityUid uid, JukeboxComponent component, JukeboxPrevTrackMessage args)
    {
        if (!component.AutoAdvance || component.AudioStream == null)
            return;

        if (component.Queue.Count == 0 || component.CurrentQueueIndex < 0)
            return;

        int prevIndex;
        if (component.RepeatMode == JukeboxRepeatMode.RepeatAll)
        {
            prevIndex = component.CurrentQueueIndex - 1;
            if (prevIndex < 0)
                prevIndex = component.Queue.Count - 1;
        }
        else
        {
            prevIndex = component.CurrentQueueIndex - 1;
            if (prevIndex < 0)
                return; 
        }

        var prevTrack = component.Queue[prevIndex];
        if (_protoManager.Resolve(prevTrack, out var prevProto))
        {
            component.SelectedSongId = prevTrack;
            component.CurrentQueueIndex = prevIndex;
            component.AudioStream = Audio.Stop(component.AudioStream);
            component.AudioStream = Audio.PlayPvs(prevProto.Path, uid, AudioParams.Default.WithMaxDistance(10f).WithVolume(MapToRange(component.Volume, component.MinSlider, component.MaxSlider, component.MinVolume, component.MaxVolume)))?.Entity;
            component.AutoAdvance = true;
            Dirty(uid, component);
        }
    }

    private void OnJukeboxPlaySelected(EntityUid uid, JukeboxComponent component, JukeboxPlaySelectedMessage args)
    {
        component.SelectedSongId = args.SongId;
        component.AudioStream = Audio.Stop(component.AudioStream);

        if (_protoManager.Resolve(args.SongId, out var jukeboxProto))
        {
            component.AudioStream = Audio.PlayPvs(jukeboxProto.Path, uid, AudioParams.Default.WithMaxDistance(10f).WithVolume(MapToRange(component.Volume, component.MinSlider, component.MaxSlider, component.MinVolume, component.MaxVolume)))?.Entity;

            component.Queue.Clear();
            component.Queue.AddRange(component.ShuffleEnabled
                ? component.Playlist.OrderBy(_ => _random.Next()).ToList()
                : component.Playlist.ToList());

            var index = component.Queue.IndexOf(args.SongId);
            component.CurrentQueueIndex = index >= 0 ? index : 0;
            component.AutoAdvance = true;

            DirectSetVisualState(uid, JukeboxVisualState.Select);
            component.Selecting = true;
            component.SelectAccumulator = 0f;
        
            Dirty(uid, component);
        }
    }
    // VG-Tweak end

    private void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        // VG-Tweak start
        RefreshPlaylist(uid, component);
        // VG-Tweak end

        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }

    // VG-Tweak start
    private void RefreshPlaylist(EntityUid uid, JukeboxComponent component)
    {
        component.Playlist.Clear();
        foreach (var proto in _protoManager.EnumeratePrototypes<JukeboxPrototype>())
        {
            component.Playlist.Add(proto.ID);
        }
    }

    private ProtoId<JukeboxPrototype>? GetNextTrack(EntityUid uid, JukeboxComponent component)
    {
        if (component.Playlist.Count == 0)
            return null;

        // Если очередь пуста, перестраиваем очередь
        if (component.Queue.Count == 0)
        {
            component.Queue.Clear();
            component.Queue.AddRange(component.ShuffleEnabled
                ? component.Playlist.OrderBy(_ => _random.Next()).ToList()
                : component.Playlist.ToList());
            component.CurrentQueueIndex = -1;
        }

        switch (component.RepeatMode)
        {
            case JukeboxRepeatMode.RepeatOne:
                // Просто играем тот же трек
                return component.SelectedSongId;

            case JukeboxRepeatMode.RepeatAll:
                // Переходим к следующему в очереди, зацикливаем
                component.CurrentQueueIndex++;
                if (component.CurrentQueueIndex >= component.Queue.Count)
                    component.CurrentQueueIndex = 0;
                return component.Queue[component.CurrentQueueIndex];

            case JukeboxRepeatMode.NoRepeat:
            default:
                // Переходим к следующему в очереди, останавливаемся если в конце
                component.CurrentQueueIndex++;
                if (component.CurrentQueueIndex >= component.Queue.Count)
                {
                    component.CurrentQueueIndex = -1;
                    return null;
                }
                return component.Queue[component.CurrentQueueIndex];
        }
    }

    private void OnJukeboxSetRepeat(EntityUid uid, JukeboxComponent component, JukeboxSetRepeatMessage args)
    {
        component.RepeatMode = args.Mode;
        Dirty(uid, component);
    }

    private void OnJukeboxSetShuffle(EntityUid uid, JukeboxComponent component, JukeboxSetShuffleMessage args)
    {
        if (component.ShuffleEnabled == args.Enabled)
            return;

        component.ShuffleEnabled = args.Enabled;

        // Перестраиваем очередь с новым режимом перемешивания
        component.Queue.Clear();
        component.Queue.AddRange(component.ShuffleEnabled
            ? component.Playlist.OrderBy(_ => _random.Next()).ToList()
            : component.Playlist.ToList());

        // Пытаемся найти текущий трек в новой очереди
        if (component.SelectedSongId != null)
        {
            var index = component.Queue.IndexOf(component.SelectedSongId.Value);
            component.CurrentQueueIndex = index >= 0 ? index : 0;
        }
        else
        {
            component.CurrentQueueIndex = -1;
        }

        Dirty(uid, component);
    }
    // VG-Tweak end

    private void OnJukeboxPlay(EntityUid uid, JukeboxComponent component, ref JukeboxPlayingMessage args)
    {
        if (Exists(component.AudioStream))
        {
            Audio.SetState(component.AudioStream, AudioState.Playing);
            // VG-Tweak start
            component.AutoAdvance = true;
            // VG-Tweak end
        }
        else
        {
            component.AudioStream = Audio.Stop(component.AudioStream);

            if (string.IsNullOrEmpty(component.SelectedSongId) ||
                !_protoManager.Resolve(component.SelectedSongId, out var jukeboxProto))
            {
                // VG-Tweak start
                // Пробуем играть первый трек из очереди
                var nextTrack = GetNextTrack(uid, component);
                if (nextTrack == null || !_protoManager.Resolve(nextTrack.Value, out jukeboxProto))
                    return;

                component.SelectedSongId = nextTrack.Value;
                component.AutoAdvance = true;
                // VG-Tweak end
            }

            component.AudioStream = Audio.PlayPvs(jukeboxProto.Path, uid, AudioParams.Default.WithMaxDistance(10f).WithVolume(MapToRange(component.Volume, component.MinSlider, component.MaxSlider, component.MinVolume, component.MaxVolume)))?.Entity; /// ADT-Tweak
            Dirty(uid, component);
        }
    }

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
        // VG-Tweak start
        ent.Comp.AutoAdvance = false;
        // VG-Tweak end
    }

    private void OnJukeboxSetTime(EntityUid uid, JukeboxComponent component, JukeboxSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            Audio.SetPlaybackPosition(component.AudioStream, args.SongTime + offset);
        }
    }

    /// ADT-Tweak start
    private void OnJukeboxSetVolume(EntityUid uid, JukeboxComponent component, JukeboxSetVolumeMessage args)
    {
        SetJukeboxVolume(uid, component, args.Volume);

        if (!TryComp<AudioComponent>(component.AudioStream, out var audioComponent))
            return;

        Audio.SetVolume(component.AudioStream, MapToRange(args.Volume, component.MinSlider, component.MaxSlider, component.MinVolume, component.MaxVolume));
    }
    /// ADT-Tweak end

    private void OnPowerChanged(Entity<JukeboxComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(entity);

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity);
        }
    }

    private void OnJukeboxStop(Entity<JukeboxComponent> entity, ref JukeboxStopMessage args)
    {
        Stop(entity);
    }

    private void Stop(Entity<JukeboxComponent> entity)
    {
        Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped);
        // VG-Tweak start
        entity.Comp.AutoAdvance = false;
        // VG-Tweak end
        Dirty(entity);
    }

    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        if (!Audio.IsPlaying(component.AudioStream))
        {
            component.SelectedSongId = args.SongId;
            DirectSetVisualState(uid, JukeboxVisualState.Select);
            component.Selecting = true;
            component.AudioStream = Audio.Stop(component.AudioStream);

            // VG-Tweak start
            // Обновляем очередь, чтобы начинать с выбранной песни
            component.Queue.Clear();
            component.Queue.AddRange(component.ShuffleEnabled
                ? component.Playlist.OrderBy(_ => _random.Next()).ToList()
                : component.Playlist.ToList());

            var index = component.Queue.IndexOf(args.SongId);
            component.CurrentQueueIndex = index >= 0 ? index : 0;
            component.AutoAdvance = false;
            // VG-Tweak end
        }

        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Selecting)
            {
                comp.SelectAccumulator += frameTime;
                if (comp.SelectAccumulator >= 0.5f)
                {
                    comp.SelectAccumulator = 0f;
                    comp.Selecting = false;
                    TryUpdateVisualState(uid, comp);
                }
            }

            // VG-Tweak start
            // Проверяем, закончился ли текущий трек
            if (comp.AutoAdvance && comp.AudioStream != null)
            {
                if (!TryComp<AudioComponent>(comp.AudioStream, out var audio))
                {
                    // Аудио компонент пропал - трек точно закончился
                    TryAdvanceToNextTrack(uid, comp);
                    continue;
                }

                // Получаем длину трека из прототипа
                if (_protoManager.TryIndex(comp.SelectedSongId, out var trackProto))
                {
                    var length = (float)Audio.GetAudioLength(trackProto.Path.Path.ToString()).TotalSeconds;
                    
                    // Проверяем по позиции воспроизведения
                    if (audio.PlaybackPosition >= length - 0.2f) // Небольшой допуск
                    {
                        TryAdvanceToNextTrack(uid, comp);
                    }
                }
            }
            // VG-Tweak end
        }
    }

    // VG-Tweak start
    private void TryAdvanceToNextTrack(EntityUid uid, JukeboxComponent comp)
    {
        var nextTrack = GetNextTrack(uid, comp);
        if (nextTrack != null && _protoManager.Resolve(nextTrack.Value, out var nextProto))
        {
            comp.SelectedSongId = nextTrack.Value;
            comp.AudioStream = Audio.Stop(comp.AudioStream);
            comp.AudioStream = Audio.PlayPvs(nextProto.Path, uid, AudioParams.Default.WithMaxDistance(10f).WithVolume(MapToRange(comp.Volume, comp.MinSlider, comp.MaxSlider, comp.MinVolume, comp.MaxVolume)))?.Entity;
            comp.AutoAdvance = true;
            Dirty(uid, comp);
        }
        else
        {
            // Конец плейлиста без повтора
            comp.AutoAdvance = false;
            comp.AudioStream = Audio.Stop(comp.AudioStream);
            comp.SelectedSongId = null;
            comp.CurrentQueueIndex = -1;
            Dirty(uid, comp);
        }
    }
    // VG-Tweak end

    /// ADT-Tweak start
    private void SetJukeboxVolume(EntityUid uid, JukeboxComponent component, float volume)
    {
        component.Volume = volume;
        Dirty(uid, component);
    }
    /// ADT-Tweak end
    
    private void OnComponentShutdown(EntityUid uid, JukeboxComponent component, ComponentShutdown args)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
    }

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(EntityUid uid, JukeboxComponent? jukeboxComponent = null)
    {
        if (!Resolve(uid, ref jukeboxComponent))
            return;

        var finalState = JukeboxVisualState.On;

        if (!this.IsPowered(uid, EntityManager))
        {
            finalState = JukeboxVisualState.Off;
        }

        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, finalState);
    }
}