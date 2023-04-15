﻿using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System.Numerics;

namespace gotosan
{
    public static class Gotosan
    {
        public const string Version = "1.0.0";

        public static string Parse(string Code) {
            // Begin parsing code
            string ParsedCode = "async Task Main() {";
            List<string> Variables = new();
            void AddVariable(string VariableName, string Type) {
                if (Variables.Contains(VariableName) == false) {
                    Variables.Add(VariableName);
                    ParsedCode = $"{Type} {VariableName};" + ParsedCode;
                }
            }
            void AddLine(int LineNumber, string Line) {
                ParsedCode += $"{GetIdentifierFromNumber(LineNumber)}: {Line}\n";
            }

            // Strip code
            Code = StripCode(Code) + '\n';
            // Get each line of code
            string[] CodeLines = Code.Split('\n');

            // Loop through each line
            for (int LineNumber = 0; LineNumber < CodeLines.Length; LineNumber++) {
                // Handle empty (indented) lines
                string Line = CodeLines[LineNumber];
                if (string.IsNullOrWhiteSpace(Line)) {
                    Line = "";
                }
                // Check the first word
                string[] Words = Line.Split(' ');
                if (Words.Length >= 1 && Words[0] != "") {
                    // Goto
                    if (Words[0] == "goto") {
                        if (Words.Length >= 2 && Words[1].Length >= 1) {
                            // If statement
                            string[] GotoIfStatement = {"", ""};
                            if (Words.Length >= 4 && Words[2] == "if") {
                                if (IsVariableNameValid(Words[3])) {
                                    GotoIfStatement = new string[] {$"if (variable_{Words[3]}.Equals(true)) {{", "}"};
                                }
                                else {
                                    Error(LineNumber, "variable identifiers may only contain letters.");
                                }
                            }
                            else if (Words.Length == 3 || (Words.Length > 3 && Words[2] != "if")) {
                                Error(LineNumber, "goto statements can only end in an if statement.");
                            }
                            string GotoTargetLabel = "";
                            string CurrentLineForGoto = "";
                            // Goto line number offset
                            if (Words[1][0] == '+' || Words[1][0] == '-') {
                                if (int.TryParse(Words[1].AsSpan(1), out int TargetLineNumberOffset)) {
                                    int Modifier = (Words[1][0] == '+' ? 1 : -1);
                                    int TargetLineNumberFromOffset = Math.Clamp(LineNumber + Modifier * TargetLineNumberOffset, 0, CodeLines.Length - 1);
                                    GotoTargetLabel = GetIdentifierFromNumber(TargetLineNumberFromOffset);
                                    CurrentLineForGoto = $"goto {GotoTargetLabel};";
                                }
                                else {
                                    Error(LineNumber, "line number offset is invalid.");
                                }
                            }
                            // Goto line number
                            else if (int.TryParse(Words[1], out int TargetLineNumber)) {
                                TargetLineNumber = Math.Clamp(TargetLineNumber, 0, CodeLines.Length - 1);
                                GotoTargetLabel = GetIdentifierFromNumber(TargetLineNumber);
                                CurrentLineForGoto = $"goto {GotoTargetLabel};";
                            }
                            // Goto label
                            else if (IsVariableNameValid(Words[1])) {
                                string LabelName = Words[1];
                                // Goto built-in label
                                if (BuiltInMethods.Methods.ContainsKey(Words[1])) {
                                    GotoTargetLabel = Words[1];
                                    CurrentLineForGoto = $"await {Words[1]}();";
                                }
                                // Goto custom label
                                else {
                                    int LineOfLabel = Array.IndexOf(CodeLines, $"label {LabelName}");
                                    if (LineOfLabel >= 0) {
                                        GotoTargetLabel = GetIdentifierFromNumber(LineOfLabel);
                                        CurrentLineForGoto = $"goto {GotoTargetLabel};";
                                    }
                                    else {
                                        Error(LineNumber, $"goto label '{LabelName}' does not exist.");
                                    }
                                }
                            }
                            // Error
                            else {
                                Error(LineNumber, $"goto must be followed by a label, line number or line number offset (got '{Words[1]}').");
                            }

                            // Add goto statement
                            AddVariable($"callline_{GotoTargetLabel}", "int");
                            AddLine(LineNumber, GotoIfStatement[0] + $"callline_{GotoTargetLabel} = {LineNumber + 1};" + CurrentLineForGoto + GotoIfStatement[1]);
                        }
                        else {
                            Error(LineNumber, "goto must be followed by a label, line number or line number offset.");
                        }
                    }
                    // Label
                    else if (Words[0] == "label") {
                        if (Words.Length >= 2 && Words[1].Length >= 1 && IsVariableNameValid(Words[1])) {
                            AddVariable($"label_{Words[1]}", "int");
                            AddLine(LineNumber, "");
                        }
                        else {
                            Error(LineNumber, $"'{Words[1]}' is not a valid label identifier.");
                        }
                    }
                    // Backto
                    else if (Words[0] == "backto") {
                        if (Words.Length >= 2 && Words[1].Length >= 1 && IsVariableNameValid(Words[1])) {
                            int LineOfLabel = Array.IndexOf(CodeLines, $"label {Words[1]}");
                            if (LineOfLabel >= 0) {
                                string Identifier = GetIdentifierFromNumber(LineOfLabel);
                                // Add backto possibilities
                                string BackToMethod = $"switch (callline_{Identifier}) {{";
                                for (int i = 0; i < CodeLines.Length; i++) {
                                    BackToMethod += $"case {i}: goto {GetIdentifierFromNumber(i)}; break;";
                                }
                                BackToMethod += "}";
                                AddVariable($"callline_{Identifier}", "int");
                                AddLine(LineNumber, BackToMethod);
                            }
                            else {
                                Error(LineNumber, $"backto label '{Words[1]}' does not exist.");
                            }
                        }
                        else {
                            Error(LineNumber, $"backto label '{Words[1]}' is not valid.");
                        }
                    }
                    // Variable operation
                    else if (IsVariableNameValid(Words[0])) {
                        if (Words.Length >= 2 && Words[1].Length >= 1) {
                            if (Words.Length >= 3 && Words[2].Length >= 1 && IsGotochanValueValid(Words[2])) {
                                // Get second value
                                string SecondValue = GotochanValueToCSharpValue(LineNumber, Words[2]);
                                bool SecondValueIsVariable = SecondValue.StartsWith("variable_") && IsVariableNameValid(SecondValue.Substring("variable_".Length));
                                object SecondValueValue = !SecondValueIsVariable ? CSharpScript.EvaluateAsync(SecondValue).Result : SecondValue;
                                // Set
                                if (Words[1] == "=") {
                                    // Set variable to comparison
                                    if (Words.Length == 5 && new string[] {"==", "!=", ">", "<", ">=", "<=" }.Contains(Words[3])) {
                                        string ThirdValue = GotochanValueToCSharpValue(LineNumber, Words[4]);
                                        if (Words[3] == "==" || Words[3] == "!=") {
                                            AddLine(LineNumber, $"variable_{Words[0]} = {(Words[3] == "!=" ? "!" : "")}{SecondValue}.Equals({ThirdValue});");
                                        }
                                        else {
                                            AddLine(LineNumber, $"variable_{Words[0]} = Compare({SecondValue}, \"{Words[3]}\", {ThirdValue});");
                                        }
                                    }
                                    // Error
                                    else if (Words.Length == 4) {
                                        Error(LineNumber, $"unfinished comparison when setting variable '{Words[0]}'.");
                                    }
                                    // Set variable to value
                                    else {
                                        AddLine(LineNumber, $"variable_{Words[0]} = {SecondValue};");
                                    }
                                    AddVariable($"variable_{Words[0]}", "object?");
                                }
                                // Add
                                else if (Words[1] == "+=") {
                                    AddLine(LineNumber, $"variable_{Words[0]} = Add(variable_{Words[0]}, {SecondValue});");
                                }
                                // Subtract / Multiply / Divide
                                else if (Words[1] == "-=" || Words[1] == "*=" || Words[1] == "/=") {
                                    string Function = (Words[1] == "-=" ? "Subtract" : (Words[1] == "*=" ? "Multiply" : "Divide"));
                                    AddLine(LineNumber, $"variable_{Words[0]} = {Function}(variable_{Words[0]}, {SecondValue});");
                                }
                                // Error
                                else {
                                    Error(LineNumber, $"unknown variable operation '{Words[1]}'.");
                                }
                            }
                            else {
                                Error(LineNumber, $"variable operation value '{Words[2]}' is invalid.");
                            }
                        }
                        else if (Words[0] != string.Empty) {
                            Error(LineNumber, $"variable '{Words[0]}' must be followed by a valid operation.");
                        }
                    }
                }
                else {
                    AddLine(LineNumber, "");
                }
            }

            // Add built-in method variables
            foreach (string Variable in BuiltInMethods.VariablesUsed) {
                AddVariable(Variable, "object?");
            }
            // Add using statements
            ParsedCode = "using System;using System.Threading.Tasks;using System.Collections.Generic;" + ParsedCode;
            // Add methods
            foreach (KeyValuePair<string, string> MethodInfo in BuiltInMethods.Methods) {
                ParsedCode += $"\nasync Task {MethodInfo.Key}() {{\n" +
                    MethodInfo.Value.Trim() +
                "\n}";
            }

            ParsedCode += "}\nMain().Wait();";

            return ParsedCode;
        }
        public class RunGlobals {
            public static void Error(int LineNumber, string? Message) {
                Gotosan.Error(LineNumber, Message);
            }
            public static void Error(string? Message) {
                Gotosan.Error(Message);
            }
            public static object Add(object Num1, object Num2) {
                if (Num1.GetType() == typeof(string) || Num2.GetType() == typeof(string)) {
                    return Num1.ToString() + Num2.ToString();
                }
                else if (Num1.GetType() == typeof(double) && Num2.GetType() == typeof(double)) {
                    return (double)Num1 + (double)Num2;
                }
                else {
                    Error($"cannot add objects of type {Num1.GetType()} and {Num2.GetType()}.");
                    return 0;
                }
            }
            public static object Subtract(object Num1, object Num2) {
                if (Num1.GetType() == typeof(double) && Num2.GetType() == typeof(double)) {
                    return (double)Num1 - (double)Num2;
                }
                else {
                    Error($"cannot subtract objects of type {Num1.GetType()} and {Num2.GetType()}.");
                    return 0;
                }
            }
            public static object Multiply(object Num1, object Num2) {
                if (Num1.GetType() == typeof(double) && Num2.GetType() == typeof(double)) {
                    return (double)Num1 * (double)Num2;
                }
                else {
                    Error($"cannot subtract objects of type {Num1.GetType()} and {Num2.GetType()}.");
                    return 0;
                }
            }
            public static object Divide(object Num1, object Num2) {
                if (Num1.GetType() == typeof(double) && Num2.GetType() == typeof(double)) {
                    return (double)Num1 / (double)Num2;
                }
                else {
                    Error($"cannot subtract objects of type {Num1.GetType()} and {Num2.GetType()}.");
                    return 0;
                }
            }
            public static bool Compare(object Num1, string Operator, object Num2) {
                if (Num1.GetType() == typeof(double) && Num2.GetType() == typeof(double)) {
                    switch (Operator) {
                        case ">":
                            return (double)Num1 > (double)Num2;
                        case "<":
                            return (double)Num1 < (double)Num2;
                        case ">=":
                            return (double)Num1 >= (double)Num2;
                        case "<=":
                            return (double)Num1 <= (double)Num2;
                        default:
                            Error($"cannot compare objects of type {Num1.GetType()} and {Num2.GetType()}.");
                            return false;
                    }
                }
                else {
                    Error($"cannot compare objects of type {Num1.GetType()} and {Num2.GetType()}.");
                    return false;
                }
            }
        }
        public static async Task Run(string ParsedCode) {
            try {
                await CSharpScript.RunAsync(ParsedCode, ScriptOptions.Default.WithOptimizationLevel(OptimizationLevel.Release), new RunGlobals());
            }
            catch (Exception E) {
                Error($"there was an error running your code: '{E.Message}'.\n{ParsedCode}");
            }
        }

