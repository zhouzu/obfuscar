#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Xunit;

namespace ObfuscarTests
{
    static class AssemblyHelper
    {
        public static void CheckAssembly(string name, int expectedTypes,
            Predicate<TypeDefinition> isType, Action<TypeDefinition> checkType)
        {
            AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(name);

            Assert.Equal(expectedTypes + 1, assmDef.MainModule.Types.Count);
            // String.Format ("Should contain only {0} types, and <Module>.", expectedTypes));

            bool foundType = false;

            foreach (TypeDefinition typeDef in assmDef.MainModule.Types)
            {
                if (typeDef.Name == "<Module>")
                    continue;
                else if (isType(typeDef))
                {
                    foundType = true;
                    if (checkType != null)
                        CheckTypeNested(typeDef, isType, checkType);
                }
            }

            Assert.True(foundType, "Should have found non-<Module> type.");
        }

        private static void CheckTypeNested(TypeDefinition typeDef, Predicate<TypeDefinition> isType,
            Action<TypeDefinition> checkType)
        {
            checkType(typeDef);
            if (typeDef.HasNestedTypes)
                foreach (var nested in typeDef.NestedTypes)
                    if (isType(nested))
                        CheckTypeNested(nested, isType, checkType);
        }

        public static void CheckAssembly(string name, int expectedTypes, string[] expectedMethods,
            string[] notExpectedMethods,
            Predicate<TypeDefinition> isType, Action<TypeDefinition> checkType)
        {
            HashSet<string> methodsToFind = new HashSet<string>(expectedMethods);
            HashSet<string> methodsNotToFind = new HashSet<string>(notExpectedMethods);

            CheckAssembly(name, expectedTypes, isType,
                delegate(TypeDefinition typeDef)
                {
                    // make sure we have enough methods...
                    Assert.Equal(expectedMethods.Length + notExpectedMethods.Length + 1, typeDef.Methods.Count);
                    // "Some of the methods for the type are missing.");

                    foreach (MethodDefinition method in typeDef.Methods)
                    {
                        Assert.False(methodsNotToFind.Contains(method.Name), string.Format(
                            "Did not expect to find method '{0}'.", method.Name));

                        methodsToFind.Remove(method.Name);
                    }

                    checkType?.Invoke(typeDef);
                });

            Assert.False(methodsToFind.Count > 0, "Failed to find all expected methods.");
        }
    }
}
