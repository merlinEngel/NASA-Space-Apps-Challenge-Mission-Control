namespace MissionGame
{
    public interface ISimulated
    {
        public void SimStep(double t);
        public int SimOrder { get; }
    }
}