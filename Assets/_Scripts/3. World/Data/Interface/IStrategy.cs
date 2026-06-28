namespace World 
{
    public interface IStrategy
    {
        NodeState Process();
        void Reset() 
        {
            //Null
        }
    }
}
