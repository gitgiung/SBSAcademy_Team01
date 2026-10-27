using UnityEngine;

public class Monster : Creature
{
    public override bool Init()
    {
        if (base.Init() == false)
            return false;



        return true;
    }
}
