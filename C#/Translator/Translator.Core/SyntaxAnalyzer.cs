namespace Translator.Core
{
    /// <summary>
    /// Класс, ответственный за синтаксический анализ и компиляцию исходного кода.
    /// </summary>
    public class SyntaxAnalyzer
    {
        private NameTable nameTable = new NameTable();

        /// <summary>
        /// Компилирует исходный код.
        /// </summary>
        /// <param name="code">Исходный код.</param>
        public void Compile(string code)
        {
            LexicalAnalyzer.Initialize(code);
            CodeGenerator.Initialize();
            CodeGenerator.DeclareDataSegment();

            LexicalAnalyzer.ParseNextLexem();
            ParseVariableDeclaration();
            CodeGenerator.DeclareVariables(nameTable);

            CodeGenerator.DeclareStackAndCodeSegments();
            CheckLexem(Lexems.Semi);
            CheckLexem(Lexems.Begin);

            ParseAssignmentSequence();

            CheckLexem(Lexems.End);
            CheckLexem(Lexems.Semi);

            ParsePrintInstruction();
            CodeGenerator.DeclareMainProcedureEnd();
            CodeGenerator.DeclarePrintProcedure();
            CodeGenerator.DeclareEndOfCode();
        }

        /// <summary>
        /// Парсит инструкцию печати из исходного кода.
        /// </summary>
        private void ParsePrintInstruction()
        {
            CheckLexem(Lexems.Print);

            if (LexicalAnalyzer.CurrentLexem == Lexems.Name)
            {
                Identifier x = nameTable.FindByName(LexicalAnalyzer.CurrentName);
                if (!x.Equals(default(Identifier)) && x.Category == tCat.Var)
                {
                    // Проверяем, что переменная была инициализирована (тип не Undefined)
                    if (x.Type == tType.Undefined)
                    {
                        throw new Exception(
                            $"Ошибка: Переменная '{x.Name}' не инициализирована. Невозможно вывести значение.");
                    }

                    CodeGenerator.AddInstruction("mov ax, " + LexicalAnalyzer.CurrentName);
                    CodeGenerator.AddInstruction("CALL PRINT");
                    LexicalAnalyzer.ParseNextLexem();
                }
                else
                {
                    Error();
                }
            }
            else
            {
                Error();
            }

            CheckLexem(Lexems.Semi);
        }

        /// <summary>
        /// Парсит объявления переменных из исходного кода.
        /// </summary>
        private void ParseVariableDeclaration()
        {
            CheckLexem(Lexems.Var);

            List<string> variables = new List<string>();

            while (true)
            {
                if (LexicalAnalyzer.CurrentLexem == Lexems.Name)
                {
                    string variableName = LexicalAnalyzer.CurrentName;
                    LexicalAnalyzer.ParseNextLexem();

                    if (LexicalAnalyzer.CurrentLexem == Lexems.Comma)
                    {
                        variables.Add(variableName);
                        LexicalAnalyzer.ParseNextLexem();
                    }
                    else
                    {
                        variables.Add(variableName);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            // Все переменные создаются с типом Undefined
            variables.ForEach(variable => nameTable.AddIdentifier(variable, tCat.Var, tType.Undefined));
        }

        /// <summary>
        /// Парсит последовательность присваиваний.
        /// </summary>
        private void ParseAssignmentSequence()
        {
            ParseAssignment();
            while (LexicalAnalyzer.CurrentLexem == Lexems.Semi)
            {
                LexicalAnalyzer.ParseNextLexem();
                ParseAssignment();
            }
        }

        /// <summary>
        /// Парсит одно присваивание.
        /// </summary>
        private void ParseAssignment()
        {
            if (LexicalAnalyzer.CurrentLexem == Lexems.Name)
            {
                Identifier x = nameTable.FindByName(LexicalAnalyzer.CurrentName);
                if (!x.Equals(default(Identifier)))
                {
                    string varName = LexicalAnalyzer.CurrentName;
                    LexicalAnalyzer.ParseNextLexem();

                    CheckLexem(Lexems.Assign);

                    // Вычисляем тип выражения и генерируем код
                    tType exprType = ParseExpression();

                    // Устанавливаем или проверяем тип переменной
                    nameTable.SetType(varName, exprType);

                    CodeGenerator.AddInstruction("pop ax");
                    CodeGenerator.AddInstruction("mov " + varName + ", ax");
                }
                else
                {
                    Error();
                }
            }
        }

        /// <summary>
        /// Парсит выражение и возвращает его тип.
        /// </summary>
        private tType ParseExpression()
        {
            tType type = ParseTerm();

            while (LexicalAnalyzer.CurrentLexem == Lexems.Sum ||
                   LexicalAnalyzer.CurrentLexem == Lexems.Subtract)
            {
                Lexems op = LexicalAnalyzer.CurrentLexem;
                LexicalAnalyzer.ParseNextLexem();
                tType rightType = ParseTerm();

                if (type != tType.Int || rightType != tType.Int)
                    Error();

                if (op == Lexems.Sum)
                    CodeGenerator.AddSumInstruction();
                else if (op == Lexems.Subtract)
                    CodeGenerator.AddSubtractInstruction();
            }

            return type;
        }

        /// <summary>
        /// Парсит терм и возвращает его тип.
        /// </summary>
        private tType ParseTerm()
        {
            tType type = ParseFactor();

            while (LexicalAnalyzer.CurrentLexem == Lexems.Multiplication ||
                   LexicalAnalyzer.CurrentLexem == Lexems.Division)
            {
                Lexems op = LexicalAnalyzer.CurrentLexem;
                LexicalAnalyzer.ParseNextLexem();
                tType rightType = ParseFactor();

                if (type != tType.Int || rightType != tType.Int)
                    Error();

                if (op == Lexems.Multiplication)
                    CodeGenerator.AddMultiplicationInstruction();
                else if (op == Lexems.Division)
                    CodeGenerator.AddDivisionInstruction();
            }

            return type;
        }

        /// <summary>
        /// Парсит фактор и возвращает его тип.
        /// </summary>
        /// <summary>
        /// Парсит фактор и возвращает его тип.
        /// </summary>
        private tType ParseFactor()
        {
            if (LexicalAnalyzer.CurrentLexem == Lexems.Subtract)
            {
                // Унарный минус
                LexicalAnalyzer.ParseNextLexem();
                tType type = ParseFactor();

                if (type != tType.Int)
                    Error();

                CodeGenerator.AddUnaryMinusInstruction();
                return tType.Int;
            }
            else if (LexicalAnalyzer.CurrentLexem == Lexems.Name)
            {
                Identifier x = nameTable.FindByName(LexicalAnalyzer.CurrentName);
                if (!x.Equals(default(Identifier)) && x.Category == tCat.Var)
                {
                    // Если переменная ещё не инициализирована — ошибка
                    if (x.Type == tType.Undefined)
                    {
                        throw new Exception(
                            $"Ошибка: Переменная '{x.Name}' не инициализирована. " +
                            $"Нельзя использовать неинициализированную переменную в выражении.");
                    }

                    CodeGenerator.AddExtractValueInstruction();
                    tType varType = x.Type;
                    LexicalAnalyzer.ParseNextLexem();
                    return varType;
                }
                else
                {
                    Error();
                }
            }
            else if (LexicalAnalyzer.CurrentLexem == Lexems.Integer)
            {
                CodeGenerator.AddLoadIntegerInstruction(LexicalAnalyzer.CurrentName);
                LexicalAnalyzer.ParseNextLexem();
                return tType.Int;
            }
            else if (LexicalAnalyzer.CurrentLexem == Lexems.LeftBracket)
            {
                LexicalAnalyzer.ParseNextLexem();
                tType type = ParseExpression();
                CheckLexem(Lexems.RightBracket);
                return type;
            }
            else
            {
                Error();
            }
            return tType.Int;  // ← заменил None на Int
        }

        /// <summary>
        /// Проверяет, совпадает ли текущая лексема с ожидаемой лексемой. 
        /// Если нет, вызывает метод Error().
        /// </summary>
        /// <param name="expectedLexem">Ожидаемая лексема для проверки.</param>
        private void CheckLexem(Lexems expectedLexem)
        {
            if (LexicalAnalyzer.CurrentLexem != expectedLexem)
            {
                Error();
            }
            LexicalAnalyzer.ParseNextLexem();
        }

        /// <summary>
        /// Обрабатывает ошибки в процессе синтаксического анализа, выводя детали ошибки.
        /// </summary>
        private void Error()
        {
            throw new Exception(
                $"Ошибка в строке {Reader.LineNumber}, позиция {Reader.CharacterPositionInLine}: " +
                $"Неверная лексема: {LexicalAnalyzer.CurrentLexem}");
        }
    }
}