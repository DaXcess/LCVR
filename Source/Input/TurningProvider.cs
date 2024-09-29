namespace LCVR.Input;

public interface TurningProvider
{
    float Update();

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

    public float Update()
    {
        return 0;
    }
}
