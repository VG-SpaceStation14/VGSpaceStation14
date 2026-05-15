using System.Linq;
using Content.Client.ADT.Language.UI;
using Content.Shared.ADT.Language;
using Content.Shared.Humanoid.Prototypes;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private string _languagesSearchText = string.Empty;

    public void RefreshLanguages()
    {
        if (Profile == null) return;
        var species = _prototypeManager.Index(Profile.Species);
        
        // Обновляем прогресс-бар
        UpdateLanguagesProgressBar(Profile.Languages.Count, species.MaxLanguages);
        
        // Получаем все доступные языки
        var allLanguages = _prototypeManager.EnumeratePrototypes<LanguagePrototype>()
            .Where(x => x.Roundstart)
            .ToList();
            
        foreach (var item in species.UniqueLanguages)
        {
            var proto = _prototypeManager.Index(item);
            if (!allLanguages.Contains(proto))
                allLanguages.Add(proto);
        }

        // Сортируем
        allLanguages.Sort((x, y) => x.LocalizedName[0].CompareTo(y.LocalizedName[0]));
        allLanguages.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        // Формируем список по умолчанию
        List<LanguagePrototype> defaultList = new();
        defaultList.AddRange(allLanguages.Where(x => species.DefaultLanguages.Contains(x) && !species.UniqueLanguages.Contains(x)));
        defaultList.AddRange(allLanguages.Where(x => species.UniqueLanguages.Contains(x)));
        defaultList.Sort((x, y) => x.LocalizedName[0].CompareTo(y.LocalizedName[0]));
        defaultList.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        // Фильтруем по поисковому запросу
        var filteredLanguages = allLanguages
            .Where(l => string.IsNullOrEmpty(_languagesSearchText) ||
                        l.LocalizedName.Contains(_languagesSearchText, StringComparison.OrdinalIgnoreCase) ||
                        l.LocalizedDescription?.ToString().Contains(_languagesSearchText, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // Очищаем список
        LanguagesList.RemoveAllChildren();

        // Сначала добавляем языки из defaultList, которые прошли фильтр
        foreach (var item in defaultList)
        {
            if (filteredLanguages.Contains(item))
                AddLanguageEntry(item, species);
        }

        // Затем остальные, которых нет в defaultList
        foreach (var item in filteredLanguages)
        {
            if (!defaultList.Contains(item))
                AddLanguageEntry(item, species);
        }
    }

    private void UpdateLanguagesProgressBar(int current, int max)
    {
        LanguagesCountLabel.Text = $"{current} / {max}";
        var parent = LanguagesProgressBar.Parent;
        if (parent == null) return;
        
        float percent = max > 0 ? Math.Clamp((float)current / max, 0f, 1f) : 0f;
        if (parent.Width > 0)
            LanguagesProgressBar.SetWidth = (int)((parent.Width - 2) * percent);
        else
            parent.OnResized += () =>
            {
                if (parent.Width > 0)
                    LanguagesProgressBar.SetWidth = (int)((parent.Width - 2) * percent);
            };
    }

    private void AddLanguageEntry(LanguagePrototype proto, SpeciesPrototype species)
    {
        if (Profile == null) return;
        var entry = new LanguageEntry(proto, false)
        {
            Margin = new(7),
            HorizontalExpand = true
        };
        bool isSelected = Profile.Languages.Contains(proto);
        entry.SelectButton.Text = isSelected
            ? Loc.GetString("language-lobby-remove-button")
            : Loc.GetString("language-lobby-add-button");
        entry.SelectButton.Disabled = !isSelected && Profile.Languages.Count >= species.MaxLanguages;
        entry.OnLanguageSelected += SelectLanguage;
        LanguagesList.AddChild(entry);
    }

    public void SelectLanguage(string protoId)
    {
        Profile = (Profile?.Languages.Contains(protoId) ?? false) 
            ? Profile?.WithoutLanguage(protoId) 
            : Profile?.WithLanguage(protoId);
        SetDirty();
        RefreshLanguages();
    }

    public void SetDefaultLanguages()
    {
        if (Profile == null) return;
        var species = _prototypeManager.Index(Profile.Species);
        foreach (var item in Profile.Languages.ToList())
            Profile = Profile?.WithoutLanguage(item);
        foreach (var item in species.DefaultLanguages)
            Profile = Profile?.WithLanguage(item);
        SetDirty();
        RefreshLanguages();
    }

    // Обработчик поиска – вызывается из XAML (подписка в конструкторе)
    private void OnLanguagesSearchTextChanged(LineEdit.LineEditEventArgs args)
    {
        _languagesSearchText = args.Text.Trim();
        RefreshLanguages();
    }
}