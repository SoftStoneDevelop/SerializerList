Given on assignment: Exist interface "IListSerializer" and class "ListNode".<br>
Task: to implement "IListSerializer" with as efficient methods as possible and with minimal memory allocation.

## Results:
V1 Dictionary:

|   Method |    Size |          Mean |    Allocated |
|--------- |-------- |--------------:|-------------:|
| **DeepCopy** |     **100** |      **25.24 μs** |     **29.26 KB** |
| **DeepCopy** |    **1000** |     **163.20 μs** |    **272.78 KB** |
| **DeepCopy** |   **10000** |   **2,160.72 μs** |   **2540.21 KB** |
| **DeepCopy** |  **100000** |  **42,029.04 μs** |  **27473.57 KB** |
| **DeepCopy** |  **250000** | **129,741.19 μs** |  **60531.11 KB** |
| **DeepCopy** |  **500000** | **299,268.48 μs** | **122877.36 KB** |
| **DeepCopy** | **1000000** | **790,209.90 μs** | **249510.95 KB** |

V2 List:

|   Method |   Size |            Mean |   Allocated |
|--------- |------- |----------------:|------------:|
| **DeepCopy** |    **100** |        **22.24 μs** |    **15.98 KB** |
| **DeepCopy** |   **1000** |       **783.43 μs** |   **142.36 KB** |
| **DeepCopy** |  **10000** |    **80,752.97 μs** |  **1293.68 KB** |
| **DeepCopy** | **100000** | **7,889,959.73 μs** | **16006.35 KB** |

V3 List parallelization:

|   Method |   Size |             Mean |   Allocated |
|--------- |------- |-----------------:|------------:|
| **DeepCopy** |    **100** |         **24.74 μs** |    **15.99 KB** |
| **DeepCopy** |   **1000** |        **230.72 μs** |   **154.35 KB** |
| **DeepCopy** |  **10000** |      **4,196.01 μs** |  **1817.76 KB** |
| **DeepCopy** | **100000** |    **309,452.94 μs** | **16017.63 KB** |
| **DeepCopy** | **250000** |  **2,456,529.48 μs** | **35933.05 KB** |
| **DeepCopy** | **350000** | **12,158,672.29 μs** | **60129.64 KB** |
