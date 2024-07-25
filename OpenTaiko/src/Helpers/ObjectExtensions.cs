using System.ArrayExtensions;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using TJAPlayer3;

// https://github.com/Burtsev-Alexey/net-object-deep-copy/blob/master/ObjectExtensions.cs

namespace System {
	public static class ObjectExtensions {
		private static readonly MethodInfo CloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

		public static bool IsPrimitive(this Type type) {
			if (type == typeof(String)) return true;
			return (type.IsValueType & type.IsPrimitive);
		}

		public static Object Copy(this Object originalObject) {
			return InternalCopy(originalObject, new Dictionary<Object, Object>(new ReferenceEqualityComparer()));
		}
		private static Object InternalCopy(Object originalObject, IDictionary<Object, Object> visited) {
			if (originalObject == null) return null;
			var typeToReflect = originalObject.GetType();
			if (IsPrimitive(typeToReflect)) return originalObject;
			if (visited.ContainsKey(originalObject)) return visited[originalObject];
			if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
			var cloneObject = CloneMethod.Invoke(originalObject, null);
			if (typeToReflect.IsArray) {
				var arrayType = typeToReflect.GetElementType();
				if (IsPrimitive(arrayType) == false) {
					Array clonedArray = (Array)cloneObject;
					clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
				}

			}
			visited.Add(originalObject, cloneObject);
			CopyFields(originalObject, visited, cloneObject, typeToReflect);
			RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
			return cloneObject;
		}

		private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect) {
			if (typeToReflect.BaseType != null) {
				RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
				CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
			}
		}

		private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null) {
			foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags)) {
				if (filter != null && filter(fieldInfo) == false) continue;
				if (IsPrimitive(fieldInfo.FieldType)) continue;
				var originalFieldValue = fieldInfo.GetValue(originalObject);
				var clonedFieldValue = InternalCopy(originalFieldValue, visited);
				fieldInfo.SetValue(cloneObject, clonedFieldValue);
			}
		}
		public static T Copy<T>(this T original) {
			return (T)Copy((Object)original);
		}
	}

	public class ReferenceEqualityComparer : EqualityComparer<Object> {
		public override bool Equals(object x, object y) {
			return ReferenceEquals(x, y);
		}
		public override int GetHashCode(object obj) {
			if (obj == null) return 0;
			return obj.GetHashCode();
		}
	}

	namespace ArrayExtensions {
		public static class ArrayExtensions {
			public static void ForEach(this Array array, Action<Array, int[]> action) {
				if (array.LongLength == 0) return;
				ArrayTraverse walker = new ArrayTraverse(array);
				do action(array, walker.Position);
				while (walker.Step());
			}
		}

		internal class ArrayTraverse {
			public int[] Position;
			private int[] maxLengths;

			public ArrayTraverse(Array array) {
				maxLengths = new int[array.Rank];
				for (int i = 0; i < array.Rank; ++i) {
					maxLengths[i] = array.GetLength(i) - 1;
				}
				Position = new int[array.Rank];
			}

			public bool Step() {
				for (int i = 0; i < Position.Length; ++i) {
					if (Position[i] < maxLengths[i]) {
						Position[i]++;
						for (int j = 0; j < i; j++) {
							Position[j] = 0;
						}
						return true;
					}
				}
				return false;
			}
		}
	}


	// Below methods are added by 0AuBSQ

	public static class StringExtensions {
		// TagRegex and RemoveTags are copies of TagRegex and Purify from the CSkiaSharpTextRenderer class (Have two instances because of both being in 2 different projects)

		private const string TagRegex = @"<(/?)([gc](?:\.#[0-9a-fA-F]{6})*?)>";

		public static string RemoveTags(this string input) {
			return Regex.Replace(input, TagRegex, "");
		}

		public static string EscapeSingleQuotes(this string input) {
			return input.Replace(@"'", @"''");
		}

		public static string SafeFormat(this string format, params object?[] args) {
			try {
				return String.Format(format, args);
			} catch {
				return format;
			}
		}

		public static string[] SplitByCommas(this string input) {
			// Regular expression to split by commas, but not by escaped commas (\,)
			var pattern = @"(?<!\\),";
			var parts = Regex.Split(input, pattern);

			// Replace escaped commas with actual commas in the parts
			for (int i = 0; i < parts.Length; i++) {
				parts[i] = parts[i].Replace(@"\,", ",");
			}

			// Filter out empty strings
			var filteredParts = parts.Where(part => !string.IsNullOrEmpty(part)).ToArray();

			return filteredParts;
		}

		public static double[] ParseComplex(this string input) {
			try {
				// Removing all spaces from the input for easier processing
				input = input.Replace(" ", "").ToLower();

				double real = 0;
				double imaginary = 0;

				// If the input contains 'i', we need to handle the imaginary part
				if (input.Contains("i")) {
					// Special cases for 'i', '-i', '1+i', '1-i'
					if (input == "i") {
						imaginary = 1;
					} else if (input == "-i") {
						imaginary = -1;
					} else {
						// Remove 'i' for further processing
						input = input.Replace("i", "");

						// Check if input ends with '+' or '-', meaning it was something like '1+i' or '1-i'
						if (input.EndsWith("+") || input.EndsWith("-")) {
							real = double.Parse(input.TrimEnd('+', '-'), CultureInfo.InvariantCulture);
							imaginary = input.EndsWith("+") ? 1 : -1;
						} else {
							// Split the input into real and imaginary parts
							string[] parts = input.Split(new[] { '+', '-' }, StringSplitOptions.RemoveEmptyEntries);

							if (input.Contains("+")) {
								real = double.Parse(parts[0], CultureInfo.InvariantCulture);
								imaginary = double.Parse(parts[1], CultureInfo.InvariantCulture);
							} else if (input.LastIndexOf('-') > 0) // handling cases like "1-2i"
							  {
								real = double.Parse(parts[0], CultureInfo.InvariantCulture);
								imaginary = -double.Parse(parts[1], CultureInfo.InvariantCulture);
							} else if (input.StartsWith("-")) {
								imaginary = -double.Parse(parts[0], CultureInfo.InvariantCulture);
							} else {
								imaginary = double.Parse(parts[0], CultureInfo.InvariantCulture);
							}
						}
					}
				} else {
					// If there is no 'i', it is purely a real number
					real = double.Parse(input, CultureInfo.InvariantCulture);
				}

				return new double[] { real, imaginary };
			} catch (Exception ex) {
				LogNotification.PopWarning($"'{input}': Incorrect complex number formatting, defaulting to 0");
				return new double[] { 0, 0 }; // Return default value in case of error
			}
		}
	}
}
