using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser] // reports Gen0/allocs too
public class ParamPassingBench
{
    // Pre-created instances so allocs don't distort results
    private PointClass _class;
    private PointStruct _struct;
    private PointRecord _recordClass;
    private PointRecordStruct _recordStruct;

    [GlobalSetup]
    public void Setup()
    {
        // Some arbitrary but deterministic values
        var v = new int[16];
        for (int i = 0; i < v.Length; i++) v[i] = i + 1;

        _class = PointClass.FromArray(v);
        _struct = PointStruct.FromArray(v);
        _recordClass = PointRecord.FromArray(v);
        _recordStruct = PointRecordStruct.FromArray(v);
    }

    // ---- Benchmarks: passing each shape by value ----
    [Benchmark] public int ClassParam() => Consume(_class);
    [Benchmark] public int StructParam() => Consume(_struct);
    [Benchmark] public int RecordClassParam() => Consume(_recordClass);
    [Benchmark] public int RecordStructParam() => Consume(_recordStruct);

    // ---- Benchmarks: 'in' (readonly by-ref) to avoid struct copies ----
    [Benchmark] public int StructParam_In() => ConsumeIn(in _struct);
    [Benchmark] public int RecordStructParam_In() => ConsumeIn(in _recordStruct);

    // Prevent inlining so we actually measure parameter passing
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Consume(PointClass p) => p.Sum();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Consume(PointStruct p) => p.Sum();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Consume(PointRecord p) => p.Sum();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Consume(PointRecordStruct p) => p.Sum();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeIn(in PointStruct p) => p.Sum();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeIn(in PointRecordStruct p) => p.Sum();
}

public sealed class PointClass
{
    public int A01, A02, A03, A04, A05, A06, A07, A08,
               A09, A10, A11, A12, A13, A14, A15, A16;

    public static PointClass FromArray(int[] v) => new PointClass
    {
        A01=v[0],  A02=v[1],  A03=v[2],  A04=v[3],
        A05=v[4],  A06=v[5],  A07=v[6],  A08=v[7],
        A09=v[8],  A10=v[9],  A11=v[10], A12=v[11],
        A13=v[12], A14=v[13], A15=v[14], A16=v[15]
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Sum() =>
        A01+A02+A03+A04+A05+A06+A07+A08+A09+A10+A11+A12+A13+A14+A15+A16;
}

public struct PointStruct
{
    public int A01, A02, A03, A04, A05, A06, A07, A08,
               A09, A10, A11, A12, A13, A14, A15, A16;

