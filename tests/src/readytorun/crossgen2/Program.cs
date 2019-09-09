// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

internal class ClassWithStatic
{
    public const int StaticValue = 0x666;
    
    [ThreadStatic]
    public static int Static = StaticValue;
}

internal class Program
{
    const int LineCountInitialValue = 0x12345678;

    [ThreadStatic]
    private static string TextFileName;

    [ThreadStatic]
    private static int LineCount = LineCountInitialValue;

    private static volatile List<string> _passedTests;

    private static List<string> _failedTests;

    private static bool NewString()
    {
        string s = new string('x', 10);
        return s.Length == 10;
    }

    private static bool WriteLine()
    {
        Console.WriteLine("Hello CoreRT R2R running on CoreCLR!");
        return true;
    }
    
    private static bool IsInstanceOf()
    {
        object obj = TextFileName;
        if (obj is string str)
        {
            Console.WriteLine($@"Object is string: {str}");
            return true;
        }
        else
        {
            Console.Error.WriteLine($@"Object is not a string: {obj}");
            return false;
        }
    }

    private static bool IsInstanceOfValueType()
    {
        object obj = LineCount;
        if (obj is int i)
        {
            Console.WriteLine($@"Object {obj:X8} is int: {i:X8}");
            return true;
        }
        else
        {
            Console.Error.WriteLine($@"Object is not an int: {obj}");
            return false;
        }
    }
    
    private unsafe static bool CheckNonGCThreadLocalStatic()
    {
        fixed (int *lineCountPtr = &LineCount)
        {
            Console.WriteLine($@"LineCount: 0x{LineCount:X8}, @ = 0x{(ulong)lineCountPtr:X8}");
        }
        fixed (int *staticPtr = &ClassWithStatic.Static)
        {
            Console.WriteLine($@"ClassWithStatic.Static: 0x{ClassWithStatic.Static:X8}, @ = 0x{(ulong)staticPtr:X8}");
        }
        fixed (int *lineCountPtr = &LineCount)
        {
            Console.WriteLine($@"LineCount: 0x{LineCount:X8}, @ = 0x{(ulong)lineCountPtr:X8}");
        }
        return LineCount == LineCountInitialValue &&
            ClassWithStatic.Static == ClassWithStatic.StaticValue;
    }

    private static bool ChkCast()
    {
        object obj = TextFileName;
        string objString = (string)obj;
        Console.WriteLine($@"String: {objString}");
        return objString == TextFileName;
    }

    private static bool ChkCastValueType()
    {
        object obj = LineCount;
        int objInt = (int)obj;
        Console.WriteLine($@"Int: {objInt:X8}");
        return objInt == LineCount;
    }
    
