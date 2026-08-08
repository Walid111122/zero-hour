using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using ZeroHour.Sim;

namespace ZeroHour.Sim.Tests
{
    /// <summary>
    /// Structural guards on the simulation assembly.
    /// <para>
    /// Determinism cannot be maintained by discipline alone. One <c>float</c> added during a
    /// tired evening produces a desync that only shows up months later, on one device family,
    /// in one battle out of a thousand. These tests fail the build the moment that happens,
    /// which is far cheaper than diagnosing it in production (docs/23 §3, risk R6).
    /// </para>
    /// </summary>
    public class DeterminismGuardTests
    {
        private static Assembly SimAssembly => typeof(Fixed).Assembly;

        private const BindingFlags AllMembers =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static IEnumerable<Type> SimTypes =>
            SimAssembly.GetTypes().Where(t => !IsCompilerGenerated(t));

        private static bool IsCompilerGenerated(MemberInfo member) =>
            member.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);

        [Fact]
        public void Sim_Has_No_Floating_Point_Fields()
        {
            var offenders = new List<string>();

            foreach (Type type in SimTypes)
            {
                foreach (FieldInfo field in type.GetFields(AllMembers))
                {
                    if (IsCompilerGenerated(field))
                    {
                        continue;
                    }

                    if (field.FieldType == typeof(float) || field.FieldType == typeof(double))
                    {
                        offenders.Add(type.FullName + "." + field.Name + " : " + field.FieldType.Name);
                    }
                }
            }

            Assert.True(
                offenders.Count == 0,
                "Floating-point fields found in ZeroHour.Sim. Use Fixed instead:\n  " +
                string.Join("\n  ", offenders));
        }

        [Fact]
        public void Sim_Has_No_Floating_Point_In_Method_Signatures()
        {
            var offenders = new List<string>();

            foreach (Type type in SimTypes)
            {
                foreach (MethodInfo method in type.GetMethods(AllMembers))
                {
                    if (IsCompilerGenerated(method))
                    {
                        continue;
                    }

                    if (method.ReturnType == typeof(float) || method.ReturnType == typeof(double))
                    {
                        offenders.Add(type.FullName + "." + method.Name + " returns " + method.ReturnType.Name);
                    }

                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        if (parameter.ParameterType == typeof(float) || parameter.ParameterType == typeof(double))
                        {
                            offenders.Add(
                                type.FullName + "." + method.Name +
                                " takes " + parameter.ParameterType.Name + " " + parameter.Name);
                        }
                    }
                }
            }

            Assert.True(
                offenders.Count == 0,
                "Floating-point method signatures found in ZeroHour.Sim. Use Fixed instead:\n  " +
                string.Join("\n  ", offenders));
        }

        [Fact]
        public void Sim_Does_Not_Reference_UnityEngine()
        {
            var referenced = SimAssembly
                .GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .ToList();

            Assert.DoesNotContain(referenced, name => name.StartsWith("UnityEngine", StringComparison.Ordinal));
            Assert.DoesNotContain(referenced, name => name.StartsWith("UnityEditor", StringComparison.Ordinal));
        }

        [Fact]
        public void Sim_Exposes_No_Ambient_Time_Or_Random()
        {
            // The sim must receive time and randomness as explicit inputs. If it can reach
            // DateTime.Now or System.Random on its own, the server cannot reproduce a result.
            var offenders = new List<string>();

            foreach (Type type in SimTypes)
            {
                foreach (FieldInfo field in type.GetFields(AllMembers))
                {
                    if (IsCompilerGenerated(field))
                    {
                        continue;
                    }

                    if (field.FieldType == typeof(Random))
                    {
                        offenders.Add(type.FullName + "." + field.Name + " holds a System.Random");
                    }

                    if (field.FieldType == typeof(DateTime) || field.FieldType == typeof(DateTimeOffset))
                    {
                        offenders.Add(type.FullName + "." + field.Name + " holds a wall-clock time");
                    }
                }
            }

            Assert.True(
                offenders.Count == 0,
                "Ambient time or randomness found in ZeroHour.Sim. Pass DetRandom and an explicit " +
                "tick count instead:\n  " + string.Join("\n  ", offenders));
        }
    }
}
