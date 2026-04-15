using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public class RandomSelectorNode : PrioritySelectorNode
    {
        protected  override List<Node> SortChildren() => _children.Shuffle();

        public RandomSelectorNode(string name): base(name) { }
    }

    public static class ListExtensions
    {
        public static List<T> Shuffle<T>(this List<T> list)
        {
            var rng = new System.Random();
            var shuffled = new List<T>(list);

            int n = shuffled.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (shuffled[k], shuffled[n]) = (shuffled[n], shuffled[k]);
            }

            return shuffled;
        }
    }
}

