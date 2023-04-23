Given on assignment: Exist interface "IListSerializer" and class "ListNode".<br>
Task: to implement "IListSerializer" with as efficient methods as possible and with minimal memory allocation.

## Results:
|                                           Method |   Size |             Mean |  Ratio |   Allocated |
|------------------------------------------------- |------- |-----------------:|-------:|------------:|
|           **&#39;V1(fastest): single thread algorithm&#39;** |    **100** |         **28.61 μs** |   **1.00** |    **30.47 KB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; |    100 |         35.46 μs |   1.24 |    16.34 KB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; |    100 |         21.93 μs |   0.77 |    18.52 KB |
|                                                  |        |                  |        |             |
|           **&#39;V1(fastest): single thread algorithm&#39;** |   **1000** |        **178.91 μs** |   **1.00** |   **286.65 KB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; |   1000 |      2,000.95 μs |  11.18 |    160.8 KB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; |   1000 |        176.38 μs |   0.99 |   176.49 KB |
|                                                  |        |                  |        |             |
|           **&#39;V1(fastest): single thread algorithm&#39;** |  **10000** |      **2,396.95 μs** |   **1.00** |  **3192.76 KB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; |  10000 |    195,788.66 μs |  81.70 |  1880.56 KB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; |  10000 |      2,627.84 μs |   1.10 |  2136.36 KB |
|                                                  |        |                  |        |             |
|           **&#39;V1(fastest): single thread algorithm&#39;** | **100000** |     **41,697.28 μs** |   **1.00** | **28879.64 KB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; | 100000 | 19,041,807.83 μs | 456.71 | 16663.63 KB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; | 100000 |     74,338.23 μs |   1.79 | 18692.77 KB |
