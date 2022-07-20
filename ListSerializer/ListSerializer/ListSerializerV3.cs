using Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
                ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;
                var intBytes = new byte[sizeof(int)];

                //package [linkBytes 4byte][length 4byte][data]
                //All stream packages...link datas 'idLinkNodeHead'...'idLinkNodeTail'
                s.Position = 0;
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
                    Unsafe.As<byte, int>(ref intBytes[0]) = globalLinkId++;
                    s.Write(intBytes);

                    if (current.Data == null)
                    {
                        WriteNullRefferenceValue(in s);
                    }
                    else
                    {
                        var byteCount = Encoding.Unicode.GetMaxByteCount(current.Data.Count());
                        var bytes = arrayPool.Rent(byteCount);
                        try
                        {
                            var size = Encoding.Unicode.GetBytes(current.Data, bytes);
                            Unsafe.As<byte, int>(ref intBytes[0]) = size;
                            s.Write(intBytes);
                            s.Write(bytes, 0, size);
                        }
                        finally
                        {
                            arrayPool.Return(bytes);
                        }
                    }
                }
                while (current.Next != null);

                //write comma between packages and links
                WriteNullRefferenceValue(in s);

                var uniqueNodesCount = globalLinkId;
                globalLinkId = -1;
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
                        Unsafe.As<byte, int>(ref intBytes[0]) = globalLinkId;
                        s.Write(intBytes);

                        Unsafe.As<byte, int>(ref intBytes[0]) = randomLinkId;
                        s.Write(intBytes);
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
                            var task = FindLinks(current, itemsInPackage, globalLinkId, s, lockStream, intBytes);
                            tasksFindingLinks.Add(task);
                        }
                        else
                        {
                            var steps = uniqueNodesCount - (itemsInPackage * (currentThread - 1));
                            var task = FindLinks(current, steps, globalLinkId, s, lockStream, intBytes);
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
            byte[] intBytes
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
                            Unsafe.As<byte, int>(ref intBytes[0]) = currentId;
                            s.Write(intBytes);

                            Unsafe.As<byte, int>(ref intBytes[0]) = -1;
                            s.Write(intBytes);
                        }
                        
                        currentId++;
                        steps--;
                        continue;
                    }

                    randomLinkId = FindRealIdNode(current, currentId);
                    lock (lockStream)
                    {
                        Unsafe.As<byte, int>(ref intBytes[0]) = currentId;
                        s.Write(intBytes);

                        Unsafe.As<byte, int>(ref intBytes[0]) = randomLinkId;
                        s.Write(intBytes);
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
            Span<byte> bufferForInt32 = stackalloc byte[4];
            var allUniqueNodes = new List<ListNode>();

            ListNode current = null;
            ListNode previous = null;
            ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;

            while (s.Read(bufferForInt32) == bufferForInt32.Length)
            {
                var linkId = BitConverter.ToInt32(bufferForInt32);
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

                if (s.Read(bufferForInt32) < bufferForInt32.Length)
                {
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");
                }

                var length = BitConverter.ToInt32(bufferForInt32);
                if (length != -1)
                {
                    var bufferData = arrayPool.Rent(length);
                    try
                    {
                        if (s.Read(bufferData, 0, length) != length)
                        {
                            throw new ArgumentException("Unexpected end of stream, expect bytes represent string data");
                        }

                        current.Data = Encoding.Unicode.GetString(new Span<byte>(bufferData, 0, length));
                    }
                    finally
                    {
                        arrayPool.Return(bufferData);
                    }
                }

                previous = current;
            }

            while (s.Read(bufferForInt32) == bufferForInt32.Length)
            {
                int index = BitConverter.ToInt32(bufferForInt32);

                if (s.Read(bufferForInt32) != bufferForInt32.Length)
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");

                int linkIndex = BitConverter.ToInt32(bufferForInt32);
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