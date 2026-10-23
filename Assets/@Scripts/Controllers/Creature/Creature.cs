using UnityEngine;

public class Creature : BaseObject
{
    protected float Speed = 5f;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;



        return true;
    }

    private void Update()
    {
        Move(GetMoveDirection());
    }

    protected virtual Vector2 GetMoveDirection() => Vector2.zero;

    private void Move(Vector2 dir)
    {
        transform.position += (Vector3)dir * Speed * Time.deltaTime;
        Flip(dir);
    }

    private void Flip(Vector2 dir)
    {
        if (dir.x == 0)
            return;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dir.x < 0 ? 1f : -1f);
        transform.localScale = scale;
    }
}
