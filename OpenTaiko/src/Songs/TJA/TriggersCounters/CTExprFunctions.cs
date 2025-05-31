namespace OpenTaiko {
	internal class CTExprFunctions {
		public static double DoFunc(CTExpression expr, double idxFunc, List<double> args) {
			double value;
			string strArgList = string.Join(", ", args.Select(x => x.ToString()).ToList());
			//Console.WriteLine($"Function call: Function({idxFunc})({strArgList})");
			value = 0; // mock function
			return value;
		}
	}
}
