# ArrayPool
[日本語](README_ja.md)

## Summary
This library `ArrayPool` provides fast array cache processing.  
It is more limited than the .Net standard `System.Buffers.ArrayPool`, but it is faster.  

## System Requirements
|  Environment  |  Version  |
| ---- | ---- |
| Unity | 2021.3.15f1, 2022.2.0f1 |
| .Net | 4.x, Standard 2.1 |

## Performance
### Measurement code on the editor
[Test Code.](packages/Tests/Runtime/ArrayPoolPerformanceTest.cs) 

#### Result
|  Process  |  Time  |
| ---- | ---- |
| Get_Legacy | 0.4604 ms |
| Get_Fast | 0.2526 ms |
| Get_ConcurrentPool | 0.37965 ms |
| Get_ThreadStaticPool | 0.2832 ms |
| Return_Legacy | 4.47095 ms |
| Return_Fast | 0.21675 ms |
| Return_ConcurrentPool | 0.59515 ms |
| Return_ThreadStaticPool | 0.20135 ms |

Get shows more than 2X performance improvement and Return shows more than 20X performance improvement.  

### Measurement code on the runtime
```.cs
private readonly ref struct Measure
{
    private readonly string _label;
    private readonly StringBuilder _builder;
    private readonly float _time;

    public Measure(string label, StringBuilder builder)
    {
        _label = label;
        _builder = builder;
        _time = (Time.realtimeSinceStartup * 1000);
    }

    public void Dispose()
    {
        _builder.AppendLine($"{_label}: {(Time.realtimeSinceStartup * 1000) - _time} ms");
    }
}
：
var log = new StringBuilder();
Stack<int[]> arrs = new Stack<int[]>(CacheCount);
using (new Measure("Get_Legacy", log))
{
    for (int i = 0; i < CacheCount; ++i)
    {
        arrs.Push(ArrayPool<int>.Shared.Rent(32));
    }
}
using (new Measure("Return_Legacy", log))
{
    foreach (var arr in arrs)
    {
        ArrayPool<int>.Shared.Return(arr);
    }
}
:
```
#### Result
|  Process  |  Mono  |  IL2CPP  |
| ---- | ---- | ---- |
| Get_Legacy | 10.59292 ms | 43.74219 ms |
| Get_Fast | 0.3275452 ms | 0.1464844 ms |
| Get_ConcurrentPool | 0.3051529 ms | 0.1708984 ms |
| Get_ThreadStaticPool | 0.296154 ms | 0.3076172 ms |
| Return_Legacy | 4.778381 ms | 37.94922 ms |
| Return_Fast | 0.1763153 ms | 0.1230469 ms |
| Return_ConcurrentPool | 0.5840683 ms | 0.8242188 ms |
| Return_ThreadStaticPool | 0.1847572 ms | 0.1679688 ms |

The IL2CPP environment is nearly 400X faster.

## How to install
### Install dependent packages
Install the following packages  

- [GenericEnhance v1.0.3](https://github.com/Katsuya100/GenericEnhance/tree/v1.0.3)
- 
### Installing ArrayPool
1. Open [Window > Package Manager].
2. click [+ > Add package from git url...].
3. Type `https://github.com/Katsuya100/ArrayPool.git?path=packages` and click [Add].

#### If it doesn't work
The above method may not work well in environments where git is not installed.  
Download the appropriate version of `com.katuusagi.arraypool.tgz` from [Releases](https://github.com/Katsuya100/ArrayPool/releases), and then [Package Manager > + > Add package from tarball...] Use [Package Manager > + > Add package from tarball...] to install the package.

#### If it still doesn't work
Download the appropriate version of `Katuusagi.ArrayPool.unitypackage` from [Releases](https://github.com/Katsuya100/ArrayPool/releases) and Import it into your project from [Assets > Import Package > Custom Package].

## How to Use
### Normal usage
You can use ArrayPool with the following notation.  
When using ArrayPool, please return an array if possible.  
If not returned, the cache in the Pool will be reduced, which may lead to performance degradation.  
```.cs
int[] arr = ArrayPool<int, _32>.Get();
Debug.Log(arr.Length); // 32
ArrayPool<int, _32>.TryReturn(arr);
```

If it is troublesome to return, the following notation is also valid.  
```.cs
using(ArrayPool<int, _32>.Get(out var arr))
{
    Debug.Log(arr.Length); // 32
}
```

### If you want to support multi-threading
Use `ConcurrentArrayPool` if you want to use it in a multi-threaded environment.  
```.cs
int[] arr = ConcurrentArrayPool<int, _32>.Get();
```
The `ConcurrentArrayPool` has a unique Pool different from the `ArrayPool`.  
This allows it to be used in multi-threaded environments.  
However, it has performance issues compared to `ArrayPool`.  
Specifically, allocation occurs during `TryReturn`.  
I plan to improve this in a later update.  

#### ThreadStatic pools
Using `ThreadStaticArrayPool` allows for multi-threading without performance loss.  
```.cs
int[] arr = ThreadStaticArrayPool<int, _32>.Get();
```
This may consume more memory than `ConcurrentArrayPool` because it uses a different pool for each Thread.
Also, be careful not to return the Get array in a different thread.  
The return will be completed successfully, but will be returned in a different pool than the pool from which it was retrieved.  

### Cache Creation
You can create a cache in advance by calling the `MakeCache` method.  
Creating a cache in advance allows you to determine the cache size and suppress allocation on the first run.  
```.cs
ArrayPool<int, _32>.MakeCache(32);
```

## Reasons for high performance
`System.Buffers.ArrayPool` allows you to specify the array length as a variable, but this incurs an overhead to get the Pool.  
`Katuusagi.Pool.ArrayPool` allows the array length to be specified as a constant from a generic argument.  
Therefore, it can be retrieved quickly using `Static Type Caching`.  
The first time access to the cache, an allocation to build the cache is performed, but if the cache is built in advance, this allocation will be zero.  

Since the `MethodImpl` attribute is set to `AggressiveInline`, you can also expect optimization by inline expansion at build time.

The above techniques provide overwhelming performance compared to `System.Buffers.ArrayPool`.  
