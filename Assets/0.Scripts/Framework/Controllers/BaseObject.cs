using UnityEngine;

public class BaseObject : InitBase
{
    public override bool Init()
    {
        if (base.Init() == false)
            return false;



        return true;
    }
}
