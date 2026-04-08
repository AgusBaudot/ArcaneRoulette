namespace world 
{
    public interface IStrategy
    {
        Node.NodeState Process();
        void Reset() 
        {
            //Null
        }
    }

}
