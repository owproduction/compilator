using System.Diagnostics;
using Translator.Core;

const string DosBoxApplicationPath = @"DOSBox\DOSBox.exe";
const string DosBoxProgramData = @"DOSBox\data\";
const string SourceFileName = "code";
const string ProgramFileName = "compile";

// Запуск теста в DOSBox
void RunDosBoxTest(string fileName, string code)
{
    if (!File.Exists(DosBoxProgramData + fileName + ".asm"))
    {
        throw new FileNotFoundException("Не найден файл: " + DosBoxProgramData + fileName + ".asm");
    }

    File.WriteAllText(DosBoxProgramData + fileName + ".asm", code);
    var mountData = @"mount D " + DosBoxProgramData.Remove(DosBoxProgramData.Length - 1);
    var masmData = "MASM.EXE " + fileName + ".asm" + " " + fileName + ".obj" + " " + fileName + ".lst" + " " + fileName + ".crf";
    var linkData = "LINK.EXE " + fileName + ".obj" + "," + fileName + ".exe" + "," + fileName + ".map" + "," + "/NODEFAULTLIB";
    var programLauch = fileName + ".exe";
    var psi = new ProcessStartInfo
    {
        FileName = DosBoxApplicationPath,
        Arguments = $"-c \"{mountData}\" -c D: -c \"{masmData}\" -c \"{linkData}\" -c " + programLauch,
        RedirectStandardOutput = true,
        UseShellExecute = false
    };
    Process.Start(psi);
}

string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
string sourceFilePath = Path.Combine(baseDirectory, SourceFileName + ".txt");
string compiledFilePath = Path.Combine(baseDirectory, ProgramFileName + ".asm");

if (!File.Exists(sourceFilePath))
{
    Console.WriteLine($"Файл не найден: {sourceFilePath}");
    Console.WriteLine("Создаю тестовый файл...");
    File.WriteAllText(sourceFilePath, "Var x, y, z;\r\n\r\nBegin\r\n    x := 10;\r\n    y := 20;\r\n    z := x + y * 2;\r\nEnd;\r\n\r\nPrint z;");
}

string sourceCode = File.ReadAllText(sourceFilePath);
Console.WriteLine("Исходный код:");
Console.WriteLine(sourceCode);
Console.WriteLine("---");

var syntaxAnalyzer = new SyntaxAnalyzer();
try
{
    syntaxAnalyzer.Compile(sourceCode);
    var code = string.Join("\n", CodeGenerator.GetGeneratedCode());
    File.WriteAllText(compiledFilePath, code);
    Console.WriteLine("Скомпилированный код:");
    Console.WriteLine(code);
    RunDosBoxTest(ProgramFileName, code);
}
catch (Exception e)
{
    Console.WriteLine($"Ошибка: {e.Message}");
    Console.WriteLine($"Стек вызовов: {e.StackTrace}");
}