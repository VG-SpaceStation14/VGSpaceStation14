using System.Numerics;
using Content.Client.Animations;
using Content.Client.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Map;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem
{
    private const string FadeAnimationKey = "melee-fade";
    private const string SlashAnimationKey = "melee-slash";
    private const string ThrustAnimationKey = "melee-thrust";

    public override void DoLunge(EntityUid user, EntityUid weapon, Angle angle, Vector2 localPos, string? animation, bool predicted = true)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var lunge = GetLungeAnimation(localPos);
        _animation.Stop(user, MeleeLungeKey);
        _animation.Play(user, lunge, MeleeLungeKey);

        if (localPos == Vector2.Zero || animation == null)
            return;

        if (!_xformQuery.TryGetComponent(user, out var userXform) || userXform.MapID == MapId.Nullspace)
            return;

        var animationUid = Spawn(animation, userXform.Coordinates);

        if (!TryComp<SpriteComponent>(animationUid, out var sprite) ||
            !TryComp<WeaponArcVisualsComponent>(animationUid, out var arcComponent))
            return;

        var length = 1f;
        var offset = 1f;
        var spriteRotation = Angle.Zero;

        if (arcComponent.Animation != WeaponArcAnimation.None && TryComp(weapon, out MeleeWeaponComponent? meleeWeapon))
        {
            if (user != weapon && !meleeWeapon.CustomWideAnim && TryComp(weapon, out SpriteComponent? weaponSprite))
                _sprite.CopySprite((weapon, weaponSprite), (animationUid, sprite));

            spriteRotation = meleeWeapon.WideAnimationRotation;

            if (meleeWeapon.SwingLeft)
                angle *= -1;

            // Длина анимации зависит от скорости атаки
            length = (1 / meleeWeapon.AttackRate) * 0.6f;
            offset = meleeWeapon.AnimationOffset;
        }

        _sprite.SetRotation((animationUid, sprite), localPos.ToWorldAngle());

        var xform = _xformQuery.GetComponent(animationUid);
        TrackUserComponent track;

        // Динамическое смещение в зависимости от расстояния до цели
        var distance = Math.Clamp(localPos.Length() / 1.5f, 0.3f, 1.2f);
        var slashOffset = Math.Clamp(localPos.Length() / 1.2f, 0.5f, 1.5f);

        switch (arcComponent.Animation)
        {
            case WeaponArcAnimation.Slash:
                track = EnsureComp<TrackUserComponent>(animationUid);
                track.User = user;
                _animation.Play(animationUid, GetSlashAnimation((animationUid, sprite), angle, spriteRotation, length, slashOffset), SlashAnimationKey);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, length * 0.5f, length + 0.15f), FadeAnimationKey);
                break;
            case WeaponArcAnimation.Thrust:
                track = EnsureComp<TrackUserComponent>(animationUid);
                track.User = user;
                _animation.Play(animationUid, GetThrustAnimation((animationUid, sprite), distance, spriteRotation, length), ThrustAnimationKey);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, length * 0.5f, length + 0.15f), FadeAnimationKey);
                break;
            case WeaponArcAnimation.None:
                var (mapPos, mapRot) = TransformSystem.GetWorldPositionRotation(userXform);
                var worldPos = mapPos + (mapRot - userXform.LocalRotation).RotateVec(localPos);
                var newLocalPos = Vector2.Transform(worldPos, TransformSystem.GetInvWorldMatrix(xform.ParentUid));
                TransformSystem.SetLocalPositionNoLerp(animationUid, newLocalPos, xform);
                if (arcComponent.Fadeout)
                    _animation.Play(animationUid, GetFadeAnimation(sprite, 0f, 0.15f), FadeAnimationKey);
                break;
        }
    }

    private Animation GetSlashAnimation(Entity<SpriteComponent> sprite, Angle arc, Angle spriteRotation, float length, float offset)
    {
        var startRotation = sprite.Comp.Rotation + (arc * 0.5f);
        var endRotation = sprite.Comp.Rotation - (arc * 0.5f);

        var startRotationOffset = startRotation.RotateVec(new Vector2(0f, -offset * 0.9f));
        var minRotationOffset = sprite.Comp.Rotation.RotateVec(new Vector2(0f, -offset * 1.1f));
        var endRotationOffset = endRotation.RotateVec(new Vector2(0f, -offset * 0.9f));

        startRotation += spriteRotation;
        endRotation += spriteRotation;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length + 0.05f),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation, endRotation, 0.0f), length * 0.0f),
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation, endRotation, 0.5f), length * 0.10f, Easings.OutCubic),
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation, endRotation, 1.0f), length * 0.15f),
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation, endRotation, 0.9f), length * 0.20f),
                        new AnimationTrackProperty.KeyFrame(Angle.Lerp(startRotation, endRotation, 0.80f), length * 0.60f, Easings.OutQuart)
                    },
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startRotationOffset, endRotationOffset, 0.0f), length * 0.0f),
                        new AnimationTrackProperty.KeyFrame(minRotationOffset, length * 0.10f, Easings.OutCubic),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startRotationOffset, endRotationOffset, 1.0f), length * 0.15f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(startRotationOffset, endRotationOffset, 0.80f), length * 0.60f, Easings.OutQuart)
                    }
                },
            }
        };
    }

    private Animation GetThrustAnimation(Entity<SpriteComponent> sprite, float offset, Angle spriteRotation, float length)
    {
        var startOffset = sprite.Comp.Rotation.RotateVec(new Vector2(0f, 0f));
        var midOffset = sprite.Comp.Rotation.RotateVec(new Vector2(0f, -offset * 0.8f));
        var endOffset = sprite.Comp.Rotation.RotateVec(new Vector2(0f, -offset * 1.3f));
        var backOffset = sprite.Comp.Rotation.RotateVec(new Vector2(0f, -offset * 0.5f));
        
        _sprite.SetRotation(sprite.AsNullable(), sprite.Comp.Rotation + spriteRotation);

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(midOffset, length * 0.1f, Easings.OutCubic),
                        new AnimationTrackProperty.KeyFrame(endOffset, length * 0.25f, Easings.OutQuad),
                        new AnimationTrackProperty.KeyFrame(backOffset, length * 0.45f, Easings.InQuad),
                        new AnimationTrackProperty.KeyFrame(startOffset, length * 0.7f, Easings.OutExpo),
                    }
                },
            }
        };
    }

    private Animation GetFadeAnimation(SpriteComponent sprite, float start, float end)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(end),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, start),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), end, Easings.OutQuad)
                    }
                }
            }
        };
    }

    private Animation GetLungeAnimation(Vector2 direction)
    {
        const float length = 0.1f;
        return new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(direction.Normalized() * 0.15f, length * 0.4f, Easings.OutQuad),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, length * 0.6f, Easings.InQuad),
                    },
                },
            },
        };
    }

    private void UpdateEffects()
    {
        var query = EntityQueryEnumerator<TrackUserComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var arcComponent, out var xform))
        {
            if (arcComponent.User == null || Deleted(arcComponent.User))
                continue;

            var targetPos = TransformSystem.GetWorldPosition(arcComponent.User.Value);
            if (arcComponent.Offset != Vector2.Zero)
            {
                var entRotation = TransformSystem.GetWorldRotation(xform);
                targetPos += entRotation.RotateVec(arcComponent.Offset);
            }
            TransformSystem.SetWorldPosition(uid, targetPos);
        }
    }
}