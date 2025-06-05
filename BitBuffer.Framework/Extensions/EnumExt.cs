using System;
using System.Runtime.CompilerServices;

namespace BitBuffer.Framework.Extensions;

public static class EnumExt
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static unsafe bool Has<TEnum>(this TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
  {
    return sizeof(TEnum) switch
    {
      1 => (*(byte*)(&lhs) & *(byte*)(&rhs)) > 0,
      2 => (*(ushort*)(&lhs) & *(ushort*)(&rhs)) > 0,
      4 => (*(uint*)(&lhs) & *(uint*)(&rhs)) > 0,
      8 => (*(ulong*)(&lhs) & *(ulong*)(&rhs)) > 0,
      _ => throw new Exception("Size does not match a known Enum backing type."),
    };
  }
}