    private static bool BoxUnbox()
    {
        bool success = true;
        object intAsObject = LineCount;
        int unboxedInt = (int)intAsObject;
        if (unboxedInt == LineCount)
        {
            Console.WriteLine($@"unbox == box: original {LineCount}, boxed {intAsObject:X8}, unboxed {unboxedInt:X8}");
        }
        else
        {
            Console.Error.WriteLine($@"unbox != box: original {LineCount}, boxed {intAsObject:X8}, unboxed {unboxedInt:X8}");
            success = false;
        }
        int? nullableInt = LineCount;
        object nullableIntAsObject = nullableInt;
        int? unboxedNullable = (int?)nullableIntAsObject;
        if (unboxedNullable == nullableInt)
        {
            Console.WriteLine($@"unbox_nullable == box_nullable: original {nullableInt:X8}, boxed {nullableIntAsObject:X8}, unboxed {unboxedNullable:X8}");
        }
        else
        {
            Console.Error.WriteLine($@"unbox_nullable != box_nullable: original {nullableInt:X8}, boxed {nullableIntAsObject:X8}, unboxed {unboxedNullable:X8}");
            success = false;
        }
        return success;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ExplicitFieldOffsetStruct
    {
        [FieldOffset(0)]
        public int Field00;
        [FieldOffset(0x0f)]
        public int Field15;
    }

    private static ExplicitFieldOffsetStruct HelperCreateExplicitLayoutStruct()
    {
        ExplicitFieldOffsetStruct epl = new ExplicitFieldOffsetStruct();
        epl.Field00 = 40;
        epl.Field15 = 15;
        return epl;
    }

    private static bool HelperCompare(ExplicitFieldOffsetStruct val, ExplicitFieldOffsetStruct val1)
    {
        bool match = true;
        if (val.Field00 != val1.Field00)
        {
            match = false;
            Console.WriteLine("ExplicitLayout: val.Field00 = {0}, val1.Field00 = {1}", val.Field00, val1.Field00);
        }
        if (val.Field15 != val1.Field15)
        {
            match = false;
            Console.WriteLine("ExplicitLayout: val.Field15 = {0}, val1.Field15 = {1}", val.Field15, val1.Field15);
        }
        return match;
    }

    private static bool HelperCompare(ExplicitFieldOffsetStruct? val, ExplicitFieldOffsetStruct val1)
    {
        return val == null ? false : HelperCompare(val.Value, val1);
    }

    private static bool BoxUnboxToQ2(ExplicitFieldOffsetStruct? val)
    {
        return HelperCompare(val, HelperCreateExplicitLayoutStruct());
    }

    private static bool BoxUnboxToQ1(ValueType vt)
    {
        return BoxUnboxToQ2((ExplicitFieldOffsetStruct?)vt);
    }

    private static bool BoxUnboxToQ(object o)
    {
        return BoxUnboxToQ1((ValueType)o);
    }

    private static bool NullableWithExplicitLayoutTest()
    {
        ExplicitFieldOffsetStruct? s = HelperCreateExplicitLayoutStruct();
        return BoxUnboxToQ(s);
    }

    private static char HelperCreateChar()
    {
        return 'c';
    }

    private static bool HelperCompare(char val, char val1)
    {
        if (val == val1)
        {
            return true;
        }
        Console.Error.WriteLine("val = {0} = 0x{1:x2}, val1 = {2} = 0x{3:x2}", val, (int)val, val1, (int)val1);
        return false;
    }

    private static bool BoxUnboxToNQ2(char c)
    {
        return HelperCompare(c, HelperCreateChar());
    }

    private static bool BoxUnboxToNQ1(ValueType vt)
    {
        Console.WriteLine("BoxUnboxToNQ1: {0}", vt);
        return BoxUnboxToNQ2((char)vt);
    }

    private static bool BoxUnboxToNQ(object o)
    {
        Console.WriteLine("BoxUnboxToNQ: {0}", o);
        return BoxUnboxToNQ1((ValueType)o);
    }
    
    private static bool CastClassWithCharTest()
    {
        char? s = HelperCreateChar();
        return BoxUnboxToNQ(s);
    }

    private static bool TypeHandle()
    {
        Console.WriteLine(TextFileName.GetType().ToString());
        Console.WriteLine(LineCount.GetType().ToString());
        return true;
    }

    private static bool RuntimeTypeHandle()
    {
        Console.WriteLine(typeof(string).ToString());
        return true;
    }

    private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
    {
        return ((MethodCallExpression)expr.Body).Method;
    }

    private static bool RuntimeMethodHandle()
    {
        {
            MethodInfo mi = GetMethodInfo<Func<object, int, object>>((object p1, int p2) => RuntimeMethodHandleMethods.StaticMethod(p1, p2));
            if (mi.Name != "StaticMethod")
            {
                Console.WriteLine($"Expected to find runtime method handle for StaticMethod but got {mi.Name}");
                return false;
            }
        }
        
        var testClass = new RuntimeMethodHandleMethods();
        {
            MethodInfo mi = GetMethodInfo<Func<object, string, object>>((object p1, string p2) => testClass.InstanceMethod(p1, p2));
            if (mi.Name != "InstanceMethod")
            {
                Console.WriteLine($"Expected to find runtime method handle for InstanceMethod but got {mi.Name}");
                return false;
            }
        }
        
        
        {
            MethodInfo mi = GetMethodInfo<Func<string, object, object>>((string p1, object p2) => testClass.GenericMethod<string>(p1, p2));
            if (mi.Name != "GenericMethod")
            {
                Console.WriteLine($"Expected to find runtime method handle for GenericMethod but got {mi.Name}");
                return false;
            }
        }

        {
            MethodInfo mi = GetMethodInfo<Func<object, object, object>>((object p1, object p2) => GenericRuntimeMethodHandleMethods<object>.StaticMethod(p1, p2));
            if (mi.Name != "StaticMethod")
            {
                Console.WriteLine($"Expected to find runtime method handle for StaticMethod but got {mi.Name}");
                return false;
            }
        }

        {
            var genericTestClass = new GenericRuntimeMethodHandleMethods<Program>();
            MethodInfo mi = GetMethodInfo<Func<Program, string, object>>((Program p1, string p2) => genericTestClass.InstanceMethod(p1, p2));
            if (mi.Name != "InstanceMethod")
            {
                Console.WriteLine($"Expected to find runtime method handle for InstanceMethod but got {mi.Name}");
                return false;
            }
        }

        {
            var genericTestClass = new GenericRuntimeMethodHandleMethods<string>();
            MethodInfo mi = GetMethodInfo<Func<string, string, object>>((string p1, string p2) => genericTestClass.GenericMethod<string>(p1, p2));
            if (mi.Name != "GenericMethod")
            {
                Console.WriteLine($"Expected to find runtime method handle for GenericMethod but got {mi.Name}");
                return false;
            }
        }
        return true;
    }

    class RuntimeMethodHandleMethods
    {
        public static object StaticMethod(object p1, int p2)
        {
            return new object();
        }

        public object InstanceMethod(object p1, string p2)
        {
            return p2;
        }

        public object GenericMethod<T>(T p1, object p2)
        {
            return p2;
        }
    }

    class GenericRuntimeMethodHandleMethods<T>
    {
        public static object StaticMethod(T p1, object p2)
        {
            return p2;
        }

        public object InstanceMethod(T p1, string p2)
        {
            return p2;
        }

        public object GenericMethod<U>(T p1, U p2)
        {
            return p2;
        }
    }

    private static bool ReadAllText()
    {
        Console.WriteLine($@"Dumping file: {TextFileName}");
        string textFile = File.ReadAllText(TextFileName);
        if (textFile.Length > 100)
        {
            textFile = textFile.Substring(0, 100) + "...";
        }
        Console.WriteLine(textFile);

        return textFile.Length > 0;
    }

    private static bool StreamReaderReadLine()
    {
        Console.WriteLine($@"Dumping file: {TextFileName}");
        using (StreamReader reader = new StreamReader(TextFileName, System.Text.Encoding.UTF8))
        {
            Console.WriteLine("StreamReader created ...");
            string line1 = reader.ReadLine();
            Console.WriteLine($@"Line 1: {line1}");
            string line2 = reader.ReadLine();
            Console.WriteLine($@"Line 2: {line2}");
            return line2 != null;
        }
    }
    
    private static bool ConstructListOfInt()
    {
        List<int> listOfInt = new List<int>();
        if (listOfInt.Count == 0)
        {
            Console.WriteLine("Successfully constructed empty List<int>!");
            return true;
        }
        else
        {
            Console.WriteLine($@"Invalid element count in List<int>: {listOfInt.Count}");
            return false;
        }
    }
    
    private static bool ManipulateListOfInt()
    {
        List<int> listOfInt = new List<int>();
        const int ItemCount = 100;
        for (int index = ItemCount; index > 0; index--)
        {
            listOfInt.Add(index);
        }
        listOfInt.Sort();
        for (int index = 0; index < listOfInt.Count; index++)
        {
            Console.Write($@"{listOfInt[index]} ");
            if (index > 0 && listOfInt[index] <= listOfInt[index - 1])
            {
                // The list should be monotonically increasing now
                return false;
            }
        }
        Console.WriteLine();
        return listOfInt.Count == ItemCount;
    }

    private static bool ConstructListOfString()
    {
        List<string> listOfString = new List<string>();
        return listOfString.Count == 0;
    }

    private static bool ManipulateListOfString()
    {
        List<string> listOfString = new List<string>();
        const int ItemCount = 100;
        for (int index = ItemCount; index > 0; index--)
        {
            listOfString.Add(index.ToString());
        }
        listOfString.Sort();
        for (int index = 0; index < listOfString.Count; index++)
        {
            Console.Write($@"{listOfString[index]} ");
            if (index > 0 && listOfString[index].CompareTo(listOfString[index - 1]) <= 0)
            {
                // The list should be monotonically increasing now
                return false;
            }
        }
        Console.WriteLine();
        return listOfString.Count == ItemCount;
    }

    private delegate char CharFilterDelegate(char inputChar);

    private static bool SimpleDelegateTest()
    {
        CharFilterDelegate filterDelegate = CharFilterUpperCase;
        char lower = 'x';
        char upper = filterDelegate(lower);
        Console.WriteLine($@"lower = '{lower}', upper = '{upper}'");
        return upper == Char.ToUpper(lower);
    }

    private static bool CharFilterDelegateTest()
    {
        string transformedString = TransformStringUsingCharFilter(TextFileName, CharFilterUpperCase);
        Console.WriteLine(transformedString);
        return transformedString.Length == TextFileName.Length;
    }

    private static string TransformStringUsingCharFilter(string inputString, CharFilterDelegate charFilter)
    {
        StringBuilder outputBuilder = new StringBuilder(inputString.Length);
        foreach (char c in inputString)
        {
            char filteredChar = charFilter(c);
            if (filteredChar != '\0')
            {
                outputBuilder.Append(filteredChar);
            }
        }
        return outputBuilder.ToString();
    }

    private static char CharFilterUpperCase(char c)
    {
        return Char.ToUpperInvariant(c);
    }

    static bool s_sampleActionFlag;

    private static bool ActionTest()
    {
        s_sampleActionFlag = false;
        Action action = SampleAction;
        action();
        return s_sampleActionFlag;
    }

    private static void SampleAction()
    {
        Console.WriteLine("SampleAction() called!");
        s_sampleActionFlag = true;
    }

    private static bool FuncCharCharTest()
    {
        Func<char, char> charFunc = CharFilterUpperCase;

        StringBuilder builder = new StringBuilder();
        foreach (char c in TextFileName)
        {
            builder.Append(charFunc(c));
        }

        Console.WriteLine($@"Func<char,char> string: {builder}");

        return builder.ToString() == TextFileName.ToUpperInvariant();
    }

    class DisposeClass : IDisposable
    {
        public static bool DisposedFlag = false;
        
        public DisposeClass()
        {
            Console.WriteLine("DisposeClass created!");
        }
    
        public void Dispose()
        {
            Console.WriteLine("DisposeClass disposed!");
            DisposedFlag = true;
        }
    }
    
    struct DisposeStruct : IDisposable
    {
        public static bool DisposedFlag = false;

        public void Dispose()
        {
            Console.WriteLine("DisposeStruct disposed!");
            DisposedFlag = true;
        }
    }
    
    private static bool DisposeStructTest()
    {
        using (var disposeStruct = new DisposeStruct())
        {
            Console.WriteLine($@"DisposeStruct test: {disposeStruct}");
        }
        return DisposeStruct.DisposedFlag;
    }

    private static bool DisposeClassTest()
    {
        using (var disposeClass = new DisposeClass())
        {
            Console.WriteLine($@"DisposeClass test: {disposeClass}");
        }
        return DisposeClass.DisposedFlag;
    }
    
    private static bool DisposeEnumeratorTest()
    {
        List<string> listOfString = new List<string>();
        using (var enumerator = listOfString.GetEnumerator())
        {
            Console.WriteLine($@"DisposeEnumeratorTest: {enumerator}");
        }
        return true;
    }

    private static bool EmptyArrayOfInt()
    {
        int[] emptyIntArray = Array.Empty<int>();
        Console.WriteLine("Successfully constructed Array.Empty<int>!");
        return emptyIntArray.Length == 0;
    }

    private static bool EnumerateEmptyArrayOfInt()
    {
        foreach (int element in Array.Empty<int>())
        {
            Console.Error.WriteLine($@"Error: Array.Empty<int> has an element: {element}");
            return false;
        }
        Console.WriteLine("Array.Empty<int> enumeration passed");
        return true;
    }

    private static bool EmptyArrayOfString()
    {
        string[] emptyStringArray = Array.Empty<string>();
        Console.WriteLine("Successfully constructed Array.Empty<string>!");
        return emptyStringArray.Length == 0;
    }

    private static bool EnumerateEmptyArrayOfString()
    {
        foreach (string element in Array.Empty<string>())
        {
            Console.Error.WriteLine($@"Error: Array.Empty<string> has an element: {element}");
            return false;
        }
        Console.WriteLine("Array.Empty<string> enumeration passed");
        return true;
    }
    
    private static bool CreateLocalClassInstance()
    {
        var testClass = new TestClass(1234);
        Console.WriteLine("Successfully constructed TestClass");
        return testClass.A == 1234;
    }

    private class TestClass
    {
        private int _a;

        public TestClass(int a)
        {
            _a = a;
        }

        public int A => _a;
    }

    private static bool TryCatch()
    {
        try
        {
            throw new Exception("Test exception!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Expected exception: {ex.ToString()}");
            return true;
        }
    }

    private class GenException<T> : Exception {}
    
    private static bool GenericTryCatch<T>()
    {
        Exception thrown = new GenException<T>();
        try
        {
            throw thrown;
        }
        catch (GenException<T>)
        {
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Caught {0} (expected {1})", ex.GetType(), thrown.GetType());
            return false;
        }
    }

    private class RefX1<T> {}
    private class RefX2<T,U> {}
    private struct ValX1<T> {}
    private struct ValX2<T,U> {}
    private struct ValX3<T,U,V>{}

    private static bool GenericTryCatchTest()
    {
        bool success = true;
        success = GenericTryCatch<double>() && success;
        success = GenericTryCatch<string>() && success;
        success = GenericTryCatch<object>() && success;
        success = GenericTryCatch<Guid>() && success;

        success = GenericTryCatch<int[]>() && success;
        success = GenericTryCatch<double[,]>() && success;
        success = GenericTryCatch<string[][][]>() && success;
        success = GenericTryCatch<object[,,,]>() && success;
        success = GenericTryCatch<Guid[][,,,][]>() && success;

        success = GenericTryCatch<RefX1<int>[]>() && success;
        success = GenericTryCatch<RefX1<double>[,]>() && success;
        success = GenericTryCatch<RefX1<string>[][][]>() && success;
        success = GenericTryCatch<RefX1<object>[,,,]>() && success;
        success = GenericTryCatch<RefX1<Guid>[][,,,][]>() && success;
        success = GenericTryCatch<RefX2<int,int>[]>() && success;
        success = GenericTryCatch<RefX2<double,double>[,]>() && success;
        success = GenericTryCatch<RefX2<string,string>[][][]>() && success;
        success = GenericTryCatch<RefX2<object,object>[,,,]>() && success;
        success = GenericTryCatch<RefX2<Guid,Guid>[][,,,][]>() && success;
        success = GenericTryCatch<ValX1<int>[]>() && success;
        success = GenericTryCatch<ValX1<double>[,]>() && success;
        success = GenericTryCatch<ValX1<string>[][][]>() && success;
        success = GenericTryCatch<ValX1<object>[,,,]>() && success;
        success = GenericTryCatch<ValX1<Guid>[][,,,][]>() && success;

        success = GenericTryCatch<ValX2<int,int>[]>() && success;
        success = GenericTryCatch<ValX2<double,double>[,]>() && success;
        success = GenericTryCatch<ValX2<string,string>[][][]>() && success;
        success = GenericTryCatch<ValX2<object,object>[,,,]>() && success;
        success = GenericTryCatch<ValX2<Guid,Guid>[][,,,][]>() && success;

        success = GenericTryCatch<ValX1<int>>() && success;
        success = GenericTryCatch<ValX1<RefX1<int>>>() && success;
        success = GenericTryCatch<ValX2<int,string>>() && success;
        success = GenericTryCatch<ValX3<int,string,Guid>>() && success;

        success = GenericTryCatch<ValX1<ValX1<int>>>() && success;
        success = GenericTryCatch<ValX1<ValX1<ValX1<string>>>>() && success;
        success = GenericTryCatch<ValX1<ValX1<ValX1<ValX1<Guid>>>>>() && success;

        success = GenericTryCatch<ValX1<ValX2<int,string>>>() && success;
        success = GenericTryCatch<ValX2<ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>,ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>>>() && success;
        success = GenericTryCatch<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>();
        
        return success;
    }

    private static bool FileStreamNullRefTryCatch()
    {
        try
        {
            FileStream fileStream = new FileStream(null, FileMode.Open, FileAccess.Read);
            Console.Error.WriteLine("Why haven't we thrown an exception?");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Expected exception: {ex.ToString()}");
            return true;
        }
    }

    class InstanceMethodCaller<T> where T : IComparable
    {
        public static int Compare(T t, object o)
        {
            if ((o is Int32 && 123.CompareTo(o) == 0) ||
                (o is string && "hello".CompareTo(o) == 0))
            {
                return -42;
            }
    
            return t.CompareTo(o);
        }
    }

    private static bool InstanceMethodTest()
    {
        int intResult = InstanceMethodCaller<int>.Compare(122, 123);
        const int ExpectedIntResult = -42;
        Console.WriteLine("Int result: {0}, expected: {1}", intResult, ExpectedIntResult);
        
        int stringResult = InstanceMethodCaller<string>.Compare("hello", "world");
        const int ExpectedStringResult = -1;
        Console.WriteLine("String result: {0}, expected: {1}", stringResult, ExpectedStringResult);
        
        return intResult == ExpectedIntResult && stringResult == ExpectedStringResult;
    }

    private static string GetTypeName<T>()
    {
        return typeof(T).ToString();
    }

    private static bool CompareArgName(string actual, string expected)
    {
        if (actual == expected)
        {
            Console.WriteLine("Arg type match: {0}", actual);
            return true;
        }
        else
        {
            Console.WriteLine("Arg type mismatch: actual = {0}, expected = {1}", actual, expected);
            return false;
        }
    }

    class GenericLookup<T>
    {
        public static bool CheckStaticTypeArg(string typeArgName)
        {
            return CompareArgName(GetTypeName<T>(), typeArgName);
        }
        
        public bool CheckInstanceTypeArg(string typeArgName)
        {
            return CompareArgName(GetTypeName<T>(), typeArgName);
        }

        public static bool CheckStaticTypeArg<U>(string tName, string uName)
        {
            return CompareArgName(GetTypeName<T>(), tName) && CompareArgName(GetTypeName<U>(), uName);
        }

        public bool CheckInstanceTypeArg<U>(string tName, string uName)
        {
            return CompareArgName(GetTypeName<T>(), tName) && CompareArgName(GetTypeName<U>(), uName);
        }
    }

    struct GenericStruct<T>
    {
        public T FieldOfT;
        
        public GenericStruct(T fieldOfT)
        {
            FieldOfT = fieldOfT;
        }
    }
    
    class GenericClass<T>
    {
        public T FieldOfT;
        
        public GenericClass(T fieldOfT)
        {
            FieldOfT = fieldOfT;
        }
    }
    
    private static bool ThisObjGenericLookupTest()
    {
        Console.WriteLine("ThisObjGenericLookup:");
        bool result = true;
        result &= (new GenericLookup<object>()).CheckInstanceTypeArg("System.Object");
        result &= (new GenericLookup<string>()).CheckInstanceTypeArg("System.String");
        result &= (new GenericLookup<int>()).CheckInstanceTypeArg("System.Int32");
        result &= (new GenericLookup<GenericStruct<object>>()).CheckInstanceTypeArg("Program+GenericStruct`1[System.Object]");
        result &= (new GenericLookup<GenericStruct<string>>()).CheckInstanceTypeArg("Program+GenericStruct`1[System.String]");
        result &= (new GenericLookup<GenericStruct<int>>()).CheckInstanceTypeArg("Program+GenericStruct`1[System.Int32]");
        result &= (new GenericLookup<GenericClass<object>>()).CheckInstanceTypeArg("Program+GenericClass`1[System.Object]");
        result &= (new GenericLookup<GenericClass<string>>()).CheckInstanceTypeArg("Program+GenericClass`1[System.String]");
        result &= (new GenericLookup<GenericClass<int>>()).CheckInstanceTypeArg("Program+GenericClass`1[System.Int32]");
        return result;
    }

    private static bool ClassParamGenericLookupTest()
    {
        Console.WriteLine("ClassParamGenericLookup:");
        bool result = true;
        result &= GenericLookup<object>.CheckStaticTypeArg("System.Object");
        result &= GenericLookup<string>.CheckStaticTypeArg("System.String");
        result &= GenericLookup<int>.CheckStaticTypeArg("System.Int32");
        result &= GenericLookup<GenericStruct<object>>.CheckStaticTypeArg("Program+GenericStruct`1[System.Object]");
        result &= GenericLookup<GenericStruct<string>>.CheckStaticTypeArg("Program+GenericStruct`1[System.String]");
        result &= GenericLookup<GenericStruct<int>>.CheckStaticTypeArg("Program+GenericStruct`1[System.Int32]");
        result &= GenericLookup<GenericClass<object>>.CheckStaticTypeArg("Program+GenericClass`1[System.Object]");
        result &= GenericLookup<GenericClass<string>>.CheckStaticTypeArg("Program+GenericClass`1[System.String]");
        result &= GenericLookup<GenericClass<int>>.CheckStaticTypeArg("Program+GenericClass`1[System.Int32]");
        return result;
    }

    private static bool MethodParamGenericLookupTest()
    {
        Console.WriteLine("MethodParamGenericLookup:");
        bool result = true;

        result &= GenericLookup<object>.CheckStaticTypeArg<int>("System.Object", "System.Int32");
        result &= GenericLookup<string>.CheckStaticTypeArg<object>("System.String", "System.Object");
        result &= GenericLookup<int>.CheckStaticTypeArg<string>("System.Int32", "System.String");

        result &= GenericLookup<GenericStruct<object>>.CheckStaticTypeArg<GenericStruct<int>>(
            "Program+GenericStruct`1[System.Object]", "Program+GenericStruct`1[System.Int32]");
        result &= GenericLookup<GenericStruct<string>>.CheckStaticTypeArg<GenericStruct<object>>(
            "Program+GenericStruct`1[System.String]", "Program+GenericStruct`1[System.Object]");
        result &= GenericLookup<GenericStruct<int>>.CheckStaticTypeArg<GenericStruct<string>>(
            "Program+GenericStruct`1[System.Int32]", "Program+GenericStruct`1[System.String]");

        result &= GenericLookup<GenericClass<object>>.CheckStaticTypeArg<GenericClass<int>>(
            "Program+GenericClass`1[System.Object]", "Program+GenericClass`1[System.Int32]");
        result &= GenericLookup<GenericClass<string>>.CheckStaticTypeArg<GenericClass<object>>(
            "Program+GenericClass`1[System.String]", "Program+GenericClass`1[System.Object]");
        result &= GenericLookup<GenericClass<int>>.CheckStaticTypeArg<GenericClass<string>>(
            "Program+GenericClass`1[System.Int32]", "Program+GenericClass`1[System.String]");

        result &= GenericLookup<GenericClass<object>>.CheckStaticTypeArg<GenericStruct<int>>(
            "Program+GenericClass`1[System.Object]", "Program+GenericStruct`1[System.Int32]");
        result &= GenericLookup<GenericClass<string>>.CheckStaticTypeArg<GenericStruct<object>>(
            "Program+GenericClass`1[System.String]", "Program+GenericStruct`1[System.Object]");
        result &= GenericLookup<GenericClass<int>>.CheckStaticTypeArg<GenericStruct<string>>(
            "Program+GenericClass`1[System.Int32]", "Program+GenericStruct`1[System.String]");

        result &= GenericLookup<GenericStruct<object>>.CheckStaticTypeArg<GenericClass<int>>(
            "Program+GenericStruct`1[System.Object]", "Program+GenericClass`1[System.Int32]");
        result &= GenericLookup<GenericStruct<string>>.CheckStaticTypeArg<GenericClass<object>>(
            "Program+GenericStruct`1[System.String]", "Program+GenericClass`1[System.Object]");
        result &= GenericLookup<GenericStruct<int>>.CheckStaticTypeArg<GenericClass<string>>(
            "Program+GenericStruct`1[System.Int32]", "Program+GenericClass`1[System.String]");

        result &= (new GenericLookup<object>()).CheckInstanceTypeArg<GenericStruct<int>>(
            "System.Object", "Program+GenericStruct`1[System.Int32]");
        result &= (new GenericLookup<string>()).CheckInstanceTypeArg<GenericStruct<object>>(
            "System.String", "Program+GenericStruct`1[System.Object]");
        result &= (new GenericLookup<int>()).CheckInstanceTypeArg<GenericStruct<string>>(
            "System.Int32", "Program+GenericStruct`1[System.String]");
        result &= (new GenericLookup<GenericStruct<object>>()).CheckInstanceTypeArg<int>(
            "Program+GenericStruct`1[System.Object]", "System.Int32");
        result &= (new GenericLookup<GenericStruct<string>>()).CheckInstanceTypeArg<object>(
            "Program+GenericStruct`1[System.String]", "System.Object");
        result &= (new GenericLookup<GenericStruct<int>>()).CheckInstanceTypeArg<string>(
            "Program+GenericStruct`1[System.Int32]", "System.String");

        result &= (new GenericLookup<object>()).CheckInstanceTypeArg<GenericClass<int>>(
            "System.Object", "Program+GenericClass`1[System.Int32]");
        result &= (new GenericLookup<string>()).CheckInstanceTypeArg<GenericClass<object>>(
            "System.String", "Program+GenericClass`1[System.Object]");
        result &= (new GenericLookup<int>()).CheckInstanceTypeArg<GenericClass<string>>(
            "System.Int32", "Program+GenericClass`1[System.String]");
        result &= (new GenericLookup<GenericClass<object>>()).CheckInstanceTypeArg<int>(
            "Program+GenericClass`1[System.Object]", "System.Int32");
        result &= (new GenericLookup<GenericClass<string>>()).CheckInstanceTypeArg<object>(
            "Program+GenericClass`1[System.String]", "System.Object");
        result &= (new GenericLookup<GenericClass<int>>()).CheckInstanceTypeArg<string>(
            "Program+GenericClass`1[System.Int32]", "System.String");

        return result;
    }

    private static bool VectorTestOf<T>(T value1, T value2, T sum)
        where T : struct
    {
        Console.WriteLine("Constructing vector of {0}", value1);
        Vector<T> vector1 = new Vector<T>(value1);
        Console.WriteLine("Vector[0] = {0}", vector1[0]);
        Vector<T> vector2 = new Vector<T>(value2);
        Vector<T> vectorSum = vector1 + vector2;
        Console.WriteLine("Vector sum = {0}, {1} expected", vectorSum[0], sum);
        return vectorSum[0].Equals(sum);
    }

    private static bool VectorTest()
    {
        bool success = true;
        success &= VectorTestOf<int>(10, 20, 30);
        success &= VectorTestOf<float>(15.0f, 30.0f, 45.0f);
        success &= VectorTestOf<double>(50.0, 100.0, 150.0);
        return success;
    }

    private enum ByteEnum : byte
    {
        Value0,
        Value1,
        Value2,
        Value3,
    }
    
    private enum IntEnum : int
    {
        Value0,
        Value1,
        Value2,
        Value3,
    }

    private static bool EnumHashValueTest()
    {
        Console.WriteLine("ByteEnum.Value1.GetHashCode: ", ByteEnum.Value1.GetHashCode());
        Console.WriteLine("IntEnum.Value3.GetHashCode: ", IntEnum.Value3.GetHashCode());
        
        ByteEnum[] byteEnumValues = { ByteEnum.Value3, ByteEnum.Value1, ByteEnum.Value0, ByteEnum.Value2, };
        foreach (ByteEnum enumValue in byteEnumValues)
        {
            Console.WriteLine("{0}.GetHashCode: {1}", enumValue, enumValue.GetHashCode());
            if (enumValue.GetHashCode() != (int)enumValue)
            {
                return false;
            }
        }
        
        IntEnum[] intEnumValues = { IntEnum.Value2, IntEnum.Value0, IntEnum.Value1, IntEnum.Value3, };
        foreach (IntEnum enumValue in intEnumValues)
        {
            Console.WriteLine("{0}.GetHashCode: {1}", enumValue, enumValue.GetHashCode());
            if (enumValue.GetHashCode() != (int)enumValue)
            {
                return false;
            }
        }

        return true;
    }
    
    class MyGen<T>
    {
        public static string GcValue;
        public static int NonGcValue;
        [ThreadStatic]
        public static string TlsGcValue;
        [ThreadStatic]
        public static int TlsNonGcValue;
    }

    private static void SetGenericGcStatic<U, V>(string uValue, string vValue)
    {
        MyGen<U>.GcValue = uValue;
        MyGen<V>.GcValue = vValue;
    }

    private static void SetGenericNonGcStatic<U, V>(int uValue, int vValue)
    {
        MyGen<U>.NonGcValue = uValue;
        MyGen<V>.NonGcValue = vValue;
    }

    private static void SetGenericTlsGcStatic<U, V>(string uValue, string vValue)
    {
        MyGen<U>.TlsGcValue = uValue;
        MyGen<V>.TlsGcValue = vValue;
    }

    private static void SetGenericTlsNonGcStatic<U, V>(int uValue, int vValue)
    {
        MyGen<U>.TlsNonGcValue = uValue;
        MyGen<V>.TlsNonGcValue = vValue;
    }

    private static bool SharedGenericGcStaticTest()
    {
        string objectValue = "Hello";
        string stringValue = "World";
        SetGenericGcStatic<object, string>(objectValue, stringValue);
        Console.WriteLine("Object GC value: {0}, expected {1}", MyGen<object>.GcValue, objectValue);
        Console.WriteLine("String GC value: {0}, expected {1}", MyGen<string>.GcValue, stringValue);
        return MyGen<object>.GcValue == objectValue && MyGen<string>.GcValue == stringValue;
    }

    private static bool SharedGenericNonGcStaticTest()
    {
        int objectValue = 42;
        int stringValue = 666;
        SetGenericNonGcStatic<object, string>(objectValue, stringValue);
        Console.WriteLine("Object non-GC value: {0}, expected {1}", MyGen<object>.NonGcValue, objectValue);
        Console.WriteLine("String non-GC value: {0}, expected {1}", MyGen<string>.NonGcValue, stringValue);
        return MyGen<object>.NonGcValue == objectValue && MyGen<string>.NonGcValue == stringValue;
    }

    private static bool SharedGenericTlsGcStaticTest()
    {
        string objectValue = "Cpaot";
        string stringValue = "Rules";
        SetGenericTlsGcStatic<object, string>(objectValue, stringValue);
        Console.WriteLine("Object TLS GC value: {0}, expected {1}", MyGen<object>.TlsGcValue, objectValue);
        Console.WriteLine("String TLS GC value: {0}, expected {1}", MyGen<string>.TlsGcValue, stringValue);
        return MyGen<object>.TlsGcValue == objectValue && MyGen<string>.TlsGcValue == stringValue;
    }

    private static bool SharedGenericTlsNonGcStaticTest()
    {
        int objectValue = 1234;
        int stringValue = 5678;
        SetGenericTlsNonGcStatic<object, string>(objectValue, stringValue);
        Console.WriteLine("Object TLS non-GC value: {0}, expected {1}", MyGen<object>.TlsNonGcValue, objectValue);
        Console.WriteLine("String TLS non-GC value: {0}, expected {1}", MyGen<string>.TlsNonGcValue, stringValue);
        return MyGen<object>.TlsNonGcValue == objectValue && MyGen<string>.TlsNonGcValue == stringValue;
    }

    static bool RVAFieldTest()
    {
        ReadOnlySpan<byte> value = new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        bool match = true;
        for (byte i = 0; i < value.Length; i++)
        {
            if (value[i] != (byte)(9 - i))
            {
                match = false;
                Console.WriteLine(
                    "Mismatch at offset {0}: value[{0}] = {1}, should be {2}",
                    i,
                    value[i],
                    9 - i);
            }
        }
        return match;
    }

    internal class ClassWithVirtual
    {
        public bool VirtualCalledFlag = false;

        public virtual void Virtual()
        {
            Console.WriteLine("Virtual called");
            VirtualCalledFlag = true;
        }
    }

    public class BaseClass
    {
        public virtual int MyGvm<T>()
        {
            Console.WriteLine("MyGvm returning 100");
            return 100;
        }
    }

    // Test that ldvirtftn can load a virtual instance delegate method
    public static bool VirtualDelegateLoadTest()
    {
        bool success = true;

        var classWithVirtual = new ClassWithVirtual();

        Action virtualMethod = classWithVirtual.Virtual;
        virtualMethod();

        success &= classWithVirtual.VirtualCalledFlag;

        
        var bc = new BaseClass();
        success &= (bc.MyGvm<int>() == 100);

        return success;
    }

    class ClassWithGVM
    {
        public virtual bool GVM<T>(string expectedTypeName)
        {
            string typeName = GetTypeName<T>();
            Console.WriteLine("GVM<{0}> called ({1} expected)", typeName, expectedTypeName);
            return typeName == expectedTypeName;
        }
    }

    private static void GVMTestCase(Func<string, bool> gvm, string expectedTypeName, ref bool success)
    {
        if (!gvm(expectedTypeName))
        {
            success = false;
        }
    }

    private static bool GVMTest()
    {
        ClassWithGVM gvmInstance = new ClassWithGVM();
        bool success = true;
        GVMTestCase(gvmInstance.GVM<int>, "System.Int32", ref success);
        GVMTestCase(gvmInstance.GVM<object>, "System.Object", ref success);
        GVMTestCase(gvmInstance.GVM<string>, "System.String", ref success);
        return success;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object ObjectGetTypeOnGenericParamTestWorker<T>(T t)
    {
        return t.GetType();
    }

    private static bool ObjectGetTypeOnGenericParamTest()
    {
        object returnedObject = ObjectGetTypeOnGenericParamTestWorker<int>(42);
        if (returnedObject == null) return false;
        if (!(returnedObject is Type)) return false;
        if (!Object.ReferenceEquals(returnedObject, typeof(int))) return false;
        return true;
    }

    private static string EmitTextFileForTesting()
    {
        string file = Path.GetTempFileName();
        File.WriteAllText(file, "Input for a test\nA small cog in the machine\nTurning endlessly\n");
        return file;
    }

    public static int Main(string[] args)
    {
        _passedTests = new List<string>();
        _failedTests = new List<string>();

        TextFileName = EmitTextFileForTesting();

        RunTest("NewString", NewString());
        RunTest("WriteLine", WriteLine());
        RunTest("IsInstanceOf", IsInstanceOf());
        RunTest("IsInstanceOfValueType", IsInstanceOfValueType());
        RunTest("CheckNonGCThreadLocalStatic", CheckNonGCThreadLocalStatic());
        RunTest("ChkCast", ChkCast());
        RunTest("ChkCastValueType", ChkCastValueType());
        RunTest("BoxUnbox", BoxUnbox());
        // TODO: enabling this test requires fixes to IsManagedSequential I'm going to send out
        // in a subsequent PR together with removal of this temporary clause [trylek]
        // RunTest("NullableWithExplicitLayoutTest", NullableWithExplicitLayoutTest());
        RunTest("CastClassWithCharTest", CastClassWithCharTest());
        RunTest("TypeHandle", TypeHandle());
        RunTest("RuntimeTypeHandle", RuntimeTypeHandle());
        RunTest("ReadAllText", ReadAllText());
        RunTest("StreamReaderReadLine", StreamReaderReadLine());
        RunTest("SimpleDelegateTest", SimpleDelegateTest());
        RunTest("CharFilterDelegateTest", CharFilterDelegateTest());
        RunTest("ActionTest", ActionTest());
        RunTest("FuncCharCharTest", FuncCharCharTest());
        RunTest("ConstructListOfInt", ConstructListOfInt());
        RunTest("ManipulateListOfInt", ManipulateListOfInt());
        RunTest("ConstructListOfString", ConstructListOfString());
        RunTest("ManipulateListOfString", ManipulateListOfString());
        RunTest("CreateLocalClassInstance", CreateLocalClassInstance());
        RunTest("DisposeStructTest", DisposeStructTest());
        RunTest("DisposeClassTest", DisposeClassTest());
        RunTest("DisposeEnumeratorTest", DisposeEnumeratorTest());
        RunTest("EmptyArrayOfInt", EmptyArrayOfInt());
        RunTest("EnumerateEmptyArrayOfInt", EnumerateEmptyArrayOfInt());
        RunTest("EmptyArrayOfString", EmptyArrayOfString());
        RunTest("EnumerateEmptyArrayOfString", EnumerateEmptyArrayOfString());
        RunTest("TryCatch", TryCatch());
        RunTest("GenericTryCatchTest", GenericTryCatchTest());
        RunTest("FileStreamNullRefTryCatch", FileStreamNullRefTryCatch());
        RunTest("InstanceMethodTest", InstanceMethodTest());
        RunTest("ThisObjGenericLookupTest", ThisObjGenericLookupTest());
        RunTest("ClassParamGenericLookupTest", ClassParamGenericLookupTest());
        RunTest("MethodParamGenericLookupTest", MethodParamGenericLookupTest());
        RunTest("VectorTest", VectorTest());
        RunTest("EnumHashValueTest", EnumHashValueTest());
        RunTest("RVAFieldTest", RVAFieldTest());
        RunTest("SharedGenericGcStaticTest", SharedGenericGcStaticTest());
        RunTest("SharedGenericNonGcStaticTest", SharedGenericNonGcStaticTest());
        RunTest("SharedGenericTlsGcStaticTest", SharedGenericTlsGcStaticTest());
        RunTest("SharedGenericTlsNonGcStaticTest", SharedGenericTlsNonGcStaticTest());
        RunTest("VirtualDelegateLoadTest", VirtualDelegateLoadTest());
        RunTest("GVMTest", GVMTest());
        RunTest("RuntimeMethodHandle", RuntimeMethodHandle());
        RunTest("ObjectGetTypeOnGenericParamTest", ObjectGetTypeOnGenericParamTest());

        File.Delete(TextFileName);

        Console.WriteLine($@"{_passedTests.Count} tests pass:");
        foreach (string testName in _passedTests)
        {
            Console.WriteLine($@"    {testName}");
        }
        
        if (_failedTests.Count == 0)
        {
            Console.WriteLine($@"All {_passedTests.Count} tests pass!");
            return 100;
        }
        else
        {
            Console.Error.WriteLine($@"{_failedTests.Count} test failed:");
            foreach (string testName in _failedTests)
            {
                Console.Error.WriteLine($@"    {testName}");
            }
            return 1;
        }
    }

    private static void RunTest(string name, bool result)
    {
        if (result)
        {
            _passedTests.Add(name);
        }
        else
        {
            _failedTests.Add(name);
        }
    }
}
