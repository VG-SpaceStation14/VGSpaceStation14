using Content.Shared.Audio.Jukebox;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Audio.Jukebox;

public sealed class JukeboxBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    [ViewVariables]
    private JukeboxMenu? _menu;

    public JukeboxBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<JukeboxMenu>();

        _menu.OnPlayPressed += args =>
        {
            if (args)
            {
                SendMessage(new JukeboxPlayingMessage());
            }
            else
            {
                SendMessage(new JukeboxPauseMessage());
            }
        };

        _menu.OnStopPressed += () =>
        {
            SendMessage(new JukeboxStopMessage());
        };

        // VG-Tweak start - изменена логика
        _menu.OnSongSelected += SelectSong; 
        _menu.OnPlaySelected += PlaySelectedSong; 
        // VG-Tweak end

        _menu.SetTime += SetTime;
        _menu.SetVolume += SetVolume; // ADT-Tweak

        // VG-Tweak start
        _menu.OnRepeatModeChanged += mode =>
        {
            SendMessage(new JukeboxSetRepeatMessage(mode));
        };

        _menu.OnShuffleToggled += enabled =>
        {
            SendMessage(new JukeboxSetShuffleMessage(enabled));
        };

        _menu.OnNextTrack += () =>
        {
            SendMessage(new JukeboxNextTrackMessage());
        };

        _menu.OnPrevTrack += () =>
        {
            SendMessage(new JukeboxPrevTrackMessage());
        };
        // VG-Tweak end

        PopulateMusic();
        Reload();
    }

    // VG-Tweak start
    private void PlaySelectedSong(ProtoId<JukeboxPrototype> songid)
    {
        SendMessage(new JukeboxPlaySelectedMessage(songid));
    }
    // VG-Tweak end

    /// <summary>
    /// Reloads the attached menu if it exists.
    /// </summary>
    public void Reload()
    {
        if (_menu == null || !EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox))
            return;

        _menu.SetAudioStream(jukebox.AudioStream);
        _menu.SetVolumeSlider(jukebox.Volume); // ADT-Tweak

        // VG-Tweak start
        _menu.SetRepeatMode(jukebox.RepeatMode);
        _menu.SetShuffleEnabled(jukebox.ShuffleEnabled);
        // VG-Tweak end

        if (_protoManager.Resolve(jukebox.SelectedSongId, out var songProto))
        {
            var length = EntMan.System<AudioSystem>().GetAudioLength(songProto.Path.Path.ToString());
            _menu.SetSelectedSong(songProto.Name, (float)length.TotalSeconds); // ADT-Tweak
        }
        else
        {
            _menu.SetSelectedSong(string.Empty, 0f);
        }
    }

    public void PopulateMusic()
    {
        _menu?.Populate(_protoManager.EnumeratePrototypes<JukeboxPrototype>());
    }

    public void SelectSong(ProtoId<JukeboxPrototype> songid)
    {
        SendMessage(new JukeboxSelectedMessage(songid));
    }

    public void SetTime(float time)
    {
        var sentTime = time;

        if (EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox) &&
            EntMan.TryGetComponent(jukebox.AudioStream, out AudioComponent? audioComp))
        {
            audioComp.PlaybackPosition = time;
        }

        SendMessage(new JukeboxSetTimeMessage(sentTime));
    }

    /// ADT-Tweak start
    public void SetVolume(float volume)
    {
        var sentVolume = volume;

        if (EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox) &&
            EntMan.TryGetComponent(jukebox.AudioStream, out AudioComponent? audioComp))
        {
            audioComp.Volume = SharedJukeboxSystem.MapToRange(volume, jukebox.MinSlider, jukebox.MaxSlider, jukebox.MinVolume, jukebox.MaxVolume);
        }

        SendMessage(new JukeboxSetVolumeMessage(sentVolume));
    }
    /// ADT-Tweak end
}