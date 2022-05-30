using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace ListSerializer
{
    public class ListSerializerV3 : IListSerializer
    {
        private static readonly byte[] NullReferenceBytes = new byte[] { 255, 255, 255, 255 };

        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(() =>
                {
                    var globalLinkId = 0;

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
                        s.Write(BitConverter.GetBytes(globalLinkId++));

                        if (current.Data == null)
                        {
                            s.Write(NullReferenceBytes);
                        }
                        else
                        {
                            byte[] bytes = Encoding.Unicode.GetBytes(current.Data);
                            var lengthBytes = BitConverter.GetBytes(bytes.Length);
                            s.Write(lengthBytes);
                            s.Write(bytes);
                        }
                    }
                    while (current.Next != null);

                    //write comma between packages and links
                    s.Write(NullReferenceBytes);

                    var uniqueNodesCount = globalLinkId;
                    globalLinkId = -1;
                    //write links
                    current = null;

                    int tenPercent = (int)Math.Round(Environment.ProcessorCount * 0.1, 0);
                    var logicalProcessors = tenPercent > 2 ? Environment.ProcessorCount - tenPercent : Environment.ProcessorCount;
                    var itemsInPackage = ((int)(uniqueNodesCount / logicalProcessors));
                    
                    if(itemsInPackage < 500)
                    {
                        itemsInPackage = uniqueNodesCount < 500 ? uniqueNodesCount : 500;
                    }
                    var threadsCount = (int)(uniqueNodesCount/ itemsInPackage) > logicalProcessors ? logicalProcessors : (int)(uniqueNodesCount / itemsInPackage);

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
                                s.Write(NullReferenceBytes);
                                continue;
                            }

                            var randomLinkId = FindRealIdNode(current, globalLinkId);
                            s.Write(BitConverter.GetBytes(randomLinkId));
                        }
                        while (current.Next != null);
                    }
                    else
                    {
                        globalLinkId = 0;
                        current = head;
                        var tasksFindingLinks = new List<Task<List<int>>>(threadsCount);
                        var currentThread = 0;
                        var skeepCount = 0;

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
                                var task = FindLinks(current, itemsInPackage, globalLinkId);
                                tasksFindingLinks.Add(task);
                            }
                            else
                            {
                                var steps = uniqueNodesCount - (itemsInPackage * (currentThread - 1));
                                var task = FindLinks(current, steps, globalLinkId);
                                tasksFindingLinks.Add(task);
                            }
                            skeepCount = itemsInPackage;
                        }

                        foreach (var task in tasksFindingLinks)
                        {
                            task.Wait();
                            var result = task.Result;
                            foreach(var link in result)
                                s.Write(BitConverter.GetBytes(link));
                        }
                    }
                })
                ;
        }

        Task<List<int>> FindLinks(ListNode node, int steps, int currentId)
        {
            return Task.Factory.StartNew(() =>
            {
                var findLinks = new List<int>(steps);

                ListNode current = null;
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
                        findLinks.Add(-1);
                        currentId++;
                        steps--;
                        continue;
                    }

                    var randomLinkId = FindRealIdNode(current, currentId);
                    findLinks.Add(randomLinkId);
                    currentId++;
                    steps--;
                }
                while (steps > 0);

                return findLinks;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindRealIdNode(ListNode node, int currentId)
        {
            CancellationTokenSource ctsUpSeeker = new CancellationTokenSource();
            var ctUpSeeker = ctsUpSeeker.Token;
            CancellationTokenSource ctsDownSeeker = new CancellationTokenSource();
            var ctDownSeeker = ctsDownSeeker.Token;

            try
            {
                var currentIdUp = currentId;
                Task<int> upSeeker = Task.Factory.StartNew(() =>
                {
                    ListNode current = null;
                    do
                    {
                        ctUpSeeker.ThrowIfCancellationRequested();

                        if (current == null)
                        {
                            current = node;
                        }
                        else
                        {
                            current = current.Previous;
                            currentIdUp--;
                        }

                        if (!object.Equals(node.Random, current) || !object.ReferenceEquals(node.Random, current))
                            continue;

                        ctsDownSeeker.Cancel();
                        return currentIdUp;
                    }
                    while (current.Previous != null);

                    return -1;
                }, ctUpSeeker);

                var currentIdDown = currentId;
                Task<int> downSeeker = Task.Factory.StartNew(() =>
                {
                    ListNode current = null;
                    do
                    {
                        ctDownSeeker.ThrowIfCancellationRequested();

                        if (current == null)
                        {
                            current = node;
                        }
                        else
                        {
                            current = current.Next;
                            currentIdDown++;
                        }

                        if (!object.Equals(node.Random, current) || !object.ReferenceEquals(node.Random, current))
                            continue;

                        ctsUpSeeker.Cancel();
                        return currentIdDown;
                    }
                    while (current.Next != null);

                    return -1;
                }, ctDownSeeker);

                int result = -1;
                try
                {
                    upSeeker.Wait();
                    result = upSeeker.Result;
                }
                catch(OperationCanceledException)
                {
                    //ignore
                }
                catch (AggregateException agg)
                {
                    if(!agg.InnerExceptions.Any(ex => ex is OperationCanceledException))
                        throw;

                    //ignore
                }
                catch
                {
                    throw;
                }

                if (result != -1)
                    return result;

                try
                {
                    downSeeker.Wait();
                    result = downSeeker.Result;
                }
                catch (OperationCanceledException)
                {
                    //ignore
                }
                catch (AggregateException agg)
                {
                    if (!agg.InnerExceptions.Any(ex => ex is OperationCanceledException))
                        throw;

                    //ignore
                }
                catch
                {
                    throw;
                }

                return result;
            }
            finally
            {
                ctsUpSeeker.Dispose();
                ctsDownSeeker.Dispose();
            }
        }

        /// <summary>
        /// Deserializes the list from the stream, returns the head node of the list
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when a stream has invalid data</exception>
        public Task<ListNode> Deserialize(Stream s)
        {
            return Task<ListNode>.Factory.StartNew(() =>
            {
                s.Position = 0;
                ListNode head = null;
                var bufferLink = new byte[4];
                var allUniqueNodes = new List<ListNode>();

                ListNode current = null;
                ListNode previous = null;
                while (s.Read(bufferLink, 0, bufferLink.Length) > 0)
                {
                    var linkId = BitConverter.ToInt32(bufferLink, 0);
                    if(linkId == -1)//end unique nodes
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

                    var bufferLength = new byte[4];
                    if (s.Read(bufferLength, 0, bufferLength.Length) <= 0)
                    {
                        throw new ArgumentException("Unexpected end of stream, expect four bytes");
                    }

                    var length = BitConverter.ToInt32(bufferLength);
                    if (length != -1)
                    {
                        var bufferData = new byte[length];
                        if (s.Read(bufferData, 0, bufferData.Length) <= 0)
                        {
                            throw new ArgumentException("Unexpected end of stream, expect bytes represent string data");
                        }

                        current.Data = Encoding.Unicode.GetString(bufferData);
                    }

                    previous = current;
                }

                int uniqueNodesIndex = 0;
                while (s.Read(bufferLink, 0, bufferLink.Length) > 0)
                {
                    int linkIndex = BitConverter.ToInt32(bufferLink, 0);
                    if(linkIndex != -1)
                        allUniqueNodes[uniqueNodesIndex].Random = allUniqueNodes[linkIndex];

                    uniqueNodesIndex++;
                }

                return head;
            });
        }

        /// <summary>
        /// Makes a deep copy of the list, returns the head node of the list 
        /// </summary>
        public Task<ListNode> DeepCopy(ListNode head)
        {
            return Task<ListNode>.Factory.StartNew(() =>
            {
                using (var stream = new MemoryStream())
                {
                    var taskSerialize = Serialize(head, stream);
                    taskSerialize.Wait();
                    var taskDeserialize = Deserialize(stream);
                    taskDeserialize.Wait();
                    return taskDeserialize.Result;
                }
            });
        }
    }
}