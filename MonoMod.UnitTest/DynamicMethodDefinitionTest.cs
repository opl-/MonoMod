﻿using Xunit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.ModInterop;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace MonoMod.UnitTest {
    public class DynamicMethodDefinitionTest {
        [Fact]
        public void TestDMD() {
            Counter = 0;

            // Run the original method.
            Assert.Equal(Tuple.Create(StringOriginal, 1), ExampleMethod(1));

            MethodInfo original = typeof(DynamicMethodDefinitionTest).GetMethod(nameof(ExampleMethod));
            MethodBase patched;
            using (DynamicMethodDefinition dmd = new DynamicMethodDefinition(original)) {
                // Modify the MethodDefinition.
                foreach (Instruction instr in dmd.Definition.Body.Instructions) {
                    if (instr.Operand as string == StringOriginal)
                        instr.Operand = StringPatched;
                }

                // Generate a DynamicMethod from the modified MethodDefinition.
                patched = dmd.Generate();
            }

            // Run the DynamicMethod.
            Assert.Equal(Tuple.Create(StringPatched, 3), patched.Invoke(null, new object[] { 2 }));

            // Detour the original method to the patched DynamicMethod, then run the patched method.
            using (new Detour(original, patched)) {
                // The detour is only active in this block.
                Assert.Equal(Tuple.Create(StringPatched, 6), ExampleMethod(3));
            }

            // Run the original method again.
            Assert.Equal(Tuple.Create(StringOriginal, 10), ExampleMethod(4));
        }

        public const string StringOriginal = "Hello from ExampleMethod!";
        public const string StringPatched = "Hello from DynamicMethodDefinition!";

        public static int Counter;

        public static Tuple<string, int> ExampleMethod(int i) {
            TestObjectGeneric<string> test = new TestObjectGeneric<string>();
            try {
                Console.WriteLine(StringOriginal);

                Counter += new int?(i).Value;

                Console.WriteLine(new List<TestObjectGeneric<TestObject>>() { new TestObjectGeneric<TestObject>() }.GetEnumerator().Current);

                List<string> list = new List<string>();
                list.AddRange(new string[] { "A", "B", "C" });
                foreach (string str in list) {
                    TargetTest("A", str);
                }

            } catch (Exception) {
                return Tuple.Create("", -1);
            }
            return Tuple.Create(StringOriginal, Counter);
        }

        public static int TargetTest<T>(string a, string b) {
            return (a + b).GetHashCode();
        }

        public static int TargetTest(string a, string b) {
            return (a + b).GetHashCode();
        }

    }
}
