using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Shared.Player;

namespace Content.Server.Paper;

public sealed class SignatureSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private const string SignatureStampState = "sign";
    private const string SignatureFont = "/Fonts/VG/goodvibescyr.ttf";

    public override void Initialize()
    {
        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(Entity<PaperComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using is not { } pen || !_tags.HasTag(pen, "Write"))
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TrySignPaper(ent, user, pen);
            },
            Text = Loc.GetString("paper-sign-verb"),
            DoContactInteraction = true,
            Priority = 10
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Tries to add a signature to the paper with the signer's name.
    /// </summary>
    public bool TrySignPaper(Entity<PaperComponent> paper, EntityUid signer, EntityUid pen)
    {
        var comp = paper.Comp;

        var ev = new SignAttemptEvent(paper, signer);
        RaiseLocalEvent(pen, ref ev);
        if (ev.Cancelled)
            return false;

        var signatureName = DetermineEntitySignature(signer);
        if (comp.StampedBy.Any(stamp => stamp.Type == StampType.Signature && stamp.StampedName == signatureName))
        {
            _popup.PopupEntity(
                Loc.GetString("paper-signed-failure", ("target", paper.Owner)),
                signer,
                signer,
                PopupType.SmallCaution);

            return false;
        }

        var signatureColor = SignatureInkColor.Black.ToColor();

        if (TryComp<SignatureComponent>(pen, out var signatureComp))
            signatureColor = signatureComp.Color.ToColor();

        var stampInfo = new StampDisplayInfo
        {
            StampedName = signatureName,
            StampedColor = signatureColor,
            Type = StampType.Signature,
            Font = SignatureFont
        };

        if (!comp.StampedBy.Contains(stampInfo) &&
            _paper.TryStamp(paper, stampInfo, SignatureStampState, signatureColor))
        {
            var signedOtherMessage = Loc.GetString("paper-signed-other", ("user", signer), ("target", paper.Owner));
            _popup.PopupEntity(signedOtherMessage, signer, Filter.PvsExcept(signer, entityManager: EntityManager), true);

            var signedSelfMessage = Loc.GetString("paper-signed-self", ("target", paper.Owner));
            _popup.PopupEntity(signedSelfMessage, signer, signer);

            _audio.PlayPvs(comp.Sound, signer);

            _paper.UpdateUserInterface(paper);

            return true;
        }

        _popup.PopupEntity(
            Loc.GetString("paper-signed-failure", ("target", paper.Owner)),
            signer,
            signer,
            PopupType.SmallCaution);

        return false;
    }

    private string DetermineEntitySignature(EntityUid uid)
    {
        if (_idCard.TryFindIdCard(uid, out var id) && !string.IsNullOrWhiteSpace(id.Comp.FullName))
            return id.Comp.FullName;

        return Name(uid);
    }
}
