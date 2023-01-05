using Common;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ListSerializer
{
    internal static class NodesExtensions
    {
        public static void QuickSort(List<ListNode> nodes)
        {
            if (nodes.Count < 2)
            {
                return;
            }

            var stack = new Stack<(int left, int right)>();
            var pIndx = Partition(nodes, 0, nodes.Count - 1);
            stack.Push((pIndx + 1, nodes.Count - 1));
            stack.Push((0, pIndx - 1));

            while (stack.TryPop(out var pair))
            {
                if (pair.left >= pair.right)
                {
                    continue;
                }

                pIndx = Partition(nodes, pair.left, pair.right);
                stack.Push((pIndx + 1, pair.right));
                stack.Push((pair.left, pIndx - 1));
            }
        }

        private static int Partition(List<ListNode> nodes, int leftIndex, int rightIndex)
        {
            var pivot = nodes[rightIndex];
            var pivotIndex = leftIndex - 1;
            for (int j = leftIndex; j < rightIndex; j++)
            {
                if (string.Compare(nodes[j].Data, pivot.Data) <= 0)
                {
                    Swap(nodes, ++pivotIndex, j);
                }
            }

            Swap(nodes, ++pivotIndex, rightIndex);
            return pivotIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(List<ListNode> nodes, int i, int j)
        {
            var temp = nodes[i];
            nodes[i] = nodes[j];
            nodes[j] = temp;
        }
    }
}