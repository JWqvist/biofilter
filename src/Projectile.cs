using Godot;

namespace BioFilter;

/// <summary>
/// UV Steriliser projectile.
/// Moves toward a target particle and deals damage on contact.
/// Visual: small 4x4 white square.
/// </summary>
public partial class Projectile : Node2D
{
    private Particle? _target;
    private float _damage;
    private const float Speed = 150f; // pixels per second
    private const float HitRadius = 4f; // pixels

    public void Initialize(Particle target, float damage = GameConfig.UVSteriliserDamage)
    {
        _target = target;
        _damage = damage;
    }

    public override void _Process(double delta)
    {
        // Destroy if target is gone
        if (_target == null || !GodotObject.IsInstanceValid(_target) || _target.Health <= 0f)
        {
            QueueFree();
            return;
        }

        Vector2 dir = (_target.GlobalPosition - GlobalPosition).Normalized();
        GlobalPosition += dir * Speed * (float)delta;

        // Hit check
        if (GlobalPosition.DistanceTo(_target.GlobalPosition) <= HitRadius)
        {
            _target.TakeDamage(_damage);
            QueueFree();
        }
    }

    public override void _Draw()
    {
        // 4x4 white square centered
        DrawRect(new Rect2(-2f, -2f, 4f, 4f), Colors.White);
    }
}
