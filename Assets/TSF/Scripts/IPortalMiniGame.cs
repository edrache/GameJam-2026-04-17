namespace TSF
{
    public enum PortalSide
    {
        Front,
        Back
    }

    public interface IPortalMiniGame
    {
        void OnHandEnter(ArmReachController reach, PortalSide side);
        void OnHandExit();
    }
}
