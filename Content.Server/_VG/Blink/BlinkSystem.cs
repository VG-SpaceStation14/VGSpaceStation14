using System.Linq;
using Content.Shared._VG.Blink;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Maths;

namespace Content.Server._VG.Blink;

public sealed class BlinkSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float DefaultMaxHp = 100f;

    public override void Initialize()
    {
        SubscribeLocalEvent<BlinkComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BlinkComponent, DamageChangedEvent>(OnDamage);
    }


    private void OnMobStateChanged(EntityUid uid, BlinkComponent blink, MobStateChangedEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        switch (args.NewMobState)
        {
            case MobState.Alive:
                SetEyesClosed(uid, blink, humanoid, false);
                ScheduleNextBlink(uid, blink, 0f);
                break;

            case MobState.Critical:
                SetEyesClosed(uid, blink, humanoid, true);
                blink.NextBlinkTime = TimeSpan.MaxValue;
                blink.BlinkEndTime = TimeSpan.MaxValue;
                Dirty(uid, blink);
                break;

            case MobState.Dead:
                SetEyesClosed(uid, blink, humanoid, false);
                blink.NextBlinkTime = TimeSpan.MaxValue;
                blink.BlinkEndTime = TimeSpan.MaxValue;
                Dirty(uid, blink);
                break;
        }
    }


    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BlinkComponent, HumanoidAppearanceComponent, MobStateComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var blink, out var humanoid, out var mobState))
        {
            if (mobState.CurrentState != MobState.Alive)
                continue;

            float intensity = GetBlinkIntensity(uid, blink);

            if (blink.EyesClosed)
            {
                if (curTime >= blink.BlinkEndTime)
                {
                    SetEyesClosed(uid, blink, humanoid, false);
                    ScheduleNextBlink(uid, blink, intensity);
                }
                continue;
            }

            if (curTime >= blink.NextBlinkTime)
            {
                if (_random.Prob(blink.SkipBlinkChance * (1f - intensity)))
                {
                    ScheduleNextBlink(uid, blink, intensity);
                    continue;
                }

                StartBlink(uid, blink, humanoid, intensity);
            }
        }
    }

    private void OnDamage(EntityUid uid, BlinkComponent blink, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (!TryComp<MobStateComponent>(uid, out var mobState) || mobState.CurrentState != MobState.Alive)
            return;

        float damage = (float) args.DamageDelta.DamageDict.Values.Sum();

        if (damage < blink.ReflexBlinkDamageThreshold)
            return;

        if (!_random.Prob(blink.ReflexBlinkChance))
            return;

        TriggerReflexBlink(uid, blink, damage);
    }

    private void TriggerReflexBlink(EntityUid uid, BlinkComponent blink, float damage)
    {
        if (blink.EyesClosed)
        {
            blink.BlinkEndTime += TimeSpan.FromSeconds(blink.ReflexBlinkDuration);
            Dirty(uid, blink);
            return;
        }

        SetEyesClosed(uid, blink, null, true);

        float mult = Math.Clamp(damage / (blink.ReflexBlinkDamageThreshold * 2f), 1f, 2f);

        blink.BlinkEndTime = _timing.CurTime + TimeSpan.FromSeconds(blink.ReflexBlinkDuration * mult);
        blink.NextBlinkTime = blink.BlinkEndTime;

        Dirty(uid, blink);
    }

    private void StartBlink(EntityUid uid, BlinkComponent blink, HumanoidAppearanceComponent humanoid, float intensity)
    {
        if (!blink.EyesClosed)
            blink.OriginalEyeColor = humanoid.EyeColor;

        float duration = MathHelper.Lerp(
            blink.NormalBlinkDuration,
            blink.InjuredBlinkDuration,
            intensity);

        SetEyesClosed(uid, blink, humanoid, true);

        blink.BlinkEndTime = _timing.CurTime + TimeSpan.FromSeconds(duration);
        Dirty(uid, blink);
    }

    private void ScheduleNextBlink(EntityUid uid, BlinkComponent blink, float intensity)
    {
        float min = MathHelper.Lerp(blink.NormalMinBlinkInterval, blink.InjuredMinBlinkInterval, intensity);
        float max = MathHelper.Lerp(blink.NormalMaxBlinkInterval, blink.InjuredMaxBlinkInterval, intensity);

        float interval = _random.NextFloat(min, max);

        blink.NextBlinkTime = _timing.CurTime + TimeSpan.FromSeconds(interval);
        Dirty(uid, blink);
    }

    private void SetEyesClosed(EntityUid uid, BlinkComponent blink, HumanoidAppearanceComponent? humanoid, bool closed)
    {
        if (blink.EyesClosed == closed)
            return;

        blink.EyesClosed = closed;

        if (humanoid != null)
        {
            if (closed)
            {
                blink.OriginalEyeColor ??= humanoid.EyeColor;
                humanoid.EyeColor = humanoid.SkinColor;
            }
            else if (blink.OriginalEyeColor.HasValue)
            {
                humanoid.EyeColor = blink.OriginalEyeColor.Value;
            }

            Dirty(uid, humanoid);
        }

        Dirty(uid, blink);
    }

    private float GetBlinkIntensity(EntityUid uid, BlinkComponent blink)
    {
        float damage = GetDamageFraction(uid);
        float stamina = GetStaminaFraction(uid);

        return Math.Clamp(
            damage * blink.DamageWeight +
            stamina * blink.StaminaWeight,
            0f, 1f);
    }

    private float GetDamageFraction(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return 0f;

        float max = FixedPoint2.New(DefaultMaxHp);

        if (TryComp<MobThresholdsComponent>(uid, out var thresholds) &&
            thresholds.Thresholds.Count > 0)
        {
            max = thresholds.Thresholds.Keys.Max();
        }

        return Math.Clamp((float)(damageable.TotalDamage / max), 0f, 1f);
    }

    private float GetStaminaFraction(EntityUid uid)
    {
        if (!TryComp<StaminaComponent>(uid, out var stamina) || stamina.CritThreshold <= 0f)
            return 0f;

        return Math.Clamp(stamina.StaminaDamage / stamina.CritThreshold, 0f, 1f);
    }
}