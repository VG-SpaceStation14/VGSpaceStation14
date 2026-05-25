using System.Linq;
using Content.Client._VG.Sponsors;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Humanoid;

/// <summary>
/// View model for marking customization UI.
/// Adapted for VG's MarkingSet/MarkingCategories architecture.
/// </summary>
public sealed class MarkingsViewModel
{
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SponsorsManager _sponsorsManager = default!;

    private MarkingSet _markings = new();
    private string _species = SharedHumanoidAppearanceSystem.DefaultSpecies;
    private Sex _sex = Sex.Unsexed;

    public Color SkinColor = Color.White;
    public Color EyeColor = Color.Black;
    public Marking? HairMarking;
    public Marking? FacialHairMarking;
    public bool Forced;
    public bool IgnoreSpecies;

    private readonly HashSet<MarkingCategories> _ignoreCategories = new();

    /// <summary>Fired when any marking changes in a specific category.</summary>
    public event Action<MarkingCategories>? MarkingsChanged;

    /// <summary>Fired when all markings are reset (species/sex change, SetData).</summary>
    public event Action? MarkingsReset;

    /// <summary>Fired when visible category list changes.</summary>
    public event Action? CategoriesChanged;

    public MarkingsViewModel()
    {
        IoCManager.InjectDependencies(this);
    }

    public HashSet<MarkingCategories> IgnoreCategories => _ignoreCategories;

    public void SetIgnoreCategories(IEnumerable<MarkingCategories> categories)
    {
        _ignoreCategories.Clear();
        foreach (var c in categories)
            _ignoreCategories.Add(c);
        CategoriesChanged?.Invoke();
    }

    public IEnumerable<MarkingCategories> GetVisibleCategories()
    {
        foreach (var c in Enum.GetValues<MarkingCategories>())
        {
            if (_ignoreCategories.Contains(c))
                continue;
            if (GetAvailableMarkings(c).Count == 0)
                continue;
            yield return c;
        }
    }

    public void SetData(List<Marking> markings, string species, Sex sex, Color skinColor, Color eyeColor)
    {
        var pointsProto = _prototype.Index<SpeciesPrototype>(species).MarkingPoints;
        _markings = new MarkingSet(markings, pointsProto, _marking, _prototype);
        if (!IgnoreSpecies)
            _markings.EnsureSpecies(species, skinColor, _marking, _prototype);
        _species = species;
        _sex = sex;
        SkinColor = skinColor;
        EyeColor = eyeColor;
        MarkingsReset?.Invoke();
    }

    public void SetData(MarkingSet set, string species, Sex sex, Color skinColor, Color eyeColor)
    {
        _markings = set;
        if (!IgnoreSpecies)
            _markings.EnsureSpecies(species, skinColor, _marking, _prototype);
        _species = species;
        _sex = sex;
        SkinColor = skinColor;
        EyeColor = eyeColor;
        MarkingsReset?.Invoke();
    }

    public void SetSpecies(string species)
    {
        _species = species;
        var markingList = _markings.GetForwardEnumerator().ToList();
        var speciesProto = _prototype.Index<SpeciesPrototype>(species);
        _markings = new MarkingSet(markingList, speciesProto.MarkingPoints, _marking, _prototype);
        if (!IgnoreSpecies)
        {
            _markings.EnsureSpecies(species, null, _marking, _prototype);
            _markings.EnsureSexes(_sex, _marking);
        }
        MarkingsReset?.Invoke();
        CategoriesChanged?.Invoke();
    }

    public void SetSex(Sex sex)
    {
        _sex = sex;
        var markingList = _markings.GetForwardEnumerator().ToList();
        var speciesProto = _prototype.Index<SpeciesPrototype>(_species);
        _markings = new MarkingSet(markingList, speciesProto.MarkingPoints, _marking, _prototype);
        if (!IgnoreSpecies)
        {
            _markings.EnsureSpecies(_species, null, _marking, _prototype);
            _markings.EnsureSexes(_sex, _marking);
        }
        MarkingsReset?.Invoke();
        CategoriesChanged?.Invoke();
    }

    public IReadOnlyDictionary<string, MarkingPrototype> GetAvailableMarkings(MarkingCategories category)
    {
        return IgnoreSpecies
            ? _marking.MarkingsByCategoryAndSex(category, _sex)
            : _marking.MarkingsByCategoryAndSpeciesAndSex(category, _species, _sex);
    }

