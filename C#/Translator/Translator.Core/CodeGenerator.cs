namespace Translator.Core
{
    /// <summary>
    /// Статический класс, ответственный за генерацию кода во время компиляции.
    /// </summary>
    public static class CodeGenerator
    {
        private static List<string> code = new List<string>();

        /// <summary>
        /// Добавляет инструкцию в сгенерированный код.
        /// </summary>
        /// <param name="instruction">Инструкция для добавления.</param>
        public static void AddInstruction(string instruction)
        {
            code.Add(instruction);
        }

        /// <summary>
        /// Объявляет сегмент данных в сгенерированном коде.
        /// </summary>
        public static void DeclareDataSegment()
        {
            AddInstruction("data segment");
        }

        /// <summary>
        /// Объявляет сегменты стека и кода в сгенерированном коде.
        /// </summary>
        public static void DeclareStackAndCodeSegments()
        {
            AddInstruction("PRINT_BUF DW ' ' DUP(10)");
            AddInstruction("BUFEND    DW '$'");
            AddInstruction("data ends");
            AddInstruction("stk segment stack");
            AddInstruction("db 256 dup (\"?\")");
            AddInstruction("stk ends");
            AddInstruction("code segment");
            AddInstruction("assume cs:code,ds:data,ss:stk");
            AddInstruction("start:");
            AddInstruction("main proc");
            AddInstruction("mov ax,data");
            AddInstruction("mov ds,ax");
        }

        /// <summary>
        /// Объявляет конец основной процедуры.
        /// </summary>
        public static void DeclareMainProcedureEnd()
        {
            AddInstruction("mov ax,4c00h");
            AddInstruction("int 21h");
            AddInstruction("main endp");
        }

        /// <summary>
        /// Объявляет конец сгенерированного кода.
        /// </summary>
        public static void DeclareEndOfCode()
        {
            AddInstruction("code ends");
            AddInstruction("end main");
        }

        /// <summary>
        /// Объявляет процедуру печати в сгенерированном коде.
        /// </summary>
        public static void DeclarePrintProcedure()
        {
            AddInstruction("PRINT PROC NEAR");
            AddInstruction("MOV CX, 10");
            AddInstruction("MOV DI, BUFEND - PRINT_BUF");
            AddInstruction("PRINT_LOOP:");
            AddInstruction("MOV DX, 0");
            AddInstruction("DIV CX");
            AddInstruction("ADD DL, '0'");
            AddInstruction("MOV [PRINT_BUF + DI - 1], DL");
            AddInstruction("DEC DI");
            AddInstruction("CMP AL, 0");
            AddInstruction("JNE PRINT_LOOP");
            AddInstruction("LEA DX, PRINT_BUF");
            AddInstruction("ADD DX, DI");
            AddInstruction("MOV AH, 09H");
            AddInstruction("INT 21H");
            AddInstruction("RET");
            AddInstruction("PRINT ENDP");
        }

        /// <summary>
        /// Объявляет переменные в сегменте данных на основе таблицы имен.
        /// </summary>
        /// <param name="nameTable">Таблица имен, содержащая идентификаторы.</param>
        public static void DeclareVariables(NameTable nameTable)
        {
            nameTable.GetIdentifiers().ForEach(
            node =>
            {
                AddInstruction($"{node.Name}  dw    1");
            });
        }

        /// <summary>
        /// Генерация инструкций сложения
        /// </summary>
        public static void AddSumInstruction()
        {
            AddInstruction("pop bx");
            AddInstruction("pop ax");
            AddInstruction("add ax, bx");
            AddInstruction("push ax");
        }

        /// <summary>
        /// Генерация инструкций вычитания
        /// </summary>
        public static void AddSubtractInstruction()
        {
            AddInstruction("pop bx");
            AddInstruction("pop ax");
            AddInstruction("sub ax, bx");
            AddInstruction("push ax");
        }

        /// <summary>
        /// Генерация инструкций умножения
        /// </summary>
        public static void AddMultiplicationInstruction()
        {
            AddInstruction("pop bx");
            AddInstruction("pop ax");
            AddInstruction("mul bx");
            AddInstruction("push ax");
        }

        /// <summary>
        /// Генерация инструкций деления
        /// </summary>
        public static void AddDivisionInstruction()
        {
            AddInstruction("pop bx");
            AddInstruction("pop ax");
            AddInstruction("xor dx, dx");
            AddInstruction("div bx");
            AddInstruction("mov ax,al");
            AddInstruction("push ax");
        }

        /// <summary>
        /// Генерация инструкций унарного минуса
        /// </summary>
        public static void AddUnaryMinusInstruction()
        {
            AddInstruction("pop ax");
            AddInstruction("neg ax");
            AddInstruction("push ax");
        }

        /// <summary>
        /// Генерация инструкций добавления в регистр целого числа
        /// </summary>
        /// <param name="currentName">Строковое представление числа</param>
        public static void AddLoadIntegerInstruction(string? currentName)
        {
            AddInstruction($"mov ax, {currentName}");
            AddInstruction("push ax");
        }

        /// <summary>
        /// Генерация инструкций добавления в регистр значения из переменной
        /// </summary>
        public static void AddExtractValueInstruction()
        {
            AddInstruction("mov ax, " + LexicalAnalyzer.CurrentName);
            AddInstruction("push ax");
        }

        /// <summary>
        /// Получает сгенерированный код в виде массива строк.
        /// </summary>
        /// <returns>Массив строк с сгенерированным кодом.</returns>
        public static string[] GetGeneratedCode()
        {
            return code.ToArray();
        }

        /// <summary>
        /// Инициализирует генератор кода, очищая список инструкций.
        /// </summary>
        public static void Initialize()
        {
            code.Clear();
        }
    }
}