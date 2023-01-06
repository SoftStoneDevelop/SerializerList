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
| **DeepCopy** |    **100** |         **22.95 μs** |    **19.33 KB** |
| **DeepCopy** |   **1000** |        **337.10 μs** |   **172.44 KB** |
| **DeepCopy** |  **10000** |     **25,067.67 μs** |  **2202.53 KB** |
| **DeepCopy** | **100000** |  **2,341,603.00 μs** |  **19461.1 KB** |
| **DeepCopy** | **250000** | **14,582,359.25 μs** | **43528.88 KB** |
| **DeepCopy** | **350000** | **28,522,511.49 μs** | **73228.55 KB** |

V3 List parallelization:

|   Method |   Size |            Mean |   Allocated |
|--------- |------- |----------------:|------------:|
| **DeepCopy** |    **100** |        **22.46 μs** |    **19.33 KB** |
| **DeepCopy** |   **1000** |       **181.23 μs** |   **184.38 KB** |
| **DeepCopy** |  **10000** |     **3,109.78 μs** |  **2214.52 KB** |
| **DeepCopy** | **100000** |   **121,224.22 μs** | **19474.24 KB** |
| **DeepCopy** | **250000** |   **638,544.03 μs** |  **59929.2 KB** |
| **DeepCopy** | **350000** | **1,225,948.97 μs** | **73243.71 KB** |
