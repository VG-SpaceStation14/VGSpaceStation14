using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.ADT.UserInterface.Controls;

[Virtual]
public class StaticSpriteView : Control
{
    protected SpriteSystem? SpriteSystem;
    private SharedTransformSystem? _transform;
    protected readonly IEntityManager EntMan;

    [ViewVariables]
    public SpriteComponent? Sprite => Entity?.Comp1;

    [ViewVariables]
    public Entity<SpriteComponent, TransformComponent>? Entity { get; private set; }

    [ViewVariables]
    public NetEntity? NetEnt { get; private set; }

    public bool IsVisible { get; set; } = true;

    public StretchMode Stretch { get; set; } = StretchMode.Fit;

    public enum StretchMode
    {
        None,
        Fit,
        Fill
    }

    public Direction? OverrideDirection { get; set; }

    private Vector2 _scale = Vector2.One;
    private Angle _eyeRotation = Angle.Zero;
    private Angle? _worldRotation = Angle.Zero;
    private Vector2 _spriteSize;

    public Vector2 Offset { get; set; } = Vector2.Zero;
    public bool SpriteOffset { get; set; }

    public Angle EyeRotation
    {
        get => _eyeRotation;
        set
        {
            _eyeRotation = value;
            InvalidateMeasure();
        }
    }

    public Angle? WorldRotation
    {
        get => _worldRotation;
        set
        {
            _worldRotation = value;
            InvalidateMeasure();
        }
    }

    public Vector2 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            InvalidateMeasure();
        }
    }

    public StaticSpriteView()
    {
        IoCManager.Resolve(ref EntMan);
        RectClipContent = true;
    }

    public StaticSpriteView(IEntityManager entMan)
    {
        EntMan = entMan;
        RectClipContent = true;
    }

    public StaticSpriteView(EntityUid? uid, IEntityManager entMan)
    {
        EntMan = entMan;
        RectClipContent = true;
        SetEntity(uid);
    }

    public StaticSpriteView(NetEntity uid, IEntityManager entMan)
    {
        EntMan = entMan;
        RectClipContent = true;
        SetEntity(uid);
    }

    public void SetEntity(NetEntity netEnt)
    {
        if (netEnt == NetEnt)
            return;

        Entity = null;
        NetEnt = netEnt;
    }

    public void SetEntity(EntityUid? uid)
    {
        if (Entity?.Owner == uid)
            return;

        if (!EntMan.TryGetComponent(uid, out SpriteComponent? sprite) ||
            !EntMan.TryGetComponent(uid, out TransformComponent? xform))
        {
            Entity = null;
            NetEnt = null;
            return;
        }

        Entity = new(uid.Value, sprite, xform);
        NetEnt = EntMan.GetNetEntity(uid);
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        UpdateSize();
        var setSize = SetSize;
        if (!float.IsNaN(setSize.X) && !float.IsNaN(setSize.Y))
            return setSize;

        return _spriteSize;
    }

    private void UpdateSize()
    {
        if (!ResolveEntity(out _, out var sprite, out _))
            return;

        var spriteBox = sprite.CalculateRotatedBoundingBox(default, _worldRotation ?? Angle.Zero, _eyeRotation)
            .CalcBoundingBox();

        if (!SpriteOffset)
            spriteBox = spriteBox.Translated(-spriteBox.Center);

        var scale = _scale * EyeManager.PixelsPerMeter;
        var bl = spriteBox.BottomLeft * scale;
        var tr = spriteBox.TopRight * scale;

        tr = Vector2.Max(tr, Vector2.Zero);
        bl = Vector2.Min(bl, Vector2.Zero);
        tr = Vector2.Max(tr, -bl);
        bl = Vector2.Min(bl, -tr);
        var box = new Box2(bl, tr);

        DebugTools.Assert(box.Contains(Vector2.Zero));
        DebugTools.Assert(box.TopLeft.EqualsApprox(-box.BottomRight));

        if (_worldRotation != null && _eyeRotation == Angle.Zero)
        {
            _spriteSize = box.Size;
            return;
        }

        var size = box.Size;
        var longestSide = MathF.Max(size.X, size.Y);
        var longestRotatedSide = Math.Max(longestSide, (size.X + size.Y) / MathF.Sqrt(2));
        _spriteSize = new Vector2(longestRotatedSide, longestRotatedSide);
    }

    protected override void Draw(IRenderHandle renderHandle)
    {
        if (!ResolveEntity(out var uid, out var sprite, out var xform))
            return;

        SpriteSystem ??= EntMan.System<SpriteSystem>();
        _transform ??= EntMan.System<TransformSystem>();
        SpriteSystem.ForceUpdate(uid);

        var stretchVec = Stretch switch
        {
            StretchMode.Fit => Vector2.Min(Size / _spriteSize, Vector2.One),
            StretchMode.Fill => Size / _spriteSize,
            _ => Vector2.One,
        };
        var stretch = MathF.Min(stretchVec.X, stretchVec.Y);

        var offset = SpriteOffset
            ? Vector2.Zero
            : -(-_eyeRotation).RotateVec(sprite.Offset * _scale) * new Vector2(1, -1) * EyeManager.PixelsPerMeter;

        var position = PixelSize / 2 + offset * stretch * UIScale + Offset * UIScale;
        var scale = Scale * UIScale * stretch;

        var world = renderHandle.DrawingHandleWorld;
        var oldModulate = world.Modulate;
        world.Modulate *= Modulate * ActualModulateSelf;

        renderHandle.DrawEntity(uid, position, scale, _worldRotation, _eyeRotation, OverrideDirection, sprite, xform, _transform);
        world.Modulate = oldModulate;
    }

    private bool ResolveEntity(
        out EntityUid uid,
        [NotNullWhen(true)] out SpriteComponent? sprite,
        [NotNullWhen(true)] out TransformComponent? xform)
    {
        if (NetEnt != null && Entity == null && EntMan.TryGetEntity(NetEnt, out var ent))
            SetEntity(ent);

        if (Entity != null)
        {
            (uid, sprite, xform) = Entity.Value;
            return !EntMan.Deleted(uid);
        }

        sprite = null;
        xform = null;
        uid = default;
        return false;
    }
}
