namespace MiniSharpCompiler
{
    using System;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

	public enum MiniSharpSimpleType
	{
		Integral,
		FloatingPoint,
		Pointer
	}

	public static class TypeSystem
	{
        public static readonly LLVMBool True = new LLVMBool(1);

        public static readonly LLVMBool False = new LLVMBool(0);

	    public static LLVMTypeRef Int1Type = LLVM.Int1Type();

		public static LLVMTypeRef Int8Type = LLVM.Int8Type();

		public static LLVMTypeRef SByteType = LLVM.Int8Type();

		public static LLVMTypeRef Int16Type = LLVM.Int16Type();

		public static LLVMTypeRef Int32Type = LLVM.Int32Type();

		public static LLVMTypeRef Int64Type = LLVM.Int64Type();

		public static LLVMTypeRef FloatType = LLVM.FloatType();

		public static LLVMTypeRef DoubleType = LLVM.DoubleType();

		public static LLVMTypeRef DecimalType = LLVM.FP128Type();

		public static LLVMTypeRef NullType = LLVM.PointerType(LLVM.Int8Type(), 0);

		public static LLVMTypeRef PointerType = LLVM.PointerType(LLVM.Int8Type(), 0);

		public static LLVMValueRef Convert(this LLVMValueRef value, LLVMBuilderRef builder, TypeInfo t)
		{
			if (t.Type.Equals(t.ConvertedType))
			{
				return value;
			}

			var convertedType = t.ConvertedType.Convert();

			switch (t.Type.SpecialType)
			{
				case SpecialType.System_IntPtr:
				case SpecialType.System_UIntPtr:
					switch (t.ConvertedType.SpecialType)
					{
						case SpecialType.System_Int32:
						case SpecialType.System_UInt32:
						case SpecialType.System_Int64:
						case SpecialType.System_UInt64:
							return LLVM.BuildPtrToInt(builder, value, convertedType, "ptrtoint");
					}
					break;
				case SpecialType.System_SByte:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_Int16:
						case SpecialType.System_Int32:
						case SpecialType.System_Int64:
							return LLVM.BuildSExt(builder, value, convertedType, "conv");
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildSIToFP(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_Char:
						case SpecialType.System_UInt16:
						case SpecialType.System_UInt32:
						case SpecialType.System_UInt64:
							return LLVM.BuildSExt(builder, value, convertedType, "conv");
						case SpecialType.System_Byte:
							return value;
					}
					break;
				case SpecialType.System_Byte:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_Int16:
						case SpecialType.System_UInt16:
						case SpecialType.System_Int32:
						case SpecialType.System_UInt32:
						case SpecialType.System_Int64:
						case SpecialType.System_UInt64:
							return LLVM.BuildZExt(builder, value, convertedType, "conv");
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildUIToFP(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_Char:
							return LLVM.BuildZExt(builder, value, convertedType, "conv");
						case SpecialType.System_SByte:
							return value;
					}
					break;
				case SpecialType.System_Int16:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_Int32:
						case SpecialType.System_Int64:
							return LLVM.BuildSExt(builder, value, convertedType, "conv");
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildSIToFP(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Byte:
							return LLVM.BuildTrunc(builder, value, convertedType, "trunc");
						case SpecialType.System_UInt32:
						case SpecialType.System_UInt64:
							return LLVM.BuildSExt(builder, value, convertedType, "conv");
						case SpecialType.System_Char:
						case SpecialType.System_UInt16:
							return value;
					}
					break;
				case SpecialType.System_UInt16:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_Int32:
						case SpecialType.System_UInt32:
						case SpecialType.System_Int64:
						case SpecialType.System_UInt64:
							return LLVM.BuildZExt(builder, value, convertedType, "conv");
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildUIToFP(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Byte:
							return LLVM.BuildTrunc(builder, value, convertedType, "trunc");
						case SpecialType.System_Char:
						case SpecialType.System_Int16:
							return value;
					}
					break;
				case SpecialType.System_Int32:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_Int64:
							return LLVM.BuildSExt(builder, value, convertedType, "conv");
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildSIToFP(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Byte:
						case SpecialType.System_Int16:
						case SpecialType.System_UInt16:
						case SpecialType.System_Char:
							return LLVM.BuildTrunc(builder, value, convertedType, "trunc");
						case SpecialType.System_UInt64:
							return LLVM.BuildSExt(builder, value, convertedType, "conv");
						case SpecialType.System_UInt32:
							return value;
					}
					break;
				case SpecialType.System_UInt32:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_Int64:
						case SpecialType.System_UInt64:
							return LLVM.BuildZExt(builder, value, convertedType, "conv");
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildUIToFP(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Byte:
						case SpecialType.System_Int16:
						case SpecialType.System_UInt16:
						case SpecialType.System_Char:
							return LLVM.BuildTrunc(builder, value, convertedType, "trunc");
						case SpecialType.System_Int32:
							return value;
					}
					break;
				case SpecialType.System_Int64:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildSIToFP(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Byte:
						case SpecialType.System_Int16:
						case SpecialType.System_UInt16:
						case SpecialType.System_Int32:
						case SpecialType.System_UInt32:
						case SpecialType.System_Char:
							return LLVM.BuildTrunc(builder, value, convertedType, "trunc");
						case SpecialType.System_UInt64:
							return value;
					}
					break;
				case SpecialType.System_UInt64:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildUIToFP(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Byte:
						case SpecialType.System_Int16:
						case SpecialType.System_UInt16:
						case SpecialType.System_Int32:
						case SpecialType.System_UInt32:
						case SpecialType.System_Char:
							return LLVM.BuildTrunc(builder, value, convertedType, "trunc");
						case SpecialType.System_Int64:
							return value;
					}
					break;
				case SpecialType.System_Char:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_UInt16:
							return value;
						case SpecialType.System_Int32:
						case SpecialType.System_UInt32:
						case SpecialType.System_Int64:
						case SpecialType.System_UInt64:
							return LLVM.BuildZExt(builder, value, convertedType, "conv");
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildUIToFP(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Byte:
							return LLVM.BuildTrunc(builder, value, convertedType, "trunc");
						case SpecialType.System_Int16:
							return value;
					}
					break;
				case SpecialType.System_Single:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.1.2 Implicit numeric conversions
						case SpecialType.System_Double:
							return LLVM.BuildFPExt(builder, value, convertedType, "conv");
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Int16:
						case SpecialType.System_Int32:
						case SpecialType.System_Int64:
							return LLVM.BuildFPToSI(builder, value, convertedType, "conv");
						case SpecialType.System_Byte:
						case SpecialType.System_UInt16:
						case SpecialType.System_Char:
						case SpecialType.System_UInt32:
						case SpecialType.System_UInt64:
							return LLVM.BuildFPToUI(builder, value, convertedType, "conv");
						case SpecialType.System_Decimal:
							return LLVM.BuildFPExt(builder, value, convertedType, "conv");
					}
					break;
				case SpecialType.System_Double:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Int16:
						case SpecialType.System_Int32:
						case SpecialType.System_Int64:
							return LLVM.BuildFPToSI(builder, value, convertedType, "conv");
						case SpecialType.System_Byte:
						case SpecialType.System_UInt16:
						case SpecialType.System_Char:
						case SpecialType.System_UInt32:
						case SpecialType.System_UInt64:
							return LLVM.BuildFPToUI(builder, value, convertedType, "conv");
						case SpecialType.System_Single:
							return LLVM.BuildFPTrunc(builder, value, convertedType, "conv");
						case SpecialType.System_Decimal:
							return LLVM.BuildFPExt(builder, value, convertedType, "conv");
					}
					break;
				case SpecialType.System_Decimal:
					switch (t.ConvertedType.SpecialType)
					{
						// 6.2.1 Explicit numeric conversions
						case SpecialType.System_SByte:
						case SpecialType.System_Int16:
						case SpecialType.System_Int32:
						case SpecialType.System_Int64:
							return LLVM.BuildFPToSI(builder, value, convertedType, "conv");
						case SpecialType.System_Byte:
						case SpecialType.System_UInt16:
						case SpecialType.System_Char:
						case SpecialType.System_UInt32:
						case SpecialType.System_UInt64:
							return LLVM.BuildFPToUI(builder, value, convertedType, "conv");
						case SpecialType.System_Single:
						case SpecialType.System_Double:
						case SpecialType.System_Decimal:
							return LLVM.BuildFPExt(builder, value, convertedType, "conv");
					}
					break;
				default:
					throw new Exception("Unreachable");
			}

