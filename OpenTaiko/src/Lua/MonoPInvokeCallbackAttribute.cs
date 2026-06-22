using System;

namespace OpenTaiko {
	// The Mono / .NET-for-iOS AOT compiler matches this attribute BY NAME to pre-generate the
	// native-to-managed thunk for a static method used as an unmanaged callback (a Lua C function or
	// debug hook). Defined locally because OpenTaiko (net8.0) cannot reference ObjCRuntime, where the
	// iOS SDK's MonoPInvokeCallbackAttribute lives. Without it, calling such a method from native Lua
	// throws ExecutionEngineException ("Attempting to JIT compile ... while running in aot-only mode").
	[AttributeUsage(AttributeTargets.Method)]
	internal sealed class MonoPInvokeCallbackAttribute : Attribute {
		public MonoPInvokeCallbackAttribute(Type delegateType) { }
	}
}
