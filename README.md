Given on assignment: Exist interface "IListSerializer" and class "ListNode".<br>
Task: to implement "IListSerializer" with as efficient methods as possible and with minimal memory allocation.

## Results:
V1(fastest):<br>
single thread algorithm
|   Method |   Size |          Mean |    Allocated |
|--------- |------- |--------------:|-------------:|
| **DeepCopy** |    **100** |      **28.15 μs** |     **30.47 KB** |
| **DeepCopy** |   **1000** |     **172.70 μs** |    **294.67 KB** |
| **DeepCopy** |  **10000** |   **2,303.02 μs** |   **3192.76 KB** |
| **DeepCopy** | **100000** |  **42,739.18 μs** |   **28879.3 KB** |
| **DeepCopy** | **250000** | **131,159.76 μs** |  **64046.22 KB** |
| **DeepCopy** | **350000** | **213,744.68 μs** | **112567.06 KB** |

V2(smallest by memory):<br>
multi thread algorithm

|   Method |   Size |             Mean |  Allocated |
|--------- |------- |-----------------:|-----------:|
| **DeepCopy** |    **100** |         **37.29 μs** |   **16.34 KB** |
| **DeepCopy** |   **1000** |      **2,015.70 μs** |  **160.79 KB** |
| **DeepCopy** |  **10000** |    **194,372.64 μs** | **1880.56 KB** |
| **DeepCopy** | **100000** | **19,058,509.61 μs** | **16648.1 KB** |

V3 (compromise):<br>
multi thread algorithm

|   Method |   Size |            Mean |   Allocated |
|--------- |------- |----------------:|------------:|
| **DeepCopy** |    **100** |        **24.50 μs** |    **18.52 KB** |
| **DeepCopy** |   **1000** |       **185.99 μs** |   **176.53 KB** |
| **DeepCopy** |  **10000** |     **3,555.78 μs** |  **2136.36 KB** |
| **DeepCopy** | **100000** |   **129,685.25 μs** |  **18693.2 KB** |
| **DeepCopy** | **250000** |   **657,504.40 μs** |  **57976.4 KB** |
| **DeepCopy** | **350000** | **1,269,452.50 μs** | **70509.81 KB** |
