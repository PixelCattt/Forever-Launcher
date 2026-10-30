using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#nullable disable
namespace ForeverLauncher
{
  internal sealed class TargetBytesChangedException : Exception
  {
    public TargetBytesChangedException()
      : base("The target data changed before the update could be written.")
    {
    }
  }

  internal static class NativeMethods
  {
    private const uint MEM_COMMIT = 4096;
    private const uint MEM_PRIVATE = 131072;
    private const uint PAGE_READWRITE = 4;
    private const uint PROCESS_QUERY_INFORMATION = 1024;
    private const uint PROCESS_VM_READ = 16;
    private const uint PROCESS_VM_WRITE = 32;
    private const uint PROCESS_VM_OPERATION = 8;
    public static long LastBytesScanned;
    public static int LastRegionsScanned;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
      IntPtr p,
      IntPtr a,
      byte[] d,
      UIntPtr n,
      out UIntPtr r);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
      IntPtr p,
      IntPtr a,
      byte[] d,
      UIntPtr n,
      out UIntPtr w);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr VirtualQueryEx(
      IntPtr p,
      IntPtr a,
      out NativeMethods.Region r,
      UIntPtr n);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr p);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
      IntPtr table,
      ref int size,
      bool sort,
      int ipVersion,
      int tableClass,
      uint reserved);

    public static HashSet<string> TcpConnections(int pid)
    {
      HashSet<string> stringSet = new HashSet<string>();
      int size = 0;
      int extendedTcpTable = (int) NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref size, false, 2, 4, 0U);
      if (size <= 0)
        return stringSet;
      IntPtr num1 = Marshal.AllocHGlobal(size);
      try
      {
        if (NativeMethods.GetExtendedTcpTable(num1, ref size, false, 2, 4, 0U) != 0U)
          return stringSet;
        int num2 = Marshal.ReadInt32(num1);
        for (int index = 0; index < num2; ++index)
        {
          IntPtr ptr = num1 + 4 + index * 24;
          if (Marshal.ReadInt32(ptr, 20) == pid)
          {
            uint num3 = (uint) Marshal.ReadInt32(ptr, 12);
            int num4 = (int) Marshal.ReadByte(ptr, 16) << 8 | (int) Marshal.ReadByte(ptr, 17);
            int num5 = (int) Marshal.ReadByte(ptr, 8) << 8 | (int) Marshal.ReadByte(ptr, 9);
            stringSet.Add(string.Format("{0:X8}:{1}/{2}", (object) num3, (object) num4, (object) num5));
          }
        }
      }
      finally
      {
        Marshal.FreeHGlobal(num1);
      }
      return stringSet;
    }

    private static IntPtr Open(int pid, bool forWrite)
    {
      IntPtr num = NativeMethods.OpenProcess((uint) (1040 | (forWrite ? 40 : 0)), false, pid);
      return !(num == IntPtr.Zero) ? num : throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcess");
    }

    private static NativeMethods.Region Query(IntPtr p, long a)
    {
      NativeMethods.Region r;
      if (NativeMethods.VirtualQueryEx(p, new IntPtr(a), out r, new UIntPtr((uint) Marshal.SizeOf(typeof (NativeMethods.Region)))) == UIntPtr.Zero)
        throw new Win32Exception(Marshal.GetLastWin32Error(), "VirtualQueryEx");
      return r;
    }

    private static bool IsHeap(NativeMethods.Region r)
    {
      return r.State == 4096U && r.Type == 131072U && r.Protect == 4U;
    }

    private static byte[] Read(IntPtr p, long a, int n)
    {
      byte[] d = new byte[n];
      UIntPtr r;
      if (!NativeMethods.ReadProcessMemory(p, new IntPtr(a), d, new UIntPtr((uint) n), out r) || (long) r.ToUInt64() != (long) n)
        throw new Win32Exception(Marshal.GetLastWin32Error(), "ReadProcessMemory @0x" + a.ToString("X"));
      return d;
    }

    public static long[] Scan(int pid, byte[] needle, int step = 4)
    {
      return NativeMethods.ScanAny(pid, new byte[1][]{ needle }, step).Select<KeyValuePair<long, int>, long>((Func<KeyValuePair<long, int>, long>) (h => h.Key)).ToArray<long>();
    }

    public static List<KeyValuePair<long, int>> ScanAny(int pid, byte[][] needles, int step)
    {
      int num1 = ((IEnumerable<byte[]>) needles).Max<byte[]>((Func<byte[], int>) (nd => nd.Length));
      if (((IEnumerable<byte[]>) needles).Any<byte[]>((Func<byte[], bool>) (nd => nd.Length < 8)))
        throw new ArgumentException("needle shorter than 8 bytes");
      Dictionary<uint, List<int[]>> probes = new Dictionary<uint, List<int[]>>();
      bool[] filter = new bool[65536];
      for (int index = 0; index < needles.Length; ++index)
      {
        for (int startIndex = 0; startIndex < 4; startIndex += step)
        {
          uint uint32 = BitConverter.ToUInt32(needles[index], startIndex);
          List<int[]> numArrayList;
          if (!probes.TryGetValue(uint32, out numArrayList))
            probes[uint32] = numArrayList = new List<int[]>();
          numArrayList.Add(new int[2]{ index, startIndex });
          filter[(int) (uint32 & (uint) ushort.MaxValue)] = true;
        }
      }
      int overlap = (num1 + 3) / 4 * 4;
      IntPtr p = NativeMethods.Open(pid, false);
      try
      {
        List<long[]> source = new List<long[]>();
        NativeMethods.Region r1;
        long int64;
        long uint64_1;
        for (long index = 0; index < 140737488289792L && !(NativeMethods.VirtualQueryEx(p, new IntPtr(index), out r1, new UIntPtr((uint) Marshal.SizeOf(typeof (NativeMethods.Region)))) == UIntPtr.Zero); index = int64 + uint64_1)
        {
          int64 = r1.BaseAddress.ToInt64();
          uint64_1 = (long) r1.RegionSize.ToUInt64();
          if (uint64_1 > 0L)
          {
            if (NativeMethods.IsHeap(r1))
              source.Add(new long[2]{ int64, uint64_1 });
            if (int64 + uint64_1 <= index)
              break;
          }
          else
            break;
        }
        List<KeyValuePair<long, int>> hits = new List<KeyValuePair<long, int>>();
        long total = 0;
        Parallel.ForEach<long[], byte[]>((IEnumerable<long[]>) source, (Func<byte[]>) (() => new byte[4194304]), (Func<long[], ParallelLoopState, byte[], byte[]>) ((reg, state, buf) =>
        {
          long num2 = reg[0];
          long num3 = reg[1];
          long num4 = 0;
          long num5 = 0;
          for (; num4 < num3; num4 += (long) (4194304 - overlap))
          {
            int num6 = (int) Math.Min(4194304L, num3 - num4);
            UIntPtr r2;
            if (NativeMethods.ReadProcessMemory(p, new IntPtr(num2 + num4), buf, new UIntPtr((uint) num6), out r2) && r2.ToUInt64() > 0UL)
            {
              int uint64_2 = (int) r2.ToUInt64();
              num5 += (long) uint64_2;
              int num7 = num6 < 4194304 ? uint64_2 : 4194304 - overlap;
              for (int startIndex = 0; startIndex + 4 <= uint64_2; startIndex += 4)
              {
                List<int[]> numArrayList;
                if (filter[(int) buf[startIndex] | (int) buf[startIndex + 1] << 8] && probes.TryGetValue(BitConverter.ToUInt32(buf, startIndex), out numArrayList))
                {
                  foreach (int[] numArray in numArrayList)
                  {
                    byte[] needle = needles[numArray[0]];
                    int num8 = startIndex - numArray[1];
                    if (num8 >= 0 && num8 < num7 && num8 + needle.Length <= uint64_2)
                    {
                      int index = 0;
                      while (index < needle.Length && (int) buf[num8 + index] == (int) needle[index])
                        ++index;
                      if (index == needle.Length)
                      {
                        lock (hits)
                          hits.Add(new KeyValuePair<long, int>(num2 + num4 + (long) num8, numArray[0]));
                      }
                    }
                  }
                }
              }
            }
            if (num6 < 4194304)
              break;
          }
          Interlocked.Add(ref total, num5);
          return buf;
        }), (Action<byte[]>) (buf => { }));
        NativeMethods.LastBytesScanned = total;
        NativeMethods.LastRegionsScanned = source.Count;
        hits.Sort((Comparison<KeyValuePair<long, int>>) ((x, y) => x.Key.CompareTo(y.Key)));
        return hits;
      }
      finally
      {
        NativeMethods.CloseHandle(p);
      }
    }

    public static byte[] ReadHeapBlock(int pid, long a, int n)
    {
      IntPtr p = NativeMethods.Open(pid, false);
      try
      {
        NativeMethods.Region r = NativeMethods.Query(p, a);
        long num = r.BaseAddress.ToInt64() + (long) r.RegionSize.ToUInt64();
        return !NativeMethods.IsHeap(r) || a < r.BaseAddress.ToInt64() || a + (long) n > num ? (byte[]) null : NativeMethods.Read(p, a, n);
      }
      finally
      {
        NativeMethods.CloseHandle(p);
      }
    }

    public static void Write32(int pid, long a, byte[] before, byte[] after)
    {
      NativeMethods.WriteHeap(pid, a, before, after);
    }

    public static void WriteHeap(int pid, long a, byte[] before, byte[] after)
    {
      int length = after.Length;
      if (before.Length != length)
        throw new ArgumentException("before/after length");
      IntPtr p = NativeMethods.Open(pid, true);
      try
      {
        NativeMethods.Region r = NativeMethods.Query(p, a);
        long num = r.BaseAddress.ToInt64() + (long) r.RegionSize.ToUInt64();
        if (!NativeMethods.IsHeap(r) || a + (long) length > num)
          throw new Exception("ABORT: not in MEM_PRIVATE+PAGE_READWRITE heap");
        byte[] numArray1 = NativeMethods.Read(p, a, length);
        for (int index = 0; index < length; ++index)
        {
          if ((int) numArray1[index] != (int) before[index])
            throw new TargetBytesChangedException();
        }
        UIntPtr w;
        if (!NativeMethods.WriteProcessMemory(p, new IntPtr(a), after, new UIntPtr((uint) length), out w) || (long) w.ToUInt64() != (long) length)
          throw new Win32Exception(Marshal.GetLastWin32Error(), "WriteProcessMemory");
        byte[] numArray2 = NativeMethods.Read(p, a, length);
        for (int index = 0; index < length; ++index)
        {
          if ((int) numArray2[index] != (int) after[index])
            throw new Exception("read-back verification failed");
        }
      }
      finally
      {
        NativeMethods.CloseHandle(p);
      }
    }

    private struct Region
    {
      public IntPtr BaseAddress;
      public IntPtr AllocationBase;
      public uint AllocationProtect;
      public UIntPtr RegionSize;
      public uint State;
      public uint Protect;
      public uint Type;
    }
  }
}
