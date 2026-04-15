namespace World 
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
