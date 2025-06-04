using System.Text.RegularExpressions;

namespace OpenTaiko {
	internal class CTExpression {
		private string _expr;
		private int _pos;
		public int _player { get; private set; }
		public int _actual { get; private set; }
		public SaveFile? _sfref { get; private set; }

		public static double Evaluate(string strExpr, int player) {
			//Console.WriteLine($"Resolved: {strExpr}");
			var expr = new CTExpression(strExpr, player);
			double value = expr.ParseExpression();
			expr.CheckNoRemain();
			return value;
		}

		private string ResolveAllVariables(string expr) {
			string pattern = @"<([^<>]*)>";
			while (Regex.IsMatch(expr, pattern)) {
				expr = Regex.Replace(expr, pattern, match => {
					string[] parts = match.Groups[1].Value.Split(':');
					string name = parts[0].Trim();
					List<string> args = new List<string>();
					for (int i = 1; i < parts.Length; i++)
						args.Add(parts[i].Trim());
					return $" {this.ResolveVariable(name, args).ToString()} "; // prevent concatenating
				});
			}
			return expr;
		}

		private double ResolveVariable(string name, List<string> args) {
			if (_sfref == null) return 0;
			return CTExprVariables.ResolveVariable(this, name, args);
		}

		private CTExpression(string expr, int player) {
			string _resexp = this.ResolveAllVariables(expr);
			this._expr = string.Join(" ", _resexp.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
			this._player = player;
			this._actual = OpenTaiko.GetActualPlayer(player);
			this._pos = 0;
			this._sfref = (player >= 0) ? OpenTaiko.SaveFileInstances[this._actual] : null;
		}

		private char Current => _pos < _expr.Length ? _expr[_pos] : '\0';
		private string Remain => _expr.Substring(_pos);
		private void Next() => _pos++;

		private void CheckNoRemain() {
			if (Current == ' ')
				Next();
			if (_pos < _expr.Length)
				throw new Exception($"Unexpected character {Current} of {Remain}");
		}

		private bool Match(char expected) {
			if (Current == ' ')
				Next();
			if (Current == expected) {
				Next();
				return true;
			}
			return false;
		}

		private double ParseExpression() => ParseOr();

		private double ParseOr() {
			double left = ParseXor();
			while (Match('|')) {
				bool bLeft = ToBool(left);
				bool bRight = ToBool(ParseXor());
				//Console.Write($"Or({bLeft}, {bRight})");
				left = (bLeft || bRight) ? 1 : 0;
				//Console.WriteLine($" == {left}");
			}
			return left;
		}

		private double ParseXor() {
			double left = ParseAnd();
			while (Match('^')) {
				bool bLeft = ToBool(left);
				bool bRight = ToBool(ParseAnd());
				//Console.Write($"Xor({bLeft}, {bRight})");
				left = (bLeft ^ bRight) ? 1 : 0;
				//Console.WriteLine($" == {left}");
			}
			return left;
		}

		private double ParseAnd() {
			double left = ParseEquality();
			while (Match('&')) {
				bool bLeft = ToBool(left);
				bool bRight = ToBool(ParseEquality());
				//Console.Write($"And({bLeft}, {bRight})");
				left = (bLeft && bRight) ? 1 : 0;
				//Console.WriteLine($" == {left}");
			}
			return left;
		}

		private double ParseEquality() {
			double left = ParseComparison();
			while (true) {
				if (Match('=')) {
					if (!Match('='))
						throw new Exception("Expected '=='");
					double right = ParseComparison();
					//Console.Write($"Equal({left}, {right})");
					left = right == left ? 1 : 0;
					//Console.WriteLine($" == {left}");
				} else if (Match('!')) {
					if (!Match('='))
						throw new Exception("Expected '!='");
					double right = ParseComparison();
					//Console.Write($"NotEqual({left}, {right})");
					left = right != left ? 1 : 0;
					//Console.WriteLine($" == {left}");
				} else return left;
			}
		}

		private double ParseComparison() {
			double left = ParseAddSub();
			while (true) {
				if (Match('>')) {
					if (Match('=')) {
						double right = ParseAddSub();
						//Console.Write($"AboveEqual({left}, {right})");
						left = left >= right ? 1 : 0;
					} else {
						double right = ParseAddSub();
						//Console.Write($"Above({left}, {right})");
						left = left > right ? 1 : 0;
					}
					//Console.WriteLine($" == {left}");
				} else if (Match('<')) {
					if (Match('=')) {
						double right = ParseAddSub();
						//Console.Write($"BelowEqual({left}, {right})");
						left = left <= right ? 1 : 0;
					} else {
						double right = ParseAddSub();
						//Console.Write($"Below({left}, {right})");
						left = left < right ? 1 : 0;
					}
					//Console.WriteLine($" == {left}");
				} else return left;
			}
		}

		private double ParseAddSub() {
			double left = ParseMulDiv();
			while (true) {
				if (Match('+')) {
					double right = ParseMulDiv();
					//Console.Write($"Add({left}, {right})");
					left += right;
					//Console.WriteLine($" == {left}");
				} else if (Match('-')) {
					double right = ParseMulDiv();
					//Console.Write($"Sub({left}, {right})");
					left -= right;
					//Console.WriteLine($" == {left}");
				} else return left;
			}
		}

		private double ParseMulDiv() {
			double left = ParseUnary();
			while (true) {
				if (Match('*')) {
					double right = ParseUnary();
					//Console.Write($"Mul({left}, {right})");
					left *= right;
					//Console.WriteLine($" == {left}");
				} else if (Match('/')) {
					double right = ParseUnary();
					//Console.Write($"Div({left}, {right})");
					left /= right;
					//Console.WriteLine($" == {left}");
				} else return left;
			}
		}

		private double ParseUnary() {
			if (Match('-')) {
				double left = ParseUnary();
				//Console.WriteLine($"Neg({left})");
				left = -left;
				//Console.WriteLine($" == {left}");
				return left;
			}
			if (Match('!')) {
				bool bLeft = ToBool(ParseUnary());
				//Console.Write($"Not({bLeft})");
				double left = !bLeft ? 1 : 0;
				//Console.WriteLine($" == {left}");
				return left;
			}
			return ParseFunctionCall();
		}

		private double ParseFunctionCall() {
			double left = ParsePrimary();
			while (Match('(')) // function call (with function represented by a number index)
			{
				var args = new List<double>();
				while (true) {
					args.Add(ParseExpression());
					if (Match(':')) continue;
					if (Match(')')) break;
					throw new Exception($"Unexpected character: {Current} of {Remain}");
				}
				left = DoFunc(left, args);
			}
			return left;
		}

		private double ParsePrimary() {
			if (Match('(')) {
				double val = ParseExpression();
				if (!Match(')')) throw new Exception($"Expected ')', got {Current} of {Remain}");
				return val;
			}

			int start = _pos;
			while (char.IsDigit(Current) || Current == '.') Next();
			if (start == _pos) {
				if (Current == '\0')
					throw new Exception($"Unexpected end of input");
				throw new Exception($"Unexpected character: {Current} of {Remain}");
			}
			string strLeft = _expr.Substring(start, _pos - start);
			//Console.Write($"Number(\"{strLeft}\")");
			double left = double.Parse(strLeft);
			//Console.WriteLine($" == {left}");
			return left;
		}

		private bool ToBool(double value) {
			if (value != 0 && value != 1)
				Console.WriteLine($"Warning: treating non-zero logical value {value} as 1");
			//Console.Write($"Bool({value})");
			bool bLeft = value != 0;
			//Console.WriteLine($" == {bLeft}");
			return bLeft;
		}

		private double DoFunc(double idxFunc, List<double> args) {
			return CTExprFunctions.DoFunc(this, idxFunc, args);
		}

		#region [Unit tests]

		public static void TEST() {
			int nFail = 0;

			(bool NoError, double Value) Exec(string expr) {
				Console.WriteLine($"\nExpr: {expr}");
				try {
					double result = Evaluate(expr, -1);
					Console.WriteLine($"Result: {result}");
					return (true, result);
				} catch (Exception ex) {
					Console.WriteLine($"Error: {ex.Message}");
					return (false, default);
				}
			}

			void Expect(string expr, double value) {
				if (Exec(expr) != (true, value)) {
					Console.WriteLine($"Wrong: Expected {value}");
					++nFail;
				}
			}

			bool Bool(double v) => v != 0;
			double Double(bool v) => v ? 1 : 0;
			void ExpectBool(string expr, bool value) => Expect(expr, Double(value));

			void ExpectError(string expr) {
				if (Exec(expr).NoError) {
					Console.WriteLine($"Wrong: Expected error");
					++nFail;
				}
			}

			ExpectBool("((3 + 4) * 2 > 10) & ((8 / 2) == 4)", ((3 + 4) * 2 > 10) && ((8 / 2) == 4));
			ExpectBool("(5 * (2 + 3) != 20) | ((1 + 1) * 2 == 4)", (5 * (2 + 3) != 20) || ((1 + 1) * 2 == 4));
			ExpectBool("!((3 + 2) < 4)", !((3 + 2) < 4));
			ExpectBool("!(2 + 2 == 4) | (3 ^ 3)", !(2 + 2 == 4) || (Bool(3) ^ Bool(3)));
			ExpectBool("(((1+2)*3)>8)^((4*2)<=8)", (((1 + 2) * 3) > 8) ^ ((4 * 2) <= 8));
			ExpectBool("(<a:val> + 3) * 2 > (<b:x:y> - 1)", (0 + 3) * 2 > (0 - 1));
			ExpectBool("((<x> + 2) * 5) == (10 + <x>)", ((0 + 2) * 5) == (10 + 0));
			Expect("5 + (3 * (<temp> + 2))", 5 + (3 * (0 + 2)));
			ExpectBool("!(1 & 0) | (0 ^ 1)", !(Bool(1) & Bool(0)) || (Bool(0) ^ Bool(1)));
			ExpectBool("!1 & 1 | 1", !Bool(1) && Bool(1) || Bool(1));
			ExpectBool("!(1 | 0) & (1 ^ 0)", !(Bool(1) || Bool(0)) && (Bool(1) ^ Bool(0)));
			ExpectBool("10 / (2 + 3) >= 2", 10 / (2 + 3) >= 2);
			ExpectBool("1 + 2 + 3 + 4 + 5 == 15", 1 + 2 + 3 + 4 + 5 == 15);
			ExpectBool("((2+3)*2 + 1) == (3*3 + 1)", ((2 + 3) * 2 + 1) == (3 * 3 + 1));
			ExpectBool("4 * (2 + (1 + 3)) > 20", 4 * (2 + (1 + 3)) > 20);
			ExpectBool("((<a:x:y> * 2 + 1) >= 1) & (3 < 4)", ((0 * 2 + 1) >= 1) && (3 < 4));
			ExpectBool("<foo:bar:baz> + 1 != 1", 0 + 1 != 1);

			// Function call
			Expect("<math:atan2>(<math:cos>(<lc:t>) * <math:pi> : <math:sin>(<lc:t>) * <math:pi>)", 0);
			Expect("<func:select>(<math:get_random_int>(0 : 1) : <math:sin> : <math:cos>)(<lc:t>)", 0);
			Expect("(<game:set_effect_attr> + <math:get_random_int>(0 : <game:effect_attr_count>))(42)", 0);

			// Special case
			Expect("1.-0.7", 1.0 - 0.7);
			ExpectBool("\t40\t+\t2\t==\t42\t", 40 + 2 == 42);
			ExpectBool(" < jp > < < lc : perfect_count > < ( < jb > < < lc : bad_count > ) ", Double(0 < 0) < Double(0 < 0));
			ExpectBool(" 3 < < lc : perfect_count > < ( 1 < < lc : bad_count > ) ", Double(3 < 0) < Double(1 < 0));
			Expect("42(0(69) : 11(45))", 0); // Somehow someone managed to remember all the function indexes
			Expect("(8)(8)(8)", 0);

			// variable lookup failure
			Expect("<a b>", 0);
			Expect("<:simplestyleSweat:>", 0);

			// Illegal cases
			ExpectError("<artist:Tanger>(:3)");
			ExpectError("<math:max>(3 0 0)");
			ExpectError("3+");
			ExpectError("helloworld");
			ExpectError("🥁");
			ExpectError(" ");
			ExpectError("");
			ExpectError("<jp><lc>"); // no forming numbers by concatenating
			ExpectError("3<jp>");
			ExpectError("()"); // function parenthses are not normal parentheses
			ExpectError("(1:1)");

			if (nFail > 0)
				Console.WriteLine($"\n{nFail} cases failed!");
			else
				Console.WriteLine($"\nPass!");
		}

		#endregion
	}
}
