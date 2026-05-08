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
                        // Все переменные целочисленные по умолчанию
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            // Добавляем все переменные в таблицу имен
            variables.ForEach(variable => nameTable.AddIdentifier(variable, tCat.Var, tType.Int));
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
                    ParseExpression();

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
        /// Парсит выражение
        /// </summary>
        private void ParseExpression()
        {
            ParseTerm();

            while (LexicalAnalyzer.CurrentLexem == Lexems.Sum ||
                   LexicalAnalyzer.CurrentLexem == Lexems.Subtract)
            {
                Lexems op = LexicalAnalyzer.CurrentLexem;
                LexicalAnalyzer.ParseNextLexem();
                ParseTerm();

                if (op == Lexems.Sum)
                    CodeGenerator.AddSumInstruction();
                else if (op == Lexems.Subtract)
                    CodeGenerator.AddSubtractInstruction();
            }
        }

        /// <summary>
        /// Парсит терм (умножение, деление, унарный минус, подвыражение)
        /// </summary>
        private void ParseTerm()
        {
            ParseFactor();

            while (LexicalAnalyzer.CurrentLexem == Lexems.Multiplication ||
                   LexicalAnalyzer.CurrentLexem == Lexems.Division)
            {
                Lexems op = LexicalAnalyzer.CurrentLexem;
                LexicalAnalyzer.ParseNextLexem();
                ParseFactor();

                if (op == Lexems.Multiplication)
                    CodeGenerator.AddMultiplicationInstruction();
                else if (op == Lexems.Division)
                    CodeGenerator.AddDivisionInstruction();
            }
        }

        /// <summary>
        /// Парсит фактор (операнд, унарный минус, скобки)
        /// </summary>
        private void ParseFactor()
        {
            if (LexicalAnalyzer.CurrentLexem == Lexems.Subtract)
            {
                // Унарный минус
                LexicalAnalyzer.ParseNextLexem();
                ParseFactor();
                CodeGenerator.AddUnaryMinusInstruction();
            }
            else if (LexicalAnalyzer.CurrentLexem == Lexems.Name)
            {
                Identifier x = nameTable.FindByName(LexicalAnalyzer.CurrentName);
                if (!x.Equals(default(Identifier)) && x.Category == tCat.Var)
                {
                    CodeGenerator.AddExtractValueInstruction();
                    LexicalAnalyzer.ParseNextLexem();
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
            }
            else if (LexicalAnalyzer.CurrentLexem == Lexems.LeftBracket)
            {
                LexicalAnalyzer.ParseNextLexem();
                ParseExpression();
                CheckLexem(Lexems.RightBracket);
            }
            else
            {
                Error();
            }
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