    public bool IsMarkingSelected(MarkingCategories category, string markingId)
    {
        return _markings.TryGetMarking(category, markingId, out _);
    }

    public Marking? GetMarking(MarkingCategories category, string markingId)
    {
        _markings.TryGetMarking(category, markingId, out var marking);
        return marking;
    }

    public bool IsMarkingColorCustomizable(MarkingCategories category, string markingId)
    {
        if (!_prototype.TryIndex<MarkingPrototype>(markingId, out var proto))
            return false;
        if (proto.ForcedColoring)
            return false;
        return !_marking.MustMatchSkin(_species, proto.BodyPart, out _, _prototype);
    }

    public bool IsSponsorOnly(string markingId)
    {
        if (!_prototype.TryIndex<MarkingPrototype>(markingId, out var proto))
            return false;
        return proto.SponsorOnly;
    }

    public bool IsSponsorAllowed(string markingId)
    {
        if (!_prototype.TryIndex<MarkingPrototype>(markingId, out var proto) || !proto.SponsorOnly)
            return true;
        if (_sponsorsManager.TryGetInfo(out var sponsor))
            return sponsor.AllowedMarkings.Contains(markingId);
        return false;
    }

    public bool TrySelectMarking(MarkingCategories category, string markingId)
    {
        if (!_prototype.TryIndex<MarkingPrototype>(markingId, out var proto))
            return false;

        var pointsLeft = _markings.PointsLeft(category);
        if (pointsLeft == 0 && !Forced)
            return false;

        var markingObject = proto.AsMarking();
        markingObject.Forced = Forced;

        var markingSet = new MarkingSet(_markings);
        if (HairMarking != null)
            markingSet.AddBack(MarkingCategories.Hair, HairMarking);
        if (FacialHairMarking != null)
            markingSet.AddBack(MarkingCategories.FacialHair, FacialHairMarking);

        if (!_marking.MustMatchSkin(_species, proto.BodyPart, out _, _prototype))
        {
            var colors = MarkingColoring.GetMarkingLayerColors(proto, SkinColor, EyeColor, markingSet);
            for (var i = 0; i < colors.Count; i++)
                markingObject.SetColor(i, colors[i]);
        }
        else
        {
            for (var i = 0; i < proto.Sprites.Count; i++)
                markingObject.SetColor(i, SkinColor);
        }

        _markings.AddBack(category, markingObject);
        MarkingsChanged?.Invoke(category);
        return true;
    }

    public bool TryDeselectMarking(MarkingCategories category, string markingId)
    {
        if (!_markings.TryGetMarking(category, markingId, out _))
            return false;
        _markings.Remove(category, markingId);
        MarkingsChanged?.Invoke(category);
        return true;
    }

    public void TrySetMarkingColor(MarkingCategories category, string markingId, int colorIndex, Color color)
    {
        var index = _markings.FindIndexOf(category, markingId);
        if (index < 0) return;
        var marking = new Marking(_markings.Markings[category][index]);
        marking.SetColor(colorIndex, color);
        _markings.Replace(category, index, marking);
        MarkingsChanged?.Invoke(category);
    }

    public List<Marking>? SelectedMarkings(MarkingCategories category)
    {
        return _markings.Markings.GetValueOrDefault(category);
    }

    public void ChangeMarkingOrder(MarkingCategories category, string markingId, CandidatePosition position, int positionIndex)
    {
        var markings = _markings.Markings.GetValueOrDefault(category);
        if (markings == null) return;

        var currentIndex = markings.FindIndex(m => m.MarkingId == markingId);
        if (currentIndex < 0) return;
        var marking = markings[currentIndex];

        markings.RemoveAt(currentIndex);

        int insertionIndex;
        if (position == CandidatePosition.Before)
            insertionIndex = currentIndex < positionIndex ? positionIndex - 1 : positionIndex;
        else
            insertionIndex = currentIndex > positionIndex ? positionIndex + 1 : positionIndex;

        insertionIndex = Math.Clamp(insertionIndex, 0, markings.Count);
        markings.Insert(insertionIndex, marking);
        MarkingsChanged?.Invoke(category);
    }

    public int PointsLeft(MarkingCategories category) => _markings.PointsLeft(category);

    public MarkingSet GetMarkings() => _markings;
}

public enum CandidatePosition
{
    Before,
    After,
}
