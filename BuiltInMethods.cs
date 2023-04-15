namespace gotosan
{
    internal static class BuiltInMethods
    {
        public static readonly List<string> VariablesUsed = new() {
            "variable_param",
            "variable_result",
        };

        public static readonly Dictionary<string, string> Methods = new() {
            {"say",  @"
Console.Write(variable_param);
            "},
            {"clear",  @"
Console.Clear();
            "},
            {"gettime",  @"
variable_result = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000d;
            "},
            {"wait",  @"
if (variable_param.GetType() == typeof(double)) {
    await Task.Delay((int)Math.Round((double)variable_param * 1000));
}
else {
    Error(""wait param must be a number."");
}
            "},
            {"input",  @"
while (true) {
    ConsoleKeyInfo GetKey = Console.ReadKey(true);
    if (GetKey.Key == ConsoleKey.Enter) {
        variable_result = ""\n"";
        break;
    }
    else if (GetKey.Key == ConsoleKey.Backspace || GetKey.KeyChar == '\r') {
    }
    else {
        variable_result = GetKey.KeyChar.ToString();
        break;
    }
}
            "},
            {"hasinput",  @"
variable_result = Console.KeyAvailable;
            "},
            {"random",  @"
if (long.TryParse(variable_param.ToString(), out long RandomMaximum)) {
    if (RandomMaximum >= 0) {
        variable_result = double.Parse(new Random().NextInt64(0, RandomMaximum + 1).ToString());
    }
    else {
        Error(""random param must not be negative."");
    }
}
else {
    Error(""random param must be an integer."");
}
            "},
            {"error",  @"
Error(variable_param.ToString());
            "},
            {"gettype",  @"
Dictionary<Type, string> TypeNames = new() {
    {typeof(string), ""string""},
    {typeof(double), ""number""},
    {typeof(bool), ""bool""},
};
if (variable_param == null) {
    variable_result = ""null"";
}
else if (TypeNames.TryGetValue(variable_param.GetType(), out string TypeName)) {
    variable_result = TypeName;
}
else {
    variable_result = ""unknown"";
}
            "},
            {"length",  @"
if (variable_param != null) {
    variable_result = Convert.ToDouble(variable_param.ToString().Length);
}
else {
    Error(""length param must not be null."");
}
            "},
            {"truncate",  @"
if (variable_param.GetType() == typeof(double)) {
    variable_result = Math.Truncate((double)variable_param);
}
else {
    Error(""truncate param must be a number."");
}
            "},
            {"round",  @"
if (variable_param.GetType() == typeof(double)) {
    variable_result = Math.Round((double)variable_param);
}
else {
    Error(""round param must be a number."");
}
            "},
        };
    }
}
