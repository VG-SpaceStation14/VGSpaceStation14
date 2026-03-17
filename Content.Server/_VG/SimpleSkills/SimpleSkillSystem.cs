using Content.Server.Popups;
using Content.Shared._VG.SimpleSkills;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;

namespace Content.Server._VG.SimpleSkills;

public sealed class SimpleSkillSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Проверка навыков ДО открытия интерфейса
        SubscribeLocalEvent<SimpleSkillRequiredComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        
        // Проверка навыков при активации (на всякий случай)
        SubscribeLocalEvent<SimpleSkillRequiredComponent, ActivateInWorldEvent>(OnActivate);
        
        // Изучение навыков через предметы
        SubscribeLocalEvent<SimpleSkillBookComponent, UseInHandEvent>(OnUseSkillBook);
        SubscribeLocalEvent<SimpleSkillBookComponent, ActivateInWorldEvent>(OnActivateBook);
        
        // Автоматическое применение группы при инициализации компонента
        SubscribeLocalEvent<SimpleSkillComponent, ComponentInit>(OnComponentInit);
    }

    /// <summary>
    ///     Проверка навыков при попытке взаимодействия (срабатывает ДО открытия интерфейса)
    /// </summary>
    private void OnBeforeInteract(EntityUid uid, SimpleSkillRequiredComponent component, BeforeRangedInteractEvent args)
    {
        if (args.Handled) 
            return;

        if (!HasSkill(args.User, component.RequiredSkill))
        {
            _popup.PopupEntity($"Вам нужен навык: {GetSkillName(component.RequiredSkill)}", args.User, args.User);
            args.Handled = true;  // Блокируем всё взаимодействие
        }
    }

    /// <summary>
    ///     Проверка навыков при активации объекта (ПКМ -> Активировать)
    /// </summary>
    private void OnActivate(EntityUid uid, SimpleSkillRequiredComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled) 
            return;

        if (!HasSkill(args.User, component.RequiredSkill))
        {
            _popup.PopupEntity($"Вам нужен навык: {GetSkillName(component.RequiredSkill)}", args.User, args.User);
            args.Handled = true;
        }
    }

    /// <summary>
    ///     Использование книги навыков через "Use" (ПКМ -> Использовать)
    /// </summary>
    private void OnUseSkillBook(EntityUid uid, SimpleSkillBookComponent component, UseInHandEvent args)
    {
        if (args.Handled) 
            return;

        TryLearnFromBook(uid, component, args.User);
        args.Handled = true;
    }

    /// <summary>
    ///     Использование книги навыков через активацию
    /// </summary>
    private void OnActivateBook(EntityUid uid, SimpleSkillBookComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled) 
            return;

        TryLearnFromBook(uid, component, args.User);
        args.Handled = true;
    }

    /// <summary>
    ///     Попытка изучить навык из книги
    /// </summary>
    private void TryLearnFromBook(EntityUid book, SimpleSkillBookComponent component, EntityUid user)
    {
        // Проверяем, есть ли уже навык
        if (HasSkill(user, component.TeachesSkill))
        {
            _popup.PopupEntity("Вы уже знаете этот навык!", user, user);
            return;
        }

        // Добавляем навык
        AddSkill(user, component.TeachesSkill);
        
        // Звук и сообщение
        _popup.PopupEntity($"Вы изучили {GetSkillName(component.TeachesSkill)}!", user, user);
        
        if (component.Sound != null)
            _audio.PlayPvs(component.Sound, book);
        
        // Удаляем книгу
        QueueDel(book);
    }

    /// <summary>
    ///     Проверка наличия навыка
    /// </summary>
    public bool HasSkill(EntityUid uid, string skillId)
    {
        if (!TryComp<SimpleSkillComponent>(uid, out var skills))
            return false;

        return skills.Skills.TryGetValue(skillId, out var known) && known;
    }

    /// <summary>
    ///     Добавление навыка
    /// </summary>
    public void AddSkill(EntityUid uid, string skillId)
    {
        var skills = EnsureComp<SimpleSkillComponent>(uid);
        skills.Skills[skillId] = true;

        Dirty(uid, skills);
        
        RaiseNetworkEvent(new SkillsChangedEvent(GetNetEntity(uid)));
        
        Logger.InfoS("simple.skills", $"Добавлен навык {skillId} для {ToPrettyString(uid)}");
    }

    /// <summary>
    ///     Получение названия навыка из прототипа
    /// </summary>
    private string GetSkillName(string skillId)
    {
        return _prototype.TryIndex<SimpleSkillPrototype>(skillId, out var proto) 
            ? proto.Name 
            : skillId;
    }

    /// <summary>
    ///     Применить группу навыков к игроку
    /// </summary>
    public void ApplySkillGroup(EntityUid uid, string groupId)
    {
        if (!_prototype.TryIndex<SimpleSkillGroupPrototype>(groupId, out var group))
        {
            Logger.ErrorS("simple.skills", $"Группа навыков {groupId} не найдена");
            return;
        }

        var skills = EnsureComp<SimpleSkillComponent>(uid);
    
        // Если группа пустая - просто создаём компонент без навыков
        if (group.Skills == null || group.Skills.Count == 0)
        {
            Logger.InfoS("simple.skills", $"Применена пустая группа {groupId} для {ToPrettyString(uid)}. Навыков не добавлено.");
            return;
        }

        // Добавляем навыки из группы
        foreach (var skillId in group.Skills)
        {
            skills.Skills[skillId] = true;
            Logger.InfoS("simple.skills", $"  Добавлен навык {skillId} = true");
        }

        Dirty(uid, skills);

        RaiseNetworkEvent(new SkillsChangedEvent(GetNetEntity(uid)));
        
        Logger.InfoS("simple.skills", $"Применена группа {groupId} для {ToPrettyString(uid)}. Всего навыков: {skills.Skills.Count}");
    }

    /// <summary>
    ///     Обработчик инициализации компонента для применения группы
    /// </summary>
    private void OnComponentInit(EntityUid uid, SimpleSkillComponent component, ComponentInit args)
    {
        // Если указана группа, применяем её
        if (!string.IsNullOrEmpty(component.SkillGroup))
        {
            // Проверяем, есть ли уже навыки (чтобы не затереть)
            if (component.Skills == null || component.Skills.Count == 0)
            {
                ApplySkillGroup(uid, component.SkillGroup);
            }
            else
            {
                Logger.InfoS("simple.skills", $"Компонент уже содержит навыки, группа {component.SkillGroup} не применяется");
            }
        }
    }
}

/// <summary>
///     Компонент для объектов, требующих навык
/// </summary>
[RegisterComponent]
public sealed partial class SimpleSkillRequiredComponent : Component
{
    [DataField(required: true)]
    public string RequiredSkill = string.Empty;
}

/// <summary>
///     Компонент для книг навыков
/// </summary>
[RegisterComponent]
public sealed partial class SimpleSkillBookComponent : Component
{
    [DataField(required: true)]
    public string TeachesSkill = string.Empty;
    
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_VG/SimplSkill/book1.ogg");
}