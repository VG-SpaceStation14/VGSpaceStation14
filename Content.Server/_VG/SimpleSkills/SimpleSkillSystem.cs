using Content.Server.Popups;
using Content.Shared._VG.SimpleSkills;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.DoAfter;
using Robust.Shared.Containers;

namespace Content.Server._VG.SimpleSkills;

public sealed class SimpleSkillSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

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
        
        // Обучение через учителя
        SubscribeLocalEvent<SimpleSkillComponent, GetVerbsEvent<Verb>>(OnGetTeachingVerbs);
        SubscribeLocalEvent<SkillTeacherComponent, ComponentShutdown>(OnTeacherShutdown);
        SubscribeLocalEvent<SkillTeacherComponent, SkillTeachDoAfterEvent>(OnTeachDoAfter);
        SubscribeLocalEvent<SimpleSkillBookComponent, SkillLearnDoAfterEvent>(OnLearnDoAfter);
        
        // Добавляем верб для изучения книги
        SubscribeLocalEvent<SimpleSkillBookComponent, GetVerbsEvent<Verb>>(OnGetSkillBookVerbs);
    }

    /// <summary>
    ///     Проверка наличия книги навыка в руках
    /// </summary>
    private bool HasSkillBookInHands(EntityUid user, string skillId)
    {
        foreach (var hand in _hands.EnumerateHeld(user))
        {
            if (TryComp<SimpleSkillBookComponent>(hand, out var book) && book.TeachesSkill == skillId)
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Получение книги навыка из рук
    /// </summary>
    private EntityUid? GetSkillBookInHands(EntityUid user, string skillId)
    {
        foreach (var hand in _hands.EnumerateHeld(user))
        {
            if (TryComp<SimpleSkillBookComponent>(hand, out var book) && book.TeachesSkill == skillId)
                return hand;
        }
        return null;
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
        if (HasSkill(user, component.TeachesSkill))
        {
            _popup.PopupEntity(Loc.GetString("skill-already-known"), user, user);
            return;
        }

        var learnTime = 120f;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(learnTime), new SkillLearnDoAfterEvent(component.TeachesSkill), book, target: user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = true,
            CancelDuplicate = true,
            BlockDuplicate = true,
            NeedHand = true
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupEntity(Loc.GetString("skill-learn-interrupted"), user, user);
            return;
        }

        _popup.PopupEntity(Loc.GetString("skill-learn-start", ("time", learnTime)), user, user);

        if (component.SoundStart != null)
            _audio.PlayPvs(component.SoundStart, book);
        else if (component.Sound != null) 
            _audio.PlayPvs(component.Sound, book);
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
    public string GetSkillName(string skillId)
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

    /// <summary>
    ///     Рекурсивный поиск предмета в контейнерах (инвентарь, рюкзак и т.д.)
    /// </summary>
    private bool TryFindItemInInventory(EntityUid entity, string skillId, out EntityUid foundItem)
    {
        if (TryComp<SimpleSkillBookComponent>(entity, out var book) && book.TeachesSkill == skillId)
        {
            foundItem = entity;
            return true;
        }

        if (TryComp<ContainerManagerComponent>(entity, out var containers))
        {
            foreach (var container in containers.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    if (TryFindItemInInventory(contained, skillId, out foundItem))
                        return true;
                }
            }
        }

        foundItem = default;
        return false;
    }

    /// <summary>
    ///     Получение доступных действий обучения
    /// </summary>
    private void OnGetTeachingVerbs(Entity<SimpleSkillComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        var user = args.User;
        var target = args.Target;

        if (user == target)
            return;

        if (!TryComp<SimpleSkillComponent>(user, out var userSkills))
            return;

        var bookInHands = false;
        string? availableSkill = null;
        
        foreach (var hand in _hands.EnumerateHeld(user))
        {
            if (TryComp<SimpleSkillBookComponent>(hand, out var book))
            {
                bookInHands = true;
                availableSkill = book.TeachesSkill;
                break;
            }
        }

        if (!bookInHands || availableSkill == null)
            return;

        if (!userSkills.Skills.TryGetValue(availableSkill, out var known) || !known)
            return;

        if (HasSkill(target, availableSkill))
            return;

        var verb = new Verb
        {
            Text = Loc.GetString("skill-teach-verb", ("skill", GetSkillName(availableSkill))),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
            Act = () => StartTeaching(user, target, availableSkill),
            Priority = 1
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Получение вербов для книг навыков
    /// </summary>
    private void OnGetSkillBookVerbs(EntityUid uid, SimpleSkillBookComponent component, GetVerbsEvent<Verb> args)
    {
        var user = args.User;

        if (!args.CanAccess || !args.CanInteract)
            return;

        if (HasSkill(user, component.TeachesSkill))
        {
            return;
        }

        var verb = new Verb
        {
            Text = Loc.GetString("skill-learn-verb", ("skill", GetSkillName(component.TeachesSkill))),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
            Act = () => TryLearnFromBook(uid, component, user),
            Priority = 2 
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Начать обучение навыку
    /// </summary>
    private void StartTeaching(EntityUid teacher, EntityUid student, string skillId)
    {
        if (!Transform(teacher).Coordinates.TryDistance(EntityManager, Transform(student).Coordinates, out var distance) || distance > 3f)
        {
            _popup.PopupEntity(Loc.GetString("skill-teach-too-far"), teacher, teacher);
            return;
        }

        if (!HasSkill(teacher, skillId))
        {
            _popup.PopupEntity(Loc.GetString("skill-teach-not-known"), teacher, teacher);
            return;
        }

        if (HasSkill(student, skillId))
        {
            _popup.PopupEntity(Loc.GetString("skill-teach-already-known"), teacher, teacher);
            return;
        }

        var book = GetSkillBookInHands(teacher, skillId);
        if (book == null)
        {
            _popup.PopupEntity(Loc.GetString("skill-teach-no-book"), teacher, teacher);
            return;
        }

        var teacherComp = EnsureComp<SkillTeacherComponent>(teacher);
        teacherComp.SkillId = skillId;
        teacherComp.Student = student;
        teacherComp.Book = book.Value;

        if (TryComp<SimpleSkillBookComponent>(book.Value, out var bookComp))
        {
            if (bookComp.SoundStart != null)
                _audio.PlayPvs(bookComp.SoundStart, book.Value);
            else if (bookComp.Sound != null)
                _audio.PlayPvs(bookComp.Sound, book.Value);
        }

        var teachDoAfter = new DoAfterArgs(EntityManager, teacher, TimeSpan.FromSeconds(30), new SkillTeachDoAfterEvent(skillId, GetNetEntity(student)), book.Value, target: student)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = true,
            CancelDuplicate = true,
            DistanceThreshold = 3f,
            NeedHand = true,
            EventTarget = teacher 
        };

        if (!_doAfter.TryStartDoAfter(teachDoAfter, out var doAfterId))
        {
            RemComp<SkillTeacherComponent>(teacher);
            return;
        }

        teacherComp.DoAfterId = doAfterId?.Index;

        _popup.PopupEntity(Loc.GetString("skill-teach-start-teacher", ("student", student), ("skill", GetSkillName(skillId))), teacher, teacher);
        _popup.PopupEntity(Loc.GetString("skill-teach-start-student", ("teacher", teacher), ("skill", GetSkillName(skillId))), student, student);
    }

    /// <summary>
    ///     Завершение обучения
    /// </summary>
    private void OnTeachDoAfter(EntityUid uid, SkillTeacherComponent component, SkillTeachDoAfterEvent args)
    {
        component.DoAfterId = null;

        if (TryComp<SimpleSkillBookComponent>(component.Book, out var bookComp))
        {
            if (bookComp.SoundEnd != null)
                _audio.PlayPvs(bookComp.SoundEnd, component.Book);
            else if (bookComp.Sound != null)
                _audio.PlayPvs(bookComp.Sound, component.Book);
        }

        if (args.Cancelled || args.Handled)
        {
            _popup.PopupEntity(Loc.GetString("skill-teach-cancelled"), uid, uid);
            RemComp<SkillTeacherComponent>(uid);
            return;
        }

        var student = GetEntity(args.Student);

        if (!Exists(student) || TerminatingOrDeleted(student))
        {
            RemComp<SkillTeacherComponent>(uid);
            return;
        }

        AddSkill(student, component.SkillId);

        _popup.PopupEntity(Loc.GetString("skill-teach-success-teacher", ("student", student), ("skill", GetSkillName(component.SkillId))), uid, uid);
        _popup.PopupEntity(Loc.GetString("skill-teach-success-student", ("teacher", uid), ("skill", GetSkillName(component.SkillId))), student, student);

        RemComp<SkillTeacherComponent>(uid);
    }

    /// <summary>
    ///     Обработчик завершения DoAfter книги
    /// </summary>
    private void OnLearnDoAfter(EntityUid uid, SimpleSkillBookComponent component, SkillLearnDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
        {
            if (component.SoundEnd != null)
                _audio.PlayPvs(component.SoundEnd, uid);
            else if (component.Sound != null)
                _audio.PlayPvs(component.Sound, uid);

            _popup.PopupEntity(Loc.GetString("skill-learn-cancelled"), args.User, args.User);
            return;
        }

        var user = args.User;

        if (HasSkill(user, component.TeachesSkill))
        {
            _popup.PopupEntity(Loc.GetString("skill-already-known"), user, user);
            return;
        }

        AddSkill(user, component.TeachesSkill);

        _popup.PopupEntity(Loc.GetString("skill-learn-success", ("skill", GetSkillName(component.TeachesSkill))), user, user);

        if (component.SoundEnd != null)
            _audio.PlayPvs(component.SoundEnd, uid);
        else if (component.Sound != null)
            _audio.PlayPvs(component.Sound, uid);
            
    }

    /// <summary>
    ///     Обработчик отключения учителя
    /// </summary>
    private void OnTeacherShutdown(EntityUid uid, SkillTeacherComponent component, ComponentShutdown args)
    {
        if (component.Student != null && component.DoAfterId != null)
        {
            // Конвертируем uint в ushort для конструктора DoAfterId
            var doAfterId = new DoAfterId(uid, (ushort)component.DoAfterId.Value);
            _doAfter.Cancel(doAfterId);
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
    public SoundSpecifier? Sound; // звук начала изучения (для обратной совместимости)

    [DataField]
    public SoundSpecifier? SoundStart; // звук начала изучения (приоритетнее)

    [DataField]
    public SoundSpecifier? SoundEnd; // звук завершения/отмены

    /// <summary>
    ///     Время изучения из книги (в секундах) - не используется, оставлено для совместимости
    /// </summary>
    [DataField]
    public float LearnTime = 360f;
}