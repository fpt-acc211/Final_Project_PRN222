using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Services
{
    public class QuestionImportService : IQuestionImportService
    {
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        public QuestionImportPreview ParseText(string text)
        {
            var rows = new List<QuestionImportRow>();
            var blocks = Regex.Split(text.Replace("\r\n", "\n").Trim(), @"\n\s*\n")
                .Where(block => !string.IsNullOrWhiteSpace(block))
                .ToList();

            for (var i = 0; i < blocks.Count; i++)
            {
                rows.Add(ParseTextBlock(blocks[i], i + 1));
            }

            return ValidateRows(rows);
        }

        public QuestionImportPreview ParseExcel(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            var sheetRows = ReadFirstWorksheet(stream);
            if (sheetRows.Count < 2)
            {
                return new QuestionImportPreview
                {
                    Errors =
                    {
                        new QuestionImportError
                        {
                            RowNumber = 1,
                            Message = "File Excel cần có dòng tiêu đề và ít nhất một dòng dữ liệu."
                        }
                    }
                };
            }

            var headerMap = BuildHeaderMap(sheetRows[0]);
            var importRows = new List<QuestionImportRow>();

            for (var rowIndex = 1; rowIndex < sheetRows.Count; rowIndex++)
            {
                var row = sheetRows[rowIndex];
                if (row.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                var importRow = new QuestionImportRow
                {
                    RowNumber = rowIndex + 1,
                    Content = GetValue(row, headerMap, "question", "content", "question content", "cau hoi", "noi dung", "noi dung cau hoi"),
                    Explanation = NullIfWhiteSpace(GetValue(row, headerMap, "explanation", "explain", "giai thich", "loi giai"))
                };

                var typeValue = GetValue(row, headerMap, "type", "question type", "loai", "loai cau hoi");
                importRow.QuestionType = ParseQuestionType(typeValue) ?? 0;

                for (var answerIndex = 1; answerIndex <= 8; answerIndex++)
                {
                    var answer = GetValue(row, headerMap,
                        $"answer{answerIndex}",
                        $"answer {answerIndex}",
                        $"a{answerIndex}",
                        $"dap an {answerIndex}",
                        $"dapan{answerIndex}",
                        $"tra loi {answerIndex}",
                        $"traloi{answerIndex}");

                    if (string.IsNullOrWhiteSpace(answer))
                    {
                        continue;
                    }

                    var correctValue = GetValue(row, headerMap,
                        $"correct{answerIndex}",
                        $"correct {answerIndex}",
                        $"iscorrect{answerIndex}",
                        $"is correct {answerIndex}",
                        $"answer{answerIndex}correct",
                        $"answer {answerIndex} correct",
                        $"dung{answerIndex}",
                        $"dap an dung {answerIndex}",
                        $"dapandung{answerIndex}");

                    importRow.Answers.Add(new QuestionImportAnswer
                    {
                        Content = answer.Trim(),
                        IsCorrect = IsTruthy(correctValue)
                    });
                }

                importRows.Add(importRow);
            }

            return ValidateRows(importRows);
        }

        public QuestionImportPreview ValidateRows(IEnumerable<QuestionImportRow> rows)
        {
            var preview = new QuestionImportPreview();

            foreach (var sourceRow in rows)
            {
                var row = new QuestionImportRow
                {
                    RowNumber = sourceRow.RowNumber,
                    Content = sourceRow.Content?.Trim() ?? string.Empty,
                    QuestionType = sourceRow.QuestionType,
                    Explanation = NullIfWhiteSpace(sourceRow.Explanation),
                    Answers = sourceRow.Answers
                        .Where(answer => !string.IsNullOrWhiteSpace(answer.Content))
                        .Select(answer => new QuestionImportAnswer
                        {
                            Content = answer.Content.Trim(),
                            IsCorrect = answer.IsCorrect
                        })
                        .ToList()
                };

                if (row.QuestionType == 0)
                {
                    row.QuestionType = row.Answers.Count(answer => answer.IsCorrect) > 1 ? 2 : 1;
                }

                var errors = new List<string>();
                if (string.IsNullOrWhiteSpace(row.Content))
                {
                    errors.Add("Thiếu nội dung câu hỏi.");
                }

                if (row.QuestionType is not 1 and not 2)
                {
                    errors.Add("Loại câu hỏi phải là single/1 hoặc multiple/2.");
                }

                if (row.Answers.Count < 2)
                {
                    errors.Add("Cần ít nhất 2 đáp án.");
                }

                var correctCount = row.Answers.Count(answer => answer.IsCorrect);
                if (row.QuestionType == 1 && correctCount != 1)
                {
                    errors.Add("Câu single choice phải có đúng 1 đáp án đúng.");
                }

                if (row.QuestionType == 2 && correctCount < 1)
                {
                    errors.Add("Câu multiple choice phải có ít nhất 1 đáp án đúng.");
                }

                if (errors.Count > 0)
                {
                    preview.Errors.Add(new QuestionImportError
                    {
                        RowNumber = row.RowNumber,
                        Message = string.Join(" ", errors)
                    });
                }
                else
                {
                    preview.ValidRows.Add(row);
                }
            }

            return preview;
        }

        public string GetTextTemplate()
        {
            return """
Question: What is 2 + 2?
Type: single
* 4
- 3
- 5
Explanation: 2 + 2 equals 4.

Question: Select prime numbers
Type: multiple
* 2
* 3
- 4
- 6
Explanation: 2 and 3 are prime numbers.
""";
        }

        private static QuestionImportRow ParseTextBlock(string block, int rowNumber)
        {
            var row = new QuestionImportRow { RowNumber = rowNumber };
            var contentLines = new List<string>();
            var explanationLines = new List<string>();

            foreach (var rawLine in block.Replace("\r\n", "\n").Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (TryReadPrefixedValue(line, "Question:", out var question) ||
                    TryReadPrefixedValue(line, "Q:", out question))
                {
                    contentLines.Add(question);
                    continue;
                }

                if (TryReadPrefixedValue(line, "Type:", out var type))
                {
                    row.QuestionType = ParseQuestionType(type) ?? 0;
                    continue;
                }

                if (TryReadPrefixedValue(line, "Explanation:", out var explanation) ||
                    TryReadPrefixedValue(line, "Explain:", out explanation) ||
                    TryReadPrefixedValue(line, "E:", out explanation))
                {
                    explanationLines.Add(explanation);
                    continue;
                }

                if (line.StartsWith(">"))
                {
                    explanationLines.Add(line[1..].Trim());
                    continue;
                }

                if (TryParseAnswerLine(line, out var answer))
                {
                    row.Answers.Add(answer);
                    continue;
                }

                contentLines.Add(line);
            }

            row.Content = string.Join(Environment.NewLine, contentLines).Trim();
            row.Explanation = NullIfWhiteSpace(string.Join(Environment.NewLine, explanationLines));
            return row;
        }

        private static bool TryParseAnswerLine(string line, out QuestionImportAnswer answer)
        {
            answer = new QuestionImportAnswer();
            var text = line.Trim();

            if (text.StartsWith("[x]", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("[*]", StringComparison.OrdinalIgnoreCase))
            {
                answer.Content = text[3..].Trim();
                answer.IsCorrect = true;
                return !string.IsNullOrWhiteSpace(answer.Content);
            }

            if (text.StartsWith("[ ]", StringComparison.OrdinalIgnoreCase))
            {
                answer.Content = text[3..].Trim();
                answer.IsCorrect = false;
                return !string.IsNullOrWhiteSpace(answer.Content);
            }

            if (text.StartsWith("*"))
            {
                answer.Content = text[1..].Trim();
                answer.IsCorrect = true;
                return !string.IsNullOrWhiteSpace(answer.Content);
            }

            if (text.StartsWith("-"))
            {
                answer.Content = text[1..].Trim();
                answer.IsCorrect = false;
                return !string.IsNullOrWhiteSpace(answer.Content);
            }

            var match = Regex.Match(text, @"^[A-Ha-h][\.\)]\s*(?<star>\*)?\s*(?<content>.+)$");
            if (match.Success)
            {
                answer.Content = match.Groups["content"].Value.Trim();
                answer.IsCorrect = match.Groups["star"].Success;
                return !string.IsNullOrWhiteSpace(answer.Content);
            }

            return false;
        }

        private static List<List<string>> ReadFirstWorksheet(Stream stream)
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var sharedStrings = ReadSharedStrings(archive);
            var worksheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                ?? throw new InvalidDataException("Không tìm thấy sheet1 trong file Excel.");

            using var worksheetStream = worksheetEntry.Open();
            var document = XDocument.Load(worksheetStream);
            var rows = new List<List<string>>();

            foreach (var rowElement in document.Descendants(SpreadsheetNs + "row"))
            {
                var valuesByColumn = new Dictionary<int, string>();
                var fallbackColumnIndex = 0;

                foreach (var cell in rowElement.Elements(SpreadsheetNs + "c"))
                {
                    var reference = (string?)cell.Attribute("r");
                    var columnIndex = !string.IsNullOrWhiteSpace(reference)
                        ? GetColumnIndex(reference)
                        : fallbackColumnIndex;

                    valuesByColumn[columnIndex] = ReadCellValue(cell, sharedStrings);
                    fallbackColumnIndex = columnIndex + 1;
                }

                if (valuesByColumn.Count == 0)
                {
                    rows.Add(new List<string>());
                    continue;
                }

                var maxColumn = valuesByColumn.Keys.Max();
                var row = Enumerable.Repeat(string.Empty, maxColumn + 1).ToList();
                foreach (var (column, value) in valuesByColumn)
                {
                    row[column] = value.Trim();
                }

                rows.Add(row);
            }

            return rows;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry is null)
            {
                return new List<string>();
            }

            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            return document.Descendants(SpreadsheetNs + "si")
                .Select(si => string.Concat(si.Descendants(SpreadsheetNs + "t").Select(t => (string?)t ?? string.Empty)))
                .ToList();
        }

        private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
        {
            var type = (string?)cell.Attribute("t");
            if (type == "inlineStr")
            {
                return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(t => (string?)t ?? string.Empty));
            }

            var value = (string?)cell.Element(SpreadsheetNs + "v") ?? string.Empty;
            if (type == "s" &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedStringIndex) &&
                sharedStringIndex >= 0 &&
                sharedStringIndex < sharedStrings.Count)
            {
                return sharedStrings[sharedStringIndex];
            }

            if (type == "b")
            {
                return value == "1" ? "true" : "false";
            }

            return value;
        }

        private static int GetColumnIndex(string cellReference)
        {
            var index = 0;
            foreach (var ch in cellReference)
            {
                if (!char.IsLetter(ch))
                {
                    break;
                }

                index = index * 26 + char.ToUpperInvariant(ch) - 'A' + 1;
            }

            return Math.Max(0, index - 1);
        }

        private static Dictionary<string, int> BuildHeaderMap(List<string> headerRow)
        {
            var map = new Dictionary<string, int>();
            for (var i = 0; i < headerRow.Count; i++)
            {
                var normalized = NormalizeHeader(headerRow[i]);
                if (!string.IsNullOrWhiteSpace(normalized) && !map.ContainsKey(normalized))
                {
                    map[normalized] = i;
                }
            }

            return map;
        }

        private static string GetValue(List<string> row, Dictionary<string, int> headerMap, params string[] names)
        {
            foreach (var name in names)
            {
                if (headerMap.TryGetValue(NormalizeHeader(name), out var index) && index < row.Count)
                {
                    return row[index].Trim();
                }
            }

            return string.Empty;
        }

        private static int? ParseQuestionType(string value)
        {
            var normalized = NormalizeHeader(value);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            if (normalized.Contains('2') || normalized.Contains("multiple") || normalized.Contains("multi") || normalized.Contains("nhieu"))
            {
                return 2;
            }

            if (normalized.Contains('1') || normalized.Contains("single") || normalized.Contains("mot"))
            {
                return 1;
            }

            return null;
        }

        private static bool IsTruthy(string value)
        {
            var normalized = NormalizeHeader(value);
            return normalized is "1" or "true" or "yes" or "y" or "x" or "correct" or "dung" or "da";
        }

        private static string NormalizeHeader(string value)
        {
            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString()
                .Replace("đ", "d", StringComparison.Ordinal)
                .Replace("Đ", "d", StringComparison.Ordinal);
        }

        private static bool TryReadPrefixedValue(string line, string prefix, out string value)
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = line[prefix.Length..].Trim();
                return true;
            }

            value = string.Empty;
            return false;
        }

        private static string? NullIfWhiteSpace(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}