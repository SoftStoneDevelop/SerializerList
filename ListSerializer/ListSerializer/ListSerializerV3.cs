using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ListSerializer
{
    public class ListSerializerV3 : IListSerializer
    {
        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(() =>
            {
                var globalLinkId = 0;
                var buffer = new byte[500];

                //package [linkBytes 4byte][length 4byte][data]
                //All stream packages...link datas 'idLinkNodeHead'...'idLinkNodeTail'
                s.Position = 0;
                s.Write(buffer, 0, sizeof(int));//reserved for count all unique nodes

                ListNode current = null;
                do
                {
                    if (current == null)
                    {
                        current = head;
                    }
                    else
                    {
                        current = current.Next;
                    }

                    //write id unique Node
                    Unsafe.As<byte, int>(ref buffer[0]) = globalLinkId++;
                    s.Write(buffer, 0, sizeof(int));

                    if (current.Data == null)
                    {
                        WriteNullRefferenceValue(in s);
                    }
                    else
                    {
                        var size = current.Data.Length * sizeof(char);
                        Unsafe.As<byte, int>(ref buffer[0]) = size;
                        s.Write(buffer, 0, sizeof(int));

                        int partDataSize = 0;
                        int offsetDestination = 0;
                        while (size > 0)
                        {
                            partDataSize = size > buffer.Length ? buffer.Length : size;
                            unsafe
                            {
                                fixed (byte* pDest = &buffer[0])
                                fixed (char* pSource = current.Data)
                                {
                                    Buffer.MemoryCopy(pSource + offsetDestination, pDest, buffer.Length, partDataSize);
                                }
                            }
                            s.Write(buffer, 0, partDataSize);

                            offsetDestination += partDataSize;
                            size -= partDataSize;
                        }
                    }
                }
                while (current.Next != null);

                //write comma between packages and links
                WriteNullRefferenceValue(in s);

                var uniqueNodesCount = globalLinkId;
                globalLinkId = -1;
                //write uniqueNodesCount
                long tempPosition = s.Position;
                s.Position = 0;
                Unsafe.As<byte, int>(ref buffer[0]) = uniqueNodesCount;
                s.Write(buffer, 0, sizeof(int));
                s.Position = tempPosition;

                //write links
                current = null;

                int tenPercent = (int)Math.Round(Environment.ProcessorCount * 0.1, 0);
                var logicalProcessors = tenPercent > 2 ? Environment.ProcessorCount - tenPercent : Environment.ProcessorCount;
                var itemsInPackage = ((int)(uniqueNodesCount / logicalProcessors));

                if (itemsInPackage < 500)
                {
                    itemsInPackage = uniqueNodesCount < 500 ? uniqueNodesCount : 500;
                }
                var threadsCount = (int)(uniqueNodesCount / itemsInPackage) > logicalProcessors ? logicalProcessors : (int)(uniqueNodesCount / itemsInPackage);

                if (threadsCount == 1)
                {
                    do
                    {
                        globalLinkId++;
                        if (current == null)
                        {
                            current = head;
                        }
                        else
                        {
                            current = current.Next;
                        }

                        if (current.Random == null)
                        {
                            WriteNullRefferenceValue(in s);
                            continue;
                        }

                        var randomLinkId = FindRealIdNode(current, globalLinkId);
                        Unsafe.As<byte, int>(ref buffer[0]) = globalLinkId;
                        s.Write(buffer, 0, sizeof(int));

                        Unsafe.As<byte, int>(ref buffer[0]) = randomLinkId;
                        s.Write(buffer, 0, sizeof(int));
                    }
                    while (current.Next != null);
                }
                else
                {
                    globalLinkId = 0;
                    current = head;
                    var tasksFindingLinks = new List<Task<bool>>(threadsCount);
                    var currentThread = 0;
                    var skeepCount = 0;
                    var lockStream = new object();

                    while ((globalLinkId < uniqueNodesCount - 1) && currentThread < threadsCount)
                    {
                        while (skeepCount > 0)
                        {
                            skeepCount--;
                            if (current == null)
                            {
                                current = head;
                            }
                            else
                            {
                                current = current.Next;
                            }
                            globalLinkId++;
                        }

                        currentThread++;

                        if (currentThread < threadsCount)
                        {
                            var task = FindLinks(current, itemsInPackage, globalLinkId, s, lockStream, buffer);
                            tasksFindingLinks.Add(task);
                        }
                        else
                        {
                            var steps = uniqueNodesCount - (itemsInPackage * (currentThread - 1));
                            var task = FindLinks(current, steps, globalLinkId, s, lockStream, buffer);
                            tasksFindingLinks.Add(task);
                        }
                        skeepCount = itemsInPackage;
                    }

                    Task.WaitAll(tasksFindingLinks.ToArray());
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNullRefferenceValue(in Stream s)
        {
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
        }

        Task<bool> FindLinks(
            ListNode node,
            int steps,
            int currentId,
            Stream s,
            object lockStream,
            byte[] buffer
            )
        {
            return Task.Factory.StartNew(() =>
            {
                ListNode current = null;
                int randomLinkId = -1;
                do
                {
                    if (current == null)
                    {
                        current = node;
                    }
                    else
                    {
                        current = current.Next;
                    }

                    if (current.Random == null)
                    {
                        lock(lockStream)
                        {
                            Unsafe.As<byte, int>(ref buffer[0]) = currentId;
                            s.Write(buffer, 0, sizeof(int));

                            Unsafe.As<byte, int>(ref buffer[0]) = -1;
                            s.Write(buffer, 0, sizeof(int));
                        }
                        
                        currentId++;
                        steps--;
                        continue;
                    }

                    randomLinkId = FindRealIdNode(current, currentId);
                    lock (lockStream)
                    {
                        Unsafe.As<byte, int>(ref buffer[0]) = currentId;
                        s.Write(buffer, 0, sizeof(int));

                        Unsafe.As<byte, int>(ref buffer[0]) = randomLinkId;
                        s.Write(buffer, 0, sizeof(int));
                    }
                    currentId++;
                    steps--;
                }
                while (steps > 0);

                return true;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindRealIdNode(ListNode node, int currentId)
        {
            var currentLoopId = currentId;
            ListNode current = null;
            do
            {
                if (current == null)
                {
                    current = node;
                }
                else
                {
                    current = current.Previous;
                    currentLoopId--;
                }

                if (!ReferenceEquals(node.Random, current))
                    continue;

                return currentLoopId;
            }
            while (current.Previous != null);

            currentLoopId = currentId;
            current = null;
            do
            {
                if (current == null)
                {
                    current = node;
                }
                else
                {
                    current = current.Next;
                    currentLoopId++;
                }

                if (!ReferenceEquals(node.Random, current))
                    continue;

                return currentLoopId;
            }
            while (current.Next != null);

            return -1;
        }

        /// <summary>
        /// Deserializes the list from the stream, returns the head node of the list
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when a stream has invalid data</exception>
        public Task<ListNode> Deserialize(Stream s)
        {
            return Task<ListNode>.Factory.StartNew(DeserializeInternal, s);
        }

        public ListNode DeserializeInternal(object obj)
        {
            var s = (Stream)obj;
            s.Position = 0;
            ListNode head = null;
            Span<byte> buffer = stackalloc byte[1000];

            if (s.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
            {
                throw new ArgumentException("Unexpected end of stream, expect four bytes");
            }

            var allUniqueNodes = new List<ListNode>(BitConverter.ToInt32(buffer.Slice(0, sizeof(int))));

            ListNode current = null;
            ListNode previous = null;

            while (s.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                var linkId = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkId == -1)//end unique nodes
                    break;

                current = new ListNode();
                allUniqueNodes.Add(current);
                if (previous != null)
                {
                    previous.Next = current;
                    current.Previous = previous;
                }
                else
                {
                    head = current;
                }

                if (s.Read(buffer.Slice(0, sizeof(int))) < sizeof(int))
                {
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");
                }

                var length = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (length != -1)
                {
                    int partDataSize = 0;
                    int offsetDestination = 0;
                    while(length > 0)
                    {
                        partDataSize = length > buffer.Length ? buffer.Length : length;
                        if (s.Read(buffer.Slice(0, partDataSize)) != partDataSize)
                        {
                            throw new ArgumentException("Unexpected end of stream, expect bytes represent string data");
                        }

                        current.Data = new string(' ', length / sizeof(char));
                        unsafe
                        {
                            fixed (byte* pSource = &buffer[0])
                            fixed (char* pDest = current.Data)
                            {
                                Buffer.MemoryCopy(pSource, pDest + offsetDestination, length, partDataSize);
                            }
                        }

                        offsetDestination += partDataSize;
                        length -= partDataSize;
                    }
                }

                previous = current;
            }

            while (s.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                int index = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));

                if (s.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");

                int linkIndex = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkIndex != -1)
                    allUniqueNodes[index].Random = allUniqueNodes[linkIndex];
            }

            return head;
        }

        /// <summary>
        /// Makes a deep copy of the list, returns the head node of the list 
        /// </summary>
        public Task<ListNode> DeepCopy(ListNode head)
        {
            return Task<ListNode>.Factory.StartNew(DeepCopyInternal, head);
        }

        private ListNode DeepCopyInternal(object obj)
        {
            var head = (ListNode)obj;
            using (var stream = new MemoryStream())
            {
                var taskSerialize = Serialize(head, stream);
                taskSerialize.Wait();
                var taskDeserialize = Deserialize(stream);
                taskDeserialize.Wait();
                return taskDeserialize.Result;
            }
        }
    }
}