namespace LCVR.Input;

internal interface TurningProvider
{
    void Update();

    void SetOffset(float offset);

    float GetRotationOffset();
}

public class NullTurningProvider : TurningProvider
{
    public float GetRotationOffset()
    {
        return 0;
    }

    public void SetOffset(float _)
    { 
    }

    public void Update()
    {
    }
}