			throw new Exception("Unreachable");
		}

		public static MiniSharpSimpleType ToMiniSharpSimpleType(this ITypeSymbol type)
		{
			switch (type.SpecialType)
			{
				case SpecialType.System_SByte:
				case SpecialType.System_Byte:
				case SpecialType.System_Int16:
				case SpecialType.System_UInt16:
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
				case SpecialType.System_Char:
					return MiniSharpSimpleType.Integral;
				case SpecialType.System_Single:
				case SpecialType.System_Double:
				case SpecialType.System_Decimal:
					return MiniSharpSimpleType.FloatingPoint;
				case SpecialType.System_IntPtr:
				case SpecialType.System_String:
				case SpecialType.System_Object:
				case SpecialType.System_Array:
					return MiniSharpSimpleType.Pointer;
				default:
					throw new Exception("Unreachable");
			}
		}

		public static bool IsIntegralType(this ITypeSymbol type)
		{
			switch (type.SpecialType)
			{
				case SpecialType.System_SByte:
				case SpecialType.System_Byte:
				case SpecialType.System_Int16:
				case SpecialType.System_UInt16:
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
				case SpecialType.System_Char:
					return true;
				default:
					return false;
			}
		}

		public static bool IsIntegerType(this LLVMTypeRef type)
		{
			var ptr = type.Pointer;
			if (ptr == Int32Type.Pointer || ptr == Int16Type.Pointer || ptr == Int64Type.Pointer)
			{
				return true;
			}

			return false;
		}

		public static bool IsFloatType(this LLVMTypeRef type)
		{
			var ptr = type.Pointer;
			if (ptr == FloatType.Pointer || ptr == DoubleType.Pointer || ptr == DecimalType.Pointer)
			{
				return true;
			}

			return false;
		}

		public static LLVMBool IsSignExtended(this TypeInfo t)
		{
			return t.ConvertedType.SpecialType.IsSignExtended();
		}

		public static LLVMBool IsSignExtended(this SpecialType t)
		{
			switch (t)
			{
				case SpecialType.System_SByte:
				case SpecialType.System_Int16:
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
					return True;
				case SpecialType.System_Byte:
				case SpecialType.System_UInt16:
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
					return False;
				default:
					throw new Exception("Unreachable");
			}
		}

		public static LLVMTypeRef Convert(this ITypeSymbol t)
		{
			switch (t.SpecialType)
			{
				case SpecialType.System_Boolean:
					return Int1Type;
				case SpecialType.System_SByte:
				case SpecialType.System_Byte:
					return Int8Type;
				case SpecialType.System_Char:
				case SpecialType.System_Int16:
				case SpecialType.System_UInt16:
					return Int16Type;
				case SpecialType.System_UInt32:
				case SpecialType.System_Int32:
					return Int32Type;
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
					return Int64Type;
				case SpecialType.System_IntPtr:
					return PointerType; // int8* addrspace(0) is convention
				case SpecialType.System_String:
					return PointerType;
				case SpecialType.System_Array:
					return PointerType;
				case SpecialType.System_Object:
					return PointerType;
				case SpecialType.System_Void:
					return LLVM.VoidType();
				default:
					throw new Exception("Unreachable");
			}
		}

		public static LLVMTypeRef LLVMTypeRef(this TypeInfo t)
		{
			return t.ConvertedType.Convert();
		}

		public static bool ToBool(this LLVMBool b)
		{
			return b.Value != 0;
		}

		public static LLVMIntPredicate IntPredicate(SpecialType type, SyntaxKind kind)
		{
			bool signExtended = type.IsSignExtended().ToBool();
			switch (kind)
			{
				case SyntaxKind.EqualsExpression:
					return LLVMIntPredicate.LLVMIntEQ;
				case SyntaxKind.NotEqualsExpression:
					return LLVMIntPredicate.LLVMIntNE;
				case SyntaxKind.GreaterThanExpression:
					return signExtended ? LLVMIntPredicate.LLVMIntSGT : LLVMIntPredicate.LLVMIntUGT;
				case SyntaxKind.GreaterThanOrEqualExpression:
					return signExtended ? LLVMIntPredicate.LLVMIntSGE : LLVMIntPredicate.LLVMIntUGE;
				case SyntaxKind.LessThanExpression:
					return signExtended ? LLVMIntPredicate.LLVMIntSLT : LLVMIntPredicate.LLVMIntULT;
				case SyntaxKind.LessThanOrEqualExpression:
					return signExtended ? LLVMIntPredicate.LLVMIntSLE : LLVMIntPredicate.LLVMIntULE;
				default:
					throw new Exception("Unreachable");
			}
		}

		public static LLVMRealPredicate RealPredicate(SyntaxKind kind)
		{
			switch (kind)
			{
				case SyntaxKind.EqualsExpression:
					return LLVMRealPredicate.LLVMRealOEQ;
				case SyntaxKind.NotEqualsExpression:
					return LLVMRealPredicate.LLVMRealUNE;
				case SyntaxKind.GreaterThanExpression:
					return LLVMRealPredicate.LLVMRealOGT;
				case SyntaxKind.GreaterThanOrEqualExpression:
					return LLVMRealPredicate.LLVMRealOGE;
				case SyntaxKind.LessThanExpression:
					return LLVMRealPredicate.LLVMRealOLT;
				case SyntaxKind.LessThanOrEqualExpression:
					return LLVMRealPredicate.LLVMRealOLE;
				default:
					throw new Exception("Unreachable");
			}
		}
	}
}