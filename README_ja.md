# ArrayPool
## 概要
本ライブラリ「ArrayPool」は高速な配列キャッシュ処理を提供します。  
.Net標準の`System.Buffers.ArrayPool`よりも限定的ですが、高速に処理することが可能です。  

## 動作確認環境
|  環境  |  バージョン  |
| ---- | ---- |
| Unity | 2021.3.15f1, 2022.2.0f1 |
| .Net | 4.x, Standard 2.1 |

## 性能
### エディタ上の計測コード
[テストコード](packages/Tests/Runtime/ArrayPoolPerformanceTest.cs) 

#### 結果
|  実行処理  |  処理時間  |
| ---- | ---- |
| Get_Legacy | 0.4604 ms |
| Get_Fast | 0.2526 ms |
| Get_ConcurrentPool | 0.37965 ms |
| Get_ThreadStaticPool | 0.2832 ms |
| Return_Legacy | 4.47095 ms |
| Return_Fast | 0.21675 ms |
| Return_ConcurrentPool | 0.59515 ms |
| Return_ThreadStaticPool | 0.20135 ms |

Getは2倍以上、Returnは20倍以上のパフォーマンス改善が見られます。  

### ビルド後の計測コード
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
#### 結果
|  実行処理  |  Mono  | IL2CPP
| ---- | ---- | ---- |
| Get_Legacy | 10.59292 ms | 43.74219 ms |
| Get_Fast | 0.3275452 ms | 0.1464844 ms |
| Get_ConcurrentPool | 0.3051529 ms | 0.1708984 ms |
| Get_ThreadStaticPool | 0.296154 ms | 0.3076172 ms |
| Return_Legacy | 4.778381 ms | 37.94922 ms |
| Return_Fast | 0.1763153 ms | 0.1230469 ms |
| Return_ConcurrentPool | 0.5840683 ms | 0.8242188 ms |
| Return_ThreadStaticPool | 0.1847572 ms | 0.1679688 ms |

IL2CPP環境では400倍近く高速化されています。

## インストール方法
### 依存パッケージをインストール
以下のパッケージをインストールする。  

- [GenericEnhance v1.0.3](https://github.com/Katsuya100/GenericEnhance/tree/v1.0.3)

### ArrayPoolのインストール
1. [Window > Package Manager]を開く。
2. [+ > Add package from git url...]をクリックする。
3. `https://github.com/Katsuya100/ArrayPool.git?path=packages`と入力し[Add]をクリックする。

#### うまくいかない場合
上記方法は、gitがインストールされていない環境ではうまく動作しない場合があります。
[Releases](https://github.com/Katsuya100/ArrayPool/releases)から該当のバージョンの`com.katuusagi.arraypool.tgz`をダウンロードし
[Package Manager > + > Add package from tarball...]を使ってインストールしてください。

#### それでもうまくいかない場合
[Releases](https://github.com/Katsuya100/ArrayPool/releases)から該当のバージョンの`Katuusagi.ArrayPool.unitypackage`をダウンロードし
[Assets > Import Package > Custom Package]からプロジェクトにインポートしてください。

## 使い方
### 通常の使用法
以下の記法でArrayPoolを使用できます。  
使う際はなるべく配列を返却してください。  
返却されない場合はPool内のキャッシュが減り、パフォーマンス低下に繋がる可能性があります。  
```.cs
int[] arr = ArrayPool<int, _32>.Get();
Debug.Log(arr.Length); // 32
ArrayPool<int, _32>.TryReturn(arr);
```

もし、返却が面倒な場合は下記の記法も有効です。  
```.cs
using(ArrayPool<int, _32>.Get(out var arr))
{
    Debug.Log(arr.Length); // 32
}
```

### マルチスレッドに対応したい場合
マルチスレッド環境で使用したい場合は  
`ConcurrentArrayPool`を使用してください。  
```.cs
int[] arr = ConcurrentArrayPool<int, _32>.Get();
```
`ConcurrentArrayPool`は`ArrayPool`と異なる固有のPoolを持っています。  
これにより、マルチスレッド環境でも使用することが可能です。  
しかし、`ArrayPool`に比べて性能面での課題があります。  
具体的には`TryReturn`時にアロケーションが発生します。  
後のアップデートで改善していく予定です。  

#### ThreadStaticなプール
`ThreadStaticArrayPool`を使用することで  
パフォーマンスを落とさずにマルチスレッド対応が可能です。  
```.cs
int[] arr = ThreadStaticArrayPool<int, _32>.Get();
```
Thread毎に異なるプールを用いるため、`ConcurrentArrayPool`に比べてメモリ消費量が多くなる可能性があります。
また、Getした配列を異なるスレッドで返却しないように注意してください。  
返却は正常に完了しますが取得したプールと異なるプールに返却されてしまいます。  

### キャッシュの作成
`MakeCache`関数を呼び出すことで事前にキャッシュを作成することができます。  
事前にキャッシュを作成しておけばキャッシュサイズを決められる他、初回実行時のアロケーションを抑制することができます。  
```.cs
ArrayPool<int, _32>.MakeCache(32);
```

## 高速な理由
`System.Buffers.ArrayPool`は配列長を変数で指定することができますが、そのためPoolの取得にオーバーヘッドが発生します。  
`Katuusagi.Pool.ArrayPool`は配列長をGeneric引数から定数で指定することができます。  
そのため`Static Type Caching`を用いて高速に取得できます。  
初回アクセス時にキャッシュ構築のアロケーションが走りますが、事前にキャッシュを作っておけばこのアロケーションもゼロになります。  

`MethodImpl`属性で`AggressiveInline`を設定しているため、ビルド時のインライン展開による最適化も期待できます。

以上のテクニックにより`System.Buffers.ArrayPool`に比べ圧倒的なパフォーマンスを実現しています。  
