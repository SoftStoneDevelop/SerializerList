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

|   Method |   Size |             Mean |   Allocated |
|--------- |------- |-----------------:|------------:|
| **DeepCopy** |    **100** |         **22.08 μs** |    **18.52 KB** |
| **DeepCopy** |   **1000** |        **383.50 μs** |   **164.59 KB** |
| **DeepCopy** |  **10000** |     **26,451.36 μs** |  **2124.37 KB** |
| **DeepCopy** | **100000** |  **2,566,517.79 μs** |  **18679.8 KB** |
| **DeepCopy** | **250000** | **15,806,007.91 μs** | **41576.67 KB** |
| **DeepCopy** | **350000** | **31,161,903.55 μs** | **70494.11 KB** |

V3 List parallelization:

|   Method |   Size |            Mean |   Allocated |
|--------- |------- |----------------:|------------:|
| **DeepCopy** |    **100** |        **24.60 μs** |    **18.52 KB** |
| **DeepCopy** |   **1000** |       **189.43 μs** |   **176.54 KB** |
| **DeepCopy** |  **10000** |     **3,423.31 μs** |  **2136.36 KB** |
| **DeepCopy** | **100000** |   **129,094.19 μs** | **18693.12 KB** |
| **DeepCopy** | **250000** |   **655,246.85 μs** | **57976.34 KB** |
| **DeepCopy** | **350000** | **1,262,112.45 μs** | **70509.88 KB** |
