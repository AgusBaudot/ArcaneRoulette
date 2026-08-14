using System;

namespace Foundation
{
    public interface IDoTReadable
    {
        event Action OnDoTApplied;
        event Action OnDoTRemoved;
        
        bool HasActiveDoTs { get; }
    }
}