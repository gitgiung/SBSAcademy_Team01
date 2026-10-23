using UnityEngine;

public class Player1 : Creature
{
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        return true;
    }

    protected override Vector2 GetMoveDirection()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
}
