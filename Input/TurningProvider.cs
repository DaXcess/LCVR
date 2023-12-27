namespace LCVR.Input
{
    internal interface TurningProvider
    {
        void Update();

        float GetRotationOffset();
    }

    public class NullTurningProvider : TurningProvider
    {
        public float GetRotationOffset()
        {
            return 0;
        }

        public void Update()
        {
        }
    }
}
