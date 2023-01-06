using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
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
                SerializeInternal(in head, s);
            });
        }

        [SkipLocalsInit]
        private void SerializeInternal(in ListNode head, Stream s)
        {
            var globalLinkId = 0;
            Span<byte> buffer = stackalloc byte[500];

            //package [linkBytes 4byte][length 4byte][data]
            //All stream packages...link datas 'idLinkNodeHead'...'idLinkNodeTail'
            s.Position = 0;
            s.Write(buffer.Slice(0, sizeof(int)));//reserved for count all unique nodes
            var uniqueNodes = new List<ListNode>();

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
                s.Write(buffer.Slice(0, sizeof(int)));

                if (current.Data == null)
                {
                    WriteNullRefferenceValue(in s);
                }
                else
                {
                    var size = current.Data.Length * sizeof(char);
                    Unsafe.As<byte, int>(ref buffer[0]) = size;
                    s.Write(buffer.Slice(0, sizeof(int)));

                    int partDataSize = 0;
                    int offsetDestination = 0;
                    unsafe
                    {
                        fixed (byte* pDest = &buffer[0])
                        fixed (char* pSource = current.Data)
                        {
                            while (size > 0)
                            {
                                partDataSize = size > buffer.Length ? buffer.Length : size;
                                Buffer.MemoryCopy(pSource + offsetDestination, pDest, buffer.Length, partDataSize);
                                s.Write(buffer.Slice(0, partDataSize));

                                offsetDestination += partDataSize;
                                size -= partDataSize;
                            }
                        }
                    }
                }

                uniqueNodes.Add(current);
            }
            while (current.Next != null);

            //write comma between packages and links
            WriteNullRefferenceValue(in s);

            globalLinkId = -1;
            //write uniqueNodesCount
            long tempPosition = s.Position;
            s.Position = 0;
            Unsafe.As<byte, int>(ref buffer[0]) = uniqueNodes.Count;
            s.Write(buffer.Slice(0, sizeof(int)));
            s.Position = tempPosition;

            //write links
            current = null;

            int tenPercent = (int)Math.Round(Environment.ProcessorCount * 0.1, 0);
            var logicalProcessors = tenPercent > 2 ? Environment.ProcessorCount - tenPercent : Environment.ProcessorCount;
            if (uniqueNodes.Count <= 200)
            {
                for (int i = 0; i < uniqueNodes.Count; i++)
                {
                    current = uniqueNodes[i];
                    if (current.Random == null)
                    {
                        WriteNullRefferenceValue(in s);
                        continue;
                    }

                    int randomLinkId = EnumerationSearch(in uniqueNodes, in current);
                    Unsafe.As<byte, int>(ref buffer[0]) = i;
                    s.Write(buffer.Slice(0, sizeof(int)));

                    Unsafe.As<byte, int>(ref buffer[0]) = randomLinkId;
                    s.Write(buffer.Slice(0, sizeof(int)));
                }
            }
            else
            {
                var itemsInPackage = ((int)(uniqueNodes.Count / logicalProcessors));
                var threadsCount = (int)(uniqueNodes.Count / itemsInPackage) > logicalProcessors ? logicalProcessors : (int)(uniqueNodes.Count / itemsInPackage);

                globalLinkId = 0;
                current = head;
                var tasksFindingLinks = new List<Task<bool>>(threadsCount);
                var currentThread = 0;
                var skeepCount = 0;
                var lockStream = new object();

                while ((globalLinkId < uniqueNodes.Count - 1) && currentThread < threadsCount)
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
                        var parametrs = new FindLinksParam(uniqueNodes, itemsInPackage, globalLinkId, s, lockStream);
                        tasksFindingLinks.Add(Task.Factory.StartNew(FindLinks, parametrs));
                    }
                    else
                    {
                        var parametrs =
                            new FindLinksParam(
                                uniqueNodes,
                                uniqueNodes.Count - (itemsInPackage * (currentThread - 1)),
                                globalLinkId,
                                s,
                                lockStream
                                );
                        tasksFindingLinks.Add(Task.Factory.StartNew(FindLinks, parametrs));
                    }
                    skeepCount = itemsInPackage;
                }

                Task.WaitAll(tasksFindingLinks.ToArray());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNullRefferenceValue(in Stream s)
        {
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
        }

        [SkipLocalsInit]
        private class FindLinksParam
        {
            public FindLinksParam(
                List<ListNode> nodes,
                int steps,
                int currentId,
                Stream s,
                object lockStream
                )
            {
                Nodes = nodes;
                Steps = steps;
                CurrentId = currentId;
                Stream = s;
                LockStream = lockStream;
            }

            public List<ListNode> Nodes;
            public int Steps;
            public int CurrentId;
            public Stream Stream;
            public object LockStream;
        }

        [SkipLocalsInit]
        bool FindLinks(object paramObj)
        {
            var param = (FindLinksParam)paramObj;
            Span<byte> buffer = stackalloc byte[sizeof(int) * 2 * 10];
            byte bufferOffset = 0;
            int randomLinkId = -1;

            for (int indx = param.CurrentId; indx < param.CurrentId + param.Steps; indx++)
            {
                var current = param.Nodes[indx];
                if (current.Random == null)
                {
                    Unsafe.As<byte, int>(ref buffer[bufferOffset]) = indx;
                    Unsafe.As<byte, int>(ref buffer[bufferOffset + sizeof(int)]) = -1;
                    bufferOffset += sizeof(int) * 2;

                    if (bufferOffset == buffer.Length)
                    {
                        bufferOffset = 0;

                        int i = 0;
                        while (true)
                        {
                            if (Monitor.TryEnter(param.LockStream))
                            {
                                try
                                {
                                    param.Stream.Write(buffer);
                                    break;
                                }
                                finally
                                {
                                    Monitor.Exit(param.LockStream);
                                }
                            }
                            else
                            {
                                if (i++ == 5000)
                                {
                                    lock (param.LockStream)
                                    {
                                        param.Stream.Write(buffer);
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    continue;
                }

                randomLinkId = EnumerationSearch(in param.Nodes, in current);
                if (randomLinkId < 0)
                {
                    throw new ArgumentException("Algorithm error");
                }

                Unsafe.As<byte, int>(ref buffer[bufferOffset]) = indx;
                Unsafe.As<byte, int>(ref buffer[bufferOffset + sizeof(int)]) = randomLinkId;
                bufferOffset += sizeof(int) * 2;

                if (bufferOffset == buffer.Length)
                {
                    bufferOffset = 0;
                    int i = 0;
                    while (true)
                    {
                        if (Monitor.TryEnter(param.LockStream))
                        {
                            try
                            {
                                param.Stream.Write(buffer);
                                break;
                            }
                            finally
                            {
                                Monitor.Exit(param.LockStream);
                            }
                        }
                        else
                        {
                            if (i++ == 5000)
                            {
                                lock (param.LockStream)
                                {
                                    param.Stream.Write(buffer);
                                }
                                break;
                            }
                        }
                    }
                }
            }

            if (bufferOffset != 0)
            {
                int i = 0;
                while (true)
                {
                    if (Monitor.TryEnter(param.LockStream))
                    {
                        try
                        {
                            param.Stream.Write(buffer.Slice(0, bufferOffset));
                            break;
                        }
                        finally
                        {
                            Monitor.Exit(param.LockStream);
                        }
                    }
                    else
                    {
                        if (i++ == 5000)
                        {
                            lock (param.LockStream)
                            {
                                param.Stream.Write(buffer.Slice(0, bufferOffset));
                            }
                            break;
                        }
                    }
                }
            }

            return true;
        }

        private int EnumerationSearch(in List<ListNode> uniqueNodes, in ListNode node)
        {
            for (int i = 0; i < uniqueNodes.Count; i++)
            {
                var currentNode = uniqueNodes[i];
                if (ReferenceEquals(node.Random, currentNode))
                {
                    return i;
                }
            }

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

        [SkipLocalsInit]
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
                    current.Data = new string(' ', length / sizeof(char));

                    unsafe
                    {
                        fixed (byte* pSource = &buffer[0])
                        fixed (char* pDest = current.Data)
                        {
                            while (length > 0)
                            {
                                partDataSize = length > buffer.Length ? buffer.Length : length;
                                if (s.Read(buffer.Slice(0, partDataSize)) != partDataSize)
                                {
                                    throw new ArgumentException("Unexpected end of stream, expect bytes represent string data");
                                }

                                Buffer.MemoryCopy(pSource, pDest + offsetDestination, length, partDataSize);

                                offsetDestination += partDataSize;
                                length -= partDataSize;
                            }
                        }
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