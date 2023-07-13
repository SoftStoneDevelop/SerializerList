Given on assignment: Exist interface "IListSerializer" and class "ListNode".<br>
Task: to implement "IListSerializer" with as efficient methods as possible and with minimal memory allocation.

## Results:
|                                           Method | Size |          Mean | Ratio |   Allocated |
|------------------------------------------------- |----- |--------------:|------:|------------:|
|           **&#39;V1(fastest): single thread algorithm&#39;** |  **100** |      **27.93 μs** |  **1.00** |    **62.17 KB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; |  100 |      34.82 μs |  1.22 |    48.05 KB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; |  100 |      24.72 μs |  0.87 |    50.26 KB |
|                                                  |      |               |       |             |
|           **&#39;V1(fastest): single thread algorithm&#39;** |  **500** |     **263.43 μs** |  **1.00** |   **813.72 KB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; |  500 |     862.90 μs |  3.28 |   760.61 KB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; |  500 |     301.32 μs |  1.14 |   768.39 KB |
|                                                  |      |               |       |             |
|           **&#39;V1(fastest): single thread algorithm&#39;** | **1000** |     **965.94 μs** |  **1.00** |  **3049.35 KB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; | 1000 |   4,046.92 μs |  4.19 |  2923.51 KB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; | 1000 |   1,048.29 μs |  1.08 |  2939.27 KB |
|                                                  |      |               |       |             |
|           **&#39;V1(fastest): single thread algorithm&#39;** | **2500** |   **7,820.88 μs** |  **1.00** | **21793.35 KB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; | 2500 |  29,285.10 μs |  3.73 | 21485.98 KB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; | 2500 |   8,412.66 μs |  1.08 | 21550.05 KB |
|                                                  |      |               |       |             |
|           **&#39;V1(fastest): single thread algorithm&#39;** | **5000** |  **28,291.58 μs** |  **1.00** |  **86146.6 KB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; | 5000 | 187,971.71 μs |  6.65 | 85496.04 KB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; | 5000 |  28,657.29 μs |  1.01 | 85622.64 KB |
