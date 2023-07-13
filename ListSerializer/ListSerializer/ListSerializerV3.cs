using Common;
using ListSerializer.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ListSerializer
{
    file class SerializeParametrs
    {
        public SerializeParametrs(
            ListNode head,
            Stream stream,
            List<ListNode> allUniqueNodes,
            List<string> allUniqueDatas
            )
        {
            Stream = stream;
            AllUniqueNodes = allUniqueNodes;
            AllUniqueDatas = allUniqueDatas;
            Head = head;
        }

        public ListNode Head;
        public Stream Stream;
        public List<ListNode> AllUniqueNodes;
        public List<string> AllUniqueDatas;
    }

    file class DeserializeParams
    {
        public DeserializeParams(
            Stream stream,
            List<ListNode> allUniqueNodes,
            List<string> allUniqueDatas
            )
        {
            Stream = stream;
            AllUniqueNodes = allUniqueNodes;
            AllUniqueDatas = allUniqueDatas;
        }

        public Stream Stream;
        public List<ListNode> AllUniqueNodes;
        public List<string> AllUniqueDatas;
    }

    [SkipLocalsInit]
    file class FindLinksParam
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

    public class ListSerializerV3 : IListSerializer
    {
        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(SerializeInternal, new SerializeParametrs(head, s, null, null));
        }

        [SkipLocalsInit]
        private void SerializeInternal(object obj)
        {
            var param = (SerializeParametrs)obj;
            Span<byte> buffer = stackalloc byte[500];

            //package [linkBytes 4byte][length 4byte][data]
            //All stream packages...link 'datas' ... 'idLinkNodeHead'...'idLinkNodeTail'
            param.Stream.Position = 0;
            param.Stream.Write(buffer.Slice(0, sizeof(int) * 2));//reserved for count all unique nodes and datas
            if (param.AllUniqueNodes == null)
            {
                param.AllUniqueNodes = new List<ListNode>();
            }
            else
            {
                param.AllUniqueNodes.Clear();
            }

            var datas = new HashSet<string>();
            ListNode current = null;

            var globalLinkId = 0;
            do
            {
                if (current == null)
                {
                    current = param.Head;
                }
                else
                {
                    current = current.Next;
                }

                globalLinkId++;
                if (current.Data == null)
                {
                    //nothing
                }
                else if(datas.Add(current.Data))
                {
                    var size = current.Data.Length * sizeof(char);
                    Unsafe.As<byte, int>(ref buffer[0]) = size;
                    param.Stream.Write(buffer.Slice(0, sizeof(int)));

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
                                Buffer.MemoryCopy(pSource + (offsetDestination / sizeof(char)), pDest, buffer.Length, partDataSize);
                                param.Stream.Write(buffer.Slice(0, partDataSize));

                                offsetDestination += partDataSize;
                                size -= partDataSize;
                            }
                        }
                    }
                }

                param.AllUniqueNodes.Add(current);
            }
            while (current.Next != null);
            param.AllUniqueDatas = datas.ToList();

            //write comma between packages and links
            WriteNullRefferenceValue(in param.Stream);

            //write uniqueNodesCount
            long tempPosition = param.Stream.Position;
            param.Stream.Position = 0;
            Unsafe.As<byte, int>(ref buffer[0]) = param.AllUniqueDatas.Count;
            param.Stream.Write(buffer.Slice(0, sizeof(int)));
            Unsafe.As<byte, int>(ref buffer[0]) = param.AllUniqueNodes.Count;
            param.Stream.Write(buffer.Slice(0, sizeof(int)));
            param.Stream.Position = tempPosition;

            current = null;
            globalLinkId = 0;

            do
            {
                if (current == null)
                {
                    current = param.Head;
                }
                else
                {
                    current = current.Next;
                }

                //write id unique Node
                Unsafe.As<byte, int>(ref buffer[0]) = globalLinkId++;
                param.Stream.Write(buffer.Slice(0, sizeof(int)));

                if (current.Data == null)
                {
                    WriteNullRefferenceValue(in param.Stream);
                }
                else
                {
                    var index = StringHashSetHelper.FindItemIndex(datas, current.Data);
                    if (index == -1)
                    {
                        throw new Exception("Unknown string");
                    }

                    Unsafe.As<byte, int>(ref buffer[0]) = index;
                    param.Stream.Write(buffer.Slice(0, sizeof(int)));
                }
            }
            while (current.Next != null);

            //write comma between packages and links
            WriteNullRefferenceValue(in param.Stream);

            //write links
            current = null;

            int tenPercent = (int)Math.Round(Environment.ProcessorCount * 0.1, 0);
            var logicalProcessors = tenPercent > 2 ? Environment.ProcessorCount - tenPercent : Environment.ProcessorCount;
            if (param.AllUniqueNodes.Count <= 200)
            {
                var spanNodes = CollectionsMarshal.AsSpan(param.AllUniqueNodes);
                for (int i = 0; i < spanNodes.Length; i++)
                {
                    current = spanNodes[i];
                    if (current.Random == null)
                    {
                        WriteNullRefferenceValue(in param.Stream);
                        continue;
                    }

                    int randomLinkId = EnumerationSearch(in spanNodes, in current);
                    Unsafe.As<byte, int>(ref buffer[0]) = i;
                    param.Stream.Write(buffer.Slice(0, sizeof(int)));

                    Unsafe.As<byte, int>(ref buffer[0]) = randomLinkId;
                    param.Stream.Write(buffer.Slice(0, sizeof(int)));
                }
            }
            else
            {
                var itemsInPackage = ((int)(param.AllUniqueNodes.Count / logicalProcessors));
                var threadsCount = (int)(param.AllUniqueNodes.Count / itemsInPackage) > logicalProcessors ? logicalProcessors : (int)(param.AllUniqueNodes.Count / itemsInPackage);

                globalLinkId = 0;
                var tasksFindingLinks = new List<Task<bool>>(threadsCount);
                var currentThread = 0;
                var skeepCount = 0;
                var lockStream = new object();

                while ((globalLinkId < param.AllUniqueNodes.Count - 1) && currentThread < threadsCount)
                {
                    if (skeepCount > 0)
                    {
                        globalLinkId += skeepCount;
                        skeepCount = 0;
                    }

                    if (++currentThread < threadsCount)
                    {
                        var parametrs = new FindLinksParam(param.AllUniqueNodes, itemsInPackage, globalLinkId, param.Stream, lockStream);
                        tasksFindingLinks.Add(Task.Factory.StartNew(FindLinks, parametrs));
                    }
                    else
                    {
                        var parametrs =
                            new FindLinksParam(
                                param.AllUniqueNodes,
                                param.AllUniqueNodes.Count - (itemsInPackage * (currentThread - 1)),
                                globalLinkId,
                                param.Stream,
                                lockStream
                                );
                        tasksFindingLinks.Add(Task.Factory.StartNew(FindLinks, parametrs));
                    }

                    skeepCount = itemsInPackage;
                }

                Task.WaitAll(tasksFindingLinks.ToArray());
            }
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNullRefferenceValue(in Stream s)
        {
            Span<byte> nullRef = stackalloc byte[4] { byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue };
            s.Write(nullRef);
        }

        [SkipLocalsInit]
        bool FindLinks(object paramObj)
        {
            var param = (FindLinksParam)paramObj;
            Span<byte> buffer = stackalloc byte[sizeof(int) * 2 * 10];
            byte bufferOffset = 0;
            int randomLinkId = -1;

            var spanNodes = CollectionsMarshal.AsSpan(param.Nodes);
            for (int indx = param.CurrentId; indx < param.CurrentId + param.Steps; indx++)
            {
                var current = spanNodes[indx];
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

                randomLinkId = EnumerationSearch(in spanNodes, in current);
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

        private int EnumerationSearch(in Span<ListNode> uniqueNodes, in ListNode node)
        {
            for (int i = 0; i < uniqueNodes.Length; i++)
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
            return Task<ListNode>.Factory.StartNew(DeserializeInternal, new DeserializeParams(s, null, null));
        }

        [SkipLocalsInit]
        public ListNode DeserializeInternal(object obj)
        {
            var param = (DeserializeParams)obj;
            param.Stream.Position = 0;
            Span<byte> buffer = stackalloc byte[1000];

            if (param.Stream.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
            {
                throw new ArgumentException("Unexpected end of stream, expect four bytes");
            }

            var datasCount = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
            if (param.AllUniqueDatas == null)
            {
                param.AllUniqueDatas = new List<string>(datasCount);
            }
            else
            {
                param.AllUniqueDatas.Clear();
            }

            if (param.Stream.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
            {
                throw new ArgumentException("Unexpected end of stream, expect four bytes");
            }

            if (param.AllUniqueNodes == null)
            {
                param.AllUniqueNodes = new List<ListNode>(BitConverter.ToInt32(buffer.Slice(0, sizeof(int))));
            }
            else
            {
                param.AllUniqueNodes.Clear();
            }

            for (int i = 0; i < datasCount; i++)
            {
                if (param.Stream.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
                {
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");
                }

                var length = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                int partDataSize = 0;
                int offsetDestination = 0;
                var newStr = StringHelper.FastAllocateString(length / sizeof(char));

                unsafe
                {
                    fixed (byte* pSource = &buffer[0])
                    fixed (char* pDest = newStr)
                    {
                        while (length > 0)
                        {
                            partDataSize = length > buffer.Length ? buffer.Length : length;
                            if (param.Stream.Read(buffer.Slice(0, partDataSize)) != partDataSize)
                            {
                                throw new ArgumentException("Unexpected end of stream, expect bytes represent string data");
                            }

                            Buffer.MemoryCopy(pSource, pDest + (offsetDestination / sizeof(char)), length, partDataSize);

                            offsetDestination += partDataSize;
                            length -= partDataSize;
                        }
                    }
                }

                param.AllUniqueDatas.Insert(i, newStr);
            }

            if (param.Stream.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))//comma
            {
                throw new ArgumentException("Unexpected end of stream, expect four bytes");
            }

            ListNode head = null;
            ListNode current = null;
            ListNode previous = null;

            while (param.Stream.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                var linkId = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkId == -1)//end unique nodes
                    break;

                current = new ListNode();
                param.AllUniqueNodes.Add(current);
                if (previous != null)
                {
                    previous.Next = current;
                    current.Previous = previous;
                }
                else
                {
                    head = current;
                }

                if (param.Stream.Read(buffer.Slice(0, sizeof(int))) < sizeof(int))
                {
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");
                }

                var dataIndex = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (dataIndex != -1)
                {
                    current.Data = param.AllUniqueDatas[dataIndex];
                }

                previous = current;
            }

            var nodesSpan = CollectionsMarshal.AsSpan(param.AllUniqueNodes);
            while (param.Stream.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                int index = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));

                if (param.Stream.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");

                int linkIndex = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkIndex != -1)
                    nodesSpan[index].Random = nodesSpan[linkIndex];
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
                var nodes = new List<ListNode>();
                var datas = new List<string>();
                SerializeInternal(new SerializeParametrs(head, stream, nodes, datas));
                return 
                    DeserializeInternal(new DeserializeParams(stream, nodes, datas));
            }
        }
    }
}