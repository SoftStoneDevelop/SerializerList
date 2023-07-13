Given on assignment: Exist interface "IListSerializer" and class "ListNode".<br>
Task: to implement "IListSerializer" with as efficient methods as possible and with minimal memory allocation.

## Benchmark:
|                                           Method |  Size |           Mean | Ratio | Completed Work Items | Lock Contentions |  Allocated |
|------------------------------------------------- |------ |---------------:|------:|---------------------:|-----------------:|-----------:|
|      **&#39;V1: single thread algorithm on Dictionary&#39;** |  **1000** |       **661.5 μs** |  **1.00** |               **1.0000** |           **0.0010** |     **1.6 MB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; |  1000 |     2,053.9 μs |  3.10 |              59.0000 |                - |    1.48 MB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; |  1000 |       746.8 μs |  1.13 |              59.0000 |           0.0088 |     1.5 MB |
|                                                  |       |                |       |                      |                  |            |
|      **&#39;V1: single thread algorithm on Dictionary&#39;** |  **2500** |     **5,156.3 μs** |  **1.00** |               **1.0000** |                **-** |   **10.95 MB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; |  2500 |    13,927.5 μs |  2.55 |              59.0000 |                - |   10.64 MB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; |  2500 |     5,190.5 μs |  0.96 |              59.0000 |                - |   10.72 MB |
|                                                  |       |                |       |                      |                  |            |
|      **&#39;V1: single thread algorithm on Dictionary&#39;** |  **5000** |    **23,710.2 μs** |  **1.00** |               **1.0000** |                **-** |   **42.68 MB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; |  5000 |    49,084.5 μs |  2.08 |              59.0000 |                - |   42.03 MB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; |  5000 |    19,846.0 μs |  0.84 |              59.0000 |                - |   42.19 MB |
|                                                  |       |                |       |                      |                  |            |
|      **&#39;V1: single thread algorithm on Dictionary&#39;** | **10000** |    **57,550.0 μs** |  **1.00** |               **1.0000** |                **-** |  **168.28 MB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; | 10000 |   193,388.9 μs |  3.36 |              59.0000 |                - |  166.97 MB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; | 10000 |    63,795.9 μs |  1.11 |              59.0000 |                - |  167.29 MB |
|                                                  |       |                |       |                      |                  |            |
|      **&#39;V1: single thread algorithm on Dictionary&#39;** | **50000** |   **947,925.7 μs** |  **1.00** |               **1.0000** |                **-** | **3012.49 MB** |
| &#39;V2(smallest by memory): multi thread algorithm&#39; | 50000 | 4,590,753.2 μs |  4.84 |              59.0000 |                - | 3006.37 MB |
|       &#39;V3 (compromise): multi thread algorithm)&#39; | 50000 | 1,041,195.0 μs |  1.10 |              59.0000 |                - | 3007.87 MB |

