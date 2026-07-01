using System;
using System.Collections.Generic;
using System.Linq;

namespace World
{
    public class RoomWeightManager
    {
        private List<WeightedRoom> _roomPool;
        private readonly int _penaltyWeight;
        private readonly int _bonusWeight;
        private Random _rng;

        public RoomWeightManager(int startingWeight = 100, int penalty = 20, int bonus = 40)
        {
            _penaltyWeight = penalty;
            _bonusWeight = bonus;
            _rng = new Random();
            
            //Initialize the pool
            _roomPool = new()
            {
                new WeightedRoom { Type = RoomType.Shop, CurrentWeight = startingWeight },
                new WeightedRoom { Type = RoomType.Resting, CurrentWeight = startingWeight },
                // new WeightedRoom { Type = RoomType.Artifact, CurrentWeight = startingWeight },
            };
        }

        /// <summary>
        /// Rolls the dice, selects the next room, and updates the weights for the future.
        /// </summary>
        public RoomType GetNextRoom()
        {
            int totalWeight = _roomPool.Sum(r => r.CurrentWeight);
            
            int roll = _rng.Next(0, totalWeight);

            RoomType selectedRoom = _roomPool[0].Type; //Safe fallback
            int cumulativeWeight = 0;

            foreach (var room in _roomPool)
            {
                cumulativeWeight += room.CurrentWeight;
                if (roll < cumulativeWeight)
                {
                    selectedRoom = room.Type;
                    break;
                }
            }

            AdjustWeights(selectedRoom);
            
            return selectedRoom;
        }

        /// <summary>
        /// Applies the designer's rule: winner drops to base penalty, losers gain bonus.
        /// </summary>
        /// <param name="winner"></param>
        private void AdjustWeights(RoomType winner)
        {
            for (int i = 0; i < _roomPool.Count; i++)
            {
                WeightedRoom room = _roomPool[i]; 

                if (room.Type == winner)
                {
                    room.CurrentWeight = _penaltyWeight; 
                }
                else
                {
                    room.CurrentWeight += _bonusWeight;
                }

                _roomPool[i] = room; 
            }
        }
    }

    [System.Serializable]
    public struct WeightedRoom
    {
        public RoomType Type;
        public int CurrentWeight;
    }
}