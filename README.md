Given on assignment: Exist interface "IListSerializer" and class "ListNode".<br>
Task: to implement "IListSerializer" with as efficient methods as possible and with minimal memory allocation.

## Benchmark with duplicate data:
|                                                                    Method | Size |         Mean | Ratio |   Allocated |
|-------------------------------------------------------------------------- |----- |-------------:|------:|------------:|
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** |  **100** |     **31.19 μs** |  **1.00** |    **62.17 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; |  100 |     38.44 μs |  1.24 |    30.17 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; |  100 |     27.63 μs |  0.90 |    33.52 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** |  **500** |    **262.47 μs** |  **1.00** |   **817.75 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; |  500 |    515.60 μs |  1.96 |   415.67 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; |  500 |    254.38 μs |  0.97 |   427.63 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** | **1000** |    **973.27 μs** |  **1.00** |  **3049.35 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; | 1000 |  2,074.76 μs |  2.13 |   1510.5 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; | 1000 |    728.49 μs |  0.75 |  1534.43 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** | **2500** |  **8,199.02 μs** |  **1.00** | **21792.94 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; | 2500 | 13,710.11 μs |  1.68 | 10892.45 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; | 2500 |  5,097.87 μs |  0.62 | 10973.34 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** | **5000** | **28,331.45 μs** |  **1.00** | **86146.56 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; | 5000 | 49,715.13 μs |  1.75 | 43038.31 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; | 5000 | 19,613.77 μs |  0.70 | 43198.16 KB |

## Benchmark without duplicate data:
|                                                                    Method | Size |         Mean | Ratio |   Allocated |
|-------------------------------------------------------------------------- |----- |-------------:|------:|------------:|
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** |  **100** |     **29.57 μs** |  **1.00** |    **62.25 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; |  100 |     39.53 μs |  1.34 |    52.13 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; |  100 |     32.16 μs |  1.09 |    56.49 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** |  **500** |    **260.57 μs** |  **1.00** |   **814.12 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; |  500 |    635.93 μs |  2.44 |   779.85 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; |  500 |    370.75 μs |  1.42 |   795.83 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** | **1000** |    **945.42 μs** |  **1.00** |  **3050.13 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; | 1000 |  2,486.49 μs |  2.63 |  2964.28 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; | 1000 |  1,234.41 μs |  1.31 |  2996.26 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** | **2500** |  **8,254.24 μs** |  **1.00** | **21795.08 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; | 2500 | 15,292.51 μs |  1.89 | 21654.68 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; | 2500 |  7,897.08 μs |  0.98 | 21750.69 KB |
|                                                                           |      |              |       |             |
| **&#39;V1: single thread algorithm on Dictionary and not check duplicate data&#39;** | **5000** | **28,572.14 μs** |  **1.00** | **86151.33 KB** |
|                          &#39;V2(smallest by memory): multi thread algorithm&#39; | 5000 | 63,178.92 μs |  2.22 |  85681.3 KB |
|                                &#39;V3 (compromise): multi thread algorithm)&#39; | 5000 | 32,784.96 μs |  1.17 | 85873.12 KB |