        private static string StripCode(string Code) {
            // Remove carriage returns
            Code = Code.Replace("\r", "");
            // Remove tabs
            Code = Code.Replace("\t", "");
            // Get each line of code
            string[] CodeLines = Code.Split('\n');
            // Iterate through each line of code
            string NewCode = "";
            for (int i = 0; i < CodeLines.Length; i++) {
                string Line = CodeLines[i];
                // Ignore comments
                int IndexOfComment = Line.IndexOf('#');
                if (IndexOfComment >= 0) {
                    Line = Line[..IndexOfComment];
                }
                // Trim spaces
                Line = Line.Trim(' ');
                // Build new code
                NewCode += Line + '\n';
            }
            return NewCode.Trim('\n');
        }
        /// <summary>Converts each digit of a number to a letter and returns the string.</summary>
        private static string GetIdentifierFromNumber(int Number) {
            const string Characters = "abcdefghij";
            string Identifier = "id__";
            foreach (char Character in Number.ToString()) {
                Identifier += Characters[int.Parse(Character.ToString())];
            }
            return Identifier;
        }
        private static bool IsGotochanValueValid(string ValueString) {
            if (ValueString.StartsWith('~') || (ValueString.Contains(',') == false && double.TryParse(ValueString, out _)) || ValueString == "yes" ||
                ValueString == "no" || ValueString == "null" || IsVariableNameValid(ValueString)) {
                return true;
            }
            return false;
        }
        private static string GotochanValueToCSharpValue(int LineNumber, string ValueString) {
            // String
            if (ValueString.StartsWith('~')) {
                return "@\"" + ValueString[1..].Replace('~', ' ').Replace("\\n", "\n").Replace("\\h", "#").Replace("\"", "\"\"") + '"';
            }
            // Double
            else if (ValueString.Contains(',') == false && double.TryParse(ValueString, out double DoubleResult)) {
                return ValueString + "d";
            }
            // Bool
            else if (ValueString == "yes") {
                return "true";
            }
            else if (ValueString == "no") {
                return "false";
            }
            // Null
            else if (ValueString == "null") {
                return "null";
            }
            // Variable
            else if (IsVariableNameValid(ValueString)) {
                return $"variable_{ValueString}";
            }
            // Unknown
            else {
                Error(LineNumber, $"unknown data type of value '{ValueString}'.");
                return string.Empty;
            }
        }
        private static bool IsVariableNameValid(string VariableName) {
            for (int i = 0; i < VariableName.Length; i++) {
                if (char.IsLetter(VariableName[i]) || (i != 0 && char.IsDigit(VariableName[i]))) {
                    continue;
                }
                return false;
            }
            return true;
        }
        private static void Error(int LineNumber, string? Message) {
            ConsoleColor PreviousForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.Write($"there was an error");
            if (LineNumber >= 0) {
                Console.Write($" on line {LineNumber + 1}");
            }
            if (Message != null) {
                Console.Write(":\n    " + Message);
            }
            else {
                Console.Write(".");
            }
            Console.ForegroundColor = PreviousForegroundColor;
            Console.ReadLine();
            Environment.Exit(0);
        }
        private static void Error(string? Message) {
            Error(-1, Message);
        }
    }
}