    public static PointStruct FromArray(int[] v) => new PointStruct
    {
        A01=v[0],  A02=v[1],  A03=v[2],  A04=v[3],
        A05=v[4],  A06=v[5],  A07=v[6],  A08=v[7],
        A09=v[8],  A10=v[9],  A11=v[10], A12=v[11],
        A13=v[12], A14=v[13], A15=v[14], A16=v[15]
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int Sum() =>
        A01+A02+A03+A04+A05+A06+A07+A08+A09+A10+A11+A12+A13+A14+A15+A16;
}

public record PointRecord(
    int A01, int A02, int A03, int A04, int A05, int A06, int A07, int A08,
    int A09, int A10, int A11, int A12, int A13, int A14, int A15, int A16)
{
    public static PointRecord FromArray(int[] v) =>
        new PointRecord(v[0],v[1],v[2],v[3],v[4],v[5],v[6],v[7],
                        v[8],v[9],v[10],v[11],v[12],v[13],v[14],v[15]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Sum() =>
        A01+A02+A03+A04+A05+A06+A07+A08+A09+A10+A11+A12+A13+A14+A15+A16;
}

// Positional 'record struct' is a value type
public readonly record struct PointRecordStruct(
    int A01, int A02, int A03, int A04, int A05, int A06, int A07, int A08,
    int A09, int A10, int A11, int A12, int A13, int A14, int A15, int A16)
{
    public static PointRecordStruct FromArray(int[] v) =>
        new PointRecordStruct(v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7],
                              v[8], v[9], v[10], v[11], v[12], v[13], v[14], v[15]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Sum() =>
        A01 + A02 + A03 + A04 + A05 + A06 + A07 + A08 + A09 + A10 + A11 + A12 + A13 + A14 + A15 + A16;
}

[MemoryDiagnoser]
[GcForce] // GC between iterations for cleaner signals
public class GcImpactBench
{
    // Adjust N to make Gen0 collections visible on your machine
    [Params(10_000, 100_000)]
    public int N;

    // 64-byte payload (16 * 4B) magnifies value-type copy cost,
    // and makes array size large enough to hit LOH at higher N.
    public sealed class PayloadClass : IPayload
    {
        public int A01, A02, A03, A04, A05, A06, A07, A08,
                   A09, A10, A11, A12, A13, A14, A15, A16;

        public PayloadClass(int seed)
        {
            A01=seed+1;  A02=seed+2;  A03=seed+3;  A04=seed+4;
            A05=seed+5;  A06=seed+6;  A07=seed+7;  A08=seed+8;
            A09=seed+9;  A10=seed+10; A11=seed+11; A12=seed+12;
            A13=seed+13; A14=seed+14; A15=seed+15; A16=seed+16;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Sum() =>
            A01+A02+A03+A04+A05+A06+A07+A08+A09+A10+A11+A12+A13+A14+A15+A16;
    }

    public readonly record struct PayloadRecordStruct(
        int A01, int A02, int A03, int A04, int A05, int A06, int A07, int A08,
        int A09, int A10, int A11, int A12, int A13, int A14, int A15, int A16
    ) : IPayload
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Sum() =>
            A01+A02+A03+A04+A05+A06+A07+A08+A09+A10+A11+A12+A13+A14+A15+A16;
    }

    public record PayloadRecordClass(
        int A01, int A02, int A03, int A04, int A05, int A06, int A07, int A08,
        int A09, int A10, int A11, int A12, int A13, int A14, int A15, int A16
    ) : IPayload
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Sum() =>
            A01+A02+A03+A04+A05+A06+A07+A08+A09+A10+A11+A12+A13+A14+A15+A16;
    }

    public struct PayloadStruct : IPayload
    {
        public int A01, A02, A03, A04, A05, A06, A07, A08,
                   A09, A10, A11, A12, A13, A14, A15, A16;

        public PayloadStruct(int seed)
        {
            A01=seed+1;  A02=seed+2;  A03=seed+3;  A04=seed+4;
            A05=seed+5;  A06=seed+6;  A07=seed+7;  A08=seed+8;
            A09=seed+9;  A10=seed+10; A11=seed+11; A12=seed+12;
            A13=seed+13; A14=seed+14; A15=seed+15; A16=seed+16;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int Sum() =>
            A01+A02+A03+A04+A05+A06+A07+A08+A09+A10+A11+A12+A13+A14+A15+A16;
    }

    public interface IPayload { int Sum(); }

    // ------------------------------------------------------------
    // 1) N separate heap objects (class / record class)
    // ------------------------------------------------------------

    [Benchmark(Description = "N x new class")]
    public int Allocate_N_Classes()
    {
        var arr = new PayloadClass[N]; // array of references
        for (int i = 0; i < N; i++)
            arr[i] = new PayloadClass(i); // N separate objects -> GC tracks N objects + 1 for the array
        return ConsumeArray(arr);
    }

    [Benchmark(Description = "N x new record class")]
    public int Allocate_N_RecordClasses()
    {
        var arr = new PayloadRecordClass[N];
        for (int i = 0; i < N; i++)
            arr[i] = new PayloadRecordClass(i+1,i+2,i+3,i+4,i+5,i+6,i+7,i+8,
                                            i+9,i+10,i+11,i+12,i+13,i+14,i+15,i+16);
        return ConsumeArray(arr);
    }

    // ------------------------------------------------------------
    // 2) One heap object containing N inlined structs (no boxing)
    //    Notice: only the *array* is a heap object; the structs are inlined.
    // ------------------------------------------------------------

    [Benchmark(Description = "Array<struct> with N elements (no boxing)")]
    public int ArrayOfStructs_NoBoxing()
    {
        var arr = new PayloadStruct[N]; // single heap object (the array)
        for (int i = 0; i < N; i++)
            arr[i] = new PayloadStruct(i);
        return ConsumeArray(arr);
    }

    [Benchmark(Description = "Array<record struct> with N elements (no boxing)")]
    public int ArrayOfRecordStructs_NoBoxing()
    {
        var arr = new PayloadRecordStruct[N];
        for (int i = 0; i < N; i++)
            arr[i] = new PayloadRecordStruct(i+1,i+2,i+3,i+4,i+5,i+6,i+7,i+8,
                                             i+9,i+10,i+11,i+12,i+13,i+14,i+15,i+16);
        return ConsumeArray(arr);
    }

    // ------------------------------------------------------------
    // 3) Boxing: storing structs behind an interface/object causes N heap allocs
    //    (each boxed struct is its own object)
    // ------------------------------------------------------------

    [Benchmark(Description = "IPayload[] filled with structs (BOXING)")]
    public int InterfaceArray_BoxedStructs()
    {
        IPayload[] arr = new IPayload[N]; // one array + N boxed structs
        for (int i = 0; i < N; i++)
            arr[i] = new PayloadStruct(i); // boxing occurs here
        return ConsumeArray(arr);
    }

    [Benchmark(Description = "object[] filled with structs (BOXING)")]
    public int ObjectArray_BoxedStructs()
    {
        object[] arr = new object[N]; // one array + N boxed structs
        for (int i = 0; i < N; i++)
            arr[i] = new PayloadStruct(i); // boxing occurs here
        return ConsumeObjectArray(arr);
    }

    // ------------------------------------------------------------
    // Consumers to prevent dead-code elimination
    // ------------------------------------------------------------
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeArray<T>(T[] arr) where T : IPayload
    {
        int sum = 0;
        for (int i = 0; i < arr.Length; i++) sum += arr[i].Sum();
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeArray(PayloadClass[] arr)
    {
        int sum = 0;
        for (int i = 0; i < arr.Length; i++) sum += arr[i].Sum();
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeArray(PayloadRecordClass[] arr)
    {
        int sum = 0;
        for (int i = 0; i < arr.Length; i++) sum += arr[i].Sum();
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeArray(PayloadStruct[] arr)
    {
        int sum = 0;
        for (int i = 0; i < arr.Length; i++) sum += arr[i].Sum();
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeArray(PayloadRecordStruct[] arr)
    {
        int sum = 0;
        for (int i = 0; i < arr.Length; i++) sum += arr[i].Sum();
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeObjectArray(object[] arr)
    {
        int sum = 0;
        for (int i = 0; i < arr.Length; i++)
            sum += ((IPayload)arr[i]).Sum(); // safe for this benchmark
        return sum;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(new[]
        {
            typeof(ParamPassingBench),
            typeof(GcImpactBench)
        });
    }
}
