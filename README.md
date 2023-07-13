Given on assignment: Exist interface "IListSerializer" and class "ListNode".<br>
Task: to implement "IListSerializer" with as efficient methods as possible and with minimal memory allocation.

## Benchmark with duplicate datas:
|                                                                    Method | Size |         Mean | Ratio |   Allocated |
|-------------------------------------------------------------------------- |----- |-------------:|------:|------------:|
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate datas&#39;** |  **100** |     **31.19 μs** |  **1.00** |    **62.17 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; |  100 |     38.44 μs |  1.24 |    30.17 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; |  100 |     27.63 μs |  0.90 |    33.52 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate datas&#39;** |  **500** |    **262.47 μs** |  **1.00** |   **817.75 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; |  500 |    515.60 μs |  1.96 |   415.67 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; |  500 |    254.38 μs |  0.97 |   427.63 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate datas&#39;** | **1000** |    **973.27 μs** |  **1.00** |  **3049.35 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; | 1000 |  2,074.76 μs |  2.13 |   1510.5 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; | 1000 |    728.49 μs |  0.75 |  1534.43 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate datas&#39;** | **2500** |  **8,199.02 μs** |  **1.00** | **21792.94 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; | 2500 | 13,710.11 μs |  1.68 | 10892.45 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; | 2500 |  5,097.87 μs |  0.62 | 10973.34 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate datas&#39;** | **5000** | **28,331.45 μs** |  **1.00** | **86146.56 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; | 5000 | 49,715.13 μs |  1.75 | 43038.31 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; | 5000 | 19,613.77 μs |  0.70 | 43198.16 KB |
